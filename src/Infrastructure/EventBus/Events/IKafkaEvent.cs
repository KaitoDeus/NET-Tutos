namespace NET_Tutos.BuildingBlocks.EventBus.Events;

/// <summary>
/// Contract for high-throughput stream events published to Apache Kafka partitions.
/// </summary>
public interface IKafkaEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string PartitionKey { get; }
}

/// <summary>
/// Base record for all Apache Kafka stream events in NET-Tutos.
/// </summary>
public abstract record KafkaEvent : IKafkaEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public abstract string PartitionKey { get; }
}
