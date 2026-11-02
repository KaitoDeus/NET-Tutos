using NET_Tutos.Gamification.Domain.Common;

namespace NET_Tutos.Gamification.Domain.Entities;

public class UserStreak : BaseEntity, IAggregateRoot
{
    public string UserId { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastCheckInDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DailyCheckIn : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; } = DateTime.UtcNow;
    public int XpEarned { get; set; }
    public int StreakCountAtTime { get; set; }
}

public class Badge : BaseEntity, IAggregateRoot
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-award";
    public string BadgeColor { get; set; } = "primary";
    public int RequiredCount { get; set; }
}

public class UserBadge : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int BadgeId { get; set; }
    public Badge? Badge { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}

public class UserXpRecord : BaseEntity, IAggregateRoot
{
    public string UserId { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; } = 1;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
