using System.ComponentModel.DataAnnotations;

namespace NET_Tutos.Models.Entities;

public class CommentUpvote
{
    public int Id { get; set; }

    public int CommentId { get; set; }
    public DiscussionComment? Comment { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
