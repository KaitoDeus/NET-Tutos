using Microsoft.Extensions.Logging;
using NET_Tutos.Mobile.Services;
using NET_Tutos.Mobile.ViewModels;
using NET_Tutos.Mobile.Views;

namespace NET_Tutos.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Services
		builder.Services.AddSingleton<IApiService, ApiService>();
		builder.Services.AddSingleton<IThemeService, ThemeService>();

		// ViewModels
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<TutorialsViewModel>();
		builder.Services.AddTransient<TutorialDetailViewModel>();
		builder.Services.AddTransient<RoadmapViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Views
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<TutorialsPage>();
		builder.Services.AddTransient<TutorialDetailPage>();
		builder.Services.AddTransient<RoadmapPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SettingsPage>();

		return builder.Build();
	}
}
