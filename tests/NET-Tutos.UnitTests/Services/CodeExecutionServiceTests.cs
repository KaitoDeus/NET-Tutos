using FluentAssertions;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class CodeExecutionServiceTests
{
    private readonly CodeExecutionService _service = new();

    [Fact]
    public async Task ExecuteAsync_ValidConsoleCode_ReturnsSuccessWithCapturedOutput()
    {
        // Arrange
        var code = "Console.WriteLine(\"Hello .NET 10!\");";

        // Act
        var result = await _service.ExecuteAsync(code);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Output.Trim().Should().Be("Hello .NET 10!");
        result.Error.Should().BeNullOrEmpty();
        result.IsTimeout.Should().BeFalse();
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ExecuteAsync_EmptyOrWhitespaceCode_ReturnsFailureWithoutExecuting(string? code)
    {
        // Act
        var result = await _service.ExecuteAsync(code!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Mã nguồn không được để trống");
    }

    [Theory]
    [InlineData("System.Diagnostics.Process.Start(\"cmd.exe\");")]
    [InlineData("Process.Start(\"calc.exe\");")]
    [InlineData("System.IO.File.WriteAllText(\"test.txt\", \"data\");")]
    [InlineData("System.IO.Directory.CreateDirectory(\"hack\");")]
    [InlineData("System.Environment.Exit(1);")]
    public async Task ExecuteAsync_ForbiddenKeywords_BlocksExecutionWithSecurityError(string maliciousCode)
    {
        // Act
        var result = await _service.ExecuteAsync(maliciousCode);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("bị cấm vì lý do an toàn bảo mật");
        result.Output.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_CompilationError_ReturnsCompilerDiagnostics()
    {
        // Arrange (Syntax error: missing semicolon and invalid type)
        var code = "int a = \"invalid string\"";

        // Act
        var result = await _service.ExecuteAsync(code);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.Error.Should().ContainAny("CS", "error", ";");
    }

    [Fact]
    public async Task ExecuteAsync_RuntimeDivisionByZeroException_CapturesAndReturnsDetails()
    {
        // Arrange
        var code = @"
int divisor = 0;
int result = 100 / divisor;
Console.WriteLine(result);
";

        // Act
        var result = await _service.ExecuteAsync(code);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().ContainAny("DivideByZeroException", "zero");
    }

    [Fact]
    public async Task ExecuteAsync_InfiniteLoop_TimesOutGracefully()
    {
        // Arrange (Run with tight 300ms timeout)
        var code = @"await Task.Delay(3000);";

        // Act
        var result = await _service.ExecuteAsync(code, timeoutMs: 300);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsTimeout.Should().BeTrue();
        result.Error.Should().ContainAny("vượt quá giới hạn", "quá thời gian", "timeout");
    }

    [Fact]
    public async Task EvaluateChallengeAsync_AllMatchingCases_ReturnsAllPassed()
    {
        // Arrange
        var challenge = new CodingChallenge
        {
            Id = 1,
            Title = "Add Two Numbers",
            TestCases = new List<CodeTestCase>
            {
                new() { Id = 1, InputParameters = "1, 2", ExpectedOutput = "3" },
                new() { Id = 2, InputParameters = "10, -5", ExpectedOutput = "5" }
            }
        };

        var solutionCode = @"
public class Solution
{
    public static int Add(int a, int b) => a + b;
}
";

        // Act
        var response = await _service.EvaluateChallengeAsync(challenge, solutionCode);

        // Assert
        if (!string.IsNullOrEmpty(response.CompileError))
        {
            Assert.Fail($"Compilation unexpectedly failed: {response.CompileError}");
        }

        response.Should().NotBeNull();
        response.AllPassed.Should().BeTrue();
        response.PassedTestsCount.Should().Be(2);
        response.TotalTestsCount.Should().Be(2);
        response.Details.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
    }

    [Fact]
    public async Task EvaluateChallengeAsync_FailingTestCase_IdentifiesMismatchedOutput()
    {
        // Arrange
        var challenge = new CodingChallenge
        {
            Id = 2,
            Title = "Add Numbers",
            TestCases = new List<CodeTestCase>
            {
                new() { Id = 1, InputParameters = "5, 3", ExpectedOutput = "8" } // 5 - 3 = 2 != 8
            }
        };

        var solutionCode = @"
public class Solution
{
    public static int Add(int a, int b) => a - b;
}
";

        // Act
        var response = await _service.EvaluateChallengeAsync(challenge, solutionCode);

        // Assert
        if (!string.IsNullOrEmpty(response.CompileError))
        {
            Assert.Fail($"Compilation unexpectedly failed: {response.CompileError}");
        }

        response.Should().NotBeNull();
        response.AllPassed.Should().BeFalse();
        response.PassedTestsCount.Should().Be(0);
        response.Details.Should().ContainSingle();
        response.Details[0].Passed.Should().BeFalse();
        response.Details[0].Actual.Should().Be("2");
        response.Details[0].Expected.Should().Be("8");
    }
}
