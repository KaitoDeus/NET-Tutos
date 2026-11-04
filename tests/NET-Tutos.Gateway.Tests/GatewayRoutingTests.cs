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
}
