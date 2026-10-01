using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class DiscussionComment
{
    public int Id { get; set; }

    public int? TutorialId { get; set; }
    public Tutorial? Tutorial { get; set; }

    public int? CodingChallengeId { get; set; }
    public CodingChallenge? CodingChallenge { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int? ParentCommentId { get; set; }
    public DiscussionComment? ParentComment { get; set; }

    [Required]
    [MaxLength(4000)]
    public string ContentMarkdown { get; set; } = string.Empty;

    public int UpvotesCount { get; set; } = 0;

    public bool IsPinned { get; set; } = false;

    public bool IsBestAnswer { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<DiscussionComment> Replies { get; set; } = new List<DiscussionComment>();
    public ICollection<CommentUpvote> Upvotes { get; set; } = new List<CommentUpvote>();
}
