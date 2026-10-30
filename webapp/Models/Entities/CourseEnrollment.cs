namespace NET_Tutos.Models.Entities;

public class CourseEnrollment
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public double ProgressPercentage { get; set; } = 0.0;
}
