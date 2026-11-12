using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NET_Tutos.BuildingBlocks.Common.Sandbox;
using NET_Tutos.BuildingBlocks.EventBus.Events;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class CodeExecutionService : ICodeExecutionService
{
    private static readonly SemaphoreSlim _executionLock = new(1, 1);
    private readonly HttpClient? _httpClient;
    private readonly string? _remoteServiceUrl;
    private readonly ILogger<CodeExecutionService>? _logger;
    private readonly IAppCacheService? _cacheService;
    private readonly IKafkaProducer? _kafkaProducer;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public CodeExecutionService(
        IHttpClientFactory? httpClientFactory = null,
        IConfiguration? configuration = null,
        ILogger<CodeExecutionService>? logger = null,
        IAppCacheService? cacheService = null,
        IKafkaProducer? kafkaProducer = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _logger = logger;
        _cacheService = cacheService;
        _kafkaProducer = kafkaProducer;
        _httpContextAccessor = httpContextAccessor;
        _remoteServiceUrl = configuration?["Services:CodeRunner:Url"]?.TrimEnd('/');
        if (!string.IsNullOrEmpty(_remoteServiceUrl) && httpClientFactory != null)
        {
            _httpClient = httpClientFactory.CreateClient("CodeRunner");
            _httpClient.BaseAddress = new Uri(_remoteServiceUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(8);
        }
    }

    private static readonly ScriptOptions _defaultOptions = ScriptOptions.Default
        .AddImports(
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Threading.Tasks",
            "System.Text.Json"
        )
        .AddReferences(
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            typeof(System.Text.Json.JsonSerializer).Assembly
        );

    public async Task<CodeExecutionResponse> ExecuteAsync(string code, int timeoutMs = 4000)
    {
        var result = await ExecuteInternalAsync(code, timeoutMs);
        if (_kafkaProducer != null && !string.IsNullOrWhiteSpace(code))
        {
            var userId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var errSnippet = result.Error?.Length > 140 ? result.Error[..140] : result.Error;
            bool hasCompileErr = !result.IsSuccess && !result.IsTimeout &&
                (result.Error?.Contains("biên dịch", StringComparison.OrdinalIgnoreCase) == true ||
                 result.Error?.Contains("CS", StringComparison.Ordinal) == true);

            _kafkaProducer.ProduceFireAndForget(
                KafkaTopics.CodeExecutionTelemetry,
                new CodeExecutionTelemetryEvent(
                    ExecutionId: Guid.NewGuid().ToString("N")[..12],
                    UserId: userId,
                    Language: "csharp",
                    ExecutionDurationMs: result.ExecutionTimeMs,
                    MemoryBytes: GC.GetTotalMemory(false),
                    IsSuccess: result.IsSuccess,
                    HasCompileErrors: hasCompileErr,
                    ErrorSnippet: errSnippet));
        }

        return result;
    }

    private async Task<CodeExecutionResponse> ExecuteInternalAsync(string code, int timeoutMs)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = "Mã nguồn không được để trống.",
                Output = string.Empty
            };
        }

        var securityError = ValidateSecurity(code);
        bool isDeterministic = string.IsNullOrEmpty(securityError) && IsDeterministicCode(code);
        string? cacheKey = isDeterministic && _cacheService != null
            ? $"coderunner:exec:{HybridRedisCacheService.ComputeSha256Hash(code)}"
            : null;
        if (cacheKey != null && _cacheService != null)
        {
            var cachedExec = await _cacheService.GetAsync<CodeExecutionResponse>(cacheKey);
            if (cachedExec != null) return cachedExec;
        }
        if (!string.IsNullOrEmpty(securityError))
        {
            return new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = securityError,
                Output = string.Empty
            };
        }

        int safeTimeoutMs = SandboxGuardrails.ClampTimeoutMs(timeoutMs);

        // Microservices Delegation: If remote CodeRunner service is configured, delegate execution
        if (_httpClient != null && !string.IsNullOrEmpty(_remoteServiceUrl))
        {
            try
            {
                var payload = new { Code = code, TimeoutMs = safeTimeoutMs };
                var response = await _httpClient.PostAsJsonAsync("/api/v1/runner/execute", payload);
                if (response.IsSuccessStatusCode)
                {
                    var envelope = await response.Content.ReadFromJsonAsync<JsonNode>();
                    var data = envelope?["data"] ?? envelope;
                    if (data != null)
                    {
                        var remoteResult = new CodeExecutionResponse
                        {
                            IsSuccess = data["isSuccess"]?.GetValue<bool>() ?? false,
                            Output = data["output"]?.GetValue<string>() ?? string.Empty,
                            Error = data["error"]?.GetValue<string?>(),
                            ExecutionTimeMs = data["executionTimeMs"]?.GetValue<long>() ?? 0,
                            IsTimeout = data["isTimeout"]?.GetValue<bool>() ?? false
                        };
                        if (remoteResult.IsSuccess && !remoteResult.IsTimeout && cacheKey != null && _cacheService != null)
                        {
                            await _cacheService.SetAsync(cacheKey, remoteResult, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(5));
                        }
                        return remoteResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "CodeRunner.API remote execution call failed. Falling back to in-process compiler.");
            }
        }

        var stopwatch = Stopwatch.StartNew();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stringWriter = new BoundedStringWriter(SandboxGuardrails.MaxOutputCharacters);
        bool lockAcquired = false;

        try
        {
            // Allow waiting for the execution lock with a reasonable queue timeout
            using var queueCts = new CancellationTokenSource(5000);
            try
            {
                await _executionLock.WaitAsync(queueCts.Token);
                lockAcquired = true;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new CodeExecutionResponse
                {
                    IsSuccess = false,
                    IsTimeout = true,
                    Error = "Hệ thống biên dịch đang bận xử lý tác vụ khác, vui lòng thử lại sau vài giây.",
                    Output = string.Empty,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            using var executionCts = new CancellationTokenSource(safeTimeoutMs);

            Console.SetOut(stringWriter);
            Console.SetError(stringWriter);

            var script = CSharpScript.Create(code, _defaultOptions);
            var diagnostics = script.Compile(executionCts.Token);
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (errors.Count > 0)
            {
                var compileErrMsg = string.Join("\n", errors.Select(e =>
                {
                    var lineSpan = e.Location.GetLineSpan();
                    var line = lineSpan.StartLinePosition.Line + 1;
                    var col = lineSpan.StartLinePosition.Character + 1;
                    return $"[Dòng {line}, Cột {col}] {e.GetMessage()}";
                }));

                stopwatch.Stop();
                return new CodeExecutionResponse
                {
                    IsSuccess = false,
                    Error = $"Lỗi biên dịch (Compilation Error):\n{compileErrMsg}",
                    Output = stringWriter.ToString(),
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            var runTask = script.RunAsync(cancellationToken: executionCts.Token);
            var timeoutTask = Task.Delay(safeTimeoutMs);
            var completedTask = await Task.WhenAny(runTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                executionCts.Cancel();
                stopwatch.Stop();
                return new CodeExecutionResponse
                {
                    IsSuccess = false,
                    IsTimeout = true,
                    Error = $"Thời gian chạy vượt quá giới hạn an toàn ({safeTimeoutMs / 1000.0:F1}s). Vui lòng kiểm tra lại vòng lặp hoặc thuật toán.",
                    Output = stringWriter.ToString(),
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            var state = await runTask;
            stopwatch.Stop();

            var output = stringWriter.ToString();
            if (string.IsNullOrWhiteSpace(output) && state.ReturnValue != null)
            {
                output = state.ReturnValue.ToString() ?? string.Empty;
            }

            var finalResult = new CodeExecutionResponse
            {
                IsSuccess = true,
                Output = string.IsNullOrWhiteSpace(output) ? "[Chương trình kết thúc thành công - Không có dữ liệu in ra màn hình]" : output,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
            if (cacheKey != null && _cacheService != null)
            {
                await _cacheService.SetAsync(cacheKey, finalResult, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(5));
            }
            return finalResult;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new CodeExecutionResponse
            {
                IsSuccess = false,
                IsTimeout = true,
                Error = $"Thời gian chạy vượt quá giới hạn an toàn ({safeTimeoutMs / 1000.0:F1}s). Vui lòng kiểm tra lại vòng lặp hoặc thuật toán.",
                Output = stringWriter.ToString(),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (CompilationErrorException ex)
        {
            stopwatch.Stop();
            return new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = $"Lỗi biên dịch:\n{string.Join("\n", ex.Diagnostics.Select(d => d.GetMessage()))}",
                Output = stringWriter.ToString(),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = $"Lỗi thời gian chạy (Runtime Error): {ex.GetType().Name}\n{ex.Message}",
                Output = stringWriter.ToString(),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        finally
        {
            if (lockAcquired)
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                _executionLock.Release();
            }
        }
    }

    public async Task<ChallengeSubmissionResponse> EvaluateChallengeAsync(CodingChallenge challenge, string userCode, int timeoutMs = 4000)
    {
        var response = new ChallengeSubmissionResponse();
        var stopwatch = Stopwatch.StartNew();

        var securityError = ValidateSecurity(userCode);
        if (!string.IsNullOrEmpty(securityError))
        {
            response.CompileError = securityError;
            return response;
        }

        var testCases = challenge.TestCases.ToList();
        response.TotalTestsCount = testCases.Count;

        if (testCases.Count == 0)
        {
            response.AllPassed = true;
            return response;
        }

        // Microservices Delegation: If remote CodeRunner service is configured, delegate challenge evaluation
        if (_httpClient != null && !string.IsNullOrEmpty(_remoteServiceUrl))
        {
            try
            {
                var payload = new
                {
                    ChallengeId = challenge.Id,
                    SolutionCode = userCode,
                    TestCases = testCases.Select(tc => new
                    {
                        Id = tc.Id,
                        InputParameters = tc.InputParameters,
                        ExpectedOutput = tc.ExpectedOutput,
                        IsHidden = tc.IsHidden
                    }).ToList()
                };

                var httpResponse = await _httpClient.PostAsJsonAsync("/api/v1/runner/evaluate", payload);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var envelope = await httpResponse.Content.ReadFromJsonAsync<JsonNode>();
                    var data = envelope?["data"] ?? envelope;
                    if (data != null)
                    {
                        var remoteResult = new ChallengeSubmissionResponse
                        {
                            AllPassed = data["allPassed"]?.GetValue<bool>() ?? false,
                            PassedTestsCount = data["passedTestsCount"]?.GetValue<int>() ?? 0,
                            TotalTestsCount = data["totalTestsCount"]?.GetValue<int>() ?? 0,
                            ExecutionTimeMs = data["executionTimeMs"]?.GetValue<long>() ?? 0,
                            CompileError = data["compileError"]?.GetValue<string?>()
                        };

                        var detailsArray = data["details"]?.AsArray();
                        if (detailsArray != null)
                        {
                            int idx = 1;
                            foreach (var item in detailsArray)
                            {
                                remoteResult.Details.Add(new TestCaseEvaluationItem
                                {
                                    TestIndex = idx++,
                                    Input = item?["input"]?.GetValue<string>() ?? string.Empty,
                                    Expected = item?["expected"]?.GetValue<string>() ?? string.Empty,
                                    Actual = item?["actual"]?.GetValue<string>() ?? string.Empty,
                                    Passed = item?["passed"]?.GetValue<bool>() ?? false,
                                    IsHidden = item?["isHidden"]?.GetValue<bool>() ?? false,
                                    Error = item?["error"]?.GetValue<string?>()
                                });
                            }
                        }

                        return remoteResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "CodeRunner.API remote evaluate call failed. Falling back to in-process compiler.");
            }
        }

        int passedCount = 0;
        int testIndex = 1;

        foreach (var tc in testCases)
        {
            // Build evaluation script
            // Expect userCode has Solution.MethodName(...) or function call
            var harnessCode = BuildTestHarness(userCode, tc.InputParameters);

            var execResult = await ExecuteAsync(harnessCode, timeoutMs);

            if (!execResult.IsSuccess && execResult.Error != null && execResult.Error.Contains("Lỗi biên dịch"))
            {
                response.CompileError = execResult.Error;
                return response;
            }

            var actual = execResult.IsSuccess ? execResult.Output.Trim() : (execResult.Error ?? string.Empty);
            var expected = tc.ExpectedOutput.Trim();

            // Check if matches
            bool passed = execResult.IsSuccess && string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

            if (passed)
            {
                passedCount++;
            }

            response.Details.Add(new TestCaseEvaluationItem
            {
                TestIndex = testIndex++,
                Input = tc.IsHidden ? "Test ẩn (Hidden Test Case)" : tc.InputParameters,
                Expected = tc.IsHidden ? "Test ẩn" : expected,
                Actual = tc.IsHidden ? (passed ? "Chính xác" : "Không khớp kết quả mong muốn") : actual,
                Passed = passed,
                IsHidden = tc.IsHidden,
                Error = execResult.IsSuccess ? null : execResult.Error
            });
        }

        stopwatch.Stop();
        response.PassedTestsCount = passedCount;
        response.AllPassed = (passedCount == testCases.Count);
        response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        return response;
    }

    private static bool IsDeterministicCode(string code)
    {
        string[] nonDeterministicTokens = ["DateTime.", "DateTimeOffset.", "Random", "Guid.NewGuid", "Stopwatch", "Environment.TickCount", "Console.Read"];
        foreach (var token in nonDeterministicTokens)
        {
            if (code.Contains(token, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static string ValidateSecurity(string code) =>
        SandboxGuardrails.ValidateSecurityAndLimits(code);

    private static string BuildTestHarness(string userCode, string inputParameters)
    {
        // Check method name from class Solution: e.g. public static ... MethodName(...) or public ... MethodName(...)
        var match = Regex.Match(userCode, @"public\s+(?<static>static\s+)?[\w<>\[\]\?]+\s+(?<name>\w+)\s*\(");
        if (match.Success)
        {
            var methodName = match.Groups["name"].Value;
            bool isStatic = match.Groups["static"].Success;
            string invocation = isStatic ? $"Solution.{methodName}({inputParameters})" : $"new Solution().{methodName}({inputParameters})";

            return $@"{userCode}

// Evaluation Test Harness
object? __result = {invocation};
if (__result is System.Collections.IEnumerable __enumerable && !(__result is string))
{{
    var __items = new System.Collections.Generic.List<string>();
    foreach (var __item in __enumerable) __items.Add(__item?.ToString() ?? ""null"");
    Console.Write(string.Join("", "", __items));
}}
else
{{
    Console.Write(__result?.ToString() ?? ""null"");
}}";
        }

        // Fallback: If top-level code or already contains call
        return $@"{userCode}
// Direct execution with input: {inputParameters}";
    }
}
