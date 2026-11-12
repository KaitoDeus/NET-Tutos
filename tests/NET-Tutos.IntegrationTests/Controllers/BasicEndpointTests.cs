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
    [InlineData("/coding-arena")]
    [InlineData("/dau-truong-code")]
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
    public async Task Post_AiTutorChat_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var request = new AiTutorChatRequest
        {
            Message = "What is dependency injection in .NET?"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/AiTutor/Chat", request);

        // Assert: Unauthenticated access requires signing in before chatting with agent
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_LoginPage_DoesNotShowDemoAccountsAndContainsGitHubAndGoogleExternalLoginForms()
    {
        // Act
        var loginHtml = await (await _client.GetAsync("/Account/Login")).Content.ReadAsStringAsync();
        var registerHtml = await (await _client.GetAsync("/Account/Register")).Content.ReadAsStringAsync();

        // Assert: No demo account buttons or credentials displayed
        loginHtml.Should().NotContain("student@nettutos.com");
        loginHtml.Should().NotContain("fillDemoCredentials");

        // Assert: Real GitHub & Google OAuth forms are present
        loginHtml.Should().Contain("/Account/ExternalLogin");
        loginHtml.Should().Contain("value=\"GitHub\"");
        loginHtml.Should().Contain("value=\"Google\"");

        registerHtml.Should().Contain("/Account/ExternalLogin");
        registerHtml.Should().Contain("value=\"GitHub\"");
        registerHtml.Should().Contain("value=\"Google\"");
    }

    [Fact]
    public async Task Get_AdaptiveRecommendationsAndKafkaAnalytics_ReturnValidJsonPayloads()
    {
        // Act
        var adaptiveResponse = await _client.GetAsync("/api/adaptive/recommendations");
        var kafkaResponse = await _client.GetAsync("/api/kafka/analytics");
        var interviewResponse = await _client.GetAsync("/Interview");

        // Assert
        adaptiveResponse.EnsureSuccessStatusCode();
        kafkaResponse.EnsureSuccessStatusCode();
        interviewResponse.EnsureSuccessStatusCode();

        var adaptiveJson = await adaptiveResponse.Content.ReadAsStringAsync();
        adaptiveJson.Should().Contain("recommendedLessons");
        adaptiveJson.Should().Contain("suggestedInterviewLevel");

        var kafkaJson = await kafkaResponse.Content.ReadAsStringAsync();
        kafkaJson.Should().Contain("totalEventsConsumed");
        kafkaJson.Should().Contain("recentEvents");

        var interviewHtml = await interviewResponse.Content.ReadAsStringAsync();
        interviewHtml.Should().Contain("adaptiveLearningPanel");
    }

    [Fact]
    public async Task Get_AiTutorPage_RendersValidHeaderTitleWithoutRawJavaScriptLeak()
    {
        // Act
        var response = await _client.GetAsync("/AiTutor");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("id=\"currentChatTitle\"");
        // Verify header section does not leak raw JS code before <script>
        var headerEnd = html.IndexOf("</header>", StringComparison.OrdinalIgnoreCase);
        headerEnd.Should().BeGreaterThan(0);
        html[..headerEnd].Should().NotContain("if (titleEl)");
    }

    [Fact]
    public async Task Get_MockInterviewAndTtsEndpoint_SupportVietnameseVoiceFallback()
    {
        // Act
        var mockPageResp = await _client.GetAsync("/Interview/Mock");
        var emptyTtsResp = await _client.GetAsync("/api/tts/speak?text=");

        // Assert
        mockPageResp.EnsureSuccessStatusCode();
        var html = await mockPageResp.Content.ReadAsStringAsync();
        html.Should().Contain("/api/tts/speak");
        html.Should().Contain("playBackendTts");
        html.Should().NotContain("bg-opacity-15");

        emptyTtsResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdminTelemetry_RequiresAdminRole_AndAdaptiveChallengeApiReturnsRunnableCode()
    {
        // Arrange
        var noRedirectClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var telemetryResp = await noRedirectClient.GetAsync("/Admin/Telemetry");
        var telemetryDataResp = await noRedirectClient.GetAsync("/Admin/TelemetryData");
        var challengeResp = await _client.PostAsJsonAsync("/api/adaptive/challenge", new AiAdaptiveChallengeRequest
        {
            Topic = "Async/Await & ThreadPool",
            Difficulty = "Intermediate"
        });
        var interviewHtml = await (await _client.GetAsync("/Interview")).Content.ReadAsStringAsync();

        // Assert - Security boundaries
        telemetryResp.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
        telemetryDataResp.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);

        // Assert - Adaptive challenge endpoint
        challengeResp.EnsureSuccessStatusCode();
        var challenge = await challengeResp.Content.ReadFromJsonAsync<AiAdaptiveChallengeResponse>();
        challenge.Should().NotBeNull();
        challenge!.Success.Should().BeTrue();
        challenge.StarterCode.Should().NotBeNullOrWhiteSpace();
        challenge.SolutionCode.Should().NotBeNullOrWhiteSpace();

        interviewHtml.Should().Contain("adaptiveChallengeModal");
        interviewHtml.Should().Contain("btnGenerateAdaptiveChallenge");
    }

    [Fact]
    public async Task Get_MultiSourceKnowledgeSearchAndLiveCodingWhiteboard_WorkEndToEnd()
    {
        // Act
        var knowledgeResp = await _client.GetAsync("/api/search/knowledge?q=async%20await&top=5");
        var mockResp = await _client.GetAsync("/Interview/Mock");

        // Assert
        knowledgeResp.EnsureSuccessStatusCode();
        var knowledgeJson = await knowledgeResp.Content.ReadAsStringAsync();
        knowledgeJson.Should().Contain("totalIndexedDocuments");
        knowledgeJson.Should().Contain("sourceCounts");

        mockResp.EnsureSuccessStatusCode();
        var mockHtml = await mockResp.Content.ReadAsStringAsync();
        mockHtml.Should().Contain("tabModeLiveCode");
        mockHtml.Should().Contain("liveCodingWhiteboardContainer");
        mockHtml.Should().Contain("submitWhiteboardReview");
    }

    [Fact]
    public async Task PrometheusMetricsAndVsCodeIdeEndpoints_WorkEndToEnd()
    {
        // Act - Prometheus /metrics scrape
        var metricsResp = await _client.GetAsync("/metrics");
        var metricsBody = await metricsResp.Content.ReadAsStringAsync();

        // Act - VS Code Extension IDE Bridge endpoints
        var statusResp = await _client.GetAsync("/api/v1/ide/status");
        var statusBody = await statusResp.Content.ReadAsStringAsync();

        var searchResp = await _client.GetAsync("/api/v1/ide/search?q=async&top=3&lang=en");
        var searchBody = await searchResp.Content.ReadAsStringAsync();

        var reviewResp = await _client.PostAsJsonAsync("/api/v1/ide/review", new
        {
            sourceCode = "public class Demo { public async Task<int> RunAsync() { await Task.Delay(5); return 42; } }",
            fileName = "Demo.cs",
            lang = "en"
        });
        var reviewBody = await reviewResp.Content.ReadAsStringAsync();

        var execResp = await _client.PostAsJsonAsync("/api/v1/ide/execute", new
        {
            code = "Console.WriteLine(6 * 7);",
            timeoutMs = 4000
        });
        var execBody = await execResp.Content.ReadAsStringAsync();

        // Assert
        metricsResp.EnsureSuccessStatusCode();
        metricsBody.Should().Contain("nettutos_service_up");
        metricsBody.Should().Contain("nettutos_web_http_requests_total");
        metricsBody.Should().Contain("nettutos_tts_cache_hit_ratio_percent");
        metricsBody.Should().Contain("nettutos_kafka_consumer_total_lag_messages");

        statusResp.EnsureSuccessStatusCode();
        statusBody.Should().Contain("NET-Tutos.IDE.Bridge");
        statusBody.Should().Contain("indexedKnowledgeDocuments");

        searchResp.EnsureSuccessStatusCode();
        searchBody.Should().Contain("totalIndexedDocuments");

        reviewResp.EnsureSuccessStatusCode();
        reviewBody.Should().Contain("\"review\"");
        reviewBody.Should().Contain("\"complexity\"");

        execResp.EnsureSuccessStatusCode();
        execBody.Should().Contain("42");
    }

    [Fact]
    public async Task TutorialsAndRoadmap_EnforceStageLocking_AndFixCardTitleMinutesLayout()
    {
        // Act
        var stage7ListResp = await _client.GetAsync("/Tutorials?category=microservices-distributed-systems");
        var stage1DetailResp = await _client.GetAsync("/bai-hoc/tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong");
        var stage7DetailResp = await _client.GetAsync("/bai-hoc/distributed-observability-opentelemetry-prometheus-grafana");
        var roadmapResp = await _client.GetAsync("/Roadmap");

        // Assert
        stage7ListResp.EnsureSuccessStatusCode();
        var listHtml = await stage7ListResp.Content.ReadAsStringAsync();
        listHtml.Should().Contain("text-truncate");
        listHtml.Should().Contain("text-nowrap flex-shrink-0");
        listHtml.Should().Contain("bi-lock-fill");

        stage1DetailResp.EnsureSuccessStatusCode();
        var stage1Html = await stage1DetailResp.Content.ReadAsStringAsync();
        stage1Html.Should().Contain("markdown-body");
        stage1Html.Should().NotContain("lessonLockedGate");

        stage7DetailResp.EnsureSuccessStatusCode();
        var stage7Html = await stage7DetailResp.Content.ReadAsStringAsync();
        stage7Html.Should().Contain("lessonLockedGate");

        roadmapResp.EnsureSuccessStatusCode();
        var roadmapHtml = await roadmapResp.Content.ReadAsStringAsync();
        roadmapHtml.Should().Contain("bi-unlock-fill");
        roadmapHtml.Should().Contain("bi-lock-fill");
    }

    [Fact]
    public async Task ResponsiveDesign_ServesMobileNavigationControlsAndLayoutGuardrails()
    {
        // Act
        var homeResp = await _client.GetAsync("/");
        var cssResp = await _client.GetAsync("/css/site.css");
        var jsResp = await _client.GetAsync("/js/site.js");
        var playgroundResp = await _client.GetAsync("/Playground");
        var mockResp = await _client.GetAsync("/Interview/Mock");
        var quizResp = await _client.GetAsync("/Quiz");

        // Assert
        homeResp.EnsureSuccessStatusCode();
        var homeHtml = await homeResp.Content.ReadAsStringAsync();
        homeHtml.Should().Contain("mobile-quick-actions");
        homeHtml.Should().Contain("mobile-drawer-controls");
        homeHtml.Should().Contain("nav-practice-dropdown");

        cssResp.EnsureSuccessStatusCode();
        var css = await cssResp.Content.ReadAsStringAsync();
        css.Should().Contain("overflow-x: clip");
        css.Should().Contain("mobile-quick-actions");
        css.Should().Contain("responsive-btn-group");

        jsResp.EnsureSuccessStatusCode();
        var js = await jsResp.Content.ReadAsStringAsync();
        js.Should().Contain("openMobileNotifications");

        playgroundResp.EnsureSuccessStatusCode();
        mockResp.EnsureSuccessStatusCode();
        quizResp.EnsureSuccessStatusCode();

        var mockHtml = await mockResp.Content.ReadAsStringAsync();
        mockHtml.Should().Contain("responsive-btn-group");

        var quizHtml = await quizResp.Content.ReadAsStringAsync();
        quizHtml.Should().Contain("min-w-0 flex-grow-1");

        var aiResp = await _client.GetAsync("/AiTutor");
        aiResp.EnsureSuccessStatusCode();
        var aiHtml = await aiResp.Content.ReadAsStringAsync();
        aiHtml.Should().Contain("aitutor-viewport position-relative");
        aiHtml.Should().Contain("id=\"chatSidebarBackdrop\"");
        aiHtml.Should().Contain("id=\"closeSidebarBtn\"");
        aiHtml.Should().Contain("closeSidebarOnMobile");
    }

    [Fact]
    public async Task Phase20_PersistentArenaAndLiveSignalR_ServesEndpointsLeaderboardMatchesAndReset()
    {
        // Act
        var pageResp = await _client.GetAsync("/coding-arena");
        var joinResp = await _client.PostAsJsonAsync("/api/arena/join", new
        {
            roomCode = "ARENA-LINQ-01",
            playerName = "phase20Tester"
        });
        var submitResp = await _client.PostAsJsonAsync("/api/arena/submit", new
        {
            roomCode = "ARENA-LINQ-01",
            playerName = "phase20Tester",
            sourceCode = "Console.WriteLine(88);"
        });
        var leaderboardResp = await _client.GetAsync("/api/arena/leaderboard");
        var matchesResp = await _client.GetAsync("/api/arena/matches?roomCode=ARENA-LINQ-01");
        var resetResp = await _client.PostAsJsonAsync("/api/arena/reset", new
        {
            roomCode = "ARENA-LINQ-01"
        });

        // Assert
        pageResp.EnsureSuccessStatusCode();
        var arenaHtml = await pageResp.Content.ReadAsStringAsync();
        arenaHtml.Should().Contain("/hubs/arena");
        arenaHtml.Should().Contain("ArenaStateUpdated");
        arenaHtml.Should().Contain("arenaCountdownTimer");
        arenaHtml.Should().Contain("arenaLeaderboardBody");

        joinResp.EnsureSuccessStatusCode();
        submitResp.EnsureSuccessStatusCode();
        var submitBody = await submitResp.Content.ReadAsStringAsync();
        submitBody.Should().Contain("\"isCorrectOutput\":true");

        leaderboardResp.EnsureSuccessStatusCode();
        var lbJson = await leaderboardResp.Content.ReadAsStringAsync();
        lbJson.Should().Contain("phase20Tester");

        matchesResp.EnsureSuccessStatusCode();
        var matchesJson = await matchesResp.Content.ReadAsStringAsync();
        matchesJson.Should().Contain("ARENA-LINQ-01");
        matchesJson.Should().Contain("phase20Tester");

        resetResp.EnsureSuccessStatusCode();
        var resetJson = await resetResp.Content.ReadAsStringAsync();
        resetJson.Should().Contain("\"status\":\"Open\"");
    }

    [Fact]
    public async Task Phase21_NeuralEmbeddingsAndPgVectorHybridSearch_SyncsAndQueriesKnowledgeBase()
    {
        // Act
        var syncResp = await _client.PostAsync("/api/search/embeddings/sync", content: null);
        var statusResp = await _client.GetAsync("/api/search/embeddings/status");
        var knowledgeResp = await _client.GetAsync("/api/search/knowledge?q=bat+dong+bo+async+await&top=5");

        // Assert
        syncResp.EnsureSuccessStatusCode();
        var syncJson = await syncResp.Content.ReadAsStringAsync();
        syncJson.Should().Contain("\"success\":true");
        syncJson.Should().Contain("\"neuralDimensions\":768");

        statusResp.EnsureSuccessStatusCode();
        var statusJson = await statusResp.Content.ReadAsStringAsync();
        statusJson.Should().Contain("\"persistedEmbeddingsCount\":");

        knowledgeResp.EnsureSuccessStatusCode();
        var searchJson = await knowledgeResp.Content.ReadAsStringAsync();
        searchJson.Should().Contain("\"searchMode\":\"dense-neural-hybrid\"");
        searchJson.Should().Contain("\"neuralDimensions\":768");
    }

    [Fact]
    public async Task Phase23_ScaleOutDiagnostics_ReturnsTopologyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/scaleout/diagnostics");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"dataProtectionMode\":");
        json.Should().Contain("\"signalRBackplane\":");
        json.Should().Contain("nettutos:dataprotection-keys");
        json.Should().Contain("nettutos:signalr:");
    }
}
