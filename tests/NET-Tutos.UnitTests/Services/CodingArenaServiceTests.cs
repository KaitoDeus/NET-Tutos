using FluentAssertions;
using Moq;
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
}
