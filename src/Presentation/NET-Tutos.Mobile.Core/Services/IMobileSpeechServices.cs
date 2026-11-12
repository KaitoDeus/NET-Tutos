using NET_Tutos.Mobile.Core.Models;

namespace NET_Tutos.Mobile.Core.Services;

/// <summary>
/// Platform Speech-to-Text contract for bilingual (vi-VN / en-US) voice dictation and interview training.
/// </summary>
public interface ISpeechToTextService
{
    bool IsSupported { get; }
    Task<SpeechRecognitionResult> RecognizeOnceAsync(string languageCode = "vi-VN", CancellationToken cancellationToken = default);
    Task StopAsync();
}

/// <summary>
/// Cross-platform audio speech synthesis contract backed by server Neural TTS MP3 cache with OS fallback.
/// </summary>
public interface IAudioPlaybackService
{
    Task SpeakAsync(string text, string languageCode = "vi-VN", CancellationToken cancellationToken = default);
    Task StopSpeakingAsync();
}
