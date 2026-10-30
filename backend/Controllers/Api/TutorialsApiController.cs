using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.DTOs.Mobile;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

[ApiController]
[Route("api/tutorials")]
[Produces("application/json")]
public class TutorialsApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILearningProgressService _progressService;
    private readonly IMobileAuthService _mobileAuthService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TutorialsApiController(
        AppDbContext context,
        ILearningProgressService progressService,
        IMobileAuthService mobileAuthService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _progressService = progressService;
        _mobileAuthService = mobileAuthService;
        _userManager = userManager;
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

    [HttpGet]
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

        var totalItems = await query.CountAsync();

        var items = await query
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

        return Ok(new
        {
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            Items = items
        });
    }

    [HttpGet("{slug}")]
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

        tutorial.ViewCount++;
        await _context.SaveChangesAsync();

        var userId = GetCurrentUserId();
        bool isCompleted = false;
        if (!string.IsNullOrEmpty(userId))
        {
            isCompleted = await _context.UserLessonProgresses
                .AnyAsync(p => p.UserId == userId && p.TutorialId == tutorial.Id && p.IsCompleted);
        }

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

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để lưu tiến độ hoàn thành." });
        }

        var isCompleted = await _progressService.ToggleLessonCompletedAsync(userId, id);
        var user = await _userManager.FindByIdAsync(userId);

        return Ok(new
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
}

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
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
}
