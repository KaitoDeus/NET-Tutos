using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.DTOs.Mobile;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

[ApiController]
[Route("api/mobile/flashcards")]
public class MobileFlashcardsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFlashcardService _flashcardService;
    private readonly IMobileAuthService _mobileAuthService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MobileFlashcardsController(
        AppDbContext context,
        IFlashcardService flashcardService,
        IMobileAuthService mobileAuthService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _flashcardService = flashcardService;
        _mobileAuthService = mobileAuthService;
        _userManager = userManager;
    }

    private string? GetCurrentUserId()
    {
        return _mobileAuthService.GetBearerUserId(Request);
    }

    [HttpGet]
    public async Task<IActionResult> GetFlashcards(
        [FromQuery] string? topic,
        [FromQuery] string? difficulty,
        [FromQuery] int limit = 50,
        [FromQuery] bool dueOnly = false)
    {
        var query = _context.InterviewFlashcards.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(topic) && Enum.TryParse<FlashcardTopic>(topic, true, out var topicEnum))
        {
            query = query.Where(f => f.Topic == topicEnum);
        }

        if (!string.IsNullOrWhiteSpace(difficulty) && Enum.TryParse<DifficultyLevel>(difficulty, true, out var diffEnum))
        {
            query = query.Where(f => f.Difficulty == diffEnum);
        }

        var userId = GetCurrentUserId();
        var progressMap = new Dictionary<int, UserFlashcardProgress>();
        if (!string.IsNullOrEmpty(userId))
        {
            progressMap = await _context.UserFlashcardProgresses
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.FlashcardId);
        }

        var utcNow = DateTime.UtcNow;
        var allMatching = await query
            .OrderBy(f => f.Topic)
            .ThenBy(f => f.OrderIndex)
            .ToListAsync();

        var mapped = allMatching.Select(c =>
        {
            progressMap.TryGetValue(c.Id, out var p);
            bool isDue = p == null || p.NextReviewDate <= utcNow;
            return new MobileFlashcardDto
            {
                Id = c.Id,
                Slug = c.Slug,
                Topic = c.Topic.ToString(),
                TopicName = GetTopicDisplayName(c.Topic),
                Difficulty = c.Difficulty.ToString(),
                QuestionVi = c.QuestionVi,
                QuestionEn = c.QuestionEn,
                AnswerMarkdownVi = c.AnswerMarkdownVi,
                AnswerMarkdownEn = c.AnswerMarkdownEn,
                InterviewTipVi = c.InterviewTipVi ?? string.Empty,
                InterviewTipEn = c.InterviewTipEn ?? string.Empty,
                OrderIndex = c.OrderIndex,
                BoxLevel = p?.BoxLevel ?? 1,
                IsMastered = p?.IsMastered ?? false,
                ReviewCount = p?.ReviewCount ?? 0,
                NextReviewDate = p?.NextReviewDate,
                IsDue = isDue
            };
        });

        if (dueOnly)
        {
            mapped = mapped.Where(c => c.IsDue);
        }

        var clampedLimit = Math.Clamp(limit, 1, 200);
        return Ok(mapped.Take(clampedLimit).ToList());
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics()
    {
        var allCards = await _context.InterviewFlashcards.AsNoTracking().ToListAsync();
        var userId = GetCurrentUserId();
        var progressMap = new Dictionary<int, UserFlashcardProgress>();

        if (!string.IsNullOrEmpty(userId))
        {
            progressMap = await _context.UserFlashcardProgresses
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.FlashcardId);
        }

        var topics = Enum.GetValues<FlashcardTopic>().Select(t =>
        {
            var cardsInTopic = allCards.Where(c => c.Topic == t).ToList();
            int total = cardsInTopic.Count;
            int mastered = cardsInTopic.Count(c => progressMap.TryGetValue(c.Id, out var p) && p.IsMastered);

            return new MobileFlashcardTopicDto
            {
                Topic = t.ToString(),
                Name = GetTopicDisplayName(t),
                Description = GetTopicDescription(t),
                ColorClass = GetTopicColor(t),
                TotalCards = total,
                MasteredCards = mastered
            };
        }).ToList();

        return Ok(topics);
    }

    [HttpPost("review")]
    public async Task<IActionResult> ReviewFlashcard([FromBody] MobileFlashcardReviewRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new MobileFlashcardReviewResponse
            {
                IsSuccess = false,
                Message = "Vui lòng đăng nhập để lưu kết quả ôn tập flashcard."
            });
        }

        var rating = (FlashcardRating)Math.Clamp(request.Rating, 1, 4);
        var reviewResult = await _flashcardService.RecordReviewAsync(userId, request.FlashcardId, rating);

        if (!reviewResult.Success)
        {
            return BadRequest(new MobileFlashcardReviewResponse
            {
                IsSuccess = false,
                Message = reviewResult.Message
            });
        }

        var user = await _userManager.FindByIdAsync(userId);

        return Ok(new MobileFlashcardReviewResponse
        {
            IsSuccess = true,
            Message = reviewResult.Message,
            NewBoxLevel = reviewResult.NewBoxLevel,
            XpEarned = reviewResult.XpEarned,
            TotalXp = user?.ExperiencePoints ?? 0,
            StreakDays = user?.CurrentStreak ?? 0,
            IsMastered = reviewResult.IsMastered,
            NextReviewDate = DateTime.UtcNow.AddDays(Math.Max(1, reviewResult.NextReviewDays))
        });
    }

    /// <summary>
    /// Batch synchronizes offline spaced-repetition flashcard reviews queued by mobile clients.
    /// </summary>
    [HttpPost("review/batch")]
    public async Task<IActionResult> ReviewFlashcardsBatch([FromBody] MobileBatchFlashcardReviewRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new MobileBatchFlashcardReviewResponse
            {
                IsSuccess = false,
                Message = "Vui lòng đăng nhập để đồng bộ lịch sử ôn tập offline."
            });
        }

        if (request?.Reviews == null || request.Reviews.Count == 0)
        {
            return BadRequest(new MobileBatchFlashcardReviewResponse
            {
                IsSuccess = false,
                Message = "Danh sách thẻ cần đồng bộ không được để trống."
            });
        }

        bool isEn = string.Equals(request.Language, "en", StringComparison.OrdinalIgnoreCase);
        var seenClientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var itemResults = new List<MobileBatchFlashcardReviewItemResult>();
        int duplicateSkipped = 0;
        int totalXpEarned = 0;

        foreach (var item in request.Reviews.Take(200))
        {
            if (!string.IsNullOrWhiteSpace(item.ClientReviewId) && !seenClientIds.Add(item.ClientReviewId))
            {
                duplicateSkipped++;
                continue;
            }

            var rating = (FlashcardRating)Math.Clamp(item.Rating, 1, 4);
            var reviewResult = await _flashcardService.RecordReviewAsync(userId, item.FlashcardId, rating, isEn);

            if (reviewResult.Success)
            {
                totalXpEarned += reviewResult.XpEarned;
            }

            itemResults.Add(new MobileBatchFlashcardReviewItemResult
            {
                ClientReviewId = item.ClientReviewId,
                FlashcardId = item.FlashcardId,
                IsSuccess = reviewResult.Success,
                Message = reviewResult.Message,
                NewBoxLevel = reviewResult.NewBoxLevel,
                XpEarned = reviewResult.XpEarned,
                IsMastered = reviewResult.IsMastered,
                NextReviewDate = reviewResult.Success
                    ? DateTime.UtcNow.AddDays(Math.Max(1, reviewResult.NextReviewDays))
                    : null
            });
        }

        var user = await _userManager.FindByIdAsync(userId);
        int processedCount = itemResults.Count(r => r.IsSuccess);

        return Ok(new MobileBatchFlashcardReviewResponse
        {
            IsSuccess = processedCount > 0,
            Message = isEn
                ? $"Synced {processedCount} offline flashcard review(s), +{totalXpEarned} XP."
                : $"Đã đồng bộ {processedCount} lượt ôn thẻ offline (+{totalXpEarned} XP).",
            ProcessedCount = processedCount,
            DuplicateSkippedCount = duplicateSkipped,
            TotalXpEarned = totalXpEarned,
            TotalXp = user?.ExperiencePoints ?? 0,
            StreakDays = user?.CurrentStreak ?? 0,
            Results = itemResults
        });
    }

    private static string GetTopicDisplayName(FlashcardTopic topic) => topic switch
    {
        FlashcardTopic.CSharpCore => "C# Core & CLR",
        FlashcardTopic.OopDesignPatterns => "OOP & Design Patterns",
        FlashcardTopic.AsyncConcurrency => "Async & Concurrency",
        FlashcardTopic.EfCoreDatabase => "EF Core & Database",
        FlashcardTopic.AspNetCoreWebAPI => "ASP.NET Core & Web API",
        FlashcardTopic.ArchitectureCloud => "Clean Architecture & Cloud",
        _ => topic.ToString()
    };

    private static string GetTopicDescription(FlashcardTopic topic) => topic switch
    {
        FlashcardTopic.CSharpCore => "Bản chất ngôn ngữ, bộ nhớ Heap/Stack, Garbage Collection",
        FlashcardTopic.OopDesignPatterns => "4 tính chất OOP, SOLID, Factory, Singleton, Strategy",
        FlashcardTopic.AsyncConcurrency => "async/await, Task, ThreadPool, Deadlock, SemaphoreSlim",
        FlashcardTopic.EfCoreDatabase => "Truy vấn LINQ, tối ưu hóa N+1, Indexing, Change Tracking",
        FlashcardTopic.AspNetCoreWebAPI => "Middleware pipeline, DI lifecycle, Security, REST API",
        FlashcardTopic.ArchitectureCloud => "Clean Architecture, CQRS, Caching, Resilience Polly",
        _ => string.Empty
    };

    private static string GetTopicColor(FlashcardTopic topic) => topic switch
    {
        FlashcardTopic.CSharpCore => "#38BDF8",        // Sky
        FlashcardTopic.OopDesignPatterns => "#A78BFA", // Purple
        FlashcardTopic.AsyncConcurrency => "#FBBF24",  // Amber
        FlashcardTopic.EfCoreDatabase => "#34D399",    // Emerald
        FlashcardTopic.AspNetCoreWebAPI => "#FB7185",  // Rose
        FlashcardTopic.ArchitectureCloud => "#818CF8", // Indigo
        _ => "#6366F1"
    };
}
