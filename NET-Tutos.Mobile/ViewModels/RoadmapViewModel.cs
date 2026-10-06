using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class RoadmapViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    public ObservableCollection<RoadmapModule> Modules { get; } = new();

    public RoadmapViewModel(IApiService apiService)
    {
        _apiService = apiService;
        Title = "Lộ Trình C# .NET";
    }

    [RelayCommand]
    public async Task LoadRoadmapAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var list = await _apiService.GetRoadmapAsync();
            Modules.Clear();
            foreach (var mod in list)
            {
                Modules.Add(mod);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading roadmap: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenTutorialAsync(TutorialSummary? tutorial)
    {
        if (tutorial == null || string.IsNullOrEmpty(tutorial.Slug)) return;
        await Shell.Current.GoToAsync($"tutorialdetail?slug={tutorial.Slug}");
    }
}
