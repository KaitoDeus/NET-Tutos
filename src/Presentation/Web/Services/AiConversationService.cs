using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class AiConversationService : IAiConversationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AiConversationService> _logger;

    public AiConversationService(AppDbContext context, ILogger<AiConversationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AiConversationSummaryDto>> GetUserConversationsAsync(string userId, bool isEnglish)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new List<AiConversationSummaryDto>();
        }

        var conversations = await _context.AiConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsArchived)
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.IsPinned,
                c.CreatedAt,
                c.UpdatedAt,
                MessageCount = c.Messages.Count
            })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var sevenDaysAgo = today.AddDays(-7);

        return conversations.Select(c =>
        {
            string group;
            if (c.IsPinned)
            {
                group = isEnglish ? "Pinned" : "Đã ghim";
            }
            else if (c.UpdatedAt.Date == today)
            {
                group = isEnglish ? "Today" : "Hôm nay";
            }
            else if (c.UpdatedAt.Date == yesterday)
            {
                group = isEnglish ? "Yesterday" : "Hôm qua";
            }
            else if (c.UpdatedAt.Date >= sevenDaysAgo)
            {
                group = isEnglish ? "Previous 7 Days" : "7 ngày trước";
            }
            else
            {
                group = isEnglish ? "Older" : "Cũ hơn";
            }

            return new AiConversationSummaryDto
            {
                Id = c.Id,
                Title = string.IsNullOrWhiteSpace(c.Title) ? (isEnglish ? "New Chat" : "Cuộc trò chuyện mới") : c.Title,
                IsPinned = c.IsPinned,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                MessageCount = c.MessageCount,
                TimeGroup = group
            };
        }).ToList();
    }

    public async Task<AiConversationDetailDto?> GetConversationAsync(Guid conversationId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || conversationId == Guid.Empty)
        {
            return null;
        }

        var conversation = await _context.AiConversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation == null)
        {
            return null;
        }

        return new AiConversationDetailDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            IsPinned = conversation.IsPinned,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = conversation.Messages.Select(m => new AiChatMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                Role = m.Role,
                Content = m.Content,
                CodeSnippet = m.CodeSnippet,
                SuggestedFollowUps = DeserializeFollowUps(m.SuggestedFollowUpsJson),
                ModelUsed = m.ModelUsed,
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }

    public async Task<AiConversationDetailDto> CreateConversationAsync(string userId, string? initialTitle = null)
    {
        var conversation = new AiConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(initialTitle) ? "Cuộc trò chuyện mới" : initialTitle.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AiConversations.Add(conversation);
        await _context.SaveChangesAsync();

        return new AiConversationDetailDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            IsPinned = false,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = new List<AiChatMessageDto>()
        };
    }

    public async Task<bool> UpdateTitleAsync(Guid conversationId, string userId, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return false;

        var conversation = await _context.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation == null) return false;

        conversation.Title = newTitle.Trim();
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePinAsync(Guid conversationId, string userId, bool isPinned)
    {
        var conversation = await _context.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation == null) return false;

        conversation.IsPinned = isPinned;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, string userId)
    {
        var conversation = await _context.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation == null) return false;

        _context.AiConversations.Remove(conversation);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AiChatMessageDto> SaveMessageAsync(
        Guid conversationId,
        string role,
        string content,
        string? codeSnippet = null,
        List<string>? followUps = null,
        string? modelUsed = null)
    {
        var message = new AiChatMessage
        {
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CodeSnippet = codeSnippet,
            SuggestedFollowUpsJson = followUps != null && followUps.Count > 0 ? JsonSerializer.Serialize(followUps) : null,
            ModelUsed = modelUsed ?? "gemini-3.8-flash",
            CreatedAt = DateTime.UtcNow
        };

        _context.AiChatMessages.Add(message);

        // Touch the conversation's UpdatedAt timestamp
        var conversation = await _context.AiConversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new AiChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Role = message.Role,
            Content = message.Content,
            CodeSnippet = message.CodeSnippet,
            SuggestedFollowUps = followUps ?? new List<string>(),
            ModelUsed = message.ModelUsed,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<List<AiChatMessageDto>> GetRecentMessagesForContextAsync(Guid conversationId, int limit = 10)
    {
        var messages = await _context.AiChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();

        return messages.Select(m => new AiChatMessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role,
            Content = m.Content,
            CodeSnippet = m.CodeSnippet,
            SuggestedFollowUps = DeserializeFollowUps(m.SuggestedFollowUpsJson),
            ModelUsed = m.ModelUsed,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task EnsureAutoTitleAsync(Guid conversationId, string firstPrompt, bool isEnglish)
    {
        try
        {
            var conversation = await _context.AiConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null) return;

            // Only auto-title if it still has the default title or <= 2 messages
            bool isDefaultTitle = string.IsNullOrWhiteSpace(conversation.Title) ||
                                  conversation.Title == "Cuộc trò chuyện mới" ||
                                  conversation.Title == "New Chat";

            if (!isDefaultTitle && conversation.Messages.Count > 2) return;

            var clean = CleanPromptForTitle(firstPrompt);
            if (string.IsNullOrWhiteSpace(clean))
            {
                clean = isEnglish ? "General Discussion" : "Hỏi đáp C# & .NET";
            }

            conversation.Title = clean;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-generate conversation title for {Id}", conversationId);
        }
    }

    private static string CleanPromptForTitle(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;

        // Remove markdown, code blocks, urls
        var text = Regex.Replace(prompt, @"```[\s\S]*?```", "");
        text = Regex.Replace(text, @"`[^`]+`", "");
        text = Regex.Replace(text, @"https?://\S+", "");

        // Remove common greeting prefixes
        text = Regex.Replace(text, @"^(xin chào|chào bạn|hello|hi|cho mình hỏi|làm thế nào để|giải thích|hãy giải thích)\s*", "", RegexOptions.IgnoreCase);
        text = text.Trim();

        // Capitalize first letter
        if (text.Length > 0)
        {
            text = char.ToUpper(text[0]) + (text.Length > 1 ? text.Substring(1) : "");
        }

        const int maxLength = 42;
        if (text.Length > maxLength)
        {
            text = text.Substring(0, maxLength).TrimEnd() + "...";
        }

        return text;
    }

    private static List<string> DeserializeFollowUps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
