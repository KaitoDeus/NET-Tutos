# 07. PostgreSQL 17 & Docker Compose Infrastructure Specification

Document Type: Infrastructure, Database Architecture & DevOps Specification  
Author: Senior Solution Architect & DevOps Lead  
System: **.NET Tutos - Comprehensive .NET Learning & Assessment Platform**  
Database Engine: **PostgreSQL 17 (Alpine)**  
ORM Provider: **`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3**  
Container Orchestration: **Docker & Docker Compose**  
Status: **Production Ready & Verified**

---

## 1. Database Architecture Overview

To maximize environmental portability, concurrent transaction throughput, and cloud readiness across staging, CI/CD, and production deployments, the .NET Tutos platform utilizes **PostgreSQL 17** orchestrated via **Docker Compose** as its primary persistence engine.

```mermaid
graph TD
    subgraph HostMachine ["Host Machine (Windows / Linux / macOS)"]
        VSCode["VS Code (SQLTools + Atus Extension)"]
        DotnetLocal["dotnet run (src/Presentation/Web/web.csproj)"]
        Browser["Web Browser (http://localhost:5000 or :5001)"]
    end

    subgraph DockerCompose ["Docker Compose Environment (13 Containers)"]
        Gateway["nettutos-gateway (YARP Port 5000)"]
        WebApp["nettutos-web (Port 5001 -> 5000)"]
        CodeRunner["nettutos-coderunner (Port 5002 -> 8080)"]
        Gamification["nettutos-gamification (Port 5004 -> 8080)"]
        AiTutor["nettutos-aitutor (Port 5006 -> 8080)"]
        Catalog["nettutos-catalog (Port 5008 -> 8080)"]
        Redis["nettutos-redis (Port 6379)"]
        RabbitMQ["nettutos-rabbitmq (Port 5672 / 15672)"]
        Kafka["nettutos-kafka (Port 9092 / 29092)"]
        Prometheus["nettutos-prometheus (Port 9090)"]
        Tempo["nettutos-tempo (OTLP gRPC 4317 / HTTP 3200)"]
        Grafana["nettutos-grafana (Port 3000)"]
        PostgresDB[("nettutos-postgres (Port 5432)\nnettutos_db & nettutos_gamification_db")]
    end

    Browser --> Gateway
    Gateway --> WebApp
    Gateway --> CodeRunner
    Gateway --> Gamification
    Gateway --> AiTutor
    Gateway --> Catalog
    WebApp --> Redis
    WebApp --> RabbitMQ
    RabbitMQ --> Gamification
    WebApp --> Kafka
    WebApp --> PostgresDB
    Gamification --> PostgresDB
    Prometheus -. "Scrape /metrics" .-> WebApp
    Prometheus -. "Scrape /metrics" .-> Gateway
    Grafana --> Prometheus
    VSCode -->|Port 5432 / Trust Auth| PostgresDB
    VSCode -->|"/api/v1/ide/*"| WebApp
    DotnetLocal -->|Host=localhost:5432| PostgresDB
```

---

## 2. Service Specifications in `docker-compose.yml`

The deployment environment orchestrates 13 integrated containers defined in [`docker-compose.yml`](file:///d:/NET-Tutos/docker-compose.yml) (plus Helm/Kubernetes chart parity in [`deploy/helm/nettutos`](file:///d:/NET-Tutos/deploy/helm/nettutos)):

| Service Name | Image / Build Target | Port Mapping (Host:Container) | Role & Operational Configuration |
| :--- | :--- | :--- | :--- |
| **`postgres`** | `pgvector/pgvector:pg17` | `5432:5432` | Core relational & HNSW vector persistence (`nettutos_db` & `nettutos_gamification_db`) backed by `postgres_data`. Built-in `pg_isready` healthcheck. |
| **`redis`** | `redis:7-alpine` | `6379:6379` | L2 Cache, DataProtection keys, SignalR backplane & distributed Gateway quotas (`redis_data` volume). |
| **`rabbitmq`** | `rabbitmq:3-management-alpine` | `5672:5672`<br/>`15672:15672` | AMQP Message Broker for MassTransit integration events & Web Management UI. |
| **`kafka`** | `apache/kafka:latest` | `9092:9092`<br/>`29092:29092` | KRaft event streaming broker (AI telemetry, CodeRunner executions, user learning activities). |
| **`coderunner`** | `build: src/Services/CodeRunner/NET-Tutos.CodeRunner.API/Dockerfile` | `5002:8080` | Dedicated Roslyn C# 14 sandbox execution microservice exposing `/metrics` & `/healthz`. |
| **`gamification`** | `build: src/Services/Gamification/NET-Tutos.Gamification.API/Dockerfile` | `5004:8080` | Gamification microservice (`MassTransit.RabbitMQ 8.3.7`) + isolated PostgreSQL DB + `/metrics`. |
| **`aitutor`** | `build: src/Services/AiTutor/NET-Tutos.AiTutor.API/Dockerfile` | `5006:8080` | Algorithmic Big-O, Diagnostics, xUnit synthesis & Mock Interview microservice + `/metrics`. |
| **`catalog`** | `build: src/Services/Catalog/NET-Tutos.Catalog.API/Dockerfile` | `5008:8080` | Catalog, 7-stage curriculum, exam grading & certificate verification microservice + `/metrics`. |
| **`web`** | `build: src/Presentation/Web/Dockerfile` | `5001:5000` | ASP.NET Core 10 MVC Web BFF, OAuth 2.0 SSO, Hybrid RAG, Neural TTS, IDE Bridge (`/api/v1/ide/*`) & `/metrics`. |
| **`gateway`** | `build: src/Gateways/Gateway/Dockerfile` | `5000:8080` | Microsoft YARP API Gateway & Reverse Proxy (Rate Limiting, `X-Correlation-ID` & `/metrics`). |
| **`prometheus`** | `prom/prometheus:latest` | `9090:9090` | Scrapes `/metrics` across `web`, `gateway`, 4 microservices & `prometheus` every `5s` (`prometheus_data` volume). |
| **`tempo`** | `grafana/tempo:latest` | `3200:3200`<br/>`4317:4317` | Distributed OTLP trace receiver (`gRPC :4317`, HTTP `:4318`) synced into Grafana (`tempo_data` volume). |
| **`grafana`** | `grafana/grafana:latest` | `3000:3000` | Pre-provisioned Prometheus + Tempo datasources & 12-panel dashboard `nettutos-overview` (`grafana_data` volume). |

---

## 3. Entity Framework Core (Npgsql) Configuration

### 3.1. NuGet Dependencies
The web application project [`src/Presentation/Web/web.csproj`](file:///d:/NET-Tutos/src/Presentation/Web/web.csproj) references the latest PostgreSQL relational provider:
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
```

### 3.2. Resilient Provider Auto-Fallback Mechanism
In [`src/Presentation/Web/Program.cs`](file:///d:/NET-Tutos/src/Presentation/Web/Program.cs), the application implements an active, non-blocking connection handshake:
1. **Primary PostgreSQL Connection**: The runtime attempts an initial TCP handshake to PostgreSQL with `Timeout = 3s`. Upon success, `options.UseNpgsql(postgreSqlConnection)` is registered.
2. **Resilient Offline Fallback to SQLite**: If the Docker daemon is offline or PostgreSQL is unreachable, the runtime automatically falls back to local SQLite (`dotnet_tutorials.db`). This ensures developers can execute `dotnet run` immediately without setup roadblocks.
3. **Legacy Timestamp Handling**: The runtime configures `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` to provide full cross-provider compatibility for `DateTime` types (UTC, Local, and Unspecified).

### 3.3. Standardized 31-Table Schema Catalog
The schema engine manages 31 relational tables within the `public` schema (including `AiConversations` and `AiMessages` for persistent Atus AI chat history):
- **Identity & Access Management (RBAC)**: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.
- **LMS Curriculum & Courseware**: `Categories`, `Tutorials`, `QuizQuestions`, `CodeSnippets`, `UserLessonProgresses`, `CourseEnrollments`, `QuizAttempts`, `Certificates`.
- **Roslyn Sandbox & Challenge Online Judge**: `CodingChallenges`, `CodeTestCases`, `CodeSubmissions`.
- **Community & Social Interaction**: `DiscussionComments`, `CommentUpvotes`.
- **Gamification & Daily Engagement**: `UserBadges`, `DailyCheckIns`, `UserNotifications`, `ActivityFeedItems`.
- **Capstone Engineering Projects**: `CapstoneProjects`, `ProjectSubmissions`.
- **Technical Interview Simulator & Spaced Repetition**: `InterviewFlashcards`, `UserFlashcardProgresses`.
- **Personalized Learning Plans, Showcases & Atus Chat History**: `StudyPlans`, `StudyPlanItems`, `DeveloperProfiles`, `AiConversations`, `AiMessages`.

---

## 4. Operational Runbook

### Mode 1: Full-Stack Docker Compose Deployment (Recommended)
Suitable for complete integration testing, staging, and containerized evaluation:
```bash
docker compose up -d --build
```
- **YARP API Gateway Entry Point**: [http://localhost:5000](http://localhost:5000)
- **Web Application & Swagger UI**: [http://localhost:5001](http://localhost:5001) (`/swagger`)
- **Prometheus Targets UI**: [http://localhost:9090/targets](http://localhost:9090/targets)
- **Grafana Overview Dashboard**: [http://localhost:3000/d/nettutos-overview](http://localhost:3000/d/nettutos-overview)
- **PostgreSQL Database Endpoint**: `localhost:5432` (`Database: nettutos_db`, `User: postgres`, `Pass: postgres`)

To stop all services:
```bash
docker compose down
```

---

### Mode 2: Hybrid Local Development (.NET SDK + Postgres Container)
Optimized for local C#, Razor, and API debugging in Visual Studio / VS Code:
```powershell
# 1. Start infrastructure containers (PostgreSQL, Redis, Kafka)
docker compose up -d postgres redis kafka

# 2. Launch the Web Application locally using the .NET CLI
dotnet run --project src/Presentation/Web/web.csproj
```

---

## 5. Visual Database Tooling & IDE Integration (VS Code / DBeaver)

### Connection Parameters
- **Host**: `localhost` (or `127.0.0.1`)
- **Port**: `5432`
- **Database**: `nettutos_db`
- **User**: `postgres`
- **Password**: `postgres`
- **SSL Mode**: `disable` (or `false`)
- **Connection URI**: `postgresql://postgres:postgres@localhost:5432/nettutos_db`

### Built-In VS Code Pre-Configuration
The workspace includes pre-configured settings in [`.vscode/settings.json`](file:///d:/NET-Tutos/.vscode/settings.json) for the `SQLTools` extension under the profile **`NET-Tutos PostgreSQL (Docker)`**. Developers can connect with a single click from the VS Code Activity Bar.

---

## 6. Configuring Live GitHub & Google OAuth 2.0 SSO Credentials

By default, if `GITHUB_CLIENT_ID` or `GOOGLE_CLIENT_ID` are not set in `.env`, NET-Tutos automatically falls back to the built-in **Interactive OAuth 2.0 Dev/Sandbox Consent Screen** (`/Account/ExternalLoginSandbox`). Once you add your live OAuth keys below, clicking **GitHub** or **Google** on `/Account/Login` or `/Account/Register` will redirect directly to GitHub/Google OAuth 2.0 servers.

### 6.1. How to Get GitHub OAuth 2.0 Keys (`GITHUB_CLIENT_ID` & `GITHUB_CLIENT_SECRET`)
1. Sign in to GitHub and navigate to **[GitHub Developer Settings → OAuth Apps](https://github.com/settings/developers)**.
2. Click **"New OAuth App"** and fill in:
   - **Application name**: `NET-Tutos Learning Platform`
   - **Homepage URL**: `http://localhost:5000` (or your production domain `https://your-domain.com`)
   - **Authorization callback URL**: `http://localhost:5000/signin-github` (or `https://your-domain.com/signin-github`)
3. Click **"Register application"**.
4. Copy the **Client ID** (`GITHUB_CLIENT_ID`), then click **"Generate a new client secret"** and copy the **Client Secret** (`GITHUB_CLIENT_SECRET`).

### 6.2. How to Get Google OAuth 2.0 Keys (`GOOGLE_CLIENT_ID` & `GOOGLE_CLIENT_SECRET`)
1. Open **[Google Cloud Console → APIs & Services → Credentials](https://console.cloud.google.com/apis/credentials)**.
2. Create or select a project (e.g., `NET-Tutos`).
3. Go to **OAuth consent screen**:
   - Choose **External**, enter App name `NET-Tutos`, user support email, and developer contact email.
   - Add scopes: `openid`, `.../auth/userinfo.email`, `.../auth/userinfo.profile`.
4. Go to **Credentials** → **Create Credentials** → **OAuth client ID**:
   - **Application type**: `Web application`
   - **Authorized JavaScript origins**: `http://localhost:5000`
   - **Authorized redirect URIs**: `http://localhost:5000/signin-google` (and `http://localhost:5001/signin-google` if testing direct Web container port)
5. Click **Create** and copy the **Client ID** (`GOOGLE_CLIENT_ID`) and **Client Secret** (`GOOGLE_CLIENT_SECRET`).

### 6.3. Apply Keys in `.env` & Restart Docker
Create or edit `.env` in the repository root (`cp .env.example .env`):
```env
GITHUB_CLIENT_ID=<your-github-client-id>
GITHUB_CLIENT_SECRET=<your-github-client-secret>
GOOGLE_CLIENT_ID=<your-google-client-id>
GOOGLE_CLIENT_SECRET=<your-google-client-secret>
```
Then restart the Web container:
```bash
docker compose up -d web
```
