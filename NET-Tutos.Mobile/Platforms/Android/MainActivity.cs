using Android.App;
using Android.Content.PM;
using Android.OS;

namespace NET_Tutos.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0B1120"));
        Window?.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0B1120"));
    }
}
