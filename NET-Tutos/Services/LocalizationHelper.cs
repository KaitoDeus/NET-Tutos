using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public static class LocalizationHelper
{
    public static readonly Dictionary<string, string> ViToEnTutorialSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong"] = "dotnet-ecosystem-overview-and-environment-setup",
        ["cu-phap-csharp-nen-tang-bien-kieu-du-lieu-luong-dieu-khien"] = "csharp-core-syntax-variables-data-types-control-flow",
        ["cau-truc-du-lieu-xu-ly-chuoi-list-dictionary-stringbuilder"] = "data-structures-and-strings-list-dictionary-stringbuilder",
        ["4-tru-cot-lap-trinh-huong-doi-tuong-oop-trong-csharp"] = "4-pillars-of-object-oriented-programming-in-csharp",
        ["interface-va-generics-trong-csharp-hien-dai"] = "interfaces-and-generics-in-modern-csharp",
        ["linq-language-integrated-query-va-lambda-expressions"] = "linq-and-lambda-expressions-in-csharp",
        ["lap-trinh-bat-dong-bo-async-await-trong-dotnet"] = "asynchronous-programming-async-await-in-dotnet",
        ["entity-framework-core-code-first-dbcontext-migrations"] = "ef-core-code-first-dbcontext-and-migrations",
        ["kien-truc-aspnet-core-mvc-programcs-middleware-controller"] = "aspnet-core-mvc-architecture-pipeline-controllers",
        ["xay-dung-restful-web-api-chuan-voi-aspnet-core"] = "building-restful-web-apis-with-aspnet-core",
        ["dependency-injection-va-nguyen-ly-solid-trong-aspnet-core"] = "dependency-injection-and-solid-in-aspnet-core"
    };

    public static readonly Dictionary<string, string> ViToEnProjectSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["xay-dung-restful-web-api-quan-ly-thu-vien-sach"] = "restful-web-api-book-library-management",
        ["he-thong-dat-hang-mini-clean-architecture-cqrs"] = "mini-ordering-clean-architecture-cqrs",
        ["nen-tang-dau-gia-truc-tuyen-realtime-signalr"] = "realtime-online-auction-signalr"
    };

    public static readonly Dictionary<string, string> ViToEnCategorySlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp-co-ban"] = "csharp-fundamentals",
        ["oop-csharp-nang-cao"] = "oop-advanced-csharp",
        ["entity-framework-core"] = "entity-framework-core",
        ["aspnet-core-mvc-api"] = "aspnet-core-mvc-api"
    };

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
        // Actual Seeded Tutorials in Database
        ["tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong"] = (
            "Lesson 1: .NET Ecosystem Overview & Environment Setup",
            "Explore the history of .NET Framework vs .NET Core vs modern .NET (.NET 8/9/10), CLR architecture, BCL, and setting up the C# dev environment."
        ),
        ["cu-phap-csharp-nen-tang-bien-kieu-du-lieu-luong-dieu-khien"] = (
            "Lesson 2: Core C# Syntax – Variables, Data Types & Control Flow",
            "Master primitive data types, var keyword, nullable types, if-else statements, switch expressions, and loops in modern C#."
        ),
        ["cau-truc-du-lieu-xu-ly-chuoi-list-dictionary-stringbuilder"] = (
            "Lesson 3: Data Structures & Strings – List, Dictionary, Array & StringBuilder",
            "Learn fixed-size arrays, dynamic generic List<T>, Dictionary<TKey, TValue>, and high-performance string concatenation using StringBuilder."
        ),
        ["4-tru-cot-lap-trinh-huong-doi-tuong-oop-trong-csharp"] = (
            "Lesson 4: The 4 Pillars of Object-Oriented Programming (OOP) in C#",
            "Deep dive into Encapsulation, Inheritance, Polymorphism, and Abstraction through real-world enterprise code examples."
        ),
        ["interface-va-generics-trong-csharp-hien-dai"] = (
            "Lesson 5: Interfaces & Generics in Modern C#",
            "Understand Interface contracts, solve multiple behavior inheritance, and leverage Generics (T) to build flexible, type-safe, and high-performance components."
        ),
        ["linq-language-integrated-query-va-lambda-expressions"] = (
            "Lesson 6: LINQ (Language Integrated Query) & Lambda Expressions",
            "Master essential LINQ operators: Where, Select, OrderBy, GroupBy, Any, All, FirstOrDefault, Sum, Count, and understand Deferred Execution."
        ),
        ["lap-trinh-bat-dong-bo-async-await-trong-dotnet"] = (
            "Lesson 7: Asynchronous Programming (Async / Await) in .NET",
            "Understand Non-blocking I/O mechanisms, how Tasks work, async State Machine internals, and golden rules to avoid deadlocks."
        ),
        ["entity-framework-core-code-first-dbcontext-migrations"] = (
            "Lesson 8: Entity Framework Core – Code-First, DbContext & Migrations",
            "Step-by-step guide to building ORM with EF Core: Designing entities, configuring DbContext, creating, and applying migrations to database."
        ),
        ["kien-truc-aspnet-core-mvc-programcs-middleware-controller"] = (
            "Lesson 9: ASP.NET Core MVC Architecture – Program.cs, Middleware & Controller",
            "Explore HTTP request lifecycle in ASP.NET Core: From Kestrel Web Server through Middleware Pipeline to MVC Routing and Razor Views."
        ),
        ["xay-dung-restful-web-api-chuan-voi-aspnet-core"] = (
            "Lesson 10: Building Standard RESTful Web APIs with ASP.NET Core",
            "Design enterprise APIs adhering to REST standards: HTTP Methods (GET, POST, PUT, DELETE), Status Codes, Model Validation, and Swagger/OpenAPI."
        ),
        ["dependency-injection-va-nguyen-ly-solid-trong-aspnet-core"] = (
            "Lesson 11: Dependency Injection (DI) & SOLID Principles in ASP.NET Core",
            "Differentiate 3 Service Lifetimes (Transient, Scoped, Singleton) in .NET IoC container and apply the 5 SOLID principles to real-world code."
        ),

        // Legacy / Additional Slugs Support
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

    private static readonly Dictionary<string, (string TitleEn, string SummaryEn)> TutorialByTitleTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bài 1: Tổng quan hệ sinh thái .NET & Cài đặt môi trường"] = (
            "Lesson 1: .NET Ecosystem Overview & Environment Setup",
            "Explore the history of .NET Framework vs .NET Core vs modern .NET (.NET 8/9/10), CLR architecture, BCL, and setting up the C# dev environment."
        ),
        ["Bài 2: Cú pháp C# nền tảng - Biến, Kiểu dữ liệu & Luồng điều khiển"] = (
            "Lesson 2: Core C# Syntax – Variables, Data Types & Control Flow",
            "Master primitive data types, var keyword, nullable types, if-else statements, switch expressions, and loops in modern C#."
        ),
        ["Bài 3: Cấu trúc dữ liệu & Xử lý Chuỗi - List, Dictionary, Array & StringBuilder"] = (
            "Lesson 3: Data Structures & Strings – List, Dictionary, Array & StringBuilder",
            "Learn fixed-size arrays, dynamic generic List<T>, Dictionary<TKey, TValue>, and high-performance string concatenation using StringBuilder."
        ),
        ["Bài 4: 4 Trụ cột Lập trình Hướng đối tượng (OOP) trong C#"] = (
            "Lesson 4: The 4 Pillars of Object-Oriented Programming (OOP) in C#",
            "Deep dive into Encapsulation, Inheritance, Polymorphism, and Abstraction through real-world enterprise code examples."
        ),
        ["Bài 5: Interface & Generics trong C# Hiện đại"] = (
            "Lesson 5: Interfaces & Generics in Modern C#",
            "Understand Interface contracts, solve multiple behavior inheritance, and leverage Generics (T) to build flexible, type-safe, and high-performance components."
        ),
        ["Bài 6: LINQ (Language Integrated Query) & Lambda Expressions"] = (
            "Lesson 6: LINQ (Language Integrated Query) & Lambda Expressions",
            "Master essential LINQ operators: Where, Select, OrderBy, GroupBy, Any, All, FirstOrDefault, Sum, Count, and understand Deferred Execution."
        ),
        ["Bài 7: Lập trình Bất đồng bộ (Async / Await) trong .NET"] = (
            "Lesson 7: Asynchronous Programming (Async / Await) in .NET",
            "Understand Non-blocking I/O mechanisms, how Tasks work, async State Machine internals, and golden rules to avoid deadlocks."
        ),
        ["Bài 8: Entity Framework Core - Code-First, DbContext & Migrations"] = (
            "Lesson 8: Entity Framework Core – Code-First, DbContext & Migrations",
            "Step-by-step guide to building ORM with EF Core: Designing entities, configuring DbContext, creating, and applying migrations to database."
        ),
        ["Bài 9: Kiến trúc ASP.NET Core MVC - Program.cs, Middleware & Controller"] = (
            "Lesson 9: ASP.NET Core MVC Architecture – Program.cs, Middleware & Controller",
            "Explore HTTP request lifecycle in ASP.NET Core: From Kestrel Web Server through Middleware Pipeline to MVC Routing and Razor Views."
        ),
        ["Bài 10: Xây dựng RESTful Web API chuẩn quốc tế với ASP.NET Core"] = (
            "Lesson 10: Building Standard RESTful Web APIs with ASP.NET Core",
            "Design enterprise APIs adhering to REST standards: HTTP Methods (GET, POST, PUT, DELETE), Status Codes, Model Validation, and Swagger/OpenAPI."
        ),
        ["Bài 11: Dependency Injection (DI) & Nguyên lý SOLID trong ASP.NET Core"] = (
            "Lesson 11: Dependency Injection (DI) & SOLID Principles in ASP.NET Core",
            "Differentiate 3 Service Lifetimes (Transient, Scoped, Singleton) in .NET IoC container and apply the 5 SOLID principles to real-world code."
        )
    };

    public static string GetCanonicalSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return string.Empty;

        foreach (var kvp in ViToEnTutorialSlugs)
        {
            if (kvp.Value.Equals(slug, StringComparison.OrdinalIgnoreCase))
                return kvp.Key;
        }

        foreach (var kvp in ViToEnProjectSlugs)
        {
            if (kvp.Value.Equals(slug, StringComparison.OrdinalIgnoreCase))
                return kvp.Key;
        }

        foreach (var kvp in ViToEnCategorySlugs)
        {
            if (kvp.Value.Equals(slug, StringComparison.OrdinalIgnoreCase))
                return kvp.Key;
        }

        return slug;
    }

    public static string GetSlug(this Tutorial? tut, bool isEn)
    {
        if (tut == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(tut.Slug) && ViToEnTutorialSlugs.TryGetValue(tut.Slug, out var enSlug))
        {
            return enSlug;
        }
        return tut.Slug ?? string.Empty;
    }

    public static string GetSlug(this CapstoneProject? project, bool isEn)
    {
        if (project == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(project.Slug) && ViToEnProjectSlugs.TryGetValue(project.Slug, out var enSlug))
        {
            return enSlug;
        }
        return project.Slug ?? string.Empty;
    }

    public static string GetSlug(this ProjectCardViewModel? project, bool isEn)
    {
        if (project == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(project.Slug) && ViToEnProjectSlugs.TryGetValue(project.Slug, out var enSlug))
        {
            return enSlug;
        }
        return project.Slug ?? string.Empty;
    }

    public static string GetSlug(this Category? cat, bool isEn)
    {
        if (cat == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(cat.Slug) && ViToEnCategorySlugs.TryGetValue(cat.Slug, out var enSlug))
        {
            return enSlug;
        }
        return cat.Slug ?? string.Empty;
    }

    public static string GetTitle(this Category? cat, bool isEn)
    {
        if (cat == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(cat.Slug))
        {
            var canonical = GetCanonicalSlug(cat.Slug);
            if (CategoryTranslations.TryGetValue(canonical, out var trans) || CategoryTranslations.TryGetValue(cat.Slug, out trans))
            {
                return trans.NameEn;
            }
        }
        return cat.Name ?? string.Empty;
    }

    public static string GetDesc(this Category? cat, bool isEn)
    {
        if (cat == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(cat.Slug))
        {
            var canonical = GetCanonicalSlug(cat.Slug);
            if (CategoryTranslations.TryGetValue(canonical, out var trans) || CategoryTranslations.TryGetValue(cat.Slug, out trans))
            {
                return trans.DescEn;
            }
        }
        return cat.Description ?? string.Empty;
    }

    public static string GetTitle(this Tutorial? tut, bool isEn)
    {
        if (tut == null) return string.Empty;
        if (!isEn) return tut.Title ?? string.Empty;

        if (!string.IsNullOrEmpty(tut.Slug))
        {
            var canonical = GetCanonicalSlug(tut.Slug);
            if (TutorialTranslations.TryGetValue(canonical, out var trans) || TutorialTranslations.TryGetValue(tut.Slug, out trans))
            {
                return trans.TitleEn;
            }
        }

        if (!string.IsNullOrEmpty(tut.Title))
        {
            var normalizedTitle = tut.Title.Replace('–', '-').Trim();
            if (TutorialByTitleTranslations.TryGetValue(normalizedTitle, out var transByTitle))
            {
                return transByTitle.TitleEn;
            }

            foreach (var kvp in TutorialByTitleTranslations)
            {
                var prefix = kvp.Key.Split(':')[0].Trim();
                if (tut.Title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.TitleEn;
                }
            }
        }

        return tut.Title ?? string.Empty;
    }

    public static string GetSummary(this Tutorial? tut, bool isEn)
    {
        if (tut == null) return string.Empty;
        if (!isEn) return tut.Summary ?? string.Empty;

        if (!string.IsNullOrEmpty(tut.Slug))
        {
            var canonical = GetCanonicalSlug(tut.Slug);
            if (TutorialTranslations.TryGetValue(canonical, out var trans) || TutorialTranslations.TryGetValue(tut.Slug, out trans))
            {
                return trans.SummaryEn;
            }
        }

        if (!string.IsNullOrEmpty(tut.Title))
        {
            var normalizedTitle = tut.Title.Replace('–', '-').Trim();
            if (TutorialByTitleTranslations.TryGetValue(normalizedTitle, out var transByTitle))
            {
                return transByTitle.SummaryEn;
            }

            foreach (var kvp in TutorialByTitleTranslations)
            {
                var prefix = kvp.Key.Split(':')[0].Trim();
                if (tut.Title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.SummaryEn;
                }
            }
        }

        return tut.Summary ?? string.Empty;
    }

    public static string GetContentMarkdown(this Tutorial? tut, bool isEn)
    {
        if (tut == null) return string.Empty;
        if (!isEn) return tut.ContentMarkdown ?? string.Empty;

        if (!string.IsNullOrEmpty(tut.Slug))
        {
            var canonical = GetCanonicalSlug(tut.Slug);
            var en = TutorialContentTranslations.GetContentMarkdownEn(canonical) ?? TutorialContentTranslations.GetContentMarkdownEn(tut.Slug);
            if (!string.IsNullOrEmpty(en)) return en;
        }

        return tut.ContentMarkdown ?? string.Empty;
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
        if (context == null)
        {
            return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);
        }

        if (context.Request.Query.TryGetValue("culture", out var queryCulture))
        {
            if (queryCulture.ToString().StartsWith("en", StringComparison.OrdinalIgnoreCase)) return true;
            if (queryCulture.ToString().StartsWith("vi", StringComparison.OrdinalIgnoreCase)) return false;
        }

        if (context.Request.Cookies.TryGetValue(Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName, out var rawCookieVal))
        {
            var cookieVal = System.Net.WebUtility.UrlDecode(rawCookieVal ?? string.Empty);
            if (!string.IsNullOrEmpty(cookieVal) && (cookieVal.Contains("uic=en", StringComparison.OrdinalIgnoreCase) || cookieVal.Contains("c=en", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            if (!string.IsNullOrEmpty(cookieVal) && (cookieVal.Contains("uic=vi", StringComparison.OrdinalIgnoreCase) || cookieVal.Contains("c=vi", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        var feature = context.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
        if (feature != null)
        {
            return feature.RequestCulture.UICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);
        }

        return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);
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

    private static readonly Dictionary<string, (string TitleEn, string ShortDescEn)> CapstoneProjectTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["xay-dung-restful-web-api-quan-ly-thu-vien-sach"] = (
            "Build RESTful Web API for Book Library Management",
            "Design and build a professional RESTful API using ASP.NET Core Web API and Entity Framework Core, applying DTOs, Repository Pattern, Dependency Injection, and Swagger UI."
        ),
        ["he-thong-dat-hang-mini-clean-architecture-cqrs"] = (
            "Mini Ordering & Payment System with Clean Architecture & CQRS",
            "Apply Clean Architecture (Domain-Driven Design), CQRS pattern with MediatR, FluentValidation, Unit Testing, and ACID transactions in ASP.NET Core."
        ),
        ["nen-tang-dau-gia-truc-tuyen-realtime-signalr"] = (
            "Real-time Online Auction Platform with ASP.NET Core & SignalR",
            "Build a multi-user Live Auction system: real-time countdown, instant bidding with SignalR, multi-client state synchronization, and Optimistic Concurrency Control."
        )
    };

    public static string GetTitle(this CapstoneProject? project, bool isEn) => project.GetProjectTitle(isEn);
    public static string GetTitle(this ProjectCardViewModel? project, bool isEn) => project.GetProjectTitle(isEn);

    public static string GetProjectTitle(this CapstoneProject? project, bool isEn)
    {
        if (project == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(project.Slug))
        {
            var canonical = GetCanonicalSlug(project.Slug);
            if (CapstoneProjectTranslations.TryGetValue(canonical, out var trans) || CapstoneProjectTranslations.TryGetValue(project.Slug, out trans))
            {
                return trans.TitleEn;
            }
        }
        return project.Title ?? string.Empty;
    }

    public static string GetProjectShortDesc(this CapstoneProject? project, bool isEn)
    {
        if (project == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(project.Slug))
        {
            var canonical = GetCanonicalSlug(project.Slug);
            if (CapstoneProjectTranslations.TryGetValue(canonical, out var trans) || CapstoneProjectTranslations.TryGetValue(project.Slug, out trans))
            {
                return trans.ShortDescEn;
            }
        }
        return project.ShortDescription ?? string.Empty;
    }

    public static string GetProjectTitle(this ProjectCardViewModel? project, bool isEn)
    {
        if (project == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(project.Slug))
        {
            var canonical = GetCanonicalSlug(project.Slug);
            if (CapstoneProjectTranslations.TryGetValue(canonical, out var trans) || CapstoneProjectTranslations.TryGetValue(project.Slug, out trans))
            {
                return trans.TitleEn;
            }
        }
        return project.Title ?? string.Empty;
    }

    public static string GetProjectShortDesc(this ProjectCardViewModel? project, bool isEn)
    {
        if (project == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(project.Slug))
        {
            var canonical = GetCanonicalSlug(project.Slug);
            if (CapstoneProjectTranslations.TryGetValue(canonical, out var trans) || CapstoneProjectTranslations.TryGetValue(project.Slug, out trans))
            {
                return trans.ShortDescEn;
            }
        }
        return project.ShortDescription ?? string.Empty;
    }

    private static readonly Dictionary<string, (string TitleEn, string DescEn, string CondEn)> BadgeTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NEWBIE"] = ("NET Rookie", "Begin your journey and complete your first theoretical lesson", "Complete any 1 lesson"),
        ["SCHOLAR"] = ("Diligent Scholar", "Persistently acquire knowledge and complete 5 or more lessons", "Complete 5 lessons"),
        ["CODER"] = ("Algorithm Hunter", "Execute and pass your first C# programming challenge", "Pass 1 algorithm challenge"),
        ["ALGO_MASTER"] = ("Algorithm Master", "Conquer 3 or more algorithm challenges in C# Sandbox", "Pass 3 algorithm challenges"),
        ["CERTIFIED"] = ("Certified Engineer", "Pass the final graduation exam and earn an official digital certificate", "Obtain course completion certificate"),
        ["COMMUNITY_HERO"] = ("Community Torchbearer", "Actively contribute solutions and help fellow learners", "Have an accepted best answer or submit 3 comments"),
        ["STREAK_3"] = ("Persistent Flame", "Maintain a 3-day continuous learning streak", "Reach a streak of 3 consecutive days"),
        ["STREAK_7"] = ("Disciplined Warrior", "Maintain a 7-day continuous learning streak without interruption", "Reach a streak of 7 consecutive days"),
        ["STREAK_30"] = ("Unstoppable Legend", "Record 30 days of continuous learning in .NET", "Reach a streak of 30 consecutive days"),
        ["ARCHITECT"] = (".NET Architect", "Successfully defend a Capstone Project and pass the review standard", "Have at least 1 Capstone Project approved")
    };

    public static string GetBadgeTitle(string? badgeCode, string fallback, bool isEn)
    {
        if (isEn && !string.IsNullOrEmpty(badgeCode) && BadgeTranslations.TryGetValue(badgeCode, out var trans))
            return trans.TitleEn;
        return fallback;
    }

    public static string GetBadgeDescription(string? badgeCode, string fallback, bool isEn)
    {
        if (isEn && !string.IsNullOrEmpty(badgeCode) && BadgeTranslations.TryGetValue(badgeCode, out var trans))
            return trans.DescEn;
        return fallback;
    }

    public static string GetBadgeCondition(string? badgeCode, string fallback, bool isEn)
    {
        if (isEn && !string.IsNullOrEmpty(badgeCode) && BadgeTranslations.TryGetValue(badgeCode, out var trans))
            return trans.CondEn;
        return fallback;
    }

    private static readonly Dictionary<string, (string TitleEn, string ShortDescEn, string InstructionsEn)> ChallengeTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tinh-tong-hai-so"] = (
            "1. Sum of Two Integers",
            "Get familiar with C# method syntax by writing a function to calculate the sum of two integers.",
            @"### Problem Statement
Write a method `Sum(int a, int b)` that takes two integers `a` and `b`. Return their sum.

#### Example 1:
- **Input**: `a = 5, b = 10`
- **Output**: `15`

#### Example 2:
- **Input**: `a = -3, b = 8`
- **Output**: `5`"
        ),
        ["dao-nguoc-chuoi"] = (
            "2. Reverse String",
            "Basic string manipulation in C# using character arrays or LINQ.",
            @"### Problem Statement
Write a method `ReverseString(string s)` that takes a string `s` and returns its reversed string.

#### Example 1:
- **Input**: `s = ""hello""`
- **Output**: `""olleh""`

#### Example 2:
- **Input**: `s = ""csharp""`
- **Output**: `""prahsc""`"
        ),
        ["loc-so-chan-linq"] = (
            "3. Filter & Sort Even Numbers with LINQ",
            "Use LINQ Where and OrderBy operators to process an array of integers.",
            @"### Problem Statement
Write a method `FilterEvens(int[] numbers)` that takes an integer array `numbers`. Use LINQ to filter out even numbers and sort them in ascending order.

#### Example 1:
- **Input**: `numbers = [1, 2, 3, 4, 5, 6]`
- **Output**: `2, 4, 6`

#### Example 2:
- **Input**: `numbers = [10, 3, 8, 1, 4]`
- **Output**: `4, 8, 10`"
        ),
        ["kiem-tra-so-nguyen-to"] = (
            "4. Prime Number Check",
            "Optimized O(sqrt(n)) primality test algorithm.",
            @"### Problem Statement
Write a method `IsPrime(int n)` to check whether integer $n$ is a prime number. Return `true` if prime, otherwise `false`.
*Note: A prime number is an integer greater than 1 that is divisible only by 1 and itself.*

#### Example 1:
- **Input**: `n = 7`
- **Output**: `True`

#### Example 2:
- **Input**: `n = 4`
- **Output**: `False`"
        ),
        ["tim-so-fibonacci"] = (
            "5. Find Nth Fibonacci Number",
            "Compute the nth Fibonacci number with optimal O(n) time complexity.",
            @"### Problem Statement
The Fibonacci sequence is defined as: $F(0) = 0$, $F(1) = 1$, and $F(n) = F(n-1) + F(n-2)$ for $n \ge 2$.
Write a method `Fibonacci(int n)` returning the $n$-th Fibonacci number.

#### Example 1:
- **Input**: `n = 6`
- **Output**: `8` (Sequence: 0, 1, 1, 2, 3, 5, 8)

#### Example 2:
- **Input**: `n = 10`
- **Output**: `55`"
        )
    };

    public static string GetChallengeTitle(this CodingChallenge? challenge, bool isEn)
    {
        if (challenge == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(challenge.Slug) && ChallengeTranslations.TryGetValue(challenge.Slug, out var trans))
        {
            return trans.TitleEn;
        }
        return challenge.Title ?? string.Empty;
    }

    public static string GetChallengeShortDesc(this CodingChallenge? challenge, bool isEn)
    {
        if (challenge == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(challenge.Slug) && ChallengeTranslations.TryGetValue(challenge.Slug, out var trans))
        {
            return trans.ShortDescEn;
        }
        return challenge.ShortDescription ?? string.Empty;
    }

    public static string GetChallengeInstructions(this CodingChallenge? challenge, bool isEn)
    {
        if (challenge == null) return string.Empty;
        if (isEn && !string.IsNullOrEmpty(challenge.Slug) && ChallengeTranslations.TryGetValue(challenge.Slug, out var trans))
        {
            return trans.InstructionsEn;
        }
        return challenge.InstructionsMarkdown ?? string.Empty;
    }

    public static string GetActivityTitle(this ActivityFeedItemViewModel? item, bool isEn)
    {
        if (item == null) return string.Empty;
        if (!isEn) return item.Title;

        var t = item.Title.Trim().ToLowerInvariant();
        if (t.Contains("hoàn thành bài học")) return "completed lesson";
        if (t.Contains("giải thành công thử thách thuật toán") || t.Contains("thử thách thuật toán")) return "solved algorithm challenge";
        if (t.Contains("mở khóa thành tích mới")) return "unlocked new achievement";
        if (t.Contains("mở khóa huy hiệu")) return "unlocked badge";
        if (t.Contains("chuỗi ngọn lửa") || t.Contains("chuỗi streak")) return "achieved a learning streak";
        if (t.Contains("hoàn thành xuất sắc đồ án") || t.Contains("hoàn thành đồ án")) return "completed capstone project";
        if (t.Contains("thi đạt kỳ thi")) return "passed graduation exam";
        if (t.Contains("nhận chứng chỉ số") || t.Contains("chứng chỉ số")) return "earned digital certificate";
        if (t.Contains("thảo luận")) return "joined discussion";

        return item.Type switch
        {
            ActivityType.LessonCompleted => "completed lesson",
            ActivityType.ChallengeSolved => "solved algorithm challenge",
            ActivityType.ExamPassed => "passed graduation exam",
            ActivityType.CertificateEarned => "earned digital certificate",
            ActivityType.StreakAchieved => "achieved a learning streak",
            ActivityType.BadgeEarned => "unlocked badge",
            ActivityType.DiscussionComment => "joined discussion",
            ActivityType.ProjectApproved => "completed capstone project",
            _ => "shared an update"
        };
    }

    public static string GetActivityTypeLabel(this ActivityFeedItemViewModel? item, bool isEn)
    {
        if (item == null) return string.Empty;
        if (!isEn) return item.TypeLabel;

        return item.Type switch
        {
            ActivityType.LessonCompleted => "Lesson",
            ActivityType.ChallengeSolved => "Algorithm",
            ActivityType.ExamPassed => "Exam",
            ActivityType.CertificateEarned => "Certificate",
            ActivityType.StreakAchieved => "Streak",
            ActivityType.BadgeEarned => "Badge",
            ActivityType.DiscussionComment => "Discussion",
            ActivityType.ProjectApproved => "Capstone",
            _ => "Activity"
        };
    }

    public static string GetActivityTimeAgo(this ActivityFeedItemViewModel? item, bool isEn)
    {
        if (item == null) return string.Empty;
        return NotificationService.FormatTimeAgo(item.CreatedAt, isEn);
    }

    public static string GetActivityDescription(this ActivityFeedItemViewModel? item, bool isEn)
    {
        if (item == null) return string.Empty;
        if (!isEn) return item.Description;

        var desc = item.Description ?? string.Empty;

        // 1. Challenge descriptions (e.g. "2. Đảo ngược chuỗi ký tự (40 XP)", "1. Tính tổng hai số nguyên (30 XP)", "Hai Con Số (Two Sum)...")
        if (item.Type == ActivityType.ChallengeSolved || desc.Contains("Đảo ngược chuỗi") || desc.Contains("Tính tổng hai số") || desc.Contains("Fibonacci") || desc.Contains("số nguyên tố") || desc.Contains("LINQ"))
        {
            string xpSuffix = "";
            var matchXp = System.Text.RegularExpressions.Regex.Match(desc, @"\s*\(\d+\s*XP\)$");
            if (matchXp.Success)
            {
                xpSuffix = matchXp.Value;
            }

            if (desc.Contains("Tính tổng hai số")) return "1. Sum of Two Integers" + xpSuffix;
            if (desc.Contains("Đảo ngược chuỗi")) return "2. Reverse String" + xpSuffix;
            if (desc.Contains("Lọc và sắp xếp số chẵn")) return "3. Filter & Sort Even Numbers with LINQ" + xpSuffix;
            if (desc.Contains("số nguyên tố")) return "4. Prime Number Check" + xpSuffix;
            if (desc.Contains("Fibonacci")) return "5. Find Nth Fibonacci Number" + xpSuffix;
            if (desc.Contains("Hai Con Số") || desc.Contains("Two Sum")) return "Two Sum - C# Algorithmic Mastery" + xpSuffix;
        }

        // 2. Lesson descriptions (e.g. "Bài 7: Lập trình Bất đồng bộ (Async / Await) trong .NET", "Bài 6: LINQ...", "Bài 1: ...")
        if (item.Type == ActivityType.LessonCompleted || desc.StartsWith("Bài "))
        {
            if (desc.StartsWith("Bài 1:") || desc.Contains("Tổng quan hệ sinh thái"))
                return "Lesson 1: .NET Ecosystem Overview & Environment Setup";
            if (desc.StartsWith("Bài 2:") || desc.Contains("Cú pháp C# nền tảng"))
                return "Lesson 2: Core C# Syntax – Variables, Data Types & Control Flow";
            if (desc.StartsWith("Bài 3:") || desc.Contains("Cấu trúc dữ liệu"))
                return "Lesson 3: Data Structures & Strings – List, Dictionary, Array & StringBuilder";
            if (desc.StartsWith("Bài 4:") || desc.Contains("4 trụ cột"))
                return "Lesson 4: The 4 Pillars of Object-Oriented Programming (OOP) in C#";
            if (desc.StartsWith("Bài 5:") || desc.Contains("Interface và Generics"))
                return "Lesson 5: Interfaces & Generics in Modern C#";
            if (desc.StartsWith("Bài 6:") || desc.Contains("LINQ"))
                return "Lesson 6: LINQ (Language Integrated Query) & Lambda Expressions";
            if (desc.StartsWith("Bài 7:") || desc.Contains("Bất đồng bộ") || desc.Contains("Async"))
                return "Lesson 7: Asynchronous Programming (Async / Await) in .NET";
            if (desc.StartsWith("Bài 8:") || desc.Contains("Entity Framework Core"))
                return "Lesson 8: Entity Framework Core – Code-First, DbContext & Migrations";
            if (desc.StartsWith("Bài 9:") || desc.Contains("kiến trúc ASP.NET Core MVC") || desc.Contains("Program.cs"))
                return "Lesson 9: ASP.NET Core MVC Architecture – Program.cs, Middleware & Controller";
            if (desc.StartsWith("Bài 10:") || desc.Contains("RESTful Web API"))
                return "Lesson 10: Building Standard RESTful Web APIs with ASP.NET Core";
            if (desc.StartsWith("Bài 11:") || desc.Contains("Dependency Injection") || desc.Contains("SOLID"))
                return "Lesson 11: Dependency Injection & SOLID Principles in ASP.NET Core";
        }

        // 3. Badges (e.g. "Thợ Săn Thuật Toán C# 🏆", "Huyền Thoại Bất Bại (30 ngày kiên trì học tập)")
        if (item.Type == ActivityType.BadgeEarned || !string.IsNullOrEmpty(item.BadgeCode) || desc.Contains("Thợ Săn") || desc.Contains("Huyền Thoại"))
        {
            if (item.BadgeCode != null && BadgeTranslations.TryGetValue(item.BadgeCode, out var bTrans))
            {
                var trophy = desc.Contains("🏆") ? " 🏆" : "";
                return bTrans.TitleEn + trophy;
            }
            if (desc.Contains("Thợ Săn Thuật Toán C#")) return "C# Algorithm Hunter 🏆";
            if (desc.Contains("Huyền Thoại Bất Bại")) return "Unstoppable Legend (30-day streak) 🏆";
            if (desc.Contains("Tân Binh .NET")) return ".NET Rookie";
            if (desc.Contains("Học Giả Chăm Chỉ")) return "Diligent Scholar";
            if (desc.Contains("Bậc Thầy Thuật Toán")) return "Algorithm Master";
            if (desc.Contains("Kỹ Sư Đạt Chuẩn")) return "Certified Engineer";
            if (desc.Contains("Ngọn Đuốc Tri Thức")) return "Community Torchbearer";
            if (desc.Contains("Ngọn Lửa Bền Bỉ")) return "Persistent Flame";
            if (desc.Contains("Chiến Binh Kỷ Luật")) return "Disciplined Warrior";
            if (desc.Contains("Kiến Trúc Sư .NET")) return ".NET Architect";
        }

        // 4. Streak (e.g. "5 ngày học tập liên tục không ngắt quãng 🔥", "5 ngày kiên trì liên tục 🔥", "Chuỗi 5 ngày...")
        if (item.Type == ActivityType.StreakAchieved || desc.Contains("ngày kiên trì") || desc.Contains("ngày học tập liên tục") || desc.Contains("ngày học liên tục"))
        {
            var matchDays = System.Text.RegularExpressions.Regex.Match(desc, @"(\d+)\s*ngày");
            if (matchDays.Success)
            {
                int days = int.Parse(matchDays.Groups[1].Value);
                return $"{days}-day continuous learning streak 🔥";
            }
        }

        // 5. Capstone Project
        if (item.Type == ActivityType.ProjectApproved || desc.Contains("điểm") || desc.Contains("Đồ án") || desc.Contains("RESTful") || desc.Contains("SignalR") || desc.Contains("Clean Architecture"))
        {
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(desc, @"\((\d+(?:\.\d+)?)/100\s*điểm\)");
            string scoreSuffix = scoreMatch.Success ? $" ({scoreMatch.Groups[1].Value}/100 points) 🎯" : "";

            if (desc.Contains("Thư viện Sách")) return "RESTful Web API Book Library Management" + scoreSuffix;
            if (desc.Contains("Đặt hàng Mini") || desc.Contains("CQRS")) return "Mini Ordering System (Clean Architecture + CQRS)" + scoreSuffix;
            if (desc.Contains("Đấu giá Trực tuyến") || desc.Contains("SignalR")) return "Realtime Online Auction Platform with SignalR" + scoreSuffix;
        }

        // 6. Exam & Certificate
        if (item.Type == ActivityType.ExamPassed)
        {
            var m = System.Text.RegularExpressions.Regex.Match(desc, @"Đạt\s*(\d+)%");
            if (m.Success) return $"Scored {m.Groups[1].Value}% on course graduation exam";
            return "Passed the course graduation exam";
        }
        if (item.Type == ActivityType.CertificateEarned)
        {
            var m = System.Text.RegularExpressions.Regex.Match(desc, @"#([A-Za-z0-9\-]+)");
            if (m.Success) return $"Certificate of Completion .NET Developer #{m.Groups[1].Value}";
            return "Certificate of Completion .NET Developer";
        }

        return desc;
    }

    public static string GetActivityTargetUrl(this ActivityFeedItemViewModel? item, bool isEn)
    {
        if (item == null || string.IsNullOrEmpty(item.TargetUrl)) return string.Empty;
        if (!isEn) return item.TargetUrl;

        if (item.TargetUrl.StartsWith("/bai-hoc/"))
        {
            var slug = item.TargetUrl.Substring("/bai-hoc/".Length).Trim('/');
            if (ViToEnTutorialSlugs.TryGetValue(slug, out var enSlug))
            {
                return $"/tutorials/{enSlug}";
            }
        }
        return item.TargetUrl;
    }

    public static string GetNotificationTypeName(NotificationType type, bool isEn)
    {
        if (!isEn)
        {
            return type switch
            {
                NotificationType.BadgeEarned => "Huy hiệu mới",
                NotificationType.StreakReminder => "Chuỗi học tập",
                NotificationType.DiscussionReply => "Thảo luận",
                NotificationType.BestAnswer => "Giải pháp chính xác",
                NotificationType.ExamPassed => "Kỳ thi tốt nghiệp",
                NotificationType.CertificateIssued => "Chứng chỉ số",
                NotificationType.BonusXpAwarded => "Thưởng điểm XP",
                NotificationType.LessonCompleted => "Bài học",
                NotificationType.ChallengeSolved => "Thử thách C#",
                NotificationType.ProjectSubmitted => "Nộp đồ án",
                NotificationType.ProjectReviewed => "Đánh giá đồ án",
                _ => "Hệ thống"
            };
        }

        return type switch
        {
            NotificationType.BadgeEarned => "New Badge",
            NotificationType.StreakReminder => "Learning Streak",
            NotificationType.DiscussionReply => "Discussion Reply",
            NotificationType.BestAnswer => "Accepted Solution",
            NotificationType.ExamPassed => "Graduation Exam",
            NotificationType.CertificateIssued => "Digital Certificate",
            NotificationType.BonusXpAwarded => "Bonus XP",
            NotificationType.LessonCompleted => "Lesson",
            NotificationType.ChallengeSolved => "C# Challenge",
            NotificationType.ProjectSubmitted => "Capstone Submission",
            NotificationType.ProjectReviewed => "Capstone Review",
            _ => "System"
        };
    }

    public static string TranslateNotificationTitle(string? title, bool isEn)
    {
        if (string.IsNullOrWhiteSpace(title) || !isEn) return title ?? string.Empty;

        if (title.Contains("Điểm danh nhận thưởng thành công")) return "Check-in Reward Claimed! 🔥";
        if (title.Contains("Huy hiệu mới đã mở khóa")) return "New Badge Unlocked! 🏆";
        if (title.Contains("kỳ thi tốt nghiệp") || title.Contains("thi tốt nghiệp")) return "Graduation Exam Passed! 🎓";
        if (title.Contains("Chứng chỉ số") || title.Contains("chứng chỉ")) return "Digital Certificate Ready! 📜";
        if (title.Contains("trả lời thảo luận") || title.Contains("trả lời")) return "New Reply to Discussion";
        if (title.Contains("giải pháp chính xác")) return "Accepted Solution! 🌟";
        if (title.Contains("nộp đồ án")) return "Capstone Submitted Successfully! 🚀";
        if (title.Contains("kết quả đánh giá") || title.Contains("đánh giá")) return "Capstone Review Result Ready! 📝";
        if (title.Contains("thử thách C#") || title.Contains("thử thách")) return "C# Challenge Solved! ⚡";
        if (title.Contains("hoàn thành bài học")) return "Lesson Completed! 🎉";
        if (title.Contains("thưởng điểm") || title.Contains("thưởng XP")) return "Bonus XP Awarded! ⭐";

        return title;
    }

    public static string TranslateNotificationMessage(string? message, bool isEn)
    {
        if (string.IsNullOrWhiteSpace(message) || !isEn) return message ?? string.Empty;

        // Match: "Bạn đã duy trì chuỗi 1 ngày liên tiếp (+10 XP)!"
        var streakMatch = System.Text.RegularExpressions.Regex.Match(message, @"chuỗi\s+(\d+)\s+ngày\s+liên\s+tiếp\s+\(\+(\d+)\s+XP\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (streakMatch.Success)
        {
            var days = streakMatch.Groups[1].Value;
            var xp = streakMatch.Groups[2].Value;
            var dayLabel = days == "1" ? "day" : "days";
            return $"You maintained a streak of {days} consecutive {dayLabel} (+{xp} XP)!";
        }

        if (message.Contains("Mở khóa huy hiệu:"))
        {
            var replaced = message
                .Replace("Mở khóa huy hiệu:", "Badge unlocked:")
                .Replace("XP thưởng", "bonus XP")
                .Replace("Ngọn Lửa Bền Bỉ", "Persistent Flame")
                .Replace("Chiến Binh Kỷ Luật", "Disciplined Warrior")
                .Replace("Huyền Thoại Bất Bại", "Unstoppable Legend")
                .Replace("Thuật Toán Săn Bàn", "Algorithm Hunter")
                .Replace("Vua Thuật Toán", "Algorithm Master")
                .Replace("Kỹ Sư .NET", "Certified Engineer")
                .Replace("Học Giả Chăm Chỉ", "Diligent Scholar")
                .Replace("Tân Binh .NET", "NET Rookie")
                .Replace("Người Hùng Cộng Đồng", "Community Torchbearer")
                .Replace("Kiến Trúc Sư .NET", ".NET Architect");
            return replaced;
        }

        if (message.Contains("hoàn thành bài học"))
        {
            return System.Text.RegularExpressions.Regex.Replace(message, @"Chúc mừng bạn đã hoàn thành bài học\s*(.*)", "Congratulations on completing lesson $1");
        }

        if (message.Contains("thử thách"))
        {
            return System.Text.RegularExpressions.Regex.Replace(message, @"Chúc mừng bạn đã giải thành công thử thách\s*(.*)", "Congratulations on solving algorithm challenge $1");
        }

        return message;
    }
}
