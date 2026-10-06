# NET-Tutos

[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?logo=dotnet)](https://learn.microsoft.com/aspnet/core)
[![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-Android-512BD4?logo=dotnet)](https://learn.microsoft.com/dotnet/maui/)
[![C# 14](https://img.shields.io/badge/C%23-14.0-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-10.0-512BD4)](https://learn.microsoft.com/ef/core/)
[![Real-Time](https://img.shields.io/badge/SignalR-Real--Time-blue)](https://learn.microsoft.com/aspnet/core/signalr)
[![PWA Ready](https://img.shields.io/badge/PWA-Offline_Enabled-success)](https://web.dev/progressive-web-apps/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**NET-Tutos** is an enterprise-grade, full-stack educational ecosystem and Learning Management System (LMS) designed for mastering modern .NET development. Engineered on **.NET 10**, the solution pairs a high-performance **ASP.NET Core MVC** web application with a cross-platform **.NET MAUI** mobile app, backed by a unified RESTful API, Roslyn sandboxed code evaluation, real-time SignalR networking, and dual-provider database persistence.

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
  - [1. Comprehensive Curriculum & Markdown Tutorials](#1-comprehensive-curriculum--markdown-tutorials)
  - [2. Interactive C# Playground & Online Judge (Roslyn)](#2-interactive-c-playground--online-judge-roslyn)
  - [3. Cross-Platform .NET MAUI Mobile Application](#3-cross-platform-net-maui-mobile-application)
  - [4. Timed Examinations, Vector PDF Certificates & QR Verification](#4-timed-examinations-vector-pdf-certificates--qr-verification)
  - [5. Real-Time SignalR Community Discussion & Q&A](#5-real-time-signalr-community-discussion--qa)
  - [6. Gamification: Flame Streaks, XP, Badges & Leaderboard](#6-gamification-flame-streaks-xp-badges--leaderboard)
  - [7. Technical Interview Preparation & Mock Interviews](#7-technical-interview-preparation--mock-interviews)
  - [8. Capstone Project Studio & Public Developer Portfolios](#8-capstone-project-studio--public-developer-portfolios)
  - [9. Offline-First PWA & Universal Command Palette (Ctrl+K)](#9-offline-first-pwa--universal-command-palette-ctrlk)
  - [10. Administrative LMS Control Center & CMS Studio](#10-administrative-lms-control-center--cms-studio)
  - [11. Bilingual Localization (English & Tiếng Việt)](#11-bilingual-localization-english--tiếng-việt)
- [Architecture & Solution Structure](#architecture--solution-structure)
- [Mobile REST API Endpoints](#mobile-rest-api-endpoints)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running the Web Application](#running-the-web-application)
  - [Running the Mobile Application](#running-the-mobile-application)
  - [Pre-Seeded Demo Accounts](#pre-seeded-demo-accounts)
- [Database Configuration & Resilient Fallback](#database-configuration--resilient-fallback)
- [License](#license)

---

## Overview

NET-Tutos delivers an end-to-end learning environment for developers transitioning from C# fundamentals to cloud-scale enterprise architectures. It combines self-paced reading with active coding challenges, real-time peer collaboration, structured practice quizzes, and formal certification.

The system is organized into a clean .NET 10 solution (`NET-Tutos.slnx`) containing:
- **`NET-Tutos.WebApp`**: The flagship web application featuring MVC presentation, CMS authoring tools, Roslyn code compilation, SignalR hubs, and Mobile REST APIs.
- **`NET-Tutos.Mobile`**: A native Android client built with .NET MAUI and the MVVM architecture pattern, providing seamless offline/online learning on the go.

---

## Key Features

### 1. Comprehensive Curriculum & Markdown Tutorials
- **4-Stage Mastery Roadmap**: Fundamentals & C# Syntax ➔ Object-Oriented Programming & LINQ ➔ Entity Framework Core & Data Modeling ➔ ASP.NET Core MVC & Enterprise Web APIs.
- **Rich Markdown Engine**: Markdig-powered lesson renderer supporting GFM tables, alerts, callouts, and code blocks.
- **Syntax Highlighting**: Integrated Prism.js with dark theme styling, line highlights, and instant copy-to-clipboard.
- **Reading Progress Bar**: Dynamic scroll-based progress indicator and lesson-to-lesson stepper navigation.
- **Interactive Checkpoints**: Lesson completion toggle (+20 XP) with instant AJAX state persistence.

### 2. Interactive C# Playground & Online Judge (Roslyn)
- **Embedded Monaco Editor**: VS Code's editor engine in the browser with code folding, autocomplete, and theme toggling.
- **Sandboxed Execution**: Powered by `Microsoft.CodeAnalysis.CSharp.Scripting` (Roslyn) executing safe C# scripts with timeout protection (4-second guardrail) and security filters.
- **Algorithm Challenge Catalog**: LeetCode-style problem set across Easy, Intermediate, and Advanced tiers with hidden unit tests, runtime measurement, and automated XP rewards.
- **Pre-Configured Starter Templates**: One-click code templates for Hello World, LINQ transforms, C# 14 pattern matching, async/await parallelism, and algorithms.

### 3. Cross-Platform .NET MAUI Mobile Application
- **Native Android Experience**: Optimized for smartphones and tablets using .NET MAUI and Android SDK 36.
- **MVVM Architecture**: Built using `CommunityToolkit.Mvvm` with reactive ViewModels, observable properties, and relay commands.
- **Real-Time Data Sync**: Synchronizes lessons, categories, roadmaps, study progress, and streaks directly with the backend API.
- **Token-Based Mobile Authentication**: Secure mobile login, registration, and persistent user session storage via MAUI Preferences.

### 4. Timed Examinations, Vector PDF Certificates & QR Verification
- **Automated Exam Engine**: Dynamic question pool shuffling, countdown timer, and instantaneous grading with comprehensive answer explanations.
- **Vector PDF Certificate Generation**: High-fidelity, print-ready digital certificates generated dynamically using **QuestPDF**.
- **Cryptographic QR Code Validation**: Embedded QR codes generated via **QRCoder** linking to a public verification endpoint (`/verify-certificate/{code}`).

### 5. Real-Time SignalR Community Discussion & Q&A
- **Bidirectional Lesson Discussions**: ASP.NET Core SignalR (`DiscussionHub`) broadcasting new questions and replies live to active learners.
- **Nested Threaded Comments**: Markdown-formatted replies, peer upvoting, and learner presence counters.
- **Accepted Solutions**: Question authors and administrators can mark comments as "Accepted Solution", rewarding community contributors with bonus XP (+15 XP).

### 6. Gamification: Flame Streaks, XP, Badges & Leaderboard
- **Daily Check-In & Streak Engine**: Consecutive day tracking with flame animations, milestone notifications, and bonus experience points.
- **Hall of Fame Leaderboard**: Live global student rankings featuring top-3 podiums, gold/silver/bronze medallions, and detailed XP stats.
- **Achievement Badges**: Automated trigger-based badges (Newbie, Scholar, Algorithm Hunter, Algorithm Master, Certified, Community Hero).

### 7. Technical Interview Preparation & Mock Interviews
- **Curated Question Bank**: Senior-level and junior-level technical interview questions covering C#, CLR internals, EF Core query performance, dependency injection, and REST API design.
- **Interactive Flashcards**: 3D flip card animations with self-assessment rating (Needs Review vs. Mastered).
- **Mock Interview Simulator**: Timed evaluation sessions mimicking real technical interview rounds.

### 8. Capstone Project Studio & Public Developer Portfolios
- **Hands-On Capstone Submissions**: Students submit GitHub repository links and live demo URLs for practical graduation projects.
- **Admin Review & Grading**: Instructors review code submissions, assign grades, and provide feedback directly in the CMS.
- **Public Developer Showcase**: Shareable profile page (`/u/{username}`) showcasing completed milestones, earned badges, algorithm stats, and verified certificates.

### 9. Offline-First PWA & Universal Command Palette (Ctrl+K)
- **Progressive Web App**: Service Worker (`sw.js`) and manifest caching core app shell and offline fallbacks (`offline.html`).
- **Command Palette (`Ctrl + K`)**: Keyboard-driven command center for jumping to lessons, challenges, quizzes, and documentation in milliseconds.

### 10. Administrative LMS Control Center & CMS Studio
- **Role-Based Access Control**: Strict segregation between `Admin` and `Student` roles via ASP.NET Core Identity.
- **KPI Metrics Dashboard**: High-level platform analytics covering active users, pass rates, popular lessons, and completion percentages.
- **Split-Screen WYSIWYG Studio**: Real-time live Markdown editor for publishing and editing course content with image and syntax preview.
- **Question & User Management**: Full CRUD over question banks, quiz sets, student roles, and XP moderation.

### 11. Bilingual Localization (English & Tiếng Việt)
- **Native Dual-Language Support**: Complete UI, navigation, roadmap, lesson content, quizzes, and certificate templates localized in both English and Vietnamese.
- **One-Click Flag Switcher**: Cookie-backed language preference toggling instantly without page loss.

---

## Architecture & Solution Structure

The project follows clean architecture principles with distinct separation of concerns:

```text
d:\.ASPNET-Tutos\
├── NET-Tutos.slnx                      # Modern .NET 10 solution definition
├── .gitignore                          # Standard .NET gitignore with local script exclusions
├── README.md                           # Platform documentation
│
├── NET-Tutos.WebApp/                   # ASP.NET Core 10 MVC & Mobile Web API
│   ├── NET-Tutos.WebApp.csproj         # Web project dependencies & SDK targets
│   ├── Controllers/                    # MVC Controllers
│   │   ├── AccountController.cs        # Identity, student dashboard & progress
│   │   ├── Admin*.cs                   # LMS administrative CMS, quizzes, projects & users
│   │   ├── CertificateController.cs    # PDF generation & public QR verification
│   │   ├── DiscussionController.cs     # Real-time discussion API with SignalR
│   │   ├── ExamController.cs           # Online exams & automated grading
│   │   ├── InterviewController.cs      # Flashcards & mock interview simulator
│   │   ├── LeaderboardController.cs    # Rankings, podium & achievements
│   │   ├── PlaygroundController.cs     # Monaco editor & Roslyn judge harness
│   │   ├── ProfileController.cs        # Public developer portfolio showcase (/u/{username})
│   │   ├── ProjectController.cs        # Capstone project submission & reviews
│   │   ├── StreakController.cs         # Daily check-ins & streak tracking
│   │   ├── TutorialsController.cs      # Course curriculum & lesson reader
│   │   └── Api/                        # RESTful API for Mobile App
│   │       ├── MobileAuthController.cs # Mobile JWT/Bearer authentication
│   │       └── MobileTutorialsController.cs # Tutorials, categories & roadmap API
│   ├── Data/                           # Database contexts & seeders
│   │   ├── AppDbContext.cs             # Identity & EF Core DbContext
│   │   └── DbInitializer.cs            # Zero-config schema & sample curriculum seeder
│   ├── Hubs/                           # SignalR Hubs
│   │   └── DiscussionHub.cs            # Live learner presence & comments hub
│   ├── Models/                         # Domain entities & ViewModels
│   │   ├── Entities/                   # Tutorials, Quizzes, Challenges, Streaks, Badges
│   │   └── DTOs/Mobile/                # Data Transfer Objects for Mobile endpoints
│   ├── Services/                       # Business logic services
│   │   ├── CertificateService.cs       # QuestPDF vector document generator
│   │   ├── CodeExecutionService.cs     # Roslyn C# sandbox & test-case evaluator
│   │   ├── DiscussionService.cs        # Discussion persistence & upvoting
│   │   └── MobileAuthService.cs        # Mobile token validation & profile service
│   ├── Views/                          # Razor MVC views (.cshtml)
│   ├── wwwroot/                        # Static assets, PWA Service Worker & manifest
│   └── Program.cs                      # Application pipeline, DI configuration & middleware
│
└── NET-Tutos.Mobile/                   # Cross-Platform .NET MAUI 10 Mobile App
    ├── NET_Tutos.Mobile.csproj         # MAUI Android project configuration
    ├── AppShell.xaml                   # Flyout/Tab navigation shell
    ├── MauiProgram.cs                  # MAUI dependency injection builder
    ├── Models/                         # Client-side domain models
    ├── Services/                       # HTTP API client (ApiService.cs)
    ├── ViewModels/                     # MVVM ViewModels (Home, Login, Roadmap, Tutorials)
    └── Views/                          # XAML Pages (HomePage, LoginPage, RoadmapPage, etc.)
```

---

## Mobile REST API Endpoints

The WebApp exposes dedicated JSON endpoints under `/api/mobile/` consumed by the .NET MAUI application:

| Endpoint | Method | Auth | Description |
| :--- | :---: | :---: | :--- |
| `/api/mobile/auth/login` | `POST` | Public | Authenticates credentials and returns user profile & token |
| `/api/mobile/auth/register` | `POST` | Public | Registers a new student account |
| `/api/mobile/auth/profile` | `GET` | Required | Retrieves the authenticated student's profile & statistics |
| `/api/mobile/categories` | `GET` | Public | Returns curriculum categories with lesson counters |
| `/api/mobile/tutorials` | `GET` | Public | Retrieves paginated lessons with search and category filters |
| `/api/mobile/tutorials/{slug}` | `GET` | Public | Returns full lesson Markdown details and completion state |
| `/api/mobile/tutorials/{id}/complete` | `POST` | Required | Toggles lesson completion status and awards XP |
| `/api/mobile/roadmap` | `GET` | Public | Returns structured learning pathways & stage milestones |
| `/api/mobile/streak` | `GET` | Required | Returns current daily check-in streak and flame status |

---

## Technology Stack

| Layer | Technologies |
| :--- | :--- |
| **Backend Framework** | ASP.NET Core 10.0 (MVC + Minimal API endpoints) |
| **Language** | C# 14 |
| **Mobile Platform** | .NET MAUI 10.0 (Targeting Android 5.0+ / API 21-36) |
| **MVVM Framework** | `CommunityToolkit.Mvvm` 8.4 |
| **ORM / Data Access** | Entity Framework Core 10.0 |
| **Database Engines** | Microsoft SQL Server (LocalDB) / SQLite (Resilient fallback) |
| **Code Scripting** | `Microsoft.CodeAnalysis.CSharp.Scripting` 5.0 (Roslyn Engine) |
| **Real-Time Web** | ASP.NET Core SignalR |
| **PDF Generation** | QuestPDF 2026.9 |
| **QR Code Engine** | QRCoder 1.8 |
| **Markdown Parser** | Markdig 1.4 |
| **Web Frontend** | Bootstrap 5.3, Bootstrap Icons, Monaco Editor, Prism.js, PWA |

---

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or .NET 8.0+ SDK)
- Modern web browser (Chrome, Edge, Firefox)
- *(Optional for Mobile)* Android SDK (API 34+) and Android Emulator (e.g. Pixel 9a)

---

### Running the Web Application

1. **Clone the repository:**
   ```bash
   git clone https://github.com/KaitoDeus/NET-Tutos.git
   cd NET-Tutos
   ```

2. **Build the entire solution:**
   ```bash
   dotnet build NET-Tutos.slnx
   ```

3. **Launch the Web Application:**
   ```powershell
   dotnet run --project NET-Tutos.WebApp/NET-Tutos.WebApp.csproj
   ```

4. **Access the application:**
   Open your browser and navigate to:
   - **`http://localhost:5000`** or **`http://localhost:5262`**

---

### Running the Mobile Application

To run the .NET MAUI mobile app on an Android emulator:

1. **Ensure the WebApp is running on port 5000** (as the Android emulator connects to host via `10.0.2.2:5000`).
2. **Start your Android emulator** (e.g. via Android Studio Device Manager or `emulator -avd Pixel_9a`).
3. **Build and deploy the app:**
   ```powershell
   dotnet build NET-Tutos.Mobile/NET_Tutos.Mobile.csproj -t:Run -f net10.0-android
   ```

---

### Pre-Seeded Demo Accounts

The system automatically initializes and seeds default accounts upon first execution:

| Role | Username / Email | Password | Permissions |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@nettutos.com` | `AdminPassword@123` | Full access to LMS CMS studio, analytics, quiz manager, grading, and role moderation. |
| **Demo Student** | `student@nettutos.com` | `StudentPassword@123` | Pre-enrolled student with 350 XP, 5-day active streak, and progress history. |

---

## Database Configuration & Resilient Fallback

The application features a dual-provider database configuration in `appsettings.json`:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DotNetTutorialsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
    "SqliteConnection": "Data Source=dotnet_tutorials.db"
  }
}
```

### Automatic Resilient Fallback:
1. **Primary Provider**: Microsoft SQL Server LocalDB (`DefaultConnection`).
2. **Instant Fallback**: If SQL Server LocalDB is not installed or unreachable, the system automatically and seamlessly shifts to local **SQLite** (`dotnet_tutorials.db`).
3. **Zero Migration Overhead**: The database schema, roles, default users, quizzes, flashcards, challenges, and roadmaps are seeded on startup (`DbInitializer.cs`), allowing developers to clone and run instantly with zero manual SQL configuration.

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for complete details.
