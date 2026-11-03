using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NET_Tutos.Models.Entities;

public class AiChatMessage
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid ConversationId { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public AiConversation? Conversation { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "user"; // "user" or "model"

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? CodeSnippet { get; set; }

    public string? SuggestedFollowUpsJson { get; set; }

    [MaxLength(50)]
    public string? ModelUsed { get; set; } = "gemini-3.8-flash";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
