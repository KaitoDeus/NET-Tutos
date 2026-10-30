using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class CodeSubmission
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CodingChallengeId { get; set; }
    public CodingChallenge CodingChallenge { get; set; } = null!;

    [Required]
    public string SubmittedCode { get; set; } = string.Empty;

    public bool IsPassed { get; set; }
    public int PassedTestsCount { get; set; }
    public int TotalTestsCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int XpEarned { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
