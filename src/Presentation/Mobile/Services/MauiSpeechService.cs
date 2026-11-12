using NET_Tutos.Mobile.Core.Models;
using NET_Tutos.Mobile.Core.Services;

namespace NET_Tutos.Mobile.Services;

/// <summary>
/// Cross-platform neural audio playback combining server-cached MP3 streams with OS Text-to-Speech fallback.
/// </summary>
public sealed partial class MauiAudioPlaybackService : IAudioPlaybackService
{
    private readonly IApiService _apiService;
    private CancellationTokenSource? _speechCts;

    public MauiAudioPlaybackService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task SpeakAsync(string text, string languageCode = "vi-VN", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        await StopSpeakingAsync().ConfigureAwait(false);
        _speechCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _speechCts.Token;

        var shortLang = NeuralTtsCacheService.NormalizeLanguageCode(languageCode);
        var audioFile = await _apiService.GetTtsAudioFileAsync(text, shortLang, ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(audioFile) && await TryPlayPlatformAudioAsync(audioFile, ct).ConfigureAwait(false))
        {
            return;
        }

        var preparedText = NeuralTtsCacheService.SanitizeForSpeech(text);
        if (string.IsNullOrWhiteSpace(preparedText) || ct.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync().ConfigureAwait(false);
            var locale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(shortLang, StringComparison.OrdinalIgnoreCase));

            var options = new SpeechOptions
            {
                Locale = locale,
                Pitch = 1.0f,
                Volume = 1.0f
            };

            await TextToSpeech.Default.SpeakAsync(preparedText, options, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation on consecutive speech taps.
        }
        catch
        {
            // Swallow platform TTS engine unavailability errors gracefully.
        }
    }

    public Task StopSpeakingAsync()
    {
        _speechCts?.Cancel();
        _speechCts?.Dispose();
        _speechCts = null;
        StopPlatformAudio();
        return Task.CompletedTask;
    }

    private partial Task<bool> TryPlayPlatformAudioAsync(string filePath, CancellationToken cancellationToken);
    private partial void StopPlatformAudio();
}

#if !ANDROID
public sealed partial class MauiAudioPlaybackService
{
    private partial Task<bool> TryPlayPlatformAudioAsync(string filePath, CancellationToken cancellationToken)
        => Task.FromResult(false);

    private partial void StopPlatformAudio()
    {
    }
}

/// <summary>
/// Non-Android fallback STT service indicating platform microphone dictation is unavailable outside Android.
/// </summary>
public sealed class FallbackSpeechToTextService : ISpeechToTextService
{
    public bool IsSupported => false;

    public Task<SpeechRecognitionResult> RecognizeOnceAsync(string languageCode = "vi-VN", CancellationToken cancellationToken = default)
        => Task.FromResult(new SpeechRecognitionResult
        {
            IsSuccess = false,
            LanguageCode = languageCode,
            ErrorMessage = "Native Speech-to-Text requires an Android device; type or edit your answer manually below."
        });

    public Task StopAsync() => Task.CompletedTask;
}
#endif
