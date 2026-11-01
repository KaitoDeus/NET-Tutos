using FluentAssertions;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class AiTutorServiceTests
{
    [Theory]
    [InlineData("How does linq deferred execution work?", true)]
    [InlineData("Giải thích linq trong C#", false)]
    public async Task ChatAsync_LinqQuery_ReturnsStructuredLinqKnowledge(string query, bool isEnglish)
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiTutorService(context);
        var request = new AiTutorChatRequest { Message = query };

        // Act
        var response = await service.ChatAsync(request, isEnglish);

        // Assert
        response.Should().NotBeNull();
        response.ResponseMarkdown.Should().ContainAny("LINQ", "Deferred Execution", "Thực thi");
        response.CodeSnippet.Should().NotBeNullOrEmpty();
        response.SuggestedFollowUps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExplainErrorAsync_MissingSemicolon_ReturnsDiagnosticAndFix()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiTutorService(context);
        var request = new AiExplainErrorRequest
        {
            ErrorMessage = "error CS1002: ; expected",
            SourceCode = "int a = 10\nConsole.WriteLine(a);"
        };

        // Act
        var response = await service.ExplainErrorAsync(request, isEnglish: true);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.ErrorCode.Should().Be("CS1002");
        response.HowToFix.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AnalyzeComplexityAsync_SingleLoop_ReturnsOOfN()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiTutorService(context);
        var request = new AiComplexityRequest
        {
            SourceCode = @"
public int SumArray(int[] arr)
{
    int sum = 0;
    for (int i = 0; i < arr.Length; i++) {
        sum += arr[i];
    }
    return sum;
}
"
        };

        // Act
        var response = await service.AnalyzeComplexityAsync(request, isEnglish: true);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.TimeComplexity.Should().ContainAny("O(n)", "O(N)");
        response.SpaceComplexity.Should().ContainAny("O(1)", "constant");
    }

    [Fact]
    public async Task GenerateUnitTestAsync_ValidMethod_ProducesArrangeActAssertSuite()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiTutorService(context);
        var request = new AiGenerateUnitTestRequest
        {
            SourceCode = @"
public class Calculator
{
    public int Multiply(int a, int b) => a * b;
}
"
        };

        // Act
        var response = await service.GenerateUnitTestAsync(request, isEnglish: true);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.TestCode.Should().Contain("[Fact]");
        response.TestCode.Should().ContainAny("// Arrange", "// Act", "// Assert");
        response.TestFramework.Should().Contain("xUnit");
    }

    [Fact]
    public async Task EvaluateInterviewAnswerAsync_StrongAnswer_ReturnsHighTechnicalScore()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var service = new AiTutorService(context);
        var request = new AiInterviewEvaluationRequest
        {
            Topic = "Async",
            Question = "Explain how async and await work in C# and ASP.NET Core.",
            CandidateAnswer = "async and await provide a non-blocking execution model. Under the hood, the Roslyn compiler synthesizes a state machine struct to manage continuations. This returns threads to the thread pool to scale throughput. In production, we should propagate a CancellationToken to prevent wasted computation."
        };

        // Act
        var response = await service.EvaluateInterviewAnswerAsync(request, isEnglish: true);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.OverallScore.Should().BeGreaterThanOrEqualTo(70);
        response.Strengths.Should().NotBeEmpty();
    }
}
