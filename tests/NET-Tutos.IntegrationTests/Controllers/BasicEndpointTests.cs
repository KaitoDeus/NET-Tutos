using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NET_Tutos.Models.ViewModels;
using Xunit;

namespace NET_Tutos.IntegrationTests.Controllers;

public class BasicEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BasicEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Streak")]
    [InlineData("/chuoi-ngay-hoc")]
    [InlineData("/Playground")]
    [InlineData("/Tutorials")]
    [InlineData("/AiTutor")]
    [InlineData("/Exam")]
    [InlineData("/Leaderboard")]
    public async Task Get_PublicEndpoints_ReturnSuccessAndCorrectContentType(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Post_StreakCheckIn_WhenUnauthenticated_IsProtectedBySecurity()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await unauthorizedClient.PostAsync("/Streak/CheckIn", null);

        // Assert: ASP.NET Core redirects unauthenticated cookie requests to Login, or rejects with 401/400
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            response.Headers.Location?.ToString().Should().Contain("Login");
        }
    }

    [Fact]
    public async Task Post_PlaygroundExecute_WithValidConsoleCode_ReturnsSuccessOutput()
    {
        // Arrange
        var request = new CodeExecutionRequest
        {
            Code = "Console.WriteLine(\"Integration Test OK\");"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/Playground/Execute", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CodeExecutionResponse>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Output.Trim().Should().Be("Integration Test OK");
    }

    [Fact]
    public async Task Post_PlaygroundExecute_WithMaliciousCode_ReturnsBlockedError()
    {
        // Arrange
        var request = new CodeExecutionRequest
        {
            Code = "System.Diagnostics.Process.Start(\"cmd.exe\");"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/Playground/Execute", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CodeExecutionResponse>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("bị cấm vì lý do an toàn bảo mật");
    }

    [Fact]
    public async Task Post_AiTutorChat_ReturnsValidMarkdownExplanation()
    {
        // Arrange
        var request = new AiTutorChatRequest
        {
            Message = "What is dependency injection in .NET?"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/AiTutor/Chat", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AiTutorChatResponse>();
        result.Should().NotBeNull();
        result!.ResponseMarkdown.Should().NotBeNullOrEmpty();
    }
}
