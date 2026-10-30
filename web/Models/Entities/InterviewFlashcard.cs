using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public enum FlashcardTopic
{
    CSharpCore,         // C# Fundamentals & Modern Syntax
    OopDesignPatterns,  // OOP Principles & Design Patterns
    AsyncConcurrency,   // async/await, Task & Concurrency
    EfCoreDatabase,     // Entity Framework Core, LINQ & SQL Performance
    AspNetCoreWebAPI,   // ASP.NET Core, Web API, Middleware & Security
    ArchitectureCloud   // Clean Architecture, Microservices & Caching
}

public enum FlashcardRating
{
    Again = 1,   // Quên / Cần học lại (Next review: 1 day, Box 1)
    Hard = 2,    // Khó / Hơi nhớ (Next review: 3 days, Box 2)
    Good = 3,    // Tốt / Nhớ được (Next review: 7 days, Box 3)
    Mastered = 4 // Nắm vững / Rất thuộc (Next review: 14+ days, Box 4-5)
}

public class InterviewFlashcard
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    public FlashcardTopic Topic { get; set; } = FlashcardTopic.CSharpCore;

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    [Required]
    [MaxLength(500)]
    public string QuestionVi { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string QuestionEn { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? QuickHintVi { get; set; }

    [MaxLength(300)]
    public string? QuickHintEn { get; set; }

    [Required]
    public string AnswerMarkdownVi { get; set; } = string.Empty;

    [Required]
    public string AnswerMarkdownEn { get; set; } = string.Empty;

    public string? InterviewTipVi { get; set; }

    public string? InterviewTipEn { get; set; }

    public int OrderIndex { get; set; } = 1;

    public int ViewCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserFlashcardProgress> UserProgresses { get; set; } = new List<UserFlashcardProgress>();
}

public class UserFlashcardProgress
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int FlashcardId { get; set; }
    public InterviewFlashcard? Flashcard { get; set; }

    // Leitner Box level (1 to 5)
    public int BoxLevel { get; set; } = 1;

    public int ReviewCount { get; set; } = 0;

    public FlashcardRating LastRating { get; set; } = FlashcardRating.Again;

    public DateTime LastReviewedAt { get; set; } = DateTime.UtcNow;

    public DateTime NextReviewDate { get; set; } = DateTime.UtcNow;

    public bool IsMastered { get; set; } = false;
}
