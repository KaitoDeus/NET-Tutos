using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace NET_Tutos.BuildingBlocks.Common.Telemetry;

public sealed record ServiceOperationMetricDto(
    string Operation,
    long TotalCalls,
    long FailedCalls,
    double AverageLatencyMs);

public sealed record ServiceTelemetrySnapshotDto(
    string ServiceName,
    string ActivitySourceName,
    string MeterName,
    long UptimeSeconds,
    long TotalRequests,
    long FailedRequests,
    double SuccessRatePercent,
    double AverageLatencyMs,
    IReadOnlyList<ServiceOperationMetricDto> Operations);

/// <summary>
/// Shared W3C OpenTelemetry ActivitySource and Meter instrumentation for NET-Tutos microservices and API Gateway.
/// </summary>
public sealed class ServiceTelemetry
{
    private readonly string _serviceName;
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _latencyHistogram;
    private readonly ConcurrentDictionary<string, OperationTracker> _operations = new(StringComparer.OrdinalIgnoreCase);

    private long _totalRequests;
    private long _failedRequests;
    private long _totalLatencyMicros;

    public ActivitySource ActivitySource { get; }
    public Meter Meter { get; }

    public ServiceTelemetry(string serviceName)
    {
        _serviceName = string.IsNullOrWhiteSpace(serviceName) ? "NET-Tutos.Service" : serviceName.Trim();
        ActivitySource = new ActivitySource($"{_serviceName}.Tracing", "1.0.0");
        Meter = new Meter($"{_serviceName}.Metrics", "1.0.0");

        _requestCounter = Meter.CreateCounter<long>("nettutos.requests.total", description: "Total HTTP/RPC requests processed");
        _latencyHistogram = Meter.CreateHistogram<double>("nettutos.requests.duration.ms", unit: "ms", description: "Request execution duration in milliseconds");
    }

    public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(operationName, kind);
    }

    public void RecordOperation(string operation, double durationMs, bool isSuccess)
    {
        Interlocked.Increment(ref _totalRequests);
        if (!isSuccess)
        {
            Interlocked.Increment(ref _failedRequests);
        }

        long micros = (long)Math.Max(0, durationMs * 1000.0);
        Interlocked.Add(ref _totalLatencyMicros, micros);

        _requestCounter.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("success", isSuccess));
        _latencyHistogram.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));

        var tracker = _operations.GetOrAdd(operation, _ => new OperationTracker());
        tracker.Record(micros, isSuccess);
    }

    public ServiceTelemetrySnapshotDto GetSnapshot()
    {
        long total = Interlocked.Read(ref _totalRequests);
        long failed = Interlocked.Read(ref _failedRequests);
        long totalMicros = Interlocked.Read(ref _totalLatencyMicros);

        double successRate = total > 0 ? Math.Round((double)(total - failed) / total * 100.0, 2) : 100.0;
        double avgLatencyMs = total > 0 ? Math.Round(totalMicros / 1000.0 / total, 2) : 0.0;

        var ops = _operations
            .Select(kvp => kvp.Value.ToDto(kvp.Key))
            .OrderByDescending(o => o.TotalCalls)
            .ToList();

        return new ServiceTelemetrySnapshotDto(
            ServiceName: _serviceName,
            ActivitySourceName: ActivitySource.Name,
            MeterName: Meter.Name,
            UptimeSeconds: (long)Math.Max(0, (DateTime.UtcNow - _startedAtUtc).TotalSeconds),
            TotalRequests: total,
            FailedRequests: failed,
            SuccessRatePercent: successRate,
            AverageLatencyMs: avgLatencyMs,
            Operations: ops);
    }

    private sealed class OperationTracker
    {
        private long _calls;
        private long _failures;
        private long _latencyMicros;

        public void Record(long micros, bool isSuccess)
        {
            Interlocked.Increment(ref _calls);
            if (!isSuccess)
            {
                Interlocked.Increment(ref _failures);
            }
            Interlocked.Add(ref _latencyMicros, micros);
        }

        public ServiceOperationMetricDto ToDto(string operation)
        {
            long calls = Interlocked.Read(ref _calls);
            long failures = Interlocked.Read(ref _failures);
            long micros = Interlocked.Read(ref _latencyMicros);
            double avgMs = calls > 0 ? Math.Round(micros / 1000.0 / calls, 2) : 0.0;
            return new ServiceOperationMetricDto(operation, calls, failures, avgMs);
        }
    }
}
