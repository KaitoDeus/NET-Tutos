using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class UserBadge
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(50)]
    public string BadgeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string IconClass { get; set; } = "bi-award";

    [MaxLength(50)]
    public string ColorClass { get; set; } = "primary";

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}
