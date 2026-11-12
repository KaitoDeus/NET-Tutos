using NET_Tutos.Mobile.ViewModels;

namespace NET_Tutos.Mobile.Views;

public partial class VoiceTrainingPage : ContentPage
{
    private readonly VoiceTrainingViewModel _viewModel;

    public VoiceTrainingPage(VoiceTrainingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadQuestionsAsync();
    }
}
