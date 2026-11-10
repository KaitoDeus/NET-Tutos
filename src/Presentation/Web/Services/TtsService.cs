using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NET_Tutos.Services;

public interface ITtsService
{
    Task<byte[]?> SynthesizeSpeechAsync(string text, string lang, CancellationToken cancellationToken = default);
}

/// <summary>
/// Server-side Neural Text-to-Speech proxy with L1/L2 Redis caching and automatic text chunking.
/// Guarantees natural Vietnamese and English voice playback across all browsers even when the user's
/// OS lacks local Vietnamese language speech packs.
/// </summary>
public sealed class TtsService : ITtsService
{
    private const int MaxInputChars = 1500;
    private const int MaxChunkChars = 180;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAppCacheService? _cacheService;
    private readonly IObservabilityService? _observability;
    private readonly ILogger<TtsService> _logger;

    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MultiSpaceRegex = new(@"\s+", RegexOptions.Compiled);

    public TtsService(
        IHttpClientFactory httpClientFactory,
        ILogger<TtsService> logger,
        IAppCacheService? cacheService = null,
        IObservabilityService? observability = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cacheService = cacheService;
        _observability = observability;
    }

    public async Task<byte[]?> SynthesizeSpeechAsync(string text, string lang, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var sw = Stopwatch.StartNew();
        string normalizedLang = lang.Equals("en", StringComparison.OrdinalIgnoreCase) || lang.StartsWith("en-", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "vi";

        string prepared = PrepareSpeechText(text);
        if (string.IsNullOrWhiteSpace(prepared)) return null;

        string cacheKey = $"tts:{normalizedLang}:{ComputeSha256(prepared)}";
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<byte[]>(cacheKey, cancellationToken);
            if (cached != null && cached.Length > 0)
            {
                sw.Stop();
                _observability?.RecordTtsRequest(normalizedLang, cacheHit: true, isSuccess: true, sw.Elapsed.TotalMilliseconds);
                return cached;
            }
        }

        var chunks = SplitIntoChunks(prepared, MaxChunkChars);
        if (chunks.Count == 0) return null;

        try
        {
            var client = _httpClientFactory.CreateClient("Tts");
            using var ms = new MemoryStream();

            foreach (var chunk in chunks)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var url = $"https://translate.google.com/translate_tts?ie=UTF-8&tl={normalizedLang}&client=tw-ob&q={Uri.EscapeDataString(chunk)}";
                using var resp = await client.GetAsync(url, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    sw.Stop();
                    _observability?.RecordTtsRequest(normalizedLang, cacheHit: false, isSuccess: false, sw.Elapsed.TotalMilliseconds);
                    _logger.LogWarning("TTS upstream returned HTTP {Status} for lang={Lang}", (int)resp.StatusCode, normalizedLang);
                    return null;
                }

                var segmentBytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
                if (segmentBytes.Length > 0)
                {
                    await ms.WriteAsync(segmentBytes, cancellationToken);
                }
            }

            var audioBytes = ms.ToArray();
            if (audioBytes.Length > 0 && _cacheService != null)
            {
                await _cacheService.SetAsync(cacheKey, audioBytes, l2Expiration: TimeSpan.FromDays(7), l1Expiration: TimeSpan.FromHours(6), cancellationToken: cancellationToken);
            }

            sw.Stop();
            bool ok = audioBytes.Length > 0;
            _observability?.RecordTtsRequest(normalizedLang, cacheHit: false, isSuccess: ok, sw.Elapsed.TotalMilliseconds);
            return ok ? audioBytes : null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _observability?.RecordTtsRequest(normalizedLang, cacheHit: false, isSuccess: false, sw.Elapsed.TotalMilliseconds);
            _logger.LogWarning("TTS synthesis failed cleanly ({Message}). Browser client will use fallback.", ex.Message);
            return null;
        }
    }

    public static string PrepareSpeechText(string raw)
    {
        var text = HtmlTagRegex.Replace(raw, " ");
        text = text.Replace("ASP.NET", "A S P dot net", StringComparison.OrdinalIgnoreCase)
                   .Replace(".NET", "dot net", StringComparison.OrdinalIgnoreCase)
                   .Replace("C#", "C sharp", StringComparison.OrdinalIgnoreCase);
        text = MultiSpaceRegex.Replace(text, " ").Trim();

        if (text.Length > MaxInputChars)
        {
            text = text[..MaxInputChars];
        }

        return text;
    }

    public static List<string> SplitIntoChunks(string text, int maxChunkLength)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        int index = 0;
        while (index < text.Length)
        {
            int remaining = text.Length - index;
            if (remaining <= maxChunkLength)
            {
                var tail = text[index..].Trim();
                if (!string.IsNullOrEmpty(tail)) result.Add(tail);
                break;
            }

            int sliceEnd = index + maxChunkLength;
            int splitAt = -1;

            // Prefer splitting on sentence boundaries
            foreach (char punct in new[] { '.', '?', '!', ';', ':' })
            {
                int pos = text.LastIndexOf(punct, sliceEnd - 1, maxChunkLength);
                if (pos > index + 20)
                {
                    splitAt = Math.Max(splitAt, pos + 1);
                }
            }

            // Second preference: comma or newline
            if (splitAt == -1)
            {
                int commaPos = text.LastIndexOf(',', sliceEnd - 1, maxChunkLength);
                if (commaPos > index + 20) splitAt = commaPos + 1;
            }

            // Fallback: word boundary space
            if (splitAt == -1)
            {
                int spacePos = text.LastIndexOf(' ', sliceEnd - 1, maxChunkLength);
                splitAt = spacePos > index ? spacePos : sliceEnd;
            }

            var segment = text[index..splitAt].Trim();
            if (!string.IsNullOrEmpty(segment))
            {
                result.Add(segment);
            }

            index = splitAt;
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }

        return result;
    }

    private static string ComputeSha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }
}
