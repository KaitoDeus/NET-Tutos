using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NET_Tutos.Gamification.API.Data;
using NET_Tutos.Gamification.API.Services;
using Xunit;

namespace NET_Tutos.Gamification.Tests;

public class GamificationServiceTests
{
    private static GamificationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(databaseName: "GamificationTestDb_" + Guid.NewGuid())
            .Options;

        var context = new GamificationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CheckInTodayAsync_FirstTime_InitializesStreakAndAwardsXp()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new GamificationService(context, NullLogger<GamificationService>.Instance);
        var userId = "user-101";

        // Act
        var result = await service.CheckInTodayAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.CurrentStreak.Should().Be(1);
        result.XpEarned.Should().Be(12); // 10 base + min(1*2, 40) = 12

        var streak = await service.GetUserStreakAsync(userId);
        streak.CurrentStreak.Should().Be(1);
        streak.IsCheckedInToday.Should().BeTrue();
        streak.DaysToNextMilestone.Should().Be(2); // 3 - 1 = 2
    }

    [Fact]
    public async Task CheckInTodayAsync_SameDay_RejectsDuplicate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new GamificationService(context, NullLogger<GamificationService>.Instance);
        var userId = "user-102";

        // Act 1: First check-in
        await service.CheckInTodayAsync(userId);

        // Act 2: Second check-in today
        var duplicateResult = await service.CheckInTodayAsync(userId);

        // Assert
        duplicateResult.IsSuccess.Should().BeFalse();
        duplicateResult.Message.Should().Contain("Bạn đã điểm danh ngày hôm nay rồi");
        duplicateResult.XpEarned.Should().Be(0);
    }

    [Fact]
    public async Task AwardXpAsync_IncreasesLevelAccurately()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new GamificationService(context, NullLogger<GamificationService>.Instance);
        var userId = "user-103";

        // Act
        var progress1 = await service.AwardXpAsync(userId, 80, "Read Tutorial");
        var progress2 = await service.AwardXpAsync(userId, 150, "Solved Challenge");

        // Assert
        progress1.CurrentLevel.Should().Be(1); // 80 / 100 = 0 -> level 1
        progress1.TotalXp.Should().Be(80);

        progress2.TotalXp.Should().Be(230);
        progress2.CurrentLevel.Should().Be(3); // 230 / 100 = 2 -> level 3
        progress2.ProgressToNextLevelPercent.Should().Be(30); // 30% of level 3
        progress2.XpForNextLevel.Should().Be(300);
    }

    [Fact]
    public async Task GetBadgesAsync_ReturnsSeededBadgesWithUniqueCodes()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new GamificationService(context, NullLogger<GamificationService>.Instance);

        // Act
        var badges = await service.GetBadgesAsync();

        // Assert
        badges.Should().NotBeEmpty();
        badges.Count.Should().BeGreaterThanOrEqualTo(4);
        badges.Select(b => b.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetLeaderboardAsync_OrdersByTotalXpDescending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new GamificationService(context, NullLogger<GamificationService>.Instance);

        await service.AwardXpAsync("user-a", 100, "test");
        await service.AwardXpAsync("user-b", 500, "test");
        await service.AwardXpAsync("user-c", 250, "test");

        // Act
        var leaderboard = await service.GetLeaderboardAsync();

        // Assert
        leaderboard.Should().HaveCount(3);
        leaderboard[0].UserId.Should().Be("user-b");
        leaderboard[0].TotalXp.Should().Be(500);
        leaderboard[0].Rank.Should().Be(1);

        leaderboard[1].UserId.Should().Be("user-c");
        leaderboard[1].TotalXp.Should().Be(250);
        leaderboard[1].Rank.Should().Be(2);

        leaderboard[2].UserId.Should().Be("user-a");
        leaderboard[2].TotalXp.Should().Be(100);
        leaderboard[2].Rank.Should().Be(3);
    }

    [Fact]
    public async Task ChallengeSolvedConsumer_ConsumesEvent_AwardsXpAndUnlocksBadge()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new GamificationService(context, NullLogger<GamificationService>.Instance);
        var consumer = new NET_Tutos.Gamification.API.Consumers.ChallengeSolvedConsumer(
            service, context, NullLogger<NET_Tutos.Gamification.API.Consumers.ChallengeSolvedConsumer>.Instance);

        var mockContext = new Moq.Mock<MassTransit.ConsumeContext<NET_Tutos.BuildingBlocks.EventBus.Events.ChallengeSolvedIntegrationEvent>>();
        mockContext.Setup(c => c.Message).Returns(new NET_Tutos.BuildingBlocks.EventBus.Events.ChallengeSolvedIntegrationEvent(
            "user-solver", 1, "Two Sum", 30, 150));

        // Act
        await consumer.Consume(mockContext.Object);

        // Assert
        var badges = await service.GetBadgesAsync("user-solver");
        badges.Should().Contain(b => b.Code == "code-master" && b.UnlockedAt != null);
    }

    [Fact]
    public async Task LessonCompletedConsumer_ConsumesEvent_AwardsConfiguredXp()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new GamificationService(context, NullLogger<GamificationService>.Instance);
        var consumer = new NET_Tutos.Gamification.API.Consumers.LessonCompletedConsumer(
            service, NullLogger<NET_Tutos.Gamification.API.Consumers.LessonCompletedConsumer>.Instance);

        var mockContext = new Moq.Mock<MassTransit.ConsumeContext<NET_Tutos.BuildingBlocks.EventBus.Events.LessonCompletedIntegrationEvent>>();
        mockContext.Setup(c => c.Message).Returns(new NET_Tutos.BuildingBlocks.EventBus.Events.LessonCompletedIntegrationEvent(
            "user-reader", 5, "Advanced LINQ", 15));

        // Act
        await consumer.Consume(mockContext.Object);

        // Assert
        var leaderboard = await service.GetLeaderboardAsync();
        leaderboard.Should().ContainSingle(l => l.UserId == "user-reader" && l.TotalXp == 15);
    }
}
