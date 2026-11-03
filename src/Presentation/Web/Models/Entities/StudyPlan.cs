using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NET_Tutos.Models.Entities;

public enum StudyPlanTrack
{
    FastTrack = 1,        // Intensive pace (1 month, ~3-4 lessons/day)
    Standard = 2,         // Standard pace (3 months, ~1-2 lessons/day)
    SteadyPartTime = 3,   // Part-time steady pace (6 months, ~3-4 lessons/week)
    Custom = 4            // Custom personalized schedule
}

public enum StudyPlanFocus
{
    All101Lessons = 1,          // Comprehensive 101 C# & .NET Core lessons
    BackendApiSpecialist = 2,   // Deep-dive Backend Web API & EF Core track
    FundamentalsOnly = 3        // C# Core & OOP Fundamentals (Parts 1 & 2)
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
