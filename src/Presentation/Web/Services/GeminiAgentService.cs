using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

/// <summary>
/// Autonomous AI Agent powered by Google Gemini 2.0 Flash with internal Function Calling (Tools)
/// and resilient zero-downtime fallback to the local expert rule engine.
/// </summary>
public class GeminiAgentService : IAiTutorService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ICodeExecutionService _codeExecutionService;
    private readonly AppDbContext _context;
    private readonly IStreakService _streakService;
    private readonly AiTutorService _fallbackEngine;
    private readonly ILogger<GeminiAgentService> _logger;
    private readonly IAiConversationService? _conversationService;

    private readonly string? _apiKey;
    private readonly string _model;
    private readonly bool _enabled;
    private readonly double _temperature;

    public GeminiAgentService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ICodeExecutionService codeExecutionService,
        AppDbContext context,
        IStreakService streakService,
        AiTutorService fallbackEngine,
        ILogger<GeminiAgentService> logger,
        IAiConversationService? conversationService = null)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _configuration = configuration;
        _codeExecutionService = codeExecutionService;
        _context = context;
        _streakService = streakService;
        _fallbackEngine = fallbackEngine;
        _logger = logger;
        _conversationService = conversationService;

        var rawKey = (_configuration["Gemini:ApiKey"] 
            ?? _configuration["GEMINI_API_KEY"] 
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
            ?? string.Empty).Trim();
        if (rawKey.StartsWith("${") && rawKey.EndsWith("}"))
        {
            rawKey = rawKey.Substring(2, rawKey.Length - 3).Trim();
        }
        _apiKey = string.IsNullOrWhiteSpace(rawKey) ? null : rawKey;

        _model = _configuration["Gemini:Model"] ?? "gemini-3.8-flash";
        _enabled = _configuration.GetValue<bool>("Gemini:Enabled", true);
        _temperature = _configuration.GetValue<double>("Gemini:Temperature", 0.4);
    }

    public bool IsLiveGeminiAvailable => _enabled && !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AiTutorChatResponse> ChatAsync(AiTutorChatRequest request, bool isEnglish)
    {
        // 1. Save user question if conversation exists
        if (_conversationService != null && request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty)
        {
            await _conversationService.SaveMessageAsync(
                request.ConversationId.Value,
                role: "user",
                content: request.Message,
                codeSnippet: request.CodeContext);
        }

        AiTutorChatResponse response;

        if (!IsLiveGeminiAvailable)
        {
            _logger.LogInformation("Atus AI live agent key is not configured or disabled. Delegating to local rule engine.");
            response = await _fallbackEngine.ChatAsync(request, isEnglish);
        }
        else
        {
            try
            {
                response = await ExecuteAgentChatLoopAsync(request, isEnglish);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Atus AI Agent encountered an issue: {Message}. Fallback to local expert rule engine.", ex.Message);
                response = await _fallbackEngine.ChatAsync(request, isEnglish);

                string shortError = "Connection Busy";
                if (ex.Message.Contains("404")) shortError = "Model 404";
                else if (ex.Message.Contains("503")) shortError = "Server Busy 503";
                else if (ex.Message.Contains("429")) shortError = "Rate Limit 429";
                else if (ex.Message.Contains("400")) shortError = "Protocol Error 400";

                response.ResponseMarkdown = (isEnglish
                    ? $"> ⚠️ *[Offline Engine Active - AI Assistant busy ({shortError})]*\n\n"
                    : $"> ⚠️ *[Chế độ Ngoại tuyến kích hoạt - Hệ thống AI đang bận ({shortError})]*\n\n")
                    + response.ResponseMarkdown;
            }
        }

        // 2. Save model answer and auto-generate title if conversation exists
        if (_conversationService != null && request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty)
        {
            await _conversationService.SaveMessageAsync(
                request.ConversationId.Value,
                role: "model",
                content: response.ResponseMarkdown,
                codeSnippet: response.CodeSnippet,
                followUps: response.SuggestedFollowUps,
                modelUsed: response.ModelUsed ?? _model);

            await _conversationService.EnsureAutoTitleAsync(request.ConversationId.Value, request.Message, isEnglish);
            response.ConversationId = request.ConversationId.Value;
        }

        return response;
    }

    private async Task<AiTutorChatResponse> ExecuteAgentChatLoopAsync(AiTutorChatRequest request, bool isEnglish)
    {
        var modelsToTry = new List<string> { _model };
        string[] fallbacks = ["gemini-3.5-flash-lite", "gemini-3-flash-preview", "gemini-3.1-flash-lite-preview"];
        foreach (var fb in fallbacks)
        {
            if (!modelsToTry.Contains(fb, StringComparer.OrdinalIgnoreCase))
            {
                modelsToTry.Add(fb);
            }
        }

        Exception? lastEx = null;
        foreach (var currentModel in modelsToTry)
        {
            try
            {
                return await ExecuteModelChatLoopAsync(currentModel, request, isEnglish);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                _logger.LogWarning("AI model {Model} returned non-success ({Message}). Auto-recovering using resilient fallback chain.", currentModel, ex.Message);
            }
        }

        throw lastEx ?? new InvalidOperationException("All Gemini models in fallback chain were exhausted.");
    }

    private async Task<AiTutorChatResponse> ExecuteModelChatLoopAsync(string modelName, AiTutorChatRequest request, bool isEnglish)
    {
        var systemInstruction = BuildSystemPrompt(isEnglish);
        var toolsDefinition = BuildToolsDefinition();

        var contents = new JsonArray();

        // Load multi-turn history if conversation context is available
        if (_conversationService != null && request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty)
        {
            var history = await _conversationService.GetRecentMessagesForContextAsync(request.ConversationId.Value, 8);
            foreach (var msg in history)
            {
                // Ensure valid roles: 'user' or 'model'
                var role = msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
                contents.Add(new JsonObject
                {
                    ["role"] = role,
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = msg.Content }
                    }
                });
            }
        }

        // Append current turn user prompt
        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray
            {
                new JsonObject
                {
                    ["text"] = BuildUserPrompt(request)
                }
            }
        });

        const int maxToolTurns = 4;
        int currentTurn = 0;

        while (currentTurn < maxToolTurns)
        {
            currentTurn++;

            var requestBody = new JsonObject
            {
                ["system_instruction"] = new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = systemInstruction }
                    }
                },
                ["contents"] = contents.DeepClone(),
                ["tools"] = toolsDefinition.DeepClone(),
                ["generationConfig"] = new JsonObject
                {
                    ["temperature"] = _temperature,
                    ["maxOutputTokens"] = 2048
                }
            };

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent";
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                reqMsg.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);
            }
            using var response = await _httpClient.SendAsync(reqMsg);

            if (!response.IsSuccessStatusCode)
            {
                var errorSummary = $"HTTP {(int)response.StatusCode}";
                throw new HttpRequestException($"AI Service returned {errorSummary}");
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
            var candidate = jsonResult?["candidates"]?[0]?["content"];
            if (candidate == null)
            {
                throw new InvalidOperationException("Gemini returned empty candidate content.");
            }

            var parts = candidate["parts"]?.AsArray();
            if (parts == null || parts.Count == 0)
            {
                throw new InvalidOperationException("Gemini returned candidate without parts.");
            }

            // Check if model called a function
            var functionCallPart = parts.FirstOrDefault(p => p?["functionCall"] != null);
            if (functionCallPart != null)
            {
                var functionCall = functionCallPart["functionCall"]!.AsObject();
                var functionName = functionCall["name"]?.GetValue<string>() ?? string.Empty;
                var functionArgs = functionCall["args"]?.AsObject() ?? new JsonObject();

                _logger.LogInformation("Agent invoked tool: {FunctionName}", functionName);

                // Execute local tool
                var toolResult = await DispatchToolAsync(functionName, functionArgs);

                // Add assistant's call to history (preserving candidate parts and thoughtSignature)
                contents.Add(candidate.DeepClone());

                // Add tool result to history (Google Gemini v1beta requires role: "user" for functionResponse)
                contents.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["functionResponse"] = new JsonObject
                            {
                                ["name"] = functionName,
                                ["response"] = toolResult
                            }
                        }
                    }
                });

                // Continue loop to let Gemini synthesize with tool output
                continue;
            }

            // If text response received, extract final answer
            var textParts = parts
                .Where(p => p?["text"] != null)
                .Select(p => p!["text"]!.GetValue<string>())
                .ToList();

            var fullText = string.Join("\n", textParts);
            var res = ParseAgentResponse(fullText, isEnglish);
            res.ModelUsed = modelName;
            return res;
        }

        throw new InvalidOperationException("Exceeded maximum tool calling loop iterations.");
    }

    private string BuildSystemPrompt(bool isEnglish)
    {
        if (isEnglish)
        {
            return @"You are Atus, an elite Principal .NET Architect, Technical Lead, and dedicated AI Agent of the NET-Tutos platform.
Your name 'Atus' has a special, creative meaning:
- 'A' stands for 'Agent'.
- 'tus' is the suffix taken from the website name 'Tutos'.
Together, Agent + Tutos forms your unique identity: Atus!

Your mission is to guide learners to master C# 14, .NET 10, ASP.NET Core, EF Core, and Enterprise Clean Architecture.

Core Principles:
1. Identity: When asked who you are or about your name, proudly introduce yourself as Atus, and explain that 'A' represents Agent and 'tus' represents Tutos.
2. Socratic Mentorship: Guide learners to discover solutions rather than giving away entire solutions upfront. Give hints and structural patterns.
3. Verified Execution: When user asks to run code, debug an error, or test an algorithm, ALWAYS use the `execute_csharp_code` tool to verify before responding.
4. High Standards: Recommend Modern C# (records, pattern matching, span/memory, primary constructors, async best practices, CancellationToken, AsNoTracking).
5. Curriculum Integration: When discussing topics covered in .NET Tutos, use `search_curriculum` to point to specific tutorials.
6. Format: Clear Markdown, bold keywords, syntax-highlighted ```csharp code blocks.
7. Boundaries: Never mention the word 'Gemini' or external AI provider names. Do NOT include suggested follow-up questions at the end of your response.";
        }

        return @"Bạn là Atus, chuyên gia Principal .NET Architect & Technical Lead, trợ lý AI Agent thông minh độc quyền của nền tảng NET-Tutos.
Tên gọi 'Atus' của bạn mang một ý nghĩa đặc biệt và gần gũi:
- Chữ 'A' đại diện cho 'Agent'.
- Hậu tố 'tus' được lấy từ tên website 'Tutos'.
Kết hợp lại, Agent + Tutos tạo nên danh xưng: Atus!

Sứ mệnh của bạn là dẫn dắt học viên làm chủ C# 14, .NET 10, ASP.NET Core, Entity Framework Core và Clean Architecture chuẩn công nghiệp.

Nguyên tắc cốt lõi:
1. Danh tính & Ý nghĩa tên gọi: Khi học viên hỏi bạn là ai, tên gì, hoặc ý nghĩa tên của bạn, hãy tự hào giới thiệu bạn là Atus và giải thích rõ ràng rằng: chữ 'A' là Agent, còn 'tus' là hậu tố của 'Tutos'.
2. Phương pháp Socratic: Gợi mở tư duy từng bước, phân tích nguyên nhân gốc rễ và cơ chế bên dưới (Memory Allocation, Heap/Stack, ThreadPool, StateMachine) thay vì giải hộ toàn bộ bài tập.
3. Thực thi kiểm chứng: Khi học viên gửi code nhờ kiểm tra hoặc nhờ chạy thử, LUÔN LUÔN gọi Tool `execute_csharp_code` để biên dịch và kiểm tra lỗi thật trên máy chủ Roslyn trước khi phân tích.
4. Chuẩn hiện đại: Khuyến khích cú pháp C# mới (.NET 10, record, pattern matching, async/await non-blocking, CancellationToken, AsNoTracking).
5. Gắn kết giáo trình: Dùng Tool `search_curriculum` để tìm kiếm bài học trong 101 bài của hệ thống và điều hướng học viên.
6. Định dạng: Markdown đẹp mắt, in đậm từ khóa quan trọng, khối code ```csharp rõ ràng.
7. Giới hạn: Tuyệt đối không nhắc đến từ 'Gemini' hoặc tên nhà cung cấp AI. KHÔNG đưa phần gợi ý câu hỏi tiếp theo vào cuối câu trả lời.";
    }

    private static string BuildUserPrompt(AiTutorChatRequest request)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(request.Message);

        if (!string.IsNullOrWhiteSpace(request.CodeContext))
        {
            sb.AppendLine("\n```csharp");
            sb.AppendLine(request.CodeContext);
            sb.AppendLine("```");
        }

        if (!string.IsNullOrWhiteSpace(request.ErrorContext))
        {
            sb.AppendLine("\n[Compiler / Runtime Error Message]:");
            sb.AppendLine(request.ErrorContext);
        }

        return sb.ToString();
    }

    private static JsonArray BuildToolsDefinition()
    {
        return new JsonArray
        {
            new JsonObject
            {
                ["function_declarations"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "execute_csharp_code",
                        ["description"] = "Compiles and executes C# code inside the sandboxed Roslyn runner to verify correctness, compiler diagnostics, or program output. Always use this when the user asks to debug, test or run code.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["code"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "The complete C# script or method code to compile and run."
                                }
                            },
                            ["required"] = new JsonArray { "code" }
                        }
                    },
                    new JsonObject
                    {
                        ["name"] = "search_curriculum",
                        ["description"] = "Searches .NET Tutos 101 lessons and tutorials by topic to find relevant learning materials and URLs.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["query"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Search keyword (e.g. LINQ, async await, dependency injection, ef core, record, threadpool)."
                                }
                            },
                            ["required"] = new JsonArray { "query" }
                        }
                    },
                    new JsonObject
                    {
                        ["name"] = "get_challenge_details",
                        ["description"] = "Retrieves the details, title, and difficulty of a coding challenge by ID.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["challengeId"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "The unique challenge ID in the database."
                                }
                            },
                            ["required"] = new JsonArray { "challengeId" }
                        }
                    }
                }
            }
        };
    }

    private async Task<JsonObject> DispatchToolAsync(string functionName, JsonObject args)
    {
        try
        {
            switch (functionName)
            {
                case "execute_csharp_code":
                {
                    var code = args["code"]?.GetValue<string>() ?? string.Empty;
                    var execResult = await _codeExecutionService.ExecuteAsync(code, 3000);
                    return new JsonObject
                    {
                        ["isSuccess"] = execResult.IsSuccess,
                        ["output"] = execResult.Output,
                        ["error"] = execResult.Error,
                        ["executionTimeMs"] = execResult.ExecutionTimeMs,
                        ["isTimeout"] = execResult.IsTimeout
                    };
                }

                case "search_curriculum":
                {
                    var query = args["query"]?.GetValue<string>() ?? string.Empty;
                    var results = await _context.Tutorials
                        .AsNoTracking()
                        .Where(t => EF.Functions.Like(t.Title, $"%{query}%") || EF.Functions.Like(t.Summary, $"%{query}%"))
                        .Take(3)
                        .Select(t => new { t.Id, t.Title, t.Summary, t.Difficulty })
                        .ToListAsync();

                    var arr = new JsonArray();
                    foreach (var item in results)
                    {
                        arr.Add(new JsonObject
                        {
                            ["id"] = item.Id,
                            ["title"] = item.Title,
                            ["summary"] = item.Summary,
                            ["difficulty"] = item.Difficulty.ToString(),
                            ["url"] = $"/Tutorial/Detail/{item.Id}"
                        });
                    }

                    return new JsonObject
                    {
                        ["foundCount"] = results.Count,
                        ["lessons"] = arr
                    };
                }

                case "get_challenge_details":
                {
                    var challengeId = args["challengeId"]?.GetValue<int>() ?? 0;
                    var challenge = await _context.CodingChallenges
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == challengeId);

                    if (challenge == null)
                    {
                        return new JsonObject { ["error"] = $"Challenge with ID {challengeId} not found." };
                    }

                    return new JsonObject
                    {
                        ["id"] = challenge.Id,
                        ["title"] = challenge.Title,
                        ["difficulty"] = challenge.Difficulty.ToString(),
                        ["description"] = challenge.ShortDescription,
                        ["initialCode"] = challenge.InitialCode
                    };
                }

                default:
                    return new JsonObject { ["error"] = $"Unknown tool: {functionName}" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {FunctionName}: {Message}", functionName, ex.Message);
            return new JsonObject
            {
                ["error"] = ex.Message,
                ["isSuccess"] = false
            };
        }
    }

    private static AiTutorChatResponse ParseAgentResponse(string fullText, bool isEnglish)
    {
        var followUps = new List<string>();
        string cleanMarkdown = fullText;

        var markers = new[]
        {
            "### Gợi ý câu hỏi tiếp theo",
            "### Gợi ý câu hỏi",
            "### Gợi ý tiếp theo",
            "### Gợi ý",
            "**Gợi ý câu hỏi tiếp theo",
            "Gợi ý câu hỏi tiếp theo:",
            "Gợi ý câu hỏi:",
            "### Suggested Follow-ups",
            "### Suggested Follow-up",
            "### Suggested questions",
            "### Next Steps",
            "Suggested Follow-ups:",
            "Suggested follow-ups:"
        };

        foreach (var marker in markers)
        {
            int idx = cleanMarkdown.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                cleanMarkdown = cleanMarkdown.Substring(0, idx).Trim();
                break;
            }
        }

        cleanMarkdown = System.Text.RegularExpressions.Regex.Replace(cleanMarkdown, @"(\r?\n\s*[-*_]{3,}\s*)+$", "").Trim();

        // Extract code snippet if present
        string? snippet = null;
        var codeStart = cleanMarkdown.IndexOf("```csharp", StringComparison.OrdinalIgnoreCase);
        if (codeStart >= 0)
        {
            var codeEnd = cleanMarkdown.IndexOf("```", codeStart + 9, StringComparison.OrdinalIgnoreCase);
            if (codeEnd > codeStart)
            {
                snippet = cleanMarkdown.Substring(codeStart + 9, codeEnd - (codeStart + 9)).Trim();
            }
        }

        return new AiTutorChatResponse
        {
            Success = true,
            ResponseMarkdown = cleanMarkdown,
            CodeSnippet = snippet,
            SuggestedFollowUps = new List<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    // Pass-through delegation with fallback to local expert engine for specialized rubrics
    public async Task<AiExplainErrorResponse> ExplainErrorAsync(AiExplainErrorRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.ExplainErrorAsync(request, isEnglish);

        try
        {
            var prompt = isEnglish
                ? $"Analyze this C# compiler error:\nError: {request.ErrorMessage}\nCode:\n```csharp\n{request.SourceCode}\n```\nProvide: Error title, Explanation, How to fix, and Fixed code."
                : $"Phân tích lỗi biên dịch C# sau:\nLỗi: {request.ErrorMessage}\nĐoạn code:\n```csharp\n{request.SourceCode}\n```\nNêu rõ: Tên lỗi, Nguyên nhân, Cách khắc phục, và Mã code sửa lỗi.";

            var chatRes = await ChatAsync(new AiTutorChatRequest { Message = prompt, CodeContext = request.SourceCode, ErrorContext = request.ErrorMessage }, isEnglish);
            return new AiExplainErrorResponse
            {
                Success = true,
                ErrorCode = request.ErrorMessage.Contains("CS") ? request.ErrorMessage.Substring(request.ErrorMessage.IndexOf("CS"), 6) : "Diagnostic",
                ErrorTitle = isEnglish ? "Compiler Issue Diagnosed by Gemini Agent" : "Chẩn đoán bởi Gemini AI Agent",
                Explanation = chatRes.ResponseMarkdown,
                HowToFix = isEnglish ? "Review the recommended code modification below." : "Xem chi tiết hướng dẫn và đoạn mã đã khắc phục ở trên.",
                SuggestedFixCode = chatRes.CodeSnippet,
                ProTip = isEnglish ? "Always inspect variable scope and namespace imports." : "Luôn kiểm tra phạm vi biến và câu lệnh using."
            };
        }
        catch
        {
            return await _fallbackEngine.ExplainErrorAsync(request, isEnglish);
        }
    }

    public async Task<AiChallengeHintResponse> GetChallengeHintAsync(AiChallengeHintRequest request, bool isEnglish)
    {
        return await _fallbackEngine.GetChallengeHintAsync(request, isEnglish);
    }

    public async Task<AiCodeReviewResponse> ReviewCodeAsync(AiCodeReviewRequest request, bool isEnglish)
    {
        return await _fallbackEngine.ReviewCodeAsync(request, isEnglish);
    }

    public async Task<AiComplexityResponse> AnalyzeComplexityAsync(AiComplexityRequest request, bool isEnglish)
    {
        return await _fallbackEngine.AnalyzeComplexityAsync(request, isEnglish);
    }

    public async Task<AiDiagnoseTestFailureResponse> DiagnoseTestFailureAsync(AiDiagnoseTestFailureRequest request, bool isEnglish)
    {
        return await _fallbackEngine.DiagnoseTestFailureAsync(request, isEnglish);
    }

    public async Task<AiGenerateUnitTestResponse> GenerateUnitTestAsync(AiGenerateUnitTestRequest request, bool isEnglish)
    {
        return await _fallbackEngine.GenerateUnitTestAsync(request, isEnglish);
    }

    public async Task<AiInterviewEvaluationResponse> EvaluateInterviewAnswerAsync(AiInterviewEvaluationRequest request, bool isEnglish)
    {
        return await _fallbackEngine.EvaluateInterviewAnswerAsync(request, isEnglish);
    }

    public async Task<AiInterviewFollowUpResponse> AnswerFollowUpAsync(AiInterviewFollowUpRequest request, bool isEnglish)
    {
        return await _fallbackEngine.AnswerFollowUpAsync(request, isEnglish);
    }

    public async Task<AiInterviewReportResponse> GenerateInterviewReportAsync(AiInterviewReportRequest request, bool isEnglish)
    {
        return await _fallbackEngine.GenerateInterviewReportAsync(request, isEnglish);
    }

    public async Task<AiTutorStatusResponse> CheckStatusAsync()
    {
        if (!IsLiveGeminiAvailable)
        {
            return new AiTutorStatusResponse
            {
                Success = false,
                IsLiveGemini = false,
                Provider = "Google Gemini Agent (Chua cau hinh)",
                Model = _model,
                IsAvailable = false,
                LatencyMs = 0,
                Message = "Gemini API key chua duoc cau hinh hoac dang bi tat."
            };
        }

        var pingPayload = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = "Ping test: respond with single word PONG." } } }
            }
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            using var pingReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(pingPayload)
            };
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                pingReq.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);
            }
            var response = await _httpClient.SendAsync(pingReq, cts.Token);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new AiTutorStatusResponse
                {
                    Success = true,
                    IsLiveGemini = true,
                    Provider = "Atus AI Agent",
                    Model = "Atus",
                    IsAvailable = true,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Message = "Kết nối Trợ Lý AI Atus thành công và sẵn sàng phục vụ!"
                };
            }
        }
        catch { }

        // If primary model failed/timed out, try fallback model gemini-3.5-flash
        var backupStopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var backupCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var backupEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent";
            using var backupReq = new HttpRequestMessage(HttpMethod.Post, backupEndpoint)
            {
                Content = JsonContent.Create(pingPayload)
            };
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                backupReq.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);
            }
            var backupResponse = await _httpClient.SendAsync(backupReq, backupCts.Token);
            backupStopwatch.Stop();

            if (backupResponse.IsSuccessStatusCode)
            {
                return new AiTutorStatusResponse
                {
                    Success = true,
                    IsLiveGemini = true,
                    Provider = "Atus AI Agent (Dự phòng thông minh)",
                    Model = "Atus (Hạ tầng dự phòng)",
                    IsAvailable = true,
                    LatencyMs = backupStopwatch.ElapsedMilliseconds,
                    Message = "Kết nối Trợ Lý AI Atus sẵn sàng phục vụ!"
                };
            }
            else
            {
                return new AiTutorStatusResponse
                {
                    Success = false,
                    IsLiveGemini = true,
                    Provider = "Atus AI Agent",
                    Model = "Atus",
                    IsAvailable = false,
                    LatencyMs = backupStopwatch.ElapsedMilliseconds,
                    Message = $"AI service returned HTTP {(int)backupResponse.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            backupStopwatch.Stop();
            return new AiTutorStatusResponse
            {
                Success = false,
                IsLiveGemini = true,
                Provider = "Atus AI Agent",
                Model = "Atus",
                IsAvailable = false,
                Message = $"Không thể kết nối đến dịch vụ AI: {ex.Message}"
            };
        }
    }
}