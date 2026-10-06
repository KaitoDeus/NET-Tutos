using NET_Tutos.Models.Entities;

namespace NET_Tutos.Models.ViewModels;

public class UserNotificationViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string MessageEn { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public NotificationType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string TypeNameEn { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-bell";
    public string ColorClass { get; set; } = "text-primary";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string TimeAgoEn { get; set; } = string.Empty;
}

public class NotificationListViewModel
{
    public int UnreadCount { get; set; }
    public List<UserNotificationViewModel> Items { get; set; } = new();
}

public class ActivityFeedItemViewModel
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public ActivityType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public string TypeLabelEn { get; set; } = string.Empty;
    public string TypeBadgeColor { get; set; } = "primary";
    public string IconClass { get; set; } = "bi-activity";
    public string Title { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public int XpEarned { get; set; }
    public string? BadgeCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string TimeAgoEn { get; set; } = string.Empty;
}

public class ActivityFeedPageViewModel
{
    public List<ActivityFeedItemViewModel> Activities { get; set; } = new();
    public int TotalCount { get; set; }
    public string? CurrentFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
