using System.Net;
using System.Text;
using FluentAssertions;
using NET_Tutos.Mobile.Core.Models;
using NET_Tutos.Mobile.Core.Services;
using Xunit;

namespace NET_Tutos.Mobile.Tests;

public sealed class AtusStreamingAndVoiceTests : IDisposable
{
    private readonly string _cacheDir;

    public AtusStreamingAndVoiceTests()
    {
        _cacheDir = Path.Combine(Path.GetTempPath(), $"nettutos_tts_cache_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_cacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDir))
        {
            Directory.Delete(_cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task SseParser_ParsesInitChunksDoneAndIgnoresNoise()
    {
        var convId = Guid.NewGuid();
        var ssePayload = string.Join("\n\n",
            ": keepalive comment",
            $"data: {{\"type\":\"init\",\"conversationId\":\"{convId}\",\"persisted\":true}}",
            "data: {\"type\":\"chunk\",\"text\":\"Async \"}",
            "data: {\"type\":\"chunk\",\"text\":\"StateMachine\"}",
            "data: [DONE]",
            $"data: {{\"type\":\"done\",\"conversationId\":\"{convId}\",\"title\":\"Async Deep Dive\",\"persisted\":true}}",
            string.Empty);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ssePayload));
        var events = new List<AtusStreamEvent>();

        await foreach (var evt in AtusSseStreamParser.ParseAsync(stream))
        {
            events.Add(evt);
        }

        events.Should().HaveCount(4);
        events[0].Type.Should().Be(AtusStreamEventType.Init);
        events[0].ConversationId.Should().Be(convId);
        events[0].Persisted.Should().BeTrue();
        events[1].Text.Should().Be("Async ");
        events[2].Text.Should().Be("StateMachine");
        events[3].Type.Should().Be(AtusStreamEventType.Done);
        events[3].Title.Should().Be("Async Deep Dive");
    }

    [Fact]
    public async Task AtusMobileClient_StreamsSseAndFallsBackToOfflineVoiceRubricOnError()
    {
        var convId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("atus/chat/stream", StringComparison.OrdinalIgnoreCase))
            {
                var sse = $"data: {{\"type\":\"init\",\"conversationId\":\"{convId}\",\"persisted\":false}}\n\n" +
                          "data: {\"type\":\"chunk\",\"text\":\"Hello Atus\"}\n\n" +
                          $"data: {{\"type\":\"done\",\"conversationId\":\"{convId}\",\"title\":\"Demo\",\"persisted\":false}}\n\n";

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
                };
            }

            throw new HttpRequestException("Simulated offline socket drop");
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = MobileEndpointResolver.NormalizeMobileApiUri("http://10.0.2.2:5000/api/mobile")
        };
        var client = new AtusMobileClient(httpClient);

        var streamed = new List<AtusStreamEvent>();
        await foreach (var evt in client.StreamChatAsync(new AtusChatRequestPayload { Message = "Hi" }))
        {
            streamed.Add(evt);
        }

        streamed.Should().Contain(e => e.Type == AtusStreamEventType.Chunk && e.Text == "Hello Atus");

        var offlineEval = await client.EvaluateVoiceAnswerAsync(new VoiceEvaluationRequest
        {
            Topic = "AsyncConcurrency",
            Language = "vi",
            Transcript = "async/await giúp non-blocking cho ThreadPool bằng State Machine do compiler sinh ra và cần CancellationToken để tránh deadlock."
        });

        offlineEval.IsSuccess.Should().BeTrue();
        offlineEval.IsOfflineEvaluation.Should().BeTrue();
        offlineEval.OverallScore.Should().BeGreaterThanOrEqualTo(80);
        offlineEval.SpokenSummary.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void VoiceTrainingEvaluator_ScoresBriefVsDetailedEnglishAnswers()
    {
        var brief = VoiceTrainingEvaluator.EvaluateOffline(new VoiceEvaluationRequest
        {
            Topic = "EfCoreDatabase",
            Language = "en",
            Transcript = "It tracks entities."
        });

        brief.OverallScore.Should().BeGreaterThan(30);

        var tooShort = VoiceTrainingEvaluator.EvaluateOffline(new VoiceEvaluationRequest
        {
            Language = "en",
            Transcript = "yes"
        });
        tooShort.OverallScore.Should().Be(25);

        var senior = VoiceTrainingEvaluator.EvaluateOffline(new VoiceEvaluationRequest
        {
            Topic = "EfCoreDatabase",
            Language = "en",
            Transcript = "Use AsNoTracking for read-only queries to skip Change Tracker overhead, prevent N+1 queries with Include or Select projections, and understand IQueryable translates expressions to SQL."
        });

        senior.OverallScore.Should().BeGreaterThanOrEqualTo(85);
        senior.Strengths.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task NeuralTtsCacheService_ResolvesRootEndpointDownloadsAndCachesOnDisk()
    {
        int networkCalls = 0;
        var mp3Sample = new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x11, 0x22 };

        var handler = new StubHttpMessageHandler(req =>
        {
            networkCalls++;
            req.RequestUri!.AbsolutePath.Should().Be("/api/tts/speak");
            req.RequestUri.Query.Should().Contain("lang=vi");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(mp3Sample)
            };
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = MobileEndpointResolver.NormalizeMobileApiUri("http://10.0.2.2:5000/api/mobile")
        };

        var tts = new NeuralTtsCacheService(httpClient, _cacheDir);

        var firstPath = await tts.GetOrFetchAudioFileAsync("**Xin chào** `async/await`!", "vi-VN");
        var secondPath = await tts.GetOrFetchAudioFileAsync("**Xin chào** `async/await`!", "vi-VN");

        firstPath.Should().NotBeNullOrEmpty();
        File.Exists(firstPath!).Should().BeTrue();
        secondPath.Should().Be(firstPath);
        networkCalls.Should().Be(1);
    }

    [Fact]
    public void MobileEndpointResolver_DerivesServerRootFromMobileApiBase()
    {
        var root = MobileEndpointResolver.ResolveServerRootUri("http://10.0.2.2:5000/api/mobile");
        root.ToString().Should().Be("http://10.0.2.2:5000/");

        NeuralTtsCacheService.NormalizeLanguageCode("en-US").Should().Be("en");
        NeuralTtsCacheService.NormalizeLanguageCode("vi-VN").Should().Be("vi");
        NeuralTtsCacheService.SanitizeForSpeech("## `Task.Run` **Guide**").Should().Be("Task.Run Guide");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
