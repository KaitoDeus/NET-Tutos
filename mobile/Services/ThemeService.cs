namespace NET_Tutos.Mobile.Services;

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    int ReaderFontSize { get; }
    event EventHandler<ThemeMode>? ThemeChanged;
    event EventHandler<int>? FontSizeChanged;

    void Initialize();
    void SetTheme(ThemeMode mode);
    void SetReaderFontSize(int size);
}

public class ThemeService : IThemeService
{
    private const string ThemePreferenceKey = "app_theme_mode_v2";
    private const string FontSizePreferenceKey = "app_reader_font_size_v2";

    public event EventHandler<ThemeMode>? ThemeChanged;
    public event EventHandler<int>? FontSizeChanged;

    public ThemeMode CurrentTheme
    {
        get
        {
            var val = Preferences.Default.Get(ThemePreferenceKey, (int)ThemeMode.System);
            return Enum.IsDefined(typeof(ThemeMode), val) ? (ThemeMode)val : ThemeMode.System;
        }
        private set
        {
            Preferences.Default.Set(ThemePreferenceKey, (int)value);
        }
    }

    public int ReaderFontSize
    {
        get => Preferences.Default.Get(FontSizePreferenceKey, 15);
        private set => Preferences.Default.Set(FontSizePreferenceKey, value);
    }

    public void Initialize()
    {
        ApplyTheme(CurrentTheme);
    }

    public void SetTheme(ThemeMode mode)
    {
        CurrentTheme = mode;
        ApplyTheme(mode);
        ThemeChanged?.Invoke(this, mode);
    }

    public void SetReaderFontSize(int size)
    {
        ReaderFontSize = size;
        FontSizeChanged?.Invoke(this, size);
    }

    private static void ApplyTheme(ThemeMode mode)
    {
        if (Application.Current == null) return;

        Application.Current.UserAppTheme = mode switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

#if ANDROID
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is MainActivity activity)
        {
            activity.UpdateSystemBars();
        }
#endif
    }
}
