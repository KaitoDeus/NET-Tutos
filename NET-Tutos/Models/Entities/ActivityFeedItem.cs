namespace NET_Tutos.Models.Entities;

public enum ActivityType
{
    LessonCompleted = 0,
    ChallengeSolved = 1,
    ExamPassed = 2,
    CertificateEarned = 3,
    StreakAchieved = 4,
    BadgeEarned = 5,
    DiscussionComment = 6
}

public class ActivityFeedItem
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public ActivityType Type { get; set; } = ActivityType.LessonCompleted;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public int XpEarned { get; set; } = 0;
    public string? BadgeCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
