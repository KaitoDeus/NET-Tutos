namespace NET_Tutos.Models.ViewModels;

public class UserBadgeDto
{
    public string BadgeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-award";
    public string ColorClass { get; set; } = "primary";
    public DateTime EarnedAt { get; set; }
}

public class BadgeCatalogItem
{
    public string BadgeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-award";
    public string ColorClass { get; set; } = "primary";
    public string ConditionDescription { get; set; } = string.Empty;
}

public class LeaderboardUserItem
{
    public int Rank { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int ExperiencePoints { get; set; }
    public int CompletedLessonsCount { get; set; }
    public int CompletedChallengesCount { get; set; }
    public int CertificatesCount { get; set; }
    public int HelpfulCommentsCount { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public List<UserBadgeDto> Badges { get; set; } = new();
}

public class LeaderboardViewModel
{
    public List<LeaderboardUserItem> TopUsers { get; set; } = new();
    public List<BadgeCatalogItem> AllBadges { get; set; } = new();
    public int? CurrentUserRank { get; set; }
    public LeaderboardUserItem? CurrentUserItem { get; set; }
}
