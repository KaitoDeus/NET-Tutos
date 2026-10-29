using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class TutorialsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string selectedCategorySlug = string.Empty;

    public ObservableCollection<CategoryItem> Categories { get; } = new();
    public ObservableCollection<TutorialSummary> Tutorials { get; } = new();

    public TutorialsViewModel(IApiService apiService)
    {
        _apiService = apiService;
        Title = "Kho Bài Học";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("category", out var cat) && cat is string catSlug)
        {
            SelectedCategorySlug = catSlug;
            LoadTutorialsCommand.Execute(null);
        }
    }

    [RelayCommand]
    public async Task LoadCategoriesAsync()
    {
        try
        {
            var cats = await _apiService.GetCategoriesAsync();
            Categories.Clear();
            var allCat = new CategoryItem { Id = 0, Name = "Tất cả", Slug = "", IsSelected = string.IsNullOrEmpty(SelectedCategorySlug) };
            Categories.Add(allCat);
            foreach (var c in cats)
            {
                c.IsSelected = (c.Slug == SelectedCategorySlug);
                Categories.Add(c);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadTutorialsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var categoryParam = string.IsNullOrEmpty(SelectedCategorySlug) ? null : SelectedCategorySlug;
            var searchParam = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();

            var list = await _apiService.GetTutorialsAsync(categoryParam, searchParam);
            Tutorials.Clear();
            foreach (var item in list)
            {
                Tutorials.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tutorials: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelectCategoryAsync(CategoryItem? category)
    {
        if (category == null) return;
        SelectedCategorySlug = category.Slug;
        foreach (var c in Categories)
        {
            c.IsSelected = (c.Slug == SelectedCategorySlug);
        }
        await LoadTutorialsAsync();
    }

    [RelayCommand]
    public async Task PerformSearchAsync()
    {
        await LoadTutorialsAsync();
    }

    [RelayCommand]
    public async Task OpenTutorialAsync(TutorialSummary? tutorial)
    {
        if (tutorial == null) return;
        await Shell.Current.GoToAsync($"tutorialdetail?slug={tutorial.Slug}");
    }
}
