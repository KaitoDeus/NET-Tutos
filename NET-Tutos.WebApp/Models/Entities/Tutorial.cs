using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class Tutorial
{
    public int Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(600)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string ContentMarkdown { get; set; } = string.Empty;

    public int EstimatedReadingMinutes { get; set; } = 5;

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    public int OrderIndex { get; set; }

    public int ViewCount { get; set; } = 0;

    public bool IsFeatured { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

    public ICollection<CodeSnippet> CodeSnippets { get; set; } = new List<CodeSnippet>();
    public ICollection<DiscussionComment> Comments { get; set; } = new List<DiscussionComment>();
}

