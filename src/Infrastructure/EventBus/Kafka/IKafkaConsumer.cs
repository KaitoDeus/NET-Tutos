using NET_Tutos.BuildingBlocks.EventBus.Events;

namespace NET_Tutos.BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Contract for subscribing to and processing strongly-typed Kafka stream events.
/// </summary>
public interface IKafkaConsumer<in TEvent> where TEvent : class, IKafkaEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
