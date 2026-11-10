using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace NET_Tutos.BuildingBlocks.Common.Telemetry;

/// <summary>
/// Zero-dependency Prometheus Exposition Format (v0.0.4) serializer.
/// Automatically groups samples by metric family so HELP/TYPE headers remain strictly spec-compliant.
/// </summary>
public sealed class PrometheusMetricsBuilder
{
    public const string ContentType = "text/plain; version=0.0.4; charset=utf-8";

    private readonly Dictionary<string, MetricFamily> _families = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];

    public PrometheusMetricsBuilder AddCounter(
        string name,
        string help,
        double value,
        params (string Key, string Value)[] labels)
    {
        return AddSample(name, "counter", help, value, labels);
    }

    public PrometheusMetricsBuilder AddGauge(
        string name,
        string help,
        double value,
        params (string Key, string Value)[] labels)
    {
        return AddSample(name, "gauge", help, value, labels);
    }

    public PrometheusMetricsBuilder AppendStandardRuntimeMetrics(string serviceName, long uptimeSeconds)
    {
        string svc = string.IsNullOrWhiteSpace(serviceName) ? "unknown" : serviceName.Trim();
        using var proc = Process.GetCurrentProcess();

        AddGauge("nettutos_service_up", "Whether the NET-Tutos service instance is up (1 = up).", 1, ("service", svc));
        AddGauge("nettutos_service_uptime_seconds", "Service uptime in seconds.", Math.Max(0, uptimeSeconds), ("service", svc));
        AddCounter("process_cpu_seconds_total", "Total user and kernel CPU time spent in seconds.", proc.TotalProcessorTime.TotalSeconds, ("service", svc));
        AddGauge("process_resident_memory_bytes", "Resident set size (working set) memory in bytes.", proc.WorkingSet64, ("service", svc));
        AddGauge("dotnet_gc_heap_size_bytes", "Total managed bytes currently thought to be allocated by CLR GC.", GC.GetTotalMemory(false), ("service", svc));
        AddCounter("dotnet_gc_collections_total", "Number of garbage collections by generation.", GC.CollectionCount(0), ("service", svc), ("generation", "0"));
        AddCounter("dotnet_gc_collections_total", "Number of garbage collections by generation.", GC.CollectionCount(1), ("service", svc), ("generation", "1"));
        AddCounter("dotnet_gc_collections_total", "Number of garbage collections by generation.", GC.CollectionCount(2), ("service", svc), ("generation", "2"));
        AddGauge("dotnet_threadpool_threads_count", "Number of thread pool threads currently active.", ThreadPool.ThreadCount, ("service", svc));

        return this;
    }

    public string Build()
    {
        var sb = new StringBuilder(2048);

        foreach (var name in _order)
        {
            var family = _families[name];
            sb.Append("# HELP ").Append(family.Name).Append(' ').Append(EscapeHelp(family.Help)).Append('\n');
            sb.Append("# TYPE ").Append(family.Name).Append(' ').Append(family.Type).Append('\n');

            foreach (var sample in family.Samples)
            {
                sb.Append(family.Name);
                if (sample.Labels.Length > 0)
                {
                    sb.Append('{');
                    for (int i = 0; i < sample.Labels.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(SanitizeLabelName(sample.Labels[i].Key))
                          .Append("=\"")
                          .Append(EscapeLabelValue(sample.Labels[i].Value))
                          .Append('"');
                    }
                    sb.Append('}');
                }

                sb.Append(' ')
                  .Append(sample.Value.ToString("G", CultureInfo.InvariantCulture))
                  .Append('\n');
            }
        }

        return sb.ToString();
    }

    private PrometheusMetricsBuilder AddSample(
        string name,
        string type,
        string help,
        double value,
        (string Key, string Value)[] labels)
    {
        string metricName = SanitizeMetricName(name);
        if (!_families.TryGetValue(metricName, out var family))
        {
            family = new MetricFamily(metricName, type, help);
            _families[metricName] = family;
            _order.Add(metricName);
        }

        double safeValue = double.IsNaN(value) || double.IsInfinity(value) ? 0.0 : Math.Round(value, 4);
        family.Samples.Add(new MetricSample(labels, safeValue));
        return this;
    }

    private static string SanitizeMetricName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "nettutos_unknown_metric";
        var sb = new StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == ':' || (i > 0 && c >= '0' && c <= '9'))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }

    private static string SanitizeLabelName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "label";
        var sb = new StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (i > 0 && c >= '0' && c <= '9'))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }

    private static string EscapeHelp(string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static string EscapeLabelValue(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private sealed record MetricSample((string Key, string Value)[] Labels, double Value);

    private sealed class MetricFamily(string name, string type, string help)
    {
        public string Name { get; } = name;
        public string Type { get; } = type;
        public string Help { get; } = help;
        public List<MetricSample> Samples { get; } = [];
    }
}
