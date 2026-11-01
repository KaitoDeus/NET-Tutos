using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class StreakServiceTests
{
    private readonly Mock<INotificationService> _mockNotificationService = new();
    private readonly Mock<IActivityFeedService> _mockActivityFeedService = new();
    private readonly Mock<ILogger<StreakService>> _mockLogger = new();

    [Fact]
    public async Task CheckInTodayAsync_WithValidNewUser_StartsStreakAtOneAndAwardsXp()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var userId = "user-test-1";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            CurrentStreak = 0,
            LongestStreak = 0,
            ExperiencePoints = 0,
            LastCheckInDate = null
        };

        mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CheckInTodayAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.IsAlreadyCheckedInToday.Should().BeFalse();
        result.CurrentStreak.Should().Be(1);
        result.LongestStreak.Should().Be(1);
        result.XpEarned.Should().Be(10); // Base XP for day 1

        user.CurrentStreak.Should().Be(1);
        user.LongestStreak.Should().Be(1);
        user.ExperiencePoints.Should().Be(10);
        user.LastCheckInDate.Should().NotBeNull();
        user.LastCheckInDate!.Value.Date.Should().Be(DateTime.UtcNow.Date);

        context.DailyCheckIns.Should().ContainSingle(c => c.UserId == userId && c.StreakDay == 1);
    }

    [Fact]
    public async Task CheckInTodayAsync_ConsecutiveDay_IncrementsStreakAndAwardsCorrectXp()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var userId = "user-test-consecutive";
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "consecutive_user",
            CurrentStreak = 1,
            LongestStreak = 1,
            ExperiencePoints = 10,
            LastCheckInDate = yesterday
        };

        mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CheckInTodayAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.CurrentStreak.Should().Be(2);
        result.LongestStreak.Should().Be(2);
        result.XpEarned.Should().Be(15); // Day 2: 10 + (2-1)*5 = 15 XP
        user.ExperiencePoints.Should().Be(25); // 10 initial + 15 earned
    }

    [Fact]
    public async Task CheckInTodayAsync_AlreadyCheckedInToday_ReturnsAlreadyCheckedInAndDoesNotDoubleReward()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var userId = "user-test-dup";
        var today = DateTime.UtcNow.Date;

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "dup_user",
            CurrentStreak = 3,
            LongestStreak = 3,
            ExperiencePoints = 50,
            LastCheckInDate = today
        };

        context.DailyCheckIns.Add(new DailyCheckIn
        {
            UserId = userId,
            CheckInDate = today,
            StreakDay = 3,
            XpEarned = 20
        });
        await context.SaveChangesAsync();

        mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CheckInTodayAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.IsAlreadyCheckedInToday.Should().BeTrue();
        result.CurrentStreak.Should().Be(3);
        user.ExperiencePoints.Should().Be(50); // XP should NOT increase
    }

    [Fact]
    public async Task CheckInTodayAsync_MissedYesterday_ResetsStreakToOne()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var userId = "user-test-missed";
        var threeDaysAgo = DateTime.UtcNow.Date.AddDays(-3);

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "missed_user",
            CurrentStreak = 5,
            LongestStreak = 5,
            ExperiencePoints = 150,
            LastCheckInDate = threeDaysAgo
        };

        mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CheckInTodayAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.CurrentStreak.Should().Be(1); // Reset to 1
        result.LongestStreak.Should().Be(5); // Preserves historic record
        user.CurrentStreak.Should().Be(1);
        user.LongestStreak.Should().Be(5);
    }

    [Fact]
    public async Task CheckInTodayAsync_ReachMilestone3_AwardsStreak3BadgeAndBonusXp()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var userId = "user-test-milestone";
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "milestone_user",
            CurrentStreak = 2,
            LongestStreak = 2,
            ExperiencePoints = 25,
            LastCheckInDate = yesterday
        };

        mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CheckInTodayAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.CurrentStreak.Should().Be(3);
        result.NewlyUnlockedBadges.Should().Contain(m => m.Contains("Ngọn Lửa Bền Bỉ"));

        // Day 3 check-in = 20 XP + 30 XP milestone bonus = 50 XP gained
        user.ExperiencePoints.Should().Be(25 + 20 + 30);
        context.UserBadges.Should().ContainSingle(b => b.UserId == userId && b.BadgeCode == "STREAK_3");
    }

    [Fact]
    public async Task GetUserStreakStatusAsync_NullOrEmptyUserId_ReturnsUnauthenticatedDefaultStatus()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.GetUserStreakStatusAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthenticated.Should().BeFalse();
        result.CurrentStreak.Should().Be(0);
        result.HasCheckedInToday.Should().BeFalse();
        result.Past7Days.Should().HaveCount(7);
        result.Milestones.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckInTodayAsync_UserNotFound_ReturnsFailureWithoutException()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CheckInTodayAsync("non-existent-user");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Không tìm thấy");
    }

    [Fact]
    public async Task GetUserStreakStatusAsync_LapsedStreak_ReportsZeroEffectiveStreak()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var userId = "user-lapsed";
        var fiveDaysAgo = DateTime.UtcNow.Date.AddDays(-5);

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "lapsed_user",
            CurrentStreak = 10,
            LongestStreak = 10,
            LastCheckInDate = fiveDaysAgo
        };

        mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        var service = new StreakService(
            context,
            mockUserManager.Object,
            _mockNotificationService.Object,
            _mockActivityFeedService.Object,
            _mockLogger.Object);

        // Act
        var result = await service.GetUserStreakStatusAsync(userId);

        // Assert
        result.IsAuthenticated.Should().BeTrue();
        result.HasCheckedInToday.Should().BeFalse();
        result.CurrentStreak.Should().Be(0); // Lapsed streak is effectively 0 for display
        result.LongestStreak.Should().Be(10); // Historic record remains 10
    }
}
