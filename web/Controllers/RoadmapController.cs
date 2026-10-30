using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Microsoft.AspNetCore.Mvc;

namespace NET_Tutos.Controllers;

public class RoadmapController : Controller
{
    private readonly ITutorialService _tutorialService;

    public RoadmapController(ITutorialService tutorialService)
    {
        _tutorialService = tutorialService;
    }

    public async Task<IActionResult> Index()
    {
        var allCategories = await _tutorialService.GetCategoriesWithTutorialsAsync();

        var steps = new List<RoadmapStep>
        {
            new RoadmapStep
            {
                StepNumber = 1,
                Title = "Giai đoạn 1: Nền tảng .NET & Cú pháp C#",
                Subtitle = "Cơ bản cho người mới bắt đầu",
                Description = "Hiểu rõ về .NET runtime (CLR), cách viết code C#, kiểu dữ liệu, cấu trúc rẽ nhánh, vòng lặp và xử lý chuỗi / collections cơ bản.",
                Level = DifficultyLevel.Beginner,
                BadgeColor = "success",
                Icon = "bi-mortarboard",
                KeyTopics = new List<string> { ".NET SDK & CLI", "Variables & Types", "Control Flows", "Array & List<T>", "Dictionary", "StringBuilder" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Level == DifficultyLevel.Beginner)?.Tutorials.ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 2,
                Title = "Giai đoạn 2: Lập trình Hướng đối tượng (OOP) & C# Nâng cao",
                Subtitle = "Tư duy thiết kế phần mềm chuyên nghiệp",
                Description = "Làm chủ 4 trụ cột OOP, làm việc với Interface, Generic Types, viết truy vấn dữ liệu mạnh mẽ với LINQ và xử lý đa luồng non-blocking bằng Async/Await.",
                Level = DifficultyLevel.Intermediate,
                BadgeColor = "info",
                Icon = "bi-layers",
                KeyTopics = new List<string> { "4 Trụ cột OOP", "Interface & Abstract Class", "Generics", "LINQ & Lambda", "Task & Async/Await" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Level == DifficultyLevel.Intermediate)?.Tutorials.ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 3,
                Title = "Giai đoạn 3: Cơ sở dữ liệu & Entity Framework Core",
                Subtitle = "Quản trị dữ liệu theo hướng Code-First",
                Description = "Xây dựng mô hình dữ liệu quan hệ, kết nối SQL Server, thực thi Migrations, thao tác CRUD và tối ưu tốc độ đọc dữ liệu với AsNoTracking.",
                Level = DifficultyLevel.Intermediate,
                BadgeColor = "warning",
                Icon = "bi-database",
                KeyTopics = new List<string> { "Code-First Approach", "DbContext & DbSet", "EF Core Migrations", "CRUD Operations", "Relationship 1-N & N-N" },
                RelatedTutorials = allCategories.Skip(2).FirstOrDefault()?.Tutorials.ToList() ?? new()
            },
            new RoadmapStep
            {
                StepNumber = 4,
                Title = "Giai đoạn 4: Lập trình Web ASP.NET Core MVC & RESTful API",
                Subtitle = "Sẵn sàng ứng tuyển lập trình viên .NET",
                Description = "Thiết kế Web App MVC hoàn chỉnh, xây dựng RESTful API chuẩn quốc tế, Dependency Injection (DI) với 3 vòng đời và ứng dụng nguyên lý SOLID.",
                Level = DifficultyLevel.Advanced,
                BadgeColor = "primary",
                Icon = "bi-trophy",
                KeyTopics = new List<string> { "Middleware Pipeline", "MVC Controllers & Razor", "RESTful Web API", "Dependency Injection", "SOLID Principles" },
                RelatedTutorials = allCategories.FirstOrDefault(c => c.Level == DifficultyLevel.Advanced)?.Tutorials.ToList() ?? new()
            }
        };

        var viewModel = new RoadmapViewModel { Steps = steps };
        return View(viewModel);
    }
}

