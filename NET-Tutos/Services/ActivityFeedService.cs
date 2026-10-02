using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Hubs;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class ActivityFeedService : IActivityFeedService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ActivityFeedService> _logger;

    public ActivityFeedService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<NotificationHub> hubContext,
        ILogger<ActivityFeedService> logger)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task RecordActivityAsync(
        string? userId,
        ActivityType type,
        string title,
        string description,
        string? targetUrl = null,
        int xpEarned = 0,
        string? badgeCode = null)
    {
        string displayName = "Học viên ẩn danh";
        string? avatarUrl = null;

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                displayName = !string.IsNullOrWhiteSpace(user.FullName)
                    ? user.FullName
                    : (user.UserName?.Split('@')[0] ?? "Học viên");
                avatarUrl = user.AvatarUrl;
            }
        }

        var item = new ActivityFeedItem
        {
            UserId = userId,
            UserDisplayName = displayName,
            UserAvatar = avatarUrl,
            Type = type,
            Title = title,
            Description = description,
            TargetUrl = targetUrl,
            XpEarned = xpEarned,
            BadgeCode = badgeCode,
            CreatedAt = DateTime.UtcNow
        };

        _context.ActivityFeedItems.Add(item);
        await _context.SaveChangesAsync();

        try
        {
            var vm = MapToViewModel(item);
            await _hubContext.Clients.All.SendAsync("ReceiveActivity", vm);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi phát sóng hoạt động qua SignalR");
        }
    }

    public async Task<List<ActivityFeedItemViewModel>> GetRecentActivitiesAsync(int take = 20, ActivityType? filterType = null)
    {
        var query = _context.ActivityFeedItems.AsQueryable();

        if (filterType.HasValue)
        {
            query = query.Where(a => a.Type == filterType.Value);
        }

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();

        return items.Select(MapToViewModel).ToList();
    }

    public async Task<List<ActivityFeedItemViewModel>> GetUserActivitiesAsync(string userId, int take = 10)
    {
        var items = await _context.ActivityFeedItems
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();

        return items.Select(MapToViewModel).ToList();
    }

    public async Task<ActivityFeedPageViewModel> GetActivitiesPagedAsync(int page = 1, int pageSize = 15, string? filter = null)
    {
        if (page < 1) page = 1;

        var query = _context.ActivityFeedItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            switch (filter.ToLower())
            {
                case "lesson":
                    query = query.Where(a => a.Type == ActivityType.LessonCompleted);
                    break;
                case "challenge":
                    query = query.Where(a => a.Type == ActivityType.ChallengeSolved);
                    break;
                case "exam":
                    query = query.Where(a => a.Type == ActivityType.ExamPassed || a.Type == ActivityType.CertificateEarned);
                    break;
                case "badge":
                    query = query.Where(a => a.Type == ActivityType.BadgeEarned || a.Type == ActivityType.StreakAchieved);
                    break;
                case "project":
                    query = query.Where(a => a.Type == ActivityType.ProjectApproved);
                    break;
            }
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (totalPages < 1) totalPages = 1;

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ActivityFeedPageViewModel
        {
            Activities = items.Select(MapToViewModel).ToList(),
            TotalCount = totalCount,
            CurrentFilter = filter,
            CurrentPage = page,
            TotalPages = totalPages
        };
    }

    private static ActivityFeedItemViewModel MapToViewModel(ActivityFeedItem item)
    {
        var (label, color, icon) = GetTypeMetadata(item.Type);

        return new ActivityFeedItemViewModel
        {
            Id = item.Id,
            UserId = item.UserId,
            UserDisplayName = item.UserDisplayName,
            UserAvatar = item.UserAvatar,
            Type = item.Type,
            TypeLabel = label,
            TypeBadgeColor = color,
            IconClass = icon,
            Title = item.Title,
            Description = item.Description,
            TargetUrl = item.TargetUrl,
            XpEarned = item.XpEarned,
            BadgeCode = item.BadgeCode,
            CreatedAt = item.CreatedAt,
            TimeAgo = NotificationService.FormatTimeAgo(item.CreatedAt)
        };
    }

    private static (string Label, string Color, string Icon) GetTypeMetadata(ActivityType type) => type switch
    {
        ActivityType.LessonCompleted => ("Bài học", "info", "bi-journal-check"),
        ActivityType.ChallengeSolved => ("Thuật toán", "danger", "bi-terminal-fill"),
        ActivityType.ExamPassed => ("Kỳ thi", "warning", "bi-patch-check-fill"),
        ActivityType.CertificateEarned => ("Chứng chỉ", "success", "bi-award-fill"),
        ActivityType.StreakAchieved => ("Chuỗi Streak", "danger", "bi-fire"),
        ActivityType.BadgeEarned => ("Huy hiệu", "warning", "bi-trophy-fill"),
        ActivityType.DiscussionComment => ("Thảo luận", "primary", "bi-chat-dots-fill"),
        ActivityType.ProjectApproved => ("Đồ án", "success", "bi-folder-check"),
        _ => ("Hoạt động", "secondary", "bi-activity")
    };
}
