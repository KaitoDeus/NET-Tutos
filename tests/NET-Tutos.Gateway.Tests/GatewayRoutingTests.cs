using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NET_Tutos.Gateway.Tests;

public class GatewayRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayRoutingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Healthz_ReturnsHealthyStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
        content.Should().Contain("Yarp.API.Gateway");
    }

    [Fact]
    public async Task CorrelationIdMiddleware_GeneratesNewId_WhenNotSupplied()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        response.Headers.Should().ContainKey("X-Correlation-ID");
        var correlationId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
        correlationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_PreservesSuppliedId()
    {
        // Arrange
        var client = _factory.CreateClient();
        var customId = "trace-test-uuid-999";
        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        request.Headers.Add("X-Correlation-ID", customId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("X-Correlation-ID");
        var returnedId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
        returnedId.Should().Be(customId);
    }

    [Fact]
    public async Task GatewayStatus_ReturnsTopology_WithClustersAndRoutes()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/gateway/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("coderunner-cluster");
        content.Should().Contain("gamification-cluster");
        content.Should().Contain("aitutor-cluster");
        content.Should().Contain("catalog-cluster");
        content.Should().Contain("catalog-route");
        content.Should().Contain("web-cluster");
    }

    [Fact]
    public async Task GatewayTelemetry_ReturnsOpenTelemetryActivitySourceAndMeterMetrics()
    {
        // Arrange
        var client = _factory.CreateClient();
        await client.GetAsync("/healthz");

        // Act
        var response = await client.GetAsync("/api/v1/gateway/telemetry");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("NET-Tutos.YarpGateway.Tracing");
        content.Should().Contain("NET-Tutos.YarpGateway.Metrics");
    }

    [Fact]
    public async Task Metrics_ReturnsValidPrometheusOpenMetricsFormat()
    {
        // Arrange
        var client = _factory.CreateClient();
        await client.GetAsync("/healthz");

        // Act
        var response = await client.GetAsync("/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        var metrics = await response.Content.ReadAsStringAsync();
        metrics.Should().Contain("# TYPE nettutos_service_up gauge");
        metrics.Should().Contain("nettutos_service_up{service=\"NET-Tutos.YarpGateway\"} 1");
        metrics.Should().Contain("nettutos_service_requests_total");
    }

    [Fact]
    public void Phase22_AspireAppHostAndHelmDeploymentSpecs_AreConfiguredProperly()
    {
        // Locate repository root dynamically from test output directory
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "NET-Tutos.slnx")))
        {
            dir = dir.Parent;
        }
        dir.Should().NotBeNull("repository root containing NET-Tutos.slnx must exist");

        string root = dir!.FullName;
        string appHostCsproj = File.ReadAllText(Path.Combine(root, "src", "Orchestration", "NET-Tutos.AppHost", "NET-Tutos.AppHost.csproj"));
        string appHostProgram = File.ReadAllText(Path.Combine(root, "src", "Orchestration", "NET-Tutos.AppHost", "Program.cs"));
        string helmValues = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "values.yaml"));
        string helmWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "helm-deploy.yml"));

        appHostCsproj.Should().Contain("Aspire.AppHost.Sdk");
        appHostProgram.Should().Contain("AddPostgres(\"postgres\")");
        appHostProgram.Should().Contain("pgvector/pgvector");
        appHostProgram.Should().Contain("NET_Tutos_YarpGateway");

        helmValues.Should().Contain("pgvector/pgvector:pg17");
        helmWorkflow.Should().Contain("helm lint deploy/helm/nettutos");
        helmWorkflow.Should().Contain("helm upgrade --install nettutos");
    }

    [Fact]
    public void Phase23_HelmProductionParityAndYarpAffinity_AreConfiguredProperly()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "NET-Tutos.slnx")))
        {
            dir = dir.Parent;
        }
        dir.Should().NotBeNull("repository root containing NET-Tutos.slnx must exist");

        string root = dir!.FullName;
        string gatewaySettings = File.ReadAllText(Path.Combine(root, "src", "Gateways", "Gateway", "appsettings.json"));
        string helmValues = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "values.yaml"));
        string helmInfra = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "templates", "infrastructure.yaml"));
        string helmObs = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "templates", "observability.yaml"));
        string helmMicro = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "templates", "microservices.yaml"));
        string helmWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "helm-deploy.yml"));

        gatewaySettings.Should().Contain("\"SessionAffinity\"");
        gatewaySettings.Should().Contain(".YARP.WebAffinity");

        helmValues.Should().Contain("serviceType: ClusterIP");
        helmValues.Should().Contain("gamificationDatabase: nettutos_gamification_db");
        helmValues.Should().Contain("kafka:");
        helmValues.Should().Contain("prometheus:");
        helmValues.Should().Contain("grafana:");

        helmInfra.Should().Contain("nettutos-postgres-init");
        helmInfra.Should().Contain("nettutos-kafka");
        helmObs.Should().Contain("nettutos-prometheus");
        helmObs.Should().Contain("nettutos-grafana");
        helmMicro.Should().Contain("Kafka__Enabled");
        helmMicro.Should().Contain("serviceType");

        helmWorkflow.Should().Contain("image.tag=${{ github.sha }}");
        helmWorkflow.Should().NotContain("${GITHUB_SHA::7}");
    }

    [Fact]
    public async Task Phase24_GatewayOtlpEndpointAndTempoSpecs_AreConfigured()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/gateway/otlp");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("NET-Tutos.YarpGateway");
        body.Should().Contain("NET-Tutos.YarpGateway.Tracing");
        body.Should().Contain("NET-Tutos.YarpGateway.Metrics");

        var root = LocateRepositoryRoot();
        string tempoConfig = File.ReadAllText(Path.Combine(root, "observability", "tempo", "tempo.yml"));
        string grafanaSources = File.ReadAllText(Path.Combine(root, "observability", "grafana", "provisioning", "datasources", "prometheus.yml"));
        string compose = File.ReadAllText(Path.Combine(root, "docker-compose.yml"));
        string helmValues = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "values.yaml"));
        string helmObs = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "templates", "observability.yaml"));
        string helmMicro = File.ReadAllText(Path.Combine(root, "deploy", "helm", "nettutos", "templates", "microservices.yaml"));
        string helmWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "helm-deploy.yml"));

        tempoConfig.Should().Contain("otlp:");
        grafanaSources.Should().Contain("nettutos-tempo");
        compose.Should().Contain("grafana/tempo:");
        compose.Should().Contain("OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317");
        helmValues.Should().Contain("tempo:");
        helmObs.Should().Contain("nettutos-tempo");
        helmMicro.Should().Contain("OTEL_EXPORTER_OTLP_ENDPOINT");
        helmWorkflow.Should().Contain("nettutos-tempo");
    }

    private static string LocateRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "NET-Tutos.slnx")))
        {
            dir = dir.Parent;
        }
        dir.Should().NotBeNull("repository root containing NET-Tutos.slnx must exist");
        return dir!.FullName;
    }
}

