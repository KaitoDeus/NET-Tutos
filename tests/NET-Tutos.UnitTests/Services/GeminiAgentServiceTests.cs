using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class GeminiAgentServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public List<HttpRequestMessage> CapturedRequests { get; } = new();

        public void EnqueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequests.Add(request);
            if (_responses.Count > 0)
            {
                return Task.FromResult(_responses.Dequeue());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }

    [Fact]
    public async Task ChatAsync_WhenApiKeyMissing_SeamlesslyDelegatesToFallbackEngine()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var codeService = new Mock<ICodeExecutionService>();
        var streakService = new Mock<IStreakService>();
        var fallbackEngine = new AiTutorService(context);
        var logger = new Mock<ILogger<GeminiAgentService>>();

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Gemini:ApiKey", "" }, // Missing
            { "Gemini:Enabled", "true" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("Gemini")).Returns(new HttpClient());

        var agent = new GeminiAgentService(
            mockFactory.Object,
            configuration,
            codeService.Object,
            context,
            streakService.Object,
            fallbackEngine,
            logger.Object);

        var request = new AiTutorChatRequest { Message = "Làm chủ LINQ và Deferred Execution" };

        // Act
        var result = await agent.ChatAsync(request, isEnglish: false);

        // Assert
        agent.IsLiveGeminiAvailable.Should().BeFalse();
        result.Should().NotBeNull();
        result.ResponseMarkdown.Should().Contain("LINQ");
        result.CodeSnippet.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChatAsync_WhenGeminiReturnsText_ParsesMarkdownAndFollowUps()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var codeService = new Mock<ICodeExecutionService>();
        var streakService = new Mock<IStreakService>();
        var fallbackEngine = new AiTutorService(context);
        var logger = new Mock<ILogger<GeminiAgentService>>();

        var handler = new MockHttpMessageHandler();
        var geminiResponseJson = new JsonObject
        {
            ["candidates"] = new JsonArray
            {
                new JsonObject
                {
                    ["content"] = new JsonObject
                    {
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["text"] = "### C# 14 Primary Constructors\nPrimary constructors allow concise syntax:\n```csharp\npublic class Service(ILogger logger) {}\n```\n\n### Suggested Follow-ups\n- How to use readonly fields with primary constructors?\n- Performance impact of primary constructors"
                            }
                        }
                    }
                }
            }
        };

        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(geminiResponseJson)
        });

        var client = new HttpClient(handler);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("Gemini")).Returns(client);

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Gemini:ApiKey", "AIzaSyTestMockKey" },
            { "Gemini:Model", "gemini-2.0-flash" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        var agent = new GeminiAgentService(
            mockFactory.Object,
            configuration,
            codeService.Object,
            context,
            streakService.Object,
            fallbackEngine,
            logger.Object);

        var request = new AiTutorChatRequest { Message = "Explain primary constructors in C#" };

        // Act
        var result = await agent.ChatAsync(request, isEnglish: true);

        // Assert
        agent.IsLiveGeminiAvailable.Should().BeTrue();
        result.Should().NotBeNull();
        result.ResponseMarkdown.Should().Contain("Primary Constructors");
        result.CodeSnippet.Should().Contain("public class Service(ILogger logger)");
        result.SuggestedFollowUps.Should().Contain(f => f.Contains("readonly fields"));
    }

    [Fact]
    public async Task ChatAsync_WhenGeminiCallsTool_ExecutesLocalCodeRunnerAndReturnsSynthesizedAnswer()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var codeService = new Mock<ICodeExecutionService>();
        codeService.Setup(c => c.ExecuteAsync("Console.WriteLine(10 + 20);", 3000))
            .ReturnsAsync(new CodeExecutionResponse
            {
                IsSuccess = true,
                Output = "30\n",
                ExecutionTimeMs = 45
            });

        var streakService = new Mock<IStreakService>();
        var fallbackEngine = new AiTutorService(context);
        var logger = new Mock<ILogger<GeminiAgentService>>();

        var handler = new MockHttpMessageHandler();

        // 1st Gemini response: calls tool execute_csharp_code
        var toolCallResponse = new JsonObject
        {
            ["candidates"] = new JsonArray
            {
                new JsonObject
                {
                    ["content"] = new JsonObject
                    {
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["functionCall"] = new JsonObject
                                {
                                    ["name"] = "execute_csharp_code",
                                    ["args"] = new JsonObject
                                    {
                                        ["code"] = "Console.WriteLine(10 + 20);"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // 2nd Gemini response: synthesizes final message after seeing tool output
        var finalResponse = new JsonObject
        {
            ["candidates"] = new JsonArray
            {
                new JsonObject
                {
                    ["content"] = new JsonObject
                    {
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["text"] = "I executed your code in our Roslyn sandbox: output is `30`. Verified successfully in 45ms!\n\n### Suggested Follow-ups\n- Try bitwise operations\n- Test performance benchmark"
                            }
                        }
                    }
                }
            }
        };

        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(toolCallResponse) });
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(finalResponse) });

        var client = new HttpClient(handler);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("Gemini")).Returns(client);

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Gemini:ApiKey", "AIzaSyTestMockKey" },
            { "Gemini:Model", "gemini-2.0-flash" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        var agent = new GeminiAgentService(
            mockFactory.Object,
            configuration,
            codeService.Object,
            context,
            streakService.Object,
            fallbackEngine,
            logger.Object);

        var request = new AiTutorChatRequest { Message = "Chạy thử code tính 10 + 20" };

        // Act
        var result = await agent.ChatAsync(request, isEnglish: false);

        // Assert
        result.Should().NotBeNull();
        result.ResponseMarkdown.Should().Contain("output is `30`");
        codeService.Verify(c => c.ExecuteAsync("Console.WriteLine(10 + 20);", 3000), Times.Once);
        handler.CapturedRequests.Should().HaveCount(2); // Initial turn + functionResponse turn
    }

    [Fact]
    public async Task ChatAsync_WhenGeminiHttpFails_GracefullyFallsBackToOfflineEngine()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var codeService = new Mock<ICodeExecutionService>();
        var streakService = new Mock<IStreakService>();
        var fallbackEngine = new AiTutorService(context);
        var logger = new Mock<ILogger<GeminiAgentService>>();

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("Quota exceeded for quota metric 'Queries' and limit 'Queries per minute'")
        });

        var client = new HttpClient(handler);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("Gemini")).Returns(client);

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Gemini:ApiKey", "AIzaSyRateLimitedKey" },
            { "Gemini:Model", "gemini-2.0-flash" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        var agent = new GeminiAgentService(
            mockFactory.Object,
            configuration,
            codeService.Object,
            context,
            streakService.Object,
            fallbackEngine,
            logger.Object);

        var request = new AiTutorChatRequest { Message = "explain async await" };

        // Act
        var result = await agent.ChatAsync(request, isEnglish: true);

        // Assert
        result.Should().NotBeNull();
        result.ResponseMarkdown.Should().Contain("Offline Engine Active");
        result.ResponseMarkdown.Should().Contain("Async/Await"); // Still answered by fallback rule engine!
    }
}
