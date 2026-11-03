using FluentAssertions;
using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Infrastructure.Engines;
using Xunit;

namespace NET_Tutos.AiTutor.Tests;

public class DeterministicAiTutorEngineTests
{
    private readonly DeterministicAiTutorEngine _engine = new();

    [Fact]
    public async Task AnalyzeComplexityAsync_SingleLoop_ReturnsLinearOOfN()
    {
        var request = new AnalyzeComplexityRequestDto
        {
            SourceCode = "for (int i = 0; i < items.Length; i++) { total += items[i]; }",
            IsEnglish = true
        };

        var result = await _engine.AnalyzeComplexityAsync(request);

        result.Success.Should().BeTrue();
        result.TimeComplexity.Should().Be("O(N)");
        result.SpaceComplexity.Should().Be("O(1)");
        result.EfficiencyScore.Should().BeGreaterThanOrEqualTo(90);
    }

    [Fact]
    public async Task ExplainErrorAsync_CS1002_ReturnsMissingSemicolonFix()
    {
        var request = new ExplainErrorRequestDto
        {
            ErrorMessage = "(4,15): error CS1002: ; expected",
            SourceCode = "int x = 5",
            IsEnglish = false
        };

        var result = await _engine.ExplainErrorAsync(request);

        result.Success.Should().BeTrue();
        result.ErrorCode.Should().Be("CS1002");
        result.LineNumber.Should().Be(4);
        result.SuggestedFixCode.Should().Contain(";");
    }

    [Fact]
    public async Task DiagnoseTestFailureAsync_OffByOneDifference_CategorizesCorrectly()
    {
        var request = new DiagnoseTestFailureRequestDto
        {
            InputParameters = "[1, 2, 3, 4]",
            ExpectedOutput = "4",
            ActualOutput = "3",
            IsEnglish = true
        };

        var result = await _engine.DiagnoseTestFailureAsync(request);

        result.Success.Should().BeTrue();
        result.FailureCategory.Should().Be("Off-by-one");
        result.SuggestedFixSnippet.Should().Contain("i < n");
    }

    [Fact]
    public async Task GenerateUnitTestAsync_ValidMethod_ReturnsXUnitSuite()
    {
        var request = new GenerateUnitTestRequestDto
        {
            SourceCode = "public class MathHelper { public int Add(int a, int b) => a + b; }",
            IsEnglish = true
        };

        var result = await _engine.GenerateUnitTestAsync(request);

        result.Success.Should().BeTrue();
        result.TestFramework.Should().Contain("xUnit");
        result.TestCode.Should().Contain("public class AddTests");
        result.TestCode.Should().Contain("[Fact]");
    }

    [Fact]
    public async Task EvaluateInterviewAnswerAsync_SeniorResponse_AwardsHighScore()
    {
        var request = new EvaluateInterviewRequestDto
        {
            FlashcardId = 1,
            Topic = "Async",
            Question = "How does async/await work under the hood?",
            CandidateAnswer = "Async/await uses a non-blocking model where the compiler synthesizes a state machine struct, and we pass CancellationToken across I/O boundaries.",
            IsEnglish = true
        };

        var result = await _engine.EvaluateInterviewAnswerAsync(request);

        result.Success.Should().BeTrue();
        result.OverallScore.Should().BeGreaterThanOrEqualTo(75);
        result.Strengths.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckStatusAsync_ReturnsHealthyStatus()
    {
        var status = await _engine.CheckStatusAsync();

        status.Success.Should().BeTrue();
        status.IsAvailable.Should().BeTrue();
        status.Provider.Should().Contain("Atus AI Microservice Engine");
    }
}
