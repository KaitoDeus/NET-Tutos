using Microsoft.AspNetCore.Mvc;
using NET_Tutos.BuildingBlocks.EventBus.Events;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.Services;

namespace NET_Tutos.Controllers.Api;

public sealed record IdeChatRequest(
    string Message,
    string? CodeContext = null,
    string? FileName = null,
    string? ErrorContext = null,
    string? Lang = null);

public sealed record IdeCodeRequest(
    string SourceCode,
    string? FileName = null,
    string? Context = null,
    string? Lang = null);

public sealed record IdeExplainErrorRequest(
    string ErrorMessage,
    string? SourceCode = null,
    string? FileName = null,
    string? Lang = null);

public sealed record IdeExecuteRequest(
    string Code,
    int TimeoutMs = 4000);

/// <summary>
/// RESTful API bridge dedicated to the VS Code Extension ("Atus .NET Pair Programmer").
/// Exposes stateless, CSRF-free JSON endpoints for inline code review, Big-O analysis,
/// error diagnosis, xUnit test generation, Roslyn sandbox execution, and Hybrid RAG search.
/// </summary>
[ApiController]
[Route("api/v1/ide")]
[Produces("application/json")]
public sealed class IdeAssistantController : ControllerBase
{
    private readonly IAiTutorService _aiTutorService;
    private readonly ICodeExecutionService _codeExecutionService;
    private readonly ISemanticCurriculumSearchService _semanticSearch;
    private readonly IMobileAuthService _mobileAuthService;
    private readonly IKafkaProducer? _kafkaProducer;

    public IdeAssistantController(
        IAiTutorService aiTutorService,
        ICodeExecutionService codeExecutionService,
        ISemanticCurriculumSearchService semanticSearch,
        IMobileAuthService mobileAuthService,
        IKafkaProducer? kafkaProducer = null)
    {
        _aiTutorService = aiTutorService;
        _codeExecutionService = codeExecutionService;
        _semanticSearch = semanticSearch;
        _mobileAuthService = mobileAuthService;
        _kafkaProducer = kafkaProducer;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var aiStatus = await _aiTutorService.CheckStatusAsync();
        int indexedCount = await _semanticSearch.GetIndexedKnowledgeCountAsync(ct);

        return Ok(new
        {
            success = true,
            service = "NET-Tutos.IDE.Bridge",
            agent = "Atus",
            aiStatus,
            indexedKnowledgeDocuments = indexedCount,
            authenticatedUserId = ResolveOptionalUserId(),
            serverTimestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int top = 6,
        [FromQuery] string? lang = null,
        CancellationToken ct = default)
    {
        bool isEn = ResolveIsEnglish(lang);
        int boundedTop = Math.Clamp(top, 1, 15);
        var result = await _semanticSearch.SearchKnowledgeBaseAsync(q ?? string.Empty, isEn, boundedTop, ct);
        return Ok(result);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] IdeChatRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { success = false, message = "Message is required." });
        }

        bool isEn = ResolveIsEnglish(request.Lang);
        var chatTask = _aiTutorService.ChatAsync(new AiTutorChatRequest
        {
            Message = request.Message.Trim(),
            CodeContext = request.CodeContext,
            ErrorContext = request.ErrorContext,
            AttachedFileName = request.FileName,
            CurrentPage = "vscode-extension"
        }, isEn);

        var ragTask = _semanticSearch.SearchKnowledgeBaseAsync(request.Message, isEn, topK: 3, ct);
        await Task.WhenAll(chatTask, ragTask);

        var chat = chatTask.Result;
        var rag = ragTask.Result;

        EmitIdeTelemetry("VsCodeChat", request.FileName ?? "chat", request.Message.Length > 80 ? request.Message[..80] : request.Message);

        return Ok(new
        {
            success = chat.Success,
            modelUsed = chat.ModelUsed ?? "Atus",
            responseMarkdown = chat.ResponseMarkdown,
            codeSnippet = chat.CodeSnippet,
            suggestedFollowUps = chat.SuggestedFollowUps,
            references = rag.Hits,
            timestamp = chat.Timestamp
        });
    }

    [HttpPost("review")]
    public async Task<IActionResult> Review([FromBody] IdeCodeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "SourceCode is required." });
        }

        bool isEn = ResolveIsEnglish(request.Lang);
        var reviewTask = _aiTutorService.ReviewCodeAsync(new AiCodeReviewRequest
        {
            SourceCode = request.SourceCode
        }, isEn);

        var complexityTask = _aiTutorService.AnalyzeComplexityAsync(new AiComplexityRequest
        {
            SourceCode = request.SourceCode,
            ChallengeTitle = string.IsNullOrWhiteSpace(request.FileName) ? "VS Code Active Buffer" : request.FileName
        }, isEn);

        await Task.WhenAll(reviewTask, complexityTask);

        var review = reviewTask.Result;
        var complexity = complexityTask.Result;

        EmitIdeTelemetry(
            "VsCodeReview",
            request.FileName ?? "untitled.cs",
            $"Score={review.Score}, Complexity={complexity.TimeComplexity}/{complexity.SpaceComplexity}");

        return Ok(new
        {
            success = review.Success && complexity.Success,
            fileName = request.FileName ?? "Program.cs",
            review,
            complexity
        });
    }

    [HttpPost("explain-error")]
    public async Task<IActionResult> ExplainError([FromBody] IdeExplainErrorRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ErrorMessage))
        {
            return BadRequest(new { success = false, message = "ErrorMessage is required." });
        }

        bool isEn = ResolveIsEnglish(request.Lang);
        var result = await _aiTutorService.ExplainErrorAsync(new AiExplainErrorRequest
        {
            ErrorMessage = request.ErrorMessage,
            SourceCode = request.SourceCode ?? string.Empty
        }, isEn);

        EmitIdeTelemetry("VsCodeExplainError", request.FileName ?? "diagnostic", result.ErrorCode ?? "Diagnostic");
        return Ok(result);
    }

    [HttpPost("generate-tests")]
    public async Task<IActionResult> GenerateTests([FromBody] IdeCodeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "SourceCode is required." });
        }

        bool isEn = ResolveIsEnglish(request.Lang);
        var result = await _aiTutorService.GenerateUnitTestAsync(new AiGenerateUnitTestRequest
        {
            SourceCode = request.SourceCode,
            MethodOrClassName = request.FileName
        }, isEn);

        EmitIdeTelemetry("VsCodeGenerateTests", request.FileName ?? "Tests.cs", result.TestFramework);
        return Ok(result);
    }

    [HttpPost("complexity")]
    public async Task<IActionResult> AnalyzeComplexity([FromBody] IdeCodeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { success = false, message = "SourceCode is required." });
        }

        bool isEn = ResolveIsEnglish(request.Lang);
        var result = await _aiTutorService.AnalyzeComplexityAsync(new AiComplexityRequest
        {
            SourceCode = request.SourceCode,
            ChallengeTitle = request.FileName ?? request.Context
        }, isEn);

        return Ok(result);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] IdeExecuteRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = "Source code cannot be empty."
            });
        }

        int boundedTimeout = Math.Clamp(request.TimeoutMs, 500, 8000);
        var result = await _codeExecutionService.ExecuteAsync(request.Code, boundedTimeout);
        return Ok(result);
    }

    private bool ResolveIsEnglish(string? requestedLang)
    {
        if (!string.IsNullOrWhiteSpace(requestedLang))
        {
            return requestedLang.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
        return HttpContext.IsEnglish();
    }

    private string? ResolveOptionalUserId()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var (isValid, userId) = _mobileAuthService.ValidateToken(token);
        return isValid ? userId : null;
    }

    private void EmitIdeTelemetry(string activityType, string entityId, string title)
    {
        if (_kafkaProducer == null) return;

        string userId = ResolveOptionalUserId() ?? "vscode-anonymous";
        _kafkaProducer.ProduceFireAndForget(
            KafkaTopics.UserLearningActivity,
            new UserLearningActivityEvent(
                UserId: userId,
                ActivityType: activityType,
                EntityId: entityId,
                Title: title,
                Metadata: new Dictionary<string, string>
                {
                    ["client"] = "vscode-extension"
                }));
    }
}
