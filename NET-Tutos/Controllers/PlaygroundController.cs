using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers;

public class PlaygroundController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICodeExecutionService _executionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStreakService _streakService;

    public PlaygroundController(
        AppDbContext context,
        ICodeExecutionService executionService,
        UserManager<ApplicationUser> userManager,
        IStreakService streakService)
    {
        _context = context;
        _executionService = executionService;
        _userManager = userManager;
        _streakService = streakService;
    }

    // GET: /Playground or /Playground/Challenge/{slug?}
    public async Task<IActionResult> Index(string? slug)
    {
        var templates = GetPresetTemplates();
        var challenges = await _context.CodingChallenges
            .Include(c => c.Category)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        CodingChallenge? activeChallenge = null;
        CodeSubmission? lastSubmission = null;

        if (!string.IsNullOrEmpty(slug))
        {
            activeChallenge = await _context.CodingChallenges
                .Include(c => c.Category)
                .Include(c => c.TestCases)
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        if (activeChallenge != null && User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                lastSubmission = await _context.CodeSubmissions
                    .Where(s => s.UserId == userId && s.CodingChallengeId == activeChallenge.Id)
                    .OrderByDescending(s => s.SubmittedAt)
                    .FirstOrDefaultAsync();
            }
        }

        var viewModel = new PlaygroundIndexViewModel
        {
            Templates = templates,
            Challenges = challenges,
            ActiveChallenge = activeChallenge,
            LastSubmission = lastSubmission
        };

        return View(viewModel);
    }

    // GET: /Playground/Challenges
    public async Task<IActionResult> Challenges(DifficultyLevel? difficulty, int? categoryId)
    {
        var query = _context.CodingChallenges
            .Include(c => c.Category)
            .Include(c => c.TestCases)
            .AsQueryable();

        if (difficulty.HasValue)
        {
            query = query.Where(c => c.Difficulty == difficulty.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        var challenges = await query
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.SelectedDifficulty = difficulty;
        ViewBag.SelectedCategory = categoryId;

        // Passed challenges for current user
        var passedChallengeIds = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                passedChallengeIds = (await _context.CodeSubmissions
                    .Where(s => s.UserId == userId && s.IsPassed)
                    .Select(s => s.CodingChallengeId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();
            }
        }

        ViewBag.PassedChallengeIds = passedChallengeIds;

        return View(challenges);
    }

    // POST: /Playground/Execute (AJAX)
    [HttpPost]
    public async Task<IActionResult> Execute([FromBody] CodeExecutionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = "Mã nguồn không được để trống."
            });
        }

        var result = await _executionService.ExecuteAsync(request.Code);
        return Ok(result);
    }

    // POST: /Playground/SubmitChallenge (AJAX)
    [HttpPost]
    public async Task<IActionResult> SubmitChallenge([FromBody] ChallengeSubmissionRequest request)
    {
        if (request == null || request.ChallengeId <= 0 || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new ChallengeSubmissionResponse
            {
                CompileError = "Dữ liệu nộp bài không hợp lệ."
            });
        }

        var challenge = await _context.CodingChallenges
            .Include(c => c.TestCases)
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId);

        if (challenge == null)
        {
            return NotFound(new ChallengeSubmissionResponse
            {
                CompileError = "Không tìm thấy thử thách lập trình này."
            });
        }

        var evaluation = await _executionService.EvaluateChallengeAsync(challenge, request.Code);

        // Record submission and handle XP if user is logged in
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                int xpEarned = 0;
                if (evaluation.AllPassed)
                {
                    // Check if already passed before
                    var alreadyPassed = await _context.CodeSubmissions
                        .AnyAsync(s => s.UserId == user.Id && s.CodingChallengeId == challenge.Id && s.IsPassed);

                    if (!alreadyPassed)
                    {
                        xpEarned = challenge.XpReward;
                        user.ExperiencePoints += xpEarned;
                        await _userManager.UpdateAsync(user);
                    }
                }

                var submission = new CodeSubmission
                {
                    UserId = user.Id,
                    CodingChallengeId = challenge.Id,
                    SubmittedCode = request.Code,
                    IsPassed = evaluation.AllPassed,
                    PassedTestsCount = evaluation.PassedTestsCount,
                    TotalTestsCount = evaluation.TotalTestsCount,
                    ExecutionTimeMs = evaluation.ExecutionTimeMs,
                    XpEarned = xpEarned,
                    SubmittedAt = DateTime.UtcNow
                };

                _context.CodeSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                if (evaluation.AllPassed)
                {
                    await _streakService.RecordLearningActivityStreakAsync(user.Id);
                }

                evaluation.XpEarned = xpEarned;
                evaluation.TotalUserXp = user.ExperiencePoints;
            }
        }

        return Ok(evaluation);
    }

    private static List<CodeTemplateItem> GetPresetTemplates()
    {
        return new List<CodeTemplateItem>
        {
            new()
            {
                Id = "hello-world",
                Title = "1. Hello World & Console",
                Category = "Cơ bản",
                Description = "In thông tin ra màn hình console và thao tác với chuỗi string interpolation.",
                Code = @"// Chào mừng đến với .NET C# Playground!
string name = ""Lập trình viên .NET"";
DateTime now = DateTime.Now;

Console.WriteLine($""Xin chào, {name}!"");
Console.WriteLine($""Hôm nay là: {now:dd/MM/yyyy HH:mm:ss}"");
Console.WriteLine(""Chúc bạn có một buổi luyện code thực chiến hiệu quả!"");"
            },
            new()
            {
                Id = "linq-query",
                Title = "2. LINQ Query & Data Transformation",
                Category = "LINQ",
                Description = "Lọc, ánh xạ (Select) và thống kê mảng dữ liệu số chẵn bằng cú pháp LINQ hiện đại.",
                Code = @"// Lọc và biến đổi dữ liệu với LINQ
var numbers = new List<int> { 12, 5, 8, 21, 44, 3, 18, 9, 30 };

var result = numbers
    .Where(n => n % 2 == 0)
    .OrderByDescending(n => n)
    .Select(n => new { Number = n, Square = n * n })
    .ToList();

Console.WriteLine($""Tổng số chẵn tìm thấy: {result.Count}"");
Console.WriteLine(""Danh sách số chẵn và bình phương:"");
foreach (var item in result)
{
    Console.WriteLine($""- Số: {item.Number,2} | Bình phương: {item.Square,4}"");
}"
            },
            new()
            {
                Id = "records-oop",
                Title = "3. C# Modern Records & Pattern Matching",
                Category = "OOP & C# Modern",
                Description = "Sử dụng Record bất biến (Immutable), cú pháp with-expression và switch expression.",
                Code = @"// Record đại diện cho đối tượng dữ liệu bất biến
public record Student(string Name, int Score, string Track);

var students = new List<Student>
{
    new(""Nguyễn Văn An"", 95, "".NET Backend""),
    new(""Trần Thị Bình"", 78, ""Frontend React""),
    new(""Lê Hoàng Nam"", 88, "".NET Backend""),
    new(""Phạm Minh Đức"", 62, ""DevOps"")
};

// Phân loại xếp loại học viên bằng Switch Expression
string GetGrade(int score) => score switch
{
    >= 90 => ""Xuất sắc (A+)"",
    >= 80 => ""Giỏi (A)"",
    >= 65 => ""Khá (B)"",
    _ => ""Cần cố gắng (C)""
};

Console.WriteLine(""--- KẾT QUẢ ĐÁNH GIÁ HỌC VIÊN ---"");
foreach (var s in students)
{
    Console.WriteLine($""Học viên: {s.Name,-16} | Khóa: {s.Track,-13} | Điểm: {s.Score} -> {GetGrade(s.Score)}"");
}"
            },
            new()
            {
                Id = "async-await",
                Title = "4. Asynchronous Task & Parallel",
                Category = "Async / Await",
                Description = "Mô phỏng tác vụ bất đồng bộ đa luồng với Task.WhenAll.",
                Code = @"// Tác vụ bất đồng bộ trong C#
Console.WriteLine(""Bắt đầu tải dữ liệu song song..."");

async Task<string> FetchDataAsync(string source, int delayMs)
{
    await Task.Delay(delayMs);
    return $""Dữ liệu từ {source} (hoàn thành sau {delayMs}ms)"";
}

var task1 = FetchDataAsync(""Máy chủ Hà Nội"", 120);
var task2 = FetchDataAsync(""Máy chủ TP.HCM"", 80);
var task3 = FetchDataAsync(""Máy chủ Đà Nẵng"", 150);

var results = await Task.WhenAll(task1, task2, task3);

Console.WriteLine(""Tất cả các nguồn dữ liệu đã sẵn sàng:"");
foreach (var res in results)
{
    Console.WriteLine($""[OK] {res}"");
}"
            },
            new()
            {
                Id = "algorithms",
                Title = "5. Thuật toán: Dãy số Fibonacci & Đệ quy",
                Category = "Thuật toán",
                Description = "Tính số Fibonacci tối ưu bộ nhớ O(1) và so sánh thời gian thực thi.",
                Code = @"// Tính số Fibonacci thứ n tối ưu O(n) thời gian, O(1) không gian bộ nhớ
long Fibonacci(int n)
{
    if (n <= 0) return 0;
    if (n == 1) return 1;

    long a = 0, b = 1;
    for (int i = 2; i <= n; i++)
    {
        long temp = a + b;
        a = b;
        b = temp;
    }
    return b;
}

Console.WriteLine(""15 số đầu tiên trong dãy Fibonacci:"");
for (int i = 0; i <= 15; i++)
{
    Console.Write($""{Fibonacci(i)} "");
}
Console.WriteLine();
Console.WriteLine($""Fibonacci(50) = {Fibonacci(50):N0}"");"
            }
        };
    }
}
