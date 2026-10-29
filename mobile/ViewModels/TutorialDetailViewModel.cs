using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class TutorialDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string slug = string.Empty;

    [ObservableProperty]
    private TutorialDetail? tutorial;

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private string completionButtonText = "Đánh dấu đã hoàn thành (+20 XP)";

    [ObservableProperty]
    private Color completionButtonColor = Color.FromArgb("#512bd4");

    public ObservableCollection<QuizItem> Quizzes { get; } = new();

    public TutorialDetailViewModel(IApiService apiService)
    {
        _apiService = apiService;
        Title = "Chi tiết bài học";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("slug", out var s) && s is string tutSlug)
        {
            Slug = tutSlug;
            LoadDetailCommand.Execute(null);
        }
    }

    [RelayCommand]
    public async Task LoadDetailAsync()
    {
        if (string.IsNullOrWhiteSpace(Slug) || IsBusy) return;
        IsBusy = true;

        try
        {
            Tutorial = await _apiService.GetTutorialDetailAsync(Slug);
            if (Tutorial != null)
            {
                Title = Tutorial.Title;
                IsCompleted = Tutorial.IsCompleted;
                UpdateCompletionState();

                Quizzes.Clear();
                foreach (var q in Tutorial.Quizzes)
                {
                    Quizzes.Add(q);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tutorial detail: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCompletionState()
    {
        if (IsCompleted)
        {
            CompletionButtonText = "✓ Đã hoàn thành (Bấm để hủy)";
            CompletionButtonColor = Color.FromArgb("#10b981");
        }
        else
        {
            CompletionButtonText = "Đánh dấu đã hoàn thành (+20 XP)";
            CompletionButtonColor = Color.FromArgb("#512bd4");
        }
    }

    [RelayCommand]
    public async Task ToggleCompleteAsync()
    {
        if (Tutorial == null || IsBusy) return;

        if (!_apiService.IsAuthenticated)
        {
            await Shell.Current.DisplayAlertAsync("Thông báo", "Vui lòng đăng nhập để lưu tiến độ và nhận điểm XP.", "Đồng ý");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiService.ToggleCompleteTutorialAsync(Tutorial.Id);
            if (result.IsSuccess)
            {
                IsCompleted = result.IsCompleted;
                Tutorial.IsCompleted = result.IsCompleted;
                UpdateCompletionState();

                await Shell.Current.DisplayAlertAsync(
                    IsCompleted ? "Thành công" : "Thông báo",
                    result.Message,
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void SelectQuizOption(Tuple<QuizItem, string>? param)
    {
        if (param == null) return;
        var quiz = param.Item1;
        var option = param.Item2;

        quiz.SelectedOption = option;
        quiz.IsAnswered = true;
    }

    [RelayCommand]
    public async Task NavigatePrevAsync()
    {
        if (Tutorial?.PreviousTutorial != null)
        {
            await Shell.Current.GoToAsync($"tutorialdetail?slug={Tutorial.PreviousTutorial.Slug}");
        }
    }

    [RelayCommand]
    public async Task NavigateNextAsync()
    {
        if (Tutorial?.NextTutorial != null)
        {
            await Shell.Current.GoToAsync($"tutorialdetail?slug={Tutorial.NextTutorial.Slug}");
        }
    }
}
