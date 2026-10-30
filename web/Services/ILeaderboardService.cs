using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface ILeaderboardService
{
    Task<LeaderboardViewModel> GetLeaderboardAsync(string? currentUserId, int count = 25);
    Task<List<UserBadgeDto>> GetUserBadgesAsync(string userId);
    Task CheckAndAwardBadgesAsync(string userId);
    List<BadgeCatalogItem> GetAllAvailableBadges();
}
