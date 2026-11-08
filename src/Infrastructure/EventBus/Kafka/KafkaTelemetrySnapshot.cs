namespace NET_Tutos.BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Point-in-time health, performance, and throughput snapshot of the Kafka producer.
/// </summary>
public record KafkaTelemetrySnapshot(
    bool IsConfigured,
    bool IsHealthy,
    string BootstrapServers,
    long MessagesProduced,
    long MessagesFailed,
    long FallbackInMemCount,
    string? LastError,
    DateTime Timestamp
);
