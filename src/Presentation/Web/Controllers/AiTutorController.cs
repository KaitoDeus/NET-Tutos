using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;

namespace NET_Tutos.Controllers;

public class AiTutorController : Controller
{
    private readonly IAiTutorService _aiTutorService;
    private readonly IAiConversationService _conversationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppCacheService? _cacheService;
    private readonly IKafkaProducer? _kafkaProducer;

    public AiTutorController(
        IAiTutorService aiTutorService,
        IAiConversationService conversationService,
        UserManager<ApplicationUser> userManager,
        IAppCacheService? cacheService = null,
        IKafkaProducer? kafkaProducer = null)
    {
        _aiTutorService = aiTutorService;
        _conversationService = conversationService;
        _userManager = userManager;
        _cacheService = cacheService;
        _kafkaProducer = kafkaProducer;
    }

    // GET: /AiTutor or /AiTutor?id={conversationId}
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] Guid? id)
    {
        bool isEnglish = HttpContext.IsEnglish();
        bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        var viewModel = new AiTutorIndexViewModel
        {
            IsAuthenticated = isAuthenticated,
            IsLiveGeminiConfigured = true
        };

        if (isAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                viewModel.CurrentUserId = user.Id;
                viewModel.CurrentUserName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
                viewModel.CurrentUserAvatar = user.AvatarUrl;

                viewModel.Conversations = await _conversationService.GetUserConversationsAsync(user.Id, isEnglish);

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    viewModel.ActiveConversation = await _conversationService.GetConversationAsync(id.Value, user.Id);
                }
                else if (viewModel.Conversations.Count > 0)
                {
                    // Default to the most recent conversation if none specified
                    viewModel.ActiveConversation = await _conversationService.GetConversationAsync(viewModel.Conversations[0].Id, user.Id);
                }
            }
        }

        return View(viewModel);
    }

    // GET: /AiTutor/Status
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var status = await _aiTutorService.CheckStatusAsync();
        return Json(status);
    }

    // GET: /AiTutor/Conversations
    [HttpGet("/AiTutor/Conversations")]
    [HttpGet("/AiTutor/GetConversations")]
    [Authorize]
    public async Task<IActionResult> Conversations()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        bool isEnglish = HttpContext.IsEnglish();
        var list = await _conversationService.GetUserConversationsAsync(user.Id, isEnglish);
        return Json(new { success = true, conversations = list });
    }

    // GET: /AiTutor/Conversations/{id}
    [HttpGet("/AiTutor/Conversations/{id:guid}")]
    [HttpGet("/AiTutor/GetConversation/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var conversation = await _conversationService.GetConversationAsync(id, user.Id);
        if (conversation == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện này." });
        }

        return Json(new { success = true, conversation });
    }

    // POST: /AiTutor/Conversations
    [HttpPost("/AiTutor/Conversations")]
    [HttpPost("/AiTutor/CreateConversation")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateConversation([FromBody] AiCreateConversationRequest? request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var newConv = await _conversationService.CreateConversationAsync(user.Id, request?.Title);
        return Json(new { success = true, conversation = newConv });
    }

    // PUT / POST: /AiTutor/Conversations/{id}/Title
    [HttpPut("/AiTutor/Conversations/{id:guid}/Title")]
    [HttpPost("/AiTutor/Conversations/{id:guid}/Title")]
    [HttpPut("/AiTutor/UpdateTitle/{id:guid}")]
    [HttpPost("/AiTutor/UpdateTitle/{id:guid}")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] AiUpdateTitleRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { success = false, message = "Tiêu đề không được để trống." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var success = await _conversationService.UpdateTitleAsync(id, user.Id, request.Title);
        if (!success)
        {
            return NotFound(new { success = false, message = "Không thể cập nhật cuộc trò chuyện." });
        }

        return Json(new { success = true });
    }

    // PUT / POST: /AiTutor/Conversations/{id}/Pin
    [HttpPut("/AiTutor/Conversations/{id:guid}/Pin")]
    [HttpPost("/AiTutor/Conversations/{id:guid}/Pin")]
    [HttpPut("/AiTutor/TogglePin/{id:guid}")]
    [HttpPost("/AiTutor/TogglePin/{id:guid}")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TogglePin(Guid id, [FromBody] AiTogglePinRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var success = await _conversationService.TogglePinAsync(id, user.Id, request.IsPinned);
        return Json(new { success });
    }

    // DELETE / POST: /AiTutor/Conversations/{id}
    [HttpDelete("/AiTutor/Conversations/{id:guid}")]
    [HttpPost("/AiTutor/Conversations/{id:guid}/Delete")]
    [HttpDelete("/AiTutor/DeleteConversation/{id:guid}")]
    [HttpPost("/AiTutor/DeleteConversation/{id:guid}")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var success = await _conversationService.DeleteConversationAsync(id, user.Id);
        return Json(new { success });
    }

    // POST: /AiTutor/Chat
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] AiTutorChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập câu hỏi hoặc yêu cầu cho AI Tutor." });
        }

        // Authentication requirement: User must be signed in to chat with the agent
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return StatusCode(401, new
            {
                success = false,
                requireLogin = true,
                message = "Bạn cần đăng nhập tài khoản NET-Tutos để trò chuyện với AI Tutor và lưu lịch sử chatbox."
            });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { success = false, requireLogin = true, message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại." });
        }

        // Ensure conversation exists or create a new one for this user
        if (!request.ConversationId.HasValue || request.ConversationId.Value == Guid.Empty)
        {
            var newConv = await _conversationService.CreateConversationAsync(user.Id, initialTitle: null);
            request.ConversationId = newConv.Id;
        }
        else
        {
            // Verify conversation belongs to user
            var existing = await _conversationService.GetConversationAsync(request.ConversationId.Value, user.Id);
            if (existing == null)
            {
                var newConv = await _conversationService.CreateConversationAsync(user.Id, initialTitle: null);
                request.ConversationId = newConv.Id;
            }
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.ChatAsync(request, isEnglish);
        response.ConversationId = request.ConversationId.Value;

        return Json(response);
    }

    // POST: /AiTutor/ChatStream (Server-Sent Events)
    [HttpPost("/AiTutor/ChatStream")]
    [IgnoreAntiforgeryToken]
    public async Task ChatStream([FromBody] AiTutorChatRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            var errJson = JsonSerializer.Serialize(new { type = "error", message = "Vui lòng nhập câu hỏi hoặc yêu cầu cho Atus." });
            await Response.WriteAsync($"data: {errJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
            return;
        }

        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            Response.StatusCode = 401;
            var errJson = JsonSerializer.Serialize(new { type = "error", requireLogin = true, message = "Bạn cần đăng nhập tài khoản để trò chuyện cùng Atus." });
            await Response.WriteAsync($"data: {errJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
            return;
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            Response.StatusCode = 401;
            var errJson = JsonSerializer.Serialize(new { type = "error", requireLogin = true, message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại." });
            await Response.WriteAsync($"data: {errJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
            return;
        }

        if (!request.ConversationId.HasValue || request.ConversationId.Value == Guid.Empty)
        {
            var newConv = await _conversationService.CreateConversationAsync(user.Id, initialTitle: null);
            request.ConversationId = newConv.Id;
        }
        else
        {
            var existing = await _conversationService.GetConversationAsync(request.ConversationId.Value, user.Id);
            if (existing == null)
            {
                var newConv = await _conversationService.CreateConversationAsync(user.Id, initialTitle: null);
                request.ConversationId = newConv.Id;
            }
        }

        var convId = request.ConversationId.Value;
        bool isEnglish = HttpContext.IsEnglish();

        // Save user message first
        string? snippet = !string.IsNullOrWhiteSpace(request.AttachedFileName)
            ? $"[Tệp đính kèm: {request.AttachedFileName}]"
            : request.CodeContext;

        await _conversationService.SaveMessageAsync(
            convId,
            role: "user",
            content: request.Message,
            codeSnippet: snippet,
            modelUsed: null);

        var initJson = JsonSerializer.Serialize(new { type = "init", conversationId = convId });
        await Response.WriteAsync($"data: {initJson}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        var fullAnswerBuilder = new System.Text.StringBuilder();

        try
        {
            await foreach (var chunk in _aiTutorService.StreamChatAsync(request, isEnglish, cancellationToken))
            {
                fullAnswerBuilder.Append(chunk);
                var chunkJson = JsonSerializer.Serialize(new { type = "chunk", text = chunk });
                await Response.WriteAsync($"data: {chunkJson}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch
        {
            var errChunk = JsonSerializer.Serialize(new { type = "chunk", text = "\n\n> ⚠️ *[Lỗi kết nối streaming]*" });
            await Response.WriteAsync($"data: {errChunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        var fullAnswer = fullAnswerBuilder.ToString();

        if (!string.IsNullOrWhiteSpace(fullAnswer))
        {
            await _conversationService.SaveMessageAsync(
                convId,
                role: "model",
                content: fullAnswer,
                modelUsed: "Atus");

            await _conversationService.EnsureAutoTitleAsync(convId, request.Message, isEnglish);
        }

        var updatedConv = await _conversationService.GetConversationAsync(convId, user.Id);

        var doneJson = JsonSerializer.Serialize(new
        {
            type = "done",
            conversationId = convId,
            title = updatedConv?.Title ?? (isEnglish ? "New Chat" : "Đoạn chat mới")
        });
        await Response.WriteAsync($"data: {doneJson}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    // POST: /AiTutor/ExplainError
    [HttpPost]
    public async Task<IActionResult> ExplainError([FromBody] AiExplainErrorRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ErrorMessage))
        {
            return BadRequest(new { success = false, message = "Không tìm thấy thông tin lỗi để giải thích." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.ExplainErrorAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/GetHint
    [HttpPost]
    public async Task<IActionResult> GetHint([FromBody] AiChallengeHintRequest request)
    {
        if (request == null || request.ChallengeId <= 0)
        {
            return BadRequest(new { success = false, message = "Thử thách lập trình không hợp lệ." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.GetChallengeHintAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/ReviewCode
    [HttpPost]
    public async Task<IActionResult> ReviewCode([FromBody] AiCodeReviewRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "Mã nguồn không được để trống." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.ReviewCodeAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/AnalyzeComplexity
    [HttpPost]
    public async Task<IActionResult> AnalyzeComplexity([FromBody] AiComplexityRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "Mã nguồn không được để trống để phân tích Big-O." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.AnalyzeComplexityAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/DiagnoseTestFailure
    [HttpPost]
    public async Task<IActionResult> DiagnoseTestFailure([FromBody] AiDiagnoseTestFailureRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "Thiếu thông tin mã nguồn để chẩn đoán Test Case." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.DiagnoseTestFailureAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/GenerateUnitTest
    [HttpPost]
    public async Task<IActionResult> GenerateUnitTest([FromBody] AiGenerateUnitTestRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "Mã nguồn không được để trống để sinh Unit Test." });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.GenerateUnitTestAsync(request, isEnglish);
        return Json(response);
    }

    // POST: /AiTutor/CompleteCode
    [HttpPost]
    public async Task<IActionResult> CompleteCode([FromBody] AiCodeCompletionRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.PrefixCode))
        {
            return Json(new AiCodeCompletionResponse { Success = false, Completion = string.Empty });
        }

        bool isEnglish = HttpContext.IsEnglish();
        var response = await _aiTutorService.CompleteCodeAsync(request, isEnglish);
        return Json(response);
    }

    // GET: /AiTutor/Diagnostics
    [HttpGet("/AiTutor/Diagnostics")]
    public async Task<IActionResult> GetDiagnostics()
    {
        var status = await _aiTutorService.CheckStatusAsync();
        CacheTelemetrySnapshot? cacheDiag = null;
        if (_cacheService != null)
        {
            cacheDiag = await _cacheService.GetDiagnosticsAsync();
        }

        KafkaTelemetrySnapshot? kafkaDiag = null;
        if (_kafkaProducer != null)
        {
            kafkaDiag = await _kafkaProducer.GetDiagnosticsAsync();
        }

        return Json(new
        {
            success = true,
            agentName = "Atus",
            agentMeaning = "Agent + Tutos (Principal .NET Architect & Technical Lead)",
            liveAi = status,
            cacheTelemetry = cacheDiag,
            kafkaTelemetry = kafkaDiag,
            serverTimestamp = DateTime.UtcNow
        });
    }
}
