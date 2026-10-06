using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class QuizAttempt
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    [MaxLength(200)]
    public string ExamTitle { get; set; } = string.Empty;

    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public int DurationSeconds { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
