# 03. Functional Requirements Document (Business Requirements Document - BRD)

Document Type: Business Requirements Specification  
Role: Senior Business Analyst  
System: .NET Tutos - Comprehensive .NET Learning & Assessment Platform  
Document Identifier: BRD-NET-TUTOS-2026-V2  
Status: Approved  

---

## 1. Requirement Classification Matrix

The system capabilities are structured across seven business functional modules:

| Module Identifier | Functional Module Name | Target Platform Scope | Priority Tier |
| :--- | :--- | :--- | :--- |
| **MOD-LMS** | Curriculum & Learning Management System | Web Application & Mobile App | P0 (Must Have) |
| **MOD-PLAY** | Online Interactive Code Playground (Monaco & Sandbox) | Web Application | P0 (Must Have) |
| **MOD-AI** | AI Mentor & Interactive Coding Assistant | Web Application | P0 (Must Have) |
| **MOD-INT** | AI Mock Technical Interview Simulator | Web Application | P1 (High) |
| **MOD-GAM** | Gamification, Learning Streaks & Community Engine | Web Application & Mobile App | P1 (High) |
| **MOD-MOB** | Cross-Platform Mobile Client (.NET MAUI) | Mobile (Android / iOS / Windows) | P0 (Must Have) |
| **MOD-ADM** | Administrative Back-Office & Cohort Oversight | Web Application | P1 (High) |

---

## 2. Detailed Functional Requirements

### Module 1: Curriculum & Learning Management System (MOD-LMS)

#### `REQ-LMS-01`: Standardized 7-Module Microsoft .NET Curriculum Structure
- **Description:** The system provides a sequential 101-lesson curriculum categorized into seven learning modules:
  1. *C# Fundamentals & Object-Oriented Programming*
  2. *Advanced C# & Memory Management*
  3. *LINQ & Collections Deep Dive*
  4. *Asynchronous Programming (async/await, Task, Threading)*
  5. *Entity Framework Core & Database Performance*
  6. *ASP.NET Core Web API & Modern Architecture*
  7. *Clean Architecture, Microservices & Cloud Patterns*
- **User Story:** As an aspiring .NET software engineer, I want a structured progression roadmap from introductory syntax to senior-level architectural design, so that I understand my current competencies and next learning milestones.
- **Acceptance Criteria:**
  - **Given** an authenticated or guest user navigating to the curriculum view (`/curriculum` or `/roadmap`).
  - **When** the user inspects any curriculum module.
  - **Then** the platform displays the ordered lesson list, estimated reading duration, difficulty tier (Beginner, Intermediate, Advanced), and completion state (Completed / Pending).

#### `REQ-LMS-02`: Rich Markdown Content Rendering & Syntax Highlighting
- **Description:** Lesson content is authored in extended Markdown, featuring syntax-highlighted C# code blocks, callout alert containers, diagrams, and tabular data.
- **Acceptance Criteria:**
  - Automatically generate an interactive Table of Contents indexed to `H2` and `H3` headings.
  - Provide a "Save Offline" action leveraging the Progressive Web App (PWA) cache for disconnected study.
  - Provide sequential navigation controls (Previous Lesson / Next Lesson).

#### `REQ-LMS-03`: Assessment Quizzes & Knowledge Verification
- **Description:** Each curriculum lesson includes a multi-question self-assessment quiz to reinforce key concepts.
- **Acceptance Criteria:**
  - Achieving a score of $\ge 80\%$ marks the lesson assessment as passed and credits bonus experience points (XP).

---

### Module 2: Online Interactive Code Playground (MOD-PLAY)

#### `REQ-PLAY-01`: Monaco Editor Integration
- **Description:** Embed the Microsoft Monaco Editor (version 0.45.0) within the browser environment.
- **Acceptance Criteria:**
  - Provide full syntax highlighting and IntelliSense auto-completion for C# 14.
  - Support standard keyboard shortcuts: `Ctrl + Enter` (Execute Script), `Ctrl + S` (Format Document), and `Ctrl + I` (Consult AI Mentor).
  - Provide an in-editor Settings Modal allowing users to configure font family (`Fira Code`, `JetBrains Mono`, `Cascadia Code`), font size ($12\text{px} \dots 20\text{px}$), tab indentation ($2 / 4$ spaces), font ligatures, minimap display, and word wrapping.
  - Support automatic Dark/Light theme synchronization with the web shell as well as manual theme selection (`vs-dark`, `vs`, `hc-black`).

#### `REQ-PLAY-02`: Roslyn Scripting Execution Sandbox
- **Description:** Dynamically compile and execute C# scripts in an isolated server-side sandbox environment.
- **Acceptance Criteria:**
  - Intercept standard output (`Console.WriteLine`) and stream results directly to the simulated web terminal console.
  - Measure and report execution duration in milliseconds.
  - Capture compilation diagnostics and display structured error codes (`CSxxxx`), line numbers, and readable messages.
  - Enforce an automated hard execution timeout of $3000\text{ms}$ to prevent denial of service from infinite loops.

#### `REQ-PLAY-03`: Algorithmic Coding Challenges & Automated Judge Engine
- **Description:** Provide interactive programming challenges evaluated against standardized unit test suites.
- **Acceptance Criteria:**
  - The learner writes logic within a predefined `Solution` method template and triggers "Submit Solution".
  - The judge engine executes the submission against a matrix of `CodeTestCase` records (including public and hidden evaluation cases).
  - If 100% of test cases pass: Flag the attempt as `Accepted`, display aggregate execution time, grant XP points, and update challenge completion metrics.
  - If any test case fails: Render a failure breakdown table (Input Parameters, Actual Output, Expected Output) with a dedicated "AI Diagnose" button on each failing row.

---

### Module 3: AI Mentor & Interactive Coding Assistant (MOD-AI)

#### `REQ-AI-01`: Big-O Algorithmic Complexity Analysis (`AnalyzeComplexity`)
- **Description:** Inspect the active C# snippet in the editor to evaluate algorithmic time complexity ($O(1), O(\log N), O(N), O(N \log N), O(N^2), O(2^N)$) and space complexity ($O(1), O(N)$).
- **Acceptance Criteria:**
  - Render color-coded complexity badges (Green for optimal $O(1)$ and $O(N)$; Red for sub-optimal $O(N^2)$ and $O(2^N)$).
  - Compute a numerical efficiency score from 1 to 100.
  - Enumerate identified computational bottlenecks (e.g., nested loops, unbuffered string concatenations).
  - Recommend concrete architectural and data structure optimizations.

#### `REQ-AI-02`: Test Case Failure Diagnosis (`DiagnoseTestFailure`)
- **Description:** Provide immediate diagnostic explanations when a coding challenge submission fails one or more test cases.
- **Acceptance Criteria:**
  - Accurately classify the root cause into standard engineering failure categories:
    - *Edge Case / Null Handling*
    - *Off-by-one Indexing*
    - *Case Sensitivity*
    - *Duplicate Element / Index Reuse*
    - *Logic Flaw / State Mutation*
  - Explain the discrepancy between actual output and expected behavior.
  - Provide a verified code fix snippet accompanied by two one-click actions: "Copy Code" and "Apply to Editor".

#### `REQ-AI-03`: Automated xUnit Test Suite Generation (`GenerateUnitTest`)
- **Description:** Automatically synthesize an xUnit unit test suite for the current code in the editor.
- **Acceptance Criteria:**
  - Adhere strictly to the Arrange-Act-Assert (AAA) testing pattern.
  - Produce both standard unit tests (`[Fact]`) and parameterized boundary tests (`[Theory]`, `[InlineData]`).
  - Provide copy-to-clipboard and direct editor insertion actions.

#### `REQ-AI-04`: Keyboard Shortcut `Ctrl+I` & Monaco Context Menu
- **Description:** Enable learners to select any code segment in Monaco Editor and trigger AI assistance via `Ctrl+I` or right-click context menu options:
  - *AI Mentor: Explain Selected Code*
  - *AI Mentor: Analyze Big-O Complexity*
  - *AI Mentor: Generate xUnit Test Suite*

---

### Module 4: AI Mock Technical Interview Simulator (MOD-INT)

#### `REQ-INT-01`: AI Tech Lead Role-Playing Simulation
- **Description:** Simulate a high-stakes technical interview led by an AI acting as a Principal Architect or Senior Interviewer.
- **Acceptance Criteria:**
  - Assemble a randomized 5-question interview session filtered by seniority tier (Junior, Middle, Senior) or specialized topic domain (C# Internals, Concurrency, EF Core, ASP.NET Core, Architecture).
  - Maintain an active session countdown timer (default 10 minutes).
  - Provide a circular numeric question stepper (1 to 5) allowing direct navigation and progress tracking.

#### `REQ-INT-02`: Three-Dimensional Candidate Response Assessment
- **Description:** Evaluate candidate free-text answers across three recruitment dimensions:
  1. **Technical Accuracy (0 to 100):** Grasp of CLR fundamentals, runtime mechanisms, memory allocation, and type safety.
  2. **Clarity & Structure (0 to 100):** Communication precision, concise framing, and architectural terminology.
  3. **Practical Depth (0 to 100):** Awareness of real-world pitfalls, production edge cases, and performance tradeoffs.
- **Acceptance Criteria:**
  - Classify candidate performance tier: `Senior Level`, `Strong Mid`, `Junior Level`, or `Needs Revision`.
  - Extract specific **Key Strengths** present in the answer.
  - Highlight critical **Missing Points** required to satisfy senior-level criteria.
  - Provide a comprehensive **Model Benchmark Answer**.

#### `REQ-INT-03`: Interactive Follow-Up Challenge
- **Description:** The AI interviewer formulates an inquisitive follow-up probe targeting ambiguities or edge cases in the candidate's initial answer.
- **Acceptance Criteria:**
  - Candidates can submit an in-place response to the follow-up question.
  - The AI reviews the follow-up response and grants bonus XP upon satisfactory justification.

#### `REQ-INT-04`: Speech Recognition via Web Speech API
- **Description:** Provide a speech-to-text input capability via an integrated microphone toggle.
- **Acceptance Criteria:**
  - Support both English (`en-US`) and Vietnamese (`vi-VN`) speech models based on application locale.
  - Provide an animated recording state indicator and stream transcribed text into the answer input area.

#### `REQ-INT-05`: Comprehensive Assessment Report & Recommended Learning Roadmap
- **Description:** Synthesize the completed interview session into a formal evaluation report.
- **Acceptance Criteria:**
  - Determine overall engineering readiness: `Senior Ready`, `Middle Ready`, or `Junior Ready`.
  - Display a 5-dimension competency breakdown progress matrix.
  - Present an Executive Assessment narrative from the interview board.
  - List targeted curriculum tutorials with direct navigation links to remediate identified deficiencies.
  - Credit session XP points and update the learner's daily streak.

---

### Module 5: Gamification, Learning Streaks & Community Engine (MOD-GAM)

#### `REQ-GAM-01`: Daily Learning Streak Engine
- **Description:** Track consecutive daily learning engagement when a user performs at least one qualifying action (reading a lesson, submitting code, completing a quiz, or finishing an interview).
- **Business Rule:** Inactivity exceeding 24 hours from the prior active date resets the streak counter to 1.

#### `REQ-GAM-02`: Experience Points (XP) & Progression Levels
- **XP Earning Rules:**
  - Complete tutorial reading: `+15 XP`
  - Pass lesson assessment quiz: `+20 XP`
  - Solve coding challenge: `+30 XP`
  - Complete AI mock interview session: `+50 XP` to `+90 XP`
  - Daily login check-in: `+10 XP`
- **Level Calculation Formula:** $\text{Level} = \lfloor \frac{\text{XP}}{100} \rfloor + 1$.

#### `REQ-GAM-03`: Hall of Fame & Community Leaderboard
- **Description:** Display global leaderboards tracking top learners across Weekly, Monthly, and All-Time intervals based on XP and streak milestones.

#### `REQ-GAM-04`: Real-Time Discussion Forum (SignalR)
- **Description:** Provide contextual discussion threads beneath lessons with comment upvoting, answer marking, and real-time push updates via SignalR.

---

### Module 6: Cross-Platform Mobile Client (.NET MAUI) (MOD-MOB)

#### `REQ-MOB-01`: Unified Authentication & Identity Synchronization
- **Description:** Learners authenticate using unified web credentials against `/api/mobile/auth/login`.
- **Acceptance Criteria:** Securely store issued JWT Bearer tokens in device storage (`Preferences`); automatically sync name, avatar, XP balance, level, and streak counter.

#### `REQ-MOB-02`: Tutorial Consumption & Progress Check-in
- **Description:** Provide a responsive mobile reading experience for all 101 curriculum lessons.
- **Acceptance Criteria:** Tapping "Mark as Completed" on mobile invokes the shared API, records completion in the central database, awards XP, and synchronizes status with the Web Application.

#### `REQ-MOB-03`: Adaptive Theme Engine
- **Description:** Support three display modes: System Default, Light Mode, and Dark Mode.

#### `REQ-MOB-04`: Dynamic API Server Configuration
- **Description:** Provide a configuration input on the login and settings screens allowing users and testers to switch API host endpoints (e.g., `10.0.2.2:5000` for Android emulators or local LAN IP addresses for physical devices).

---

### Module 7: Administrative Back-Office & Cohort Oversight (MOD-ADM)

#### `REQ-ADM-01`: Curriculum and Challenge Management
- **Description:** Provide administrative controls for managing categories, tutorial content, coding challenges, and unit test cases.

#### `REQ-ADM-02`: Learner Cohort Analytics & Activity Auditing
- **Description:** Enable instructors to inspect aggregate completion rates, common failure patterns, and system engagement metrics.
