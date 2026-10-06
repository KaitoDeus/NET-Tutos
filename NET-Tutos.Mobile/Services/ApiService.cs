using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NET_Tutos.Mobile.Models;

namespace NET_Tutos.Mobile.Services;

public interface IApiService
{
    bool IsAuthenticated { get; }
    string? CurrentToken { get; }
    UserProfile? CurrentUser { get; }
    string BaseUrl { get; set; }

    Task<AuthResult> LoginAsync(string usernameOrEmail, string password);
    Task<AuthResult> RegisterAsync(string username, string email, string password, string fullName);
    Task<UserProfile?> GetProfileAsync();
    Task<List<CategoryItem>> GetCategoriesAsync();
    Task<List<TutorialSummary>> GetTutorialsAsync(string? category = null, string? search = null, int page = 1, int pageSize = 50);
    Task<TutorialDetail?> GetTutorialDetailAsync(string slug);
    Task<CompleteLessonResult> ToggleCompleteTutorialAsync(int tutorialId);
    Task<List<RoadmapModule>> GetRoadmapAsync();
    Task<StreakInfo?> GetStreakAsync();
    void Logout();
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private const string TokenKey = "net_tutos_mobile_token";
    private const string BaseUrlKey = "net_tutos_mobile_base_url";
    private const string DefaultBaseUrl = "https://10.0.2.2:5001/api/mobile"; // Default for Android Emulator

    public string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlKey, "http://10.0.2.2:5000/api/mobile");
        set
        {
            Preferences.Default.Set(BaseUrlKey, value);
            _httpClient.BaseAddress = new Uri(value.EndsWith("/") ? value : value + "/");
        }
    }

    public string? CurrentToken { get; private set; }
    public UserProfile? CurrentUser { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

    public ApiService()
    {
        // Custom HttpClientHandler to bypass SSL validation for local development if needed
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        var initialUrl = BaseUrl;
        _httpClient.BaseAddress = new Uri(initialUrl.EndsWith("/") ? initialUrl : initialUrl + "/");

        // Load saved token
        CurrentToken = Preferences.Default.Get<string?>(TokenKey, null);
        if (!string.IsNullOrEmpty(CurrentToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentToken);
        }
    }

    private void SetToken(string? token)
    {
        CurrentToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            Preferences.Default.Set(TokenKey, token);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            Preferences.Default.Remove(TokenKey);
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", new
            {
                usernameOrEmail,
                password
            });

            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            if (result != null && result.IsSuccess && !string.IsNullOrEmpty(result.Token))
            {
                SetToken(result.Token);
                CurrentUser = result.User;
                return result;
            }

            return result ?? new AuthResult { IsSuccess = false, Message = "Đăng nhập thất bại." };
        }
        catch (Exception ex)
        {
            return new AuthResult { IsSuccess = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password, string fullName)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/register", new
            {
                username,
                email,
                password,
                fullName
            });

            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            if (result != null && result.IsSuccess && !string.IsNullOrEmpty(result.Token))
            {
                SetToken(result.Token);
                CurrentUser = result.User;
                return result;
            }

            return result ?? new AuthResult { IsSuccess = false, Message = "Đăng ký không thành công." };
        }
        catch (Exception ex)
        {
            return new AuthResult { IsSuccess = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<UserProfile?> GetProfileAsync()
    {
        if (!IsAuthenticated) return null;
        try
        {
            var profile = await _httpClient.GetFromJsonAsync<UserProfile>("auth/profile");
            if (profile != null)
            {
                CurrentUser = profile;
            }
            return profile;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CategoryItem>> GetCategoriesAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<CategoryItem>>("categories");
            return items ?? new List<CategoryItem>();
        }
        catch
        {
            return new List<CategoryItem>();
        }
    }

    public async Task<List<TutorialSummary>> GetTutorialsAsync(string? category = null, string? search = null, int page = 1, int pageSize = 50)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            if (page > 1) query.Add($"page={page}");
            if (pageSize != 50) query.Add($"pageSize={pageSize}");

            var url = "tutorials" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var items = await _httpClient.GetFromJsonAsync<List<TutorialSummary>>(url);
            return items ?? new List<TutorialSummary>();
        }
        catch
        {
            return new List<TutorialSummary>();
        }
    }

    public async Task<TutorialDetail?> GetTutorialDetailAsync(string slug)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TutorialDetail>($"tutorials/{Uri.EscapeDataString(slug)}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<CompleteLessonResult> ToggleCompleteTutorialAsync(int tutorialId)
    {
        if (!IsAuthenticated)
        {
            return new CompleteLessonResult { IsSuccess = false, Message = "Vui lòng đăng nhập để lưu tiến độ." };
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("tutorials/complete", new { tutorialId });
            var result = await response.Content.ReadFromJsonAsync<CompleteLessonResult>();
            if (result != null && result.IsSuccess && CurrentUser != null)
            {
                CurrentUser.TotalXp = result.TotalXp;
                CurrentUser.StreakDays = result.StreakDays;
            }
            return result ?? new CompleteLessonResult { IsSuccess = false, Message = "Không thể ghi nhận tiến độ." };
        }
        catch (Exception ex)
        {
            return new CompleteLessonResult { IsSuccess = false, Message = $"Lỗi kết nối: {ex.Message}" };
        }
    }

    public async Task<List<RoadmapModule>> GetRoadmapAsync()
    {
        try
        {
            var modules = await _httpClient.GetFromJsonAsync<List<RoadmapModule>>("roadmap");
            return modules ?? new List<RoadmapModule>();
        }
        catch
        {
            return new List<RoadmapModule>();
        }
    }

    public async Task<StreakInfo?> GetStreakAsync()
    {
        if (!IsAuthenticated) return null;
        try
        {
            return await _httpClient.GetFromJsonAsync<StreakInfo>("streak");
        }
        catch
        {
            return null;
        }
    }

    public void Logout()
    {
        SetToken(null);
        CurrentUser = null;
    }
}
