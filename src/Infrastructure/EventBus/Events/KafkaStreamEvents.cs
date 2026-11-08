namespace NET_Tutos.BuildingBlocks.EventBus.Events;

/// <summary>
/// Event produced whenever Atus AI interacts with a user (live chat, code review, compiler fix).
/// </summary>
public record AtusAiTelemetryEvent(
    string SessionId,
    string? UserId,
    string PromptSnippet,
    string ModelUsed,
    long LatencyMs,
    bool IsSuccess,
    bool IsFallback,
    string? ErrorType = null,
    string? UserRole = null
) : KafkaEvent
{
    public override string PartitionKey => UserId ?? SessionId ?? Guid.NewGuid().ToString();
}

/// <summary>
/// Event produced on any learner study action (viewing lesson, completing quiz, reviewing flashcard).
/// </summary>
public record UserLearningActivityEvent(
    string UserId,
    string ActivityType,
    string EntityId,
    string? Title = null,
    int? XpEarned = null,
    int DurationSeconds = 0,
    Dictionary<string, string>? Metadata = null
) : KafkaEvent
{
    public override string PartitionKey => UserId;
}

/// <summary>
/// Event produced whenever C# code is compiled and executed in the sandbox.
/// </summary>
public record CodeExecutionTelemetryEvent(
    string ExecutionId,
    string? UserId,
    string Language,
    long ExecutionDurationMs,
    long MemoryBytes,
    bool IsSuccess,
    bool HasCompileErrors,
    string? ErrorSnippet = null
) : KafkaEvent
{
    public override string PartitionKey => UserId ?? ExecutionId;
}
