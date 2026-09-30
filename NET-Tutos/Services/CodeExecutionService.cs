using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class CodeExecutionService : ICodeExecutionService
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

    private static readonly string[] _forbiddenKeywords =
    {
        "System.Diagnostics.Process",
        "Process.Start",
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileStream",
        "System.Environment.Exit",
        "System.Net.Sockets",
        "System.Reflection.Emit",
        "Assembly.Load"
    };

    public async Task<CodeExecutionResponse> ExecuteAsync(string code, int timeoutMs = 4000)
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
        if (!string.IsNullOrEmpty(securityError))
        {
            return new CodeExecutionResponse
            {
                IsSuccess = false,
                Error = securityError,
                Output = string.Empty
            };
        }

        var stopwatch = Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(timeoutMs);

        await _executionLock.WaitAsync(cts.Token);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stringWriter = new StringWriter();

        try
        {
            Console.SetOut(stringWriter);
            Console.SetError(stringWriter);

            var script = CSharpScript.Create(code, _defaultOptions);
            var diagnostics = script.Compile(cts.Token);
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

            var state = await script.RunAsync(cancellationToken: cts.Token);
            stopwatch.Stop();

            var output = stringWriter.ToString();
            if (string.IsNullOrWhiteSpace(output) && state.ReturnValue != null)
            {
                output = state.ReturnValue.ToString() ?? string.Empty;
            }

            return new CodeExecutionResponse
            {
                IsSuccess = true,
                Output = string.IsNullOrWhiteSpace(output) ? "[Chương trình kết thúc thành công - Không có dữ liệu in ra màn hình]" : output,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new CodeExecutionResponse
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
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            _executionLock.Release();
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

    private static string ValidateSecurity(string code)
    {
        foreach (var keyword in _forbiddenKeywords)
        {
            if (code.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return $"Từ chối thực thi: Mã nguồn chứa từ khóa bị cấm vì lý do an toàn bảo mật ('{keyword}').";
            }
        }
        return string.Empty;
    }

    private static string BuildTestHarness(string userCode, string inputParameters)
    {
        // Check method name from class Solution
        // e.g. public static ... MethodName(...)
        var match = Regex.Match(userCode, @"public\s+static\s+[\w<>\[\]\?]+\s+(\w+)\s*\(");
        if (match.Success)
        {
            var methodName = match.Groups[1].Value;
            return $@"{userCode}

// Evaluation Test Harness
var __result = Solution.{methodName}({inputParameters});
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
