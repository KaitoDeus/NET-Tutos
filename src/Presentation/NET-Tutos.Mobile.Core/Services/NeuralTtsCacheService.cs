using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NET_Tutos.Mobile.Core.Services;

/// <summary>
/// Downloads and locally caches MP3 synthesis output from the server Neural TTS endpoint (/api/tts/speak).
/// </summary>
public sealed class NeuralTtsCacheService
{
    private static readonly Regex MarkdownNoiseRegex = new(@"[`*_#>~]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly Func<Uri> _serverRootProvider;
    private readonly ConcurrentDictionary<string, byte[]> _memoryCache = new(StringComparer.Ordinal);

    public NeuralTtsCacheService(
        HttpClient httpClient,
        string cacheDirectory,
        Func<Uri>? serverRootProvider = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (string.IsNullOrWhiteSpace(cacheDirectory))
        {
            throw new ArgumentException("Cache directory cannot be empty.", nameof(cacheDirectory));
        }

        _cacheDirectory = cacheDirectory;
        Directory.CreateDirectory(_cacheDirectory);
        _serverRootProvider = serverRootProvider ?? (() => MobileEndpointResolver.ResolveServerRootUri(_httpClient.BaseAddress?.ToString()));
    }

    public static string NormalizeLanguageCode(string? language)
        => !string.IsNullOrWhiteSpace(language) && language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "vi";

    public static string SanitizeForSpeech(string rawText, int maxChars = 1200)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var cleaned = MarkdownNoiseRegex.Replace(rawText, " ");
        cleaned = WhitespaceRegex.Replace(cleaned, " ").Trim();
        return cleaned.Length > maxChars ? cleaned[..maxChars] : cleaned;
    }

    public async Task<string?> GetOrFetchAudioFileAsync(
        string text,
        string language = "vi",
        CancellationToken cancellationToken = default)
    {
        var prepared = SanitizeForSpeech(text);
        if (string.IsNullOrWhiteSpace(prepared))
        {
            return null;
        }

        var normalizedLang = NormalizeLanguageCode(language);
        var key = ComputeSha256($"{normalizedLang}:{prepared}");
        var cachedPath = Path.Combine(_cacheDirectory, $"tts_{normalizedLang}_{key}.mp3");

        if (File.Exists(cachedPath) && new FileInfo(cachedPath).Length > 0)
        {
            return cachedPath;
        }

        byte[]? audioBytes = _memoryCache.TryGetValue(key, out var memBytes) ? memBytes : null;
        if (audioBytes == null || audioBytes.Length == 0)
        {
            try
            {
                var rootUri = _serverRootProvider();
                var endpoint = new Uri(rootUri, $"api/tts/speak?text={Uri.EscapeDataString(prepared)}&lang={normalizedLang}");
                using var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                if (audioBytes.Length == 0)
                {
                    return null;
                }

                if (_memoryCache.Count >= 64)
                {
                    _memoryCache.Clear();
                }

                _memoryCache[key] = audioBytes;
            }
            catch
            {
                return null;
            }
        }

        var tempPath = cachedPath + ".tmp";
        await File.WriteAllBytesAsync(tempPath, audioBytes, cancellationToken).ConfigureAwait(false);
        File.Move(tempPath, cachedPath, overwrite: true);
        return cachedPath;
    }

    private static string ComputeSha256(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant()[..24];
    }
}
