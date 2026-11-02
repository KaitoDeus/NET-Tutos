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
        ILogger<GeminiAgentService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _configuration = configuration;
        _codeExecutionService = codeExecutionService;
        _context = context;
        _streakService = streakService;
        _fallbackEngine = fallbackEngine;
        _logger = logger;

        _apiKey = _configuration["Gemini:ApiKey"] ?? _configuration["GEMINI_API_KEY"];
        _model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        _enabled = _configuration.GetValue<bool>("Gemini:Enabled", true);
        _temperature = _configuration.GetValue<double>("Gemini:Temperature", 0.4);
    }

    public bool IsLiveGeminiAvailable => _enabled && !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AiTutorChatResponse> ChatAsync(AiTutorChatRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable)
        {
            _logger.LogInformation("Gemini API key is not configured or disabled. Delegating to local rule engine.");
            return await _fallbackEngine.ChatAsync(request, isEnglish);
        }

        try
        {
            return await ExecuteAgentChatLoopAsync(request, isEnglish);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini Agent failed with error: {Message}. Fallback to local expert rule engine.", ex.Message);
            var fallbackResponse = await _fallbackEngine.ChatAsync(request, isEnglish);
            fallbackResponse.ResponseMarkdown = (isEnglish
                ? "> ⚠️ *[Offline Engine Active - Live Gemini Agent unavailable: " + ex.Message + "]*\n\n"
                : "> ⚠️ *[Chế độ Offline kích hoạt - Kết nối Gemini Agent tạm ngắt: " + ex.Message + "]*\n\n")
                + fallbackResponse.ResponseMarkdown;
            return fallbackResponse;
        }
    }

    private async Task<AiTutorChatResponse> ExecuteAgentChatLoopAsync(AiTutorChatRequest request, bool isEnglish)
    {
        var systemInstruction = BuildSystemPrompt(isEnglish);
        var toolsDefinition = BuildToolsDefinition();

        var contents = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = BuildUserPrompt(request)
                    }
                }
            }
        };

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

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            using var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini API HTTP {(int)response.StatusCode}: {errorText}");
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

                _logger.LogInformation("Gemini Agent invoked tool: {FunctionName} with args: {Args}", functionName, functionArgs.ToJsonString());

                // Execute local tool
                var toolResult = await DispatchToolAsync(functionName, functionArgs);

                // Add assistant's call to history
                contents.Add(new JsonObject
                {
                    ["role"] = "model",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["functionCall"] = new JsonObject
                            {
                                ["name"] = functionName,
                                ["args"] = functionArgs.DeepClone()
                            }
                        }
                    }
                });

                // Add tool result to history
                contents.Add(new JsonObject
                {
                    ["role"] = "function",
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
            return ParseAgentResponse(fullText, isEnglish);
        }

        throw new InvalidOperationException("Exceeded maximum tool calling loop iterations.");
    }

    private string BuildSystemPrompt(bool isEnglish)
    {
        if (isEnglish)
        {
            return @"You are NET-Tutos AI Mentor, an elite Principal .NET Architect and Technical Lead.
Your mission is to guide learners to master C# 14, .NET 10, ASP.NET Core, EF Core, and Enterprise Clean Architecture.

Core Principles:
1. Socratic Mentorship: Guide learners to discover solutions rather than giving away entire solutions upfront. Give hints and structural patterns.
2. Verified Execution: When user asks to run code, debug an error, or test an algorithm, ALWAYS use the `execute_csharp_code` tool to verify before responding.
3. High Standards: Recommend Modern C# (records, pattern matching, span/memory, primary constructors, async best practices, CancellationToken, AsNoTracking).
4. Curriculum Integration: When discussing topics covered in .NET Tutos, use `search_curriculum` to point to specific tutorials.
5. Format: Clear Markdown, bold keywords, syntax-highlighted ```csharp code blocks. Always provide 2-3 suggested follow-up questions at the very end in a section named '### Suggested Follow-ups'.";
        }

        return @"Bạn là NET-Tutos AI Mentor, chuyên gia Principal .NET Architect & Technical Lead hàng đầu.
Sứ mệnh của bạn là dẫn dắt học viên làm chủ C# 14, .NET 10, ASP.NET Core, Entity Framework Core và Clean Architecture chuẩn công nghiệp.

Nguyên tắc cốt lõi:
1. Phương pháp Socratic: Gợi mở tư duy từng bước, phân tích nguyên nhân gốc rễ và cơ chế bên dưới (Memory Allocation, Heap/Stack, ThreadPool, StateMachine) thay vì giải hộ toàn bộ bài tập.
2. Thực thi kiểm chứng: Khi học viên gửi code nhờ kiểm tra hoặc nhờ chạy thử, LUÔN LUÔN gọi Tool `execute_csharp_code` để biên dịch và kiểm tra lỗi thật trên máy chủ Roslyn trước khi phân tích.
3. Chuẩn hiện đại: Khuyến khích cú pháp C# mới (.NET 10, record, pattern matching, async/await non-blocking, CancellationToken, AsNoTracking).
4. Gắn kết giáo trình: Dùng Tool `search_curriculum` để tìm kiếm bài học trong 101 bài của hệ thống và điều hướng học viên.
5. Định dạng: Markdown đẹp mắt, in đậm từ khóa quan trọng, khối code ```csharp rõ ràng. Cuối câu trả lời luôn gợi ý 2-3 câu hỏi tiếp theo trong phần '### Gợi ý câu hỏi tiếp theo'.";
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

        // Extract Suggested Follow-ups section if generated by model
        var marker = isEnglish ? "### Suggested Follow-ups" : "### Gợi ý câu hỏi tiếp theo";
        int idx = fullText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            cleanMarkdown = fullText.Substring(0, idx).Trim();
            var followUpBlock = fullText.Substring(idx + marker.Length);
            var lines = followUpBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var cleanLine = line.Trim().TrimStart('-', '*', '1', '2', '3', '.', ' ');
                if (!string.IsNullOrWhiteSpace(cleanLine) && cleanLine.Length > 5 && followUps.Count < 3)
                {
                    followUps.Add(cleanLine);
                }
            }
        }

        if (followUps.Count == 0)
        {
            followUps = isEnglish
                ? new List<string> { "Deep dive into internals", "Best practices in .NET 10", "Run unit tests for this" }
                : new List<string> { "Đào sâu cơ chế bên dưới", "Chuẩn Clean Code trong .NET 10", "Cách viết Unit Test cho đoạn này" };
        }

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
            SuggestedFollowUps = followUps,
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
}
