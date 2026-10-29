using NET_Tutos.Mobile.ViewModels;

namespace NET_Tutos.Mobile.Views;

public partial class RoadmapPage : ContentPage
{
    private readonly RoadmapViewModel _viewModel;

    public RoadmapPage(RoadmapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadRoadmapCommand.ExecuteAsync(null);
    }
}
