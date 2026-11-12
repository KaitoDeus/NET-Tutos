using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NET_Tutos.BuildingBlocks.Common.Telemetry;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class OpenTelemetryCollectorTests
{
    [Fact]
    public void AddNetTutosOpenTelemetry_WithoutEndpoint_DisablesOtlpExporterSafely()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:OtlpEndpoint"] = ""
            })
            .Build();

        var telemetry = new ServiceTelemetry("NET-Tutos.TestService");
        var diagnostics = services.AddNetTutosOpenTelemetry(config, "NET-Tutos.TestService", telemetry);

        diagnostics.ServiceName.Should().Be("NET-Tutos.TestService");
        diagnostics.OtlpEnabled.Should().BeFalse();
        diagnostics.OtlpEndpoint.Should().BeNull();
        diagnostics.Protocol.Should().Be("grpc");
        diagnostics.RegisteredActivitySources.Should().Contain("NET-Tutos.TestService.Tracing");
        diagnostics.RegisteredMeters.Should().Contain("NET-Tutos.TestService.Metrics");
    }

    [Fact]
    public void AddNetTutosOpenTelemetry_WithValidGrpcEndpoint_EnablesOtlpExporter()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:OtlpEndpoint"] = "http://tempo:4317",
                ["OpenTelemetry:OtlpProtocol"] = "grpc"
            })
            .Build();

        var diagnostics = services.AddNetTutosOpenTelemetry(
            config,
            "NET-Tutos.Gateway",
            ["NET-Tutos.Gateway.Tracing", "NET-Tutos.Gateway.Tracing"],
            ["NET-Tutos.Gateway.Metrics"]);

        diagnostics.OtlpEnabled.Should().BeTrue();
        diagnostics.OtlpEndpoint.Should().Be("http://tempo:4317/");
        diagnostics.Protocol.Should().Be("grpc");
        diagnostics.RegisteredActivitySources.Should().ContainSingle().Which.Should().Be("NET-Tutos.Gateway.Tracing");
        diagnostics.RegisteredMeters.Should().ContainSingle().Which.Should().Be("NET-Tutos.Gateway.Metrics");

        using var provider = services.BuildServiceProvider();
        provider.GetService<OtlpCollectorDiagnosticsDto>().Should().Be(diagnostics);
    }

    [Fact]
    public void AddNetTutosOpenTelemetry_WithHttpProtobufProtocolAndInvalidEndpoint_HandlesGracefully()
    {
        var services = new ServiceCollection();
        var httpConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf"
            })
            .Build();

        var httpDiag = services.AddNetTutosOpenTelemetry(
            httpConfig,
            "NET-Tutos.Web.BFF",
            ["NET-Tutos.Web"],
            ["NET-Tutos.Web.Metrics"]);

        httpDiag.OtlpEnabled.Should().BeTrue();
        httpDiag.Protocol.Should().Be("http/protobuf");
        httpDiag.OtlpEndpoint.Should().Be("http://localhost:4318/");

        var invalidServices = new ServiceCollection();
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:OtlpEndpoint"] = "not-a-valid-uri"
            })
            .Build();

        var invalidDiag = invalidServices.AddNetTutosOpenTelemetry(
            invalidConfig,
            "NET-Tutos.InvalidTest",
            [],
            []);

        invalidDiag.OtlpEnabled.Should().BeFalse();
        invalidDiag.OtlpEndpoint.Should().BeNull();
    }
}
