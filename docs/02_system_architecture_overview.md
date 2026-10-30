# 02. System Architecture Overview

Document Type: Technical Solution Architecture Specification  
Role: Senior Business Analyst / Enterprise Solution Specialist  
System: .NET Tutos - Comprehensive .NET Learning & Assessment Platform  
Version: 2.0  
Status: Approved  

---

## 1. System Ecosystem Topology

The **.NET Tutos** ecosystem is architected as an N-tier layered system integrated with domain-driven application services. This topology concurrently serves both the responsive Web Application and cross-platform Mobile Clients over a unified relational data tier:

```mermaid
graph TB
    subgraph ClientLayer ["1. Client Presentation Layer"]
        WebBrowser["Web Application Client<br/>(Bootstrap 5, Monaco Editor, Vanilla JS, SignalR Client, PWA)"]
        MobileClient["Mobile Application Client<br/>(.NET MAUI 10 Multi-targeting: Android / iOS / Windows)"]
    end

    subgraph GatewayAndApi ["2. Controller & API Layer (ASP.NET Core 10)"]
        WebControllers["Web MVC Controllers<br/>(Tutorials, Playground, Interview, Leaderboard, Admin)"]
        MobileApiControllers["RESTful Mobile APIs<br/>(/api/mobile/auth, /api/mobile/tutorials)"]
        SignalRHubs["Real-Time SignalR Hubs<br/>(DiscussionHub, NotificationHub)"]
    end

    subgraph ServiceCore ["3. Domain Application Services Layer"]
        AiService["AI Tutor & Assessment Service<br/>(AiTutorService)"]
        RoslynSandbox["Roslyn Code Sandbox Engine<br/>(CodeExecutionService)"]
        LmsService["Curriculum & Learning Progress<br/>(CurriculumService, LearningProgressService)"]
        GamificationService["Gamification Engine<br/>(StreakService, LeaderboardService)"]
        FlashcardServiceCore["Flashcard & Spaced Repetition<br/>(FlashcardService)"]
    end

    subgraph DataAccessLayer ["4. Data Access Layer & ORM"]
        EFCore["Entity Framework Core 10<br/>(AppDbContext with IdentityDbContext)"]
    end

    subgraph DatabaseLayer ["5. Persistence Storage Layer"]
        SQLDb[("Relational Database<br/>(PostgreSQL 17 Primary via Docker / SQLite / SQL Server)")]
        LocalCache["Client Preferences & Secure Storage<br/>(Mobile & Browser LocalStorage)"]
    end

    WebBrowser -->|HTTPS / HTML / WSS| WebControllers
    WebBrowser -->|WSS / SignalR| SignalRHubs
    MobileClient -->|HTTPS REST JSON / Bearer JWT| MobileApiControllers
    MobileClient -.->|Offline Sync| LocalCache

    WebControllers --> ServiceCore
    MobileApiControllers --> ServiceCore
    SignalRHubs --> ServiceCore

    ServiceCore --> EFCore
    EFCore --> SQLDb
```

---

## 2. Core Technologies & Technical Stack Specifications

| Component | Technology / Framework | Version | Role & Strategic Purpose |
| :--- | :--- | :--- | :--- |
| **Backend Framework** | ASP.NET Core MVC | .NET 10 | Central application server providing server-rendered HTML views and exposing RESTful APIs for mobile and background clients. |
| **Mobile Framework** | .NET MAUI (Multi-platform App UI) | .NET 10 | Native cross-platform mobile application (Android, iOS, Windows) implemented with C# and XAML. |
| **Data Access / ORM** | Entity Framework Core | 10.0 | Object-relational mapping, database migrations, and high-performance querying using compiled LINQ, `IQueryable`, and `AsNoTracking`. |
| **Relational Database** | PostgreSQL 17 (Primary via Docker Compose) / SQLite / SQL Server | 17.x / 3.x / 2022 | Persistent storage for users, curriculum, progress, coding challenges, test cases, and interview records via Npgsql EF Core. |
| **Containerization** | Docker & Docker Compose | 29.x / 5.x | Comprehensive orchestration for PostgreSQL 17 Alpine, ASP.NET Core 10 Web App, and pgAdmin 4 GUI. |
| **Code Editor** | Microsoft Monaco Editor | 0.45.0 | In-browser code editing engine (VS Code core) providing C# syntax highlighting, shortcut handling (`Ctrl+I`, `Ctrl+Enter`), and code formatting. |
| **Compiler & Sandbox** | Roslyn Scripting API (`Microsoft.CodeAnalysis.CSharp.Scripting`) | 4.12+ | In-memory dynamic compilation and isolated execution of C# code snippets with strict resource and security bounds. |
| **Real-Time Communication** | ASP.NET Core SignalR | .NET 10 | Bidirectional WebSocket transport for activity feeds, discussion threads, and real-time community interactions. |
| **Authentication & Identity** | ASP.NET Core Identity & JWT Bearer | .NET 10 | Secure user management, salted password hashing, role-based access control, and stateless JWT token issuance for mobile endpoints. |
| **Web Presentation** | Bootstrap 5, Bootstrap Icons, CSS Modern Variables | 5.3.3 | Responsive frontend presentation layer with system-wide Dark and Light theme adaptation. |
| **Speech Recognition** | Web Speech API (`SpeechRecognition`) | W3C Standard | Client-side speech-to-text dictation supporting English and Vietnamese during interactive AI mock technical interviews. |

---

## 3. Data Flow Architecture

### Flow 1: Compilation and Automated Code Assessment (Playground & Challenge Judge Engine)

```mermaid
sequenceDiagram
    autonumber
    actor Learner as Learner
    participant Editor as Monaco Editor (Browser)
    participant Controller as PlaygroundController
    participant Sandbox as CodeExecutionService
    participant Roslyn as Roslyn Scripting Engine
    participant AI as AiTutorService
    participant DB as AppDbContext

    Learner->>Editor: Enter C# solution and click "Submit Solution"
    Editor->>Controller: POST /Playground/SubmitChallenge { challengeId, code }
    Controller->>DB: Query CodeTestCase collection (InputParameters, ExpectedOutput)
    Controller->>Sandbox: ExecuteWithTestCasesAsync(code, testCases)
    
    alt Compilation Failure
        Sandbox-->>Controller: CompileError (CS0103, CS1061, ...)
        Controller-->>Editor: Return compilation diagnostics
        Editor->>Learner: Render diagnostic banner with "Explain Error with AI" button
        Learner->>AI: Request error root cause analysis and resolution steps
        AI-->>Editor: Return detailed diagnosis, line numbers, and correction guide
    else Successful Compilation (Runtime Evaluation)
        loop Iterate Through Each Test Case
            Sandbox->>Roslyn: Pass test parameters and invoke Solution method
            Roslyn-->>Sandbox: Return ActualOutput
            Sandbox->>Sandbox: Compare ActualOutput with ExpectedOutput
        end
        alt 100% Test Cases Passed
            Sandbox-->>Controller: AllPassed = True, ExecutionTimeMs
            Controller->>DB: Grant XP points and mark challenge as completed
            Controller-->>Editor: Accepted notification with XP reward summary
        else One or More Test Cases Failed
            Sandbox-->>Controller: AllPassed = False, detailed failure report
            Controller-->>Editor: Render test matrix with "AI Diagnose" button per failing case
            Learner->>AI: Trigger "AI Diagnose" for failed test case
            AI-->>Editor: Return root cause categorization (Off-by-one, Edge Case) with verified fix
        end
    end
```

### Flow 2: AI Mock Technical Interview

```mermaid
sequenceDiagram
    autonumber
    actor Candidate as Candidate
    participant WebUI as Interview Interface (Mock.cshtml)
    participant Speech as Web Speech API (Microphone)
    participant InterviewCtrl as InterviewController
    participant AiTutor as AiTutorService
    participant DB as AppDbContext & Identity

    Candidate->>WebUI: Click "Start AI Mock Interview"
    WebUI->>InterviewCtrl: GET /Interview/Mock?count=5
    InterviewCtrl->>DB: Fetch 5 randomized interview questions matching difficulty
    InterviewCtrl-->>WebUI: Initialize interview room and start countdown timer
    
    Candidate->>Speech: Click "Speak Answer" and dictate response
    Speech-->>WebUI: Stream transcribed text into answer textarea
    Candidate->>WebUI: Click "Submit to AI Tech Lead"
    
    WebUI->>InterviewCtrl: POST /Interview/EvaluateAiAnswer { flashcardId, candidateAnswer }
    InterviewCtrl->>AiTutor: EvaluateInterviewAnswerAsync(request)
    AiTutor->>AiTutor: Score across 3 dimensions: Technical Accuracy, Clarity, Practical Depth
    AiTutor->>AiTutor: Extract Strengths, Missing Points, Follow-up Challenge, and Benchmark Answer
    AiTutor-->>InterviewCtrl: AiInterviewEvaluationResponse
    InterviewCtrl-->>WebUI: Return structured evaluation
    WebUI->>Candidate: Display scores, strengths/weaknesses, and follow-up prompt
    
    opt Follow-Up Challenge Response
        Candidate->>WebUI: Enter response to follow-up challenge
        WebUI->>InterviewCtrl: POST /Interview/AnswerAiFollowUp
        InterviewCtrl->>AiTutor: AnswerFollowUpAsync
        AiTutor-->>WebUI: Tech Lead assessment and bonus XP allocation
    end
    
    Candidate->>WebUI: Click "Complete & Generate Report"
    WebUI->>InterviewCtrl: POST /Interview/SubmitAiMockReport
    InterviewCtrl->>AiTutor: GenerateInterviewReportAsync(items)
    AiTutor->>DB: Filter recommended lessons based on identified skill gaps
    AiTutor-->>InterviewCtrl: Five-dimension competency report
    InterviewCtrl->>DB: Persist earned XP and update daily streak
    InterviewCtrl-->>WebUI: Render comprehensive interview assessment report
```

### Flow 3: Cross-Platform Data Synchronization (Mobile App & Web App)

```mermaid
sequenceDiagram
    autonumber
    actor Learner as Learner
    participant Mobile as Mobile App (.NET MAUI)
    participant MobileApi as MobileAuthController / MobileTutorialsController
    participant DB as Unified Database (AppDbContext)
    participant Web as Web Application (ASP.NET Core)

    Learner->>Mobile: Log in with web credentials (email and password)
    Mobile->>MobileApi: POST /api/mobile/auth/login
    MobileApi->>DB: Verify credentials against AspNetUsers
    MobileApi-->>Mobile: Return JWT Bearer Token and user state (XP, Streak, Level)
    Mobile->>Mobile: Persist JWT securely in device storage
    
    Learner->>Mobile: Review tutorial and click "Mark as Completed"
    Mobile->>MobileApi: POST /api/mobile/tutorials/complete { tutorialId } (Bearer Token)
    MobileApi->>DB: Insert UserLessonProgress record (IsCompleted = true)
    MobileApi->>DB: Increment +15 XP in AspNetUsers
    MobileApi->>DB: Record DailyCheckIn and update streak counter
    MobileApi-->>Mobile: Return success response with updated XP balance
    
    Learner->>Web: Access Web Application in browser
    Web->>DB: Query current user profile and curriculum state
    DB-->>Web: Return updated XP, refreshed streak, and completed lessons
    Web->>Learner: Display state synchronized with mobile session
```

---

## 4. Security, Isolation & Risk Mitigation Strategy

1. **Roslyn Sandbox Execution Isolation:**
   - **Namespace Blacklisting:** Strictly disallow loading dangerous or unrestricted namespaces, including `System.IO`, `System.Diagnostics.Process`, `System.Reflection.Emit`, and `System.Net.Sockets`.
   - **Execution Timeouts:** Hard timeout capped at $3000\text{ms}$ per execution to prevent infinite loops, thread locks, and stack overflow crashes.
   - **Standard Output Redirection:** Redirect `Console.SetOut()` to an isolated in-memory `StringWriter` with a $10,000$ character output ceiling to prevent memory bloating attacks.
2. **Authentication and Authorization Security:**
   - **Web Application:** Standard Cookie-based authentication paired with Anti-Forgery Tokens (`__RequestVerificationToken`) on all state-altering requests to protect against Cross-Site Request Forgery (CSRF).
   - **Mobile API Endpoints:** Stateless JWT Bearer tokens signed via HMAC-SHA256 with cryptographically robust keys and explicit expiration windows.
3. **Data Integrity and Protection:**
   - **Password Security:** Password hashing using standard PBKDF2 with HMAC-SHA256 through ASP.NET Core Identity.
   - **SQL Injection Defense:** Strict adherence to parameterized SQL queries via Entity Framework Core LINQ query translation.
   - **Cross-Site Scripting (XSS) Mitigation:** Context-aware HTML sanitization and encoding across all user-generated markdown tutorials, comments, and interview responses.
