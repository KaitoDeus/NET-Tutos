using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.DTOs.Mobile;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

[ApiController]
[Route("api/mobile")]
public class MobileTutorialsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILearningProgressService _progressService;
    private readonly IMobileAuthService _mobileAuthService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStreakService _streakService;
    private readonly ICurriculumService _curriculumService;

    public MobileTutorialsController(
        AppDbContext context,
        ILearningProgressService progressService,
        IMobileAuthService mobileAuthService,
        UserManager<ApplicationUser> userManager,
        IStreakService streakService,
        ICurriculumService curriculumService)
    {
        _context = context;
        _progressService = progressService;
        _mobileAuthService = mobileAuthService;
        _userManager = userManager;
        _streakService = streakService;
        _curriculumService = curriculumService;
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

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.OrderIndex)
            .Select(c => new MobileCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description ?? string.Empty,
                BadgeColor = c.BadgeColor ?? "primary",
                IconClass = c.IconClass ?? "bi-book",
                TutorialCount = c.Tutorials.Count
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("tutorials")]
    public async Task<IActionResult> GetTutorials(
        [FromQuery] string? category, 
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Tutorials.AsNoTracking().Include(t => t.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Category != null && (t.Category.Slug == category || t.Category.Name == category));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(s) || (t.Summary != null && t.Summary.ToLower().Contains(s)));
        }

        var userId = GetCurrentUserId();
        var completedIds = new HashSet<int>();
        if (!string.IsNullOrEmpty(userId))
        {
            completedIds = (await _context.UserLessonProgresses
                .Where(p => p.UserId == userId && p.IsCompleted)
                .Select(p => p.TutorialId)
                .ToListAsync())
                .ToHashSet();
        }

        var tutorials = await query
            .OrderBy(t => t.OrderIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new MobileTutorialSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                Slug = t.Slug,
                Summary = t.Summary,
                EstimatedReadingMinutes = t.EstimatedReadingMinutes,
                Difficulty = t.Difficulty.ToString(),
                CategoryId = t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : string.Empty,
                OrderIndex = t.OrderIndex,
                ViewCount = t.ViewCount,
                IsCompleted = completedIds.Contains(t.Id)
            })
            .ToListAsync();

        return Ok(tutorials);
    }

    [HttpGet("tutorials/{slug}")]
    public async Task<IActionResult> GetTutorialDetail(string slug)
    {
        var tutorial = await _context.Tutorials
            .Include(t => t.Category)
            .Include(t => t.QuizQuestions)
            .FirstOrDefaultAsync(t => t.Slug == slug || t.Id.ToString() == slug);

        if (tutorial == null)
        {
            return NotFound(new { message = "Không tìm thấy bài học." });
        }

        // Increase view count
        tutorial.ViewCount++;
        await _context.SaveChangesAsync();

        var userId = GetCurrentUserId();
        bool isCompleted = false;
        if (!string.IsNullOrEmpty(userId))
        {
            isCompleted = await _context.UserLessonProgresses
                .AnyAsync(p => p.UserId == userId && p.TutorialId == tutorial.Id && p.IsCompleted);
        }

        // Previous and Next tutorials in same category or sequence
        var prevTutorial = await _context.Tutorials
            .Where(t => t.CategoryId == tutorial.CategoryId && t.OrderIndex < tutorial.OrderIndex)
            .OrderByDescending(t => t.OrderIndex)
            .Select(t => new MobileTutorialSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                Slug = t.Slug,
                Summary = t.Summary,
                EstimatedReadingMinutes = t.EstimatedReadingMinutes,
                Difficulty = t.Difficulty.ToString(),
                CategoryId = t.CategoryId,
                OrderIndex = t.OrderIndex
            })
            .FirstOrDefaultAsync();

        var nextTutorial = await _context.Tutorials
            .Where(t => t.CategoryId == tutorial.CategoryId && t.OrderIndex > tutorial.OrderIndex)
            .OrderBy(t => t.OrderIndex)
            .Select(t => new MobileTutorialSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                Slug = t.Slug,
                Summary = t.Summary,
                EstimatedReadingMinutes = t.EstimatedReadingMinutes,
                Difficulty = t.Difficulty.ToString(),
                CategoryId = t.CategoryId,
                OrderIndex = t.OrderIndex
            })
            .FirstOrDefaultAsync();

        var detailDto = new MobileTutorialDetailDto
        {
            Id = tutorial.Id,
            Title = tutorial.Title,
            Slug = tutorial.Slug,
            Summary = tutorial.Summary,
            ContentMarkdown = tutorial.ContentMarkdown,
            EstimatedReadingMinutes = tutorial.EstimatedReadingMinutes,
            Difficulty = tutorial.Difficulty.ToString(),
            CategoryId = tutorial.CategoryId,
            CategoryName = tutorial.Category?.Name ?? string.Empty,
            CategorySlug = tutorial.Category?.Slug ?? string.Empty,
            IsCompleted = isCompleted,
            CreatedAt = tutorial.CreatedAt,
            PreviousTutorial = prevTutorial,
            NextTutorial = nextTutorial,
            Quizzes = tutorial.QuizQuestions.Select(q => new MobileQuizQuestionDto
            {
                Id = q.Id,
                QuestionText = q.Question,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                CorrectOption = q.CorrectOption,
                Explanation = q.Explanation ?? string.Empty
            }).ToList()
        };

        return Ok(detailDto);
    }

    [HttpPost("tutorials/complete")]
    public async Task<IActionResult> ToggleCompleteTutorial([FromBody] MobileCompleteTutorialRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new MobileCompleteTutorialResponse
            {
                IsSuccess = false,
                Message = "Vui lòng đăng nhập để lưu tiến độ hoàn thành."
            });
        }

        var isCompleted = await _progressService.ToggleLessonCompletedAsync(userId, request.TutorialId);
        var user = await _userManager.FindByIdAsync(userId);

        return Ok(new MobileCompleteTutorialResponse
        {
            IsSuccess = true,
            IsCompleted = isCompleted,
            Message = isCompleted 
                ? "Chúc mừng! Bạn đã hoàn thành bài học và nhận được +20 XP!" 
                : "Đã hủy đánh dấu hoàn thành bài học.",
            XpEarned = isCompleted ? 20 : 0,
            TotalXp = user?.ExperiencePoints ?? 0,
            StreakDays = user?.CurrentStreak ?? 0
        });
    }

    [HttpGet("roadmap")]
    public async Task<IActionResult> GetRoadmap()
    {
        var userId = GetCurrentUserId();
        var completedIds = new HashSet<int>();
        if (!string.IsNullOrEmpty(userId))
        {
            completedIds = (await _context.UserLessonProgresses
                .Where(p => p.UserId == userId && p.IsCompleted)
                .Select(p => p.TutorialId)
                .ToListAsync())
                .ToHashSet();
        }

        var curriculum = await _curriculumService.GetCurriculumOverviewAsync(null, null, null);
        var result = new List<MobileRoadmapModuleDto>();

        foreach (var mod in curriculum.Sections)
        {
            var lessons = mod.Lessons.Select(l => new MobileTutorialSummaryDto
            {
                Id = l.LessonNumber,
                Title = l.TitleVi,
                Slug = l.MatchingTutorialSlug ?? l.Slug,
                Summary = l.SummaryVi,
                EstimatedReadingMinutes = l.EstimatedMinutes,
                Difficulty = l.Difficulty.ToString(),
                OrderIndex = l.LessonNumber,
                IsCompleted = false
            }).ToList();

            result.Add(new MobileRoadmapModuleDto
            {
                ModuleNumber = mod.SectionNumber,
                Title = mod.TitleVi,
                Description = mod.DescriptionVi,
                TotalLessons = lessons.Count,
                CompletedLessons = lessons.Count(l => l.IsCompleted),
                Lessons = lessons
            });
        }

        return Ok(result);
    }

    [HttpGet("streak")]
    public async Task<IActionResult> GetStreak()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để xem thông tin streak." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var today = DateTime.UtcNow.Date;
        var last7Days = new List<bool>();

        var checkIns = await _context.DailyCheckIns
            .Where(c => c.UserId == userId && c.CheckInDate >= today.AddDays(-6))
            .Select(c => c.CheckInDate.Date)
            .ToListAsync();

        var checkInSet = checkIns.ToHashSet();

        for (int i = 6; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            last7Days.Add(checkInSet.Contains(d));
        }

        var streakDto = new MobileStreakSummaryDto
        {
            CurrentStreak = user.CurrentStreak,
            LongestStreak = user.LongestStreak,
            IsActiveToday = user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == today,
            LastActivityDate = user.LastCheckInDate,
            Last7DaysActive = last7Days
        };

        return Ok(streakDto);
    }
}
