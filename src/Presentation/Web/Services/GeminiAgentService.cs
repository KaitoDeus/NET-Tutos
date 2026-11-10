using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.ViewModels;
using NET_Tutos.BuildingBlocks.EventBus.Events;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;

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
    private readonly IAppCacheService? _cacheService;
    private readonly IKafkaProducer? _kafkaProducer;
    private readonly ISemanticCurriculumSearchService? _semanticSearch;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IObservabilityService? _observability;

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
        IAiConversationService? conversationService = null,
        IAppCacheService? cacheService = null,
        IKafkaProducer? kafkaProducer = null,
        ISemanticCurriculumSearchService? semanticSearch = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IObservabilityService? observability = null)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _configuration = configuration;
        _codeExecutionService = codeExecutionService;
        _context = context;
        _streakService = streakService;
        _fallbackEngine = fallbackEngine;
        _logger = logger;
        _conversationService = conversationService;
        _cacheService = cacheService;
        _kafkaProducer = kafkaProducer;
        _semanticSearch = semanticSearch;
        _httpContextAccessor = httpContextAccessor;
        _observability = observability;

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
        var stopwatch = Stopwatch.StartNew();

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

        stopwatch.Stop();

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

        // 3. Emit Kafka Stream Telemetry Event for AI Auditing and Adaptive Learning Analytics
        bool isFallback = !IsLiveGeminiAvailable
            || response.ResponseMarkdown.Contains("[Chế độ Ngoại tuyến")
            || response.ResponseMarkdown.Contains("[Offline Engine");
        EmitAtusTelemetry(request, response.ModelUsed ?? _model, stopwatch.ElapsedMilliseconds, response.Success, isFallback);

        return response;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        AiTutorChatRequest request,
        bool isEnglish,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!IsLiveGeminiAvailable)
        {
            await foreach (var chunk in _fallbackEngine.StreamChatAsync(request, isEnglish, cancellationToken))
            {
                yield return chunk;
            }
            stopwatch.Stop();
            EmitAtusTelemetry(request, "OfflineEngine", stopwatch.ElapsedMilliseconds, true, isFallback: true);
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

        var ragContext = await GetCurriculumRagContextAsync(request.Message, isEnglish);
        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray { new JsonObject { ["text"] = BuildUserPrompt(request, ragContext) } }
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

            if (streamSucceeded)
            {
                stopwatch.Stop();
                EmitAtusTelemetry(request, currentModel, stopwatch.ElapsedMilliseconds, true, isFallback: false);
                yield break;
            }
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
            stopwatch.Stop();
            EmitAtusTelemetry(request, "OfflineEngine", stopwatch.ElapsedMilliseconds, true, isFallback: true);
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

        // Enrich prompt with real Semantic Vector & Curriculum RAG Context
        var ragContext = await GetCurriculumRagContextAsync(request.Message, isEnglish);

        // Append current turn user prompt
        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray
            {
                new JsonObject
                {
                    ["text"] = BuildUserPrompt(request, ragContext)
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

    private static string BuildUserPrompt(AiTutorChatRequest request, string? ragContext = null)
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
                        .Where(t => EF.Functions.Like(t.Title, $"%{query}%") || EF.Functions.Like(t.Summary, $"%{query}%") || EF.Functions.Like(t.Slug, $"%{query}%"))
                        .Take(3)
                        .Select(t => new { t.Id, t.Title, t.Slug, t.Summary, t.Difficulty })
                        .ToListAsync();

                    var arr = new JsonArray();
                    foreach (var item in results)
                    {
                        arr.Add(new JsonObject
                        {
                            ["id"] = item.Id,
                            ["title"] = item.Title,
                            ["slug"] = item.Slug,
                            ["summary"] = item.Summary,
                            ["difficulty"] = item.Difficulty.ToString(),
                            ["url"] = $"/lessons/{item.Slug}"
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

        var swJson = Stopwatch.StartNew();
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
                    swJson.Stop();
                    _observability?.RecordAiRequest("structured_json", isFallback: false, isSuccess: true, swJson.Elapsed.TotalMilliseconds);
                    return rawText.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("CallGeminiJsonAsync exception for {Model}: {Msg}", currentModel, ex.Message);
            }
        }

        swJson.Stop();
        _observability?.RecordAiRequest("structured_json", isFallback: true, isSuccess: false, swJson.Elapsed.TotalMilliseconds);
        return null;
    }

    public async Task<AiChallengeHintResponse> GetChallengeHintAsync(AiChallengeHintRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable) return await _fallbackEngine.GetChallengeHintAsync(request, isEnglish);

        string? cacheKey = null;
        if (_cacheService != null)
        {
            var hash = ComputeSha256(request.CurrentCode ?? "");
            cacheKey = $"atus:hint:{(isEnglish ? "en" : "vi")}:{request.ChallengeId}:{request.HintLevel}:{hash}";
            var cached = await _cacheService.GetAsync<AiChallengeHintResponse>(cacheKey);
            if (cached != null) return cached;
        }

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
                : @"Bạn là Atus, Principal .NET Mentor. Cung cấp gợi ý theo cấp độ cho bài tập lập trình:
Cấp độ 1: Khái niệm thuật toán cốt lõi.
Cấp độ 2: Cấu trúc dữ liệu và hướng tiếp cận tối ưu.
Cấp độ 3: Mã giả hoặc logic từng bước.
Trả về định dạng JSON hợp lệ:
{
  ""hintLevel"": 1,
  ""title"": ""Tiêu đề gợi ý"",
  ""content"": ""Nội dung gợi ý mang tính khơi gợi tư duy Socratic."",
  ""codeClue"": ""// Gợi ý cú pháp ngắn nếu cần"",
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
                    if (_cacheService != null && cacheKey != null)
                    {
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(12));
                    }
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

        string? cacheKey = null;
        if (_cacheService != null && !string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var hash = ComputeSha256(request.SourceCode.Trim());
            cacheKey = $"atus:review:{(isEnglish ? "en" : "vi")}:{hash}";
            var cached = await _cacheService.GetAsync<AiCodeReviewResponse>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
        }

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
                : @"Bạn là Atus, Chuyên gia Principal C# & .NET Code Reviewer của NET-Tutos.
Phân tích mã nguồn C# theo tiêu chuẩn Clean Code, Modern C# (.NET 10), Hiệu năng và Bảo mật.
Trả về định dạng JSON hợp lệ:
{
  ""score"": 85,
  ""summary"": ""Tóm tắt tổng quan về chất lượng mã nguồn."",
  ""points"": [
    {
      ""category"": ""Clean Code"",
      ""severity"": ""warning"",
      ""title"": ""Tiêu đề nhận xét"",
      ""description"": ""Giải thích chi tiết và đề xuất tối ưu.""
    }
  ],
  ""refactoredCode"": ""Đoạn mã C# đã được tái cấu trúc sạch đẹp""
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
                    if (_cacheService != null && cacheKey != null)
                    {
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(12));
                    }
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

        string? cacheKey = null;
        if (_cacheService != null && !string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var hash = ComputeSha256($"{request.ChallengeTitle}:{request.SourceCode.Trim()}");
            cacheKey = $"atus:complexity:{(isEnglish ? "en" : "vi")}:{hash}";
            var cached = await _cacheService.GetAsync<AiComplexityResponse>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
        }

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
                : @"Bạn là Atus, Chuyên gia Cấu trúc dữ liệu & Giải thuật của NET-Tutos.
Phân tích độ phức tạp thuật toán Big-O (Thời gian & Không gian bộ nhớ) của đoạn mã C#.
Trả về định dạng JSON hợp lệ:
{
  ""timeComplexity"": ""O(N log N)"",
  ""spaceComplexity"": ""O(1)"",
  ""efficiencyScore"": 88,
  ""summary"": ""Tóm tắt độ phức tạp thuật toán."",
  ""detailedExplanation"": ""Giải thích chi tiết các vòng lặp, đệ quy hoặc phân bổ bộ nhớ."",
  ""optimizationTip"": ""Gợi ý cụ thể để tối ưu thêm nếu có."",
  ""optimalComparison"": ""So sánh với độ phức tạp tối ưu lý thuyết của bài toán.""
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
                    if (_cacheService != null && cacheKey != null)
                    {
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(12));
                    }
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

        string? cacheKey = null;
        if (_cacheService != null && !string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var hash = ComputeSha256($"{request.ChallengeTitle}:{request.ActualOutput}:{request.SourceCode}");
            cacheKey = $"atus:diagnose:{(isEnglish ? "en" : "vi")}:{hash}";
            var cached = await _cacheService.GetAsync<AiDiagnoseTestFailureResponse>(cacheKey);
            if (cached != null) return cached;
        }

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
                : @"Bạn là Atus, Chẩn đoán Kiểm thử Tự động cho bài tập C# của NET-Tutos.
Phân tích nguyên nhân test case thất bại và đưa ra gợi ý sư phạm.
Trả về định dạng JSON hợp lệ:
{
  ""failureCategory"": ""Lỗi logic / Điều kiện biên / Tràn số / Sai kiểu"",
  ""diagnosticTitle"": ""Tiêu đề chẩn đoán"",
  ""analysis"": ""Phân tích nguyên nhân gốc rễ."",
  ""likelyFaultyCodeSegment"": ""Dòng lệnh hoặc vòng lặp nghi vấn"",
  ""guidanceHint"": ""Gợi ý khắc phục khơi mở tư duy."",
  ""suggestedFixSnippet"": ""Đoạn mã khắc phục mẫu""
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
                    if (_cacheService != null && cacheKey != null)
                    {
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(12));
                    }
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

        string? cacheKey = null;
        if (_cacheService != null && !string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var hash = ComputeSha256($"{request.MethodOrClassName}:{request.SourceCode}");
            cacheKey = $"atus:unittest:{(isEnglish ? "en" : "vi")}:{hash}";
            var cached = await _cacheService.GetAsync<AiGenerateUnitTestResponse>(cacheKey);
            if (cached != null) return cached;
        }

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
                : @"Bạn là Atus, Chuyên gia Kiểm thử Tự động & Software Engineer in Test của NET-Tutos.
Sinh mã Unit Test xUnit chất lượng cao theo chuẩn AAA Pattern (Arrange - Act - Assert) kết hợp FluentAssertions cho đoạn mã C#.
Trả về định dạng JSON hợp lệ:
{
  ""testFramework"": ""xUnit & FluentAssertions"",
  ""testCode"": ""// Lớp kiểm thử hoàn chỉnh"",
  ""explanation"": ""Giải thích các kịch bản kiểm thử đã bao phủ."",
  ""coveredScenarios"": [""Trường hợp thông thường"", ""Kiểm tra tham số null"", ""Điều kiện biên""]
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
                    if (_cacheService != null && cacheKey != null)
                    {
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(12));
                    }
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
                : @"Bạn là Atus, Principal .NET Architect & Technical Lead Interviewer độc quyền của NET-Tutos.
Đánh giá câu trả lời phỏng vấn kỹ thuật của học viên một cách khách quan, sắc bén và thực chiến.
Trả về định dạng JSON hợp lệ theo đúng cấu trúc sau:
{
  ""overallScore"": 85,
  ""technicalAccuracyScore"": 85,
  ""clarityStructureScore"": 80,
  ""depthPracticalScore"": 75,
  ""performanceRating"": ""Trình độ Senior / Middle Vững / Junior Tiềm năng"",
  ""feedback"": ""Lời nhận xét ngắn gọn, trực diện chỉ rõ ưu điểm và điểm cần cải thiện."",
  ""strengths"": [""Ưu điểm 1"", ""Ưu điểm 2""],
  ""missingPoints"": [""Điểm thiếu sót 1"", ""Điểm thiếu sót 2""],
  ""followUpQuestion"": ""Một câu hỏi xoáy phản biện sắc bén về tình huống biên, đa luồng hoặc rò rỉ bộ nhớ."",
  ""modelAnswerSample"": ""### Đáp Án Mẫu Chuẩn Senior\nNgắn gọn, chuyên sâu, đúng trọng tâm.""
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

        string? cacheKey = null;
        if (_cacheService != null && !string.IsNullOrWhiteSpace(request.CandidateAnswer))
        {
            var hash = ComputeSha256($"{request.OriginalQuestion}:{request.FollowUpQuestion}:{request.CandidateAnswer}");
            cacheKey = $"atus:interviewfollowup:{(isEnglish ? "en" : "vi")}:{hash}";
            var cached = await _cacheService.GetAsync<AiInterviewFollowUpResponse>(cacheKey);
            if (cached != null) return cached;
        }

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
                : @"Bạn là Atus, Principal .NET Architect.
Đánh giá câu trả lời của ứng viên cho câu hỏi xoáy kỹ thuật.
Trả về định dạng JSON hợp lệ theo đúng cấu trúc:
{
  ""isSatisfactory"": true,
  ""feedback"": ""Nhận xét ngắn gọn về câu trả lời cho câu hỏi phản biện."",
  ""bonusScore"": 10,
  ""deepDiveInsight"": ""Góc nhìn chuyên sâu từ kinh nghiệm thực chiến production.""
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
                    if (_cacheService != null && cacheKey != null)
                    {
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(12));
                    }
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
                : @"Bạn là Atus, Principal .NET Architect & Technical Lead.
Tổng kết báo cáo năng lực phỏng vấn toàn diện cho học viên dựa trên các câu hỏi đã trả lời.
Trả về định dạng JSON hợp lệ theo đúng cấu trúc:
{
  ""averageScore"": 80,
  ""readinessLevel"": ""Sẵn sàng Phỏng vấn Senior .NET Developer / Middle Vững"",
  ""readinessBadgeClass"": ""success"",
  ""executiveAssessment"": ""Đoạn văn nhận định tổng quan về bản lĩnh kỹ thuật, tư duy kiến trúc và mức độ sẵn sàng thực tế."",
  ""skillBreakdowns"": [
    { ""skillName"": ""Bản chất C# & Quản lý Bộ nhớ"", ""score"": 85, ""level"": ""Senior"" },
    { ""skillName"": ""Lập trình Bất đồng bộ & Xử lý Đồng thời"", ""score"": 80, ""level"": ""Mid"" },
    { ""skillName"": ""Entity Framework Core & Tối ưu Truy vấn"", ""score"": 78, ""level"": ""Mid"" },
    { ""skillName"": ""Kiến trúc ASP.NET Core & Web API"", ""score"": 82, ""level"": ""Solid"" },
    { ""skillName"": ""Clean Architecture & Mẫu Thiết kế SOLID"", ""score"": 80, ""level"": ""Proficient"" }
  ],
  ""recommendedLessons"": [
    { ""title"": ""Tên bài học"", ""slug"": ""slug-bai-hoc"", ""reason"": ""Lý do cần ôn tập thêm."" }
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
                    await EnrichReportWithRagLessonsAsync(result, request, isEnglish);
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live interview report generation failed. Falling back.");
        }

        var fallbackReport = await _fallbackEngine.GenerateInterviewReportAsync(request, isEnglish);
        await EnrichReportWithRagLessonsAsync(fallbackReport, request, isEnglish);
        return fallbackReport;
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

    public async Task<AiAdaptiveChallengeResponse> GenerateAdaptiveChallengeAsync(AiAdaptiveChallengeRequest request, bool isEnglish)
    {
        if (!IsLiveGeminiAvailable)
        {
            return await _fallbackEngine.GenerateAdaptiveChallengeAsync(request, isEnglish);
        }

        string normalizedTopic = (request.Topic ?? ".NET").Trim();
        string normalizedDiff = string.IsNullOrWhiteSpace(request.Difficulty) ? "Intermediate" : request.Difficulty.Trim();
        string? cacheKey = null;

        if (_cacheService != null)
        {
            var hash = ComputeSha256($"{normalizedTopic}:{normalizedDiff}");
            cacheKey = $"atus:adaptivechallenge:{(isEnglish ? "en" : "vi")}:{hash}";
            var cached = await _cacheService.GetAsync<AiAdaptiveChallengeResponse>(cacheKey);
            if (cached != null) return cached;
        }

        try
        {
            var systemPrompt = isEnglish
                ? @"You are Atus, Principal .NET Architect. Design a concise, practical C# 14 top-level script coding exercise tailored to remediate the candidate's weak spot.
IMPORTANT: The starterCode and solutionCode MUST run cleanly inside a lightweight Roslyn C# script sandbox (using only System, System.Collections.Generic, System.Linq, System.Text, System.Threading.Tasks, System.Text.Json — NO external namespaces or EF DbContext classes, and finish with Console.WriteLine(...)).
Return valid JSON matching this schema:
{
  ""title"": ""Concise Exercise Title"",
  ""problemStatementMarkdown"": ""Clear description of bug or optimization required."",
  ""starterCode"": ""// Runnable C# top-level snippet with TODO"",
  ""solutionCode"": ""// Working reference C# solution printing expectedOutput"",
  ""hint"": ""Helpful hint without spoiling completely."",
  ""expectedOutput"": ""Exact Console.WriteLine output of solutionCode""
}"
                : @"Bạn là Atus, Principal .NET Architect. Thiết kế 1 bài tập thực hành sửa lỗi hoặc tối ưu code C# 14 ngắn gọn, sát thực tế để khắc phục đúng điểm yếu của học viên.
QUAN TRỌNG: starterCode và solutionCode PHẢI chạy được bằng Roslyn C# Script (top-level statements, chỉ dùng System, System.Collections.Generic, System.Linq, System.Text, System.Threading.Tasks, System.Text.Json — KHÔNG dùng DbContext hay thư viện ngoài, và kết thúc bằng Console.WriteLine(...)).
Trả về định dạng JSON hợp lệ:
{
  ""title"": ""Tiêu đề bài tập thực hành"",
  ""problemStatementMarkdown"": ""Mô tả rõ lỗi hoặc yêu cầu tối ưu."",
  ""starterCode"": ""// Mã nguồn mẫu top-level có chú thích TODO"",
  ""solutionCode"": ""// Lời giải chuẩn chạy ra đúng expectedOutput"",
  ""hint"": ""Gợi ý hướng giải quyết ngắn gọn."",
  ""expectedOutput"": ""Kết quả chính xác in ra từ Console.WriteLine""
}";

            var userPrompt = $"[Weak Topic]: {normalizedTopic}\n[Difficulty]: {normalizedDiff}";
            var jsonText = await CallGeminiJsonAsync(systemPrompt, userPrompt);
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiAdaptiveChallengeResponse>(jsonText, options);
                if (result != null && !string.IsNullOrWhiteSpace(result.StarterCode))
                {
                    result.Success = true;
                    result.Topic = normalizedTopic;
                    result.Difficulty = normalizedDiff;
                    result.ModelUsed = $"Atus ({_model})";

                    if (_cacheService != null && cacheKey != null)
                    {
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(12));
                    }

                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Adaptive challenge generation failed ({Message}). Falling back to curated rule engine.", ex.Message);
        }

        return await _fallbackEngine.GenerateAdaptiveChallengeAsync(request, isEnglish);
    }

    private void EmitAtusTelemetry(
        AiTutorChatRequest request,
        string modelUsed,
        long latencyMs,
        bool isSuccess,
        bool isFallback)
    {
        _observability?.RecordAiRequest("chat", isFallback, isSuccess, latencyMs);
        if (_kafkaProducer == null) return;

        var userId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var promptSnippet = (request.Message ?? string.Empty).Length > 100
            ? (request.Message ?? string.Empty).Substring(0, 100) + "..."
            : (request.Message ?? string.Empty);

        var telemetryEvent = new AtusAiTelemetryEvent(
            SessionId: request.ConversationId?.ToString() ?? Guid.NewGuid().ToString("N")[..12],
            UserId: userId,
            PromptSnippet: promptSnippet,
            ModelUsed: modelUsed,
            LatencyMs: latencyMs,
            IsSuccess: isSuccess,
            IsFallback: isFallback
        );

        _kafkaProducer.ProduceFireAndForget(KafkaTopics.AtusAiTelemetry, telemetryEvent);
    }

    private async Task EnrichReportWithRagLessonsAsync(
        AiInterviewReportResponse report,
        AiInterviewReportRequest request,
        bool isEnglish)
    {
        if (_semanticSearch == null) return;

        try
        {
            var weakItems = request.Items
                .OrderBy(i => i.Score)
                .Take(3)
                .ToList();

            var queryParts = weakItems.Any()
                ? weakItems.Select(w => $"{w.Topic} {w.Question}").ToList()
                : new List<string> { "C# Async Await Dependency Injection Clean Architecture" };

            var verifiedLessons = new List<AiInterviewRecommendedLesson>();
            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var q in queryParts)
            {
                var searchRes = await _semanticSearch.SearchAsync(q, isEnglish, topK: 2);
                foreach (var hit in searchRes.Hits)
                {
                    if (!seenTitles.Add(hit.Title)) continue;

                    string urlSlug = isEnglish
                        ? $"/csharp-curriculum/{hit.LessonNumber}"
                        : $"/chuong-trinh-csharp/{hit.LessonNumber}";

                    verifiedLessons.Add(new AiInterviewRecommendedLesson
                    {
                        Title = hit.Title,
                        Slug = urlSlug,
                        Reason = hit.Summary
                    });

                    if (verifiedLessons.Count >= 3) break;
                }
                if (verifiedLessons.Count >= 3) break;
            }

            if (verifiedLessons.Count > 0)
            {
                report.RecommendedLessons = verifiedLessons;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("EnrichReportWithRagLessonsAsync skipped: {Msg}", ex.Message);
        }
    }

    private async Task<string?> GetCurriculumRagContextAsync(string? message, bool isEnglish)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;

        try
        {
            // Delegate to the unified 4-source Semantic Vector RAG index (Curriculum + Tutorials + Challenges + Flashcards)
            if (_semanticSearch != null)
            {
                var unifiedContext = await _semanticSearch.BuildUnifiedRagContextAsync(message, isEnglish, topK: 4);
                if (!string.IsNullOrWhiteSpace(unifiedContext))
                {
                    return unifiedContext;
                }
            }

            // Lightweight DB fallback when semantic search service is not injected (e.g. isolated unit tests)
            var lowerMsg = message.ToLowerInvariant();
            var keywords = lowerMsg.Split(new[] { ' ', ',', '.', '?', '!', '\n', '\r', ':', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !IsCommonStopWord(w))
                .Distinct()
                .Take(6)
                .ToList();

            if (!keywords.Any()) return null;

            var tutorials = await _context.Tutorials
                .AsNoTracking()
                .OrderBy(t => t.OrderIndex)
                .Select(t => new TutorialRagDto(t.Id, t.Title, t.Slug, t.Summary ?? string.Empty))
                .ToListAsync();

            var matches = tutorials
                .Select(t =>
                {
                    int score = keywords.Sum(kw => (t.Title.Contains(kw, StringComparison.OrdinalIgnoreCase) ? 5 : 0)
                                                 + (t.Slug.Contains(kw, StringComparison.OrdinalIgnoreCase) ? 3 : 0)
                                                 + (t.Summary.Contains(kw, StringComparison.OrdinalIgnoreCase) ? 1 : 0));
                    return new { Tutorial = t, Score = score };
                })
                .Where(m => m.Score >= 5)
                .OrderByDescending(m => m.Score)
                .Take(2)
                .Select(m => m.Tutorial)
                .ToList();

            if (!matches.Any()) return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(isEnglish
                ? "\n[Relevant NET-Tutos Tutorials Context (Use exact markdown [Title](/lessons/slug))]:"
                : "\n[Bài hướng dẫn NET-Tutos liên quan (Hãy gắn link [Tên bài](/lessons/slug) nếu hữu ích)]:");

            foreach (var match in matches)
            {
                sb.AppendLine($"- [{match.Title}](/lessons/{match.Slug}): {match.Summary}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GetCurriculumRagContextAsync error: {Msg}", ex.Message);
            return null;
        }
    }

    private static bool IsCommonStopWord(string word)
    {
        string[] stops = ["cho", "mình", "hỏi", "làm", "thế", "nào", "để", "giải", "thích", "cách", "trong", "với", "và", "các", "những", "được", "không", "như", "what", "how", "why", "when", "the", "and", "for", "with", "from", "that", "this", "help", "please"];
        return stops.Contains(word, StringComparer.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private record TutorialRagDto(int Id, string Title, string Slug, string Summary);
}