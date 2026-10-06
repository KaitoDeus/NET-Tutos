using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class CodeTestCase
{
    public int Id { get; set; }

    public int CodingChallengeId { get; set; }
    public CodingChallenge? CodingChallenge { get; set; }

    [Required]
    [MaxLength(500)]
    public string InputParameters { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ExpectedOutput { get; set; } = string.Empty;

    public bool IsHidden { get; set; } = false;

    [MaxLength(500)]
    public string? Explanation { get; set; }
}
