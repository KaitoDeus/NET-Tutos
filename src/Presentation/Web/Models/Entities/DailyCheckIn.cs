using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class DailyCheckIn
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime CheckInDate { get; set; } // UTC date representation (Date only component)

    public int StreakDay { get; set; } = 1; // Current streak day count at the time of check-in

    public int XpEarned { get; set; } = 10; // Bonus XP awarded for this check-in

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
