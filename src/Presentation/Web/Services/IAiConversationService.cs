using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IAiConversationService
{
    Task<List<AiConversationSummaryDto>> GetUserConversationsAsync(string userId, bool isEnglish);
    Task<AiConversationDetailDto?> GetConversationAsync(Guid conversationId, string userId);
    Task<AiConversationDetailDto> CreateConversationAsync(string userId, string? initialTitle = null);
    Task<bool> UpdateTitleAsync(Guid conversationId, string userId, string newTitle);
    Task<bool> TogglePinAsync(Guid conversationId, string userId, bool isPinned);
    Task<bool> DeleteConversationAsync(Guid conversationId, string userId);
    Task<AiChatMessageDto> SaveMessageAsync(Guid conversationId, string role, string content, string? codeSnippet = null, List<string>? followUps = null, string? modelUsed = null);
    Task<List<AiChatMessageDto>> GetRecentMessagesForContextAsync(Guid conversationId, int limit = 10);
    Task EnsureAutoTitleAsync(Guid conversationId, string firstPrompt, bool isEnglish);
}
