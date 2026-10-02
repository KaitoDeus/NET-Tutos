using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class DailyCheckIn
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime CheckInDate { get; set; } // UTC date representation (Date only component)

    public int StreakDay { get; set; } = 1; // Số ngày trong chuỗi tại thời điểm điểm danh

    public int XpEarned { get; set; } = 10; // Điểm XP thưởng nhận được

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
