using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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

        var rawKey = (new[]
        {
            Environment.GetEnvironmentVariable("GEMINI_API_KEY"),
            _configuration["GEMINI_API_KEY"],
            _configuration["Gemini:ApiKey"]
        }.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty).Trim();
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

    public async IAsyncEnumerable<string> StreamChatAsync(
        AiTutorChatRequest request,
        bool isEnglish,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsLiveGeminiAvailable)
        {
            await foreach (var chunk in _fallbackEngine.StreamChatAsync(request, isEnglish, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        var modelsToTry = new List<string> { _model };
        string[] fallbacks = ["gemini-3.5-flash-lite", "gemini-3-flash-preview", "gemini-3.1-flash-lite-preview"];
        foreach (var fb in fallbacks)
        {
            if (!modelsToTry.Contains(fb, StringComparer.OrdinalIgnoreCase))
            {
                modelsToTry.Add(fb);
            }
        }

        var systemInstruction = BuildSystemPrompt(isEnglish);
        var contents = new JsonArray();

        // Load multi-turn history
        if (_conversationService != null && request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty)
        {
            var history = await _conversationService.GetRecentMessagesForContextAsync(request.ConversationId.Value, 8);
            foreach (var msg in history)
            {
                var role = msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
                contents.Add(new JsonObject
                {
                    ["role"] = role,
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = msg.Content } }
                });
            }
        }

        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray { new JsonObject { ["text"] = BuildUserPrompt(request) } }
        });

        var requestBody = new JsonObject
        {
            ["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray { new JsonObject { ["text"] = systemInstruction } }
            },
            ["contents"] = contents,
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = _temperature,
                ["maxOutputTokens"] = 2048
            }
        };

        bool streamSucceeded = false;

        foreach (var currentModel in modelsToTry)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:streamGenerateContent?alt=sse";
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                reqMsg.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);
            }

            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Stream connection failed for {Model}: {Msg}", currentModel, ex.Message);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                _logger.LogWarning("Model {Model} returned {Status} for stream. Trying fallback.", currentModel, response.StatusCode);
                continue;
            }

            using (response)
            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("data: "))
                    {
                        var jsonStr = line.Substring(6).Trim();
                        if (string.IsNullOrWhiteSpace(jsonStr)) continue;

                        string? chunkText = null;
                        try
                        {
                            var parsed = JsonNode.Parse(jsonStr);
                            chunkText = parsed?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(chunkText))
                        {
                            streamSucceeded = true;
                            yield return chunkText;
                        }
                    }
                }
            }

            if (streamSucceeded) yield break;
        }

        if (!streamSucceeded)
        {
            yield return isEnglish
                ? "> ⚠️ *[Offline Engine Active - AI Assistant busy]*\n\n"
                : "> ⚠️ *[Chế độ Ngoại tuyến kích hoạt - Hệ thống AI đang bận]*\n\n";

            await foreach (var chunk in _fallbackEngine.StreamChatAsync(request, isEnglish, cancellationToken))
            {
                yield return chunk;
            }
        }
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

        if (!string.IsNullOrWhiteSpace(request.AttachedFileName) && !string.IsNullOrWhiteSpace(request.AttachedFileContent))
        {
            sb.AppendLine($"\n[Đính kèm tệp: {request.AttachedFileName}]:");
            sb.AppendLine("```");
            sb.AppendLine(request.AttachedFileContent);
            sb.AppendLine("```");
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

        private async Task<string?> CallGeminiJsonAsync(string systemInstruction, string userPrompt, CancellationToken cancellationToken = default)
    {
        if (!IsLiveGeminiAvailable) return null;

        var modelsToTry = new List<string> { _model };
        string[] fallbacks = ["gemini-3.5-flash-lite", "gemini-3-flash-preview", "gemini-3.1-flash-lite-preview"];
        foreach (var fb in fallbacks)
        {
            if (!modelsToTry.Contains(fb, StringComparer.OrdinalIgnoreCase))
            {
                modelsToTry.Add(fb);
            }
        }

        var requestBody = new JsonObject
        {
            ["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray { new JsonObject { ["text"] = systemInstruction } }
            },
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = userPrompt } }
                }
            },
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = 0.2,
                ["response_mime_type"] = "application/json"
            }
        };

        foreach (var currentModel in modelsToTry)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent";
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                reqMsg.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(15));
                var response = await _httpClient.SendAsync(reqMsg, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CallGeminiJsonAsync for {Model} returned {Status}", currentModel, response.StatusCode);
                    continue;
                }

                var jsonStr = await response.Content.ReadAsStringAsync(cancellationToken);
                var parsed = JsonNode.Parse(jsonStr);
                var rawText = parsed?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(rawText))
                {
                    return rawText.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("CallGeminiJsonAsync exception for {Model}: {Msg}", currentModel, ex.Message);
            }
        }

        return null;
    }

    public async Task<AiChallengeHintResponse> GetChallengeHintAsync(AiChallengeHintRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.GetChallengeHintAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal C# Mentor. Provide a progressive hint for coding challenge.
Hint Level 1: Core algorithmic concept without giving away code.
Hint Level 2: Structural architecture and data structure recommendation.
Hint Level 3: Step-by-step pseudocode logic.
Return a valid JSON object:
{
  ""hintLevel"": 1,
  ""title"": ""Hint Title"",
  ""content"": ""Explanation encouraging independent discovery."",
  ""codeClue"": ""// Optional 1-line syntax clue"",
  ""hasNextLevel"": true
}"
                : @"Báº¡n lÃ  Atus, Principal .NET Mentor. Cung cáº¥p gá»£i Ã½ theo cáº¥p Ä‘á»™ cho bÃ i táº­p láº­p trÃ¬nh:
Cáº¥p Ä‘á»™ 1: KhÃ¡i niá»‡m thuáº­t toÃ¡n cá»‘t lÃµi.
Cáº¥p Ä‘á»™ 2: Cáº¥u trÃºc dá»¯ liá»‡u vÃ  hÆ°á»›ng tiáº¿p cáº­n tá»‘i Æ°u.
Cáº¥p Ä‘á»™ 3: MÃ£ giáº£ hoáº·c logic tá»«ng bÆ°á»›c.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡:
{
  ""hintLevel"": 1,
  ""title"": ""TiÃªu Ä‘á» gá»£i Ã½"",
  ""content"": ""Ná»™i dung gá»£i Ã½ mang tÃ­nh khÆ¡i gá»£i tÆ° duy Socratic."",
  ""codeClue"": ""// Gá»£i Ã½ cÃº phÃ¡p ngáº¯n náº¿u cáº§n"",
  ""hasNextLevel"": true
}";

            var userPrompt = $"[Challenge ID]: {request.ChallengeId}\n[Requested Level]: {request.HintLevel}\n[Candidate Code]:\n```csharp\n{request.CurrentCode}\n```";
            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiChallengeHintResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live challenge hint generation failed. Falling back.");
        }

        return await _fallbackEngine.GetChallengeHintAsync(request, isEnglish);
    }

    public async Task<AiCodeReviewResponse> ReviewCodeAsync(AiCodeReviewRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.ReviewCodeAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal C# & .NET Code Reviewer.
Analyze C# source code for Clean Code, Modern C# (.NET 10), Performance, and Security.
Return a valid JSON object matching this schema:
{
  ""score"": 85,
  ""summary"": ""High-level code review summary."",
  ""points"": [
    {
      ""category"": ""Clean Code"",
      ""severity"": ""warning"",
      ""title"": ""Point Title"",
      ""description"": ""Explanation and suggestion.""
    }
  ],
  ""refactoredCode"": ""Refactored modern C# code snippet""
}"
                : @"Báº¡n lÃ  Atus, ChuyÃªn gia Principal C# & .NET Code Reviewer cá»§a NET-Tutos.
PhÃ¢n tÃ­ch mÃ£ nguá»“n C# theo tiÃªu chuáº©n Clean Code, Modern C# (.NET 10), Hiá»‡u nÄƒng vÃ  Báº£o máº­t.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡:
{
  ""score"": 85,
  ""summary"": ""TÃ³m táº¯t tá»•ng quan vá» cháº¥t lÆ°á»£ng mÃ£ nguá»“n."",
  ""points"": [
    {
      ""category"": ""Clean Code"",
      ""severity"": ""warning"",
      ""title"": ""TiÃªu Ä‘á» nháº­n xÃ©t"",
      ""description"": ""Giáº£i thÃ­ch chi tiáº¿t vÃ  Ä‘á» xuáº¥t tá»‘i Æ°u.""
    }
  ],
  ""refactoredCode"": ""Äoáº¡n mÃ£ C# Ä‘Ã£ Ä‘Æ°á»£c tÃ¡i cáº¥u trÃºc sáº¡ch Ä‘áº¹p""
}";

            var userPrompt = $"```csharp\n{request.SourceCode}\n```";
            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiCodeReviewResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live code review failed. Falling back.");
        }

        return await _fallbackEngine.ReviewCodeAsync(request, isEnglish);
    }

    public async Task<AiComplexityResponse> AnalyzeComplexityAsync(AiComplexityRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.AnalyzeComplexityAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal Algorithm & Data Structures Specialist.
Analyze the Big-O Time and Space Complexity of the C# code.
Return a valid JSON object matching this schema:
{
  ""timeComplexity"": ""O(N log N)"",
  ""spaceComplexity"": ""O(1)"",
  ""efficiencyScore"": 88,
  ""summary"": ""Concise asymptotic overview."",
  ""detailedExplanation"": ""Detailed mathematical/loop step breakdown."",
  ""optimizationTip"": ""Specific advice to optimize further."",
  ""optimalComparison"": ""Comparison with the theoretical optimal approach.""
}"
                : @"Báº¡n lÃ  Atus, ChuyÃªn gia Cáº¥u trÃºc dá»¯ liá»‡u & Giáº£i thuáº­t cá»§a NET-Tutos.
PhÃ¢n tÃ­ch Ä‘á»™ phá»©c táº¡p thuáº­t toÃ¡n Big-O (Thá»i gian & KhÃ´ng gian bá»™ nhá»›) cá»§a Ä‘oáº¡n mÃ£ C#.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡:
{
  ""timeComplexity"": ""O(N log N)"",
  ""spaceComplexity"": ""O(1)"",
  ""efficiencyScore"": 88,
  ""summary"": ""TÃ³m táº¯t Ä‘á»™ phá»©c táº¡p thuáº­t toÃ¡n."",
  ""detailedExplanation"": ""Giáº£i thÃ­ch chi tiáº¿t cÃ¡c vÃ²ng láº·p, Ä‘á»‡ quy hoáº·c phÃ¢n bá»• bá»™ nhá»›."",
  ""optimizationTip"": ""Gá»£i Ã½ cá»¥ thá»ƒ Ä‘á»ƒ tá»‘i Æ°u thÃªm náº¿u cÃ³."",
  ""optimalComparison"": ""So sÃ¡nh vá»›i Ä‘á»™ phá»©c táº¡p tá»‘i Æ°u lÃ½ thuyáº¿t cá»§a bÃ i toÃ¡n.""
}";

            var userPrompt = $"[Challenge]: {request.ChallengeTitle ?? "C# Algorithm"}\n```csharp\n{request.SourceCode}\n```";
            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiComplexityResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live complexity analysis failed. Falling back.");
        }

        return await _fallbackEngine.AnalyzeComplexityAsync(request, isEnglish);
    }

    public async Task<AiDiagnoseTestFailureResponse> DiagnoseTestFailureAsync(AiDiagnoseTestFailureRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.DiagnoseTestFailureAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Automated Diagnostic Engine for C# coding tests.
Analyze why the candidate's test case failed and provide pedagogical guidance.
Return a valid JSON object:
{
  ""failureCategory"": ""Logic Flaw / Boundary Condition / Overflow"",
  ""diagnosticTitle"": ""Diagnostic Summary"",
  ""analysis"": ""Root cause analysis."",
  ""likelyFaultyCodeSegment"": ""Faulty statement or loop"",
  ""guidanceHint"": ""Socratic hint to fix."",
  ""suggestedFixSnippet"": ""Correct snippet""
}"
                : @"Báº¡n lÃ  Atus, Cháº©n Ä‘oÃ¡n Kiá»ƒm thá»­ Tá»± Ä‘á»™ng cho bÃ i táº­p C# cá»§a NET-Tutos.
PhÃ¢n tÃ­ch nguyÃªn nhÃ¢n test case tháº¥t báº¡i vÃ  Ä‘Æ°a ra gá»£i Ã½ sÆ° pháº¡m.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡:
{
  ""failureCategory"": ""Lá»—i logic / Äiá»u kiá»‡n biÃªn / TrÃ n sá»‘ / Sai kiá»ƒu"",
  ""diagnosticTitle"": ""TiÃªu Ä‘á» cháº©n Ä‘oÃ¡n"",
  ""analysis"": ""PhÃ¢n tÃ­ch nguyÃªn nhÃ¢n gá»‘c rá»…."",
  ""likelyFaultyCodeSegment"": ""DÃ²ng lá»‡nh hoáº·c vÃ²ng láº·p nghi váº¥n"",
  ""guidanceHint"": ""Gá»£i Ã½ kháº¯c phá»¥c khÆ¡i má»Ÿ tÆ° duy."",
  ""suggestedFixSnippet"": ""Äoáº¡n mÃ£ kháº¯c phá»¥c máº«u""
}";

            var userPrompt = $"[Challenge]: {request.ChallengeTitle}\n[Input]: {request.InputParameters}\n[Expected]: {request.ExpectedOutput}\n[Actual]: {request.ActualOutput}\n```csharp\n{request.SourceCode}\n```";
            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiDiagnoseTestFailureResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live test failure diagnosis failed. Falling back.");
        }

        return await _fallbackEngine.DiagnoseTestFailureAsync(request, isEnglish);
    }

    public async Task<AiGenerateUnitTestResponse> GenerateUnitTestAsync(AiGenerateUnitTestRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.GenerateUnitTestAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal Software Engineer in Test.
Generate high-quality xUnit unit tests using AAA (Arrange-Act-Assert) pattern and FluentAssertions for the given C# code.
Return a valid JSON object matching this schema:
{
  ""testFramework"": ""xUnit & FluentAssertions"",
  ""testCode"": ""// Complete test class with AAA pattern"",
  ""explanation"": ""Explanation of test scenarios covered."",
  ""coveredScenarios"": [""Happy path"", ""Null reference guard"", ""Boundary condition""]
}"
                : @"Báº¡n lÃ  Atus, ChuyÃªn gia Kiá»ƒm thá»­ Tá»± Ä‘á»™ng & Software Engineer in Test cá»§a NET-Tutos.
Sinh mÃ£ Unit Test xUnit cháº¥t lÆ°á»£ng cao theo chuáº©n AAA Pattern (Arrange - Act - Assert) káº¿t há»£p FluentAssertions cho Ä‘oáº¡n mÃ£ C#.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡:
{
  ""testFramework"": ""xUnit & FluentAssertions"",
  ""testCode"": ""// Lá»›p kiá»ƒm thá»­ hoÃ n chá»‰nh"",
  ""explanation"": ""Giáº£i thÃ­ch cÃ¡c ká»‹ch báº£n kiá»ƒm thá»­ Ä‘Ã£ bao phá»§."",
  ""coveredScenarios"": [""TrÆ°á»ng há»£p thÃ´ng thÆ°á»ng"", ""Kiá»ƒm tra tham sá»‘ null"", ""Äiá»u kiá»‡n biÃªn""]
}";

            var userPrompt = $"Target: {request.MethodOrClassName ?? "Method"}\n```csharp\n{request.SourceCode}\n```";
            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiGenerateUnitTestResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live unit test generation failed. Falling back.");
        }

        return await _fallbackEngine.GenerateUnitTestAsync(request, isEnglish);
    }

    public async Task<AiInterviewEvaluationResponse> EvaluateInterviewAnswerAsync(AiInterviewEvaluationRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.EvaluateInterviewAnswerAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal .NET Technical Architect and Lead Interviewer.
Evaluate the candidate's technical interview answer objectively and critically.
Return a valid JSON object matching this exact schema:
{
  ""overallScore"": 85,
  ""technicalAccuracyScore"": 85,
  ""clarityStructureScore"": 80,
  ""depthPracticalScore"": 75,
  ""performanceRating"": ""Senior Competency / Solid Mid-Level / Junior"",
  ""feedback"": ""Direct feedback to candidate explaining strengths and gaps."",
  ""strengths"": [""Point 1"", ""Point 2""],
  ""missingPoints"": [""Point 1"", ""Point 2""],
  ""followUpQuestion"": ""A sharp Socratic follow-up question challenging edge cases, concurrency, or memory leaks."",
  ""modelAnswerSample"": ""### Benchmark Senior Answer\nConcise practical response with modern C# concepts.""
}"
                : @"Báº¡n lÃ  Atus, Principal .NET Architect & Technical Lead Interviewer Ä‘á»™c quyá»n cá»§a NET-Tutos.
ÄÃ¡nh giÃ¡ cÃ¢u tráº£ lá»i phá»ng váº¥n ká»¹ thuáº­t cá»§a há»c viÃªn má»™t cÃ¡ch khÃ¡ch quan, sáº¯c bÃ©n vÃ  thá»±c chiáº¿n.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡ theo Ä‘Ãºng cáº¥u trÃºc sau:
{
  ""overallScore"": 85,
  ""technicalAccuracyScore"": 85,
  ""clarityStructureScore"": 80,
  ""depthPracticalScore"": 75,
  ""performanceRating"": ""TrÃ¬nh Ä‘á»™ Senior / Middle Vá»¯ng / Junior Tiá»m nÄƒng"",
  ""feedback"": ""Lá»i nháº­n xÃ©t ngáº¯n gá»n, trá»±c diá»‡n chá»‰ rÃµ Æ°u Ä‘iá»ƒm vÃ  Ä‘iá»ƒm cáº§n cáº£i thiá»‡n."",
  ""strengths"": [""Æ¯u Ä‘iá»ƒm 1"", ""Æ¯u Ä‘iá»ƒm 2""],
  ""missingPoints"": [""Äiá»ƒm thiáº¿u sÃ³t 1"", ""Äiá»ƒm thiáº¿u sÃ³t 2""],
  ""followUpQuestion"": ""Má»™t cÃ¢u há»i xoÃ¡y pháº£n biá»‡n sáº¯c bÃ©n vá» tÃ¬nh huá»‘ng biÃªn, Ä‘a luá»“ng hoáº·c rÃ² rá»‰ bá»™ nhá»›."",
  ""modelAnswerSample"": ""### ÄÃ¡p Ã¡n Máº«u Chuáº©n Senior\nNgáº¯n gá»n, chuyÃªn sÃ¢u, Ä‘Ãºng trá»ng tÃ¢m.""
}";

            var userPrompt = $"[Question]: {request.Question}\n[Topic]: {request.Topic}\n[Difficulty]: {request.Difficulty}\n[Candidate Answer]:\n{request.CandidateAnswer}";

            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiInterviewEvaluationResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    result.FlashcardId = request.FlashcardId;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live interview evaluation failed. Falling back to rule engine.");
        }

        return await _fallbackEngine.EvaluateInterviewAnswerAsync(request, isEnglish);
    }

    public async Task<AiInterviewFollowUpResponse> AnswerFollowUpAsync(AiInterviewFollowUpRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.AnswerFollowUpAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal .NET Architect.
Evaluate the candidate's answer to your follow-up technical question.
Return a valid JSON object matching this schema:
{
  ""isSatisfactory"": true,
  ""feedback"": ""Concise assessment of their follow-up answer."",
  ""bonusScore"": 10,
  ""deepDiveInsight"": ""Senior insight on real-world production mitigation.""
}"
                : @"Báº¡n lÃ  Atus, Principal .NET Architect.
ÄÃ¡nh giÃ¡ cÃ¢u tráº£ lá»i cá»§a á»©ng viÃªn cho cÃ¢u há»i xoÃ¡y ká»¹ thuáº­t.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡ theo Ä‘Ãºng cáº¥u trÃºc:
{
  ""isSatisfactory"": true,
  ""feedback"": ""Nháº­n xÃ©t ngáº¯n gá»n vá» cÃ¢u tráº£ lá»i cho cÃ¢u há»i pháº£n biá»‡n."",
  ""bonusScore"": 10,
  ""deepDiveInsight"": ""GÃ³c nhÃ¬n chuyÃªn sÃ¢u tá»« kinh nghiá»‡m thá»±c chiáº¿n production.""
}";

            var userPrompt = $"[Original Question]: {request.OriginalQuestion}\n[Follow-up Question]: {request.FollowUpQuestion}\n[Candidate Response]:\n{request.CandidateAnswer}";

            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiInterviewFollowUpResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live follow-up evaluation failed. Falling back.");
        }

        return await _fallbackEngine.AnswerFollowUpAsync(request, isEnglish);
    }

    public async Task<AiInterviewReportResponse> GenerateInterviewReportAsync(AiInterviewReportRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.GenerateInterviewReportAsync(request, isEnglish);

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal .NET Architect and Hiring Lead.
Synthesize the overall mock interview report across all answered questions.
Return a valid JSON object matching this exact schema:
{
  ""averageScore"": 80,
  ""readinessLevel"": ""Ready for Mid/Senior .NET Engineer"",
  ""readinessBadgeClass"": ""success"",
  ""executiveAssessment"": ""Comprehensive paragraph summarizing technical maturity and practical capabilities."",
  ""skillBreakdowns"": [
    { ""skillName"": ""C# Internals & Memory Architecture"", ""score"": 85, ""level"": ""Senior"" },
    { ""skillName"": ""Asynchronous Concurrency & ThreadPool"", ""score"": 80, ""level"": ""Mid"" },
    { ""skillName"": ""Entity Framework Core & Query Optimization"", ""score"": 78, ""level"": ""Mid"" },
    { ""skillName"": ""ASP.NET Core & Web API Architecture"", ""score"": 82, ""level"": ""Solid"" },
    { ""skillName"": ""Clean Architecture & SOLID Design"", ""score"": 80, ""level"": ""Proficient"" }
  ],
  ""recommendedLessons"": [
    { ""title"": ""Lesson Title"", ""slug"": ""lesson-slug"", ""reason"": ""Why this strengthens candidate profile."" }
  ],
  ""xpAwarded"": 75
}"
                : @"Báº¡n lÃ  Atus, Principal .NET Architect & Technical Lead.
Tá»•ng káº¿t bÃ¡o cÃ¡o nÄƒng lá»±c phá»ng váº¥n toÃ n diá»‡n cho há»c viÃªn dá»±a trÃªn cÃ¡c cÃ¢u há»i Ä‘Ã£ tráº£ lá»i.
Tráº£ vá» Ä‘á»‹nh dáº¡ng JSON há»£p lá»‡ theo Ä‘Ãºng cáº¥u trÃºc:
{
  ""averageScore"": 80,
  ""readinessLevel"": ""Sáºµn sÃ ng Phá»ng váº¥n Senior .NET Developer / Middle Vá»¯ng"",
  ""readinessBadgeClass"": ""success"",
  ""executiveAssessment"": ""Äoáº¡n vÄƒn nháº­n Ä‘á»‹nh tá»•ng quan vá» báº£n lÄ©nh ká»¹ thuáº­t, tÆ° duy kiáº¿n trÃºc vÃ  má»©c Ä‘á»™ sáºµn sÃ ng thá»±c táº¿."",
  ""skillBreakdowns"": [
    { ""skillName"": ""Báº£n cháº¥t C# & Quáº£n lÃ½ Bá»™ nhá»›"", ""score"": 85, ""level"": ""Senior"" },
    { ""skillName"": ""Láº­p trÃ¬nh Báº¥t Ä‘á»“ng bá»™ & Xá»­ lÃ½ Äá»“ng thá»i"", ""score"": 80, ""level"": ""Mid"" },
    { ""skillName"": ""Entity Framework Core & Tá»‘i Æ°u Truy váº¥n"", ""score"": 78, ""level"": ""Mid"" },
    { ""skillName"": ""Kiáº¿n trÃºc ASP.NET Core & Web API"", ""score"": 82, ""level"": ""Solid"" },
    { ""skillName"": ""Clean Architecture & Máº«u Thiáº¿t káº¿ SOLID"", ""score"": 80, ""level"": ""Proficient"" }
  ],
  ""recommendedLessons"": [
    { ""title"": ""TÃªn bÃ i há»c"", ""slug"": ""slug-bai-hoc"", ""reason"": ""LÃ½ do cáº§n Ã´n táº­p thÃªm."" }
  ],
  ""xpAwarded"": 75
}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Total Questions Answered: {request.Items.Count}");
            sb.AppendLine($"Time Spent: {request.TimeSpentSeconds} seconds\n");
            foreach (var item in request.Items)
            {
                sb.AppendLine($"[Topic]: {item.Topic} | Score: {item.Score}/100");
                sb.AppendLine($"[Q]: {item.Question}");
                sb.AppendLine($"[A]: {item.CandidateAnswer}\n");
            }

            var jsonText = await CallGeminiJsonAsync(systemPrompt, sb.ToString());
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiInterviewReportResponse>(jsonText, options);
                if (result != null)
                {
                    result.Success = true;
                    result.TimeSpentSeconds = request.TimeSpentSeconds;
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live interview report generation failed. Falling back.");
        }

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

    public async Task<AiCodeCompletionResponse> CompleteCodeAsync(AiCodeCompletionRequest request, bool isEnglish)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PrefixCode))
        {
            return new AiCodeCompletionResponse { Success = false, Completion = string.Empty };
        }

        if (!IsLiveGeminiAvailable)
        {
            return await _fallbackEngine.CompleteCodeAsync(request, isEnglish);
        }

        try
        {
            var systemPrompt = @"You are an expert AI code completion copilot for C# and .NET 10.
Given the preceding C# code (and optional following code), generate ONLY the immediate continuation characters or lines to be inserted at the cursor position.
CRITICAL RULES:
- Output ONLY the raw code characters to be inserted directly at cursor position.
- DO NOT wrap the code in markdown code blocks (NO ```csharp, NO ```).
- DO NOT include explanations, conversational filler, or commentary.
- DO NOT repeat what the user already typed in the prefix.
- Provide clean, modern, type-safe C# syntax (1 to 5 lines maximum).";

            var userContent = $"[PRECEDING_CODE]\n{request.PrefixCode}\n[CURSOR]\n{(string.IsNullOrEmpty(request.SuffixCode) ? "" : $"[FOLLOWING_CODE]\n{request.SuffixCode}")}";

            var requestBody = new JsonObject
            {
                ["system_instruction"] = new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = systemPrompt }
                    }
                },
                ["contents"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JsonArray
                        {
                            new JsonObject { ["text"] = userContent }
                        }
                    }
                },
                ["generationConfig"] = new JsonObject
                {
                    ["temperature"] = 0.2,
                    ["maxOutputTokens"] = 120,
                    ["stopSequences"] = new JsonArray { "\n\n\n", "```" }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3.5));
            var httpRes = await _httpClient.PostAsJsonAsync(url, requestBody, cts.Token);
            if (!httpRes.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini code completion failed with status {Status}. Falling back to rule engine.", httpRes.StatusCode);
                return await _fallbackEngine.CompleteCodeAsync(request, isEnglish);
            }

            var jsonNode = await httpRes.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cts.Token);
            var rawText = jsonNode?["candidates"]?[0]?["content"]?[0]?["parts"]?[0]?["text"]?.GetValue<string>()
                ?? jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
                ?? string.Empty;

            // Strip any unexpected markdown formatting
            rawText = rawText.TrimStart('`');
            if (rawText.StartsWith("csharp\n", StringComparison.OrdinalIgnoreCase))
                rawText = rawText.Substring(7);
            if (rawText.StartsWith("c#\n", StringComparison.OrdinalIgnoreCase))
                rawText = rawText.Substring(3);
            if (rawText.EndsWith("```"))
                rawText = rawText.Substring(0, rawText.Length - 3);

            if (string.IsNullOrEmpty(rawText))
            {
                return await _fallbackEngine.CompleteCodeAsync(request, isEnglish);
            }

            return new AiCodeCompletionResponse
            {
                Success = true,
                Completion = rawText,
                ModelUsed = $"Gemini 2.0 Flash ({_model})"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Gemini code completion exception ({Message}). Falling back to rule engine.", ex.Message);
            return await _fallbackEngine.CompleteCodeAsync(request, isEnglish);
        }
    }
}