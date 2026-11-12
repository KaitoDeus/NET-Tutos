using FluentAssertions;
using NET_Tutos.BuildingBlocks.Common.Sandbox;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class SandboxGuardrailsTests
{
    [Fact]
    public void BoundedStringWriter_UnderLimit_PreservesFullTextWithoutTruncation()
    {
        using var writer = new BoundedStringWriter(64);
        writer.Write("Hello ");
        writer.WriteLine("NET-Tutos!");

        writer.IsTruncated.Should().BeFalse();
        writer.ToString().Should().Contain("Hello NET-Tutos!");
    }

    [Fact]
    public void BoundedStringWriter_ExceedingLimit_ClampsAndAppendsNoticeOnce()
    {
        using var writer = new BoundedStringWriter(20);
        writer.Write(new string('A', 50));
        writer.Write(new string('B', 50));

        writer.IsTruncated.Should().BeTrue();
        string output = writer.ToString();
        output.StartsWith(new string('A', 20)).Should().BeTrue();
        output.Should().Contain("Output truncated at 20 characters");
        output.Should().NotContain("B");
    }

    [Theory]
    [InlineData(-100, SandboxGuardrails.DefaultTimeoutMs)]
    [InlineData(0, SandboxGuardrails.DefaultTimeoutMs)]
    [InlineData(50, SandboxGuardrails.MinExecutionTimeoutMs)]
    [InlineData(300, 300)]
    [InlineData(999999, SandboxGuardrails.MaxExecutionTimeoutMs)]
    public void ClampTimeoutMs_EnforcesSafeExecutionBounds(int requestedMs, int expectedMs)
    {
        SandboxGuardrails.ClampTimeoutMs(requestedMs).Should().Be(expectedMs);
    }

    [Fact]
    public void ValidateSecurityAndLimits_BlocksOversizedLinesAndDangerousApis()
    {
        string oversized = new('x', SandboxGuardrails.MaxSourceCodeLength + 1);
        SandboxGuardrails.ValidateSecurityAndLimits(oversized).Should().Contain("kích thước");

        string tooManyLines = string.Join('\n', Enumerable.Repeat("int x = 1;", SandboxGuardrails.MaxLineCount + 5));
        SandboxGuardrails.ValidateSecurityAndLimits(tooManyLines).Should().Contain("số dòng");

        SandboxGuardrails.ValidateSecurityAndLimits("var env = Environment.GetEnvironmentVariable(\"SECRET\");")
            .Should().Contain("Environment.GetEnvironmentVariable");
        SandboxGuardrails.ValidateSecurityAndLimits("[DllImport(\"kernel32.dll\")] static extern void Foo();")
            .Should().Contain("DllImport");

        SandboxGuardrails.ValidateSecurityAndLimits("Enumerable.Range(1, 5).Sum();")
            .Should().BeEmpty();
    }
}
