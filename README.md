# NET-Tutos

NET-Tutos is a comprehensive, production-ready educational platform and Learning Management System (LMS) designed for mastering modern .NET development. Built with ASP.NET Core MVC (.NET 10) and Entity Framework Core, the project serves as both an interactive learning environment for students and a reference architectural blueprint for modern .NET application design.

---

## Overview

NET-Tutos provides structured learning pathways, self-paced tutorials, real-time interactive quizzes, code cheat sheets, and personal progress tracking. It adheres to clean architecture principles, modern dependency injection lifetimes, repository and service abstractions, and robust data persistence with zero-configuration automated seeding.

---

## Key Features

### 1. Account and Student Progress Management (LMS)
- Integrated ASP.NET Core Identity authentication system with secure cookie-based session management.
- Student profile dashboard displaying completed lessons, total experience points (XP), and progress bars categorized by topic.
- Dynamic lesson progress tracking with instantaneous AJAX completion toggling and reward mechanics (+20 XP per completed lesson).
- Automatic course enrollment tracking and completion rate calculations.

### 2. Structured Learning Roadmap
- Four comprehensive stages designed to transition developers from fundamentals to enterprise-grade proficiency:
  - Stage 1: C# Fundamentals and Syntax (Types, Control Flow, Collections, Memory basics).
  - Stage 2: Object-Oriented Programming and Advanced C# (Inheritance, Interfaces, Generics, LINQ, Async/Await).
  - Stage 3: Entity Framework Core and Data Persistence (Code-First modeling, Migrations, Change Tracker, AsNoTracking optimization).
  - Stage 4: ASP.NET Core MVC and Web APIs (Middleware pipeline, Controllers, Razor engine, Dependency Injection lifetimes, SOLID principles).

### 3. Reading and Tutorial Experience
- Full Markdown rendering with Markdig support for tables, blockquotes, code blocks, and structured callouts.
- Code syntax highlighting powered by Prism.js with VS Code dark theme styling.
- One-click copy-to-clipboard functionality for code snippets.
- Real-time reading progress indicator bar.
- Seamless previous and next lesson navigation.

### 4. Interactive Quiz Assessment Engine
- Topic-specific and overall difficulty-tiered quizzes (Beginner, Intermediate, Advanced).
- Instant client-side and server-side validation with score computation.
- Comprehensive explanations for every question and answer choice.

### 5. Code Syntax Reference (Cheat Sheet)
- Quick lookup directory containing standardized snippets for C#, LINQ operators, EF Core patterns, and ASP.NET Core idioms.

### 6. Modern Responsive UI
- Fully responsive design engineered with Bootstrap 5 and customized modern typography.
- Native Light and Dark theme switcher with persistent client preference storage.

---

## Technical Stack

- Framework: ASP.NET Core MVC (.NET 10)
- Language: C# 14
- ORM: Entity Framework Core 10
- Authentication: ASP.NET Core Identity
- Markdown Engine: Markdig
- Client Libraries: Bootstrap 5, Bootstrap Icons, Prism.js
- Database Support: Microsoft SQL Server (LocalDB) and SQLite

---

## Architecture and Project Structure

```
.ASPNET-Tutos/
├── NET-Tutos.slnx                      # Solution definition (.NET 10 format)
├── .gitattributes                      # Repository metadata and Linguist rules
├── README.md                           # Project documentation
└── NET-Tutos/                          # Core ASP.NET Core web application
    ├── NET-Tutos.csproj                # Project configuration and package references
    ├── Controllers/                    # MVC Controllers
    │   ├── AccountController.cs        # Authentication, profile dashboard, and lesson toggling
    │   ├── CheatSheetController.cs     # Code reference explorer
    │   ├── HomeController.cs           # Landing page, overview, and metrics
    │   ├── QuizController.cs           # Quiz taking and evaluation engine
    │   ├── RoadmapController.cs        # Learning roadmap pathways
    │   └── TutorialsController.cs      # Catalog browsing, lesson details, search
    ├── Data/                           # Data access and persistence layer
    │   ├── AppDbContext.cs             # Identity and application database context
    │   └── DbInitializer.cs            # Automated schema setup and seed data
    ├── Models/                         # Domain models, entities, and viewmodels
    │   ├── Entities/                   # ApplicationUser, Category, Tutorial, QuizQuestion, etc.
    │   └── ViewModels/                 # Presentation viewmodels
    ├── Services/                       # Application business logic layer
    │   ├── ILearningProgressService.cs # Student progress and XP calculation service
    │   ├── IMarkdownService.cs         # Markdown transformation service
    │   └── ITutorialService.cs         # Content retrieval and search service
    ├── Views/                          # Razor views (.cshtml)
    │   ├── Account/                    # Login, Register, Profile dashboard
    │   ├── CheatSheet/                 # Snippet catalog
    │   ├── Home/                       # Index, About
    │   ├── Quiz/                       # Quiz index and interactive test view
    │   ├── Roadmap/                    # Roadmap visualization
    │   ├── Shared/                     # Layout, navigation, footer
    │   └── Tutorials/                  # Lesson listing and reader view
    ├── wwwroot/                        # Static client assets (CSS, JS, images, icons)
    ├── appsettings.json                # Environment and database configuration
    └── Program.cs                      # Dependency injection, middleware pipeline, and host bootstrapping
```

---

## Getting Started

### Prerequisites

- .NET 10 SDK (or .NET 8.0+ SDK)
- Any modern web browser
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

4. Open your browser and navigate to the local server address displayed in the terminal (typically `http://localhost:5262`).

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
- **Automated Seeding**: On first run, the database schema and a complete curriculum of tutorials, categories, and quizzes are automatically provisioned without requiring manual migration scripts.

---

## License

This project is licensed under the MIT License. See the LICENSE file for details.
