using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class CodeSnippet
{
    public int Id { get; set; }

    public int? TutorialId { get; set; }
    public Tutorial? Tutorial { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Language { get; set; } = "csharp";

    [Required]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CategoryName { get; set; } = "C# Core";
}

