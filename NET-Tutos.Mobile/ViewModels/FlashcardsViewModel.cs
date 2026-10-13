using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class FlashcardsViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

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
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasStatusMessage;

    [ObservableProperty]
    private bool hasCards;

    [ObservableProperty]
    private bool isEmpty;

    public string ProgressText => Cards.Count > 0 
        ? $"Thẻ {CurrentIndex + 1} / {Cards.Count}" 
        : "0 / 0";

    public double ProgressFraction => Cards.Count > 0 
        ? (double)(CurrentIndex + 1) / Cards.Count 
        : 0;

    public bool CanGoPrevious => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < Cards.Count - 1;

    public FlashcardsViewModel(IApiService apiService)
    {
        _apiService = apiService;
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

            // 2. Load Cards
            var cardList = await _apiService.GetFlashcardsAsync(SelectedTopic);
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
            var cardList = await _apiService.GetFlashcardsAsync(SelectedTopic);
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
            if (result.IsSuccess)
            {
                card.BoxLevel = result.NewBoxLevel;
                card.IsMastered = result.IsMastered;
                ShowStatus($"+{result.XpEarned} XP! Đã ghi nhận vào {card.BoxLevelText}");
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
    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowStatus(string msg)
    {
        StatusMessage = msg;
        HasStatusMessage = true;
    }
}
