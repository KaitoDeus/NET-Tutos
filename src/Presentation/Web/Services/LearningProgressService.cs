using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class LearningProgressService : ILearningProgressService
{
    private readonly AppDbContext _context;
    private readonly IStreakService _streakService;
    private readonly INotificationService _notificationService;
    private readonly IActivityFeedService _activityFeedService;

    public LearningProgressService(
        AppDbContext context,
        IStreakService streakService,
        INotificationService notificationService,
        IActivityFeedService activityFeedService)
    {
        _context = context;
        _streakService = streakService;
        _notificationService = notificationService;
        _activityFeedService = activityFeedService;
    }

    public async Task<bool> ToggleLessonCompletedAsync(string userId, int tutorialId, bool bypassLocks = false)
    {
        var progress = await _context.UserLessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TutorialId == tutorialId);

        var tutorial = await _context.Tutorials.FindAsync(tutorialId);
        if (tutorial == null) return false;

        bool isMarkingCompleted = progress == null || !progress.IsCompleted;
        if (isMarkingCompleted && !bypassLocks)
        {
            var access = await CheckTutorialAccessAsync(userId, tutorialId, bypassLocks: false);
            if (access != null && !access.IsUnlocked)
            {
                return false;
            }
        }

        bool isCompletedNow;

        if (progress == null)
        {
            progress = new UserLessonProgress
            {
                UserId = userId,
                TutorialId = tutorialId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };
            await _context.UserLessonProgresses.AddAsync(progress);
            isCompletedNow = true;

            // Award 20 XP for completing a lesson!
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ExperiencePoints += 20;
            }
        }
        else
        {
            progress.IsCompleted = !progress.IsCompleted;
            progress.CompletedAt = progress.IsCompleted ? DateTime.UtcNow : null;
            isCompletedNow = progress.IsCompleted;

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ExperiencePoints += isCompletedNow ? 20 : -20;
                if (user.ExperiencePoints < 0) user.ExperiencePoints = 0;
            }
        }

        await _context.SaveChangesAsync();

        if (isCompletedNow)
        {
            await _streakService.RecordLearningActivityStreakAsync(userId);

            try
            {
                await _notificationService.CreateNotificationAsync(
                    userId,
                    "Hoàn thành bài học! 🎉",
                    $"Bạn đã hoàn thành bài học '{tutorial.Title}' (+20 XP).",
                    NotificationType.LessonCompleted,
                    $"/bai-hoc/{tutorial.Slug}");

                await _activityFeedService.RecordActivityAsync(
                    userId,
                    ActivityType.LessonCompleted,
                    "đã hoàn thành bài học",
                    tutorial.Title,
                    $"/bai-hoc/{tutorial.Slug}",
                    xpEarned: 20);
            }
            catch { }
        }

        // Update CourseEnrollment progress percentage
        await UpdateCourseEnrollmentAsync(userId, tutorial.CategoryId);

        return isCompletedNow;
    }

    public async Task<bool> IsLessonCompletedAsync(string userId, int tutorialId)
    {
        return await _context.UserLessonProgresses
            .AnyAsync(p => p.UserId == userId && p.TutorialId == tutorialId && p.IsCompleted);
    }

    public async Task<List<int>> GetCompletedLessonIdsAsync(string userId)
    {
        return await _context.UserLessonProgresses
            .Where(p => p.UserId == userId && p.IsCompleted)
            .Select(p => p.TutorialId)
            .ToListAsync();
    }

    public async Task RecordLessonAccessAsync(string userId, int tutorialId)
    {
        var progress = await _context.UserLessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TutorialId == tutorialId);

        if (progress != null)
        {
            progress.LastAccessedAt = DateTime.UtcNow;
        }
        else
        {
            progress = new UserLessonProgress
            {
                UserId = userId,
                TutorialId = tutorialId,
                IsCompleted = false,
                LastAccessedAt = DateTime.UtcNow
            };
            await _context.UserLessonProgresses.AddAsync(progress);
        }

        var tutorial = await _context.Tutorials.FindAsync(tutorialId);
        if (tutorial != null)
        {
            // Ensure enrollment exists
            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CategoryId == tutorial.CategoryId);

            if (enrollment == null)
            {
                enrollment = new CourseEnrollment
                {
                    UserId = userId,
                    CategoryId = tutorial.CategoryId,
                    EnrolledAt = DateTime.UtcNow,
                    ProgressPercentage = 0
                };
                await _context.CourseEnrollments.AddAsync(enrollment);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Tutorial?> GetLastAccessedTutorialAsync(string userId)
    {
        var latest = await _context.UserLessonProgresses
            .Include(p => p.Tutorial)
                .ThenInclude(t => t!.Category)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.LastAccessedAt)
            .FirstOrDefaultAsync();

        return latest?.Tutorial;
    }

    public async Task<StudentProfileViewModel> GetStudentProfileAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId) 
            ?? throw new InvalidOperationException("Không tìm thấy người dùng");

        var allCategories = await _context.Categories
            .Include(c => c.Tutorials)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        var completedProgresses = await _context.UserLessonProgresses
            .Include(p => p.Tutorial)
            .Where(p => p.UserId == userId && p.IsCompleted)
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();

        var completedIds = completedProgresses.Select(p => p.TutorialId).ToHashSet();

        var categoryProgresses = new List<CategoryProgressItem>();
        foreach (var cat in allCategories)
        {
            int completedCount = cat.Tutorials.Count(t => completedIds.Contains(t.Id));
            categoryProgresses.Add(new CategoryProgressItem
            {
                Category = cat,
                CompletedCount = completedCount,
                TotalCount = cat.Tutorials.Count
            });
        }

        var lastTutorial = await GetLastAccessedTutorialAsync(userId);
        int totalLessons = await _context.Tutorials.CountAsync();

        var certificates = await _context.Certificates
            .Include(c => c.Category)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

        var recentAttempts = await _context.QuizAttempts
            .Include(a => a.Category)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt)
            .Take(8)
            .ToListAsync();

        return new StudentProfileViewModel
        {
            User = user,
            TotalCompletedLessons = completedIds.Count,
            TotalLessons = totalLessons,
            CategoryProgresses = categoryProgresses,
            RecentCompletedLessons = completedProgresses.Take(6).ToList(),
            LastAccessedTutorial = lastTutorial,
            Certificates = certificates,
            RecentQuizAttempts = recentAttempts
        };
    }

    public async Task<UserStageProgressionSnapshot> GetStageProgressionAsync(string? userId, bool bypassLocks = false)
    {
        bool isAuthenticated = !string.IsNullOrWhiteSpace(userId);

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.OrderIndex,
                c.Slug,
                c.Name,
                Tutorials = c.Tutorials
                    .OrderBy(t => t.OrderIndex)
                    .ThenBy(t => t.Id)
                    .Select(t => new { t.Id, t.Slug })
                    .ToList()
            })
            .ToListAsync();

        var completedIds = isAuthenticated
            ? (await _context.UserLessonProgresses
                .Where(p => p.UserId == userId && p.IsCompleted)
                .Select(p => p.TutorialId)
                .ToListAsync())
                .ToHashSet()
            : new HashSet<int>();

        var stagesByCategoryId = new Dictionary<int, StageUnlockStatus>();
        var unlockedCategoryIds = new HashSet<int>();

        int? blockingCategoryId = null;
        string? blockingSlug = null;
        string? blockingTitleVi = null;
        string? blockingTitleEn = null;
        int blockingRemaining = 0;
        string? nextPrereqSlug = null;

        bool isFirstNonEmptyStage = true;

        foreach (var cat in categories)
        {
            int total = cat.Tutorials.Count;
            int completed = cat.Tutorials.Count(t => completedIds.Contains(t.Id));
            bool stageCompleted = total > 0 && completed >= total;

            var helperCat = new Category { Id = cat.Id, Slug = cat.Slug, Name = cat.Name, OrderIndex = cat.OrderIndex };
            string titleVi = cat.Name;
            string titleEn = helperCat.GetTitle(true);

            bool isUnlocked;
            if (bypassLocks || isFirstNonEmptyStage && total > 0 || blockingCategoryId == null)
            {
                isUnlocked = true;
            }
            else
            {
                isUnlocked = false;
            }

            if (total > 0 && isFirstNonEmptyStage)
            {
                isFirstNonEmptyStage = false;
            }

            var status = new StageUnlockStatus(
                CategoryId: cat.Id,
                OrderIndex: cat.OrderIndex,
                Slug: cat.Slug,
                TitleVi: titleVi,
                TitleEn: titleEn,
                CompletedCount: completed,
                TotalCount: total,
                IsUnlocked: isUnlocked,
                IsCompleted: stageCompleted,
                BlockingCategoryId: isUnlocked ? null : blockingCategoryId,
                BlockingCategorySlug: isUnlocked ? null : blockingSlug,
                BlockingStageTitleVi: isUnlocked ? null : blockingTitleVi,
                BlockingStageTitleEn: isUnlocked ? null : blockingTitleEn,
                BlockingRemainingLessons: isUnlocked ? 0 : blockingRemaining,
                NextPrerequisiteTutorialSlug: isUnlocked ? null : nextPrereqSlug);

            stagesByCategoryId[cat.Id] = status;
            if (isUnlocked)
            {
                unlockedCategoryIds.Add(cat.Id);
            }

            // First incomplete non-empty stage becomes the blocker for all subsequent stages
            if (!bypassLocks && total > 0 && !stageCompleted && blockingCategoryId == null)
            {
                blockingCategoryId = cat.Id;
                blockingSlug = cat.Slug;
                blockingTitleVi = titleVi;
                blockingTitleEn = titleEn;
                blockingRemaining = Math.Max(0, total - completed);
                nextPrereqSlug = cat.Tutorials.FirstOrDefault(t => !completedIds.Contains(t.Id))?.Slug
                              ?? cat.Tutorials.FirstOrDefault()?.Slug;
            }
        }

        return new UserStageProgressionSnapshot(
            IsAuthenticated: isAuthenticated,
            StagesByCategoryId: stagesByCategoryId,
            CompletedTutorialIds: completedIds,
            UnlockedCategoryIds: unlockedCategoryIds);
    }

    public async Task<StageUnlockStatus?> CheckTutorialAccessAsync(string? userId, int tutorialId, bool bypassLocks = false)
    {
        var tutorial = await _context.Tutorials
            .AsNoTracking()
            .Select(t => new { t.Id, t.CategoryId })
            .FirstOrDefaultAsync(t => t.Id == tutorialId);

        if (tutorial == null) return null;

        var progression = await GetStageProgressionAsync(userId, bypassLocks);
        return progression.StagesByCategoryId.TryGetValue(tutorial.CategoryId, out var status)
            ? status
            : null;
    }

    private async Task UpdateCourseEnrollmentAsync(string userId, int categoryId)
    {
        var totalCategoryLessons = await _context.Tutorials.CountAsync(t => t.CategoryId == categoryId);
        if (totalCategoryLessons == 0) return;

        var completedCategoryLessons = await _context.UserLessonProgresses
            .CountAsync(p => p.UserId == userId && p.IsCompleted && p.Tutorial != null && p.Tutorial.CategoryId == categoryId);

        double percentage = Math.Round((double)completedCategoryLessons * 100 / totalCategoryLessons, 1);

        var enrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CategoryId == categoryId);

        if (enrollment == null)
        {
            enrollment = new CourseEnrollment
            {
                UserId = userId,
                CategoryId = categoryId,
                EnrolledAt = DateTime.UtcNow,
                ProgressPercentage = percentage,
                CompletedAt = percentage >= 100 ? DateTime.UtcNow : null
            };
            await _context.CourseEnrollments.AddAsync(enrollment);
        }
        else
        {
            enrollment.ProgressPercentage = percentage;
            if (percentage >= 100 && !enrollment.CompletedAt.HasValue)
            {
                enrollment.CompletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }
}
