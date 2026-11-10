using NET_Tutos.BuildingBlocks.Common.Telemetry;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;

namespace NET_Tutos.Services;

public interface IPrometheusTelemetryExporter
{
    Task<string> ExportMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Exports unified Prometheus (v0.0.4) observability metrics across ASP.NET Core HTTP traffic,
/// downstream microservices resilience, Redis L1/L2 cache, Neural TTS cache hit-rate,
/// Atus AI latency/fallback rates, and Apache Kafka stream throughput + consumer lag.
/// </summary>
public sealed class PrometheusTelemetryExporter : IPrometheusTelemetryExporter
{
    private const string ServiceLabel = "NET-Tutos.Web.BFF";

    private readonly IObservabilityService _observability;
    private readonly IKafkaProducer? _kafkaProducer;
    private readonly IKafkaStreamAnalyticsStore? _analyticsStore;

    public PrometheusTelemetryExporter(
        IObservabilityService observability,
        IKafkaProducer? kafkaProducer = null,
        IKafkaStreamAnalyticsStore? analyticsStore = null)
    {
        _observability = observability;
        _kafkaProducer = kafkaProducer;
        _analyticsStore = analyticsStore;
    }

    public async Task<string> ExportMetricsAsync(CancellationToken cancellationToken = default)
    {
        var obs = await _observability.GetDiagnosticsAsync(cancellationToken);
        var kafkaProd = _kafkaProducer != null ? await _kafkaProducer.GetDiagnosticsAsync() : null;
        var kafkaCons = _analyticsStore?.GetSnapshot();
        var topicLags = _analyticsStore?.GetTopicLags();

        var builder = new PrometheusMetricsBuilder()
            .AppendStandardRuntimeMetrics(ServiceLabel, obs.UptimeSeconds)
            .AddCounter("nettutos_web_http_requests_total", "Total HTTP requests processed by Web BFF.", obs.TotalHttpRequests, ("service", ServiceLabel), ("status", "total"))
            .AddCounter("nettutos_web_http_requests_total", "Total HTTP requests processed by Web BFF.", obs.FailedHttpRequests, ("service", ServiceLabel), ("status", "failed"))
            .AddGauge("nettutos_web_http_success_rate_percent", "Percentage of non-5xx Web BFF HTTP requests.", obs.HttpSuccessRatePercent, ("service", ServiceLabel))
            .AddGauge("nettutos_web_http_avg_latency_ms", "Average Web BFF HTTP request latency in milliseconds.", obs.AverageHttpLatencyMs, ("service", ServiceLabel));

        foreach (var downstream in obs.DownstreamMicroservices)
        {
            builder
                .AddCounter("nettutos_downstream_calls_total", "Total HTTP calls to downstream microservices.", downstream.SuccessfulCalls, ("target", downstream.ServiceName), ("outcome", "success"))
                .AddCounter("nettutos_downstream_calls_total", "Total HTTP calls to downstream microservices.", downstream.FailedCalls, ("target", downstream.ServiceName), ("outcome", "failed"))
                .AddCounter("nettutos_downstream_retries_total", "Total Polly/Resilient handler retry attempts.", downstream.RetryCount, ("target", downstream.ServiceName))
                .AddCounter("nettutos_downstream_circuit_open_events_total", "Total circuit breaker trip events.", downstream.CircuitBreakerOpenEvents, ("target", downstream.ServiceName))
                .AddGauge("nettutos_downstream_circuit_open", "Whether the downstream circuit breaker is currently open (1 = open).", downstream.IsCircuitOpen ? 1 : 0, ("target", downstream.ServiceName))
                .AddGauge("nettutos_downstream_avg_latency_ms", "Average latency to downstream microservice in milliseconds.", downstream.AverageLatencyMs, ("target", downstream.ServiceName));
        }

        if (obs.CacheDiagnostics != null)
        {
            var cache = obs.CacheDiagnostics;
            builder
                .AddGauge("nettutos_cache_redis_healthy", "Whether distributed L2 Redis cache is reachable (1 = healthy).", cache.IsRedisHealthy ? 1 : 0)
                .AddCounter("nettutos_cache_hits_total", "Total hybrid cache hits by tier.", cache.L1MemoryHits, ("tier", "l1_memory"))
                .AddCounter("nettutos_cache_hits_total", "Total hybrid cache hits by tier.", cache.L2RedisHits, ("tier", "l2_redis"))
                .AddCounter("nettutos_cache_misses_total", "Total hybrid cache misses.", cache.CacheMisses)
                .AddCounter("nettutos_cache_redis_fallbacks_total", "Total Redis fallback circuit breaker events.", cache.RedisFallbackCount)
                .AddGauge("nettutos_cache_hit_ratio_percent", "Combined L1/L2 hybrid cache hit ratio percentage.", cache.HitRatioPercent);
        }

        if (obs.TtsDiagnostics != null)
        {
            var tts = obs.TtsDiagnostics;
            long synthesizedOk = Math.Max(0, tts.TotalRequests - tts.CacheHits - tts.FailedRequests);
            builder
                .AddCounter("nettutos_tts_requests_total", "Total Neural TTS requests by outcome.", tts.CacheHits, ("outcome", "cache_hit"))
                .AddCounter("nettutos_tts_requests_total", "Total Neural TTS requests by outcome.", synthesizedOk, ("outcome", "synthesized"))
                .AddCounter("nettutos_tts_requests_total", "Total Neural TTS requests by outcome.", tts.FailedRequests, ("outcome", "failed"))
                .AddGauge("nettutos_tts_cache_hit_ratio_percent", "Neural TTS L1/L2 cache hit ratio percentage.", tts.CacheHitRatioPercent)
                .AddGauge("nettutos_tts_avg_latency_ms", "Average Neural TTS latency in milliseconds.", tts.AverageLatencyMs);
        }

        if (obs.AiDiagnostics != null)
        {
            var ai = obs.AiDiagnostics;
            long primarySuccess = Math.Max(0, ai.TotalRequests - ai.FallbackRequests - ai.FailedRequests);
            builder
                .AddCounter("nettutos_ai_requests_total", "Total Atus AI requests by outcome.", primarySuccess, ("outcome", "success"))
                .AddCounter("nettutos_ai_requests_total", "Total Atus AI requests by outcome.", ai.FallbackRequests, ("outcome", "fallback"))
                .AddCounter("nettutos_ai_requests_total", "Total Atus AI requests by outcome.", ai.FailedRequests, ("outcome", "failed"))
                .AddGauge("nettutos_ai_avg_latency_ms", "Average Atus AI latency in milliseconds.", ai.AverageLatencyMs);
        }

        if (kafkaProd != null)
        {
            builder
                .AddGauge("nettutos_kafka_producer_healthy", "Whether Apache Kafka producer is connected and healthy (1 = healthy).", kafkaProd.IsHealthy ? 1 : 0)
                .AddCounter("nettutos_kafka_messages_produced_total", "Total messages produced to Kafka topics.", kafkaProd.MessagesProduced)
                .AddCounter("nettutos_kafka_messages_failed_total", "Total Kafka publish failures.", kafkaProd.MessagesFailed)
                .AddCounter("nettutos_kafka_producer_fallback_total", "Total events handled via in-memory fallback when Kafka unavailable.", kafkaProd.FallbackInMemCount);
        }

        if (kafkaCons != null)
        {
            builder
                .AddCounter("nettutos_kafka_consumer_events_total", "Total Kafka events consumed by stream.", kafkaCons.AiTelemetryEventsConsumed, ("stream", "ai_telemetry"))
                .AddCounter("nettutos_kafka_consumer_events_total", "Total Kafka events consumed by stream.", kafkaCons.CodeExecutionEventsConsumed, ("stream", "code_execution"))
                .AddCounter("nettutos_kafka_consumer_events_total", "Total Kafka events consumed by stream.", kafkaCons.LearningActivityEventsConsumed, ("stream", "learning_activity"))
                .AddCounter("nettutos_kafka_consumer_ai_fallback_total", "Total AI fallback events observed in Kafka consumer stream.", kafkaCons.FallbackAiCount)
                .AddGauge("nettutos_kafka_consumer_avg_latency_ms", "Average latency reported in consumed Kafka events.", kafkaCons.AverageAiLatencyMs, ("stream", "ai_telemetry"))
                .AddGauge("nettutos_kafka_consumer_avg_latency_ms", "Average latency reported in consumed Kafka events.", kafkaCons.AverageCodeExecutionMs, ("stream", "code_execution"));
        }

        long totalLag = 0;
        if (topicLags != null)
        {
            foreach (var (topic, lag) in topicLags.OrderBy(k => k.Key))
            {
                totalLag += lag;
                builder.AddGauge("nettutos_kafka_consumer_lag_messages", "Estimated Kafka consumer partition lag in messages per topic.", lag, ("topic", topic));
            }
        }
        builder.AddGauge("nettutos_kafka_consumer_total_lag_messages", "Total Kafka consumer lag across subscribed topics.", totalLag);

        return builder.Build();
    }
}
