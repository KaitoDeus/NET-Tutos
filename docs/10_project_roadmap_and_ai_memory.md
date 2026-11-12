# 10. Project Roadmap, Completed Phases & AI Context Memory (SSOT)

Document Type: Master Roadmap, Phase History & AI Context Memory  
System: **NET-Tutos** — Enterprise Distributed .NET 10 & C# 14 Learning Ecosystem  
License: [MIT License](../LICENSE) (`Copyright (c) 2026 Khải Võ (NET-Tutos)`)  
Current Topology: **26 Projects in `NET-Tutos.slnx` + VS Code Extension (`extensions/vscode-atus`) | 13 Docker Containers (`pgvector/pgvector:pg17` & Grafana Tempo) + `.NET Aspire` AppHost & Kubernetes Helm | Automated Test Suite (241 Tests: 236 .NET + 5 VS Code Node Tests)**  

---

## 1. Purpose of This Document (AI Context Memory)

This document serves as the persistent **AI Context Memory & Roadmap Single Source of Truth (SSOT)** for the **NET-Tutos** repository. Any AI coding agent or engineer inspecting the repository must read this file first to understand:
1. **Standing Engineering Rules & Constraints**
2. **Completed Roadmap History (Phase 1 – Phase 26)** and their exact files/endpoints
3. **13-Container Distributed Topology, `.NET Aspire` Orchestration, Kubernetes Helm & 241/241 Test Suite Matrix**
4. **Separate Dedicated Mobile Roadmap (`.NET MAUI 10`) & Future Enterprise Scaling Items**

---

## 2. Standing Engineering Rules & Constraints

1. **Pure .NET 10 / C# 14 Ecosystem**:
   - The platform is built 100% on **.NET 10** (`ASP.NET Core 10 MVC / Razor Views / SignalR BFF` in `src/Presentation/Web`, `.NET MAUI 10` in `src/Presentation/Mobile`, `Microsoft YARP` in `src/Gateways/Gateway`, and 4-Layer Clean Architecture Microservices in `src/Services/*`).
   - **Do NOT introduce Next.js or external JS SPA frameworks.**
2. **Auto-Commit Rule**:
   - Automatically run `git add` and `git commit` (using Conventional Commits) immediately after completing and verifying any code or documentation task.
3. **Continuous Roadmap Synchronization**:
   - Whenever a phase or feature is completed, always synchronize this roadmap (`docs/10_project_roadmap_and_ai_memory.md`), `docs/README.md`, and `README.md` so AI memory retains 100% accurate project context.
4. **Continuous SEO & Geo-IP Standard**:
   - Visitors from **Vietnam (`VN`)** default to **Vietnamese (`vi`, `vi-VN`)**.
   - Visitors from **outside Vietnam (`INTL`, `US`, `SG`, `JP`, etc.)** default to **English (`en`, `en-US`)**.
   - All public routes must maintain automatic canonical URLs, bilingual `hreflang` (`vi`, `en`, `x-default`), dynamic OpenGraph SVG images, and Schema.org JSON-LD (`EducationalOrganization`, `WebSite`, `BreadcrumbList`, `TechArticle`, `FAQPage`, `Course`).
5. **English-Only Code Comments Standard**:
   - All source code comments (`//`, `/// <summary>`, `/* */`, `#`, `<!-- -->`) across the entire project must be written strictly in **English** (while preserving Vietnamese tutorial markdown strings and bilingual UI translations).
6. **Zero Secret / Demo Leakage Standard**:
   - Never commit `.env` or secret-shaped placeholder patterns (`GOCSPX-...`, `*.apps.googleusercontent.com`, `Ov23li...`) in documentation (use `<your-...>` placeholders so GitHub Secret Scanning / Push Protection is never triggered).
   - Do not display demo accounts or demo login buttons on the Login/Register UI or seed demo student accounts in the database.
   - Ensure neither server logs nor browser DevTools `console.log`/`console.warn`/`console.error` leak OAuth tokens, API keys, or raw exception payloads.

---

## 3. Completed Roadmap History (Phase 1 – Phase 19)

| Phase | Milestone Name | Status | Key Deliverables & Architecture Components |
| :--- | :--- | :---: | :--- |
| **Phase 1** | **Core LMS, 101+ Bilingual Tutorials & PostgreSQL 17** | ✅ Completed | `src/Presentation/Web`, 101+ VI/EN C# & .NET tutorials, 18-module Curriculum, Capstone Projects, Exams, Certificates, SignalR Hubs, PostgreSQL 17 with automatic SQLite fallback, persistent DataProtection key ring (`/app/.keys`). |
| **Phase 2** | **Shared BuildingBlocks & Event-Driven Bus** | ✅ Completed | `src/Infrastructure/Common` (`NET-Tutos.BuildingBlocks.Common`) and `src/Infrastructure/EventBus` (`NET-Tutos.BuildingBlocks.EventBus`) with MassTransit + RabbitMQ integration events (`ChallengeSolvedIntegrationEvent`, `LessonCompletedIntegrationEvent`, `ExamPassedIntegrationEvent`). |
| **Phase 3** | **CodeRunner Microservice (4-Layer Clean Architecture)** | ✅ Completed | `src/Services/CodeRunner/NET-Tutos.CodeRunner.{Domain,Application,Infrastructure,API}` (Port `5002`). Isolated Roslyn C# 14 script compiler, security guard, and multi-test-case judge with Strangler Fig fallback in `CodeExecutionService.cs`. |
| **Phase 4** | **Gamification Microservice & YARP API Gateway** | ✅ Completed | `src/Services/Gamification/NET-Tutos.Gamification.{Domain,Application,Infrastructure,API}` (Port `5004`) with MassTransit consumers & dedicated PostgreSQL DB (`nettutos_gamification_db`). `src/Gateways/Gateway` (`NET-Tutos.YarpGateway`, Port `5000`) with `CorrelationIdMiddleware` & Rate Limiting. |
| **Phase 5** | **AiTutor ("Atus") Microservice Extraction** | ✅ Completed | `src/Services/AiTutor/NET-Tutos.AiTutor.{Domain,Application,Infrastructure,API}` (Port `5006`). Decouples Big-O complexity analysis, compiler error diagnostics, xUnit test synthesis, code reviews, and 3D rubric technical mock interviews (`/api/v1/aitutor/*`). |
| **Phase 5.5** | **Cross-Platform .NET MAUI 10 Mobile App** | ✅ Completed | `src/Presentation/Mobile/NET_Tutos.Mobile.csproj` targeting Android, iOS, macOS Catalyst, and Windows with JWT auth, curriculum sync, 3D interview flashcards, and bilingual UI. |
| **Phase 6** | **Hybrid L1/L2 Redis 7 Distributed Caching** | ✅ Completed | `nettutos-redis` (`redis:7-alpine`, Port `6379`, AOF persistence + `allkeys-lru`), `HybridRedisCacheService.cs` (L1 `IMemoryCache` + L2 `StackExchange.Redis`, per-key `SemaphoreSlim` Cache Stampede protection, 30s Circuit-Breaker fallback), deterministic SHA-256 Roslyn C# execution cache, catalog cache, sitemap cache, and `/api/cache/diagnostics`. |
| **Phase 7** | **Catalog & Assessment Microservice (4-Layer DDD)** | ✅ Completed | `src/Services/Catalog/NET-Tutos.Catalog.{Domain,Application,Infrastructure,API}` (Port `5008`, `nettutos-catalog`), YARP route `/api/v1/catalog/{**catch-all}`, domain value objects (`ReadingDuration`, `ExamScore`), verifiable certificates (`NT-CERT-2026-*`), and 12 unit tests in `tests/NET-Tutos.Catalog.Tests`. |
| **Phase 8** | **Geo-IP Region Detection & Advanced SEO Engine** | ✅ Completed | `GeoIpService.cs` & `GeoIpRequestCultureProvider` (`VN` -> `vi`, Outside `VN` -> `en`), `SeoMetadataService.cs`, `SeoController.cs` (`/robots.txt`, `/sitemap.xml`, `/seo/og-image/{slug}.svg`, `/seo/og-project/{slug}.svg`, `/nettutos-indexnow-2026.txt`, `/api/seo/diagnostics`), `BreadcrumbList` & `FAQPage` Schema.org JSON-LD. |
| **Phase 9** | **Interactive In-Lesson Code Lab & AI Big-O Sandbox** | ✅ Completed | Inline executable C# 14 Sandbox (`Run Code`, `Edit Inline`) & live AI Big-O Complexity Analyzer (`Big-O AI`) embedded directly into every code block inside `Views/Tutorials/Details.cshtml`. |
| **Phase 10** | **Full GitHub GitOps & DevSecOps Automation Suite** | ✅ Completed | `.github/workflows/auto-pr.yml` (Auto-PR & Labeler), `auto-merge.yml` (Auto Squash-Merge & branch cleanup on CI pass), `auto-release.yml` (Semantic Release & Changelog), `security-audit.yml` (NuGet CVE scanner), `docker-ghcr.yml` (GHCR multi-container publisher), `.github/dependabot.yml`, `.github/CODEOWNERS`, `.github/ISSUE_TEMPLATE/`, `SECURITY.md`, and `scripts/New-AutoPullRequest.ps1`. |
| **Phase 11** | **Live GitHub & Google OAuth 2.0 SSO & Secret Hardening** | ✅ Completed | `Program.cs` (`AddOAuth("GitHub")` & `AddOAuth("Google")` with `/signin-github` & `/signin-google` callbacks), `AccountController.cs` (`ExternalLogin`, `ExternalLoginCallback`), `Login.cshtml`, `Register.cshtml`, and `Profile.cshtml` external login account linking (+50 XP bonus on first SSO sign-in). Removed all demo accounts/UI and hardened `.dockerignore` & console logging. |
| **Phase 12** | **Catalog Microservice Containerization & Live BFF Delegation** | ✅ Completed | Expanded `NET-Tutos.Catalog.API` (`:5008`) with `/api/v1/catalog/sections`, `/api/v1/catalog/sections/{sectionNumber}`, and `/api/v1/catalog/tracks/estimate`. Wired `Services__Catalog__Url=http://catalog:8080` in `docker-compose.yml` & `appsettings.json`, and enabled live HTTP delegation with Hybrid L1/L2 Redis 7 caching & Strangler Fig fallback in `CurriculumService.cs`, `ExamService.cs`, and `CertificateService.cs`. |
| **Phase 13** | **Cloud-Native Observability & HTTP Resilience** | ✅ Completed | `src/Infrastructure/Common/Telemetry/ServiceTelemetry.cs` (shared `ActivitySource` + `Meter`), `ObservabilityService.cs` (W3C tracing, HTTP counters/histograms, downstream microservice health), `ResilientMicroserviceHandler` (exponential backoff retry on 5xx/transient errors, circuit breaker: 3 consecutive failures -> 20s open, `X-Correlation-ID` propagation) attached to all 4 microservice `HttpClient`s. Endpoints: `/api/telemetry/diagnostics` (Web BFF) and `/api/v1/gateway/telemetry` (YARP). Implemented with built-in `System.Diagnostics` APIs (OTLP exporter wiring deferred to backlog). |
| **Phase 14** | **Real-Time 1v1 C# Coding Arena (SignalR)** | ✅ Completed | `CodingArenaService.cs` (3 seeded battle rooms, Roslyn-validated submissions, ELO +25 winner / +10 runner-up / -5 wrong, XP rewards), `CodingArenaHub` (`/hubs/arena`), `ArenaController.cs` (`/api/arena/rooms`, `/api/arena/join`, `/api/arena/submit`), bilingual routes `/coding-arena` & `/dau-truong-code`, and `Views/Arena/Index.cshtml`. Room/ELO state is in-memory (resets on restart). |
| **Phase 15** | **RAG Semantic Curriculum Search & Kubernetes Helm Chart** | ✅ Completed | `SemanticCurriculumSearchService.cs`: in-process vector index over all 101 lessons using hashed TF-IDF sparse vectors (4096 dims, FNV-1a feature hashing, word + bi-gram + char tri-gram features, Vietnamese diacritic folding, cosine ranking) with `BuildRagContextAsync` for cited lesson context. Endpoint `/api/search/semantic?q=`. `deploy/helm/nettutos` chart: Deployments + Services for 6 .NET services, HPA (gateway, web, coderunner), PostgreSQL StatefulSet, Redis, RabbitMQ, NGINX Ingress, secrets via `existingSecret` only. Verified with `helm lint` + `helm template`. |
| **Phase 16** | **Apache Kafka Telemetry, Neural TTS & Adaptive Learning** | ✅ Completed | `apache/kafka` KRaft broker (`:9092/:29092`), `KafkaProducer`, `KafkaTelemetryConsumerWorker`, `KafkaStreamAnalyticsStore` (lag & topic analytics), `/Admin/Telemetry`, `TtsService` (`/api/tts/speak` cached on L1/L2 Redis for Vietnamese/English Neural TTS), `AdaptiveLearningService` & `POST /api/adaptive/challenge`. |
| **Phase 17** | **Live Coding Whiteboard & Multi-Source Hybrid RAG** | ✅ Completed | Pair-Programming Live Coding Whiteboard inside `/Interview/Mock` (`POST /Interview/EvaluateLiveCoding` combining Roslyn execution, concurrent Atus Code Review + Big-O analysis, refactor apply, Voice TTS, and Kafka events). Upgraded `SemanticCurriculumSearchService` to index 4 sources (`Curriculum`, `Tutorial`, `CodingChallenge`, `InterviewFlashcard`) at `/api/search/knowledge`. |
| **Phase 18** | **Prometheus + Grafana Observability Stack** | ✅ Completed | Zero-dependency `PrometheusMetricsBuilder` (`BuildingBlocks.Common`) & `PrometheusTelemetryExporter`, `/metrics` across all 6 .NET services (`Web`, `Gateway`, `CodeRunner`, `AiTutor`, `Catalog`, `Gamification`) exporting CLR/HTTP/Downstream/Redis L1-L2/TTS cache hit-rate/Atus AI latency & Kafka partition consumer lag, plus Docker `prom/prometheus` (`:9090`) & auto-provisioned `grafana/grafana` (`:3000`) dashboard `nettutos-overview`. Aligned `MassTransit.RabbitMQ` in Gamification to `8.3.7`. |
| **Phase 19** | **VS Code Extension, IDE Bridge & Responsive UI** | ✅ Completed | `IdeAssistantController.cs` exposing stateless `/api/v1/ide/{status,search,chat,review,explain-error,generate-tests,complexity,execute}` proxied by YARP Gateway & Web BFF, plus `extensions/vscode-atus/` TypeScript VS Code extension (Sidebar Webview Chat, inline C# Diagnostics, Lightbulb `CodeActionProvider`, Big-O Review + 1-click Refactor, xUnit Test Generator, Roslyn Sandbox Runner, Multi-Source RAG QuickPick). Plus Stage-by-Stage Lock/Unlock Progression (`LearningProgressService.GetStageProgressionAsync`, `/Tutorials`, `/Roadmap`, `/Account/ToggleLesson`) and comprehensive responsive Web overhaul (single-row mobile navbar, drawer containment in `AiTutor/Index.cshtml`, `overflow-x: clip`, badge wrapping guardrails in `site.css`, `_Layout.cshtml`, `Playground`, `Interview/Mock`, `Home`, `Quiz`, `Curriculum`, `Planner`). |
| **Phase 20** | **Persistent Arena & Live SignalR UI** | ✅ Completed | Persistent PostgreSQL/SQLite tables (`ArenaRoom`, `ArenaPlayerRating`, `ArenaMatchRecord` in `ArenaEntities.cs` & `DbInitializer.EnsureCodingArenaTablesExistAsync`), ELO leaderboard & match history APIs (`/api/arena/leaderboard`, `/api/arena/matches`, `/api/arena/reset`), `CodingArenaHub` live room/reset/progress signals, and real-time SignalR UI (`Views/Arena/Index.cshtml`) with countdown timer, opponent progress bars, rematch button & ELO tables. |
| **Phase 21** | **Hybrid Neural Embeddings (`pgvector`)** | ✅ Completed | Upgraded PostgreSQL container image to `pgvector/pgvector:pg17` (Docker Compose & Helm chart), added `KnowledgeEmbedding` entity + HNSW `vector(768)` column + `CREATE EXTENSION IF NOT EXISTS vector`, implemented `GeminiNeuralEmbeddingService` (`text-embedding-004` + deterministic 768-dim multilingual EN/VI dense projection fallback), fused neural + sparse TF-IDF hybrid search in `SemanticCurriculumSearchService` with `<=>` cosine distance queries, and added `/api/search/embeddings/status` & `POST /api/search/embeddings/sync`. |
| **Phase 22** | **`.NET Aspire` AppHost & CI Helm Deployment** | ✅ Completed | Added `src/Orchestration/NET-Tutos.AppHost` (`Aspire.AppHost.Sdk` 13.6.5) orchestrating PostgreSQL `pgvector`, Redis 7, RabbitMQ 3, Kafka, YARP Gateway, Web BFF, and all 4 DDD microservices with service discovery connection fallbacks. Added `.github/workflows/helm-deploy.yml` (`helm lint --strict`, `helm template` verification, atomic staging `helm upgrade --install`), updated `.github/workflows/ci.yml` to build `AppHost` and run `Catalog.Tests`. |
| **Phase 23** | **Redis Multi-Replica Scale-Out & Full Helm Parity** | ✅ Completed | `RedisScaleOutExtensions.cs` sharing ASP.NET Core DataProtection keys (`nettutos:dataprotection-keys`) and SignalR hubs (`nettutos:signalr:`) over Redis 7 with `/api/scaleout/diagnostics`, YARP sticky cookie affinity (`.YARP.WebAffinity`), and full 12/13-service Helm parity (`nettutos-postgres-init` SQL ConfigMap, Kafka, Prometheus, Grafana, `Kafka__Enabled`, Gateway `serviceType`). |
| **Phase 24** | **Distributed OpenTelemetry OTLP & Grafana Tempo** | ✅ Completed | `OpenTelemetryCollectorExtensions.cs` (`OpenTelemetry.* 1.21.0`) wired across YARP Gateway, Web BFF, and all 4 microservices exporting W3C traces & metrics over gRPC/http-protobuf to Grafana Tempo (`observability/tempo/tempo.yml`), auto-provisioned `nettutos-tempo` Grafana datasource, `correlation.id` span tags, and `/api/telemetry/otlp` + `/api/v1/gateway/otlp` diagnostics. |
| **Phase 25** | **Distributed Gateway Quotas & Sandbox Hardening** | ✅ Completed | `DistributedQuotaLimiterExtensions.cs` enforcing Redis-backed / partitioned sliding-window client quotas (`runner-policy`, `aitutor-policy`, `Retry-After` + `X-RateLimit-*` headers, `/api/v1/gateway/ratelimits`) plus shared `SandboxGuardrails` & `BoundedStringWriter` (`32 KB` / `500 lines` / `16 KB` stdout cap, clamped timeouts, expanded blacklist) across `CodeRunner` and `Web`. |
| **Phase 26** | **Interactive SQL Playground & Cert-Manager TLS Helm** | ✅ Completed | `SqlPlaygroundService.cs` & `Views/Playground/Sql.cshtml` (`/sql-playground`, `/thuc-hanh-sql`, `/api/sql/{schema,challenges,execute}`) providing read-only ephemeral in-memory SQLite relational queries and 5 bilingual SQL challenges, plus production Helm `cert-manager.yaml` (`ClusterIssuer`), HSTS, and NGINX SSL redirect annotations. |

---

## 4. 13-Container Docker Compose Topology (`& .NET Aspire AppHost / Helm`)

| Container Name | Service Role | Host Port | Internal URL |
| :--- | :--- | :---: | :--- |
| **`nettutos-gateway`** | Microsoft YARP API Gateway (Single External Ingress) | `http://localhost:5000` | `http://gateway:8080` |
| **`nettutos-web`** | ASP.NET Core 10 MVC Web BFF, OAuth 2.0 SSO & SignalR | `http://localhost:5001` | `http://web:5000` |
| **`nettutos-coderunner`** | Isolated Roslyn C# 14 Compilation & Online Judge API | `http://localhost:5002` | `http://coderunner:8080` |
| **`nettutos-gamification`** | Event-Driven Streaks, XP, Badges & Leaderboard API | `http://localhost:5004` | `http://gamification:8080` |
| **`nettutos-aitutor`** | AI Big-O Complexity, Diagnostics & Mock Interview API | `http://localhost:5006` | `http://aitutor:8080` |
| **`nettutos-catalog`** | Catalog, Curriculum, Exam Grading & Certificate API | `http://localhost:5008` | `http://catalog:8080` |
| **`nettutos-prometheus`** | Prometheus OpenMetrics Scraper (`5s` interval across 7 targets) | `http://localhost:9090` | `http://prometheus:9090` |
| **`nettutos-tempo`** | Grafana Tempo Distributed OTLP Trace Collector (`gRPC :4317`, HTTP `:3200`) | `http://localhost:3200` | `http://tempo:4317` |
| **`nettutos-grafana`** | Pre-provisioned Observability Dashboards (`nettutos-overview`, Prometheus + Tempo) | `http://localhost:3000` | `http://grafana:3000` |
| **`nettutos-redis`** | Redis 7 Alpine L2 Cache, Scale-Out Keys & Distributed Quotas | `localhost:6379` | `redis:6379` |
| **`nettutos-rabbitmq`** | RabbitMQ 3 Message Broker & Management UI | `http://localhost:15672` | `amqp://rabbitmq:5672` |
| **`nettutos-kafka`** | Apache Kafka KRaft Broker (AI, CodeRunner & Learning Streams) | `localhost:29092` | `kafka:9092` |
| **`nettutos-postgres`** | PostgreSQL 17 `pgvector` (`nettutos_db` & `nettutos_gamification_db`) | `localhost:5432` | `postgres:5432` |

---

## 5. Automated Test Suite Breakdown (241/241 Passing)

| Test Project | Path | Tests Count | Scope Covered |
| :--- | :--- | :---: | :--- |
| **`NET-Tutos.Gateway.Tests`** | `tests/NET-Tutos.Gateway.Tests` | 11 | YARP reverse proxy config, partitioned client rate limiting (`runner-policy`, `aitutor-policy`, `429 Retry-After`), `/api/v1/gateway/{status,telemetry,otlp,ratelimits}`, `/metrics`, `X-Correlation-ID`, `.NET Aspire` AppHost, Tempo & Helm Cert-Manager TLS specs. |
| **`NET-Tutos.CodeRunner.Tests`** | `tests/NET-Tutos.CodeRunner.Tests` | 13 | Roslyn C# compilation, expanded security guard blocking, payload size caps, bounded stdout truncation, timeout clamping & test case judge. |
| **`NET-Tutos.Gamification.Tests`** | `tests/NET-Tutos.Gamification.Tests` | 16 | Domain value objects (`XpValue`, `StreakCount`), streak check-in rules, MassTransit event consumers. |
| **`NET-Tutos.AiTutor.Tests`** | `tests/NET-Tutos.AiTutor.Tests` | 16 | `EfficiencyScore`, `InterviewRubricScore` (50/25/25), Big-O detection, compiler diagnostics, xUnit generator, mock interview. |
| **`NET-Tutos.Catalog.Tests`** | `tests/NET-Tutos.Catalog.Tests` | 17 | `ReadingDuration`, `ExamScore`, 7-stage bilingual `GetCatalogSectionsQueryHandler`, `EstimateLearningTrackQueryHandler`, and dual-format certificate verification (`NT-CERT-*` & `NET-2026-*`). |
| **`NET-Tutos.UnitTests`** | `tests/NET-Tutos.UnitTests` | 123 | Web services, `RedisScaleOutExtensions`, `OpenTelemetryCollectorExtensions`, `SandboxGuardrails` & `BoundedStringWriter`, `SqlPlaygroundService`, `HybridRedisCacheService`, `ObservabilityService`, `PrometheusTelemetryExporter`, `ResilientMicroserviceHandler`, `CodingArenaService`, `GeminiNeuralEmbeddingService`, `SemanticCurriculumSearchService`, `TtsService`, `KafkaStreamAnalyticsStore`, `LearningProgressService`, `GeoIpService`, `SeoMetadataService`. |
| **`NET-Tutos.IntegrationTests`** | `tests/NET-Tutos.IntegrationTests` | 40 | Full HTTP pipeline, `/api/scaleout/diagnostics`, `/api/telemetry/otlp`, `/sql-playground`, `/thuc-hanh-sql`, `/api/sql/*`, `/metrics`, `/api/v1/ide/*`, `/api/search/knowledge`, `/api/search/embeddings/*`, Persistent Arena + SignalR UI, Stage Lock/Unlock, Responsive Design, `/Interview/Mock` Live Coding Whiteboard, `/api/tts/speak`, `/Admin/Telemetry` RBAC, `/api/adaptive/challenge`, OAuth 2.0 SSO, Geo-IP, SEO & Roslyn endpoints. |
| **`atus-dotnet-pair-programmer` (`node:test`)** | `extensions/vscode-atus/src/test` | 5 | `AtusClient` HTTP headers/auth/error handling and `reviewMapper` C# diagnostic line inference & markdown report generation. |

---

## 6. Upcoming Strategic Roadmap (Separate Mobile & Enterprise Scaling)

### Separate Dedicated Roadmap: `.NET MAUI 10 Mobile` (`src/Presentation/Mobile`)
- Expand `.NET MAUI 10` Mobile Client into an independent mobile roadmap featuring mobile Atus streaming chat, offline flashcard spaced repetition, and native Vietnamese/English Speech-to-Text + Neural TTS voice training.

### Future Enterprise Scaling Items
- **Multi-Region Read Replicas & Edge CDN Caching**: Scale PostgreSQL `pgvector` HNSW read replicas and push curriculum static bundles behind Cloudflare edge workers.
