using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.DTOs.Mobile;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

/// <summary>
/// Mobile BFF API endpoints for Atus AI streaming chat, conversation sync, and bilingual voice interview evaluation.
/// </summary>
[ApiController]
[Route("api/mobile/atus")]
public class MobileAiTutorController : ControllerBase
{
    private readonly IAiTutorService _aiTutorService;
    private readonly IAiConversationService _conversationService;
    private readonly IMobileAuthService _mobileAuthService;

    public MobileAiTutorController(
        IAiTutorService aiTutorService,
        IAiConversationService conversationService,
        IMobileAuthService mobileAuthService)
    {
        _aiTutorService = aiTutorService;
        _conversationService = conversationService;
        _mobileAuthService = mobileAuthService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _aiTutorService.CheckStatusAsync();
        return Ok(new
        {
            status.Success,
            status.IsLiveGemini,
            status.Provider,
            status.Model,
            status.IsAvailable,
            status.LatencyMs,
            status.Message,
            MobileStreaming = true,
            TtsEndpoint = "/api/tts/speak",
            SupportedLanguages = new[] { "vi", "en" }
        });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] string? lang = "vi")
    {
        var userId = _mobileAuthService.GetBearerUserId(Request);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { isSuccess = false, message = "Vui lòng đăng nhập để đồng bộ lịch sử trò chuyện Atus AI." });
        }

        bool isEnglish = IsEnglishLanguage(lang);
        var list = await _conversationService.GetUserConversationsAsync(userId, isEnglish);
        return Ok(list);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var userId = _mobileAuthService.GetBearerUserId(Request);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { isSuccess = false, message = "Vui lòng đăng nhập để xem chi tiết hội thoại." });
        }

        var detail = await _conversationService.GetConversationAsync(id, userId);
        if (detail == null)
        {
            return NotFound(new { isSuccess = false, message = "Không tìm thấy cuộc trò chuyện." });
        }

        return Ok(detail);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] MobileAtusChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new AiTutorChatResponse
            {
                Success = false,
                ResponseMarkdown = "Vui lòng nhập câu hỏi hoặc nội dung cần Atus hỗ trợ."
            });
        }

        bool isEnglish = IsEnglishLanguage(request.Language);
        var userId = _mobileAuthService.GetBearerUserId(Request);
        Guid convId = Guid.Empty;

        if (!string.IsNullOrEmpty(userId))
        {
            convId = await EnsureConversationAsync(userId, request.ConversationId);
        }

        // GeminiAgentService.ChatAsync already persists both user/model turns and auto-titles when ConversationId is set.
        var tutorRequest = BuildTutorRequest(request, convId == Guid.Empty ? null : convId);
        var response = await _aiTutorService.ChatAsync(tutorRequest, isEnglish);
        response.ConversationId = convId;

        if (!string.IsNullOrEmpty(userId) && convId != Guid.Empty)
        {
            var conv = await _conversationService.GetConversationAsync(convId, userId);
            response.ConversationTitle = conv?.Title ?? response.ConversationTitle;
        }

        return Ok(response);
    }

    /// <summary>
    /// Streams Atus AI response chunks over Server-Sent Events (SSE) for mobile real-time bubble rendering.
    /// </summary>
    [HttpPost("chat/stream")]
    public async Task StreamChat([FromBody] MobileAtusChatRequest? request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteSseEventAsync(new { type = "error", message = "Vui lòng nhập câu hỏi cho Atus." }, cancellationToken);
            return;
        }

        bool isEnglish = IsEnglishLanguage(request.Language);
        var userId = _mobileAuthService.GetBearerUserId(Request);
        bool persisted = !string.IsNullOrEmpty(userId);
        Guid convId = Guid.Empty;

        if (persisted)
        {
            convId = await EnsureConversationAsync(userId!, request.ConversationId);
            await _conversationService.SaveMessageAsync(
                convId,
                role: "user",
                content: request.Message,
                codeSnippet: request.CodeContext);
        }

        await WriteSseEventAsync(new { type = "init", conversationId = convId, persisted }, cancellationToken);

        var tutorRequest = BuildTutorRequest(request, convId == Guid.Empty ? null : convId);
        var fullAnswerBuilder = new StringBuilder();

        try
        {
            await foreach (var chunk in _aiTutorService.StreamChatAsync(tutorRequest, isEnglish, cancellationToken))
            {
                fullAnswerBuilder.Append(chunk);
                await WriteSseEventAsync(new { type = "chunk", text = chunk }, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch
        {
            var warningChunk = isEnglish
                ? "\n\n> *[Streaming connection interrupted]*"
                : "\n\n> *[Kết nối streaming bị gián đoạn]*";
            fullAnswerBuilder.Append(warningChunk);
            await WriteSseEventAsync(new { type = "chunk", text = warningChunk }, cancellationToken);
        }

        var fullAnswer = fullAnswerBuilder.ToString();
        string title = isEnglish ? "Atus Mobile Session" : "Hội thoại Atus Mobile";

        if (persisted && !string.IsNullOrWhiteSpace(fullAnswer))
        {
            await _conversationService.SaveMessageAsync(
                convId,
                role: "model",
                content: fullAnswer,
                modelUsed: "Atus");

            await _conversationService.EnsureAutoTitleAsync(convId, request.Message, isEnglish);
            var updated = await _conversationService.GetConversationAsync(convId, userId!);
            if (!string.IsNullOrWhiteSpace(updated?.Title))
            {
                title = updated.Title;
            }
        }

        await WriteSseEventAsync(new { type = "done", conversationId = convId, title, persisted }, cancellationToken);
    }

    /// <summary>
    /// Evaluates spoken or dictated technical interview transcripts against flashcard/topic rubrics and returns a TTS-ready summary.
    /// </summary>
    [HttpPost("voice-evaluate")]
    public async Task<IActionResult> EvaluateVoiceAnswer([FromBody] MobileVoiceEvaluateRequest request)
    {
        if (request == null || (string.IsNullOrWhiteSpace(request.Question) && request.FlashcardId <= 0))
        {
            return BadRequest(new MobileVoiceEvaluateResponse
            {
                IsSuccess = false,
                Feedback = "Thiếu câu hỏi hoặc mã thẻ flashcard để chấm điểm."
            });
        }

        bool isEnglish = IsEnglishLanguage(request.Language);
        var evalReq = new AiInterviewEvaluationRequest
        {
            FlashcardId = request.FlashcardId,
            Question = request.Question,
            CandidateAnswer = request.Transcript ?? string.Empty,
            Topic = request.Topic ?? string.Empty,
            Difficulty = request.Difficulty ?? string.Empty
        };

        var result = await _aiTutorService.EvaluateInterviewAnswerAsync(evalReq, isEnglish);

        string spokenSummary = isEnglish
            ? $"Score {result.OverallScore} out of 100. {result.PerformanceRating}. {result.Feedback}"
            : $"Điểm đánh giá {result.OverallScore} trên 100. {result.PerformanceRating}. {result.Feedback}";

        if (!string.IsNullOrWhiteSpace(result.FollowUpQuestion))
        {
            spokenSummary += isEnglish
                ? $" Follow-up question: {result.FollowUpQuestion}"
                : $" Câu hỏi nối tiếp: {result.FollowUpQuestion}";
        }

        return Ok(new MobileVoiceEvaluateResponse
        {
            IsSuccess = result.Success,
            FlashcardId = result.FlashcardId,
            OverallScore = result.OverallScore,
            TechnicalAccuracyScore = result.TechnicalAccuracyScore,
            ClarityStructureScore = result.ClarityStructureScore,
            DepthPracticalScore = result.DepthPracticalScore,
            PerformanceRating = result.PerformanceRating,
            Feedback = result.Feedback,
            Strengths = result.Strengths,
            MissingPoints = result.MissingPoints,
            FollowUpQuestion = result.FollowUpQuestion,
            ModelAnswerSample = result.ModelAnswerSample,
            SpokenSummary = spokenSummary
        });
    }

    private async Task<Guid> EnsureConversationAsync(string userId, Guid? requestedConversationId)
    {
        if (requestedConversationId.HasValue && requestedConversationId.Value != Guid.Empty)
        {
            var existing = await _conversationService.GetConversationAsync(requestedConversationId.Value, userId);
            if (existing != null)
            {
                return existing.Id;
            }
        }

        var created = await _conversationService.CreateConversationAsync(userId, initialTitle: null);
        return created.Id;
    }

    private static AiTutorChatRequest BuildTutorRequest(MobileAtusChatRequest request, Guid? conversationId)
        => new()
        {
            ConversationId = conversationId,
            Message = request.Message,
            CodeContext = request.CodeContext,
            CurrentPage = string.IsNullOrWhiteSpace(request.CurrentPage) ? "mobile" : request.CurrentPage
        };

    private async Task WriteSseEventAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private bool IsEnglishLanguage(string? language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
           || (!string.IsNullOrWhiteSpace(language) && language.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
           || (string.IsNullOrWhiteSpace(language) && HttpContext.IsEnglish());
}
