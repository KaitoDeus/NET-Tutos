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
        DotnetLocal["dotnet run (web/web.csproj)"]
        Browser["Web Browser (http://localhost:5000)"]
    end

    subgraph DockerCompose ["Docker Compose Environment (net-tutos_default)"]
        subgraph WebContainer ["Container: nettutos-web"]
            WebApp["ASP.NET Core 10 MVC & REST API<br/>Internal Port: 5000"]
        end

        subgraph PostgresContainer ["Container: nettutos-postgres (Healthy)"]
            PostgresDB[("PostgreSQL 17 Alpine<br/>Database: nettutos_db<br/>31 Relational Tables")]
        end

        subgraph PgAdminContainer ["Container: nettutos-pgadmin"]
            PgAdmin["pgAdmin 4 Web GUI<br/>Port: 5050"]
        end

        Volume[("Volume: postgres_data")]
    end

    Browser -->|Port 5000| WebApp
    VSCode -->|Port 5432 / Trust Auth| PostgresDB
    DotnetLocal -->|Host=localhost:5432| PostgresDB
    WebApp -->|Host=postgres:5432| PostgresDB
    PgAdmin -->|Host=postgres:5432| PostgresDB
    PostgresDB --- Volume
```

---

## 2. Service Specifications in `docker-compose.yml`

The deployment environment orchestrates three tightly integrated services defined in [`docker-compose.yml`](file:///d:/NET-Tutos/docker-compose.yml):

| Service Name | Image / Build Target | Port Mapping (Host:Container) | Role & Operational Configuration |
| :--- | :--- | :--- | :--- |
| **`postgres`** | `postgres:17-alpine` | `5432:5432` | Core relational persistence engine. Durable storage backed by `postgres_data` volume. Built-in `pg_isready` healthcheck. |
| **`web`** | `build: . (Dockerfile)` | `5000:5000` | Flagship ASP.NET Core 10 MVC application & Mobile Web APIs. Automatically waits until `postgres` becomes healthy before starting. |
| **`pgadmin`** | `dpage/pgadmin4:latest` | `5050:80` | Official PostgreSQL visual web administration tool (Default credentials: `admin@nettutos.com` / `admin`). |

---

## 3. Entity Framework Core (Npgsql) Configuration

### 3.1. NuGet Dependencies
The web application project [`web/web.csproj`](file:///d:/NET-Tutos/web/web.csproj) references the latest PostgreSQL relational provider:
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
- **pgAdmin 4 Web GUI**: [http://localhost:5050](http://localhost:5050)

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
dotnet run --project web/web.csproj
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
