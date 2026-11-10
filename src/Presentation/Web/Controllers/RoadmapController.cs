using System.Security.Claims;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class RoadmapController : Controller
{
    private readonly ITutorialService _tutorialService;
    private readonly ILearningProgressService? _progressService;

    public RoadmapController(
        ITutorialService tutorialService,
        ILearningProgressService? progressService = null)
    {
        _tutorialService = tutorialService;
        _progressService = progressService;
    }

    public async Task<IActionResult> Index()
    {
        var allCategories = (await _tutorialService.GetCategoriesWithTutorialsAsync()).ToList();

        string? currentUserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        bool bypassLocks = User.IsInRole("Admin");
        var progression = _progressService != null
            ? await _progressService.GetStageProgressionAsync(currentUserId, bypassLocks)
            : null;

        string[] stageSlugs =
        [
            "csharp-co-ban",
            "oop-csharp-nang-cao",
            "csharp-dsa-leetcode",
            "design-patterns-architecture",
            "entity-framework-core",
            "aspnet-core-mvc-api",
            "microservices-distributed-systems",
            "performance-diagnostics-devops"
        ];

        var steps = new List<RoadmapStep>
        {
            new RoadmapStep
            {
                StepNumber = 1,
                Title = "Giai đoạn 1: Nền tảng .NET & C# Chuyên sâu",
                Subtitle = "Làm chủ CLR Runtime, Memory Stack vs Heap & Cú pháp C# 14",
                Description = "Hiểu sâu cơ chế thực thi của .NET CLR, JIT compiler, kiểu dữ liệu nguyên thủy, quản lý bộ nhớ Stack vs Heap, Nullable Reference Types, luồng điều khiển và xử lý ngoại lệ kiên cố.",
                Level = DifficultyLevel.Beginner,
                BadgeColor = "success",
                Icon = "bi-terminal",
                KeyTopics = new List<string> { ".NET 10 CLR & JIT", "Value vs Reference Types", "Stack vs Heap Memory", "Nullable References", "Methods & params", "Custom Exceptions" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "csharp-co-ban")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 2,
                Title = "Giai đoạn 2: Lập trình Hướng đối tượng (OOP) & Kỹ thuật C# Nâng cao",
                Subtitle = "Tư duy thiết kế phần mềm hướng đối tượng thực chiến & Bất đồng bộ",
                Description = "Làm chủ 4 trụ cột OOP, Interface Segregation, Generics Nâng cao, Delegate, Event, LINQ & Lambda internals và Lập trình Bất đồng bộ Async/Await, TPL song song.",
                Level = DifficultyLevel.Intermediate,
                BadgeColor = "info",
                Icon = "bi-layers",
                KeyTopics = new List<string> { "4 OOP Pillars", "Interface & Generics", "Delegates & Events", "LINQ Deferred Execution", "Async / Await Internals", "TPL & Channels", "Modern C# Records" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "oop-csharp-nang-cao")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 3,
                Title = "Giai đoạn 3: Cấu trúc Dữ liệu, Giải thuật & LeetCode với C#",
                Subtitle = "Rèn luyện tư duy thuật toán, Big-O & giải LeetCode tối ưu trong .NET",
                Description = "Chuyên đề độc lập phân tích độ phức tạp Big-O, cấu trúc dữ liệu kinh điển, Two Pointers, Sliding Window, Monotonic Stack, PriorityQueue, Cây nhị phân và Quy hoạch động.",
                Level = DifficultyLevel.Intermediate,
                BadgeColor = "secondary",
                Icon = "bi-diagram-3",
                KeyTopics = new List<string> { "Big-O Time & Space", "Two Pointers & Sliding Window", "Monotonic Stack", "PriorityQueue (Min-Heap)", "Binary Tree BFS & DFS", "Dynamic Programming", "Dijkstra & DSU" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "csharp-dsa-leetcode")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 4,
                Title = "Giai đoạn 4: Design Patterns & Kiến trúc Phần mềm .NET",
                Subtitle = "Làm chủ GoF Patterns, Enterprise Data Patterns, CQRS & Clean Architecture",
                Description = "Áp dụng các mẫu thiết kế kinh điển (Creational, Structural, Behavioral), Enterprise Data Patterns (Repository, Unit of Work, Specification), CQRS với MediatR và Clean Architecture chuẩn DDD.",
                Level = DifficultyLevel.Advanced,
                BadgeColor = "primary",
                Icon = "bi-boxes",
                KeyTopics = new List<string> { "GoF Creational & Structural", "Strategy & Observer Patterns", "Repository & Unit of Work", "Specification Pattern", "CQRS & MediatR Pipeline", "Clean Architecture & DDD" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "design-patterns-architecture")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 5,
                Title = "Giai đoạn 5: Cơ sở Dữ liệu, EF Core & Dapper Hiệu năng cao",
                Subtitle = "Quản trị dữ liệu theo hướng Code-First, tối ưu truy vấn & Dapper Micro-ORM",
                Description = "Xây dựng mô hình dữ liệu quan hệ, Fluent API, tối ưu AsNoTracking, Split Query, giao dịch ACID, Optimistic Concurrency và kết hợp Dapper Micro-ORM cho hệ thống tải cao.",
                Level = DifficultyLevel.Intermediate,
                BadgeColor = "warning",
                Icon = "bi-database",
                KeyTopics = new List<string> { "EF Core Code-First", "Fluent API Relationships", "AsNoTracking & Split Query", "Transactions & Concurrency", "Hybrid Dapper Micro-ORM", "Interceptors & Multi-Tenancy" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "entity-framework-core")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 6,
                Title = "Giai đoạn 6: ASP.NET Core Web API, Minimal APIs & Enterprise Backend",
                Subtitle = "Xây dựng RESTful API chuẩn quốc tế & Bảo mật doanh nghiệp",
                Description = "Thiết kế Middleware Pipeline, Minimal APIs siêu tốc độ, Dependency Injection chuyên sâu, bảo mật xác thực JWT Bearer, Identity RBAC và Distributed Output Caching với Redis.",
                Level = DifficultyLevel.Advanced,
                BadgeColor = "danger",
                Icon = "bi-globe2",
                KeyTopics = new List<string> { "Kestrel & Middleware Pipeline", "RESTful API & ProblemDetails", "Dependency Injection Lifetimes", "High-Performance Minimal APIs", "JWT Bearer & Role-Based Auth", "Redis Caching & Serilog" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "aspnet-core-mvc-api")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 7,
                Title = "Giai đoạn 7: Microservices, Distributed Systems & Event-Driven (.NET 10)",
                Subtitle = "Kiến trúc hệ thống phân tán, API Gateway YARP, RabbitMQ, Kafka & gRPC",
                Description = "Xây dựng hệ sinh thái vi dịch vụ đám mây: Bounded Contexts, YARP Reverse Proxy Gateway, Event-Driven với RabbitMQ & Apache Kafka, MassTransit Outbox Pattern, gRPC và OpenTelemetry.",
                Level = DifficultyLevel.Advanced,
                BadgeColor = "dark",
                Icon = "bi-hdd-network",
                KeyTopics = new List<string> { "Microservices & Bounded Contexts", "YARP API Gateway", "RabbitMQ vs Kafka (Triệu Request)", "MassTransit & Outbox Pattern", "gRPC & Protobuf Streaming", "Polly Resilience & Circuit Breaker", "OpenTelemetry & Prometheus" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "microservices-distributed-systems")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 8,
                Title = "Giai đoạn 8: Tối ưu Hiệu năng CLR, Diagnostics & Cloud-Native DevOps",
                Subtitle = "Kỹ sư C# cấp cao: Zero-Allocation, BenchmarkDotNet, dotnet-dump & Kubernetes",
                Description = "Chinh phục đỉnh cao hiệu năng .NET: Phân vùng bộ nhớ CLR GC, lập trình Zero-Allocation với Span/Memory, đo lường vi mô BenchmarkDotNet, chẩn đoán Production dotnet-dump và triển khai Kubernetes Helm.",
                Level = DifficultyLevel.Advanced,
                BadgeColor = "info",
                Icon = "bi-cpu",
                KeyTopics = new List<string> { "CLR GC Gen 0/1/2 & LOH/POH", "Span<T>, Memory<T> & ArrayPool", "BenchmarkDotNet Profiling", "dotnet-dump & Memory Leaks", "Roslyn Source Generators", "Multi-Stage Docker & Helm Chart" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Slug == "performance-diagnostics-devops")?.Tutorials.OrderBy(t => t.OrderIndex).ToList() ?? new()
            }
        };

        foreach (var (step, idx) in steps.Select((s, i) => (s, i)))
        {
            var catSlug = idx < stageSlugs.Length ? stageSlugs[idx] : string.Empty;
            var cat = allCategories.FirstOrDefault(c => c.Slug == catSlug);
            if (cat != null)
            {
                step.CategoryId = cat.Id;
                step.CategorySlug = cat.Slug;
                step.TotalLessonsCount = cat.Tutorials.Count;

                if (progression != null && progression.StagesByCategoryId.TryGetValue(cat.Id, out var status))
                {
                    step.IsUnlocked = status.IsUnlocked;
                    step.IsCompleted = status.IsCompleted;
                    step.CompletedLessonsCount = status.CompletedCount;
                    step.TotalLessonsCount = status.TotalCount;
                    step.BlockingStageTitleVi = status.BlockingStageTitleVi;
                    step.BlockingStageTitleEn = status.BlockingStageTitleEn;
                    step.NextPrerequisiteTutorialSlug = status.NextPrerequisiteTutorialSlug;
                }
            }
        }

        var viewModel = new RoadmapViewModel
        {
            Steps = steps,
            IsAuthenticated = progression?.IsAuthenticated ?? false,
            BypassStageLocks = bypassLocks,
            CompletedTutorialIds = progression != null
                ? new HashSet<int>(progression.CompletedTutorialIds)
                : new HashSet<int>()
        };
        return View(viewModel);
    }
}
