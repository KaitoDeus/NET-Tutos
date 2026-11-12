using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Core.Services;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class FlashcardsViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAudioPlaybackService _audioPlayback;

    [ObservableProperty]
    private ObservableCollection<FlashcardTopicItem> topics = new();

    [ObservableProperty]
    private ObservableCollection<FlashcardItem> cards = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    [NotifyPropertyChangedFor(nameof(ProgressFraction))]
    [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private int currentIndex = 0;

    [ObservableProperty]
    private FlashcardItem? currentCard;

    [ObservableProperty]
    private string? selectedTopic;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DueFilterButtonText))]
    private bool dueOnly;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingSync))]
    [NotifyPropertyChangedFor(nameof(SyncButtonText))]
    private int pendingSyncCount;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasStatusMessage;

    [ObservableProperty]
    private bool hasCards;

    [ObservableProperty]
    private bool isEmpty;

    public bool HasPendingSync => PendingSyncCount > 0;
    public string DueFilterButtonText => DueOnly ? "Chỉ thẻ đến hạn: BẬT" : "Chỉ thẻ đến hạn: TẮT";
    public string SyncButtonText => $"Đồng bộ ({PendingSyncCount})";

    public string ProgressText => Cards.Count > 0 
        ? $"Thẻ {CurrentIndex + 1} / {Cards.Count}" 
        : "0 / 0";

    public double ProgressFraction => Cards.Count > 0 
        ? (double)(CurrentIndex + 1) / Cards.Count 
        : 0;

    public bool CanGoPrevious => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < Cards.Count - 1;

    public FlashcardsViewModel(IApiService apiService, IAudioPlaybackService audioPlayback)
    {
        _apiService = apiService;
        _audioPlayback = audioPlayback;
        Title = "Thẻ Ôn Tập Phỏng Vấn";
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            HasStatusMessage = false;

            // 1. Load Topics
            var topicList = await _apiService.GetFlashcardTopicsAsync();
            Topics.Clear();

            // All topic chip
            var allChip = new FlashcardTopicItem
            {
                Topic = string.Empty,
                Name = "Tất cả chủ đề",
                ColorClass = "#6366F1",
                IsSelected = string.IsNullOrEmpty(SelectedTopic)
            };
            Topics.Add(allChip);

            foreach (var t in topicList)
            {
                t.IsSelected = t.Topic == SelectedTopic;
                Topics.Add(t);
            }

            // 2. Load Cards (falls back cleanly to offline cached deck)
            var cardList = await _apiService.GetFlashcardsAsync(SelectedTopic, dueOnly: DueOnly);
            Cards.Clear();
            foreach (var c in cardList)
            {
                Cards.Add(c);
            }

            PendingSyncCount = await _apiService.GetPendingOfflineReviewCountAsync();
            if (PendingSyncCount > 0 && _apiService.IsAuthenticated)
            {
                var syncResult = await _apiService.SyncOfflineReviewsAsync();
                PendingSyncCount = syncResult.RemainingCount;
                if (syncResult.IsSuccess && syncResult.SyncedCount > 0)
                {
                    ShowStatus(syncResult.Message);
                }
            }

            HasCards = Cards.Count > 0;
            IsEmpty = !HasCards;

            if (HasCards)
            {
                CurrentIndex = 0;
                CurrentCard = Cards[0];
                CurrentCard.IsAnswerRevealed = false;
            }
            else
            {
                CurrentCard = null;
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Không thể tải danh sách thẻ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelectTopicAsync(string? topic)
    {
        SelectedTopic = topic;
        foreach (var t in Topics)
        {
            t.IsSelected = (string.IsNullOrEmpty(topic) && string.IsNullOrEmpty(t.Topic)) || (t.Topic == topic);
        }

        await LoadCardsForSelectedTopicAsync();
    }

    private async Task LoadCardsForSelectedTopicAsync()
    {
        try
        {
            IsBusy = true;
            var cardList = await _apiService.GetFlashcardsAsync(SelectedTopic, dueOnly: DueOnly);
            Cards.Clear();
            foreach (var c in cardList)
            {
                Cards.Add(c);
            }

            HasCards = Cards.Count > 0;
            IsEmpty = !HasCards;

            if (HasCards)
            {
                CurrentIndex = 0;
                CurrentCard = Cards[0];
                CurrentCard.IsAnswerRevealed = false;
            }
            else
            {
                CurrentCard = null;
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi lọc chủ đề: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void FlipCard()
    {
        if (CurrentCard != null)
        {
            CurrentCard.IsAnswerRevealed = !CurrentCard.IsAnswerRevealed;
        }
    }

    [RelayCommand]
    public void PreviousCard()
    {
        if (CanGoPrevious)
        {
            CurrentIndex--;
            CurrentCard = Cards[CurrentIndex];
            CurrentCard.IsAnswerRevealed = false;
            HasStatusMessage = false;
        }
    }

    [RelayCommand]
    public void NextCard()
    {
        if (CanGoNext)
        {
            CurrentIndex++;
            CurrentCard = Cards[CurrentIndex];
            CurrentCard.IsAnswerRevealed = false;
            HasStatusMessage = false;
        }
    }

    [RelayCommand]
    public async Task ReviewCardAsync(string ratingStr)
    {
        if (CurrentCard == null || !int.TryParse(ratingStr, out var rating)) return;

        var card = CurrentCard;

        // Optimistic UI updates
        switch (rating)
        {
            case 1: // Again
                card.BoxLevel = 1;
                break;
            case 2: // Hard
                card.BoxLevel = Math.Max(1, card.BoxLevel);
                break;
            case 3: // Good
                card.BoxLevel = Math.Min(5, card.BoxLevel + 1);
                break;
            case 4: // Mastered
                card.BoxLevel = 5;
                card.IsMastered = true;
                break;
        }

        try
        {
            var result = await _apiService.ReviewFlashcardAsync(card.Id, rating);
            PendingSyncCount = await _apiService.GetPendingOfflineReviewCountAsync();
            if (result.IsSuccess)
            {
                card.BoxLevel = result.NewBoxLevel;
                card.IsMastered = result.IsMastered;
                card.NextReviewDate = result.NextReviewDate;
                card.IsDue = false;
                ShowStatus(result.QueuedOffline
                    ? result.Message
                    : $"+{result.XpEarned} XP! Đã ghi nhận vào {card.BoxLevelText}");
            }
        }
        catch
        {
            // Silent error; optimistic UI preserved
        }

        // Auto advance after 400ms if not last card
        await Task.Delay(400);
        if (CanGoNext)
        {
            NextCard();
        }
    }

    [RelayCommand]
    public async Task ToggleDueOnlyAsync()
    {
        DueOnly = !DueOnly;
        await LoadCardsForSelectedTopicAsync();
    }

    [RelayCommand]
    public async Task SyncOfflineReviewsAsync()
    {
        var syncResult = await _apiService.SyncOfflineReviewsAsync();
        PendingSyncCount = syncResult.RemainingCount;
        ShowStatus(syncResult.Message);
        if (syncResult.IsSuccess && syncResult.SyncedCount > 0)
        {
            await LoadCardsForSelectedTopicAsync();
        }
    }

    [RelayCommand]
    public async Task SpeakCurrentCardAsync()
    {
        if (CurrentCard == null) return;
        var text = CurrentCard.IsAnswerRevealed
            ? $"{CurrentCard.AnswerMarkdownVi} {CurrentCard.InterviewTipVi}"
            : CurrentCard.QuestionVi;

        await _audioPlayback.SpeakAsync(text, "vi-VN");
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await _audioPlayback.StopSpeakingAsync();
        await Shell.Current.GoToAsync("..");
    }

    private void ShowStatus(string msg)
    {
        StatusMessage = msg;
        HasStatusMessage = true;
    }
}
