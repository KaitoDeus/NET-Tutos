using System.ComponentModel.DataAnnotations;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalTutorials { get; set; }
    public int TotalCategories { get; set; }
    public int TotalQuizQuestions { get; set; }
    public int TotalExamsTaken { get; set; }
    public int TotalCertificatesIssued { get; set; }
    public double ExamPassRate { get; set; }
    public List<ApplicationUser> TopStudentsByXp { get; set; } = new();
    public List<QuizAttempt> RecentExams { get; set; } = new();
    public List<Certificate> RecentCertificates { get; set; } = new();
}

public class TutorialEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [MaxLength(250)]
    [Display(Name = "Tiêu đề bài học")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn Slug không được để trống")]
    [MaxLength(250)]
    [Display(Name = "Đường dẫn tĩnh (Slug)")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tóm tắt không được để trống")]
    [MaxLength(500)]
    [Display(Name = "Mô tả ngắn gọn (Tóm tắt)")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung bài học không được để trống")]
    [Display(Name = "Nội dung bài học (Markdown)")]
    public string ContentMarkdown { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn chuyên đề")]
    [Display(Name = "Chuyên đề khóa học")]
    public int CategoryId { get; set; }

    [Display(Name = "Độ khó")]
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    [Display(Name = "Thời gian đọc (phút)")]
    [Range(1, 180)]
    public int EstimatedReadingMinutes { get; set; } = 10;

    [Display(Name = "Thứ tự sắp xếp")]
    public int OrderIndex { get; set; } = 1;

    [Display(Name = "Bài học nổi bật trên trang chủ")]
    public bool IsFeatured { get; set; }

    public IEnumerable<Category> AvailableCategories { get; set; } = new List<Category>();
}

public class QuizQuestionEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Gắn với bài học cụ thể")]
    public int? TutorialId { get; set; }

    [Required(ErrorMessage = "Câu hỏi không được để trống")]
    [MaxLength(1000)]
    [Display(Name = "Nội dung câu hỏi")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đáp án A không được để trống")]
    [MaxLength(500)]
    [Display(Name = "Lựa chọn A")]
    public string OptionA { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đáp án B không được để trống")]
    [MaxLength(500)]
    [Display(Name = "Lựa chọn B")]
    public string OptionB { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đáp án C không được để trống")]
    [MaxLength(500)]
    [Display(Name = "Lựa chọn C")]
    public string OptionC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đáp án D không được để trống")]
    [MaxLength(500)]
    [Display(Name = "Lựa chọn D")]
    public string OptionD { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn đáp án chính xác")]
    [Display(Name = "Đáp án chính xác (A, B, C hoặc D)")]
    public string CorrectOption { get; set; } = "A";

    [MaxLength(1000)]
    [Display(Name = "Giải thích chi tiết (khi học viên trả lời)")]
    public string Explanation { get; set; } = string.Empty;

    [Display(Name = "Độ khó")]
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    public IEnumerable<Tutorial> AvailableTutorials { get; set; } = new List<Tutorial>();
}

public class UserManagementItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int ExperiencePoints { get; set; }
    public int CompletedLessonsCount { get; set; }
    public int CertificatesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserManagementViewModel
{
    public List<UserManagementItem> Users { get; set; } = new();
}
