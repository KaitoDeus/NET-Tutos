using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class QuizQuestion
{
    public int Id { get; set; }

    public int? TutorialId { get; set; }
    public Tutorial? Tutorial { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Question { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string OptionA { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string OptionB { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string OptionC { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    public string CorrectOption { get; set; } = "A"; // "A", "B", "C", or "D"

    [MaxLength(1000)]
    public string Explanation { get; set; } = string.Empty;

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;
}

