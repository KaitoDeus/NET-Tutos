using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using NET_Tutos.BuildingBlocks.EventBus.Events;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;

namespace NET_Tutos.Services;

/// <summary>
/// Long-running BackgroundService consuming high-throughput event streams from Apache Kafka topics
/// and feeding live metrics into IKafkaStreamAnalyticsStore for diagnostics and Adaptive Learning.
/// </summary>
public sealed class KafkaTelemetryConsumerWorker : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IKafkaStreamAnalyticsStore _analyticsStore;
    private readonly ILogger<KafkaTelemetryConsumerWorker> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public KafkaTelemetryConsumerWorker(
        IOptions<KafkaOptions> options,
        IKafkaStreamAnalyticsStore analyticsStore,
        ILogger<KafkaTelemetryConsumerWorker> logger)
    {
        _options = options.Value ?? new KafkaOptions();
        _analyticsStore = analyticsStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            _logger.LogInformation("Kafka Consumer Worker skipped: Kafka is disabled or unconfigured.");
            return;
        }

        // Yield immediately so ASP.NET Core web host startup completes cleanly
        await Task.Yield();

        // Give Kafka broker a short window if starting simultaneously in Docker Compose
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        await Task.Run(() => RunConsumerLoop(stoppingToken), stoppingToken);
    }

    private void RunConsumerLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "nettutos-web-analytics-consumer",
            ClientId = $"{_options.ClientId}-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AutoCommitIntervalMs = 5000,
            AllowAutoCreateTopics = true,
            SocketTimeoutMs = _options.TimeoutMs
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    if (e.IsFatal)
                    {
                        _logger.LogWarning("Kafka Consumer fatal error [{Code}]: {Reason}", e.Code, e.Reason);
                    }
                })
                .Build();

            consumer.Subscribe(
            [
                KafkaTopics.AtusAiTelemetry,
                KafkaTopics.CodeExecutionTelemetry,
                KafkaTopics.UserLearningActivity
            ]);

            _logger.LogInformation("Kafka Telemetry Consumer subscribed to topics on {Servers}", _options.BootstrapServers);

            DateTime lastWarn = DateTime.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result == null || result.IsPartitionEOF || string.IsNullOrWhiteSpace(result.Message?.Value))
                    {
                        continue;
                    }

                    ProcessMessage(result.Topic, result.Message.Value);
                }
                catch (ConsumeException cex)
                {
                    if (cex.Error.Code == ErrorCode.UnknownTopicOrPart)
                    {
                        // Topic doesn't exist until the first producer message creates it
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        if ((DateTime.UtcNow - lastWarn).TotalSeconds > 15)
                        {
                            lastWarn = DateTime.UtcNow;
                            _logger.LogWarning("Kafka Consumer temporary error [{Code}]: {Reason}", cex.Error.Code, cex.Error.Reason);
                        }
                        Thread.Sleep(2000);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if ((DateTime.UtcNow - lastWarn).TotalSeconds > 15)
                    {
                        lastWarn = DateTime.UtcNow;
                        _logger.LogWarning(ex, "Kafka Consumer encountered unexpected error while processing event.");
                    }
                    Thread.Sleep(2000);
                }
            }

            try
            {
                consumer.Close();
            }
            catch { }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start Kafka Consumer on {Servers}. App continues safely without stream consumption.", _options.BootstrapServers);
        }
    }

    private void ProcessMessage(string topic, string payload)
    {
        switch (topic)
        {
            case KafkaTopics.AtusAiTelemetry:
                var aiEvent = JsonSerializer.Deserialize<AtusAiTelemetryEvent>(payload, JsonOptions);
                if (aiEvent != null) _analyticsStore.RecordAiTelemetry(aiEvent);
                break;

            case KafkaTopics.CodeExecutionTelemetry:
                var codeEvent = JsonSerializer.Deserialize<CodeExecutionTelemetryEvent>(payload, JsonOptions);
                if (codeEvent != null) _analyticsStore.RecordCodeExecution(codeEvent);
                break;

            case KafkaTopics.UserLearningActivity:
                var activityEvent = JsonSerializer.Deserialize<UserLearningActivityEvent>(payload, JsonOptions);
                if (activityEvent != null) _analyticsStore.RecordLearningActivity(activityEvent);
                break;
        }
    }
}
