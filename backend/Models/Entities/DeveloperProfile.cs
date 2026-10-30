using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class DeveloperProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [MaxLength(200)]
    public string? Headline { get; set; }

    [MaxLength(300)]
    public string? GithubUrl { get; set; }

    [MaxLength(300)]
    public string? LinkedinUrl { get; set; }

    [MaxLength(300)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? SkillsCsv { get; set; }

    [MaxLength(1000)]
    public string? CustomBio { get; set; }

    public bool IsPublic { get; set; } = true;
    public bool ShowEmail { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
