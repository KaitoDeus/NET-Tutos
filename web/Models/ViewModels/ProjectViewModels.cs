using System.ComponentModel.DataAnnotations;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class ProjectCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string TechStack { get; set; } = string.Empty;
    public DifficultyLevel Level { get; set; }
    public int EstimatedHours { get; set; }
    public int RewardXp { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryBadgeColor { get; set; } = "primary";
    public int TotalSubmissions { get; set; }
    public ProjectSubmissionStatus? UserSubmissionStatus { get; set; }
    public int? UserSubmissionScore { get; set; }
}

public class ProjectListViewModel
{
    public List<ProjectCardViewModel> Projects { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
    public DifficultyLevel? SelectedLevel { get; set; }
    public int TotalProjects { get; set; }
    public int UserCompletedCount { get; set; }
    public int UserPendingCount { get; set; }
    public int UserTotalEarnedXp { get; set; }
}

public class ProjectDetailsViewModel
{
    public CapstoneProject Project { get; set; } = null!;
    public string RequirementsHtml { get; set; } = string.Empty;
    public string FullContentHtml { get; set; } = string.Empty;
    public ProjectSubmission? UserSubmission { get; set; }
    public bool IsAuthenticated { get; set; }
    public int TotalSubmissionsCount { get; set; }
    public int ApprovedSubmissionsCount { get; set; }
}

public class SubmitProjectInputModel
{
    [Required]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đường dẫn kho lưu trữ GitHub của dự án.")]
    [Url(ErrorMessage = "Đường dẫn GitHub không hợp lệ.")]
    [MaxLength(500)]
    [Display(Name = "GitHub Repository URL")]
    public string GitHubRepoUrl { get; set; } = string.Empty;

    [Url(ErrorMessage = "Đường dẫn Live Demo không hợp lệ.")]
    [MaxLength(500)]
    [Display(Name = "Live Demo URL (Tùy chọn)")]
    public string? LiveDemoUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng ghi chú tóm tắt kiến trúc, công nghệ và các điểm nổi bật của dự án.")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Ghi chú kiến trúc phải từ 20 đến 2000 ký tự.")]
    [Display(Name = "Ghi chú kiến trúc & Báo cáo kỹ thuật")]
    public string Notes { get; set; } = string.Empty;
}

public class MyProjectsViewModel
{
    public List<ProjectSubmission> Submissions { get; set; } = new();
    public int TotalSubmissions => Submissions.Count;
    public int ApprovedCount => Submissions.Count(s => s.Status == ProjectSubmissionStatus.Approved);
    public int TotalXpEarned => Submissions.Where(s => s.Status == ProjectSubmissionStatus.Approved).Sum(s => s.XpAwarded);
}

public class AdminProjectSubmissionsViewModel
{
    public List<ProjectSubmission> Submissions { get; set; } = new();
    public ProjectSubmissionStatus? SelectedStatus { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int NeedsImprovementCount { get; set; }
}

public class AdminReviewInputModel
{
    [Required]
    public int SubmissionId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập điểm đánh giá (0-100).")]
    [Range(0, 100, ErrorMessage = "Điểm số phải từ 0 đến 100.")]
    public int Score { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái đánh giá.")]
    public ProjectSubmissionStatus Status { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nhận xét chi tiết (Code Review, Clean Architecture, Bảo mật, Góp ý).")]
    [MinLength(10, ErrorMessage = "Nhận xét phải có ít nhất 10 ký tự.")]
    public string ReviewerFeedback { get; set; } = string.Empty;

    [Range(0, 500, ErrorMessage = "Điểm thưởng XP phải từ 0 đến 500.")]
    public int BonusXp { get; set; } = 0;
}
