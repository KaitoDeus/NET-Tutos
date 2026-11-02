using FluentAssertions;
using NET_Tutos.CodeRunner.Domain.Exceptions;
using NET_Tutos.CodeRunner.Domain.ValueObjects;
using Xunit;

namespace NET_Tutos.CodeRunner.Tests;

public class CodeRunnerDomainTests
{
    [Fact]
    public void SourceCode_ValidSnippet_CalculatesLineCount()
    {
        var snippet = "int a = 1;\nint b = 2;\nreturn a + b;";
        var code = new SourceCode(snippet);

        code.Code.Should().Be(snippet);
        code.LineCount.Should().Be(3);
        ((string)code).Should().Be(snippet);
    }

    [Fact]
    public void SourceCode_ForbiddenNamespace_ThrowsForbiddenSyntaxException()
    {
        var snippet = "System.IO.File.Delete(\"test.txt\");";
        var code = new SourceCode(snippet);

        var act = () => code.ValidateConstraints();
        act.Should().Throw<ForbiddenSyntaxException>()
           .WithMessage("*System.IO*");
    }

    [Theory]
    [InlineData(100, 500)]       // Minimum clamp
    [InlineData(4000, 4000)]     // Within range
    [InlineData(50000, 30000)]   // Maximum clamp
    public void ExecutionTimeout_ClampsToSafeBoundaries(int inputMs, int expectedMs)
    {
        var timeout = new ExecutionTimeout(inputMs);
        timeout.Duration.TotalMilliseconds.Should().Be(expectedMs);
    }
}
