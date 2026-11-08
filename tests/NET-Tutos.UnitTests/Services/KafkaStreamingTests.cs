using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NET_Tutos.BuildingBlocks.EventBus.Events;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class KafkaStreamingTests
{
    private readonly Mock<ILogger<KafkaProducer>> _loggerMock = new();

    [Fact]
    public async Task KafkaProducer_WhenDisabled_OperatesInResilientFallbackMode()
    {
        // Arrange
        var options = Options.Create(new KafkaOptions
        {
            Enabled = false,
            BootstrapServers = ""
        });

        using var producer = new KafkaProducer(options, _loggerMock.Object);

        var telemetryEvent = new AtusAiTelemetryEvent(
            SessionId: "session-123",
            UserId: "user-456",
            PromptSnippet: "How to use async/await?",
            ModelUsed: "atus-flash",
            LatencyMs: 150,
            IsSuccess: true,
            IsFallback: false
        );

        // Act & Assert - ProduceAsync should return false safely without throwing
        var result = await producer.ProduceAsync(KafkaTopics.AtusAiTelemetry, telemetryEvent);
        result.Should().BeFalse();

        // Fire and forget should not throw
        var act = () => producer.ProduceFireAndForget(KafkaTopics.AtusAiTelemetry, telemetryEvent);
        act.Should().NotThrow();

        // Diagnostics should report valid fallback telemetry
        var diag = await producer.GetDiagnosticsAsync();
        diag.Should().NotBeNull();
        diag.IsConfigured.Should().BeFalse();
        diag.IsHealthy.Should().BeFalse();
        diag.FallbackInMemCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void KafkaEvent_PartitionKeys_AreDeterministic()
    {
        // Arrange
        var atusEvent = new AtusAiTelemetryEvent(
            SessionId: "sess-abc",
            UserId: "usr-xyz",
            PromptSnippet: "Explain DI",
            ModelUsed: "atus",
            LatencyMs: 120,
            IsSuccess: true,
            IsFallback: false
        );

        var learningEvent = new UserLearningActivityEvent(
            UserId: "learner-01",
            ActivityType: "LessonView",
            EntityId: "lesson-efcore-101",
            Title: "EF Core 10 Performance"
        );

        var codeEvent = new CodeExecutionTelemetryEvent(
            ExecutionId: "exec-999",
            UserId: "usr-dev",
            Language: "csharp",
            ExecutionDurationMs: 45,
            MemoryBytes: 1024,
            IsSuccess: true,
            HasCompileErrors: false
        );

        // Assert
        atusEvent.PartitionKey.Should().Be("usr-xyz");
        learningEvent.PartitionKey.Should().Be("learner-01");
        codeEvent.PartitionKey.Should().Be("usr-dev");
        KafkaTopics.AtusAiTelemetry.Should().Be("nettutos.atus.ai-telemetry");
        KafkaTopics.UserLearningActivity.Should().Be("nettutos.learning.user-activities");
        KafkaTopics.CodeExecutionTelemetry.Should().Be("nettutos.coderunner.executions");
    }

    [Fact]
    public void KafkaProducer_WhenUnreachableBroker_InitializesWithoutCrashingApp()
    {
        // Arrange - Unreachable dummy address with 100ms timeout
        var options = Options.Create(new KafkaOptions
        {
            Enabled = true,
            BootstrapServers = "127.0.0.1:59999",
            TimeoutMs = 100,
            MessageTimeoutMs = 100
        });

        // Act & Assert - Constructing producer should never crash the service host
        var act = () =>
        {
            using var producer = new KafkaProducer(options, _loggerMock.Object);
            producer.Should().NotBeNull();
        };

        act.Should().NotThrow();
    }
}
