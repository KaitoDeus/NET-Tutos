using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace NET_Tutos.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        UpdateSystemBars();
    }

    protected override void OnResume()
    {
        base.OnResume();
        UpdateSystemBars();
    }

    public void UpdateSystemBars()
    {
        if (Window == null) return;

        bool isLight = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Light;
        var statusBarColor = isLight ? Android.Graphics.Color.ParseColor("#F8FAFC") : Android.Graphics.Color.ParseColor("#0B1120");
        var navBarColor = isLight ? Android.Graphics.Color.ParseColor("#FFFFFF") : Android.Graphics.Color.ParseColor("#0F172A");

        Window.SetStatusBarColor(statusBarColor);
        Window.SetNavigationBarColor(navBarColor);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            var controller = Window.InsetsController;
            if (controller != null)
            {
                if (isLight)
                {
                    controller.SetSystemBarsAppearance(
                        (int)WindowInsetsControllerAppearance.LightStatusBars,
                        (int)WindowInsetsControllerAppearance.LightStatusBars);
                }
                else
                {
                    controller.SetSystemBarsAppearance(
                        0,
                        (int)WindowInsetsControllerAppearance.LightStatusBars);
                }
            }
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            var decor = Window.DecorView;
            if (decor != null)
            {
                if (isLight)
                {
                    decor.SystemUiFlags |= SystemUiFlags.LightStatusBar;
                }
                else
                {
                    decor.SystemUiFlags &= ~SystemUiFlags.LightStatusBar;
                }
            }
        }
    }
}
