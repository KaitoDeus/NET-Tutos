using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NET_Tutos.Data;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class CodingArenaServiceTests
{
    private static Mock<ICodeExecutionService> CreateExecutor(string output, bool success = true)
    {
        var mock = new Mock<ICodeExecutionService>();
        mock.Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new CodeExecutionResponse { IsSuccess = success, Output = output, ExecutionTimeMs = 12 });
        return mock;
    }

    [Fact]
    public void GetActiveRooms_ReturnsThreeSeededBattleRooms()
    {
        var service = new CodingArenaService(CreateExecutor("0").Object);

        var rooms = service.GetActiveRooms();

        rooms.Should().HaveCount(3);
        rooms.Select(r => r.RoomCode).Should().Contain(new[] { "ARENA-LINQ-01", "ARENA-ALGO-02", "ARENA-CS14-03" });
    }

    [Fact]
    public void JoinRoom_WithTwoPlayers_TransitionsRoomToInProgress()
    {
        var service = new CodingArenaService(CreateExecutor("0").Object);

        service.JoinRoom("ARENA-ALGO-02", "alice");
        var room = service.JoinRoom("ARENA-ALGO-02", "bob");

        room.Players.Should().HaveCount(2);
        room.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task SubmitSolutionAsync_FirstCorrectSubmission_WinsRoomAndGainsElo()
    {
        var service = new CodingArenaService(CreateExecutor("88").Object);

        var result = await service.SubmitSolutionAsync(new ArenaSubmitRequest
        {
            RoomCode = "ARENA-LINQ-01",
            PlayerName = "alice",
            SourceCode = "Console.WriteLine(88);"
        });

        result.IsCorrectOutput.Should().BeTrue();
        result.WinnerName.Should().Be("alice");
        result.AwardedXp.Should().Be(100);
        result.EloDelta.Should().Be(25);
        result.NewEloRating.Should().Be(1225);
        service.GetRoom("ARENA-LINQ-01")!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task SubmitSolutionAsync_WrongOutput_PenalizesElo()
    {
        var service = new CodingArenaService(CreateExecutor("42").Object);

        var result = await service.SubmitSolutionAsync(new ArenaSubmitRequest
        {
            RoomCode = "ARENA-CS14-03",
            PlayerName = "bob",
            SourceCode = "Console.WriteLine(42);"
        });

        result.IsCorrectOutput.Should().BeFalse();
        result.EloDelta.Should().Be(-5);
        result.NewEloRating.Should().Be(1195);
        result.AwardedXp.Should().Be(0);
    }

    [Fact]
    public async Task PersistentArena_PersistsEloLeaderboardAndMatchHistoryAcrossServiceRestarts()
    {
        string dbName = $"ArenaPersist_{Guid.NewGuid()}";

        var services1 = new ServiceCollection();
        services1.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services1.AddSingleton(CreateExecutor("88").Object);
        using var sp1 = services1.BuildServiceProvider();

        var service1 = new CodingArenaService(sp1.GetRequiredService<IServiceScopeFactory>());
        var submitResult = await service1.SubmitSolutionAsync(new ArenaSubmitRequest
        {
            RoomCode = "ARENA-LINQ-01",
            PlayerName = "persistentCoder",
            SourceCode = "Console.WriteLine(88);"
        });

        submitResult.NewEloRating.Should().Be(1225);

        // Simulate app restart with a fresh service instance sharing the same database
        var services2 = new ServiceCollection();
        services2.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services2.AddSingleton(CreateExecutor("55").Object);
        using var sp2 = services2.BuildServiceProvider();

        var service2 = new CodingArenaService(sp2.GetRequiredService<IServiceScopeFactory>());

        var restoredRoom = service2.GetRoom("ARENA-LINQ-01");
        restoredRoom.Should().NotBeNull();
        restoredRoom!.WinnerName.Should().Be("persistentCoder");
        restoredRoom.Status.Should().Be("Completed");

        var leaderboard = service2.GetLeaderboard(10);
        leaderboard.Should().ContainSingle(p => p.PlayerName == "persistentCoder" && p.EloRating == 1225 && p.Wins == 1);

        var matches = service2.GetRecentMatches("ARENA-LINQ-01", 10);
        matches.Should().ContainSingle(m => m.PlayerName == "persistentCoder" && m.IsWinner && m.AwardedXp == 100);

        // Reset room clears active match round while preserving player ELO history
        var resetRoom = service2.ResetRoom("ARENA-LINQ-01");
        resetRoom.Status.Should().Be("Open");
        resetRoom.WinnerName.Should().BeNull();
        service2.GetLeaderboard(10).Should().ContainSingle(p => p.PlayerName == "persistentCoder" && p.EloRating == 1225);
    }
}
