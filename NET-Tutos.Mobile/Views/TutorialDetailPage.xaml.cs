using NET_Tutos.Mobile.ViewModels;

namespace NET_Tutos.Mobile.Views;

public partial class TutorialDetailPage : ContentPage
{
    public TutorialDetailPage(TutorialDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
