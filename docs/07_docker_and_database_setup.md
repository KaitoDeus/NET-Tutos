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
        VSCode["VS Code (Database Client / SQLTools)"]
        DotnetLocal["dotnet run (src/UI/Web/web.csproj)"]
        Browser["Web Browser (http://localhost:5000)"]
    end

    subgraph DockerCompose ["Docker Compose Environment (net-tutos_default)"]
        subgraph WebContainer ["Container: nettutos-web"]
            WebApp["ASP.NET Core 10 Web App & BFF<br/>Internal Port: 5000"]
        end

        subgraph RunnerContainer ["Container: nettutos-coderunner"]
            CodeRunner["Roslyn C# Sandbox Microservice<br/>Internal Port: 8080 (Host: 5002)"]
        end

        subgraph RabbitContainer ["Container: nettutos-rabbitmq"]
            RabbitMQ["RabbitMQ 3 Management<br/>AMQP: 5672 | Web: 15672"]
        end

        subgraph PostgresContainer ["Container: nettutos-postgres (Healthy)"]
            PostgresDB[("PostgreSQL 17 Alpine<br/>Database: nettutos_db<br/>31 Relational Tables")]
        end

        Volume[("Volume: postgres_data")]
    end

    Browser -->|Port 5000| WebApp
    WebApp -->|Port 8080| CodeRunner
    WebApp -.->|Port 5672| RabbitMQ
    VSCode -->|Port 5432 / Trust Auth| PostgresDB
    DotnetLocal -->|Host=localhost:5432| PostgresDB
    WebApp -->|Host=postgres:5432| PostgresDB
    PostgresDB --- Volume
```

---

## 2. Service Specifications in `docker-compose.yml`

The deployment environment orchestrates four tightly integrated services defined in [`docker-compose.yml`](file:///d:/NET-Tutos/docker-compose.yml):

| Service Name | Image / Build Target | Port Mapping (Host:Container) | Role & Operational Configuration |
| :--- | :--- | :--- | :--- |
| **`postgres`** | `postgres:17-alpine` | `5432:5432` | Core relational persistence engine. Durable storage backed by `postgres_data` volume. Built-in `pg_isready` healthcheck. |
| **`rabbitmq`** | `rabbitmq:3-management-alpine` | `5672:5672`<br/>`15672:15672` | AMQP Message Broker for Event-Driven Microservices. Includes Web Management UI. |
| **`coderunner`** | `build: src/Services/CodeRunner/NET-Tutos.CodeRunner.API/Dockerfile` | `5002:8080` | Dedicated Roslyn C# sandbox execution microservice. Completely decoupled from database. |
| **`gamification`** | `build: src/Services/Gamification/NET-Tutos.Gamification.API/Dockerfile` | `5004:8080` | Gamification microservice with MassTransit RabbitMQ event-driven consumers. |
| **`web`** | `build: src/UI/Web/Dockerfile` | `5000:5000` | Flagship ASP.NET Core 10 MVC application & Mobile Web APIs. Proxies to `coderunner:8080`. |

---

## 3. Entity Framework Core (Npgsql) Configuration

### 3.1. NuGet Dependencies
The web application project [`src/UI/Web/web.csproj`](file:///d:/NET-Tutos/src/UI/Web/web.csproj) references the latest PostgreSQL relational provider:
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
```

### 3.2. Resilient Provider Auto-Fallback Mechanism
In [`web/Program.cs`](file:///d:/NET-Tutos/web/Program.cs), the application implements an active, non-blocking connection handshake:
1. **Primary PostgreSQL Connection**: The runtime attempts an initial TCP handshake to PostgreSQL with `Timeout = 3s`. Upon success, `options.UseNpgsql(postgreSqlConnection)` is registered.
2. **Resilient Offline Fallback to SQLite**: If the Docker daemon is offline or PostgreSQL is unreachable, the runtime automatically falls back to local SQLite (`dotnet_tutorials.db`). This ensures developers can execute `dotnet run` immediately without setup roadblocks.
3. **Legacy Timestamp Handling**: The runtime configures `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` to provide full cross-provider compatibility for `DateTime` types (UTC, Local, and Unspecified).

### 3.3. Standardized 31-Table Schema Catalog
The schema engine manages 31 relational tables within the `public` schema:
- **Identity & Access Management (RBAC)**: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.
- **LMS Curriculum & Courseware**: `Categories`, `Tutorials`, `QuizQuestions`, `CodeSnippets`, `UserLessonProgresses`, `CourseEnrollments`, `QuizAttempts`, `Certificates`.
- **Roslyn Sandbox & Challenge Online Judge**: `CodingChallenges`, `CodeTestCases`, `CodeSubmissions`.
- **Community & Social Interaction**: `DiscussionComments`, `CommentUpvotes`.
- **Gamification & Daily Engagement**: `UserBadges`, `DailyCheckIns`, `UserNotifications`, `ActivityFeedItems`.
- **Capstone Engineering Projects**: `CapstoneProjects`, `ProjectSubmissions`.
- **Technical Interview Simulator & Spaced Repetition**: `InterviewFlashcards`, `UserFlashcardProgresses`.
- **Personalized Learning Plans & Developer Showcases**: `StudyPlans`, `StudyPlanItems`, `DeveloperProfiles`.

---

## 4. Operational Runbook

### Mode 1: Full-Stack Docker Compose Deployment (Recommended)
Suitable for complete integration testing, staging, and containerized evaluation:
```bash
docker compose up -d
```
- **Web Application & Swagger UI**: [http://localhost:5000](http://localhost:5000)
- **PostgreSQL Database Endpoint**: `localhost:5432` (`Database: nettutos_db`, `User: postgres`, `Pass: postgres`)

To stop all services:
```bash
docker compose down
```

---

### Mode 2: Hybrid Local Development (.NET SDK + Postgres Container)
Optimized for local C#, Razor, and API debugging in Visual Studio / VS Code:
```powershell
# 1. Start the PostgreSQL container only
docker compose up -d postgres

# 2. Launch the Web Application locally using the .NET CLI
dotnet run --project src/UI/Web/web.csproj
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
