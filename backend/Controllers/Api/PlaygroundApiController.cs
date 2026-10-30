using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

[ApiController]
[Route("api/playground")]
[Produces("application/json")]
public class PlaygroundApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICodeExecutionService _executionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMobileAuthService _mobileAuthService;

    public PlaygroundApiController(
        AppDbContext context,
        ICodeExecutionService executionService,
        UserManager<ApplicationUser> userManager,
        IMobileAuthService mobileAuthService)
    {
        _context = context;
        _executionService = executionService;
        _userManager = userManager;
        _mobileAuthService = mobileAuthService;
    }

    private string? GetCurrentUserId()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var (isValid, userId) = _mobileAuthService.ValidateToken(token);
        return isValid ? userId : null;
    }

    [HttpGet("templates")]
    public IActionResult GetTemplates()
    {
        var templates = new List<object>
        {
            new
            {
                Id = "hello-world",
                Title = "Hello .NET 10",
                Description = "Cú pháp C# hiện đại với Top-level statements",
                Code = "// Chào mừng đến với .NET 10 & C# 14 C# Interactive Sandbox!\nConsole.WriteLine(\"Xin chào thế giới lập trình .NET!\");\n\nvar (greeting, year) = (\"Happy Coding\", 2026);\nConsole.WriteLine($\"{greeting} in {year}!\");\n"
            },
            new
            {
                Id = "linq-query",
                Title = "LINQ & Collections",
                Description = "Thao tác mảng dữ liệu với cú pháp LINQ mạnh mẽ",
                Code = "var numbers = Enumerable.Range(1, 10).ToList();\nvar evens = numbers.Where(n => n % 2 == 0).Select(n => n * n);\n\nConsole.WriteLine(\"Các số chẵn bình phương:\");\nforeach (var num in evens)\n{\n    Console.WriteLine($\" -> {num}\");\n}\n"
            },
            new
            {
                Id = "record-pattern",
                Title = "Record & Pattern Matching",
                Description = "Cú pháp C# 14 Record type và Pattern Matching",
                Code = "public record Developer(string Name, string Role, int ExperienceYears);\n\nvar dev = new Developer(\"Alex\", \"Backend .NET Lead\", 5);\n\nstring seniority = dev switch\n{\n    { ExperienceYears: >= 5 } => \"Senior Architect\",\n    { ExperienceYears: >= 2 } => \"Mid-Level Developer\",\n    _ => \"Junior Engineer\"\n};\n\nConsole.WriteLine($\"{dev.Name} ({dev.Role}): {seniority}\");\n"
            },
            new
            {
                Id = "async-await",
                Title = "Async / Await & Tasks",
                Description = "Lập trình bất đồng bộ hiệu năng cao với Task",
                Code = "async Task<string> FetchDataAsync(int id)\n{\n    await Task.Delay(100);\n    return $\"Dữ liệu bài giảng #{id}\";\n}\n\nConsole.WriteLine(\"Bắt đầu tải...\");\nvar task1 = FetchDataAsync(1);\nvar task2 = FetchDataAsync(2);\n\nvar results = await Task.WhenAll(task1, task2);\nforeach (var res in results)\n{\n    Console.WriteLine(res);\n}\n"
            }
        };

        return Ok(templates);
    }

    [HttpGet("challenges")]
    public async Task<IActionResult> GetChallenges([FromQuery] string? difficulty, [FromQuery] int? categoryId)
    {
        var query = _context.CodingChallenges
            .Include(c => c.Category)
            .Include(c => c.TestCases)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(difficulty) && Enum.TryParse<DifficultyLevel>(difficulty, true, out var diffEnum))
        {
            query = query.Where(c => c.Difficulty == diffEnum);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        var userId = GetCurrentUserId();
        var passedIds = new HashSet<int>();
        if (!string.IsNullOrEmpty(userId))
        {
            passedIds = (await _context.CodeSubmissions
                .Where(s => s.UserId == userId && s.IsPassed)
                .Select(s => s.CodingChallengeId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();
        }

        var challenges = await query
            .OrderBy(c => c.OrderIndex)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Slug,
                c.ShortDescription,
                DescriptionMarkdown = c.InstructionsMarkdown,
                Difficulty = c.Difficulty.ToString(),
                c.OrderIndex,
                c.InitialCode,
                c.SolutionCode,
                c.XpReward,
                CategoryName = c.Category != null ? c.Category.Name : string.Empty,
                TotalTestCases = c.TestCases.Count,
                IsPassed = passedIds.Contains(c.Id)
            })
            .ToListAsync();

        return Ok(challenges);
    }

    [HttpGet("challenges/{id:int}")]
    public async Task<IActionResult> GetChallengeDetail(int id)
    {
        var challenge = await _context.CodingChallenges
            .Include(c => c.Category)
            .Include(c => c.TestCases)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (challenge == null)
        {
            return NotFound(new { message = "Không tìm thấy bài tập thử thách." });
        }

        var userId = GetCurrentUserId();
        bool isPassed = false;
        if (!string.IsNullOrEmpty(userId))
        {
            isPassed = await _context.CodeSubmissions
                .AnyAsync(s => s.UserId == userId && s.CodingChallengeId == id && s.IsPassed);
        }

        var result = new
        {
            challenge.Id,
            challenge.Title,
            challenge.Slug,
            challenge.ShortDescription,
            DescriptionMarkdown = challenge.InstructionsMarkdown,
            Difficulty = challenge.Difficulty.ToString(),
            challenge.OrderIndex,
            challenge.InitialCode,
            challenge.XpReward,
            CategoryName = challenge.Category?.Name ?? string.Empty,
            IsPassed = isPassed,
            TestCases = challenge.TestCases.Select(t => new
            {
                t.Id,
                Input = t.InputParameters,
                t.ExpectedOutput,
                t.IsHidden,
                t.Explanation
            }).ToList()
        };

        return Ok(result);
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunCode([FromBody] CodeExecutionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = "Mã nguồn C# không được để trống."
            });
        }

        var result = await _executionService.ExecuteAsync(request.Code);
        return Ok(result);
    }

    [HttpPost("submit")]
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
                CompileError = "Không tìm thấy bài tập thử thách lập trình này."
            });
        }

        var evaluation = await _executionService.EvaluateChallengeAsync(challenge, request.Code);

        // Record submission and handle XP if user is logged in
        var userId = GetCurrentUserId();
        int xpEarned = 0;
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && evaluation.AllPassed)
            {
                var alreadyPassed = await _context.CodeSubmissions
                    .AnyAsync(s => s.UserId == user.Id && s.CodingChallengeId == challenge.Id && s.IsPassed);

                if (!alreadyPassed)
                {
                    xpEarned = challenge.XpReward;
                    user.ExperiencePoints += xpEarned;
                    await _userManager.UpdateAsync(user);
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
            }
        }

        evaluation.XpEarned = xpEarned;
        return Ok(evaluation);
    }
}
