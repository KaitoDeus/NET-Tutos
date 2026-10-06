using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Hubs;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<UserNotification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType type,
        string? targetUrl = null,
        string? iconClass = null,
        string? colorClass = null)
    {
        if (string.IsNullOrEmpty(iconClass) || string.IsNullOrEmpty(colorClass))
        {
            var (defaultIcon, defaultColor) = GetDefaultsForType(type);
            iconClass ??= defaultIcon;
            colorClass ??= defaultColor;
        }

        var notification = new UserNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            TargetUrl = targetUrl,
            IconClass = iconClass,
            ColorClass = colorClass,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserNotifications.Add(notification);
        await _context.SaveChangesAsync();

        try
        {
            var vm = MapToViewModel(notification);
            var unreadCount = await _context.UserNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", vm);
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi SignalR notification cho UserId {UserId}", userId);
        }

        return notification;
    }

    public async Task<NotificationListViewModel> GetUserNotificationsAsync(string userId, int take = 15)
    {
        var notifications = await _context.UserNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();

        var unreadCount = await _context.UserNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return new NotificationListViewModel
        {
            UnreadCount = unreadCount,
            Items = notifications.Select(MapToViewModel).ToList()
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.UserNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        var item = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (item == null) return false;

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            try
            {
                var unreadCount = await _context.UserNotifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi cập nhật unread count qua SignalR cho UserId {UserId}", userId);
            }
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var unreadList = await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadList.Count == 0) return true;

        var now = DateTime.UtcNow;
        foreach (var item in unreadList)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await _context.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi cập nhật unread count 0 qua SignalR cho UserId {UserId}", userId);
        }

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
    {
        var item = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (item == null) return false;

        _context.UserNotifications.Remove(item);
        await _context.SaveChangesAsync();

        try
        {
            var unreadCount = await _context.UserNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi cập nhật unread count sau khi xóa qua SignalR");
        }

        return true;
    }

    private static UserNotificationViewModel MapToViewModel(UserNotification n)
    {
        return new UserNotificationViewModel
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            TitleEn = LocalizationHelper.TranslateNotificationTitle(n.Title, true),
            MessageEn = LocalizationHelper.TranslateNotificationMessage(n.Message, true),
            TargetUrl = n.TargetUrl,
            Type = n.Type,
            TypeName = GetTypeName(n.Type),
            TypeNameEn = LocalizationHelper.GetNotificationTypeName(n.Type, true),
            IconClass = n.IconClass ?? "bi-bell",
            ColorClass = n.ColorClass ?? "text-primary",
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            TimeAgo = FormatTimeAgo(n.CreatedAt, false),
            TimeAgoEn = FormatTimeAgo(n.CreatedAt, true)
        };
    }

    private static (string Icon, string Color) GetDefaultsForType(NotificationType type) => type switch
    {
        NotificationType.BadgeEarned => ("bi-award-fill", "text-warning"),
        NotificationType.StreakReminder => ("bi-fire", "text-danger"),
        NotificationType.DiscussionReply => ("bi-chat-dots-fill", "text-primary"),
        NotificationType.BestAnswer => ("bi-star-fill", "text-warning"),
        NotificationType.ExamPassed => ("bi-mortarboard-fill", "text-success"),
        NotificationType.CertificateIssued => ("bi-patch-check-fill", "text-success"),
        NotificationType.BonusXpAwarded => ("bi-lightning-charge-fill", "text-warning"),
        NotificationType.LessonCompleted => ("bi-check-circle-fill", "text-info"),
        NotificationType.ChallengeSolved => ("bi-code-slash", "text-danger"),
        NotificationType.ProjectSubmitted => ("bi-folder-check", "text-info"),
        NotificationType.ProjectReviewed => ("bi-star-fill", "text-success"),
        _ => ("bi-bell-fill", "text-primary")
    };

    private static string GetTypeName(NotificationType type) => type switch
    {
        NotificationType.BadgeEarned => "Huy hiệu mới",
        NotificationType.StreakReminder => "Chuỗi học tập",
        NotificationType.DiscussionReply => "Thảo luận",
        NotificationType.BestAnswer => "Giải pháp chính xác",
        NotificationType.ExamPassed => "Kỳ thi tốt nghiệp",
        NotificationType.CertificateIssued => "Chứng chỉ số",
        NotificationType.BonusXpAwarded => "Thưởng điểm XP",
        NotificationType.LessonCompleted => "Bài học",
        NotificationType.ChallengeSolved => "Thử thách C#",
        NotificationType.ProjectSubmitted => "Nộp đồ án",
        NotificationType.ProjectReviewed => "Đánh giá đồ án",
        _ => "Hệ thống"
    };

    public static string FormatTimeAgo(DateTime utcDate, bool isEn = false)
    {
        var diff = DateTime.UtcNow - utcDate;
        if (diff.TotalSeconds < 60) return isEn ? "Just now" : "Vừa xong";
        if (diff.TotalMinutes < 60)
        {
            int m = Math.Max(1, (int)diff.TotalMinutes);
            return isEn ? $"{m} min{(m > 1 ? "s" : "")} ago" : $"{m} phút trước";
        }
        if (diff.TotalHours < 24)
        {
            int h = Math.Max(1, (int)diff.TotalHours);
            return isEn ? $"{h} hour{(h > 1 ? "s" : "")} ago" : $"{h} giờ trước";
        }
        if (diff.TotalDays < 30)
        {
            int d = Math.Max(1, (int)diff.TotalDays);
            return isEn ? $"{d} day{(d > 1 ? "s" : "")} ago" : $"{d} ngày trước";
        }
        return isEn ? utcDate.ToLocalTime().ToString("MMM dd, yyyy") : utcDate.ToLocalTime().ToString("dd/MM/yyyy");
    }
}
