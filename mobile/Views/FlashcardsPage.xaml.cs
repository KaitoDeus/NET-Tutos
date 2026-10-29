using NET_Tutos.Mobile.ViewModels;

namespace NET_Tutos.Mobile.Views;

public partial class FlashcardsPage : ContentPage
{
    private readonly FlashcardsViewModel _viewModel;

    public FlashcardsPage(FlashcardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
