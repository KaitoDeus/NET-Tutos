using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;

namespace NET_Tutos.Services;

public sealed class MicroserviceHealthMetric
{
    public string ServiceName { get; set; } = string.Empty;
    public long TotalCalls { get; set; }
    public long SuccessfulCalls { get; set; }
    public long FailedCalls { get; set; }
    public long RetryCount { get; set; }
    public long CircuitBreakerOpenEvents { get; set; }
    public bool IsCircuitOpen { get; set; }
    public double AverageLatencyMs { get; set; }
}

public sealed class ObservabilityDiagnosticsSnapshot
{
    public string ServiceName { get; set; } = "NET-Tutos.Web.BFF";
    public string ActivitySourceName { get; set; } = string.Empty;
    public string MeterName { get; set; } = string.Empty;
    public long UptimeSeconds { get; set; }
    public long TotalHttpRequests { get; set; }
    public long FailedHttpRequests { get; set; }
    public double HttpSuccessRatePercent { get; set; }
    public double AverageHttpLatencyMs { get; set; }
    public List<MicroserviceHealthMetric> DownstreamMicroservices { get; set; } = [];
    public CacheTelemetrySnapshot? CacheDiagnostics { get; set; }
}

public interface IObservabilityService
{
    Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal);
    void RecordHttpRequest(string path, int statusCode, double durationMs);
    void RecordDownstreamCall(string serviceName, double durationMs, bool isSuccess, int retriesUsed, bool circuitTripped, bool isCircuitOpen);
    Task<ObservabilityDiagnosticsSnapshot> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cloud-Native Observability Service providing OpenTelemetry-compatible W3C ActivitySource tracing,
/// System.Diagnostics.Metrics counters/histograms, and downstream microservice resilience telemetry.
/// </summary>
public sealed class ObservabilityService : IObservabilityService
{
    public const string SourceName = "NET-Tutos.WebBFF.Tracing";
    public const string MeterNamespace = "NET-Tutos.WebBFF.Metrics";

    private readonly IAppCacheService? _cacheService;
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly ActivitySource _activitySource = new(SourceName, "1.0.0");
    private readonly Meter _meter = new(MeterNamespace, "1.0.0");
    private readonly Counter<long> _httpRequestsCounter;
    private readonly Histogram<double> _httpLatencyHistogram;
    private readonly ConcurrentDictionary<string, DownstreamTracker> _downstreamTrackers = new(StringComparer.OrdinalIgnoreCase);

    private long _totalRequests;
    private long _failedRequests;
    private long _totalLatencyMicros;

    public ObservabilityService(IAppCacheService? cacheService = null)
    {
        _cacheService = cacheService;
        _httpRequestsCounter = _meter.CreateCounter<long>("nettutos.web.http.requests", description: "Total HTTP requests processed by Web BFF");
        _httpLatencyHistogram = _meter.CreateHistogram<double>("nettutos.web.http.duration.ms", unit: "ms", description: "HTTP request duration in milliseconds");

        // Pre-seed known downstream microservices so they always appear in observability diagnostics
        foreach (var svc in new[] { "CodeRunner", "Gamification", "AiTutor", "Catalog" })
        {
            _downstreamTrackers.TryAdd(svc, new DownstreamTracker());
        }
    }

    public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource.StartActivity(operationName, kind);
    }

    public void RecordHttpRequest(string path, int statusCode, double durationMs)
    {
        bool isSuccess = statusCode < 500;
        Interlocked.Increment(ref _totalRequests);
        if (!isSuccess)
        {
            Interlocked.Increment(ref _failedRequests);
        }

        long micros = (long)Math.Max(0, durationMs * 1000.0);
        Interlocked.Add(ref _totalLatencyMicros, micros);

        _httpRequestsCounter.Add(1, new KeyValuePair<string, object?>("status_code", statusCode));
        _httpLatencyHistogram.Record(durationMs);
    }

    public void RecordDownstreamCall(
        string serviceName,
        double durationMs,
        bool isSuccess,
        int retriesUsed,
        bool circuitTripped,
        bool isCircuitOpen)
    {
        var tracker = _downstreamTrackers.GetOrAdd(serviceName, _ => new DownstreamTracker());
        tracker.Record(durationMs, isSuccess, retriesUsed, circuitTripped, isCircuitOpen);
    }

    public async Task<ObservabilityDiagnosticsSnapshot> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        long total = Interlocked.Read(ref _totalRequests);
        long failed = Interlocked.Read(ref _failedRequests);
        long micros = Interlocked.Read(ref _totalLatencyMicros);

        double successRate = total > 0 ? Math.Round((double)(total - failed) / total * 100.0, 2) : 100.0;
        double avgLatencyMs = total > 0 ? Math.Round(micros / 1000.0 / total, 2) : 0.0;

        var downstream = _downstreamTrackers
            .OrderBy(k => k.Key)
            .Select(k => k.Value.ToMetric(k.Key))
            .ToList();

        var cacheStats = _cacheService != null
            ? await _cacheService.GetDiagnosticsAsync(cancellationToken)
            : null;

        return new ObservabilityDiagnosticsSnapshot
        {
            ServiceName = "NET-Tutos.Web.BFF",
            ActivitySourceName = _activitySource.Name,
            MeterName = _meter.Name,
            UptimeSeconds = (long)Math.Max(0, (DateTime.UtcNow - _startedAtUtc).TotalSeconds),
            TotalHttpRequests = total,
            FailedHttpRequests = failed,
            HttpSuccessRatePercent = successRate,
            AverageHttpLatencyMs = avgLatencyMs,
            DownstreamMicroservices = downstream,
            CacheDiagnostics = cacheStats
        };
    }

    private sealed class DownstreamTracker
    {
        private long _totalCalls;
        private long _successfulCalls;
        private long _failedCalls;
        private long _retries;
        private long _circuitOpenEvents;
        private long _totalLatencyMicros;
        private volatile bool _isCircuitOpen;

        public void Record(double durationMs, bool isSuccess, int retriesUsed, bool circuitTripped, bool isCircuitOpen)
        {
            Interlocked.Increment(ref _totalCalls);
            if (isSuccess)
            {
                Interlocked.Increment(ref _successfulCalls);
            }
            else
            {
                Interlocked.Increment(ref _failedCalls);
            }

            if (retriesUsed > 0)
            {
                Interlocked.Add(ref _retries, retriesUsed);
            }

            if (circuitTripped)
            {
                Interlocked.Increment(ref _circuitOpenEvents);
            }

            _isCircuitOpen = isCircuitOpen;
            Interlocked.Add(ref _totalLatencyMicros, (long)Math.Max(0, durationMs * 1000.0));
        }

        public MicroserviceHealthMetric ToMetric(string serviceName)
        {
            long total = Interlocked.Read(ref _totalCalls);
            long micros = Interlocked.Read(ref _totalLatencyMicros);
            return new MicroserviceHealthMetric
            {
                ServiceName = serviceName,
                TotalCalls = total,
                SuccessfulCalls = Interlocked.Read(ref _successfulCalls),
                FailedCalls = Interlocked.Read(ref _failedCalls),
                RetryCount = Interlocked.Read(ref _retries),
                CircuitBreakerOpenEvents = Interlocked.Read(ref _circuitOpenEvents),
                IsCircuitOpen = _isCircuitOpen,
                AverageLatencyMs = total > 0 ? Math.Round(micros / 1000.0 / total, 2) : 0.0
            };
        }
    }
}

/// <summary>
/// HTTP Resilience DelegatingHandler implementing Exponential Backoff Retry, Circuit Breaker (3 consecutive failures -> 20s open state),
/// W3C traceparent / X-Correlation-ID header propagation, and automatic ObservabilityService telemetry recording.
/// </summary>
public sealed class ResilientMicroserviceHandler : DelegatingHandler
{
    private static readonly ConcurrentDictionary<string, CircuitState> Circuits = new(StringComparer.OrdinalIgnoreCase);

    private readonly IObservabilityService _observability;
    private readonly int _maxRetries;
    private readonly int _circuitBreakThreshold;
    private readonly TimeSpan _circuitBreakDuration;

    public ResilientMicroserviceHandler(
        IObservabilityService observability,
        int maxRetries = 2,
        int circuitBreakThreshold = 3,
        TimeSpan? circuitBreakDuration = null)
    {
        _observability = observability;
        _maxRetries = Math.Max(0, maxRetries);
        _circuitBreakThreshold = Math.Max(1, circuitBreakThreshold);
        _circuitBreakDuration = circuitBreakDuration ?? TimeSpan.FromSeconds(20);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string serviceName = ResolveServiceName(request.RequestUri);
        var circuit = Circuits.GetOrAdd(serviceName, _ => new CircuitState());

        if (circuit.IsOpen())
        {
            _observability.RecordDownstreamCall(serviceName, 0, isSuccess: false, retriesUsed: 0, circuitTripped: false, isCircuitOpen: true);
            throw new HttpRequestException($"Circuit breaker is open for downstream service '{serviceName}'. Fast-failing to local fallback.");
        }

        using var activity = _observability.StartActivity($"HTTP {request.Method} {serviceName}", ActivityKind.Client);
        if (!request.Headers.Contains("X-Correlation-ID"))
        {
            string correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
        }

        var stopwatch = Stopwatch.StartNew();
        int attempt = 0;
        Exception? lastException = null;

        while (true)
        {
            try
            {
                using var AttemptRequest = await CloneHttpRequestMessageAsync(request);
                var response = await base.SendAsync(AttemptRequest, cancellationToken);

                if ((int)response.StatusCode >= 500 && attempt < _maxRetries)
                {
                    response.Dispose();
                    attempt++;
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt)), cancellationToken);
                    continue;
                }

                stopwatch.Stop();
                bool isSuccess = (int)response.StatusCode < 500;
                bool circuitTripped = false;

                if (isSuccess)
                {
                    circuit.RecordSuccess();
                }
                else
                {
                    circuitTripped = circuit.RecordFailure(_circuitBreakThreshold, _circuitBreakDuration);
                }

                _observability.RecordDownstreamCall(
                    serviceName,
                    stopwatch.Elapsed.TotalMilliseconds,
                    isSuccess,
                    attempt,
                    circuitTripped,
                    circuit.IsOpen());

                return response;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                if (attempt < _maxRetries)
                {
                    attempt++;
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt)), cancellationToken);
                    continue;
                }

                stopwatch.Stop();
                bool circuitTripped = circuit.RecordFailure(_circuitBreakThreshold, _circuitBreakDuration);
                _observability.RecordDownstreamCall(
                    serviceName,
                    stopwatch.Elapsed.TotalMilliseconds,
                    isSuccess: false,
                    retriesUsed: attempt,
                    circuitTripped: circuitTripped,
                    isCircuitOpen: circuit.IsOpen());
                throw;
            }
        }
    }

    public static void ResetAllCircuits() => Circuits.Clear();

    private static string ResolveServiceName(Uri? uri)
    {
        if (uri == null) return "Unknown";
        var path = uri.AbsolutePath.ToLowerInvariant();
        if (path.Contains("/runner/")) return "CodeRunner";
        if (path.Contains("/gamification/")) return "Gamification";
        if (path.Contains("/aitutor/")) return "AiTutor";
        if (path.Contains("/catalog/")) return "Catalog";
        return uri.Host;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            var contentClone = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            clone.Content = contentClone;
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private sealed class CircuitState
    {
        private int _consecutiveFailures;
        private DateTime _openUntilUtc = DateTime.MinValue;

        public bool IsOpen() => DateTime.UtcNow < _openUntilUtc;

        public void RecordSuccess()
        {
            Interlocked.Exchange(ref _consecutiveFailures, 0);
            _openUntilUtc = DateTime.MinValue;
        }

        public bool RecordFailure(int threshold, TimeSpan duration)
        {
            int failures = Interlocked.Increment(ref _consecutiveFailures);
            if (failures >= threshold)
            {
                _openUntilUtc = DateTime.UtcNow.Add(duration);
                return true;
            }
            return false;
        }
    }
}
