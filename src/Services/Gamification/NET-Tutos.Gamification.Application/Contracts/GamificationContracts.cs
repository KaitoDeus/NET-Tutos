namespace NET_Tutos.Gamification.Application.Contracts;

public class StreakDto
{
    public string UserId { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int DaysToNextMilestone { get; set; }
    public bool IsCheckedInToday { get; set; }
    public DateTime? LastCheckInDate { get; set; }
}

public class CheckInResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int XpEarned { get; set; }
    public List<BadgeDto> NewlyUnlockedBadges { get; set; } = new();
}

public class BadgeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-award";
    public string BadgeColor { get; set; } = "primary";
    public DateTime? UnlockedAt { get; set; }
}

public class UserProgressDto
{
    public string UserId { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; }
    public double ProgressToNextLevelPercent { get; set; }
    public int XpForNextLevel { get; set; }
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; }
    public int CurrentStreak { get; set; }
}
