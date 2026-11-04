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
