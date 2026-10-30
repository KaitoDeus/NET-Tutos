using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NET_Tutos.Models.Entities;

public enum StudyPlanTrack
{
    FastTrack = 1,        // Cấp tốc (1 tháng, ~3-4 bài/ngày)
    Standard = 2,         // Chuẩn mực (3 tháng, ~1-2 bài/ngày)
    SteadyPartTime = 3,   // Vừa học vừa làm (6 tháng, ~3-4 bài/tuần)
    Custom = 4            // Tùy chỉnh cá nhân
}

public enum StudyPlanFocus
{
    All101Lessons = 1,          // Toàn diện 101 bài học C# & .NET Core
    BackendApiSpecialist = 2,   // Chuyên sâu Backend Web API & EF Core
    FundamentalsOnly = 3        // Nền tảng C# Core & OOP (Phần 1 & 2)
}

public class StudyPlan
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public StudyPlanTrack TrackType { get; set; } = StudyPlanTrack.Standard;

    public StudyPlanFocus FocusType { get; set; } = StudyPlanFocus.All101Lessons;

    public int TargetMinutesPerDay { get; set; } = 30;

    [MaxLength(100)]
    public string SelectedDaysOfWeek { get; set; } = "1,2,3,4,5"; // Comma-separated DayOfWeek (1=Mon, 2=Tue...)

    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    public DateTime TargetEndDate { get; set; } = DateTime.UtcNow.Date.AddDays(90);

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public ICollection<StudyPlanItem> PlanItems { get; set; } = new List<StudyPlanItem>();
}

public class StudyPlanItem
{
    public int Id { get; set; }

    public int StudyPlanId { get; set; }

    [ForeignKey(nameof(StudyPlanId))]
    public StudyPlan? StudyPlan { get; set; }

    public int LessonNumber { get; set; } // Matches 1..101 in Curriculum

    public DateTime ScheduledDate { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
