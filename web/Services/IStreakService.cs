using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IStreakService
{
    Task<StreakStatusViewModel> GetUserStreakStatusAsync(string? userId);
    Task<StreakCheckInResult> CheckInTodayAsync(string userId);
    Task<StreakCheckInResult?> RecordLearningActivityStreakAsync(string userId);
}
