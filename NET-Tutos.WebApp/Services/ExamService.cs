using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class ExamService : IExamService
{
    private readonly AppDbContext _context;
    private readonly ICertificateService _certificateService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExamService(
        AppDbContext context, 
        ICertificateService certificateService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _certificateService = certificateService;
        _userManager = userManager;
    }

    public async Task<ExamListViewModel> GetExamListAsync(string? userId)
    {
        var categories = await _context.Categories
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        var examTopics = new List<ExamTopicItem>();

        // Category-specific exams
        foreach (var cat in categories)
        {
            var qCount = await _context.QuizQuestions
                .CountAsync(q => q.Tutorial != null && q.Tutorial.CategoryId == cat.Id);

            var titleEn = $"Exam: {cat.GetTitle(true)}";
            var descEn = $"Comprehensive competency exam for {cat.GetTitle(true)}. Includes randomized timed multiple-choice questions.";

            var topic = new ExamTopicItem
            {
                CategoryId = cat.Id,
                Title = $"Kỳ thi: {cat.Name}",
                Description = $"Đánh giá toàn diện kiến thức của chuyên đề {cat.Name}. Bao gồm các câu hỏi trắc nghiệm xáo trộn và tính giờ.",
                TitleEn = titleEn,
                DescriptionEn = descEn,
                BadgeColor = cat.BadgeColor,
                IconClass = cat.IconClass,
                QuestionCount = Math.Min(10, Math.Max(5, qCount)),
                DurationMinutes = 15,
                PassingScorePercent = 80
            };

            if (!string.IsNullOrEmpty(userId))
            {
                var attempts = await _context.QuizAttempts
                    .Where(a => a.UserId == userId && a.CategoryId == cat.Id)
                    .ToListAsync();

                if (attempts.Any())
                {
                    topic.BestScore = attempts.Max(a => a.ScorePercentage);
                    topic.HasPassed = attempts.Any(a => a.IsPassed);
                }

                topic.Certificate = await _certificateService.GetUserCertificateForCategoryAsync(userId, cat.Id);
            }

            examTopics.Add(topic);
        }

        // Comprehensive graduation exam
        var totalQuestions = await _context.QuizQuestions.CountAsync();
        var generalTopic = new ExamTopicItem
        {
            CategoryId = null,
            Title = "Kỳ thi Đánh giá Năng lực .NET Toàn diện",
            Description = "Bài thi tổng hợp kiến thức từ C# cơ bản, OOP, Entity Framework Core đến ASP.NET Core MVC. Đạt điểm để nhận chứng chỉ danh dự cấp toàn khóa.",
            TitleEn = "Comprehensive .NET Full-Stack Assessment Exam",
            DescriptionEn = "Comprehensive final exam covering C# fundamentals, OOP, Entity Framework Core, and ASP.NET Core MVC. Pass to earn your verified full-course diploma.",
            BadgeColor = "primary",
            IconClass = "bi-award-fill",
            QuestionCount = Math.Min(15, totalQuestions),
            DurationMinutes = 20,
            PassingScorePercent = 80
        };

        if (!string.IsNullOrEmpty(userId))
        {
            var generalAttempts = await _context.QuizAttempts
                .Where(a => a.UserId == userId && a.CategoryId == null)
                .ToListAsync();

            if (generalAttempts.Any())
            {
                generalTopic.BestScore = generalAttempts.Max(a => a.ScorePercentage);
                generalTopic.HasPassed = generalAttempts.Any(a => a.IsPassed);
            }

            generalTopic.Certificate = await _certificateService.GetUserCertificateForCategoryAsync(userId, null);
        }

        examTopics.Add(generalTopic);

        var recentAttempts = !string.IsNullOrEmpty(userId)
            ? await _context.QuizAttempts
                .Include(a => a.Category)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CompletedAt)
                .Take(10)
                .ToListAsync()
            : new List<QuizAttempt>();

        var userCertificates = !string.IsNullOrEmpty(userId)
            ? (await _certificateService.GetUserCertificatesAsync(userId)).ToList()
            : new List<Certificate>();

        return new ExamListViewModel
        {
            AvailableExams = examTopics,
            RecentAttempts = recentAttempts,
            Certificates = userCertificates
        };
    }

    public async Task<ExamSessionViewModel?> StartExamSessionAsync(string userId, int? categoryId)
    {
        string examTitle = "Kỳ thi Đánh giá Năng lực .NET Toàn diện";
        string examTitleEn = "Comprehensive .NET Full-Stack Assessment Exam";
        int durationMinutes = 20;
        int targetQuestions = 15;

        IQueryable<QuizQuestion> query = _context.QuizQuestions
            .Include(q => q.Tutorial);

        if (categoryId.HasValue)
        {
            var cat = await _context.Categories.FindAsync(categoryId.Value);
            if (cat == null) return null;

            examTitle = $"Kỳ thi: {cat.Name}";
            examTitleEn = $"Exam: {cat.GetTitle(true)}";
            durationMinutes = 15;
            targetQuestions = 10;
            query = query.Where(q => q.Tutorial != null && q.Tutorial.CategoryId == categoryId.Value);
        }

        var allQuestions = await query.ToListAsync();
        if (!allQuestions.Any()) return null;

        // Randomize questions
        var random = new Random();
        var selectedQuestions = allQuestions
            .OrderBy(_ => random.Next())
            .Take(targetQuestions)
            .Select(q => new ExamQuestionItem
            {
                Id = q.Id,
                Question = q.Question,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD
            })
            .ToList();

        return new ExamSessionViewModel
        {
            CategoryId = categoryId,
            ExamTitle = examTitle,
            ExamTitleEn = examTitleEn,
            DurationMinutes = durationMinutes,
            Questions = selectedQuestions
        };
    }

    public async Task<ExamResultViewModel> EvaluateExamAsync(string userId, ExamSubmissionViewModel submission, string hostUrl)
    {
        var questionIds = submission.Answers.Keys.ToList();
        var questionsFromDb = await _context.QuizQuestions
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id);

        int totalQuestions = questionIds.Count;
        int correctCount = 0;
        var resultItems = new List<ExamQuestionResultItem>();

        foreach (var (qId, selectedOpt) in submission.Answers)
        {
            if (questionsFromDb.TryGetValue(qId, out var q))
            {
                bool isCorrect = q.CorrectOption.Equals(selectedOpt, StringComparison.OrdinalIgnoreCase);
                if (isCorrect) correctCount++;

                resultItems.Add(new ExamQuestionResultItem
                {
                    Id = q.Id,
                    Question = q.Question,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    SelectedOption = selectedOpt,
                    CorrectOption = q.CorrectOption,
                    Explanation = q.Explanation
                });
            }
        }

        double scorePercentage = totalQuestions > 0 
            ? Math.Round(((double)correctCount / totalQuestions) * 100.0, 1) 
            : 0;

        bool isPassed = scorePercentage >= 80.0;

        var attempt = new QuizAttempt
        {
            UserId = userId,
            CategoryId = submission.CategoryId,
            ExamTitle = string.IsNullOrWhiteSpace(submission.ExamTitle) ? "Bài thi đánh giá" : submission.ExamTitle,
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctCount,
            ScorePercentage = scorePercentage,
            IsPassed = isPassed,
            DurationSeconds = submission.ElapsedSeconds,
            StartedAt = DateTime.UtcNow.AddSeconds(-submission.ElapsedSeconds),
            CompletedAt = DateTime.UtcNow
        };

        _context.QuizAttempts.Add(attempt);

        // Update user XP: +30 XP for test attempt, +50 XP if passed
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.ExperiencePoints += 30;
            if (isPassed)
            {
                user.ExperiencePoints += 50;
            }
        }

        Certificate? cert = null;
        if (isPassed)
        {
            cert = await _certificateService.IssueCertificateAsync(userId, submission.CategoryId, scorePercentage, hostUrl);
        }

        await _context.SaveChangesAsync();

        return new ExamResultViewModel
        {
            AttemptId = attempt.Id,
            ExamTitle = attempt.ExamTitle,
            ExamTitleEn = LocalizationHelper.TranslateExamTitle(attempt.ExamTitle, true),
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctCount,
            ScorePercentage = scorePercentage,
            IsPassed = isPassed,
            DurationSeconds = submission.ElapsedSeconds,
            Certificate = cert,
            QuestionResults = resultItems
        };
    }

    public async Task<ExamResultViewModel?> GetExamAttemptResultAsync(int attemptId, string userId)
    {
        var attempt = await _context.QuizAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

        if (attempt == null) return null;

        var cert = attempt.IsPassed
            ? await _certificateService.GetUserCertificateForCategoryAsync(userId, attempt.CategoryId)
            : null;

        return new ExamResultViewModel
        {
            AttemptId = attempt.Id,
            ExamTitle = attempt.ExamTitle,
            ExamTitleEn = LocalizationHelper.TranslateExamTitle(attempt.ExamTitle, true),
            TotalQuestions = attempt.TotalQuestions,
            CorrectAnswers = attempt.CorrectAnswers,
            ScorePercentage = attempt.ScorePercentage,
            IsPassed = attempt.IsPassed,
            DurationSeconds = attempt.DurationSeconds,
            Certificate = cert
        };
    }
}
