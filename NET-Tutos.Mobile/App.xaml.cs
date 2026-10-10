using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile;

public partial class App : Application
{
	public App(IThemeService themeService)
	{
		InitializeComponent();
		themeService.Initialize();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}