namespace NET_Tutos.Models.Entities;

public enum NotificationType
{
    General = 0,
    BadgeEarned = 1,
    StreakReminder = 2,
    DiscussionReply = 3,
    BestAnswer = 4,
    ExamPassed = 5,
    CertificateIssued = 6,
    BonusXpAwarded = 7,
    LessonCompleted = 8,
    ChallengeSolved = 9,
    ProjectSubmitted = 10,
    ProjectReviewed = 11
}

public class UserNotification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public NotificationType Type { get; set; } = NotificationType.General;
    public string? IconClass { get; set; }
    public string? ColorClass { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
