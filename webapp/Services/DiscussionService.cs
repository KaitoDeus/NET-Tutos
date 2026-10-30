using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class DiscussionService : IDiscussionService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMarkdownService _markdownService;
    private readonly ILeaderboardService _leaderboardService;
    private readonly INotificationService _notificationService;
    private readonly IActivityFeedService _activityFeedService;

    public DiscussionService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IMarkdownService markdownService,
        ILeaderboardService leaderboardService,
        INotificationService notificationService,
        IActivityFeedService activityFeedService)
    {
        _context = context;
        _userManager = userManager;
        _markdownService = markdownService;
        _leaderboardService = leaderboardService;
        _notificationService = notificationService;
        _activityFeedService = activityFeedService;
    }

    public async Task<List<DiscussionCommentDto>> GetCommentsAsync(string topicType, int topicId, string? currentUserId)
    {
        var query = _context.DiscussionComments
            .Include(c => c.User)
            .Include(c => c.Upvotes)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Upvotes)
            .AsNoTracking();

        if (topicType.Equals("Challenge", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.CodingChallengeId == topicId);
        }
        else
        {
            query = query.Where(c => c.TutorialId == topicId);
        }

        // Top level comments only
        var topLevelComments = await query
            .Where(c => c.ParentCommentId == null)
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.IsBestAnswer)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        var adminUserIds = await GetAdminUserIdsAsync();

        var dtos = new List<DiscussionCommentDto>();
        foreach (var comment in topLevelComments)
        {
            dtos.Add(MapToDto(comment, currentUserId, adminUserIds));
        }

        return dtos;
    }

    public async Task<DiscussionCommentDto> AddCommentAsync(
        string userId,
        string topicType,
        int topicId,
        string contentMarkdown,
        int? parentCommentId)
    {
        if (string.IsNullOrWhiteSpace(contentMarkdown))
        {
            throw new ArgumentException("Nội dung bình luận không được để trống.", nameof(contentMarkdown));
        }

        var comment = new DiscussionComment
        {
            UserId = userId,
            ContentMarkdown = contentMarkdown.Trim(),
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        if (topicType.Equals("Challenge", StringComparison.OrdinalIgnoreCase))
        {
            comment.CodingChallengeId = topicId;
        }
        else
        {
            comment.TutorialId = topicId;
        }

        _context.DiscussionComments.Add(comment);

        // Award +5 XP for contributing to discussion
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.ExperiencePoints += 5;
        }

        await _context.SaveChangesAsync();

        // Check badge unlocks
        await _leaderboardService.CheckAndAwardBadgesAsync(userId);

        if (parentCommentId.HasValue)
        {
            var parent = await _context.DiscussionComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value);

            if (parent != null && parent.UserId != userId)
            {
                var replierName = user != null
                    ? (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.UserName?.Split('@')[0] ?? "Một học viên")
                    : "Một học viên";

                string targetUrl = topicType.Equals("Challenge", StringComparison.OrdinalIgnoreCase)
                    ? $"/Playground?challengeId={topicId}#discussion"
                    : $"/bai-hoc/{topicId}#discussion";

                try
                {
                    await _notificationService.CreateNotificationAsync(
                        parent.UserId,
                        "Phản hồi thảo luận mới! 💬",
                        $"{replierName} đã trả lời bình luận của bạn.",
                        NotificationType.DiscussionReply,
                        targetUrl);
                }
                catch { }
            }
        }

        // Reload comment with user details
        var reloaded = await _context.DiscussionComments
            .Include(c => c.User)
            .Include(c => c.Upvotes)
            .FirstAsync(c => c.Id == comment.Id);

        var adminUserIds = await GetAdminUserIdsAsync();
        return MapToDto(reloaded, userId, adminUserIds);
    }

    public async Task<(bool Success, int NewUpvotesCount, bool IsUpvoted)> ToggleUpvoteAsync(int commentId, string userId)
    {
        var comment = await _context.DiscussionComments.FindAsync(commentId);
        if (comment == null)
        {
            return (false, 0, false);
        }

        var existingUpvote = await _context.CommentUpvotes
            .FirstOrDefaultAsync(u => u.CommentId == commentId && u.UserId == userId);

        bool isUpvoted;
        if (existingUpvote != null)
        {
            _context.CommentUpvotes.Remove(existingUpvote);
            comment.UpvotesCount = Math.Max(0, comment.UpvotesCount - 1);
            isUpvoted = false;
        }
        else
        {
            _context.CommentUpvotes.Add(new CommentUpvote
            {
                CommentId = commentId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            comment.UpvotesCount += 1;
            isUpvoted = true;
        }

        await _context.SaveChangesAsync();
        return (true, comment.UpvotesCount, isUpvoted);
    }

    public async Task<(bool Success, string Message, int? AuthorAwardedXp)> MarkBestAnswerAsync(
        int commentId,
        string currentUserId,
        bool isAdmin)
    {
        var comment = await _context.DiscussionComments
            .Include(c => c.User)
            .Include(c => c.ParentComment)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            return (false, "Không tìm thấy bình luận.", null);
        }

        // Must be reply, or author of topic, or admin
        bool canMark = isAdmin;
        if (!canMark && comment.ParentComment != null)
        {
            canMark = comment.ParentComment.UserId == currentUserId;
        }

        if (!canMark)
        {
            return (false, "Bạn không có quyền đánh dấu giải pháp cho bài đăng này.", null);
        }

        int? awardedXp = null;
        if (comment.IsBestAnswer)
        {
            comment.IsBestAnswer = false;
        }
        else
        {
            comment.IsBestAnswer = true;
            // Award +15 XP to solver
            if (comment.User != null)
            {
                comment.User.ExperiencePoints += 15;
                awardedXp = 15;
            }
        }

        await _context.SaveChangesAsync();

        if (comment.User != null)
        {
            await _leaderboardService.CheckAndAwardBadgesAsync(comment.UserId);

            if (comment.IsBestAnswer && comment.UserId != currentUserId)
            {
                string targetUrl = comment.CodingChallengeId.HasValue
                    ? $"/Playground?challengeId={comment.CodingChallengeId.Value}#discussion"
                    : $"/bai-hoc/{comment.TutorialId}#discussion";

                try
                {
                    await _notificationService.CreateNotificationAsync(
                        comment.UserId,
                        "Giải pháp được công nhận! ⭐",
                        "Bình luận của bạn vừa được chọn làm Giải pháp chính xác (+15 XP)!",
                        NotificationType.BestAnswer,
                        targetUrl);

                    await _activityFeedService.RecordActivityAsync(
                        comment.UserId,
                        ActivityType.DiscussionComment,
                        "được bình chọn là Giải pháp chính xác",
                        "Được ghi nhận câu trả lời xuất sắc (+15 XP) ⭐",
                        targetUrl,
                        xpEarned: 15);
                }
                catch { }
            }
        }

        return (true, comment.IsBestAnswer ? "Đã đánh dấu là giải pháp chính xác (+15 XP)!" : "Đã gỡ bỏ đánh dấu giải pháp.", awardedXp);
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(
        int commentId,
        string currentUserId,
        bool isAdmin)
    {
        var comment = await _context.DiscussionComments
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            return (false, "Không tìm thấy bình luận.");
        }

        if (!isAdmin && comment.UserId != currentUserId)
        {
            return (false, "Bạn không có quyền xóa bình luận này.");
        }

        if (comment.Replies.Any())
        {
            _context.DiscussionComments.RemoveRange(comment.Replies);
        }

        _context.DiscussionComments.Remove(comment);
        await _context.SaveChangesAsync();

        return (true, "Đã xóa bình luận thành công.");
    }

    private DiscussionCommentDto MapToDto(
        DiscussionComment comment,
        string? currentUserId,
        HashSet<string> adminUserIds)
    {
        var authorName = comment.User?.FullName;
        if (string.IsNullOrWhiteSpace(authorName))
        {
            authorName = comment.User?.UserName ?? "Học viên ẩn danh";
        }

        var dto = new DiscussionCommentDto
        {
            Id = comment.Id,
            UserId = comment.UserId,
            AuthorName = authorName,
            AuthorAvatar = comment.User?.AvatarUrl,
            IsAdmin = adminUserIds.Contains(comment.UserId),
            AuthorXp = comment.User?.ExperiencePoints ?? 0,
            ContentMarkdown = comment.ContentMarkdown,
            ContentHtml = _markdownService.ToHtml(comment.ContentMarkdown),
            CreatedAt = comment.CreatedAt,
            CreatedAgo = FormatTimeAgo(comment.CreatedAt),
            UpvotesCount = comment.UpvotesCount,
            IsUpvotedByCurrentUser = currentUserId != null && comment.Upvotes.Any(u => u.UserId == currentUserId),
            IsPinned = comment.IsPinned,
            IsBestAnswer = comment.IsBestAnswer,
            ParentCommentId = comment.ParentCommentId
        };

        if (comment.Replies.Any())
        {
            dto.Replies = comment.Replies
                .OrderBy(r => r.CreatedAt)
                .Select(r => MapToDto(r, currentUserId, adminUserIds))
                .ToList();
        }

        return dto;
    }

    private static string FormatTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        if (timeSpan.TotalMinutes < 1) return "Vừa xong";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút trước";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} giờ trước";
        if (timeSpan.TotalDays < 30) return $"{(int)timeSpan.TotalDays} ngày trước";
        return dateTime.ToString("dd/MM/yyyy");
    }

    private async Task<HashSet<string>> GetAdminUserIdsAsync()
    {
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        return adminUsers.Select(u => u.Id).ToHashSet();
    }
}
