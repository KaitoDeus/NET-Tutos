using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class CapstoneProjectService : ICapstoneProjectService
{
    private readonly AppDbContext _context;
    private readonly IMarkdownService _markdownService;
    private readonly INotificationService _notificationService;
    private readonly IActivityFeedService _activityFeedService;
    private readonly IStreakService _streakService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CapstoneProjectService> _logger;

    public CapstoneProjectService(
        AppDbContext context,
        IMarkdownService markdownService,
        INotificationService notificationService,
        IActivityFeedService activityFeedService,
        IStreakService streakService,
        UserManager<ApplicationUser> userManager,
        ILogger<CapstoneProjectService> logger)
    {
        _context = context;
        _markdownService = markdownService;
        _notificationService = notificationService;
        _activityFeedService = activityFeedService;
        _streakService = streakService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ProjectListViewModel> GetProjectsListAsync(int? categoryId = null, DifficultyLevel? level = null, string? userId = null)
    {
        var query = _context.CapstoneProjects
            .Include(p => p.Category)
            .Include(p => p.Submissions)
            .Where(p => p.IsPublished);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (level.HasValue)
        {
            query = query.Where(p => p.Level == level.Value);
        }

        var projects = await query
            .OrderBy(p => p.OrderIndex)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        var categories = await _context.Categories
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        List<ProjectSubmission> userSubmissions = new();
        if (!string.IsNullOrEmpty(userId))
        {
            userSubmissions = await _context.ProjectSubmissions
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        var cards = projects.Select(p =>
        {
            var userSub = userSubmissions.FirstOrDefault(s => s.ProjectId == p.Id);
            return new ProjectCardViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                ShortDescription = p.ShortDescription,
                TechStack = p.TechStack,
                Level = p.Level,
                EstimatedHours = p.EstimatedHours,
                RewardXp = p.RewardXp,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "Chung",
                CategoryBadgeColor = p.Category?.BadgeColor ?? "primary",
                TotalSubmissions = p.Submissions.Count,
                UserSubmissionStatus = userSub?.Status,
                UserSubmissionScore = userSub?.Score
            };
        }).ToList();

        int completedCount = userSubmissions.Count(s => s.Status == ProjectSubmissionStatus.Approved);
        int pendingCount = userSubmissions.Count(s => s.Status == ProjectSubmissionStatus.Submitted || s.Status == ProjectSubmissionStatus.UnderReview);
        int totalEarnedXp = userSubmissions.Where(s => s.Status == ProjectSubmissionStatus.Approved).Sum(s => s.XpAwarded);

        return new ProjectListViewModel
        {
            Projects = cards,
            Categories = categories,
            SelectedCategoryId = categoryId,
            SelectedLevel = level,
            TotalProjects = projects.Count,
            UserCompletedCount = completedCount,
            UserPendingCount = pendingCount,
            UserTotalEarnedXp = totalEarnedXp
        };
    }

    public async Task<ProjectDetailsViewModel?> GetProjectDetailsAsync(string slug, string? userId = null)
    {
        var project = await _context.CapstoneProjects
            .Include(p => p.Category)
            .Include(p => p.Submissions)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (project == null) return null;

        ProjectSubmission? userSubmission = null;
        if (!string.IsNullOrEmpty(userId))
        {
            userSubmission = await _context.ProjectSubmissions
                .Include(s => s.Reviewer)
                .FirstOrDefaultAsync(s => s.ProjectId == project.Id && s.UserId == userId);
        }

        var reqHtml = _markdownService.ToHtml(project.RequirementsMarkdown);
        var contentHtml = _markdownService.ToHtml(project.FullContentMarkdown);

        return new ProjectDetailsViewModel
        {
            Project = project,
            RequirementsHtml = reqHtml,
            FullContentHtml = contentHtml,
            UserSubmission = userSubmission,
            IsAuthenticated = !string.IsNullOrEmpty(userId),
            TotalSubmissionsCount = project.Submissions.Count,
            ApprovedSubmissionsCount = project.Submissions.Count(s => s.Status == ProjectSubmissionStatus.Approved)
        };
    }

    public async Task<CapstoneProject?> GetProjectByIdAsync(int id)
    {
        return await _context.CapstoneProjects
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ProjectSubmission?> GetUserSubmissionAsync(int projectId, string userId)
    {
        return await _context.ProjectSubmissions
            .Include(s => s.Project)
            .Include(s => s.Reviewer)
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.UserId == userId);
    }

    public async Task<MyProjectsViewModel> GetUserProjectsAsync(string userId)
    {
        var submissions = await _context.ProjectSubmissions
            .Include(s => s.Project)
                .ThenInclude(p => p!.Category)
            .Include(s => s.Reviewer)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return new MyProjectsViewModel
        {
            Submissions = submissions
        };
    }

    public async Task<(bool Success, string Message, int SubmissionId)> SubmitProjectAsync(string userId, SubmitProjectInputModel input)
    {
        var project = await _context.CapstoneProjects.FindAsync(input.ProjectId);
        if (project == null || !project.IsPublished)
        {
            return (false, "Đồ án không tồn tại hoặc chưa được công khai.", 0);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "Người dùng không hợp lệ.", 0);
        }

        var existingSubmission = await _context.ProjectSubmissions
            .FirstOrDefaultAsync(s => s.ProjectId == input.ProjectId && s.UserId == userId);

        if (existingSubmission != null)
        {
            if (existingSubmission.Status == ProjectSubmissionStatus.Approved)
            {
                return (false, "Đồ án này đã được đánh giá Đạt Chuẩn (Approved). Bạn không cần nộp lại.", existingSubmission.Id);
            }

            // Update submission
            existingSubmission.GitHubRepoUrl = input.GitHubRepoUrl.Trim();
            existingSubmission.LiveDemoUrl = string.IsNullOrWhiteSpace(input.LiveDemoUrl) ? null : input.LiveDemoUrl.Trim();
            existingSubmission.Notes = input.Notes.Trim();
            existingSubmission.Status = ProjectSubmissionStatus.Submitted;
            existingSubmission.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Record streak & notification
            await _streakService.RecordLearningActivityStreakAsync(userId);

            await _notificationService.CreateNotificationAsync(
                userId,
                "Cập nhật bài nộp đồ án thành công 🚀",
                $"Bạn đã cập nhật thông tin nộp bài cho đồ án '{project.Title}'. Giảng viên sẽ sớm xem xét và review mã nguồn.",
                NotificationType.ProjectSubmitted,
                $"/Project/Details/{project.Slug}");

            return (true, "Cập nhật bài nộp đồ án thành công! Giảng viên sẽ sớm phản hồi.", existingSubmission.Id);
        }

        // Create new submission
        var newSubmission = new ProjectSubmission
        {
            ProjectId = input.ProjectId,
            UserId = userId,
            GitHubRepoUrl = input.GitHubRepoUrl.Trim(),
            LiveDemoUrl = string.IsNullOrWhiteSpace(input.LiveDemoUrl) ? null : input.LiveDemoUrl.Trim(),
            Notes = input.Notes.Trim(),
            Status = ProjectSubmissionStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProjectSubmissions.Add(newSubmission);
        await _context.SaveChangesAsync();

        // Record streak
        await _streakService.RecordLearningActivityStreakAsync(userId);

        // Notify student
        await _notificationService.CreateNotificationAsync(
            userId,
            "Nộp đồ án thực chiến thành công! 🚀",
            $"Bạn đã nộp thành công đồ án '{project.Title}'. Giảng viên sẽ tiến hành xem xét, chấm điểm và để lại code review trong thời gian sớm nhất.",
            NotificationType.ProjectSubmitted,
            $"/Project/Details/{project.Slug}");

        return (true, "Nộp đồ án thành công! Giảng viên sẽ tiến hành chấm điểm và gửi nhận xét sớm nhất.", newSubmission.Id);
    }

    public async Task<AdminProjectSubmissionsViewModel> GetAdminSubmissionsAsync(ProjectSubmissionStatus? status = null)
    {
        var query = _context.ProjectSubmissions
            .Include(s => s.Project)
            .Include(s => s.User)
            .Include(s => s.Reviewer)
            .AsQueryable();

        int pendingCount = await _context.ProjectSubmissions.CountAsync(s => s.Status == ProjectSubmissionStatus.Submitted || s.Status == ProjectSubmissionStatus.UnderReview);
        int approvedCount = await _context.ProjectSubmissions.CountAsync(s => s.Status == ProjectSubmissionStatus.Approved);
        int needsImprovementCount = await _context.ProjectSubmissions.CountAsync(s => s.Status == ProjectSubmissionStatus.NeedsImprovement);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var submissions = await query
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return new AdminProjectSubmissionsViewModel
        {
            Submissions = submissions,
            SelectedStatus = status,
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            NeedsImprovementCount = needsImprovementCount
        };
    }

    public async Task<ProjectSubmission?> GetSubmissionByIdAsync(int submissionId)
    {
        return await _context.ProjectSubmissions
            .Include(s => s.Project)
                .ThenInclude(p => p!.Category)
            .Include(s => s.User)
            .Include(s => s.Reviewer)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
    }

    public async Task<(bool Success, string Message)> ReviewSubmissionAsync(string reviewerId, AdminReviewInputModel input)
    {
        var submission = await _context.ProjectSubmissions
            .Include(s => s.Project)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == input.SubmissionId);

        if (submission == null)
        {
            return (false, "Không tìm thấy bài nộp của học viên.");
        }

        var student = submission.User;
        if (student == null)
        {
            return (false, "Không tìm thấy tài khoản học viên.");
        }

        var project = submission.Project;
        if (project == null)
        {
            return (false, "Không tìm thấy đồ án tương ứng.");
        }

        submission.Status = input.Status;
        submission.Score = input.Score;
        submission.ReviewerFeedback = input.ReviewerFeedback.Trim();
        submission.ReviewedByUserId = reviewerId;
        submission.ReviewedAt = DateTime.UtcNow;

        if (input.Status == ProjectSubmissionStatus.Approved)
        {
            int totalXp = project.RewardXp + input.BonusXp;
            if (submission.XpAwarded == 0)
            {
                submission.XpAwarded = totalXp;
                student.ExperiencePoints += totalXp;
            }

            // Check and award Architect badge
            bool hasArchitectBadge = await _context.UserBadges
                .AnyAsync(b => b.UserId == student.Id && b.BadgeCode == "ARCHITECT");

            if (!hasArchitectBadge)
            {
                var architectBadge = new UserBadge
                {
                    UserId = student.Id,
                    BadgeCode = "ARCHITECT",
                    Title = "Kiến Trúc Sư .NET",
                    Description = "Bảo vệ thành công Đồ án Thực chiến Capstone và được duyệt đạt chuẩn",
                    IconClass = "bi-diagram-3-fill",
                    ColorClass = "success",
                    EarnedAt = DateTime.UtcNow
                };
                _context.UserBadges.Add(architectBadge);

                // Send badge notification
                await _notificationService.CreateNotificationAsync(
                    student.Id,
                    "Mở khóa huy hiệu 'Kiến Trúc Sư .NET'! 🏆",
                    "Chúc mừng bạn đã bảo vệ thành công Đồ án Thực chiến Capstone đầu tiên và đạt danh hiệu Kiến Trúc Sư .NET!",
                    NotificationType.BadgeEarned,
                    "/Leaderboard");

                await _activityFeedService.RecordActivityAsync(
                    student.Id,
                    ActivityType.BadgeEarned,
                    "đã mở khóa huy hiệu",
                    "Kiến Trúc Sư .NET (Bảo vệ thành công Đồ án Thực chiến Capstone)",
                    "/Leaderboard",
                    badgeCode: "ARCHITECT");
            }

            await _context.SaveChangesAsync();

            // Send notification to student
            await _notificationService.CreateNotificationAsync(
                student.Id,
                "Đồ án đạt chuẩn & hoàn thành xuất sắc! 🌟",
                $"Đồ án '{project.Title}' của bạn đã được duyệt với điểm số {input.Score}/100. Bạn được thưởng +{totalXp} XP!",
                NotificationType.ProjectReviewed,
                $"/Project/Details/{project.Slug}");

            // Record community activity feed
            await _activityFeedService.RecordActivityAsync(
                student.Id,
                ActivityType.ProjectApproved,
                "đã hoàn thành xuất sắc đồ án thực chiến",
                $"{project.Title} ({input.Score}/100 điểm) 🎯",
                $"/Project/Details/{project.Slug}",
                xpEarned: totalXp);

            return (true, $"Đã duyệt bài nộp thành công! Học viên được cộng +{totalXp} XP.");
        }
        else
        {
            await _context.SaveChangesAsync();

            // Send improvement requested notification
            await _notificationService.CreateNotificationAsync(
                student.Id,
                "Đồ án cần bổ sung & chỉnh sửa lại 📝",
                $"Giảng viên đã để lại phản hồi Code Review cho đồ án '{project.Title}' ({input.Score}/100 điểm). Vui lòng xem nhận xét và cập nhật lại bài nộp.",
                NotificationType.ProjectReviewed,
                $"/Project/Details/{project.Slug}");

            return (true, "Đã gửi đánh giá yêu cầu hoàn thiện lại cho học viên.");
        }
    }
}
