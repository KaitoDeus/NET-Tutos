using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NET_Tutos.CodeRunner.Application.Contracts;
using NET_Tutos.CodeRunner.Application.Services;
using NET_Tutos.CodeRunner.Domain.Rules;

namespace NET_Tutos.CodeRunner.Infrastructure.Engines;

public class RoslynCodeRunnerEngine : ICodeRunnerEngine
{
    private static readonly SemaphoreSlim _executionLock = new(1, 1);

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

    public async Task<ExecuteScriptResponse> ExecuteAsync(string code, int timeoutMs = ExecutionConstraints.DefaultTimeoutMs)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new ExecuteScriptResponse
            {
                IsSuccess = false,
                Error = "Mã nguồn không được để trống.",
                Output = string.Empty
            };
        }

        var securityError = ValidateSecurity(code);
        if (!string.IsNullOrEmpty(securityError))
        {
            return new ExecuteScriptResponse
            {
                IsSuccess = false,
                Error = securityError,
                Output = string.Empty
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stringWriter = new StringWriter();
        bool lockAcquired = false;

        try
        {
            using var queueCts = new CancellationTokenSource(5000);
            try
            {
                await _executionLock.WaitAsync(queueCts.Token);
                lockAcquired = true;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new ExecuteScriptResponse
                {
                    IsSuccess = false,
                    IsTimeout = true,
                    Error = "Hệ thống biên dịch đang bận xử lý tác vụ khác, vui lòng thử lại sau vài giây.",
                    Output = string.Empty,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            using var executionCts = new CancellationTokenSource(timeoutMs);

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
                return new ExecuteScriptResponse
                {
                    IsSuccess = false,
                    Error = $"Lỗi biên dịch (Compilation Error):\n{compileErrMsg}",
                    Output = stringWriter.ToString(),
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            var runTask = script.RunAsync(cancellationToken: executionCts.Token);
            var timeoutTask = Task.Delay(timeoutMs);
            var completedTask = await Task.WhenAny(runTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                executionCts.Cancel();
                stopwatch.Stop();
                return new ExecuteScriptResponse
                {
                    IsSuccess = false,
                    IsTimeout = true,
                    Error = $"Thời gian chạy vượt quá giới hạn an toàn ({timeoutMs / 1000.0:F1}s). Vui lòng kiểm tra lại vòng lặp hoặc thuật toán.",
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

            return new ExecuteScriptResponse
            {
                IsSuccess = true,
                Output = string.IsNullOrWhiteSpace(output) ? "[Chương trình kết thúc thành công - Không có dữ liệu in ra màn hình]" : output,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new ExecuteScriptResponse
            {
                IsSuccess = false,
                IsTimeout = true,
                Error = $"Thời gian chạy vượt quá giới hạn an toàn ({timeoutMs / 1000}s). Vui lòng kiểm tra lại vòng lặp hoặc thuật toán.",
                Output = stringWriter.ToString(),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (CompilationErrorException ex)
        {
            stopwatch.Stop();
            return new ExecuteScriptResponse
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
            return new ExecuteScriptResponse
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

    public async Task<EvaluateChallengeResponse> EvaluateChallengeAsync(EvaluateChallengeRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new EvaluateChallengeResponse
        {
            ChallengeId = request.ChallengeId,
            TotalTestsCount = request.TestCases.Count
        };

        if (string.IsNullOrWhiteSpace(request.SolutionCode))
        {
            response.CompileError = "Mã giải pháp không được để trống.";
            return response;
        }

        var secError = ValidateSecurity(request.SolutionCode);
        if (!string.IsNullOrEmpty(secError))
        {
            response.CompileError = secError;
            return response;
        }

        int passedCount = 0;

        foreach (var tc in request.TestCases)
        {
            var harnessCode = BuildTestHarness(request.SolutionCode, tc.InputParameters);
            var execResult = await ExecuteAsync(harnessCode, timeoutMs: 3000);

            if (!execResult.IsSuccess && execResult.Error != null && execResult.Error.Contains("Lỗi biên dịch"))
            {
                response.CompileError = execResult.Error;
                response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return response;
            }

            var actual = (execResult.Output ?? string.Empty).Trim();
            var expected = (tc.ExpectedOutput ?? string.Empty).Trim();

            bool passed = execResult.IsSuccess && string.Equals(actual, expected, StringComparison.Ordinal);
            if (passed) passedCount++;

            response.Details.Add(new TestCaseResultDto
            {
                TestCaseId = tc.Id,
                Input = tc.IsHidden ? "Tham số kiểm thử bảo mật [Hidden]" : tc.InputParameters,
                Expected = tc.IsHidden ? "[Hidden]" : expected,
                Actual = tc.IsHidden ? (passed ? "Chính xác" : "Không khớp kết quả mong muốn") : actual,
                Passed = passed,
                IsHidden = tc.IsHidden,
                Error = execResult.IsSuccess ? null : execResult.Error
            });
        }

        stopwatch.Stop();
        response.PassedTestsCount = passedCount;
        response.AllPassed = (passedCount == request.TestCases.Count);
        response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        return response;
    }

    private static string ValidateSecurity(string code)
    {
        if (ExecutionConstraints.ContainsForbiddenKeyword(code, out var matchedKeyword))
        {
            return $"Từ chối thực thi: Mã nguồn chứa từ khóa bị cấm vì lý do an toàn bảo mật ('{matchedKeyword}').";
        }
        return string.Empty;
    }

    private static string BuildTestHarness(string userCode, string inputParameters)
    {
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

        return $@"{userCode}
// Direct execution with input: {inputParameters}";
    }
}
