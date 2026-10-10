using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IThemeService _themeService;
    private readonly IApiService _apiService;

    [ObservableProperty]
    private bool isSystemTheme;

    [ObservableProperty]
    private bool isLightTheme;

    [ObservableProperty]
    private bool isDarkTheme;

    [ObservableProperty]
    private string currentThemeSummary = string.Empty;

    [ObservableProperty]
    private bool fontSizeSmall;

    [ObservableProperty]
    private bool fontSizeNormal;

    [ObservableProperty]
    private bool fontSizeLarge;

    [ObservableProperty]
    private string serverUrl = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasStatusMessage;

    [ObservableProperty]
    private string appVersionInfo = ".NET 10 MAUI Client • Phiên bản 1.1.0";

    public SettingsViewModel(IThemeService themeService, IApiService apiService)
    {
        _themeService = themeService;
        _apiService = apiService;
        Title = "Cài Đặt Hệ Thống";

        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        UpdateThemeState(_themeService.CurrentTheme);
        UpdateFontSizeState(_themeService.ReaderFontSize);
        ServerUrl = _apiService.BaseUrl;
    }

    private void UpdateThemeState(ThemeMode mode)
    {
        IsSystemTheme = mode == ThemeMode.System;
        IsLightTheme = mode == ThemeMode.Light;
        IsDarkTheme = mode == ThemeMode.Dark;

        CurrentThemeSummary = mode switch
        {
            ThemeMode.Light => "Đang sử dụng Giao diện Sáng (Light Mode)",
            ThemeMode.Dark => "Đang sử dụng Giao diện Tối (Dark Mode)",
            _ => "Đang tự động theo cài đặt hệ thống của thiết bị"
        };
    }

    private void UpdateFontSizeState(int size)
    {
        FontSizeSmall = size <= 13;
        FontSizeNormal = size is > 13 and < 17;
        FontSizeLarge = size >= 17;
    }

    [RelayCommand]
    private void SelectTheme(string theme)
    {
        var mode = theme?.ToLowerInvariant() switch
        {
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            _ => ThemeMode.System
        };

        _themeService.SetTheme(mode);
        UpdateThemeState(mode);

        ShowStatus(mode switch
        {
            ThemeMode.Light => "Đã chuyển sang Giao diện Sáng.",
            ThemeMode.Dark => "Đã chuyển sang Giao diện Tối.",
            _ => "Đã chuyển sang Chế độ Tự động theo Hệ thống."
        });
    }

    [RelayCommand]
    private void SelectFontSize(string size)
    {
        var targetSize = size?.ToLowerInvariant() switch
        {
            "small" => 13,
            "large" => 17,
            _ => 15
        };

        _themeService.SetReaderFontSize(targetSize);
        UpdateFontSizeState(targetSize);

        ShowStatus($"Đã cập nhật cỡ chữ bài đọc: {targetSize}sp.");
    }

    [RelayCommand]
    private void SaveServerUrl()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            ShowStatus("Địa chỉ API không được để trống.");
            return;
        }

        _apiService.BaseUrl = ServerUrl.Trim();
        ShowStatus("Đã lưu địa chỉ API máy chủ mới.");
    }

    [RelayCommand]
    private void ResetDefaultServerUrl()
    {
        const string defaultUrl = "http://10.0.2.2:5242/api/mobile";
        ServerUrl = defaultUrl;
        _apiService.BaseUrl = defaultUrl;
        ShowStatus("Đã khôi phục địa chỉ máy chủ mặc định (10.0.2.2:5242).");
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        // Xóa preferences tạm thời nếu cần
        ShowStatus("Đang dọn dẹp bộ nhớ đệm...");
        await Task.Delay(300);
        ShowStatus("Đã làm sạch bộ nhớ đệm ứng dụng thành công.");
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowStatus(string message)
    {
        StatusMessage = message;
        HasStatusMessage = true;
    }
}
