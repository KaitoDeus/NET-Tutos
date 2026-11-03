# 10. Project Roadmap, Completed Phases & AI Context Memory (SSOT)

Document Type: Master Roadmap, Phase History & AI Context Memory  
System: **NET-Tutos** — Enterprise Distributed .NET 10 & C# 14 Learning Ecosystem  
License: [MIT License](../LICENSE) (`Copyright (c) 2026 Khải Võ & NET-Tutos Engineering`)  
Current Test Suite Status: **136/136 Automated Tests Passing (100% Pass Rate across 6 Test Projects)**  

---

## 1. Purpose of This Document (AI Context Memory)

This document serves as the persistent **AI Context Memory & Roadmap Single Source of Truth (SSOT)** for the **NET-Tutos** repository. Any AI coding agent or engineer inspecting the repository must read this file to understand:
1. **Standing Engineering Rules & Constraints**
2. **Completed Phases (Phase 1 – Phase 8)** and their exact files/endpoints
3. **Upcoming Strategic Phases (Phase 6, Phase 7, Phase 9+)**

---

## 2. Standing Engineering Rules & Constraints

1. **Pure .NET 10 / C# 14 Ecosystem**:
   - The platform is built 100% on **.NET 10** (`ASP.NET Core 10 MVC / Razor Views / SignalR BFF` in `src/Presentation/Web`, `.NET MAUI 10` in `src/Presentation/Mobile`, `Microsoft YARP` in `src/Gateways/Gateway`, and 4-Layer Clean Architecture Microservices in `src/Services/*`).
   - **Do NOT introduce Next.js or external JS SPA frameworks.**
2. **Auto-Commit Rule**:
   - Automatically run `git add` and `git commit` (using Conventional Commits) immediately after completing and verifying any code or documentation task.
3. **Continuous Roadmap Synchronization**:
   - Whenever a phase or feature is completed, update this roadmap (`docs/10_project_roadmap_and_ai_memory.md`) and `docs/README.md` so project memory stays 100% accurate.
4. **Continuous SEO & Geo-IP Standard**:
   - Visitors from **Vietnam (`VN`)** default to **Vietnamese (`vi`, `vi-VN`)**.
   - Visitors from **outside Vietnam (`INTL`, `US`, `SG`, `JP`, etc.)** default to **English (`en`, `en-US`)**.
   - All public routes must maintain automatic canonical URLs, bilingual `hreflang` (`vi`, `en`, `x-default`), dynamic OpenGraph SVG images, and Schema.org JSON-LD (`EducationalOrganization`, `WebSite`, `BreadcrumbList`, `TechArticle`, `FAQPage`, `Course`).

---

## 3. Completed Roadmap History (Phase 1 – Phase 8)

| Phase | Milestone Name | Status | Key Deliverables & Architecture Components |
| :--- | :--- | :---: | :--- |
| **Phase 1** | **Core LMS, 101+ Bilingual Tutorials & PostgreSQL 17** | ✅ Completed | `src/Presentation/Web`, 101+ VI/EN C# & .NET tutorials, 18-module Curriculum, Capstone Projects, Exams, Certificates, SignalR Hubs, PostgreSQL 17 with automatic SQLite fallback, persistent DataProtection key ring (`/app/.keys`). |
| **Phase 2** | **Shared BuildingBlocks & Event-Driven Bus** | ✅ Completed | `src/Common` (`NET-Tutos.Common`) and `src/Infrastructure/EventBus` (`NET-Tutos.EventBus`) with MassTransit + RabbitMQ integration events (`ChallengeSolvedIntegrationEvent`, `LessonCompletedIntegrationEvent`, `ExamPassedIntegrationEvent`). |
| **Phase 3** | **CodeRunner Microservice (4-Layer Clean Architecture)** | ✅ Completed | `src/Services/CodeRunner/NET-Tutos.CodeRunner.{Domain,Application,Infrastructure,API}` (Port `5002`). Isolated Roslyn C# 14 script compiler, security guard, and multi-test-case judge with Strangler Fig fallback in `CodeExecutionService.cs`. |
| **Phase 4** | **Gamification Microservice & YARP API Gateway** | ✅ Completed | `src/Services/Gamification/NET-Tutos.Gamification.{Domain,Application,Infrastructure,API}` (Port `5004`) with MassTransit consumers & dedicated PostgreSQL DB (`nettutos_gamification_db`). `src/Gateways/Gateway` (`NET-Tutos.YarpGateway`, Port `5000`) with `CorrelationIdMiddleware` & Rate Limiting. |
| **Phase 5** | **AiTutor ("Atus") Microservice Extraction** | ✅ Completed | `src/Services/AiTutor/NET-Tutos.AiTutor.{Domain,Application,Infrastructure,API}` (Port `5006`). Decouples Big-O complexity analysis, compiler error diagnostics, xUnit test synthesis, code reviews, and 3D rubric technical mock interviews (`/api/v1/aitutor/*`). |
| **Phase 5.5** | **Cross-Platform .NET MAUI 10 Mobile App** | ✅ Completed | `src/Presentation/Mobile/NET-Tutos.Mobile.csproj` targeting Android, iOS, macOS Catalyst, and Windows with JWT auth, curriculum sync, and bilingual UI. |
| **Phase 8** | **Geo-IP Region Detection & Advanced SEO Engine** | ✅ Completed | `GeoIpService.cs` & `GeoIpRequestCultureProvider` (`VN` -> `vi`, Outside `VN` -> `en`), `SeoMetadataService.cs`, `SeoController.cs` (`/robots.txt`, `/sitemap.xml`, `/seo/og-image/{slug}.svg`, `/seo/og-project/{slug}.svg`, `/nettutos-indexnow-2026.txt`, `/api/seo/diagnostics`), `BreadcrumbList` & `FAQPage` Schema.org JSON-LD. |

---

## 4. Automated Test Suite Breakdown (136/136 Passing)

| Test Project | Path | Tests Count | Scope Covered |
| :--- | :--- | :---: | :--- |
| **`NET-Tutos.Gateway.Tests`** | `tests/NET-Tutos.Gateway.Tests` | 4 | YARP reverse proxy config, `/healthz`, `/api/v1/gateway/status`, `X-Correlation-ID` propagation. |
| **`NET-Tutos.CodeRunner.Tests`** | `tests/NET-Tutos.CodeRunner.Tests` | 11 | Roslyn C# compilation, security guard blocking (`System.Diagnostics.Process`, `System.IO.File`), test case judge. |
| **`NET-Tutos.Gamification.Tests`** | `tests/NET-Tutos.Gamification.Tests` | 16 | Domain value objects (`XpValue`, `StreakCount`), streak check-in rules, MassTransit event consumers. |
| **`NET-Tutos.AiTutor.Tests`** | `tests/NET-Tutos.AiTutor.Tests` | 16 | `EfficiencyScore`, `InterviewRubricScore` (50/25/25), Big-O detection, compiler diagnostics, xUnit generator, mock interview. |
| **`NET-Tutos.UnitTests`** | `tests/NET-Tutos.UnitTests` | 65 | Web services, `GeoIpService` CIDR blocks (VNPT/Viettel/FPT/VNNIC IPv4 & IPv6 vs International), `SeoMetadataService` (`TechArticle`, `BreadcrumbList`, `FAQPage`). |
| **`NET-Tutos.IntegrationTests`** | `tests/NET-Tutos.IntegrationTests` | 24 | Full HTTP pipeline, Geo-IP headers & public IP language switching, `/robots.txt`, `/sitemap.xml`, dynamic OG SVG endpoints, IndexNow, security & Roslyn endpoints. |

---

## 5. Next Strategic Roadmap Phases (Backlog)

### Phase 6: Cloud-Native Observability, Resilience & Redis Caching
- **Standard Resilience Handler (`Microsoft.Extensions.Http.Resilience` / Polly v8)**: Circuit Breaker, Exponential Backoff Retry, and Timeout policies on `CodeRunner`, `Gamification`, and `AiTutor` HTTP clients in `src/Presentation/Web/Program.cs`.
- **Redis Distributed Cache (`nettutos-redis`)**: Cache deterministic Roslyn compilation outputs (`SHA-256(Code + TestCases)`), `Leaderboard` rankings, and `/sitemap.xml`.
- **OpenTelemetry Instrumentation**: Distributed tracing & metrics across `YarpGateway`, `Web`, `CodeRunner`, `Gamification`, and `AiTutor`.

### Phase 7: Extract 4th Microservice — `Catalog & Assessment Service` (`NET-Tutos.Catalog.*`)
- Extract Tutorials, Categories, 18-Module Curriculum, Flashcards, Exams, and Certificate Verification into `src/Services/Catalog/NET-Tutos.Catalog.{Domain,Application,Infrastructure,API}` (Port `5008`).
- Route `/api/v1/catalog/{**catch-all}` through `NET-Tutos.YarpGateway`.

### Phase 9: Interactive In-Lesson Code Lab & Real-Time Multiplayer Coding Arena
- **Inline Executable Code Blocks**: Allow learners to edit and run C# code snippets directly inside tutorial reading pages (`Views/Tutorials/Details.cshtml`) via `CodeRunner.API`.
- **Real-Time Coding Battles (SignalR)**: Live algorithmic duel rooms with real-time test-case progress bars and ELO/XP rewards.
