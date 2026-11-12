using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NET_Tutos.BuildingBlocks.Common.Telemetry;

/// <summary>
/// Describes the active OpenTelemetry OTLP collector export configuration for a microservice.
/// </summary>
public sealed record OtlpCollectorDiagnosticsDto(
    string ServiceName,
    bool OtlpEnabled,
    string? OtlpEndpoint,
    string Protocol,
    IReadOnlyList<string> RegisteredActivitySources,
    IReadOnlyList<string> RegisteredMeters);

/// <summary>
/// Registers OpenTelemetry W3C distributed tracing, metrics, and conditional OTLP collector export
/// across all NET-Tutos ASP.NET Core services and API Gateway.
/// </summary>
public static class OpenTelemetryCollectorExtensions
{
    public static OtlpCollectorDiagnosticsDto AddNetTutosOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        ServiceTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        return services.AddNetTutosOpenTelemetry(
            configuration,
            serviceName,
            [telemetry.ActivitySource.Name],
            [telemetry.Meter.Name]);
    }

    public static OtlpCollectorDiagnosticsDto AddNetTutosOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string[] activitySources,
        string[] meters)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var safeSources = (activitySources ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.Ordinal).ToArray();
        var safeMeters = (meters ?? []).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.Ordinal).ToArray();

        string? endpointRaw = configuration["OpenTelemetry:OtlpEndpoint"]
            ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        bool hasEndpoint = Uri.TryCreate(endpointRaw, UriKind.Absolute, out var otlpUri);

        string protocolRaw = configuration["OpenTelemetry:OtlpProtocol"]
            ?? configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")
            ?? "grpc";

        bool isHttpProtobuf = protocolRaw.Contains("http", StringComparison.OrdinalIgnoreCase);
        var otlpProtocol = isHttpProtobuf ? OtlpExportProtocol.HttpProtobuf : OtlpExportProtocol.Grpc;

        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                if (safeSources.Length > 0)
                {
                    tracing.AddSource(safeSources);
                }

                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/metrics")
                            && !httpContext.Request.Path.StartsWithSegments("/healthz");
                    })
                    .AddHttpClientInstrumentation();

                if (hasEndpoint && otlpUri is not null)
                {
                    tracing.AddOtlpExporter(exporter =>
                    {
                        exporter.Endpoint = otlpUri;
                        exporter.Protocol = otlpProtocol;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                if (safeMeters.Length > 0)
                {
                    metrics.AddMeter(safeMeters);
                }

                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (hasEndpoint && otlpUri is not null)
                {
                    metrics.AddOtlpExporter(exporter =>
                    {
                        exporter.Endpoint = otlpUri;
                        exporter.Protocol = otlpProtocol;
                    });
                }
            });

        _ = otelBuilder;

        var diagnostics = new OtlpCollectorDiagnosticsDto(
            ServiceName: serviceName,
            OtlpEnabled: hasEndpoint,
            OtlpEndpoint: hasEndpoint ? otlpUri!.ToString() : null,
            Protocol: isHttpProtobuf ? "http/protobuf" : "grpc",
            RegisteredActivitySources: safeSources,
            RegisteredMeters: safeMeters);

        services.AddSingleton(diagnostics);
        return diagnostics;
    }
}
