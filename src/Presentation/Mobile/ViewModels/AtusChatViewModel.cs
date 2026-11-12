using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NET_Tutos.Mobile.Core.Models;
using NET_Tutos.Mobile.Core.Services;
using NET_Tutos.Mobile.Models;
using NET_Tutos.Mobile.Services;

namespace NET_Tutos.Mobile.ViewModels;

/// <summary>
/// ViewModel for real-time SSE Atus AI streaming chat, language switching, and Neural TTS reply playback.
/// </summary>
public partial class AtusChatViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAudioPlaybackService _audioPlayback;
    private CancellationTokenSource? _streamCts;
    private Guid? _activeConversationId;

    public ObservableCollection<AtusChatMessageItem> Messages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendMessage))]
    private string draftMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendMessage))]
    [NotifyPropertyChangedFor(nameof(IsNotStreaming))]
    private bool isStreaming;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LanguageToggleText))]
    private bool isEnglish;

    [ObservableProperty]
    private string statusBanner = "Atus AI SSE Streaming sẵn sàng";

    [ObservableProperty]
    private string conversationTitle = "Trò chuyện cùng Atus AI";

    public bool IsNotStreaming => !IsStreaming;
    public bool CanSendMessage => !IsStreaming && !string.IsNullOrWhiteSpace(DraftMessage);
    public string LanguageToggleText => IsEnglish ? "EN" : "VI";

    public AtusChatViewModel(IApiService apiService, IAudioPlaybackService audioPlayback)
    {
        _apiService = apiService;
        _audioPlayback = audioPlayback;
        Title = "Atus AI Tutor";

        SeedWelcomeMessage();
    }

    private void SeedWelcomeMessage()
    {
        if (Messages.Count > 0) return;

        Messages.Add(new AtusChatMessageItem
        {
            Role = "model",
            Content = IsEnglish
                ? "Hi! I'm **Atus AI**. Ask me anything about C# 14, ASP.NET Core, EF Core, async/await, or interview prep — answers stream live word-by-word."
                : "Xin chào! Mình là **Atus AI**. Hãy hỏi bất kỳ chủ đề nào về C# 14, ASP.NET Core, EF Core, async/await hoặc ôn phỏng vấn — câu trả lời sẽ stream trực tiếp!"
        });
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (!_apiService.IsAuthenticated)
        {
            StatusBanner = "Chế độ Khách (streaming tức thì) • Đăng nhập để lưu lịch sử";
            return;
        }

        try
        {
            var conversations = await _apiService.GetAtusConversationsAsync(IsEnglish ? "en" : "vi");
            var latest = conversations.FirstOrDefault();
            if (latest == null)
            {
                StatusBanner = "Đã đồng bộ tài khoản • Bắt đầu hội thoại mới";
                return;
            }

            var detail = await _apiService.GetAtusConversationAsync(latest.Id);
            if (detail != null && detail.Messages.Count > 0)
            {
                _activeConversationId = detail.Id;
                ConversationTitle = string.IsNullOrWhiteSpace(detail.Title) ? "Hội thoại Atus" : detail.Title;
                Messages.Clear();
                foreach (var msg in detail.Messages)
                {
                    Messages.Add(new AtusChatMessageItem
                    {
                        Role = msg.Role,
                        Content = msg.Content
                    });
                }
                StatusBanner = $"Đã tải lịch sử đồng bộ ({detail.Messages.Count} tin nhắn)";
            }
        }
        catch
        {
            StatusBanner = "Streaming trực tiếp cùng Atus AI";
        }
    }

    [RelayCommand]
    public void ToggleLanguage()
    {
        IsEnglish = !IsEnglish;
        StatusBanner = IsEnglish
            ? "Language switched to English (en-US)"
            : "Đã chuyển ngôn ngữ sang Tiếng Việt (vi-VN)";
    }

    [RelayCommand]
    public void StartNewConversation()
    {
        _streamCts?.Cancel();
        _activeConversationId = null;
        ConversationTitle = IsEnglish ? "New Atus Session" : "Hội thoại Atus mới";
        Messages.Clear();
        SeedWelcomeMessage();
    }

    [RelayCommand]
    public async Task SendQuickPromptAsync(string prompt)
    {
        DraftMessage = prompt;
        await SendMessageAsync();
    }

    [RelayCommand]
    public async Task SendMessageAsync()
    {
        if (IsStreaming || string.IsNullOrWhiteSpace(DraftMessage))
        {
            return;
        }

        var prompt = DraftMessage.Trim();
        DraftMessage = string.Empty;

        Messages.Add(new AtusChatMessageItem
        {
            Role = "user",
            Content = prompt
        });

        var assistantBubble = new AtusChatMessageItem
        {
            Role = "model",
            Content = IsEnglish ? "Thinking..." : "Atus đang suy nghĩ...",
            IsStreaming = true
        };
        Messages.Add(assistantBubble);

        _streamCts?.Cancel();
        _streamCts?.Dispose();
        _streamCts = new CancellationTokenSource();
        var ct = _streamCts.Token;

        IsStreaming = true;
        StatusBanner = IsEnglish ? "Streaming live response..." : "Đang nhận phản hồi SSE từ Atus...";
        var accumulated = new StringBuilder();
        bool receivedChunk = false;

        try
        {
            await foreach (var evt in _apiService.StreamAtusChatAsync(
                prompt,
                _activeConversationId,
                IsEnglish ? "en" : "vi",
                ct))
            {
                if (ct.IsCancellationRequested) break;

                switch (evt.Type)
                {
                    case AtusStreamEventType.Init:
                        if (evt.ConversationId != Guid.Empty)
                        {
                            _activeConversationId = evt.ConversationId;
                        }
                        break;

                    case AtusStreamEventType.Chunk:
                        receivedChunk = true;
                        accumulated.Append(evt.Text);
                        var snapshot = accumulated.ToString();
                        MainThread.BeginInvokeOnMainThread(() => assistantBubble.Content = snapshot);
                        break;

                    case AtusStreamEventType.Done:
                        if (!string.IsNullOrWhiteSpace(evt.Title))
                        {
                            ConversationTitle = evt.Title;
                        }
                        StatusBanner = evt.Persisted
                            ? "Đã lưu vào lịch sử đám mây"
                            : "Hoàn tất streaming";
                        break;

                    case AtusStreamEventType.Error:
                        if (!receivedChunk)
                        {
                            assistantBubble.Content = evt.Message;
                        }
                        StatusBanner = evt.Message;
                        break;
                }
            }

            if (!receivedChunk && string.IsNullOrWhiteSpace(assistantBubble.Content))
            {
                assistantBubble.Content = IsEnglish
                    ? "No response received from Atus. Check server connection in Settings."
                    : "Chưa nhận được phản hồi từ Atus. Kiểm tra URL máy chủ trong Cài đặt.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusBanner = IsEnglish ? "Streaming stopped." : "Đã dừng streaming.";
        }
        catch (Exception ex)
        {
            assistantBubble.Content = IsEnglish
                ? $"Connection error: {ex.Message}"
                : $"Lỗi kết nối streaming: {ex.Message}";
            StatusBanner = "Mất kết nối máy chủ";
        }
        finally
        {
            assistantBubble.IsStreaming = false;
            IsStreaming = false;
        }
    }

    [RelayCommand]
    public void CancelStreaming()
    {
        _streamCts?.Cancel();
    }

    [RelayCommand]
    public async Task SpeakLatestReplyAsync()
    {
        var lastAssistant = Messages.LastOrDefault(m => m.IsAssistant && !string.IsNullOrWhiteSpace(m.Content));
        if (lastAssistant == null) return;

        await _audioPlayback.SpeakAsync(lastAssistant.Content, IsEnglish ? "en-US" : "vi-VN");
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        _streamCts?.Cancel();
        await _audioPlayback.StopSpeakingAsync();
        await Shell.Current.GoToAsync("..");
    }
}
