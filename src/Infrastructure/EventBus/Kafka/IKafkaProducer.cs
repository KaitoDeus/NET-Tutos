using NET_Tutos.BuildingBlocks.EventBus.Events;

namespace NET_Tutos.BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Resilient, high-throughput producer for publishing strongly-typed events to Apache Kafka topics.
/// </summary>
public interface IKafkaProducer : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Indicates whether the producer is active and connected to a Kafka broker.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Asynchronously publishes an event to a Kafka topic. Returns true if acknowledged.
    /// Never throws unhandled exceptions; falls back safely.
    /// </summary>
    Task<bool> ProduceAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IKafkaEvent;

    /// <summary>
    /// Fire-and-forget non-blocking publication for high-frequency telemetry.
    /// Completely decouples publisher from the calling thread with zero latency impact.
    /// </summary>
    void ProduceFireAndForget<TEvent>(string topic, TEvent @event)
        where TEvent : class, IKafkaEvent;

    /// <summary>
    /// Retrieves current operational and throughput diagnostics of the Kafka pipeline.
    /// </summary>
    Task<KafkaTelemetrySnapshot> GetDiagnosticsAsync();
}
