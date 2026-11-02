using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface INotificationService
{
    Task<UserNotification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType type,
        string? targetUrl = null,
        string? iconClass = null,
        string? colorClass = null);

    Task<NotificationListViewModel> GetUserNotificationsAsync(string userId, int take = 15);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(int notificationId, string userId);
}
