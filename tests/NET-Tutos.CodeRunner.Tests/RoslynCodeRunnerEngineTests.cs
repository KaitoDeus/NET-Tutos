using FluentAssertions;
using NET_Tutos.CodeRunner.API.Contracts;
using NET_Tutos.CodeRunner.API.Services;
using Xunit;

namespace NET_Tutos.CodeRunner.Tests;

public class RoslynCodeRunnerEngineTests
{
    private readonly RoslynCodeRunnerEngine _engine = new();

    [Fact]
    public async Task ExecuteAsync_SimpleConsolePrint_CapturesStandardOutput()
    {
        // Arrange
        var code = "Console.WriteLine(\"Microservice CodeRunner Active!\");";

        // Act
        var result = await _engine.ExecuteAsync(code);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Output.Trim().Should().Be("Microservice CodeRunner Active!");
        result.Error.Should().BeNullOrEmpty();
        result.IsTimeout.Should().BeFalse();
    }

    [Theory]
    [InlineData("System.Diagnostics.Process.Start(\"cmd\");")]
    [InlineData("System.IO.File.Delete(\"test.txt\");")]
    [InlineData("System.Environment.Exit(0);")]
    public async Task ExecuteAsync_BlacklistedKeywords_RejectsWithSecurityError(string maliciousScript)
    {
        // Act
        var result = await _engine.ExecuteAsync(maliciousScript);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("bị cấm vì lý do an toàn bảo mật");
    }

    [Fact]
    public async Task EvaluateChallengeAsync_ValidSolution_PassesAllTestCases()
    {
        // Arrange
        var request = new EvaluateChallengeRequest
        {
            ChallengeId = 1,
            SolutionCode = @"
public class Solution
{
    public static int Multiply(int a, int b) => a * b;
}
",
            TestCases = new List<EvaluateTestCaseDto>
            {
                new() { Id = 1, InputParameters = "3, 4", ExpectedOutput = "12", IsHidden = false },
                new() { Id = 2, InputParameters = "-2, 5", ExpectedOutput = "-10", IsHidden = true }
            }
        };

        // Act
        var response = await _engine.EvaluateChallengeAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.AllPassed.Should().BeTrue();
        response.PassedTestsCount.Should().Be(2);
        response.TotalTestsCount.Should().Be(2);
        response.Details.Should().AllSatisfy(d => d.Passed.Should().BeTrue());
    }

    [Fact]
    public async Task ExecuteAsync_InfiniteExecution_TimesOutGracefully()
    {
        // Arrange (Run with tight 300ms timeout)
        var code = "await Task.Delay(3000);";

        // Act
        var result = await _engine.ExecuteAsync(code, timeoutMs: 300);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsTimeout.Should().BeTrue();
        result.Error.Should().ContainAny("vượt quá giới hạn", "quá thời gian", "timeout");
    }
}
