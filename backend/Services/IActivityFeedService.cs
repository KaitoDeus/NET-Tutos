using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IActivityFeedService
{
    Task RecordActivityAsync(
        string? userId,
        ActivityType type,
        string title,
        string description,
        string? targetUrl = null,
        int xpEarned = 0,
        string? badgeCode = null);

    Task<List<ActivityFeedItemViewModel>> GetRecentActivitiesAsync(int take = 20, ActivityType? filterType = null);
    Task<List<ActivityFeedItemViewModel>> GetUserActivitiesAsync(string userId, int take = 10);
    Task<ActivityFeedPageViewModel> GetActivitiesPagedAsync(int page = 1, int pageSize = 15, string? filter = null);
}
