using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NET_Tutos.Mobile.Core.Models;
using NET_Tutos.Mobile.Core.Services;
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
    Task<List<FlashcardTopicItem>> GetFlashcardTopicsAsync();
    Task<List<FlashcardItem>> GetFlashcardsAsync(string? topic = null, string? difficulty = null, int limit = 50, bool dueOnly = false);
    Task<FlashcardReviewResult> ReviewFlashcardAsync(int flashcardId, int rating);
    Task<int> GetPendingOfflineReviewCountAsync();
    Task<OfflineSyncFlushResult> SyncOfflineReviewsAsync(string language = "vi");
    IAsyncEnumerable<AtusStreamEvent> StreamAtusChatAsync(string message, Guid? conversationId = null, string language = "vi", CancellationToken cancellationToken = default);
    Task<List<AtusConversationSummary>> GetAtusConversationsAsync(string language = "vi");
    Task<AtusConversationDetail?> GetAtusConversationAsync(Guid conversationId);
    Task<VoiceEvaluationResult> EvaluateVoiceAnswerAsync(VoiceEvaluationRequest request, CancellationToken cancellationToken = default);
    Task<string?> GetTtsAudioFileAsync(string text, string language = "vi", CancellationToken cancellationToken = default);
    void Logout();
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly OfflineFlashcardStore _offlineStore;
    private readonly AtusMobileClient _atusClient;
    private readonly NeuralTtsCacheService _ttsCache;
    private const string TokenKey = "net_tutos_mobile_token";
    private const string BaseUrlKey = "net_tutos_mobile_base_url";

    public string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlKey, "http://10.0.2.2:5000/api/mobile");
        set
        {
            Preferences.Default.Set(BaseUrlKey, value);
            _httpClient.BaseAddress = MobileEndpointResolver.NormalizeMobileApiUri(value);
        }
    }

    public string? CurrentToken { get; private set; }
    public UserProfile? CurrentUser { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

    public ApiService()
    {
        // Custom HttpClientHandler to bypass SSL validation for local development if needed.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.BaseAddress = MobileEndpointResolver.NormalizeMobileApiUri(BaseUrl);

        _offlineStore = new OfflineFlashcardStore(Path.Combine(FileSystem.AppDataDirectory, "offline_flashcards"));
        _atusClient = new AtusMobileClient(_httpClient);
        _ttsCache = new NeuralTtsCacheService(
            _httpClient,
            Path.Combine(FileSystem.CacheDirectory, "neural_tts"),
            () => MobileEndpointResolver.ResolveServerRootUri(BaseUrl));

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

    public async Task<List<FlashcardTopicItem>> GetFlashcardTopicsAsync()
    {
        try
        {
            var topics = await _httpClient.GetFromJsonAsync<List<FlashcardTopicItem>>("flashcards/topics");
            return topics ?? new List<FlashcardTopicItem>();
        }
        catch
        {
            return new List<FlashcardTopicItem>();
        }
    }

    public async Task<List<FlashcardItem>> GetFlashcardsAsync(string? topic = null, string? difficulty = null, int limit = 50, bool dueOnly = false)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(topic)) query.Add($"topic={Uri.EscapeDataString(topic)}");
            if (!string.IsNullOrWhiteSpace(difficulty)) query.Add($"difficulty={Uri.EscapeDataString(difficulty)}");
            if (limit != 50) query.Add($"limit={limit}");
            if (dueOnly) query.Add("dueOnly=true");

            var url = "flashcards" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var items = await _httpClient.GetFromJsonAsync<List<FlashcardItem>>(url);
            if (items != null && items.Count > 0)
            {
                await _offlineStore.SaveDeckAsync(items.Select(MapToOfflineRecord));
                var merged = await _offlineStore.LoadDeckAsync(topic, dueOnly);
                if (merged.Count > 0)
                {
                    return merged.Select(MapToFlashcardItem).Take(limit).ToList();
                }

                return items;
            }
        }
        catch
        {
            // Offline fallback to cached spaced-repetition deck below.
        }

        var offlineDeck = await _offlineStore.LoadDeckAsync(topic, dueOnly);
        return offlineDeck.Select(MapToFlashcardItem).Take(limit).ToList();
    }

    public async Task<FlashcardReviewResult> ReviewFlashcardAsync(int flashcardId, int rating)
    {
        if (IsAuthenticated)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("flashcards/review", new
                {
                    flashcardId,
                    rating
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FlashcardReviewResult>();
                    if (result != null && result.IsSuccess)
                    {
                        if (CurrentUser != null)
                        {
                            CurrentUser.TotalXp = result.TotalXp;
                            CurrentUser.StreakDays = result.StreakDays;
                        }

                        var deck = await _offlineStore.LoadDeckAsync();
                        var card = deck.FirstOrDefault(c => c.Id == flashcardId);
                        if (card != null)
                        {
                            card.BoxLevel = result.NewBoxLevel;
                            card.IsMastered = result.IsMastered;
                            card.NextReviewDate = result.NextReviewDate ?? DateTime.UtcNow.AddDays(1);
                            card.IsDue = false;
                            await _offlineStore.SaveDeckAsync(deck);
                        }

                        return result;
                    }
                }
            }
            catch
            {
                // Fall back to queueing locally when offline.
            }
        }

        var outcome = await _offlineStore.EnqueueOfflineReviewAsync(flashcardId, rating);
        return new FlashcardReviewResult
        {
            IsSuccess = true,
            QueuedOffline = true,
            NewBoxLevel = outcome.NewBoxLevel,
            XpEarned = outcome.XpEarned,
            IsMastered = outcome.IsMastered,
            NextReviewDate = outcome.NextReviewUtc,
            Message = $"Đã lưu offline (+{outcome.XpEarned} XP dự kiến, ôn lại sau {outcome.IntervalDays} ngày)"
        };
    }

    public Task<int> GetPendingOfflineReviewCountAsync()
        => _offlineStore.GetPendingReviewCountAsync();

    public async Task<OfflineSyncFlushResult> SyncOfflineReviewsAsync(string language = "vi")
    {
        if (!IsAuthenticated)
        {
            var pending = await _offlineStore.GetPendingReviewCountAsync();
            return new OfflineSyncFlushResult
            {
                Attempted = false,
                IsSuccess = false,
                RemainingCount = pending,
                Message = pending > 0
                    ? $"Còn {pending} lượt ôn chờ đăng nhập để đồng bộ XP."
                    : "Chưa đăng nhập."
            };
        }

        var result = await _offlineStore.FlushPendingReviewsAsync(async (batchReq, ct) =>
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync("flashcards/review/batch", batchReq, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<BatchFlashcardReviewResponse>(cancellationToken: ct);
            }
            catch
            {
                return null;
            }
        }, language);

        if (result.IsSuccess && result.SyncedCount > 0 && CurrentUser != null)
        {
            CurrentUser.TotalXp = result.TotalXp;
            CurrentUser.StreakDays = result.StreakDays;
        }

        return result;
    }

    public IAsyncEnumerable<AtusStreamEvent> StreamAtusChatAsync(
        string message,
        Guid? conversationId = null,
        string language = "vi",
        CancellationToken cancellationToken = default)
        => _atusClient.StreamChatAsync(
            new AtusChatRequestPayload
            {
                ConversationId = conversationId,
                Message = message,
                Language = language,
                CurrentPage = "mobile"
            },
            cancellationToken);

    public Task<List<AtusConversationSummary>> GetAtusConversationsAsync(string language = "vi")
        => _atusClient.GetConversationsAsync(language);

    public Task<AtusConversationDetail?> GetAtusConversationAsync(Guid conversationId)
        => _atusClient.GetConversationAsync(conversationId);

    public Task<VoiceEvaluationResult> EvaluateVoiceAnswerAsync(
        VoiceEvaluationRequest request,
        CancellationToken cancellationToken = default)
        => _atusClient.EvaluateVoiceAnswerAsync(request, cancellationToken);

    public Task<string?> GetTtsAudioFileAsync(
        string text,
        string language = "vi",
        CancellationToken cancellationToken = default)
        => _ttsCache.GetOrFetchAudioFileAsync(text, language, cancellationToken);

    private static OfflineFlashcardRecord MapToOfflineRecord(FlashcardItem item) => new()
    {
        Id = item.Id,
        Slug = item.Slug,
        Topic = item.Topic,
        TopicName = item.TopicName,
        Difficulty = item.Difficulty,
        QuestionVi = item.QuestionVi,
        QuestionEn = item.QuestionEn,
        AnswerMarkdownVi = item.AnswerMarkdownVi,
        AnswerMarkdownEn = item.AnswerMarkdownEn,
        InterviewTipVi = item.InterviewTipVi,
        InterviewTipEn = item.InterviewTipEn,
        OrderIndex = item.OrderIndex,
        BoxLevel = item.BoxLevel,
        IsMastered = item.IsMastered,
        ReviewCount = item.ReviewCount,
        NextReviewDate = item.NextReviewDate,
        IsDue = item.IsDue
    };

    private static FlashcardItem MapToFlashcardItem(OfflineFlashcardRecord rec) => new()
    {
        Id = rec.Id,
        Slug = rec.Slug,
        Topic = rec.Topic,
        TopicName = rec.TopicName,
        Difficulty = rec.Difficulty,
        QuestionVi = rec.QuestionVi,
        QuestionEn = rec.QuestionEn,
        AnswerMarkdownVi = rec.AnswerMarkdownVi,
        AnswerMarkdownEn = rec.AnswerMarkdownEn,
        InterviewTipVi = rec.InterviewTipVi,
        InterviewTipEn = rec.InterviewTipEn,
        OrderIndex = rec.OrderIndex,
        BoxLevel = rec.BoxLevel,
        IsMastered = rec.IsMastered,
        ReviewCount = rec.ReviewCount,
        NextReviewDate = rec.NextReviewDate,
        IsDue = rec.IsDue
    };

    public void Logout()
    {
        SetToken(null);
        CurrentUser = null;
    }
}
