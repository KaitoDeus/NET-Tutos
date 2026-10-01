namespace NET_Tutos.Models.ViewModels;

public class DiscussionCommentDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public bool IsAdmin { get; set; }
    public int AuthorXp { get; set; }
    public string ContentMarkdown { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedAgo { get; set; } = string.Empty;
    public int UpvotesCount { get; set; }
    public bool IsUpvotedByCurrentUser { get; set; }
    public bool IsPinned { get; set; }
    public bool IsBestAnswer { get; set; }
    public int? ParentCommentId { get; set; }
    public List<DiscussionCommentDto> Replies { get; set; } = new();
}

public class PostCommentRequest
{
    public string TopicType { get; set; } = "Tutorial"; // "Tutorial" or "Challenge"
    public int TopicId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class ToggleUpvoteRequest
{
    public int CommentId { get; set; }
}

public class MarkBestAnswerRequest
{
    public int CommentId { get; set; }
}

public class DiscussionSectionViewModel
{
    public string TopicType { get; set; } = "Tutorial";
    public int TopicId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;
    public List<DiscussionCommentDto> Comments { get; set; } = new();
    public int TotalCommentsCount { get; set; }
    public string? CurrentUserId { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
}
