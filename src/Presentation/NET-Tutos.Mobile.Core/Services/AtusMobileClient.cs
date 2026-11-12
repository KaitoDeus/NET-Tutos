using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NET_Tutos.Mobile.Core.Models;

namespace NET_Tutos.Mobile.Core.Services;

/// <summary>
/// Mobile client for Atus AI streaming chat (SSE), cloud conversation history, and voice interview scoring.
/// </summary>
public sealed class AtusMobileClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public AtusMobileClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<AtusStatusInfo?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AtusStatusInfo>("atus/status", JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<AtusConversationSummary>> GetConversationsAsync(
        string language = "vi",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedLang = NormalizeLanguage(language);
            var list = await _httpClient.GetFromJsonAsync<List<AtusConversationSummary>>(
                $"atus/conversations?lang={normalizedLang}",
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
            return list ?? new List<AtusConversationSummary>();
        }
        catch
        {
            return new List<AtusConversationSummary>();
        }
    }

    public async Task<AtusConversationDetail?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AtusConversationDetail>(
                $"atus/conversations/{conversationId}",
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AtusChatResponsePayload?> ChatAsync(
        AtusChatRequestPayload request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("atus/chat", request, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AtusChatResponsePayload>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async IAsyncEnumerable<AtusStreamEvent> StreamChatAsync(
        AtusChatRequestPayload request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "atus/chat/stream")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        HttpResponseMessage? response = null;
        string? transportError = null;
        try
        {
            response = await _httpClient
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }
        catch (Exception ex)
        {
            transportError = ex.Message;
        }

        if (response == null)
        {
            yield return new AtusStreamEvent
            {
                Type = AtusStreamEventType.Error,
                Message = transportError ?? "Unable to reach Atus AI endpoint."
            };
            yield break;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                yield return new AtusStreamEvent
                {
                    Type = AtusStreamEventType.Error,
                    Message = $"HTTP {(int)response.StatusCode} from Atus streaming endpoint."
                };
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await foreach (var evt in AtusSseStreamParser.ParseAsync(stream, cancellationToken).ConfigureAwait(false))
            {
                yield return evt;
            }
        }
    }

    public async Task<VoiceEvaluationResult> EvaluateVoiceAnswerAsync(
        VoiceEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var response = await _httpClient
                .PostAsJsonAsync("atus/voice-evaluate", request, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var remote = await response.Content
                    .ReadFromJsonAsync<VoiceEvaluationResult>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                if (remote != null && remote.IsSuccess)
                {
                    return remote;
                }
            }
        }
        catch
        {
            // Fall through to deterministic offline rubric evaluation.
        }

        return VoiceTrainingEvaluator.EvaluateOffline(request);
    }

    private static string NormalizeLanguage(string? language)
        => !string.IsNullOrWhiteSpace(language) && language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "vi";
}
