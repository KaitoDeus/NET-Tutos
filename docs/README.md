# System Requirements Specifications & Architectural Overview (.NET Tutos Platform)

Document Type: Master Index & Architecture Specification Suite  
Author: Senior Business Analyst / Enterprise Solution Specialist  
System: .NET Tutos - Comprehensive .NET Learning & Assessment Platform  
Scope: Web Application (ASP.NET Core 10), Mobile Client (.NET MAUI 10), AI Tutor Engine, Roslyn Sandbox  
Version: Release 2.0  
Status: Verified & Approved for Production  

---

## 1. Document Suite Overview

This documentation suite serves as the **Single Source of Truth (SSOT)** for the .NET Tutos platform across engineering and product teams (Product Owners, Solution Architects, Lead Developers, QA Engineers, DevOps, and Stakeholders).

The suite standardizes requirements across the entire software delivery lifecycle, encompassing market problem statements, target user personas, multi-tier system topology, functional requirements (BRD), non-functional requirements (NFR), relational schema models (ERD), and cross-platform RESTful API contracts.

---

## 2. Document Suite Structure

```
d:\.ASPNET-Tutos\docs\
├── README.md                           # Master Index & Requirements Traceability Matrix (Current Document)
├── 01_business_overview.md             # Business Vision, Strategic Value Proposition & Personas
├── 02_system_architecture_overview.md  # Multi-Tier Ecosystem Topology, Tech Stack & Sequence Flows
├── 03_functional_requirements_brd.md   # Functional Requirements Specification (BRD) for 7 Core Modules
├── 04_non_functional_requirements.md   # Quality Attributes, Performance SLAs, Security & PWA Specs
├── 05_data_models_and_api_specs.md     # Relational ERD Schemas & RESTful Integration API Contracts
├── 06_architecture_and_design_blueprint.md # Architecture Blueprint, Folder Breakdown, ERD & UI/UX Wireframes
└── 07_decoupling_backend_frontend_plan.md  # Backend (.NET 10 API) & Frontend (Next.js TS) Decoupling Plan
```

### Detailed Document Summaries:

1. [**01. Business Overview & Value Proposition**](file:///d:/.ASPNET-Tutos/docs/01_business_overview.md)
   - Executive project summary and target outcomes.
   - Market problem analysis across 4 foundational challenges.
   - Strategic value propositions and Key Performance Indicators (KPIs).
   - Target personas: University Student / Fresher, Junior Developer, and LMS Instructor.
   - End-to-end Learner Journey Map.

2. [**02. System Architecture Overview**](file:///d:/.ASPNET-Tutos/docs/02_system_architecture_overview.md)
   - Multi-tier system topology diagram (Presentation, Gateway/API, Application Services, Data Access, Persistence).
   - Core technology specifications (.NET 10, C# 14, .NET MAUI, EF Core 10, Roslyn Scripting API, SignalR, Monaco Editor).
   - Detailed sequence data flow diagrams:
     - Playground execution, compilation error diagnosis, and automated challenge evaluation.
     - Interactive AI Mock Technical Interview with speech transcription and scoring.
     - Bidirectional cross-platform synchronization between Mobile App and Web App.
   - Security isolation model and Roslyn sandbox boundary controls.

3. [**03. Functional Requirements Document (BRD)**](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md)
   - Formal User Stories and Given/When/Then Acceptance Criteria across 7 functional modules:
     - **MOD-LMS:** 101-lesson Microsoft .NET curriculum, rich Markdown rendering, assessment quizzes.
     - **MOD-PLAY:** Monaco Editor, execution shortcuts (`Ctrl+Enter`, `Ctrl+S`, `Ctrl+I`), automated judge engine.
     - **MOD-AI:** Algorithmic Big-O analysis ($O(1) \dots O(2^N)$), test failure diagnostics, automated xUnit generation.
     - **MOD-INT:** AI Tech Lead interview simulation, 3-dimensional scoring, speech recognition, competency radar report.
     - **MOD-GAM:** Daily streak tracking, XP points and leveling formula, Hall of Fame leaderboard, SignalR discussions.
     - **MOD-MOB:** .NET MAUI cross-platform client, lesson reading, progress sync, light/dark themes, custom API host configuration.
     - **MOD-ADM:** Administrative management and cohort oversight.

4. [**04. Non-Functional Requirements Document (NFR)**](file:///d:/.ASPNET-Tutos/docs/04_non_functional_requirements.md)
   - Performance metrics and SLAs/SLOs: Page load $< 1.2\text{s}$, API latency $< 150\text{ms}$, sandbox timeout $\le 3\text{s}$.
   - Scalability standards: $\ge 1,000$ concurrent active users, memory optimization via `AsNoTracking()` and database pagination.
   - Information security: Roslyn sandbox isolation, PBKDF2 password hashing, JWT Bearer authentication, OWASP Top 10 defenses.
   - Offline resilience via Progressive Web App (PWA) Service Workers and cross-platform responsive design (WCAG 2.1 AA).

5. [**05. Data Models & API Specifications**](file:///d:/.ASPNET-Tutos/docs/05_data_models_and_api_specs.md)
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

7. [**07. Backend & Frontend Decoupling Plan**](file:///d:/NET-Tutos/docs/07_decoupling_backend_frontend_plan.md)
   - Decoupled architecture target: ASP.NET Core 10 Web API + Next.js 15 React TypeScript + .NET MAUI.
   - Complete Solution structure and migration phases.
   - Standardized RESTful API contracts matrix covering Auth, Tutorials, Roslyn Playground, AI Tutor, and SignalR Realtime.
   - CORS, JWT authentication, and zero-downtime transition strategy.

---

## 3. Requirements Traceability Matrix (RTM)

| Business Goal | Functional Module | Implementation Components | Verification Strategy |
| :--- | :--- | :--- | :--- |
| **Zero-Friction In-Browser Coding** | `MOD-PLAY` | `PlaygroundController.cs`, `CodeExecutionService.cs`, `Index.cshtml` (Playground) | Unit tests, Roslyn Scripting sandbox, Monaco Editor integration |
| **Contextual AI Engineering Feedback** | `MOD-AI` | `AiTutorController.cs`, `AiTutorService.cs`, `AiTutorViewModels.cs` | Big-O analysis, failure diagnostics, dynamic xUnit test generation |
| **Senior Technical Hiring Readiness** | `MOD-INT` | `InterviewController.cs`, `AiTutorService.cs`, `Mock.cshtml` | 3-dimension rubric evaluation, Speech-to-Text, radar reports |
| **Continuous Cross-Platform Study** | `MOD-MOB` | `MobileAuthController.cs`, `ApiService.cs` (.NET MAUI), `LoginPage.xaml` | JWT Bearer verification, device storage, progress sync |
| **Learner Engagement & Retention** | `MOD-GAM` | `StreakService.cs`, `LeaderboardService.cs`, `ActivityFeedService.cs` | Daily check-in tests, Level formula calculation, Hall of Fame |

---

## 4. Role-Based Navigation Guide

- **Product Owners & Business Stakeholders:** Review [01. Business Overview](file:///d:/.ASPNET-Tutos/docs/01_business_overview.md) and [03. Functional Requirements (BRD)](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md) to understand the product roadmap, functional scope, and strategic business goals.
- **Solution Architects & Engineering Leads:** Review [02. System Architecture Overview](file:///d:/.ASPNET-Tutos/docs/02_system_architecture_overview.md) and [05. Data Models & API Specifications](file:///d:/.ASPNET-Tutos/docs/05_data_models_and_api_specs.md) to evaluate system topology, data flows, and schema structures.
- **Software Developers (Web & Mobile):** Refer to [03. Functional Requirements (BRD)](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md) for acceptance criteria and [05. Data Models & API Specifications](file:///d:/.ASPNET-Tutos/docs/05_data_models_and_api_specs.md) for endpoint request/response contracts.
- **QA & Test Automation Engineers:** Leverage acceptance criteria in [03. Functional Requirements (BRD)](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md) and performance benchmarks in [04. Non-Functional Requirements (NFR)](file:///d:/.ASPNET-Tutos/docs/04_non_functional_requirements.md) to create comprehensive test plans and load testing scripts.
