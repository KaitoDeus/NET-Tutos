using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class CodingChallenge
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public string InstructionsMarkdown { get; set; } = string.Empty;

    [Required]
    public string InitialCode { get; set; } = string.Empty;

    public string? SolutionCode { get; set; }

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int XpReward { get; set; } = 50;

    public int OrderIndex { get; set; } = 1;

    public ICollection<CodeTestCase> TestCases { get; set; } = new List<CodeTestCase>();
    public ICollection<CodeSubmission> Submissions { get; set; } = new List<CodeSubmission>();
}
