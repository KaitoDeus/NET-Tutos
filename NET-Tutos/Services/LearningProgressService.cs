using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class LearningProgressService : ILearningProgressService
{
    private readonly AppDbContext _context;

    public LearningProgressService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ToggleLessonCompletedAsync(string userId, int tutorialId)
    {
        var progress = await _context.UserLessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TutorialId == tutorialId);

        var tutorial = await _context.Tutorials.FindAsync(tutorialId);
        if (tutorial == null) return false;

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
