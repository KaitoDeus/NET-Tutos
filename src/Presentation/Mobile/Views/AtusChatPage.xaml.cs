using NET_Tutos.Mobile.ViewModels;

namespace NET_Tutos.Mobile.Views;

public partial class AtusChatPage : ContentPage
{
    private readonly AtusChatViewModel _viewModel;

    public AtusChatPage(AtusChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
