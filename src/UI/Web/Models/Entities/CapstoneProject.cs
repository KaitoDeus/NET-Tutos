using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public enum ProjectSubmissionStatus
{
    Submitted = 0,        // Đã nộp, chờ giảng viên chấm
    UnderReview = 1,      // Giảng viên đang xem xét
    Approved = 2,         // Đạt chuẩn - Đã duyệt thành công
    NeedsImprovement = 3  // Cần bổ sung / chỉnh sửa lại theo góp ý
}

public class CapstoneProject
{
    public int Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public string FullContentMarkdown { get; set; } = string.Empty;

    [Required]
    public string RequirementsMarkdown { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string TechStack { get; set; } = string.Empty;

    public DifficultyLevel Level { get; set; } = DifficultyLevel.Intermediate;

    public int EstimatedHours { get; set; } = 15;

    public int RewardXp { get; set; } = 200;

    [MaxLength(500)]
    public string? GitHubTemplateUrl { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int OrderIndex { get; set; } = 0;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProjectSubmission> Submissions { get; set; } = new List<ProjectSubmission>();
}

public class ProjectSubmission
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public CapstoneProject? Project { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(500)]
    public string GitHubRepoUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LiveDemoUrl { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;

    public ProjectSubmissionStatus Status { get; set; } = ProjectSubmissionStatus.Submitted;

    public int? Score { get; set; }

    public string? ReviewerFeedback { get; set; }

    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? Reviewer { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public int XpAwarded { get; set; } = 0;
}
