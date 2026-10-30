namespace NET_Tutos.Models.ViewModels;

public class StreakStatusViewModel
{
    public bool IsAuthenticated { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public bool HasCheckedInToday { get; set; }
    public DateTime? LastCheckInDate { get; set; }
    public int NextRewardXp { get; set; }
    public int TotalXpEarnedFromStreaks { get; set; }
    public List<StreakDayItem> Past7Days { get; set; } = new();
    public List<StreakMilestoneItem> Milestones { get; set; } = new();
}

public class StreakDayItem
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty; // "T2", "T3", "T4", "T5", "T6", "T7", "CN"
    public string DayNameEn { get; set; } = string.Empty; // "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"
    public bool IsCheckedIn { get; set; }
    public bool IsToday { get; set; }
    public int XpEarned { get; set; }
}

public class StreakMilestoneItem
{
    public int Days { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public int BonusXp { get; set; }
    public string BadgeCode { get; set; } = string.Empty;
    public bool IsReached { get; set; }
}

public class StreakCheckInResult
{
    public bool Success { get; set; }
    public bool IsAlreadyCheckedInToday { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int XpEarned { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageEn { get; set; } = string.Empty;
    public List<string> NewlyUnlockedBadges { get; set; } = new();
    public List<string> NewlyUnlockedBadgesEn { get; set; } = new();
    public List<StreakDayItem> Past7Days { get; set; } = new();
}
