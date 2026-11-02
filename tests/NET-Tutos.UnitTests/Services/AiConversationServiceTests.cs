using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class AiConversationServiceTests
{
    private readonly Mock<ILogger<AiConversationService>> _loggerMock = new();

    [Fact]
    public async Task CreateConversationAsync_CreatesNewConversationWithCorrectUser()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiConversationService(context, _loggerMock.Object);
        var userId = "test-user-123";

        // Act
        var result = await service.CreateConversationAsync(userId, "Làm chủ LINQ");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("Làm chủ LINQ");

        var inDb = await context.AiConversations.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task SaveMessageAsync_AddsMessageAndUpdatesConversationTimestamp()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiConversationService(context, _loggerMock.Object);
        var conv = await service.CreateConversationAsync("user-1", "Học C#");
        var initialTime = conv.UpdatedAt;

        // Act
        var savedMsg = await service.SaveMessageAsync(
            conv.Id,
            role: "user",
            content: "Giải thích Async/Await trong .NET 10",
            codeSnippet: "public async Task Run() {}",
            followUps: new List<string> { "ThreadPool hoạt động thế nào?" });

        // Assert
        savedMsg.Should().NotBeNull();
        savedMsg.Role.Should().Be("user");
        savedMsg.Content.Should().Contain("Async/Await");
        savedMsg.SuggestedFollowUps.Should().Contain("ThreadPool hoạt động thế nào?");

        var updatedConv = await context.AiConversations.FindAsync(conv.Id);
        updatedConv!.UpdatedAt.Should().BeOnOrAfter(initialTime);
    }

    [Fact]
    public async Task GetUserConversationsAsync_ReturnsOrderedAndGroupedByTime()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiConversationService(context, _loggerMock.Object);
        var userId = "user-vip";

        var conv1 = await service.CreateConversationAsync(userId, "Chat hôm nay");
        var conv2 = await service.CreateConversationAsync(userId, "Chat đã ghim");
        await service.TogglePinAsync(conv2.Id, userId, isPinned: true);

        // Act
        var list = await service.GetUserConversationsAsync(userId, isEnglish: false);

        // Assert
        list.Should().HaveCount(2);
        list[0].Id.Should().Be(conv2.Id); // Pinned should come first
        list[0].TimeGroup.Should().Be("Đã ghim");
        list[1].TimeGroup.Should().Be("Hôm nay");
    }

    [Fact]
    public async Task UpdateTitleAsync_ChangesTitleSuccessfully()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiConversationService(context, _loggerMock.Object);
        var conv = await service.CreateConversationAsync("user-1", "Tiêu đề cũ");

        // Act
        var success = await service.UpdateTitleAsync(conv.Id, "user-1", "Tiêu đề mới Clean Code");

        // Assert
        success.Should().BeTrue();
        var updated = await context.AiConversations.FindAsync(conv.Id);
        updated!.Title.Should().Be("Tiêu đề mới Clean Code");
    }

    [Fact]
    public async Task DeleteConversationAsync_RemovesConversationFromDb()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiConversationService(context, _loggerMock.Object);
        var conv = await service.CreateConversationAsync("user-1", "Cuộc trò chuyện cần xóa");

        // Act
        var deleted = await service.DeleteConversationAsync(conv.Id, "user-1");

        // Assert
        deleted.Should().BeTrue();
        var inDb = await context.AiConversations.FindAsync(conv.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task EnsureAutoTitleAsync_GeneratesCleanTitleFromPrompt()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiConversationService(context, _loggerMock.Object);
        var conv = await service.CreateConversationAsync("user-1", "Cuộc trò chuyện mới");

        // Act
        await service.EnsureAutoTitleAsync(conv.Id, "Xin chào bạn, làm thế nào để tối ưu truy vấn LINQ với AsNoTracking?", isEnglish: false);

        // Assert
        var updated = await context.AiConversations.FindAsync(conv.Id);
        updated!.Title.Should().NotBe("Cuộc trò chuyện mới");
        updated.Title.Should().ContainEquivalentOf("Tối ưu");
    }

    [Fact]
    public async Task GetRecentMessagesForContextAsync_ReturnsChronologicalOrder()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiConversationService(context, _loggerMock.Object);
        var conv = await service.CreateConversationAsync("user-1", "Multi-turn test");

        await service.SaveMessageAsync(conv.Id, "user", "Tin 1");
        await service.SaveMessageAsync(conv.Id, "model", "Trả lời 1");
        await service.SaveMessageAsync(conv.Id, "user", "Tin 2");

        // Act
        var history = await service.GetRecentMessagesForContextAsync(conv.Id, limit: 10);

        // Assert
        history.Should().HaveCount(3);
        history[0].Content.Should().Be("Tin 1");
        history[1].Content.Should().Be("Trả lời 1");
        history[2].Content.Should().Be("Tin 2");
    }
}
