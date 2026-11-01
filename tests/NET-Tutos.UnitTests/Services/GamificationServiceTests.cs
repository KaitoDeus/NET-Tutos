using FluentAssertions;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class GamificationServiceTests
{
    [Fact]
    public void GetAllAvailableBadges_HasDistinctCodesAndValidVisualTokens()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var leaderboardService = new LeaderboardService(context);

        // Act
        var badges = leaderboardService.GetAllAvailableBadges();

        // Assert
        badges.Should().NotBeNullOrEmpty();
        badges.Select(b => b.BadgeCode).Should().OnlyHaveUniqueItems();
        badges.Should().AllSatisfy(b =>
        {
            b.BadgeCode.Should().NotBeNullOrWhiteSpace();
            b.Title.Should().NotBeNullOrWhiteSpace();
            b.IconClass.Should().StartWith("bi-");
            b.ColorClass.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(399, 2)]
    [InlineData(400, 3)]
    [InlineData(899, 3)]
    [InlineData(900, 4)]
    [InlineData(1600, 5)]
    [InlineData(2500, 6)]
    public void LevelFormula_CalculatesExpectedProgression(int xp, int expectedLevel)
    {
        // Level formula used across .NET Tutos: Level = (int)Math.Floor(Math.Sqrt(xp / 100.0)) + 1
        var calculatedLevel = (int)Math.Floor(Math.Sqrt(xp / 100.0)) + 1;

        calculatedLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public async Task GetLeaderboardAsync_OrdersUsersByExperiencePointsDescending()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        context.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "Alice", FullName = "Alice Nguyen", ExperiencePoints = 250 },
            new ApplicationUser { Id = "u2", UserName = "Bob", FullName = "Bob Smith", ExperiencePoints = 800 },
            new ApplicationUser { Id = "u3", UserName = "Charlie", FullName = "Charlie Tran", ExperiencePoints = 500 }
        );
        await context.SaveChangesAsync();

        var service = new LeaderboardService(context);

        // Act
        var result = await service.GetLeaderboardAsync(currentUserId: "u1", count: 10);

        // Assert
        result.Should().NotBeNull();
        result.TopUsers.Should().HaveCount(3);
        result.TopUsers[0].FullName.Should().Be("Bob Smith");
        result.TopUsers[0].ExperiencePoints.Should().Be(800);
        result.TopUsers[0].Rank.Should().Be(1);

        result.TopUsers[1].FullName.Should().Be("Charlie Tran");
        result.TopUsers[1].ExperiencePoints.Should().Be(500);
        result.TopUsers[1].Rank.Should().Be(2);

        result.TopUsers[2].FullName.Should().Be("Alice Nguyen");
        result.TopUsers[2].ExperiencePoints.Should().Be(250);
        result.TopUsers[2].Rank.Should().Be(3);
    }
}
