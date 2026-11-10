using FluentAssertions;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class AdaptiveChallengeGeneratorTests
{
    [Theory]
    [InlineData("Async/Await & ThreadPool", false)]
    [InlineData("LINQ & Deferred Execution", true)]
    [InlineData("C# Scope CS0103", false)]
    [InlineData("Clean Architecture & Records", true)]
    public async Task GenerateAdaptiveChallengeAsync_SolutionCodeExecutesCleanlyInRoslynSandbox(string topic, bool isEn)
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var tutor = new AiTutorService(context);
        var runner = new CodeExecutionService();

        // Act
        var challenge = await tutor.GenerateAdaptiveChallengeAsync(new AiAdaptiveChallengeRequest
        {
            Topic = topic,
            Difficulty = "Intermediate"
        }, isEn);

        var execResult = await runner.ExecuteAsync(challenge.SolutionCode, timeoutMs: 5000);

        // Assert
        challenge.Success.Should().BeTrue();
        challenge.StarterCode.Should().NotBeNullOrWhiteSpace();
        challenge.ExpectedOutput.Should().NotBeNullOrWhiteSpace();

        execResult.IsSuccess.Should().BeTrue($"Solution for '{topic}' failed: {execResult.Error}");
        execResult.Output.Trim().Should().Be(challenge.ExpectedOutput.Trim());
    }
}
