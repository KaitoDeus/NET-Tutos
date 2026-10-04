using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class FlashcardService : IFlashcardService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMarkdownService _markdownService;
    private readonly IStreakService _streakService;
    private readonly IActivityFeedService _activityFeedService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<FlashcardService> _logger;

    public FlashcardService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IMarkdownService markdownService,
        IStreakService streakService,
        IActivityFeedService activityFeedService,
        INotificationService notificationService,
        ILogger<FlashcardService> logger)
    {
        _context = context;
        _userManager = userManager;
        _markdownService = markdownService;
        _streakService = streakService;
        _activityFeedService = activityFeedService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<InterviewIndexViewModel> GetIndexDataAsync(
        string? userId,
        FlashcardTopic? topic = null,
        DifficultyLevel? difficulty = null,
        string? search = null,
        int page = 1,
        int pageSize = 12,
        bool isEn = false)
    {
        var allCards = await _context.InterviewFlashcards
            .OrderBy(f => f.Topic)
            .ThenBy(f => f.OrderIndex)
            .ToListAsync();

        var userProgressMap = new Dictionary<int, UserFlashcardProgress>();
        int userXp = 0;
        int currentStreak = 0;

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                userXp = user.ExperiencePoints;
            }

            var streakStatus = await _streakService.GetUserStreakStatusAsync(userId);
            currentStreak = streakStatus.CurrentStreak;

            var progresses = await _context.UserFlashcardProgresses
                .Where(p => p.UserId == userId)
                .ToListAsync();

            userProgressMap = progresses.ToDictionary(p => p.FlashcardId);
        }

        var now = DateTime.UtcNow;

        // Group topics
        var topics = Enum.GetValues<FlashcardTopic>().Select(t =>
        {
            var cardsInTopic = allCards.Where(c => c.Topic == t).ToList();
            int total = cardsInTopic.Count;
            int mastered = cardsInTopic.Count(c => userProgressMap.TryGetValue(c.Id, out var p) && p.IsMastered);
            int due = cardsInTopic.Count(c => !userProgressMap.TryGetValue(c.Id, out var p) || p.NextReviewDate <= now);

            var (name, desc, icon, color) = GetTopicMetadata(t, isEn);

            return new TopicSummaryViewModel
            {
                Topic = t,
                Name = name,
                Description = desc,
                IconClass = icon,
                ColorClass = color,
                TotalCards = total,
                MasteredCards = mastered,
                DueCards = due
            };
        }).ToList();

        // Filter cards for list
        var filteredQuery = allCards.AsEnumerable();

        if (topic.HasValue)
        {
            filteredQuery = filteredQuery.Where(c => c.Topic == topic.Value);
        }

        if (difficulty.HasValue)
        {
            filteredQuery = filteredQuery.Where(c => c.Difficulty == difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            filteredQuery = filteredQuery.Where(c =>
                c.QuestionVi.ToLowerInvariant().Contains(s) ||
                c.QuestionEn.ToLowerInvariant().Contains(s) ||
                (c.QuickHintVi != null && c.QuickHintVi.ToLowerInvariant().Contains(s)) ||
                (c.QuickHintEn != null && c.QuickHintEn.ToLowerInvariant().Contains(s)));
        }

        var filteredList = filteredQuery.ToList();
        int totalFiltered = filteredList.Count;
        int totalPages = (int)Math.Ceiling((double)totalFiltered / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pagedCards = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapToItemViewModel(c, userProgressMap.GetValueOrDefault(c.Id), isEn))
            .ToList();

        int totalCardsCount = allCards.Count;
        int totalMasteredCount = userProgressMap.Values.Count(p => p.IsMastered);
        int dueTodayCount = allCards.Count(c => !userProgressMap.TryGetValue(c.Id, out var p) || p.NextReviewDate <= now);
        int totalReviews = userProgressMap.Values.Sum(p => p.ReviewCount);

        return new InterviewIndexViewModel
        {
            Topics = topics,
            TotalCards = totalCardsCount,
            MasteredCards = totalMasteredCount,
            DueTodayCount = dueTodayCount,
            TotalReviewsCompleted = totalReviews,
            CurrentStreak = currentStreak,
            UserXp = userXp,
            RecentCards = pagedCards,
            CurrentTopic = topic,
            CurrentDifficulty = difficulty,
            SearchQuery = search,
            CurrentPage = page,
            TotalPages = totalPages
        };
    }

    public async Task<StudySessionViewModel> GetStudySessionAsync(
        string? userId,
        FlashcardTopic? topic = null,
        DifficultyLevel? difficulty = null,
        int limit = 15,
        bool isEn = false)
    {
        var allCardsQuery = _context.InterviewFlashcards.AsQueryable();

        if (topic.HasValue)
        {
            allCardsQuery = allCardsQuery.Where(c => c.Topic == topic.Value);
        }

        if (difficulty.HasValue)
        {
            allCardsQuery = allCardsQuery.Where(c => c.Difficulty == difficulty.Value);
        }

        var allCards = await allCardsQuery.ToListAsync();

        var userProgressMap = new Dictionary<int, UserFlashcardProgress>();
        if (!string.IsNullOrEmpty(userId))
        {
            var progresses = await _context.UserFlashcardProgresses
                .Where(p => p.UserId == userId)
                .ToListAsync();
            userProgressMap = progresses.ToDictionary(p => p.FlashcardId);
        }

        var now = DateTime.UtcNow;

        // Prioritize: 1. Due cards (Spaced Repetition review), 2. New unstudied cards, 3. Review learning cards
        var dueCards = allCards
            .Where(c => userProgressMap.TryGetValue(c.Id, out var p) && p.NextReviewDate <= now)
            .OrderBy(c => userProgressMap[c.Id].NextReviewDate)
            .ToList();

        var newCards = allCards
            .Where(c => !userProgressMap.ContainsKey(c.Id))
            .OrderBy(c => c.OrderIndex)
            .ToList();

        var otherCards = allCards
            .Where(c => !dueCards.Contains(c) && !newCards.Contains(c))
            .OrderBy(c => userProgressMap[c.Id].LastReviewedAt)
            .ToList();

        var sessionList = new List<InterviewFlashcard>();
        sessionList.AddRange(dueCards);
        sessionList.AddRange(newCards);
        sessionList.AddRange(otherCards);

        var finalQueue = sessionList
            .Take(limit)
            .Select(c => MapToItemViewModel(c, userProgressMap.GetValueOrDefault(c.Id), isEn))
            .ToList();

        string topicName = topic.HasValue
            ? GetTopicMetadata(topic.Value, isEn).Name
            : (isEn ? "All Interview Topics" : "Toàn bộ chủ đề phỏng vấn");

        return new StudySessionViewModel
        {
            Topic = topic,
            TopicName = topicName,
            Difficulty = difficulty,
            QueueCards = finalQueue
        };
    }

    public async Task<FlashcardReviewResponse> RecordReviewAsync(
        string userId,
        int flashcardId,
        FlashcardRating rating,
        bool isEn = false)
    {
        var flashcard = await _context.InterviewFlashcards.FindAsync(flashcardId);
        if (flashcard == null)
        {
            return new FlashcardReviewResponse
            {
                Success = false,
                Message = isEn ? "Flashcard not found." : "Không tìm thấy thẻ câu hỏi."
            };
        }

        var progress = await _context.UserFlashcardProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.FlashcardId == flashcardId);

        int currentBox = progress?.BoxLevel ?? 1;
        int newBox = currentBox;
        int nextDays = 1;
        int xpEarned = 5;
        bool isMastered = false;

        switch (rating)
        {
            case FlashcardRating.Again:
                newBox = 1;
                nextDays = 1;
                xpEarned = 2;
                isMastered = false;
                break;
            case FlashcardRating.Hard:
                newBox = Math.Max(1, currentBox);
                nextDays = 3;
                xpEarned = 3;
                isMastered = false;
                break;
            case FlashcardRating.Good:
                newBox = Math.Min(5, currentBox + 1);
                nextDays = newBox switch
                {
                    1 => 2,
                    2 => 5,
                    3 => 9,
                    4 => 16,
                    _ => 30
                };
                xpEarned = 5;
                isMastered = newBox >= 4;
                break;
            case FlashcardRating.Mastered:
                newBox = Math.Min(5, Math.Max(4, currentBox + 2));
                nextDays = 21;
                xpEarned = 10;
                isMastered = true;
                break;
        }

        var now = DateTime.UtcNow;

        if (progress == null)
        {
            progress = new UserFlashcardProgress
            {
                UserId = userId,
                FlashcardId = flashcardId,
                BoxLevel = newBox,
                ReviewCount = 1,
                LastRating = rating,
                LastReviewedAt = now,
                NextReviewDate = now.AddDays(nextDays),
                IsMastered = isMastered
            };
            _context.UserFlashcardProgresses.Add(progress);
        }
        else
        {
            progress.BoxLevel = newBox;
            progress.ReviewCount += 1;
            progress.LastRating = rating;
            progress.LastReviewedAt = now;
            progress.NextReviewDate = now.AddDays(nextDays);
            if (isMastered) progress.IsMastered = true;
        }

        // Award XP to user
        int totalXp = 0;
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.ExperiencePoints += xpEarned;
            totalXp = user.ExperiencePoints;
            await _userManager.UpdateAsync(user);
        }

        await _context.SaveChangesAsync();

        // Increment flashcard view count
        flashcard.ViewCount += 1;
        await _context.SaveChangesAsync();

        // Maintain streak
        try
        {
            await _streakService.RecordLearningActivityStreakAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi duy trì streak khi ôn thẻ phỏng vấn");
        }

        string message = isEn
            ? (isMastered ? $"🎉 Mastered! +{xpEarned} XP. Next review in {nextDays} days." : $"Reviewed! +{xpEarned} XP. Next review in {nextDays} days.")
            : (isMastered ? $"🎉 Đã nắm vững! +{xpEarned} XP. Ôn lại sau {nextDays} ngày." : $"Đã ghi nhận! +{xpEarned} XP. Ôn lại sau {nextDays} ngày.");

        return new FlashcardReviewResponse
        {
            Success = true,
            FlashcardId = flashcardId,
            NewBoxLevel = newBox,
            NextReviewDays = nextDays,
            XpEarned = xpEarned,
            TotalXp = totalXp,
            IsMastered = isMastered,
            Message = message
        };
    }

    public async Task<MockInterviewViewModel> CreateMockInterviewAsync(
        DifficultyLevel? difficulty = null,
        int count = 5,
        bool isEn = false)
    {
        var query = _context.InterviewFlashcards.AsQueryable();

        if (difficulty.HasValue)
        {
            query = query.Where(c => c.Difficulty == difficulty.Value);
        }

        var allMatching = await query.ToListAsync();

        // Shuffle randomly
        var random = new Random();
        var selected = allMatching
            .OrderBy(_ => random.Next())
            .Take(count)
            .Select(c => MapToItemViewModel(c, null, isEn))
            .ToList();

        return new MockInterviewViewModel
        {
            Difficulty = difficulty,
            TimeLimitSeconds = count * 120, // 2 minutes per question
            Questions = selected
        };
    }

    public async Task<MockInterviewResultViewModel> SubmitMockInterviewAsync(
        string userId,
        SubmitMockInterviewRequest request,
        bool isEn = false)
    {
        var questionIds = request.Answers.Select(a => a.FlashcardId).ToList();
        var cards = await _context.InterviewFlashcards
            .Where(c => questionIds.Contains(c.Id))
            .ToListAsync();

        var cardMap = cards.ToDictionary(c => c.Id);
        var results = new List<MockQuestionResultItem>();
        double totalScore = 0;

        foreach (var ans in request.Answers)
        {
            if (cardMap.TryGetValue(ans.FlashcardId, out var card))
            {
                int score = Math.Clamp(ans.Score, 0, 100);
                totalScore += score;
                results.Add(new MockQuestionResultItem
                {
                    Flashcard = MapToItemViewModel(card, null, isEn),
                    Score = score
                });
            }
        }

        int totalCount = results.Count;
        double avgScore = totalCount > 0 ? Math.Round(totalScore / totalCount, 1) : 0;
        int passedCount = results.Count(r => r.IsPassed);

        string readinessLevel;
        string badgeClass;
        string feedback;
        int xpAwarded;

        if (avgScore >= 85)
        {
            readinessLevel = isEn ? "Senior Ready - Excellent" : "Sẵn sàng Phỏng vấn Senior / Nâng cao";
            badgeClass = "success";
            feedback = isEn
                ? "Outstanding technical depth and clear grasp of .NET internals! You are fully primed for technical interview rounds."
                : "Kiến thức chuyên sâu và nắm vững cơ chế bản chất của .NET! Bạn đã sẵn sàng tự tin bước vào các vòng phỏng vấn chuyên môn.";
            xpAwarded = 60;
        }
        else if (avgScore >= 70)
        {
            readinessLevel = isEn ? "Middle Ready - Confident" : "Sẵn sàng Phỏng vấn Middle .NET Developer";
            badgeClass = "primary";
            feedback = isEn
                ? "Solid foundation on core concepts and practical patterns. Review the few tricky questions to achieve peak confidence!"
                : "Nền tảng vững chắc về cú pháp và kiến trúc ứng dụng. Hãy ôn lại một số câu bẫy để đạt độ tự tin cao nhất!";
            xpAwarded = 45;
        }
        else if (avgScore >= 50)
        {
            readinessLevel = isEn ? "Junior Ready - Good Foundation" : "Đạt chuẩn Phỏng vấn Junior .NET Developer";
            badgeClass = "info";
            feedback = isEn
                ? "Good grasp of fundamental C# syntax. Dedicate more study time to asynchronous programming, EF Core tracking, and memory management."
                : "Nắm tốt cú pháp cơ bản C#. Nên dành thêm thời gian ôn luyện về async/await, EF Core tối ưu truy vấn và cơ chế quản lý bộ nhớ.";
            xpAwarded = 30;
        }
        else
        {
            readinessLevel = isEn ? "Needs Revision" : "Cần Ôn Luyện Thêm";
            badgeClass = "warning";
            feedback = isEn
                ? "Keep practicing! Review flashcards daily using Spaced Repetition mode to solidify theoretical concepts."
                : "Đừng nản lòng! Hãy sử dụng chế độ Lật thẻ Flashcard hàng ngày để củng cố các câu hỏi lý thuyết kinh điển.";
            xpAwarded = 15;
        }

        // Award XP to user
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.ExperiencePoints += xpAwarded;
                await _userManager.UpdateAsync(user);
            }

            // Maintain streak
            try
            {
                await _streakService.RecordLearningActivityStreakAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi duy trì streak sau phỏng vấn thử");
            }

            // Send Activity Feed if passed
            if (avgScore >= 70)
            {
                try
                {
                    await _activityFeedService.RecordActivityAsync(
                        userId,
                        ActivityType.ChallengeSolved,
                        isEn ? "completed a .NET mock interview" : "đã hoàn thành phỏng vấn thử .NET",
                        isEn ? $"Scored {avgScore:F0}% ({readinessLevel})" : $"Đạt {avgScore:F0}% điểm ({readinessLevel})",
                        "/Interview",
                        xpAwarded);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi ghi nhận ActivityFeed cho Mock Interview");
                }
            }
        }

        return new MockInterviewResultViewModel
        {
            TotalQuestions = totalCount,
            AverageScore = avgScore,
            PassedCount = passedCount,
            ReadinessLevel = readinessLevel,
            ReadinessBadgeClass = badgeClass,
            FeedbackSummary = feedback,
            XpAwarded = xpAwarded,
            TimeSpentSeconds = request.TimeSpentSeconds,
            Results = results
        };
    }

    public async Task<FlashcardItemViewModel?> GetFlashcardBySlugAsync(
        string slug,
        string? userId = null,
        bool isEn = false)
    {
        var card = await _context.InterviewFlashcards
            .FirstOrDefaultAsync(c => c.Slug == slug);

        if (card == null) return null;

        UserFlashcardProgress? progress = null;
        if (!string.IsNullOrEmpty(userId))
        {
            progress = await _context.UserFlashcardProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.FlashcardId == card.Id);
        }

        return MapToItemViewModel(card, progress, isEn);
    }

    private FlashcardItemViewModel MapToItemViewModel(
        InterviewFlashcard card,
        UserFlashcardProgress? progress,
        bool isEn)
    {
        var (topicName, _, topicIcon, topicColor) = GetTopicMetadata(card.Topic, isEn);
        var (diffName, diffColor) = GetDifficultyMetadata(card.Difficulty, isEn);

        string question = isEn ? card.QuestionEn : card.QuestionVi;
        string? hint = isEn ? card.QuickHintEn : card.QuickHintVi;
        string answerMd = isEn ? card.AnswerMarkdownEn : card.AnswerMarkdownVi;
        string? tip = isEn ? card.InterviewTipEn : card.InterviewTipVi;

        string answerHtml = _markdownService.ToHtml(answerMd);

        var now = DateTime.UtcNow;
        bool isDue = progress == null || progress.NextReviewDate <= now;

        return new FlashcardItemViewModel
        {
            Id = card.Id,
            Slug = card.Slug,
            Topic = card.Topic,
            TopicName = topicName,
            TopicBadgeClass = topicColor,
            TopicIconClass = topicIcon,
            Difficulty = card.Difficulty,
            DifficultyName = diffName,
            DifficultyBadgeClass = diffColor,
            Question = question,
            QuickHint = hint,
            AnswerMarkdown = answerMd,
            AnswerHtml = answerHtml,
            InterviewTip = tip,
            BoxLevel = progress?.BoxLevel ?? 1,
            ReviewCount = progress?.ReviewCount ?? 0,
            NextReviewDate = progress?.NextReviewDate,
            IsDueForReview = isDue,
            IsMastered = progress?.IsMastered ?? false,
            LastReviewedAt = progress?.LastReviewedAt
        };
    }

    private static (string Name, string Desc, string Icon, string Color) GetTopicMetadata(FlashcardTopic topic, bool isEn) => topic switch
    {
        FlashcardTopic.CSharpCore => (
            isEn ? "C# Fundamentals & Syntax" : "C# Nền Tảng & Cú Pháp Mới",
            isEn ? "Value vs Reference types, Memory allocation, GC, Records, Pattern matching, Generics." : "Value vs Reference Type, Quản lý bộ nhớ, Garbage Collection, Record, Pattern Matching, Generics.",
            "bi-filetype-cs",
            "primary"
        ),
        FlashcardTopic.OopDesignPatterns => (
            isEn ? "OOP & Design Patterns" : "OOP & Mẫu Thiết Kế",
            isEn ? "4 OOP Pillars, SOLID Principles, Dependency Injection lifetimes, Factory, Singleton, Repository." : "4 Trụ cột OOP, SOLID Principles, Vòng đời Dependency Injection (Transient, Scoped, Singleton).",
            "bi-diagram-3-fill",
            "info"
        ),
        FlashcardTopic.AsyncConcurrency => (
            isEn ? "Async & Concurrency" : "Bất Đồng Bộ & Concurrency",
            isEn ? "async/await state machine, Task vs ValueTask, ConfigureAwait, Deadlocks, ThreadPool." : "Cơ chế State Machine async/await, Task vs ValueTask, ConfigureAwait, Tránh Deadlock trong .NET.",
            "bi-lightning-charge-fill",
            "warning"
        ),
        FlashcardTopic.EfCoreDatabase => (
            isEn ? "EF Core & SQL Optimization" : "EF Core & Tối Ưu Truy Vấn",
            isEn ? "Change Tracker, AsNoTracking, N+1 Query problem, Split Queries, Lazy vs Eager loading, Indexes." : "Change Tracker, AsNoTracking, Vấn đề N+1 Query, Split Queries, Eager vs Lazy Loading, Đánh Index.",
            "bi-database-fill-check",
            "success"
        ),
        FlashcardTopic.AspNetCoreWebAPI => (
            isEn ? "ASP.NET Core & Web API" : "ASP.NET Core & RESTful API",
            isEn ? "Middleware pipeline, Filters, JWT Auth, Minimal APIs, Model Validation, Rate Limiting." : "Pipeline Middleware, Action Filters, Xác thực JWT, Minimal APIs, Xử lý ngoại lệ toàn cục.",
            "bi-globe2",
            "danger"
        ),
        FlashcardTopic.ArchitectureCloud => (
            isEn ? "Clean Architecture & Cloud" : "Clean Architecture & Hệ Thống",
            isEn ? "Domain-Driven Design, CQRS, Redis Caching, Microservices communication, Resiliency with Polly." : "Kiến trúc Clean Architecture, CQRS, Caching đa tầng với Redis, Khả năng chịu lỗi với Polly.",
            "bi-clouds-fill",
            "secondary"
        ),
        _ => (
            isEn ? "General .NET" : ".NET Tổng hợp",
            isEn ? "Core .NET ecosystem interview questions." : "Câu hỏi phỏng vấn tổng hợp hệ sinh thái .NET.",
            "bi-cpu-fill",
            "primary"
        )
    };

    private static (string Name, string Color) GetDifficultyMetadata(DifficultyLevel diff, bool isEn) => diff switch
    {
        DifficultyLevel.Beginner => (isEn ? "Junior (Beginner)" : "Junior (Cơ bản)", "info"),
        DifficultyLevel.Intermediate => (isEn ? "Middle (Intermediate)" : "Middle (Trung cấp)", "primary"),
        DifficultyLevel.Advanced => (isEn ? "Senior (Advanced)" : "Senior (Nâng cao)", "danger"),
        _ => (isEn ? "All Levels" : "Mọi cấp độ", "secondary")
    };
}
