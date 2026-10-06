using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class Certificate
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string CertificateCode { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    [MaxLength(200)]
    public string CourseTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string StudentFullName { get; set; } = string.Empty;

    public double FinalScore { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string VerificationUrl { get; set; } = string.Empty;
}
