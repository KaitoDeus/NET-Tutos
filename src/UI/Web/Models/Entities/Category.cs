using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string IconClass { get; set; } = "bi-journal-code";

    [MaxLength(30)]
    public string BadgeColor { get; set; } = "primary";

    public int OrderIndex { get; set; }

    public DifficultyLevel Level { get; set; } = DifficultyLevel.Beginner;

    public ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
}

