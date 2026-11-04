# 10. Project Roadmap, Completed Phases & AI Context Memory (SSOT)

Document Type: Master Roadmap, Phase History & AI Context Memory  
System: **NET-Tutos** — Enterprise Distributed .NET 10 & C# 14 Learning Ecosystem  
License: [MIT License](../LICENSE) (`Copyright (c) 2026 Khải Võ (NET-Tutos)`)  
Current Topology: **25 Projects in `NET-Tutos.slnx` | 9 Docker Containers | 152/152 Automated Tests Passing (100%)**  

---

## 1. Purpose of This Document (AI Context Memory)

This document serves as the persistent **AI Context Memory & Roadmap Single Source of Truth (SSOT)** for the **NET-Tutos** repository. Any AI coding agent or engineer inspecting the repository must read this file first to understand:
1. **Standing Engineering Rules & Constraints**
2. **Completed Roadmap History (Phase 1 – Phase 11)** and their exact files/endpoints
3. **9-Container Distributed Topology & 152/152 Test Suite Matrix**
4. **Upcoming Strategic Phases (Phase 12 – Phase 15)**

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

## 3. Completed Roadmap History (Phase 1 – Phase 12)

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

---

## 4. 9-Container Docker Compose Topology

| Container Name | Service Role | Host Port | Internal URL |
| :--- | :--- | :---: | :--- |
| **`nettutos-gateway`** | Microsoft YARP API Gateway (Single External Ingress) | `http://localhost:5000` | `http://gateway:8080` |
| **`nettutos-web`** | ASP.NET Core 10 MVC Web BFF, OAuth 2.0 SSO & SignalR | `http://localhost:5001` | `http://web:5000` |
| **`nettutos-coderunner`** | Isolated Roslyn C# 14 Compilation & Online Judge API | `http://localhost:5002` | `http://coderunner:8080` |
| **`nettutos-gamification`** | Event-Driven Streaks, XP, Badges & Leaderboard API | `http://localhost:5004` | `http://gamification:8080` |
| **`nettutos-aitutor`** | AI Big-O Complexity, Diagnostics & Mock Interview API | `http://localhost:5006` | `http://aitutor:8080` |
| **`nettutos-catalog`** | Catalog, Curriculum, Exam Grading & Certificate API | `http://localhost:5008` | `http://catalog:8080` |
| **`nettutos-redis`** | Redis 7 Alpine L2 Distributed Cache (`allkeys-lru` + AOF) | `localhost:6379` | `redis:6379` |
| **`nettutos-rabbitmq`** | RabbitMQ 3 Message Broker & Management UI | `http://localhost:15672` | `amqp://rabbitmq:5672` |
| **`nettutos-postgres`** | PostgreSQL 17 Alpine (`nettutos_db` & `nettutos_gamification_db`) | `localhost:5432` | `postgres:5432` |

---

## 5. Automated Test Suite Breakdown (158/158 Passing)

| Test Project | Path | Tests Count | Scope Covered |
| :--- | :--- | :---: | :--- |
| **`NET-Tutos.Gateway.Tests`** | `tests/NET-Tutos.Gateway.Tests` | 4 | YARP reverse proxy config, `/healthz`, `/api/v1/gateway/status`, `X-Correlation-ID` propagation. |
| **`NET-Tutos.CodeRunner.Tests`** | `tests/NET-Tutos.CodeRunner.Tests` | 11 | Roslyn C# compilation, security guard blocking (`System.Diagnostics.Process`, `System.IO.File`), test case judge. |
| **`NET-Tutos.Gamification.Tests`** | `tests/NET-Tutos.Gamification.Tests` | 16 | Domain value objects (`XpValue`, `StreakCount`), streak check-in rules, MassTransit event consumers. |
| **`NET-Tutos.AiTutor.Tests`** | `tests/NET-Tutos.AiTutor.Tests` | 16 | `EfficiencyScore`, `InterviewRubricScore` (50/25/25), Big-O detection, compiler diagnostics, xUnit generator, mock interview. |
| **`NET-Tutos.Catalog.Tests`** | `tests/NET-Tutos.Catalog.Tests` | 17 | `ReadingDuration`, `ExamScore`, 7-stage bilingual `GetCatalogSectionsQueryHandler`, `EstimateLearningTrackQueryHandler`, and dual-format certificate verification (`NT-CERT-*` & `NET-2026-*`). |
| **`NET-Tutos.UnitTests`** | `tests/NET-Tutos.UnitTests` | 69 | Web services, `CurriculumService` Redis cache & BFF delegation, `HybridRedisCacheService` (L1/L2 backfill, stampede lock, deterministic Roslyn cache), `GeoIpService`, `SeoMetadataService`. |
| **`NET-Tutos.IntegrationTests`** | `tests/NET-Tutos.IntegrationTests` | 25 | Full HTTP pipeline, GitHub & Google OAuth 2.0 SSO forms, Geo-IP headers & public IP language switching, `/robots.txt`, `/sitemap.xml`, dynamic OG SVG endpoints, IndexNow, security & Roslyn endpoints. |

---

## 6. Upcoming Strategic Roadmap Phases (Phase 13 – Phase 15 Backlog)

### Phase 13: Cloud-Native Observability & HTTP Resilience (`OpenTelemetry` + `Polly v8`)
- **Standard Resilience Handler (`Microsoft.Extensions.Http.Resilience`)**: Configure Circuit Breaker, Exponential Backoff Retry, hedging, and timeout pipelines on `CodeRunner`, `Gamification`, `AiTutor`, and `Catalog` HTTP clients.
- **OpenTelemetry Instrumentation**: End-to-end distributed tracing (`ActivitySource`) and Prometheus metrics across `YarpGateway`, `Web`, `CodeRunner`, `Gamification`, `AiTutor`, and `Catalog`.

### Phase 14: Real-Time Multiplayer Coding Arena (`SignalR` 1v1 Algorithmic Battles)
- **Live Coding Duels (`CodingArenaHub`)**: Real-time 1v1 algorithmic rooms where two developers compete on the same C# 14 challenge with live test-case progress bars, countdown timers, ELO rating updates, and bonus XP rewards.

### Phase 15: RAG Vector Knowledge Base (`pgvector` + Semantic Curriculum Search) & Cloud Orchestration
- **Semantic Lesson Retrieval & Helm Manifests**: Index all 101+ bilingual lessons using vector embeddings for sub-millisecond similarity search, paired with Kubernetes Helm charts / `.NET Aspire` AppHost manifests.
