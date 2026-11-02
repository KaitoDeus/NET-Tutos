using NET_Tutos.Mobile.ViewModels;

namespace NET_Tutos.Mobile.Views;

public partial class TutorialsPage : ContentPage
{
    private readonly TutorialsViewModel _viewModel;

    public TutorialsPage(TutorialsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);
        await _viewModel.LoadTutorialsCommand.ExecuteAsync(null);
    }
}
