using NET_Tutos.Gamification.API.Contracts;

namespace NET_Tutos.Gamification.API.Services;

public interface IGamificationService
{
    Task<StreakDto> GetUserStreakAsync(string userId);
    Task<CheckInResultDto> CheckInTodayAsync(string userId);
    Task<UserProgressDto> AwardXpAsync(string userId, int xpAmount, string reason);
    Task<List<BadgeDto>> GetBadgesAsync(string? userId = null);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int top = 50);
}
