using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NET_Tutos.BuildingBlocks.EventBus.Events;

namespace NET_Tutos.BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Enterprise-grade, resilient Kafka producer supporting automatic fallback,
/// partition-aware hashing, JSON serialization, and non-blocking fire-and-forget delivery.
/// </summary>
public class KafkaProducer : IKafkaProducer
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<string, string>? _producer;
    private readonly bool _isAvailable;

    private long _messagesProduced;
    private long _messagesFailed;
    private long _fallbackInMemCount;
    private string? _lastError;
    private DateTime _lastErrorTime = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _options = options.Value ?? new KafkaOptions();
        _logger = logger;

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            _logger.LogInformation("Kafka Event Streaming is disabled or bootstrap servers not configured. Running in Resilient Fallback Mode.");
            _isAvailable = false;
            _producer = null;
            return;
        }

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                ClientId = _options.ClientId,
                SocketTimeoutMs = _options.TimeoutMs,
                MessageTimeoutMs = _options.MessageTimeoutMs,
                Acks = _options.Acks switch
                {
                    0 => Acks.None,
                    -1 => Acks.All,
                    _ => Acks.Leader
                },
                CompressionType = _options.CompressionType?.ToLowerInvariant() switch
                {
                    "gzip" => Confluent.Kafka.CompressionType.Gzip,
                    "lz4" => Confluent.Kafka.CompressionType.Lz4,
                    "zstd" => Confluent.Kafka.CompressionType.Zstd,
                    "none" => Confluent.Kafka.CompressionType.None,
                    _ => Confluent.Kafka.CompressionType.Snappy
                },
                LingerMs = 5,
                BatchSize = 65536,
                EnableIdempotence = _options.Acks == -1
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    RecordError($"Kafka Client Error [{e.Code}]: {e.Reason} (IsFatal={e.IsFatal})");
                })
                .SetLogHandler((_, log) =>
                {
                    if (log.Level is SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error)
                    {
                        RecordError($"Kafka Log [{log.Facility}]: {log.Message}");
                    }
                })
                .Build();

            _isAvailable = true;
            _logger.LogInformation("Kafka Producer connected successfully to broker(s): {Servers}", _options.BootstrapServers);
        }
        catch (Exception ex)
        {
            RecordError($"Failed to initialize Kafka producer for broker '{_options.BootstrapServers}': {ex.Message}");
            _isAvailable = false;
            _producer = null;
        }
    }

    public bool IsAvailable => _isAvailable && _producer != null;

    public async Task<bool> ProduceAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IKafkaEvent
    {
        if (!IsAvailable || _producer == null)
        {
            Interlocked.Increment(ref _fallbackInMemCount);
            return false;
        }

        try
        {
            var key = @event.PartitionKey;
            var payload = JsonSerializer.Serialize(@event, JsonOptions);

            var message = new Message<string, string>
            {
                Key = key,
                Value = payload,
                Timestamp = new Timestamp(@event.OccurredOn)
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            if (result.Status == PersistenceStatus.Persisted || result.Status == PersistenceStatus.PossiblyPersisted)
            {
                Interlocked.Increment(ref _messagesProduced);
                return true;
            }

            Interlocked.Increment(ref _messagesFailed);
            return false;
        }
        catch (ProduceException<string, string> pex)
        {
            Interlocked.Increment(ref _messagesFailed);
            RecordError($"ProduceException to topic '{topic}': {pex.Error.Reason}");
            return false;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _messagesFailed);
            RecordError($"General exception producing to topic '{topic}': {ex.Message}");
            return false;
        }
    }

    public void ProduceFireAndForget<TEvent>(string topic, TEvent @event)
        where TEvent : class, IKafkaEvent
    {
        if (!IsAvailable || _producer == null)
        {
            Interlocked.Increment(ref _fallbackInMemCount);
            return;
        }

        try
        {
            var key = @event.PartitionKey;
            var payload = JsonSerializer.Serialize(@event, JsonOptions);

            var message = new Message<string, string>
            {
                Key = key,
                Value = payload,
                Timestamp = new Timestamp(@event.OccurredOn)
            };

            _producer.Produce(topic, message, deliveryReport =>
            {
                if (deliveryReport.Error.IsError)
                {
                    Interlocked.Increment(ref _messagesFailed);
                    RecordError($"Async delivery failed to topic '{topic}': {deliveryReport.Error.Reason}");
                }
                else
                {
                    Interlocked.Increment(ref _messagesProduced);
                }
            });
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _messagesFailed);
            RecordError($"FireAndForget enqueue failed to topic '{topic}': {ex.Message}");
        }
    }

    public Task<KafkaTelemetrySnapshot> GetDiagnosticsAsync()
    {
        var snapshot = new KafkaTelemetrySnapshot(
            IsConfigured: _options.Enabled && !string.IsNullOrWhiteSpace(_options.BootstrapServers),
            IsHealthy: IsAvailable && (DateTime.UtcNow - _lastErrorTime).TotalSeconds > 10,
            BootstrapServers: _options.BootstrapServers,
            MessagesProduced: Interlocked.Read(ref _messagesProduced),
            MessagesFailed: Interlocked.Read(ref _messagesFailed),
            FallbackInMemCount: Interlocked.Read(ref _fallbackInMemCount),
            LastError: _lastError,
            Timestamp: DateTime.UtcNow
        );

        return Task.FromResult(snapshot);
    }

    private void RecordError(string error)
    {
        _lastError = error;
        _lastErrorTime = DateTime.UtcNow;
        _logger.LogWarning("Kafka Telemetry EventBus Alert: {Error}", error);
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(2));
            _producer?.Dispose();
        }
        catch { }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
