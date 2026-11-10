using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using NET_Tutos.BuildingBlocks.EventBus.Events;

namespace NET_Tutos.Services;

public sealed record KafkaStreamEventEntry(
    DateTime Timestamp,
    string Topic,
    string PartitionKey,
    string Summary,
    long DurationMs,
    bool IsSuccess
);

public sealed record KafkaConsumerAnalyticsSnapshot(
    long TotalEventsConsumed,
    long AiTelemetryEventsConsumed,
    long CodeExecutionEventsConsumed,
    long LearningActivityEventsConsumed,
    long FallbackAiCount,
    double AverageAiLatencyMs,
    double AverageCodeExecutionMs,
    Dictionary<string, long> TopAiTopics,
    Dictionary<string, long> TopCompilerErrors,
    List<KafkaStreamEventEntry> RecentEvents,
    DateTime SnapshotAt
);

public sealed record AdaptiveWeakSpotDto(
    string Topic,
    int HitCount,
    int LowestScore,
    string Source
);

public interface IKafkaStreamAnalyticsStore
{
    void RecordAiTelemetry(AtusAiTelemetryEvent @event);
    void RecordCodeExecution(CodeExecutionTelemetryEvent @event);
    void RecordLearningActivity(UserLearningActivityEvent @event);
    void UpdateTopicLag(string topic, long lagMessages);
    Dictionary<string, long> GetTopicLags();

    KafkaConsumerAnalyticsSnapshot GetSnapshot();
    List<AdaptiveWeakSpotDto> GetUserWeakSpots(string? userId);
}

/// <summary>
/// Thread-safe real-time stream analytics store hydrated directly by the Kafka Consumer worker.
/// Aggregates live latency metrics, compiler error trends, AI query topics, and per-user adaptive weak spots.
/// </summary>
public sealed class KafkaStreamAnalyticsStore : IKafkaStreamAnalyticsStore
{
    private const int MaxRecentEvents = 30;

    private long _aiEventsConsumed;
    private long _aiFallbackCount;
    private long _aiTotalLatencyMs;
    private long _codeEventsConsumed;
    private long _codeTotalDurationMs;
    private long _learningEventsConsumed;

    private readonly ConcurrentQueue<KafkaStreamEventEntry> _recentEvents = new();
    private readonly ConcurrentDictionary<string, long> _topAiTopics = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _topCompilerErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _topicLagMessages = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AdaptiveSignal>> _userSignals = new(StringComparer.Ordinal);

    public KafkaStreamAnalyticsStore()
    {
        _topicLagMessages.TryAdd(KafkaTopics.AtusAiTelemetry, 0);
        _topicLagMessages.TryAdd(KafkaTopics.CodeExecutionTelemetry, 0);
        _topicLagMessages.TryAdd(KafkaTopics.UserLearningActivity, 0);
    }

    private static readonly (string Keyword, string DisplayTopic)[] KnownTopics =
    [
        ("async", "Async/Await & ThreadPool"),
        ("await", "Async/Await & ThreadPool"),
        ("task", "Async/Await & ThreadPool"),
        ("ef core", "Entity Framework Core"),
        ("entity framework", "Entity Framework Core"),
        ("linq", "LINQ & Deferred Execution"),
        ("dependency injection", "Dependency Injection & Lifetimes"),
        ("singleton", "Dependency Injection & Lifetimes"),
        ("scoped", "Dependency Injection & Lifetimes"),
        ("clean architecture", "Clean Architecture & DDD"),
        ("solid", "SOLID Principles"),
        ("kafka", "Apache Kafka & Event Streaming"),
        ("rabbitmq", "RabbitMQ & MassTransit"),
        ("redis", "Redis Caching"),
        ("docker", "Docker & Containers"),
        ("record", "Modern C# Features"),
        ("pattern matching", "Modern C# Features"),
        ("gc", "Memory & Garbage Collection"),
        ("span", "Memory & Span<T>")
    ];

    private static readonly Regex CompilerErrorRegex = new(@"CS\d{4}", RegexOptions.Compiled);

    public void RecordAiTelemetry(AtusAiTelemetryEvent @event)
    {
        Interlocked.Increment(ref _aiEventsConsumed);
        if (@event.IsFallback)
        {
            Interlocked.Increment(ref _aiFallbackCount);
        }

        if (@event.LatencyMs > 0)
        {
            Interlocked.Add(ref _aiTotalLatencyMs, @event.LatencyMs);
        }

        var promptLower = (@event.PromptSnippet ?? string.Empty).ToLowerInvariant();
        foreach (var (kw, display) in KnownTopics)
        {
            if (promptLower.Contains(kw))
            {
                _topAiTopics.AddOrUpdate(display, 1, (_, count) => count + 1);
            }
        }

        EnqueueRecent(new KafkaStreamEventEntry(
            @event.OccurredOn,
            KafkaTopics.AtusAiTelemetry,
            MaskKey(@event.PartitionKey),
            $"Atus Chat [{@event.ModelUsed}] (Fallback={@event.IsFallback})",
            @event.LatencyMs,
            @event.IsSuccess
        ));
    }

    public void RecordCodeExecution(CodeExecutionTelemetryEvent @event)
    {
        Interlocked.Increment(ref _codeEventsConsumed);
        if (@event.ExecutionDurationMs > 0)
        {
            Interlocked.Add(ref _codeTotalDurationMs, @event.ExecutionDurationMs);
        }

        if (!@event.IsSuccess && !string.IsNullOrWhiteSpace(@event.ErrorSnippet))
        {
            var match = CompilerErrorRegex.Match(@event.ErrorSnippet);
            string errKey = match.Success
                ? match.Value
                : (@event.HasCompileErrors ? "CompileError" : "RuntimeException");

            _topCompilerErrors.AddOrUpdate(errKey, 1, (_, count) => count + 1);

            if (!string.IsNullOrWhiteSpace(@event.UserId))
            {
                RecordUserSignal(@event.UserId, errKey switch
                {
                    "CS0103" => "C# Scope & Variable Declaration",
                    "CS0029" => "C# Type Conversion & Casting",
                    "CS0246" => "C# Namespaces & Assembly References",
                    "CS1002" => "C# Syntax Fundamentals",
                    _ => "C# Compilation & Debugging"
                }, 50, "CodeExecution");
            }
        }

        EnqueueRecent(new KafkaStreamEventEntry(
            @event.OccurredOn,
            KafkaTopics.CodeExecutionTelemetry,
            MaskKey(@event.PartitionKey),
            $"CodeRunner [{@event.Language}] (CompileErr={@event.HasCompileErrors})",
            @event.ExecutionDurationMs,
            @event.IsSuccess
        ));
    }

    public void RecordLearningActivity(UserLearningActivityEvent @event)
    {
        Interlocked.Increment(ref _learningEventsConsumed);

        if (!string.IsNullOrWhiteSpace(@event.UserId) && @event.Metadata != null)
        {
            if (@event.Metadata.TryGetValue("score", out var scoreStr) && int.TryParse(scoreStr, out int score) && score < 75)
            {
                var topic = @event.Metadata.TryGetValue("topic", out var t) && !string.IsNullOrWhiteSpace(t)
                    ? t
                    : (@event.Title ?? ".NET Architecture");

                RecordUserSignal(@event.UserId, topic, score, @event.ActivityType);
            }
        }

        EnqueueRecent(new KafkaStreamEventEntry(
            @event.OccurredOn,
            KafkaTopics.UserLearningActivity,
            MaskKey(@event.PartitionKey),
            $"Activity [{@event.ActivityType}]: {@event.Title ?? @event.EntityId}",
            @event.DurationSeconds * 1000L,
            true
        ));
    }

    public void UpdateTopicLag(string topic, long lagMessages)
    {
        if (string.IsNullOrWhiteSpace(topic)) return;
        _topicLagMessages[topic] = Math.Max(0, lagMessages);
    }

    public Dictionary<string, long> GetTopicLags()
    {
        return _topicLagMessages.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    public KafkaConsumerAnalyticsSnapshot GetSnapshot()
    {
        long aiCount = Interlocked.Read(ref _aiEventsConsumed);
        long codeCount = Interlocked.Read(ref _codeEventsConsumed);
        long learningCount = Interlocked.Read(ref _learningEventsConsumed);
        long fallbackCount = Interlocked.Read(ref _aiFallbackCount);
        long aiLatTotal = Interlocked.Read(ref _aiTotalLatencyMs);
        long codeDurTotal = Interlocked.Read(ref _codeTotalDurationMs);

        return new KafkaConsumerAnalyticsSnapshot(
            TotalEventsConsumed: aiCount + codeCount + learningCount,
            AiTelemetryEventsConsumed: aiCount,
            CodeExecutionEventsConsumed: codeCount,
            LearningActivityEventsConsumed: learningCount,
            FallbackAiCount: fallbackCount,
            AverageAiLatencyMs: aiCount > 0 ? Math.Round((double)aiLatTotal / aiCount, 1) : 0,
            AverageCodeExecutionMs: codeCount > 0 ? Math.Round((double)codeDurTotal / codeCount, 1) : 0,
            TopAiTopics: _topAiTopics.OrderByDescending(kv => kv.Value).Take(6).ToDictionary(kv => kv.Key, kv => kv.Value),
            TopCompilerErrors: _topCompilerErrors.OrderByDescending(kv => kv.Value).Take(6).ToDictionary(kv => kv.Key, kv => kv.Value),
            RecentEvents: _recentEvents.Reverse().ToList(),
            SnapshotAt: DateTime.UtcNow
        );
    }

    public List<AdaptiveWeakSpotDto> GetUserWeakSpots(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || !_userSignals.TryGetValue(userId, out var signals))
        {
            return [];
        }

        return signals.Select(kv => new AdaptiveWeakSpotDto(
                kv.Key,
                kv.Value.HitCount,
                kv.Value.LowestScore,
                kv.Value.Source
            ))
            .OrderByDescending(w => w.HitCount)
            .ThenBy(w => w.LowestScore)
            .Take(5)
            .ToList();
    }

    private void RecordUserSignal(string userId, string topic, int score, string source)
    {
        var userMap = _userSignals.GetOrAdd(userId, _ => new ConcurrentDictionary<string, AdaptiveSignal>(StringComparer.OrdinalIgnoreCase));
        userMap.AddOrUpdate(
            topic,
            _ => new AdaptiveSignal(1, score, source),
            (_, existing) => new AdaptiveSignal(existing.HitCount + 1, Math.Min(existing.LowestScore, score), source)
        );
    }

    private void EnqueueRecent(KafkaStreamEventEntry entry)
    {
        _recentEvents.Enqueue(entry);
        while (_recentEvents.Count > MaxRecentEvents)
        {
            _recentEvents.TryDequeue(out _);
        }
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "anonymous";
        return key.Length <= 8 ? key : key[..4] + "***" + key[^4..];
    }

    private sealed record AdaptiveSignal(int HitCount, int LowestScore, string Source);
}
