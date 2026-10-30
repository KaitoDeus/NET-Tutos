using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class CreateStudyPlanRequest
{
    public string Title { get; set; } = "Kế hoạch C# & .NET 10 Làm chủ Công nghệ";
    public StudyPlanTrack TrackType { get; set; } = StudyPlanTrack.Standard;
    public StudyPlanFocus FocusType { get; set; } = StudyPlanFocus.All101Lessons;
    public int TargetMinutesPerDay { get; set; } = 30;
    public List<int> SelectedDays { get; set; } = new() { 1, 2, 3, 4, 5 }; // 1=Mon, 2=Tue...
    public int CustomDurationDays { get; set; } = 90;
}

public class StudyPlanItemDto
{
    public int Id { get; set; }
    public int LessonNumber { get; set; }
    public string LessonTitleVi { get; set; } = string.Empty;
    public string LessonTitleEn { get; set; } = string.Empty;
    public string SectionTitleVi { get; set; } = string.Empty;
    public string SectionTitleEn { get; set; } = string.Empty;
    public string SectionBadgeColor { get; set; } = "primary";
    public string SectionIcon { get; set; } = "bi-laptop";
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;
    public int EstimatedMinutes { get; set; } = 20;
    public DateTime ScheduledDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsToday { get; set; }
    public bool IsOverdue { get; set; }
    public string? MatchingTutorialSlug { get; set; }
    public List<string> KeyTags { get; set; } = new();

    public string GetLessonTitle(bool isEn) => isEn ? LessonTitleEn : LessonTitleVi;
    public string GetSectionTitle(bool isEn) => isEn ? SectionTitleEn : SectionTitleVi;
}

public class StudyPlanWeekGroup
{
    public int WeekNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<StudyPlanItemDto> Items { get; set; } = new();
    public int CompletedCount => Items.Count(i => i.IsCompleted);
    public int TotalCount => Items.Count;
    public bool IsCurrentWeek { get; set; }
    public int ProgressPercentage => TotalCount > 0 ? (int)Math.Round((double)CompletedCount / TotalCount * 100) : 0;
}

public class StudyPlannerDashboardViewModel
{
    public bool HasActivePlan { get; set; }
    public int PlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public StudyPlanTrack TrackType { get; set; }
    public StudyPlanFocus FocusType { get; set; }
    public int TargetMinutesPerDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime TargetEndDate { get; set; }
    public int DaysRemaining { get; set; }
    public int TotalDays { get; set; }

    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int ProgressPercentage { get; set; }

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public bool IsStreakInDanger { get; set; }

    public List<StudyPlanItemDto> TodayLessons { get; set; } = new();
    public List<StudyPlanItemDto> OverdueLessons { get; set; } = new();
    public List<StudyPlanWeekGroup> Weeks { get; set; } = new();

    public bool CanReschedule => OverdueLessons.Any();
}

public class ToggleItemResponse
{
    public bool Success { get; set; }
    public bool IsCompleted { get; set; }
    public int CompletedCount { get; set; }
    public int ProgressPercentage { get; set; }
    public int CurrentStreak { get; set; }
    public int XpEarned { get; set; }
    public string Message { get; set; } = string.Empty;
}
