using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private UserProfile? user;

    [ObservableProperty]
    private StreakInfo? streak;

    [ObservableProperty]
    private string greetingText = "Chào mừng bạn!";

    [ObservableProperty]
    private string streakBadgeText = "0 ngày";

    public ObservableCollection<CategoryItem> Categories { get; } = new();
    public ObservableCollection<TutorialSummary> RecentTutorials { get; } = new();

    public HomeViewModel(IApiService apiService)
    {
        _apiService = apiService;
        Title = "Trang chủ";
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // Load user profile & streak
            if (_apiService.IsAuthenticated)
            {
                User = await _apiService.GetProfileAsync();
                Streak = await _apiService.GetStreakAsync();

                if (User != null)
                {
                    GreetingText = $"Chào, {User.FullName}!";
                    StreakBadgeText = $"{User.StreakDays} ngày";
                }
            }
            else
            {
                GreetingText = "Chào mừng đến với .NET Tutos!";
                StreakBadgeText = "Khách";
            }

            // Load categories
            var categories = await _apiService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in categories.Take(6))
            {
                Categories.Add(cat);
            }

            // Load recent tutorials
            var tutorials = await _apiService.GetTutorialsAsync(pageSize: 8);
            RecentTutorials.Clear();
            foreach (var tut in tutorials)
            {
                RecentTutorials.Add(tut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading home data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenTutorialAsync(TutorialSummary? tutorial)
    {
        if (tutorial == null) return;
        await Shell.Current.GoToAsync($"tutorialdetail?slug={tutorial.Slug}");
    }

    [RelayCommand]
    public async Task OpenCategoryAsync(CategoryItem? category)
    {
        if (category == null) return;
        await Shell.Current.GoToAsync($"//tutorials?category={category.Slug}");
    }

    [RelayCommand]
    public async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("settings");
    }
}
