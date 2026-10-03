using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public static class LocalizationHelper
{
    private static readonly Dictionary<string, (string NameEn, string DescEn)> CategoryTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp-co-ban"] = (
            "1. C# & .NET Fundamentals",
            "Get started with the .NET ecosystem, C# syntax, variables, data types, loops, and core data structures."
        ),
        ["oop-csharp-nang-cao"] = (
            "2. OOP & Advanced C#",
            "Master the 4 OOP pillars, Interfaces, Generics, LINQ, and async/await asynchronous programming."
        ),
        ["entity-framework-core"] = (
            "3. Entity Framework Core & Database",
            "Manage and manipulate databases with EF Core Code-First, Migrations, and performance optimization."
        ),
        ["aspnet-core-mvc-api"] = (
            "4. ASP.NET Core MVC & Web API",
            "Build complete Web Apps with MVC, RESTful Web APIs, Middleware, Dependency Injection, and Clean Architecture."
        )
    };

    private static readonly Dictionary<string, (string TitleEn, string SummaryEn)> TutorialTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong"] = (
            ".NET Ecosystem Overview and Environment Setup",
            "Introduction to .NET runtime, SDK, CLI, and installing modern developer tools like Visual Studio and VS Code."
        ),
        ["bien-kieu-du-lieu-va-toan-tu-csharp"] = (
            "Variables, Data Types, and Operators in C#",
            "Deep dive into value types, reference types, nullable types, and core operators in C# 13 / .NET 10."
        ),
        ["cau-truc-dieu-khien-if-else-switch-case"] = (
            "Control Flow: if-else and Pattern Matching switch",
            "Master branching logic with if-else and advanced modern C# switch expression pattern matching."
        ),
        ["vong-lap-for-while-do-while-foreach"] = (
            "Loops in C#: for, while, do-while, and foreach",
            "Iterate collections efficiently and learn loop control statements: break, continue, and span-based loops."
        ),
        ["mang-array-va-list-trong-csharp"] = (
            "Arrays and List<T> Collections in C#",
            "Working with fixed-size Arrays and dynamic generic List<T> collections with high performance."
        ),
        ["phuong-thuc-methods-va-nap-chong-overloading"] = (
            "Methods and Method Overloading in C#",
            "Clean method design: parameters, return types, optional/named parameters, and method overloading."
        ),
        ["lap-trinh-huong-doi-tuong-oop-class-object"] = (
            "Object-Oriented Programming (OOP): Class & Object",
            "Fundamental OOP principles: Class, Object, Constructors, and Access Modifiers."
        ),
        ["4-tinh-chat-oop-dong-goi-ke-thua-da-hinh-tru-tuong"] = (
            "The 4 Pillars of OOP: Encapsulation, Inheritance, Polymorphism, Abstraction",
            "In-depth breakdown of the four OOP core principles with real-world enterprise C# examples."
        ),
        ["interface-va-abstract-class-trong-csharp"] = (
            "Interface and Abstract Class: Choosing the Right Abstraction",
            "Compare Interfaces vs Abstract Classes and apply Dependency Inversion with loose coupling."
        ),
        ["generics-trong-csharp-toi-uu-tai-su-dung-code"] = (
            "Generics in C#: Writing Type-Safe Reusable Code",
            "Build type-safe generic classes and methods with generic constraints."
        ),
        ["delegate-event-va-lambda-expressions"] = (
            "Delegates, Events, and Lambda Expressions",
            "Master functional concepts in C#: Func, Action, Events, and concise Lambda expressions."
        ),
        ["linq-to-objects-truy-van-du-lieu-manh-me"] = (
            "LINQ to Objects: Modern In-Memory Data Querying",
            "Query and filter collections declaratively with Where, Select, GroupBy, and Joins."
        ),
        ["lap-trinh-bat-dong-bo-async-await-task"] = (
            "Asynchronous Programming: async, await, and Task",
            "Write non-blocking high-throughput code with async/await, Task, and CancellationToken."
        ),
        ["xu-ly-ngoai-le-exception-handling-csharp"] = (
            "Professional Exception Handling in C#",
            "Build robust applications with try-catch-finally, custom Exceptions, and defensive programming."
        ),
        ["tong-quan-entity-framework-core-code-first"] = (
            "Entity Framework Core Overview & Code-First Approach",
            "Introduction to modern ORM: architecture, database providers, and the Code-First paradigm."
        ),
        ["dbcontext-va-dbset-trai-tim-cua-ef-core"] = (
            "DbContext and DbSet: The Core Engine of EF Core",
            "Configure DbContext lifecycle, connection strings, and register entities with DbSet."
        ),
        ["ef-core-migrations-quan-ly-phien-ban-co-so-du-lieu"] = (
            "EF Core Migrations: Database Version Control",
            "Create, apply, and rollback database schema migrations in development and production."
        ),
        ["crud-operations-voi-ef-core"] = (
            "CRUD Operations with EF Core",
            "Execute Create, Read, Update, and Delete operations cleanly with change tracking."
        ),
        ["quan-he-giua-cac-thuc-the-relationships-ef-core"] = (
            "Entity Relationships in EF Core: 1-1, 1-N, N-N",
            "Model database foreign keys and navigation properties with Fluent API."
        ),
        ["linq-trong-ef-core-iqueryable-vs-ienumerable-asnotracking"] = (
            "LINQ in EF Core: IQueryable, AsNoTracking, and SQL Translation",
            "Optimize database queries: avoid N+1 problems, use AsNoTracking and projection."
        ),
        ["hieu-nang-ef-core-raw-sql-eager-loading-vs-lazy-loading"] = (
            "EF Core Performance: Raw SQL, Eager vs Split Queries",
            "Boost query speed: Include vs ThenInclude, AsSplitQuery, and FromSqlRaw."
        ),
        ["kien-truc-aspnet-core-program-middleware-pipeline"] = (
            "ASP.NET Core Architecture: Program.cs & Middleware Pipeline",
            "Understand how HTTP requests travel through ASP.NET Core middleware pipeline."
        ),
        ["dependency-injection-di-trong-aspnet-core"] = (
            "Dependency Injection (DI) in ASP.NET Core",
            "Master Transient, Scoped, and Singleton service lifetimes with IoC container."
        ),
        ["xay-dung-restful-web-api-voi-aspnet-core"] = (
            "Building RESTful Web APIs with ASP.NET Core",
            "Design clean RESTful APIs: HTTP methods, status codes, DTOs, and OpenAPI/Swagger documentation."
        ),
        ["aspnet-core-mvc-routing-controller-va-view"] = (
            "ASP.NET Core MVC: Routing, Controllers, and Views",
            "Understand MVC architectural pattern, Razor view rendering, and attribute routing."
        ),
        ["model-binding-validation-data-annotations"] = (
            "Model Binding, Validation, and Data Annotations",
            "Validate incoming HTTP requests with Data Annotations and ModelState handling."
        ),
        ["quan-ly-cau-hinh-appsettings-options-pattern"] = (
            "Configuration Management: appsettings.json & Options Pattern",
            "Load and bind strongly-typed application settings using IOptions<T>."
        ),
        ["xac-thuc-phan-quyen-identity-jwt"] = (
            "Authentication and Authorization with Identity & JWT",
            "Secure applications with ASP.NET Core Identity, cookie auth, JWT Bearer tokens, and Roles."
        ),
        ["clean-architecture-to-chuc-du-an-doanh-nghiep"] = (
            "Clean Architecture: Enterprise .NET Project Structure",
            "Organize scalable enterprise solutions: Domain, Application, Infrastructure, and Presentation layers."
        )
    };

    public static string GetTitle(this Category? cat, bool isEn)
    {
        if (cat == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(cat.Slug) && CategoryTranslations.TryGetValue(cat.Slug, out var trans))
        {
            return trans.NameEn;
        }
        return cat.Name ?? string.Empty;
    }

    public static string GetDesc(this Category? cat, bool isEn)
    {
        if (cat == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(cat.Slug) && CategoryTranslations.TryGetValue(cat.Slug, out var trans))
        {
            return trans.DescEn;
        }
        return cat.Description ?? string.Empty;
    }

    public static string GetTitle(this Tutorial? tut, bool isEn)
    {
        if (tut == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(tut.Slug) && TutorialTranslations.TryGetValue(tut.Slug, out var trans))
        {
            return trans.TitleEn;
        }
        return tut.Title ?? string.Empty;
    }

    public static string GetSummary(this Tutorial? tut, bool isEn)
    {
        if (tut == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(tut.Slug) && TutorialTranslations.TryGetValue(tut.Slug, out var trans))
        {
            return trans.SummaryEn;
        }
        return tut.Summary ?? string.Empty;
    }

    public static string GetDifficultyName(DifficultyLevel level, bool isEn)
    {
        return level switch
        {
            DifficultyLevel.Beginner => isEn ? "Beginner" : "Cơ bản",
            DifficultyLevel.Intermediate => isEn ? "Intermediate" : "Trung cấp",
            DifficultyLevel.Advanced => isEn ? "Advanced" : "Nâng cao",
            _ => isEn ? "Beginner" : "Cơ bản"
        };
    }

    public static bool IsEnglish(this Microsoft.AspNetCore.Http.HttpContext? context)
    {
        if (context == null) return false;
        var feature = context.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
        var culture = feature?.RequestCulture.UICulture.TwoLetterISOLanguageName ?? "vi";
        return culture.Equals("en", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetStepTitle(int stepNumber, bool isEn, string fallback)
    {
        if (!isEn) return fallback;
        return stepNumber switch
        {
            1 => "Phase 1: .NET Fundamentals & C# Syntax",
            2 => "Phase 2: Object-Oriented Programming (OOP) & Advanced C#",
            3 => "Phase 3: Database & Entity Framework Core",
            4 => "Phase 4: ASP.NET Core MVC & RESTful Web APIs",
            _ => fallback
        };
    }

    public static string GetStepSubtitle(int stepNumber, bool isEn, string fallback)
    {
        if (!isEn) return fallback;
        return stepNumber switch
        {
            1 => "Essential foundations for beginners",
            2 => "Professional software design mindset",
            3 => "Enterprise data management with Code-First",
            4 => "Job-ready .NET Backend Engineer",
            _ => fallback
        };
    }

    public static string GetStepDesc(int stepNumber, bool isEn, string fallback)
    {
        if (!isEn) return fallback;
        return stepNumber switch
        {
            1 => "Master the .NET runtime (CLR), C# language syntax, primitive types, control flows, loops, and basic collections.",
            2 => "Master the 4 OOP pillars, Interfaces, Generic Types, write powerful queries with LINQ, and execute non-blocking asynchronous code with Async/Await.",
            3 => "Build relational database schemas, connect SQL Server, run Migrations, execute CRUD operations, and optimize read speeds with AsNoTracking.",
            4 => "Design complete MVC Web Applications, build RESTful APIs with Clean Architecture, and master Dependency Injection service lifetimes.",
            _ => fallback
        };
    }

    public static List<string> GetStepKeyTopics(int stepNumber, bool isEn, List<string> fallback)
    {
        if (!isEn) return fallback;
        return stepNumber switch
        {
            1 => new List<string> { ".NET SDK & CLI", "Variables & Types", "Control Flows", "Array & List<T>", "Dictionary", "StringBuilder" },
            2 => new List<string> { "4 OOP Pillars", "Interface & Abstract Class", "Generics", "LINQ & Lambda", "Task & Async/Await" },
            3 => new List<string> { "Code-First Approach", "DbContext & DbSet", "EF Core Migrations", "CRUD Operations", "1-N & N-N Relationships" },
            4 => new List<string> { "Middleware Pipeline", "MVC Controllers & Razor", "RESTful Web API", "Dependency Injection", "SOLID Principles" },
            _ => fallback
        };
    }

    private static readonly Dictionary<string, (string TitleEn, string DescEn)> SnippetTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LINQ Filtering & Projection"] = (
            "LINQ Filtering & Projection",
            "Filter collections and project into DTO models with LINQ"
        ),
        ["Async/Await Call with Timeout & Cancellation"] = (
            "Async/Await Call with Timeout & Cancellation",
            "Safely execute asynchronous tasks with CancellationToken & Timeout"
        ),
        ["EF Core Code-First DbContext Registration"] = (
            "EF Core Code-First DbContext Registration",
            "Configure DbContext in Program.cs with ConnectionString & retry policy"
        ),
        ["ASP.NET Core RESTful Controller Template"] = (
            "ASP.NET Core RESTful Controller Template",
            "Standard template for CRUD API Controller in ASP.NET Core"
        )
    };

    public static string GetSnippetTitle(this CodeSnippet? snippet, bool isEn)
    {
        if (snippet == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(snippet.Title) && SnippetTranslations.TryGetValue(snippet.Title, out var trans))
        {
            return trans.TitleEn;
        }
        return snippet.Title ?? string.Empty;
    }

    public static string GetSnippetDesc(this CodeSnippet? snippet, bool isEn)
    {
        if (snippet == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(snippet.Title) && SnippetTranslations.TryGetValue(snippet.Title, out var trans))
        {
            return trans.DescEn;
        }
        return snippet.Description ?? string.Empty;
    }
}
