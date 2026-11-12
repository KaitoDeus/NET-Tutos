using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using NET_Tutos.Mobile.Core.Models;
using NET_Tutos.Mobile.Core.Services;

namespace NET_Tutos.Mobile.Services;

/// <summary>
/// Android partial implementation providing native MediaPlayer audio output for cached Neural TTS MP3 files.
/// </summary>
public sealed partial class MauiAudioPlaybackService
{
    private MediaPlayer? _mediaPlayer;

    private partial Task<bool> TryPlayPlatformAudioAsync(string filePath, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                StopPlatformAudio();
                var player = new MediaPlayer();
                _mediaPlayer = player;
                player.SetDataSource(filePath);
                player.Completion += (_, _) =>
                {
                    StopPlatformAudio();
                    tcs.TrySetResult(true);
                };
                player.Error += (_, _) =>
                {
                    StopPlatformAudio();
                    tcs.TrySetResult(false);
                };
                player.Prepare();
                player.Start();
            }
            catch
            {
                StopPlatformAudio();
                tcs.TrySetResult(false);
            }
        });

        cancellationToken.Register(() =>
        {
            MainThread.BeginInvokeOnMainThread(StopPlatformAudio);
            tcs.TrySetResult(false);
        });

        return tcs.Task;
    }

    private partial void StopPlatformAudio()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }

                _mediaPlayer.Release();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }
        }
        catch
        {
            _mediaPlayer = null;
        }
    }
}

/// <summary>
/// Android SpeechRecognizer wrapper supporting vi-VN and en-US voice dictation for interview practice.
/// </summary>
public sealed class AndroidSpeechToTextService : ISpeechToTextService
{
    private SpeechRecognizer? _recognizer;
    private TaskCompletionSource<SpeechRecognitionResult>? _activeTcs;

    public bool IsSupported => SpeechRecognizer.IsRecognitionAvailable(global::Android.App.Application.Context);

    public async Task<SpeechRecognitionResult> RecognizeOnceAsync(
        string languageCode = "vi-VN",
        CancellationToken cancellationToken = default)
    {
        var status = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.RequestAsync<Permissions.Microphone>()).ConfigureAwait(false);

        if (status != PermissionStatus.Granted)
        {
            return new SpeechRecognitionResult
            {
                IsSuccess = false,
                LanguageCode = languageCode,
                ErrorMessage = "Quyền truy cập Micro (RECORD_AUDIO) chưa được cấp."
            };
        }

        if (!IsSupported)
        {
            return new SpeechRecognitionResult
            {
                IsSuccess = false,
                LanguageCode = languageCode,
                ErrorMessage = "Thiết bị không hỗ trợ dịch vụ nhận dạng giọng nói Google."
            };
        }

        await StopAsync().ConfigureAwait(false);
        var tcs = new TaskCompletionSource<SpeechRecognitionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _activeTcs = tcs;

        using var reg = cancellationToken.Register(() =>
        {
            MainThread.BeginInvokeOnMainThread(CleanupRecognizer);
            tcs.TrySetResult(new SpeechRecognitionResult
            {
                IsSuccess = false,
                LanguageCode = languageCode,
                ErrorMessage = "Đã dừng thu âm."
            });
        });

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var context = global::Android.App.Application.Context;
                _recognizer = SpeechRecognizer.CreateSpeechRecognizer(context);
                if (_recognizer == null)
                {
                    tcs.TrySetResult(new SpeechRecognitionResult
                    {
                        IsSuccess = false,
                        LanguageCode = languageCode,
                        ErrorMessage = "Không thể khởi tạo SpeechRecognizer."
                    });
                    return;
                }

                _recognizer.SetRecognitionListener(new Listener(languageCode, tcs, CleanupRecognizer));

                var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                intent.PutExtra(RecognizerIntent.ExtraLanguage, languageCode);
                intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, languageCode);
                intent.PutExtra(RecognizerIntent.ExtraPartialResults, false);
                intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

                _recognizer.StartListening(intent);
            }
            catch (Exception ex)
            {
                CleanupRecognizer();
                tcs.TrySetResult(new SpeechRecognitionResult
                {
                    IsSuccess = false,
                    LanguageCode = languageCode,
                    ErrorMessage = ex.Message
                });
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    public Task StopAsync()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _recognizer?.StopListening();
            CleanupRecognizer();
        });
        _activeTcs?.TrySetCanceled();
        _activeTcs = null;
        return Task.CompletedTask;
    }

    private void CleanupRecognizer()
    {
        try
        {
            _recognizer?.Destroy();
        }
        catch
        {
            // Ignore Android lifecycle disposal races.
        }
        finally
        {
            _recognizer = null;
        }
    }

    private sealed class Listener : Java.Lang.Object, IRecognitionListener
    {
        private readonly string _languageCode;
        private readonly TaskCompletionSource<SpeechRecognitionResult> _tcs;
        private readonly Action _cleanup;

        public Listener(string languageCode, TaskCompletionSource<SpeechRecognitionResult> tcs, Action cleanup)
        {
            _languageCode = languageCode;
            _tcs = tcs;
            _cleanup = cleanup;
        }

        public void OnBeginningOfSpeech() { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnEndOfSpeech() { }
        public void OnEvent(int eventType, Bundle? @params) { }
        public void OnPartialResults(Bundle? partialResults) { }
        public void OnReadyForSpeech(Bundle? @params) { }
        public void OnRmsChanged(float rmsdB) { }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            _cleanup();
            _tcs.TrySetResult(new SpeechRecognitionResult
            {
                IsSuccess = false,
                LanguageCode = _languageCode,
                ErrorMessage = $"Speech recognition error: {error}"
            });
        }

        public void OnResults(Bundle? results)
        {
            var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            var transcript = matches?.FirstOrDefault() ?? string.Empty;
            _cleanup();

            _tcs.TrySetResult(new SpeechRecognitionResult
            {
                IsSuccess = !string.IsNullOrWhiteSpace(transcript),
                Transcript = transcript,
                LanguageCode = _languageCode,
                ErrorMessage = string.IsNullOrWhiteSpace(transcript) ? "Không nghe rõ nội dung nói, vui lòng thử lại." : null
            });
        }
    }
}
