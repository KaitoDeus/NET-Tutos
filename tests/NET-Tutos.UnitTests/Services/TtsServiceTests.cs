using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NET_Tutos.Services;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class TtsServiceTests
{
    private readonly Mock<ILogger<TtsService>> _loggerMock = new();

    [Fact]
    public void PrepareSpeechText_StripsHtmlAndNormalizesDotNetTerms()
    {
        var raw = "<p>Học <strong>ASP.NET</strong> Core và <code>C#</code> trong hệ sinh thái .NET 10!</p>";
        var result = TtsService.PrepareSpeechText(raw);

        result.Should().NotContain("<p>");
        result.Should().Contain("A S P dot net");
        result.Should().Contain("C sharp");
        result.Should().Contain("dot net 10");
    }

    [Fact]
    public void SplitIntoChunks_LongVietnameseText_SplitsWithoutBreakingWords()
    {
        var text = "Phân biệt Value Type và Reference Type trong C sharp. Chúng được cấp phát ở đâu trên bộ nhớ Stack hay Heap, và khi nào một Value Type bị đóng gói Boxing chuyển lên Managed Heap để Garbage Collector thu hồi?";
        var chunks = TtsService.SplitIntoChunks(text, maxChunkLength: 100);

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().OnlyContain(c => c.Length <= 100);
        string.Join(" ", chunks).Should().Contain("Garbage Collector");
    }

    [Fact]
    public async Task SynthesizeSpeechAsync_WhenCached_ReturnsCachedAudioWithoutHttpCall()
    {
        // Arrange
        var fakeAudio = new byte[] { 0x49, 0x44, 0x33, 0x01, 0x02 };
        var cacheMock = new Mock<IAppCacheService>();
        cacheMock.Setup(c => c.GetAsync<byte[]>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeAudio);

        var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var service = new TtsService(httpFactoryMock.Object, _loggerMock.Object, cacheMock.Object);

        // Act
        var bytes = await service.SynthesizeSpeechAsync("Xin chào bạn", "vi");

        // Assert
        bytes.Should().BeEquivalentTo(fakeAudio);
        httpFactoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SynthesizeSpeechAsync_WhenNotCached_FetchesFromUpstreamAndCaches()
    {
        // Arrange
        var sampleMp3Chunk = new byte[] { 0xFF, 0xFB, 0x90, 0x00, 0x11 };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("tl=vi")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(sampleMp3Chunk)
            });

        var client = new HttpClient(handlerMock.Object);
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(f => f.CreateClient("Tts")).Returns(client);

        var cacheMock = new Mock<IAppCacheService>();
        cacheMock.Setup(c => c.GetAsync<byte[]>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var service = new TtsService(httpFactoryMock.Object, _loggerMock.Object, cacheMock.Object);

        // Act
        var result = await service.SynthesizeSpeechAsync("Câu hỏi phỏng vấn C#", "vi");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(sampleMp3Chunk);
        cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(k => k.StartsWith("tts:vi:")),
            sampleMp3Chunk,
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
