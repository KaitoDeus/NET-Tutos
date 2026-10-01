# NET-Tutos

NET-Tutos is a comprehensive, production-ready educational platform and Learning Management System (LMS) designed for mastering modern .NET development. Built with ASP.NET Core MVC (.NET 10) and Entity Framework Core, the platform serves as an interactive learning environment for students and an administrative content management system for educators.

---

## Overview

NET-Tutos provides structured learning pathways, self-paced tutorials, real-time interactive quizzes, code cheat sheets, automated online examinations, digital vector PDF certificate generation, and an administrative CMS for curriculum management. It adheres to clean architecture principles, modern dependency injection lifetimes, repository and service abstractions, role-based authorization, and robust dual-engine data persistence with zero-configuration automated seeding.

---

## Key Features

### 1. Account and Student Progress Management (LMS)
- Integrated ASP.NET Core Identity authentication system with secure cookie-based session management.
- Student profile dashboard displaying completed lessons, total experience points (XP), enrolled tracks, and certification history.
- Dynamic lesson progress tracking with instantaneous AJAX completion toggling and reward mechanics (+20 XP per completed lesson).
- Automatic course enrollment tracking and completion rate calculations.

### 2. Timed Online Examinations and Automated Certification
- Randomized examination generator with customizable question pools and countdown timer.
- Automated instant grading and evaluation with detailed answer reviews.
- Vector PDF certificate generation powered by QuestPDF with embedded QR verification codes generated via QRCoder.
- Public digital verification endpoint (`/verify-certificate/{code}`) for third-party certificate authenticity validation.

### 3. Administrative LMS Control Panel and CMS Content Studio
- Role-based authorization distinguishing `Admin` and `Student` accounts.
- Administrative dashboard with key performance indicators (KPIs): student count, completion rates, examination metrics, and top student rankings.
- Visual Markdown authoring studio with split-screen live preview, syntax highlighting, and formatting toolbars.
- Comprehensive question bank management for creating and maintaining quiz and examination items.
- Student management directory with role elevation and experience point moderation tools.
- Default seeded administrator account:
  - Email: `admin@nettutos.com`
  - Password: `AdminPassword@123`

### 4. Structured Learning Roadmap
- Four comprehensive curriculum stages transitioning developers from fundamentals to enterprise-grade proficiency:
  - Stage 1: C# Fundamentals and Syntax (Types, Control Flow, Collections, Memory basics).
  - Stage 2: Object-Oriented Programming and Advanced C# (Inheritance, Interfaces, Generics, LINQ, Async/Await).
  - Stage 3: Entity Framework Core and Data Persistence (Code-First modeling, Migrations, Change Tracker, AsNoTracking optimization).
  - Stage 4: ASP.NET Core MVC and Web APIs (Middleware pipeline, Controllers, Razor engine, Dependency Injection lifetimes, SOLID principles).

### 5. Reading and Tutorial Experience
- Full Markdown rendering with Markdig support for tables, blockquotes, code blocks, and structured callouts.
- Code syntax highlighting powered by Prism.js with VS Code dark theme styling.
- One-click copy-to-clipboard functionality for code snippets.
- Real-time reading progress indicator bar.
- Seamless previous and next lesson navigation.

### 6. Interactive Practice Quizzes and Code Reference
- Topic-specific quizzes with instant feedback and answer explanations.
- Standardized syntax directory containing quick-reference snippets for C#, LINQ, EF Core, and ASP.NET Core.

### 7. Interactive C# Playground and Automated Coding Challenges
- Monaco Editor (VS Code web engine) with C# syntax highlighting, code folding, auto-indentation, and dark/light theme synchronization.
- Free-form C# code runner with pre-configured templates (Hello World, LINQ queries, Modern Records & Pattern Matching, Async/Await parallelism, Fibonacci generator).
- Roslyn scripting compiler backend (`Microsoft.CodeAnalysis.CSharp.Scripting`) with sandboxed execution, real-time diagnostic reporting (line/column error tracking), and standard output capture.
- LeetCode-style algorithm challenge catalog with difficulty tiering (Beginner, Intermediate, Advanced) and category categorization.
- Automated judge test harness executing code against public test cases and hidden evaluation cases.
- Execution timeout protection (4-second cutoff) and keyword security filtering to prevent malicious code invocation or infinite loops.
- Gamified experience point (XP) rewards upon passing all test cases with persistent submission history tracking (`CodeSubmission`).

### 8. Real-Time Community Discussion and Q&A Engine (SignalR)
- Instant bidirectional communication powered by ASP.NET Core SignalR (`DiscussionHub`).
- Interactive discussion threads beneath each tutorial lesson with live online learner counter.
- Markdown commenting supporting formatted text, quotes, and C# code snippets.
- Threaded discussions with parent-child nested replies.
- Peer upvoting with live counter synchronization across active learners.
- Verified solution recognition: Author or Administrator can mark comments as "Accepted Solution" with bonus XP rewards (+15 XP).
- Contribution incentives granting +5 XP for each valuable discussion contribution.

### 9. Gamification Leaderboard and Achievement Badges
- Public student ranking hall (`/Leaderboard`) showcasing top developers ranked by total Experience Points (XP).
- Visual podium honoring 1st, 2nd, and 3rd place champions with gold, silver, and bronze badges.
- Dynamic student rank card displaying real-time standing, completed lessons, solved algorithms, and earned credentials.
- Multi-tier achievement badge system:
  - Newbie: First lesson completed.
  - Scholar: 5 lessons completed.
  - Algorithm Hunter: First C# challenge solved.
  - Algorithm Master: 3 C# challenges conquered.
  - Certified: Official certificate achieved.
  - Community Hero: Accepted solution provided or active discussion participant.

### 10. Modern Responsive UI
- Fully responsive interface engineered with Bootstrap 5 and customized modern typography.
- Native Light and Dark theme switcher with persistent client preference storage.

---

## Technical Stack

- Framework: ASP.NET Core MVC (.NET 10)
- Language: C# 14
- ORM: Entity Framework Core 10
- Real-Time Communication: ASP.NET Core SignalR
- Code Analysis and Scripting Engine: Microsoft.CodeAnalysis.CSharp.Scripting (Roslyn)
- Web Code Editor: Monaco Editor
- Authentication: ASP.NET Core Identity with Role-Based Access Control (RBAC)
- Document Generation: QuestPDF
- Barcode Generation: QRCoder
- Markdown Engine: Markdig
- Client Libraries: Bootstrap 5, Bootstrap Icons, Prism.js, Monaco Editor, Microsoft SignalR Client
- Database Support: Microsoft SQL Server (LocalDB) with automated fallback to SQLite

---

## Architecture and Project Structure

```
.ASPNET-Tutos/
├── NET-Tutos.slnx                      # Solution definition (.NET 10 format)
├── .gitattributes                      # Repository metadata and Linguist rules
├── README.md                           # Project documentation
└── NET-Tutos/                          # Core ASP.NET Core web application
    ├── NET-Tutos.csproj                # Project configuration and package references
    ├── Hubs/                           # SignalR Real-Time Hubs
    │   └── DiscussionHub.cs            # Live connection group management and learner presence
    ├── Controllers/                    # MVC & API Controllers
    │   ├── AccountController.cs        # Authentication, student dashboard, and progress toggling
    │   ├── AdminController.cs          # LMS Admin dashboard, analytics, and Markdown live preview
    │   ├── AdminTutorialsController.cs # CMS tutorial management and authoring studio
    │   ├── AdminQuizzesController.cs   # Examination question bank management
    │   ├── AdminUsersController.cs     # Student roster, roles, and XP moderation
    │   ├── CertificateController.cs    # Certificate download and public QR verification
    │   ├── CheatSheetController.cs     # Code reference explorer
    │   ├── DiscussionController.cs     # Real-time discussion API with SignalR broadcasting
    │   ├── ExamController.cs           # Timed examinations, scoring, and certification issuance
    │   ├── HomeController.cs           # Landing page, curriculum overview, and metrics
    │   ├── LeaderboardController.cs    # Hall of fame student rankings and badge showcase
    │   ├── PlaygroundController.cs     # Interactive C# sandbox, code templates, and automated challenge judging
    │   ├── QuizController.cs           # Interactive practice quiz engine
    │   ├── RoadmapController.cs        # Learning roadmap pathways
    │   └── TutorialsController.cs      # Catalog browsing, lesson reading, search
    ├── Data/                           # Data access and persistence layer
    │   ├── AppDbContext.cs             # Identity and application database context
    │   └── DbInitializer.cs            # Schema setup, role creation, and default administrator seeding
    ├── Models/                         # Domain entities and viewmodels
    │   ├── Entities/                   # ApplicationUser, Category, Tutorial, QuizQuestion, Certificate, CodingChallenge, CodeTestCase, CodeSubmission, DiscussionComment, CommentUpvote, UserBadge
    │   └── ViewModels/                 # Presentation viewmodels (Account, Admin, Exam, Playground, Discussion, Leaderboard)
    ├── Services/                       # Application business logic layer
    │   ├── CertificateService.cs       # Vector PDF certificate generation with QR codes
    │   ├── CodeExecutionService.cs     # Roslyn-powered sandboxed execution and test case judge harness
    │   ├── DiscussionService.cs        # Threaded discussion logic, upvoting, and solution marking
    │   ├── ExamService.cs              # Timed examination grading and question generation
    │   ├── ICodeExecutionService.cs    # Sandboxed execution and evaluation interface contract
    │   ├── IDiscussionService.cs       # Discussion service interface contract
    │   ├── ILeaderboardService.cs      # Ranking calculations and automated badge triggers
    │   ├── LeaderboardService.cs       # Leaderboard and achievement computation service
    │   ├── LearningProgressService.cs  # Student progress and XP calculation service
    │   ├── MarkdownService.cs          # Markdown transformation service
    │   └── TutorialService.cs          # Content retrieval and search service
    ├── Views/                          # Razor views (.cshtml)
    │   ├── Account/                    # Login, Register, Student profile dashboard
    │   ├── Admin/                      # LMS overview and KPI dashboard
    │   ├── AdminQuizzes/               # Question bank management views
    │   ├── AdminTutorials/             # Tutorial CMS listing and WYSIWYG editor
    │   ├── AdminUsers/                 # Student and role management views
    │   ├── Certificate/                # Public certificate verification
    │   ├── CheatSheet/                 # Snippet catalog
    │   ├── Exam/                       # Examination hall, countdown timer, and results
    │   ├── Home/                       # Index, About
    │   ├── Leaderboard/                # Student ranking podium, table, and badges catalog
    │   ├── Playground/                 # Monaco Editor runner and challenge directory views
    │   ├── Quiz/                       # Quiz index and interactive test view
    │   ├── Roadmap/                    # Roadmap visualization
    │   ├── Shared/                     # Navigation, footer, _DiscussionSection.cshtml
    │   └── Tutorials/                  # Lesson listing, reader view, and Q&A section
    ├── wwwroot/                        # Static client assets (CSS, JS, images, icons)
    ├── appsettings.json                # Environment and database configuration
    └── Program.cs                      # Dependency injection, middleware pipeline, and host bootstrapping
```

---

## Getting Started

### Prerequisites

- .NET 10 SDK (or .NET 8.0+ SDK)
- Modern web browser
- Optional: SQL Server or SQL Server LocalDB (SQLite is supported out-of-the-box)

### Installation and Run

1. Clone the repository:
   ```bash
   git clone https://github.com/KaitoDeus/NET-Tutos.git
   cd NET-Tutos
   ```

2. Build the solution:
   ```bash
   dotnet build NET-Tutos.slnx
   ```

3. Run the application:
   ```bash
   dotnet run --project NET-Tutos/NET-Tutos.csproj
   ```

4. Open your browser and navigate to `http://localhost:5262`.

5. Sign in as administrator:
   - Email: `admin@nettutos.com`
   - Password: `AdminPassword@123`

---

## Database Configuration

The application implements a dual-provider database strategy configured in `appsettings.json`:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DotNetTutorialsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
    "SqliteConnection": "Data Source=dotnet_tutorials.db"
  }
}
```

### Automatic Fallback Architecture

- **Primary Provider**: Microsoft SQL Server LocalDB (`DefaultConnection`).
- **Resilient Fallback**: If SQL Server LocalDB is unavailable or not running on the host system, the runtime seamlessly switches to SQLite (`dotnet_tutorials.db`).
- **Automated Seeding**: On first run, the database schema, default roles (`Admin`, `Student`), administrator account, and a complete curriculum of tutorials, categories, and quizzes are automatically provisioned without requiring manual migration scripts.

---

## License

This project is licensed under the MIT License. See the LICENSE file for details.
