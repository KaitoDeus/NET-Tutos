using System.Net;
using FluentAssertions;
using NET_Tutos.Services;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class ObservabilityAndResilienceTests
{
    [Fact]
    public async Task ObservabilityService_RecordsHttpAndDownstreamMetricsCorrectly()
    {
        var obs = new ObservabilityService();
        obs.RecordHttpRequest("/Tutorials", 200, 12.5);
        obs.RecordHttpRequest("/Playground", 500, 45.0);
        obs.RecordDownstreamCall("CodeRunner", 22.0, isSuccess: true, retriesUsed: 1, circuitTripped: false, isCircuitOpen: false);

        var snapshot = await obs.GetDiagnosticsAsync();

        snapshot.TotalHttpRequests.Should().Be(2);
        snapshot.FailedHttpRequests.Should().Be(1);
        snapshot.HttpSuccessRatePercent.Should().Be(50.0);
        snapshot.ActivitySourceName.Should().Be(ObservabilityService.SourceName);

        var runnerMetric = snapshot.DownstreamMicroservices.First(m => m.ServiceName == "CodeRunner");
        runnerMetric.TotalCalls.Should().Be(1);
        runnerMetric.SuccessfulCalls.Should().Be(1);
        runnerMetric.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task ResilientMicroserviceHandler_RetriesOnTransient503AndSucceedsOnSecondAttempt()
    {
        ResilientMicroserviceHandler.ResetAllCircuits();
        var obs = new ObservabilityService();
        int callCount = 0;

        var innerHandler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var resilientHandler = new ResilientMicroserviceHandler(obs, maxRetries: 2, circuitBreakThreshold: 3)
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(resilientHandler);
        var response = await client.GetAsync("http://localhost:5008/api/v1/catalog/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(2);

        var diagnostics = await obs.GetDiagnosticsAsync();
        var catalogMetric = diagnostics.DownstreamMicroservices.First(m => m.ServiceName == "Catalog");
        catalogMetric.SuccessfulCalls.Should().Be(1);
        catalogMetric.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task ResilientMicroserviceHandler_OpensCircuitAfterConsecutiveFailures()
    {
        ResilientMicroserviceHandler.ResetAllCircuits();
        var obs = new ObservabilityService();

        var innerHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var resilientHandler = new ResilientMicroserviceHandler(
            obs,
            maxRetries: 0,
            circuitBreakThreshold: 2,
            circuitBreakDuration: TimeSpan.FromMinutes(1))
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(resilientHandler);

        var r1 = await client.GetAsync("http://localhost:5006/api/v1/aitutor/explain-error");
        var r2 = await client.GetAsync("http://localhost:5006/api/v1/aitutor/explain-error");

        r1.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        r2.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        // Third call should fast-fail because circuit breaker is now open
        Func<Task> act = async () => await client.GetAsync("http://localhost:5006/api/v1/aitutor/explain-error");
        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*Circuit breaker is open*");

        ResilientMicroserviceHandler.ResetAllCircuits();
    }

    [Fact]
    public async Task PrometheusTelemetryExporter_FormatsValidOpenMetricsWithTtsCacheAiAndKafkaLag()
    {
        var obs = new ObservabilityService();
        obs.RecordHttpRequest("/Tutorials", 200, 15.0);
        obs.RecordTtsRequest("vi", cacheHit: true, isSuccess: true, durationMs: 2.5);
        obs.RecordTtsRequest("vi", cacheHit: false, isSuccess: true, durationMs: 85.0);
        obs.RecordAiRequest("chat", isFallback: false, isSuccess: true, durationMs: 320.0);

        var store = new KafkaStreamAnalyticsStore();
        store.UpdateTopicLag("nettutos.atus.ai-telemetry", 7);

        var exporter = new PrometheusTelemetryExporter(obs, kafkaProducer: null, analyticsStore: store);
        var metricsText = await exporter.ExportMetricsAsync();

        metricsText.Should().Contain("# TYPE nettutos_web_http_requests_total counter");
        metricsText.Should().Contain("nettutos_web_http_requests_total{service=\"NET-Tutos.Web.BFF\",status=\"total\"} 1");
        metricsText.Should().Contain("nettutos_tts_cache_hit_ratio_percent 50");
        metricsText.Should().Contain("nettutos_tts_requests_total{outcome=\"cache_hit\"} 1");
        metricsText.Should().Contain("nettutos_ai_avg_latency_ms 320");
        metricsText.Should().Contain("nettutos_kafka_consumer_lag_messages{topic=\"nettutos.atus.ai-telemetry\"} 7");
        metricsText.Should().Contain("nettutos_kafka_consumer_total_lag_messages 7");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
