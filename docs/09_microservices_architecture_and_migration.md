# 09. Microservices Architecture, Service Decomposition & Strangler Fig Migration

Document Type: Enterprise Distributed Architecture Specification & Migration Guide  
Author: Senior Principal Solution Architect & Lead Distributed Systems Engineer  
System: **.NET Tutos - Comprehensive .NET Learning & Assessment Platform**  
Target Framework: **.NET 10 (`net10.0`), C# 14, ASP.NET Core**  
Architecture Pattern: **Strangler Fig Migration, Domain-Driven Design (DDD), Clean Architecture, Event-Driven Architecture (EDA)**  
Container Orchestration: **Docker Compose (PostgreSQL 17, RabbitMQ 3, CodeRunner.API, Gamification.API, Web BFF)**  
Status: **Phase 1, Phase 2 & Phase 3 Implemented & Verified in CI Pipeline (67/67 Tests Passing)**  

---

## 1. System Transformation & Architectural Drivers

To overcome monolithic scaling limitations and eliminate CPU contention caused by in-process Roslyn C# compilation, .NET Tutos is transitioning from a modular monolith to a distributed **Microservices Architecture**.

### Key Architectural Drivers:
1. **Compute & Security Isolation:** C# Roslyn compilation and dynamic script execution run in an isolated service process ([`CodeRunner.API`](file:///d:/NET-Tutos/src/Services/CodeRunner/NET-Tutos.CodeRunner.API)), ensuring heavy loops or memory spikes never degrade the web frontend or database transactions.
2. **Event-Driven Gamification & Retention:** Gamification metrics (streaks, XP curves, badges, leaderboards) are decoupled into a dedicated service ([`Gamification.API`](file:///d:/NET-Tutos/src/Services/Gamification/NET-Tutos.Gamification.API)) consuming asynchronous domain events via MassTransit and RabbitMQ.
3. **Zero-Downtime Migration (Strangler Fig Pattern):** Rather than a disruptive "big bang" rewrite, bounded contexts are extracted incrementally. The monolithic web app delegates to microservices with automatic, resilient fallback to in-process execution.
4. **Standardized Solution Folders & Explicit Layer Taxonomy:** The solution index ([`NET-Tutos.slnx`](file:///d:/NET-Tutos/NET-Tutos.slnx)) organizes projects into `/01. UI/`, `/02. Services/`, `/03. Infrastructure/`, and `/04. Tests/`.

```mermaid
flowchart TD
    Clients["Learners & Mobile Clients\n(Web Browser / .NET MAUI App)"] --> YARP["API Gateway & Reverse Proxy\n(Microsoft YARP / Port 5000)"]
    
    subgraph Frontend_BFF["UI Layer (User Interface & BFF)"]
        YARP --> WebApp["Web MVC & Razor Frontend\n(src/UI/Web / Port 5000)\n[Fallback: In-Process Roslyn & Local DB]"]
    end

    subgraph Microservices_Ecosystem["Services Layer (Backend Microservices)"]
        WebApp -- "HTTP (Resilient Delegation)\nPort 5002" --> SvcRunner["CodeRunner.API\n(src/Services/CodeRunner)\n[Roslyn C# 14, Test Case Judge]"]
        WebApp -- "HTTP / Sync Delegation\nPort 5004" --> SvcGamify["Gamification.API\n(src/Services/Gamification)\n[Streak, XP, Badges, Leaderboard]"]
        YARP -. "Planned" .-> SvcAI["AiTutor.API\n(Complexity, Test Gen, Voice Mock Interview)"]
        YARP -. "Planned" .-> SvcCurriculum["Curriculum.API\n(101 Lessons, Quizzes, Certificates)"]
        YARP -. "Planned" .-> SvcIdentity["Identity.API\n(Auth, JWT Tokens, Profiles)"]
    end

    subgraph Messaging_Persistence["Infrastructure Layer (Event Bus & Persistence)"]
        RabbitMQ["RabbitMQ 3 Management\n(Port 5672 AMQP / 15672 Web)\nMassTransit Event Bus"]
        Postgres[("PostgreSQL 17 Alpine\n(Port 5432)\nnettutos_db")]
        
        WebApp --- Postgres
        SvcGamify --- Postgres
        WebApp -. "Publish Integration Events" .-> RabbitMQ
        RabbitMQ -. "Consume Events" .-> SvcGamify
    end

    classDef gw fill:#1e1b4b,stroke:#6366f1,stroke-width:1.5px,color:#e0e7ff;
    classDef svc fill:#0f172a,stroke:#3b82f6,stroke-width:1.5px,color:#f8fafc;
    classDef infra fill:#14532d,stroke:#22c55e,stroke-width:1px,color:#f0fdf4;

    class YARP,WebApp gw;
    class SvcRunner,SvcAI,SvcGamify,SvcCurriculum,SvcIdentity svc;
    class RabbitMQ,Postgres infra;
```

---

## 2. Shared Infrastructure Layer (`src/Infrastructure`)

The platform defines reusable, enterprise-grade building blocks referenced by all microservices:

### 2.1. `NET-Tutos.BuildingBlocks.Common`
- **Location:** [`src/Infrastructure/Common/`](file:///d:/NET-Tutos/src/Infrastructure/Common)
- **Namespace:** `NET_Tutos.BuildingBlocks.Common`
- **Artifacts:**
  - `ApiResponse<T>`: Standardized JSON envelope providing `Success`, `Data`, `Message`, `Errors`, and ISO-8601 `Timestamp`.
  - `Result<T>` and `Result`: Result pattern implementation enabling expressive, exception-free functional error handling across domain boundaries.

### 2.2. `NET-Tutos.BuildingBlocks.EventBus`
- **Location:** [`src/Infrastructure/EventBus/`](file:///d:/NET-Tutos/src/Infrastructure/EventBus)
- **Namespace:** `NET_Tutos.BuildingBlocks.EventBus`
- **Artifacts:**
  - `IIntegrationEvent` & `IntegrationEvent`: Base records assigning a unique `Guid EventId` and `DateTime OccurredOn`.
  - `ChallengeSolvedIntegrationEvent`: Emitted upon accepted code challenge evaluations. Transports `UserId`, `ChallengeId`, `ChallengeTitle`, `XpAwarded`, and `ExecutionDurationMs`.
  - `LessonCompletedIntegrationEvent`: Emitted upon tutorial completion. Transports `UserId`, `TutorialId`, `TutorialTitle`, and `XpAwarded`.
  - `ExamPassedIntegrationEvent`: Emitted upon passing certification assessments. Transports `UserId`, `CategoryId`, `ExamTitle`, `ScorePercentage`, and `XpAwarded`.

---

## 3. Microservice Deep-Dive: `CodeRunner.API`

Encapsulates Roslyn C# script compilation, sandboxed runtime execution, and algorithmic test judging.

### 3.1. Project Metadata & Dependencies
- **Project File:** [`src/Services/CodeRunner/NET-Tutos.CodeRunner.API/NET-Tutos.CodeRunner.API.csproj`](file:///d:/NET-Tutos/src/Services/CodeRunner/NET-Tutos.CodeRunner.API/NET-Tutos.CodeRunner.API.csproj)
- **Target Framework:** `net10.0` (ASP.NET Core Web SDK)
- **Key Dependencies:**
  - `Microsoft.CodeAnalysis.CSharp.Scripting` (v5.0.0)
  - `NET-Tutos.BuildingBlocks.Common` (Project Reference)

### 3.2. RESTful API Endpoints
1. **Execute Arbitrary C# Script:** `POST /api/v1/runner/execute`
2. **Evaluate Coding Challenge:** `POST /api/v1/runner/evaluate`
3. **Health Check:** `GET /healthz`

---

## 4. Microservice Deep-Dive: `Gamification.API`

Manages learner motivation mechanics: daily streak tracking, exponential XP leveling curves, achievement badge unlocks, and global leaderboards.

### 4.1. Project Metadata & Dependencies
- **Project File:** [`src/Services/Gamification/NET-Tutos.Gamification.API/NET-Tutos.Gamification.API.csproj`](file:///d:/NET-Tutos/src/Services/Gamification/NET-Tutos.Gamification.API/NET-Tutos.Gamification.API.csproj)
- **Target Framework:** `net10.0` (ASP.NET Core Web SDK)
- **Key Dependencies:**
  - `MassTransit.RabbitMQ` (v9.2.5)
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.0)
  - `Microsoft.EntityFrameworkCore.InMemory` (v10.0.0 for isolated tests)
  - `NET-Tutos.BuildingBlocks.Common` (Project Reference)
  - `NET-Tutos.BuildingBlocks.EventBus` (Project Reference)

### 4.2. Domain Models & Relational Schema
- `UserStreak`: Stores `UserId`, `CurrentStreak`, `LongestStreak`, `LastCheckInDate`, and `UpdatedAt`.
- `DailyCheckIn`: Records historical check-ins with `CheckInDate`, `StreakDay`, and `XpEarned`.
- `Badge`: System catalog of achievements with `Code`, `Name`, `Description`, `IconClass`, and `BadgeColor`.
- `UserBadge`: Unlocked badges per learner with `UnlockedAt`.
- `UserXpRecord`: Ledger of XP transactions tagged by source (`StreakCheckIn`, `ChallengeSolved`, `LessonCompleted`, `ExamPassed`).

### 4.3. Event-Driven Architecture (MassTransit Consumers)
`Gamification.API` subscribes to integration events emitted over RabbitMQ without requiring synchronous coordination:

```mermaid
sequenceDiagram
    autonumber
    actor Learner as Learner
    participant Web as Web Application
    participant Bus as RabbitMQ / MassTransit
    participant Gamify as Gamification.API
    participant DB as Gamification Database

    Learner->>Web: Complete Lesson / Pass Exam / Solve Challenge
    Web->>Bus: Publish(LessonCompletedIntegrationEvent)
    Bus->>Gamify: Consume(LessonCompletedConsumer)
    Gamify->>DB: AwardXpAsync(userId, xp, source, description)
    Gamify->>DB: Check & Award Milestone Badges
    Gamify-->>DB: Commit Transaction
```

- **`ChallengeSolvedConsumer`:** Awards challenge XP and unlocks the `"code-master"` badge upon achieving milestones.
- **`LessonCompletedConsumer`:** Awards tutorial XP and unlocks the `"scholar"` badge upon milestone completions.
- **`ExamPassedConsumer`:** Awards certification XP and unlocks the `"certified-pro"` badge upon achieving $\ge 80\%$ scores.

### 4.4. RESTful API Endpoints
1. **Get User Streak:** `GET /api/v1/gamification/streak/{userId}`
2. **Perform Daily Check-In:** `POST /api/v1/gamification/streak/check-in`
3. **List Badges Catalog & Unlocks:** `GET /api/v1/gamification/badges?userId={userId}`
4. **Retrieve Leaderboard:** `GET /api/v1/gamification/leaderboard?top=50`
5. **Health Check:** `GET /healthz`

---

## 5. Resilient Delegation & Strangler Fig Dual-Write Fallback Pattern

Both [`CodeExecutionService.cs`](file:///d:/NET-Tutos/src/UI/Web/Services/CodeExecutionService.cs) and [`StreakService.cs`](file:///d:/NET-Tutos/src/UI/Web/Services/StreakService.cs) implement the **Strangler Fig Pattern**:

1. **Intelligent HTTP Client Delegation:** If microservice endpoints are configured in `appsettings.json`, calls are dispatched over HTTP with short timeouts (5s–8s).
2. **Graceful Fallback:** If the microservice is unreachable, returns an error status code, or experiences a network failure, the call is logged as a warning, and the system seamlessly falls back to local in-process execution (in-process Roslyn execution or local database transaction).
3. **Zero Learner Disruption:** End-users experience zero downtime or fatal exceptions during microservice restarts, redeployments, or temporary network partitions.

### Configuration (`src/UI/Web/appsettings.json`):
```json
"Services": {
  "CodeRunner": {
    "Url": "http://localhost:5002"
  },
  "Gamification": {
    "Url": "http://localhost:5004"
  }
}
```

---

## 6. Docker Compose Multi-Container Orchestration

The platform is orchestrated via [`docker-compose.yml`](file:///d:/NET-Tutos/docker-compose.yml) on a shared bridge network:

| Container Name | Service | Image / Dockerfile | Ports | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **`nettutos-postgres`** | `postgres` | `postgres:17-alpine` | `5432:5432` | Relational storage for Identity, Web, and Gamification |
| **`nettutos-rabbitmq`** | `rabbitmq` | `rabbitmq:3-management-alpine` | `5672:5672`<br/>`15672:15672` | AMQP Message Broker & Management Web Console |
| **`nettutos-coderunner`** | `coderunner` | `src/Services/CodeRunner/NET-Tutos.CodeRunner.API/Dockerfile` | `5002:8080` | Sandboxed Roslyn script compilation & challenge judge |
| **`nettutos-gamification`** | `gamification` | `src/Services/Gamification/NET-Tutos.Gamification.API/Dockerfile` | `5004:8080` | Gamification microservice with MassTransit consumers |
| **`nettutos-web`** | `web` | `src/UI/Web/Dockerfile` | `5000:5000` | ASP.NET Core 10 Web Application & BFF Frontend |

To spin up the entire distributed ecosystem:
```powershell
docker compose up -d --build
```

---

## 7. Verification & Automated Quality Gates

The distributed microservices architecture is verified by a 67-test automated suite executed across all projects:

```powershell
# 1. Run Gamification microservice domain & consumer tests (7 tests)
dotnet test tests/NET-Tutos.Gamification.Tests/NET-Tutos.Gamification.Tests.csproj

# 2. Run CodeRunner microservice Roslyn sandbox tests (6 tests)
dotnet test tests/NET-Tutos.CodeRunner.Tests/NET-Tutos.CodeRunner.Tests.csproj

# 3. Run Web application unit & service tests (42 tests)
dotnet test tests/NET-Tutos.UnitTests/NET-Tutos.UnitTests.csproj

# 4. Run HTTP integration pipeline tests (12 tests)
dotnet test tests/NET-Tutos.IntegrationTests/NET-Tutos.IntegrationTests.csproj
```

**Quality Gate Result:** **67/67 Tests Passing (100% Pass Rate, 0 Failed, 0 Skipped).**  
All test suites are permanently enforced in GitHub Actions continuous delivery via [`.github/workflows/ci.yml`](file:///d:/NET-Tutos/.github/workflows/ci.yml).
