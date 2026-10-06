using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string usernameOrEmail = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string serverUrl = string.Empty;

    [ObservableProperty]
    private bool isConfiguringServer;

    public LoginViewModel(IApiService apiService)
    {
        _apiService = apiService;
        Title = "Đăng nhập";
        ServerUrl = _apiService.BaseUrl;
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(UsernameOrEmail) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu.";
            HasError = true;
            return;
        }

        IsBusy = true;
        HasError = false;

        try
        {
            var result = await _apiService.LoginAsync(UsernameOrEmail.Trim(), Password);
            if (result.IsSuccess)
            {
                await Shell.Current.DisplayAlertAsync("Thành công", "Đăng nhập thành công! Chào mừng bạn trở lại.", "OK");
                await Shell.Current.GoToAsync("//home");
            }
            else
            {
                ErrorMessage = result.Message;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi kết nối: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ToggleServerConfig()
    {
        IsConfiguringServer = !IsConfiguringServer;
    }

    [RelayCommand]
    public async Task SaveServerUrlAsync()
    {
        if (!string.IsNullOrWhiteSpace(ServerUrl))
        {
            _apiService.BaseUrl = ServerUrl.Trim();
            IsConfiguringServer = false;
            await Shell.Current.DisplayAlertAsync("Cập nhật", "Đã lưu địa chỉ máy chủ API.", "OK");
        }
    }

    [RelayCommand]
    public async Task ContinueAsGuestAsync()
    {
        await Shell.Current.GoToAsync("//home");
    }
}
