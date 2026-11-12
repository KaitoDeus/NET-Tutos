using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Core.Models;
using NET_Tutos.Mobile.Core.Services;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

/// <summary>
/// Interactive bilingual (vi-VN / en-US) Speech-to-Text + Neural TTS voice interview training ViewModel.
/// </summary>
public partial class VoiceTrainingViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly ISpeechToTextService _speechToText;
    private readonly IAudioPlaybackService _audioPlayback;
    private CancellationTokenSource? _micCts;
    private int _currentCardIndex;

    public ObservableCollection<FlashcardItem> Questions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentQuestionText))]
    [NotifyPropertyChangedFor(nameof(TopicBadgeText))]
    private FlashcardItem? currentQuestion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LanguageButtonText))]
    [NotifyPropertyChangedFor(nameof(CurrentQuestionText))]
    private bool isEnglish;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordButtonText))]
    private bool isRecording;

    [ObservableProperty]
    private bool isEvaluating;

    [ObservableProperty]
    private string transcript = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Chọn ngôn ngữ, nghe câu hỏi rồi nhấn Micro để trả lời.";

    [ObservableProperty]
    private VoiceEvaluationResult? evaluation;

    [ObservableProperty]
    private bool hasEvaluation;

    public string LanguageButtonText => IsEnglish ? "Ngôn ngữ: English (en-US)" : "Ngôn ngữ: Tiếng Việt (vi-VN)";
    public string RecordButtonText => IsRecording ? "Đang nghe... (Nhấn để dừng)" : "Nhấn để nói câu trả lời (STT)";
    public string TopicBadgeText => CurrentQuestion == null ? ".NET Core" : $"{CurrentQuestion.TopicName} • {CurrentQuestion.DifficultyText}";

    public string CurrentQuestionText
    {
        get
        {
            if (CurrentQuestion != null)
            {
                return IsEnglish && !string.IsNullOrWhiteSpace(CurrentQuestion.QuestionEn)
                    ? CurrentQuestion.QuestionEn
                    : CurrentQuestion.QuestionVi;
            }

            return IsEnglish
                ? "Explain how async/await works internally in C# and why blocking with .Result causes deadlocks."
                : "Giải thích cơ chế bên dưới của async/await trong C# và tại sao dùng .Result có thể gây deadlock?";
        }
    }

    public VoiceTrainingViewModel(
        IApiService apiService,
        ISpeechToTextService speechToText,
        IAudioPlaybackService audioPlayback)
    {
        _apiService = apiService;
        _speechToText = speechToText;
        _audioPlayback = audioPlayback;
        Title = "Luyện Nói Phỏng Vấn AI";
    }

    [RelayCommand]
    public async Task LoadQuestionsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var cards = await _apiService.GetFlashcardsAsync(limit: 30);
            Questions.Clear();
            foreach (var c in cards)
            {
                Questions.Add(c);
            }

            if (Questions.Count > 0)
            {
                _currentCardIndex = 0;
                CurrentQuestion = Questions[0];
            }
        }
        catch
        {
            StatusMessage = "Đang dùng câu hỏi luyện nói mặc định (Offline ready).";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ToggleLanguage()
    {
        IsEnglish = !IsEnglish;
        StatusMessage = IsEnglish
            ? "Switched voice training to English (en-US)."
            : "Đã chuyển sang luyện phỏng vấn Tiếng Việt (vi-VN).";
    }

    [RelayCommand]
    public void NextQuestion()
    {
        if (Questions.Count == 0) return;

        _currentCardIndex = (_currentCardIndex + 1) % Questions.Count;
        CurrentQuestion = Questions[_currentCardIndex];
        Transcript = string.Empty;
        Evaluation = null;
        HasEvaluation = false;
        StatusMessage = $"Câu hỏi {_currentCardIndex + 1}/{Questions.Count} sẵn sàng.";
    }

    [RelayCommand]
    public async Task SpeakQuestionAsync()
    {
        var lang = IsEnglish ? "en-US" : "vi-VN";
        StatusMessage = IsEnglish ? "Playing Neural TTS prompt..." : "Đang đọc câu hỏi bằng Neural TTS...";
        await _audioPlayback.SpeakAsync(CurrentQuestionText, lang);
        StatusMessage = "Đã đọc xong câu hỏi.";
    }

    [RelayCommand]
    public async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            _micCts?.Cancel();
            await _speechToText.StopAsync();
            IsRecording = false;
            StatusMessage = "Đã dừng thu âm.";
            return;
        }

        if (!_speechToText.IsSupported)
        {
            StatusMessage = "Thiết bị này chưa bật Google SpeechRecognizer; bạn có thể nhập hoặc dán câu trả lời bên dưới để chấm điểm.";
            return;
        }

        _micCts?.Cancel();
        _micCts?.Dispose();
        _micCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));

        IsRecording = true;
        var lang = IsEnglish ? "en-US" : "vi-VN";
        StatusMessage = IsEnglish ? "Listening (en-US)... speak clearly." : "Đang thu âm (vi-VN)... mời bạn trả lời.";

        try
        {
            var result = await _speechToText.RecognizeOnceAsync(lang, _micCts.Token);
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Transcript))
            {
                Transcript = string.IsNullOrWhiteSpace(Transcript)
                    ? result.Transcript
                    : $"{Transcript.Trim()} {result.Transcript.Trim()}";
                StatusMessage = "Đã chuyển giọng nói thành văn bản! Nhấn 'Chấm điểm AI'.";
            }
            else
            {
                StatusMessage = result.ErrorMessage ?? "Không nhận dạng được giọng nói.";
            }
        }
        finally
        {
            IsRecording = false;
        }
    }

    [RelayCommand]
    public async Task EvaluateAnswerAsync()
    {
        if (IsEvaluating) return;
        if (string.IsNullOrWhiteSpace(Transcript))
        {
            StatusMessage = "Hãy nói qua Micro hoặc nhập câu trả lời trước khi chấm điểm.";
            return;
        }

        IsEvaluating = true;
        HasEvaluation = false;
        StatusMessage = "Atus AI đang chấm điểm bài trả lời phỏng vấn...";

        try
        {
            var req = new VoiceEvaluationRequest
            {
                FlashcardId = CurrentQuestion?.Id ?? 0,
                Question = CurrentQuestionText,
                Transcript = Transcript.Trim(),
                Topic = CurrentQuestion?.Topic ?? "CSharpCore",
                Difficulty = CurrentQuestion?.Difficulty ?? "Intermediate",
                Language = IsEnglish ? "en" : "vi"
            };

            var res = await _apiService.EvaluateVoiceAnswerAsync(req);
            Evaluation = res;
            HasEvaluation = res.IsSuccess;
            StatusMessage = res.IsOfflineEvaluation
                ? $"Chấm điểm Offline hoàn tất: {res.OverallScore}/100"
                : $"Chấm điểm Server hoàn tất: {res.OverallScore}/100";
        }
        finally
        {
            IsEvaluating = false;
        }
    }

    [RelayCommand]
    public async Task SpeakFeedbackAsync()
    {
        if (Evaluation == null || string.IsNullOrWhiteSpace(Evaluation.SpokenSummary))
        {
            return;
        }

        await _audioPlayback.SpeakAsync(Evaluation.SpokenSummary, IsEnglish ? "en-US" : "vi-VN");
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        _micCts?.Cancel();
        await _speechToText.StopAsync();
        await _audioPlayback.StopSpeakingAsync();
        await Shell.Current.GoToAsync("..");
    }
}
