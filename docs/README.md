# System Requirements Specifications & Architectural Overview (.NET Tutos Platform)

Document Type: Master Index & Architecture Specification Suite  
Author: Senior Business Analyst / Enterprise Solution Specialist  
System: .NET Tutos - Comprehensive .NET Learning & Assessment Platform  
Scope: Web Application (ASP.NET Core 10), Microservices & YARP Gateway, Kafka & Prometheus/Grafana Observability, VS Code Extension, AI Tutor Engine, Roslyn Sandbox, Mobile Client (.NET MAUI 10 — Separate Roadmap)  
Version: Release 3.0  
Status: Verified & Approved for Production (12 Docker Containers | PostgreSQL 17, Redis 7, RabbitMQ 3, Kafka, Prometheus & Grafana)  

---

## 1. Document Suite Overview

This documentation suite serves as the **Single Source of Truth (SSOT)** for the .NET Tutos platform across engineering and product teams (Product Owners, Solution Architects, Lead Developers, QA Engineers, DevOps, and Stakeholders).

The suite standardizes requirements across the entire software delivery lifecycle, encompassing market problem statements, target user personas, multi-tier system topology, functional requirements (BRD), non-functional requirements (NFR), relational schema models (ERD), and cross-platform RESTful API contracts.

---

## 2. Document Suite Structure

```
d:\NET-Tutos\docs\
├── README.md                           # Master Index & Requirements Traceability Matrix (Current Document)
├── 01_business_overview.md             # Business Vision, Strategic Value Proposition & Personas
├── 02_system_architecture_overview.md  # Multi-Tier Ecosystem Topology, Tech Stack & Sequence Flows
├── 03_functional_requirements_brd.md   # Functional Requirements Specification (BRD) for 7 Core Modules
├── 04_non_functional_requirements.md   # Quality Attributes, Performance SLAs, Security & PWA Specs
├── 05_data_models_and_api_specs.md     # Relational ERD Schemas & RESTful Integration API Contracts
├── 06_architecture_and_design_blueprint.md # Architecture Blueprint, Folder Breakdown, ERD & UI/UX Wireframes
├── 07_docker_and_database_setup.md     # PostgreSQL 17, Npgsql EF Core 10 & Docker Compose Architecture
├── 08_testing_strategy_and_test_cases.md # Automated Testing Strategy, Test Matrix & Exception Specs
├── 09_microservices_architecture_and_migration.md # Microservices Architecture, Service Decomposition & Strangler Fig Migration
├── 10_project_roadmap_and_ai_memory.md # Master Roadmap, Phase 1–11 History & AI Context Memory (SSOT)
└── DESIGN.md                           # Frontend Design System & Anti-AI-Slop Quality Constitution
```

### Detailed Document Summaries:

1. [**01. Business Overview & Value Proposition**](file:///d:/NET-Tutos/docs/01_business_overview.md)
   - Executive project summary and target outcomes.
   - Market problem analysis across 4 foundational challenges.
   - Strategic value propositions and Key Performance Indicators (KPIs).
   - Target personas: University Student / Fresher, Junior Developer, and LMS Instructor.
   - End-to-end Learner Journey Map.

2. [**02. System Architecture Overview**](file:///d:/NET-Tutos/docs/02_system_architecture_overview.md)
   - Multi-tier system topology diagram (Presentation, Gateway/API, Application Services, Data Access, Persistence).
   - Core technology specifications (.NET 10, C# 14, .NET MAUI, EF Core 10, Roslyn Scripting API, SignalR, Monaco Editor).
   - Detailed sequence data flow diagrams:
     - Playground execution, compilation error diagnosis, and automated challenge evaluation.
     - Interactive AI Mock Technical Interview with speech transcription and scoring.
     - Bidirectional cross-platform synchronization between Mobile App and Web App.
   - Security isolation model and Roslyn sandbox boundary controls.

3. [**03. Functional Requirements Document (BRD)**](file:///d:/NET-Tutos/docs/03_functional_requirements_brd.md)
   - Formal User Stories and Given/When/Then Acceptance Criteria across 7 functional modules:
     - **MOD-LMS:** 101-lesson Microsoft .NET curriculum, rich Markdown rendering, assessment quizzes.
     - **MOD-PLAY:** Monaco Editor, execution shortcuts (`Ctrl+Enter`, `Ctrl+S`, `Ctrl+I`), automated judge engine.
     - **MOD-AI:** Algorithmic Big-O analysis ($O(1) \dots O(2^N)$), test failure diagnostics, automated xUnit generation.
     - **MOD-INT:** AI Tech Lead interview simulation, 3-dimensional scoring, speech recognition, competency radar report.
     - **MOD-GAM:** Daily streak tracking, XP points and leveling formula, Hall of Fame leaderboard, SignalR discussions.
     - **MOD-MOB:** .NET MAUI cross-platform client, lesson reading, progress sync, light/dark themes, custom API host configuration.
     - **MOD-ADM:** Administrative management and cohort oversight.

4. [**04. Non-Functional Requirements Document (NFR)**](file:///d:/NET-Tutos/docs/04_non_functional_requirements.md)
   - Performance metrics and SLAs/SLOs: Page load $< 1.2\text{s}$, API latency $< 150\text{ms}$, sandbox timeout $\le 3\text{s}$.
   - Scalability standards: $\ge 1,000$ concurrent active users, memory optimization via `AsNoTracking()` and database pagination.
   - Information security: Roslyn sandbox isolation, PBKDF2 password hashing, JWT Bearer authentication, OWASP Top 10 defenses.
   - Offline resilience via Progressive Web App (PWA) Service Workers and cross-platform responsive design (WCAG 2.1 AA).

5. [**05. Data Models & API Specifications**](file:///d:/NET-Tutos/docs/05_data_models_and_api_specs.md)
   - Relational Entity Relationship Diagram (ERD).
   - Domain entity specifications: `ApplicationUser`, `Tutorial`, `Category`, `CodingChallenge`, `CodeTestCase`, `InterviewFlashcard`.
   - Comprehensive RESTful API contracts with sample request and response payloads:
     - Mobile authentication and profile retrieval (`/api/mobile/auth/*`).
     - Curriculum retrieval and completion synchronization (`/api/mobile/tutorials/*`).
     - Algorithmic Big-O analysis and failure diagnostics (`/AiTutor/*`).
     - AI interview evaluation, follow-up challenges, and session reporting (`/Interview/*`).

6. [**06. System Architecture, Clean Code Blueprint & UI/UX Design**](file:///d:/NET-Tutos/docs/06_architecture_and_design_blueprint.md)
   - Clean Code folder directory breakdown across WebApp, Mobile, and documentation.
   - 5-Tier system topology and real-time data flows (Playground judge & Voice AI mock interview).
   - Complete 23-table Entity Relationship Diagram (ERD).
   - Wireframe layout architecture (Playground Monaco IDE, Markdown reader, MAUI mobile tab navigation).

7. [**07. PostgreSQL 17 & Docker Compose Infrastructure**](file:///d:/NET-Tutos/docs/07_docker_and_database_setup.md)
   - 12-container orchestration topology (`gateway`, `web`, `coderunner`, `gamification`, `aitutor`, `catalog`, `postgres`, `redis`, `rabbitmq`, `kafka`, `prometheus`, `grafana`).
   - `Npgsql.EntityFrameworkCore.PostgreSQL` 10 configuration, connection strings, and resilient SQLite fallback.
   - Relational schema catalog, seed data initialization, and Live OAuth 2.0 setup.
   - Operations guide (`docker compose up -d --build`) and VS Code Database Client / SQLTools configuration.

8. [**08. Automated Testing Strategy, Test Case Matrix & Exception Handling**](file:///d:/NET-Tutos/docs/08_testing_strategy_and_test_cases.md)
   - Quality strategy and automated testing pyramid (Unit, Integration, Microservice, VS Code Extension Node Tests, CI/CD).
   - Automated test matrix across Streak, Gamification, Exam, Roslyn Sandbox, AI Tutor, Observability/Prometheus, Multi-Source RAG, Live Coding Whiteboard, IDE Bridge, and HTTP endpoints.
   - Comprehensive Exceptions and Error Handling matrix with system mitigation and user recovery strategies.
   - GitHub Actions CI workflow pipeline configuration (`.github/workflows/ci.yml`) and code coverage quality gates.
   - Local developer CLI execution guide (204 automated tests: 199 `.NET` + 5 `node:test`).

9. [**09. Microservices Architecture & Strangler Fig Migration**](file:///d:/NET-Tutos/docs/09_microservices_architecture_and_migration.md)
   - Distributed system transformation, bounded contexts, and architectural drivers.
   - Reusable `BuildingBlocks` (`Common`, `PrometheusMetricsBuilder`, and `EventBus` RabbitMQ + Kafka events).
   - Dedicated `CodeRunner.API`, `Gamification.API`, `AiTutor.API`, and `Catalog.API` microservices with `/metrics` and `/healthz`.
   - Resilient delegation and graceful in-process fallback pattern (`ResilientMicroserviceHandler`).
   - 12-container Docker Compose orchestration including Apache Kafka KRaft, Prometheus (`:9090`), and Grafana (`:3000`).
   - 204/204 automated test execution across 7 `.NET` test projects plus the VS Code Extension test suite.

10. [**10. Master Project Roadmap & AI Context Memory**](file:///d:/NET-Tutos/docs/10_project_roadmap_and_ai_memory.md)
   - Persistent AI Context Memory and Single Source of Truth (SSOT) for all 19 completed phases (Phase 1 through Phase 19): Hybrid L1/L2 Redis 7 Caching, Catalog Microservice, OpenTelemetry Tracing & HTTP Resilience, Real-Time 1v1 SignalR Coding Arena, Apache Kafka Streams & Neural TTS, Live Coding Whiteboard, Multi-Source Hybrid RAG, Prometheus + Grafana Observability Stack, and the VS Code Atus Pair-Programmer Extension.
   - Standing engineering rules (Pure .NET 10 / C# 14, Auto-Commit workflow, English-Only Code Comments, Zero Secret/Demo Leakage, Geo-IP Region Detection `VN` -> `vi` / Outside `VN` -> `en`, Continuous SEO, and Dedicated Mobile Roadmap separation).

11. [**DESIGN. Design System & Anti-AI-Slop Constitution**](file:///d:/NET-Tutos/docs/DESIGN.md)
   - Developer Tool standard inspired by Linear, Raycast, Stripe, and GitHub Primer.
   - Anti-Slop banned patterns (no neon gradients, no cartoonish hover transforms, no card nesting).
   - Grounded Zinc/Slate neutral palettes, disciplined 6px–8px radii, and 1px hairline borders.
   - Code-First terminal anatomy, status chips, and the 6-point review gate checklist.

---

## 3. Requirements Traceability Matrix (RTM)

| Business Goal | Functional Module | Implementation Components | Verification Strategy |
| :--- | :--- | :--- | :--- |
| **Zero-Friction In-Browser Coding** | `MOD-PLAY` | `PlaygroundController.cs`, `CodeExecutionService.cs`, `CodeRunner.API`, `Index.cshtml` | Microservice sandbox tests, Roslyn Scripting isolation, Monaco Editor |
| **Contextual AI Engineering Feedback** | `MOD-AI` | `AiTutorController.cs`, `AiTutorService.cs`, `AiTutorViewModels.cs` | Big-O analysis, failure diagnostics, dynamic xUnit test generation |
| **Senior Technical Hiring Readiness** | `MOD-INT` | `InterviewController.cs`, `AiTutorService.cs`, `Mock.cshtml` | 3-dimension rubric evaluation, Speech-to-Text, radar reports |
| **Continuous Cross-Platform Study** | `MOD-MOB` | `MobileAuthController.cs`, `ApiService.cs` (.NET MAUI), `LoginPage.xaml` | JWT Bearer verification, device storage, progress sync |
| **Learner Engagement & Retention** | `MOD-GAM` | `StreakService.cs`, `LeaderboardService.cs`, `NET-Tutos.Gamification.API` | Daily check-in tests, Level formula calculation, Gamification microservice tests |
| **Microservice High-Compute Isolation** | `MOD-ARCH` | `NET-Tutos.CodeRunner.API`, `NET-Tutos.Gamification.API`, `NET-Tutos.AiTutor.API`, `NET-Tutos.Catalog.API`, `NET-Tutos.YarpGateway` | 199 `.NET` + 5 VS Code tests (204/204), 12-container Docker topology, CI pipeline |
| **Geo-IP Localization & Technical SEO** | `MOD-SEO` | `GeoIpService.cs`, `SeoMetadataService.cs`, `SeoController.cs` | VN -> `vi` / INTL -> `en`, Dynamic OG SVG, `BreadcrumbList`, `FAQPage`, IndexNow |
| **Hybrid L1/L2 Redis 7 Caching & TTS** | `MOD-CACHE` | `HybridRedisCacheService.cs`, `TtsService.cs` (`/api/tts/speak`), `CodeExecutionService.cs`, `CurriculumService.cs` | L1 Memory + L2 Redis 7, Stampede Semaphore Lock, 30s Circuit Breaker, Neural TTS cache hit metrics |
| **Live OAuth 2.0 SSO Authentication** | `MOD-AUTH` | `Program.cs`, `AccountController.cs`, `Login.cshtml`, `Register.cshtml`, `Profile.cshtml` | Live GitHub (`/signin-github`) & Google (`/signin-google`) OAuth 2.0 SSO |
| **Prometheus, Grafana & Resilience** | `MOD-OBS` | `PrometheusMetricsBuilder`, `PrometheusTelemetryExporter.cs`, `ObservabilityService.cs`, `observability/` | `/metrics` across 6 `.NET` services, Prometheus `:9090`, Grafana `:3000` (`nettutos-overview`) |
| **Apache Kafka Streams & Adaptive AI** | `MOD-KAFKA` | `KafkaProducer`, `KafkaTelemetryConsumerWorker`, `KafkaStreamAnalyticsStore`, `AdaptiveLearningService`, `Admin/Telemetry.cshtml` | Real-time AI/CodeRunner/Learning event streams, partition lag metrics, `/api/adaptive/challenge` |
| **Real-Time 1v1 Coding Arena** | `MOD-ARENA` | `CodingArenaHub.cs`, `CodingArenaService.cs`, `ArenaController.cs`, `Arena/Index.cshtml` | SignalR real-time broadcast, Roslyn submission validation, ELO ladder updates |
| **Multi-Source Hybrid RAG & Whiteboard** | `MOD-RAG` | `SemanticCurriculumSearchService.cs`, `/api/search/knowledge`, `/Interview/Mock` (`EvaluateLiveCoding`) | 175+ indexed docs (Curriculum, Tutorials, Challenges, Flashcards) & Live Coding Whiteboard |
| **VS Code Extension & IDE Bridge** | `MOD-IDE` | `IdeAssistantController.cs` (`/api/v1/ide/*`), `extensions/vscode-atus/` | Sidebar Atus Chat, inline C# diagnostics, Big-O Review, xUnit gen, Roslyn runner (`5/5 node:test`) |

---

## 4. Role-Based Navigation Guide

- **Product Owners & Business Stakeholders:** Review [01. Business Overview](file:///d:/NET-Tutos/docs/01_business_overview.md) and [03. Functional Requirements (BRD)](file:///d:/NET-Tutos/docs/03_functional_requirements_brd.md) to understand the product roadmap, functional scope, and strategic business goals.
- **Solution Architects & Engineering Leads:** Review [02. System Architecture Overview](file:///d:/NET-Tutos/docs/02_system_architecture_overview.md), [05. Data Models & API Specifications](file:///d:/NET-Tutos/docs/05_data_models_and_api_specs.md), and [09. Microservices Architecture & Migration](file:///d:/NET-Tutos/docs/09_microservices_architecture_and_migration.md) to evaluate system topology, data flows, microservices boundaries, and event contracts.
- **Software Developers (Web & Mobile):** Refer to [03. Functional Requirements (BRD)](file:///d:/NET-Tutos/docs/03_functional_requirements_brd.md) for acceptance criteria, [05. Data Models & API Specifications](file:///d:/NET-Tutos/docs/05_data_models_and_api_specs.md) for endpoint request/response contracts, and [09. Microservices Architecture](file:///d:/NET-Tutos/docs/09_microservices_architecture_and_migration.md) for inter-service communication.
- **QA & Test Automation Engineers:** Leverage the automated test cases matrix and exception catalog in [08. Automated Testing Strategy & Test Cases](file:///d:/NET-Tutos/docs/08_testing_strategy_and_test_cases.md), acceptance criteria in [03. Functional Requirements (BRD)](file:///d:/NET-Tutos/docs/03_functional_requirements_brd.md), and performance benchmarks in [04. Non-Functional Requirements (NFR)](file:///d:/NET-Tutos/docs/04_non_functional_requirements.md) to maintain the CI/CD test suites and regression pipelines.
