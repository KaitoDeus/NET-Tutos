namespace NET_Tutos.Models.Entities;

public class UserLessonProgress
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int TutorialId { get; set; }
    public Tutorial? Tutorial { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedAt { get; set; }

    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}
