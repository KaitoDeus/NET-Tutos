using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IDiscussionService
{
    Task<List<DiscussionCommentDto>> GetCommentsAsync(string topicType, int topicId, string? currentUserId);
    Task<DiscussionCommentDto> AddCommentAsync(string userId, string topicType, int topicId, string contentMarkdown, int? parentCommentId);
    Task<(bool Success, int NewUpvotesCount, bool IsUpvoted)> ToggleUpvoteAsync(int commentId, string userId);
    Task<(bool Success, string Message, int? AuthorAwardedXp)> MarkBestAnswerAsync(int commentId, string currentUserId, bool isAdmin);
    Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, string currentUserId, bool isAdmin);
}
