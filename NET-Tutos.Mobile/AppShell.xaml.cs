using NET_Tutos.Mobile.Views;

namespace NET_Tutos.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("tutorialdetail", typeof(TutorialDetailPage));
		Routing.RegisterRoute("settings", typeof(SettingsPage));
	}
}
