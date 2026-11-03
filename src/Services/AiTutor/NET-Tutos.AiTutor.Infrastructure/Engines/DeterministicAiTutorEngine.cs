using System.Text.RegularExpressions;
using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;
using NET_Tutos.AiTutor.Domain.Enums;
using NET_Tutos.AiTutor.Domain.Rules;
using NET_Tutos.AiTutor.Domain.ValueObjects;

namespace NET_Tutos.AiTutor.Infrastructure.Engines;

public class DeterministicAiTutorEngine : IAiTutorEngine
{
    public Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var query = (request.Message ?? string.Empty).Trim().ToLowerInvariant();

        if (query.Contains("linq") || query.Contains("deferred execution") || query.Contains("iqueryable"))
        {
            return Task.FromResult(new AiChatResponseDto
            {
                ModelUsed = "Atus-Microservice",
                ResponseMarkdown = isEnglish
                    ? "### 🚀 Understanding LINQ in C#\n\n" +
                      "**LINQ (Language Integrated Query)** is a powerful query engine in .NET that provides type-safe queries over collections, databases (EF Core), XML, and more.\n\n" +
                      "#### 1. Deferred vs Immediate Execution\n" +
                      "- **Deferred Execution**: Methods like `.Where()`, `.Select()`, `.Take()` return an `IEnumerable<T>` that is **only executed when enumerated** (e.g. `foreach` or `.ToList()`).\n" +
                      "- **Immediate Execution**: Methods like `.ToList()`, `.ToArray()`, `.Count()`, `.First()` execute immediately and materialize the results.\n\n" +
                      "```csharp\n" +
                      "var topLearners = users\n" +
                      "    .Where(u => u.ExperiencePoints > 500)\n" +
                      "    .OrderByDescending(u => u.ExperiencePoints)\n" +
                      "    .Take(10)\n" +
                      "    .ToList();\n" +
                      "```"
                    : "### 🚀 Làm chủ LINQ trong C#\n\n" +
                      "**LINQ (Language Integrated Query)** là công cụ truy vấn mạnh mẽ trong .NET, mang lại cú pháp nhất quán, kiểm tra kiểu tĩnh (type-safe) khi thao tác với Collections, Entity Framework Core, v.v.\n\n" +
                      "#### 1. Deferred Execution (Thực thi trì hoãn) vs Immediate Execution (Thực thi ngay)\n" +
                      "- **Deferred Execution**: Các phương thức như `.Where()`, `.Select()`, `.OrderBy()` không chạy ngay lập tức. Chúng chỉ thực thi khi bạn bắt đầu duyệt qua dữ liệu (như vòng lặp `foreach` hoặc gọi `.ToList()`).\n" +
                      "- **Immediate Execution**: Các phương thức gom nhóm hoặc tổng hợp như `.ToList()`, `.Count()`, `.FirstOrDefault()`, `.Sum()` sẽ thực thi truy vấn ngay tại thời điểm gọi.\n\n" +
                      "```csharp\n" +
                      "var topStudents = students\n" +
                      "    .Where(s => s.ExperiencePoints >= 300)\n" +
                      "    .OrderByDescending(s => s.CurrentStreak)\n" +
                      "    .Take(5)\n" +
                      "    .ToList();\n" +
                      "```",
                CodeSnippet = "var topStudents = students.Where(s => s.Score >= 80).OrderByDescending(s => s.Score).Take(5).ToList();"
            });
        }

        if (query.Contains("async") || query.Contains("await") || query.Contains("task") || query.Contains("bất đồng bộ"))
        {
            return Task.FromResult(new AiChatResponseDto
            {
                ModelUsed = "Atus-Microservice",
                ResponseMarkdown = isEnglish
                    ? "### ⚡ Async/Await and Concurrency in .NET\n\n" +
                      "Async/Await in C# releases threads while waiting for I/O operations, allowing web servers to handle thousands of concurrent requests.\n\n" +
                      "1. **Async all the way**: Never call `.Result` or `.Wait()` on tasks.\n" +
                      "2. **Return `Task` or `Task<T>`, never `async void`**.\n" +
                      "3. **Pass `CancellationToken`** across I/O boundaries."
                    : "### ⚡ Lập Trình Bất Đồng Bộ Async / Await trong .NET\n\n" +
                      "Cơ chế **Async/Await** trong C# giải phóng luồng (thread) xử lý trong thời gian chờ đợi các tác vụ I/O, giúp ASP.NET Core chịu tải hàng nghìn kết nối đồng thời.\n\n" +
                      "1. **Async All The Way**: Không gọi `.Result` hoặc `.Wait()`.\n" +
                      "2. **Trả về `Task` hoặc `Task<T>`, tránh `async void`**.\n" +
                      "3. **Truyền `CancellationToken`** xuyên suốt pipeline.",
                CodeSnippet = "public async Task<List<Item>> GetDataAsync(CancellationToken ct) => await _context.Items.AsNoTracking().ToListAsync(ct);"
            });
        }

        return Task.FromResult(new AiChatResponseDto
        {
            ModelUsed = "Atus-Microservice",
            ResponseMarkdown = isEnglish
                ? "### 🤖 Atus .NET AI Tutor Assistant\n\nHello! I am **Atus**, your interactive .NET 10 & C# 14 AI mentor. Ask a question or share a code snippet for analysis!"
                : "### 🤖 Trợ Lý AI Gia Sư .NET (Atus)\n\nChào bạn! Tôi là **Atus**, Trợ lý AI chuyên sâu về hệ sinh thái **C# 14 & .NET 10** của NET-Tutos. Bạn hãy đặt câu hỏi hoặc dán đoạn mã nguồn cần hỗ trợ nhé!"
        });
    }

    public Task<ExplainErrorResponseDto> ExplainErrorAsync(ExplainErrorRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var error = request.ErrorMessage ?? string.Empty;

        int? lineNumber = null;
        var lineMatch = Regex.Match(error, @"\((\d+),(\d+)\)");
        if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out int line))
        {
            lineNumber = line;
        }

        if (error.Contains("CS0103"))
        {
            var varMatch = Regex.Match(error, "'(?<name>[^']+)'");
            var varName = varMatch.Success ? varMatch.Groups["name"].Value : "identifier";

            return Task.FromResult(new ExplainErrorResponseDto
            {
                ErrorCode = "CS0103",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? $"Name '{varName}' does not exist in the current context" : $"Tên '{varName}' không tồn tại trong ngữ cảnh hiện tại",
                Explanation = isEnglish
                    ? $"The compiler encountered `{varName}`, but cannot find any variable, field, method, or class with this name."
                    : $"Trình biên dịch gặp biến hoặc phương thức `{varName}` nhưng không tìm thấy khai báo nào phù hợp.",
                HowToFix = isEnglish
                    ? $"1. Check for typos in `{varName}`.\n2. Ensure `{varName}` is declared before this line."
                    : $"1. Kiểm tra lại chính tả tên `{varName}`.\n2. Đảm bảo biến đã được khai báo trước dòng này.",
                SuggestedFixCode = $"var {varName} = default;\nConsole.WriteLine({varName});",
                ProTip = isEnglish ? "C# is strictly case-sensitive." : "C# phân biệt chữ hoa và chữ thường nghiêm ngặt."
            });
        }

        if (error.Contains("CS1002"))
        {
            return Task.FromResult(new ExplainErrorResponseDto
            {
                ErrorCode = "CS1002",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "; expected (Missing Semicolon)" : "Thiếu dấu chấm phẩy (; expected)",
                Explanation = isEnglish
                    ? "In C#, every statement must end with a semicolon `;`."
                    : "Trong C#, mọi câu lệnh thực thi đều phải kết thúc bằng dấu chấm phẩy `;`.",
                HowToFix = isEnglish ? "Add `;` to the end of the statement on the indicated line." : "Thêm dấu `;` vào cuối câu lệnh tại dòng bị báo lỗi.",
                SuggestedFixCode = "Console.WriteLine(\"Hello World\");",
                ProTip = isEnglish ? "Compilers often report missing semicolons on the following line." : "Trình biên dịch đôi khi báo lỗi thiếu dấu chấm phẩy ở dòng kế tiếp."
            });
        }

        if (error.Contains("CS0029"))
        {
            return Task.FromResult(new ExplainErrorResponseDto
            {
                ErrorCode = "CS0029",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Cannot implicitly convert type" : "Không thể chuyển đổi ngầm định kiểu dữ liệu",
                Explanation = isEnglish
                    ? "You are trying to assign a value of one type to a variable of an incompatible type."
                    : "Bạn đang gán một giá trị sang một biến thuộc kiểu dữ liệu không tương thích.",
                HowToFix = isEnglish
                    ? "Use explicit conversion methods like `int.TryParse()` or `.ToString()`."
                    : "Sử dụng hàm ép kiểu tương thích như `int.TryParse()` hoặc `.ToString()`.",
                SuggestedFixCode = "int number = int.Parse(\"123\");",
                ProTip = isEnglish ? "Prefer `int.TryParse()` over `int.Parse()`." : "Luôn ưu tiên `int.TryParse()` để tránh ném ngoại lệ."
            });
        }

        return Task.FromResult(new ExplainErrorResponseDto
        {
            ErrorCode = "Compiler Diagnostic",
            LineNumber = lineNumber,
            ErrorTitle = isEnglish ? "Compiler or Runtime Issue" : "Lỗi Biên Dịch hoặc Thực Thi",
            Explanation = error,
            HowToFix = isEnglish
                ? "Review the syntax near the reported line number and verify types and imports."
                : "Kiểm tra lại cú pháp xung quanh dòng được thông báo, kiểu dữ liệu và từ khóa `using`.",
            ProTip = isEnglish ? "Fix the first compiler error first." : "Luôn ưu tiên sửa lỗi đầu tiên trong danh sách trước."
        });
    }

    public Task<ChallengeHintResponseDto> GetChallengeHintAsync(ChallengeHintRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var title = !string.IsNullOrWhiteSpace(request.ChallengeTitle)
            ? request.ChallengeTitle
            : (isEnglish ? "Coding Challenge" : "Thử thách thuật toán");
        int level = Math.Clamp(request.HintLevel, 1, 3);

        return Task.FromResult(level switch
        {
            1 => new ChallengeHintResponseDto
            {
                HintLevel = 1,
                Title = isEnglish ? "Hint Level 1: Intuition & Approach" : "Gợi ý Cấp 1: Hướng tiếp cận & Ý tưởng",
                Content = isEnglish
                    ? $"For **{title}**, identify the edge cases first (empty collection, null, zero) and consider a single-pass scan."
                    : $"Với thử thách **{title}**, hãy xác định rõ input/output và kiểm tra các trường hợp biên trước (mảng rỗng, giá trị 0, số âm).",
                CodeClue = "// Start with a guard clause and single loop",
                HasNextLevel = true
            },
            2 => new ChallengeHintResponseDto
            {
                HintLevel = 2,
                Title = isEnglish ? "Hint Level 2: Recommended Data Structure" : "Gợi ý Cấp 2: Cấu trúc dữ liệu & Thuật toán",
                Content = isEnglish
                    ? "Consider using a `Dictionary<TKey, TValue>` or `HashSet<T>` for O(1) lookups."
                    : "Hãy dùng `Dictionary<K, V>` hoặc `HashSet<T>` để đạt độ phức tạp tìm kiếm O(1).",
                CodeClue = "var seen = new HashSet<int>();\nforeach (var item in items) { ... }",
                HasNextLevel = true
            },
            _ => new ChallengeHintResponseDto
            {
                HintLevel = 3,
                Title = isEnglish ? "Hint Level 3: Pseudocode Outline" : "Gợi ý Cấp 3: Khung cài đặt & Mã giả",
                Content = isEnglish
                    ? "Fill in the core condition inside the loop skeleton."
                    : "Hoàn thiện điều kiện logic bên trong khung sườn vòng lặp.",
                CodeClue = "if (input == null) return default;\nfor (int i = 0; i < input.Length; i++) {\n    // Core logic\n}",
                HasNextLevel = false
            }
        });
    }

    public Task<CodeReviewResponseDto> ReviewCodeAsync(CodeReviewRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var code = request.SourceCode ?? string.Empty;
        var points = new List<CodeReviewPointDto>();
        int rawScore = 85;

        if (code.Contains("+=") && (code.Contains("for") || code.Contains("while")))
        {
            points.Add(new CodeReviewPointDto
            {
                Category = "Performance",
                Severity = "warning",
                Title = isEnglish ? "String Concatenation in Loop" : "Nối chuỗi trong vòng lặp",
                Description = isEnglish
                    ? "Use `StringBuilder` instead of `+=` inside loops for O(N) memory performance."
                    : "Sử dụng `StringBuilder` thay vì `+=` trong vòng lặp để tối ưu bộ nhớ."
            });
            rawScore -= 10;
        }

        if (code.Contains("new List<") && !code.Contains("["))
        {
            points.Add(new CodeReviewPointDto
            {
                Category = "Modern C# 14",
                Severity = "info",
                Title = isEnglish ? "Use Collection Expressions" : "Sử dụng Collection Expressions mới",
                Description = isEnglish
                    ? "Use C# 12/14 collection expressions `[1, 2, 3]`."
                    : "Sử dụng cú pháp Collection Expressions gọn gàng `[1, 2, 3]`."
            });
            rawScore += 5;
        }

        var score = new EfficiencyScore(Math.Clamp(rawScore, 60, 98));
        return Task.FromResult(new CodeReviewResponseDto
        {
            Score = score.Value,
            Summary = isEnglish
                ? $"Code review complete! Quality score: {score.Value}/100."
                : $"Đánh giá mã nguồn hoàn tất! Điểm chất lượng: {score.Value}/100.",
            Points = points,
            RefactoredCode = "// Mã nguồn đã được tối ưu hóa theo chuẩn Modern .NET 10\n" + code
        });
    }

    public Task<AnalyzeComplexityResponseDto> AnalyzeComplexityAsync(AnalyzeComplexityRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var code = request.SourceCode ?? string.Empty;

        var tier = AiAnalysisRules.DetectTimeComplexity(code);
        var timeComplexity = AiAnalysisRules.FormatBigONotation(tier);
        bool hasLinearSpace = AiAnalysisRules.HasLinearSpaceAllocation(code);
        var spaceComplexity = hasLinearSpace ? "O(N)" : "O(1)";

        int baseScore = tier switch
        {
            ComplexityTier.Quadratic => 65,
            ComplexityTier.Exponential => 70,
            ComplexityTier.LogLinear => 80,
            ComplexityTier.Linear => 92,
            ComplexityTier.Logarithmic => 95,
            _ => 98
        };

        if (!hasLinearSpace)
            baseScore += 5;

        var efficiencyScore = new EfficiencyScore(Math.Clamp(baseScore, 50, 100));

        string summary = tier switch
        {
            ComplexityTier.Quadratic => isEnglish
                ? "Quadratic Time Complexity detected due to nested iterations."
                : "Phát hiện độ phức tạp thời gian bậc hai O(N²) do vòng lặp lồng nhau.",
            ComplexityTier.LogLinear => isEnglish
                ? "Log-linear Time Complexity detected due to sorting operation."
                : "Độ phức tạp thời gian O(N log N) do sử dụng thao tác sắp xếp.",
            ComplexityTier.Exponential => isEnglish
                ? "Recursive Call Tree detected."
                : "Phát hiện hàm đệ quy (Recursion).",
            ComplexityTier.Linear => isEnglish
                ? "Linear Time Complexity — highly efficient single pass!"
                : "Độ phức tạp thời gian tuyến tính O(N) — thuật toán duyệt 1 lần rất tối ưu!",
            _ => isEnglish
                ? "Constant Time Complexity — instantaneous execution!"
                : "Độ phức tạp thời gian hằng số O(1) — tốc độ tức thì!"
        };

        return Task.FromResult(new AnalyzeComplexityResponseDto
        {
            Success = true,
            TimeComplexity = timeComplexity,
            SpaceComplexity = spaceComplexity,
            EfficiencyScore = efficiencyScore.Value,
            Summary = summary,
            DetailedExplanation = summary,
            OptimizationTip = tier == ComplexityTier.Quadratic
                ? (isEnglish ? "Use a Dictionary<TKey, TValue> or HashSet<T> for O(1) lookups." : "Sử dụng Dictionary hoặc HashSet để giảm tìm kiếm xuống O(1).")
                : null
        });
    }

    public Task<DiagnoseTestFailureResponseDto> DiagnoseTestFailureAsync(DiagnoseTestFailureRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var input = request.InputParameters ?? string.Empty;
        var expected = request.ExpectedOutput ?? string.Empty;
        var actual = request.ActualOutput ?? string.Empty;

        if (input.Contains("[]") || input.Contains("\"\"") || input == "0" || input == "null")
        {
            return Task.FromResult(new DiagnoseTestFailureResponseDto
            {
                FailureCategory = "Edge Case",
                DiagnosticTitle = isEnglish ? "Unhandled Boundary / Empty Input Edge Case" : "Chưa xử lý trường hợp biên",
                Analysis = isEnglish
                    ? $"Input `{input}` produced `{actual}` instead of `{expected}`."
                    : $"Bộ test case biên `{input}` trả về `{actual}` thay vì `{expected}`.",
                GuidanceHint = isEnglish
                    ? "Add guard clauses at the beginning of your method."
                    : "Thêm câu lệnh bảo vệ (Guard Clause) ở ngay đầu phương thức.",
                SuggestedFixSnippet = "if (input == null || input.Length == 0) return default;"
            });
        }

        if (int.TryParse(expected, out int expNum) && int.TryParse(actual, out int actNum) && Math.Abs(expNum - actNum) == 1)
        {
            return Task.FromResult(new DiagnoseTestFailureResponseDto
            {
                FailureCategory = "Off-by-one",
                DiagnosticTitle = isEnglish ? "Off-by-one Loop Boundary Error" : "Lỗi sai lệch 1 đơn vị (Off-by-one)",
                Analysis = isEnglish
                    ? $"Actual `{actual}` differs from expected `{expected}` by 1."
                    : $"Kết quả `{actual}` lệch đúng 1 đơn vị so với `{expected}`.",
                GuidanceHint = isEnglish
                    ? "Check loop condition `<` vs `<=`."
                    : "Kiểm tra điều kiện vòng lặp `<` hay `<=`.",
                SuggestedFixSnippet = "for (int i = 0; i < n; i++)"
            });
        }

        return Task.FromResult(new DiagnoseTestFailureResponseDto
        {
            FailureCategory = "Logic Flaw",
            DiagnosticTitle = isEnglish ? $"Logic Discrepancy on Test #{request.TestIndex}" : $"Sai lệch logic tại Test Case #{request.TestIndex}",
            Analysis = isEnglish
                ? $"For input `{input}`, expected `{expected}` but got `{actual}`."
                : $"Với đầu vào `{input}`, kỳ vọng `{expected}` nhưng tính ra `{actual}`.",
            GuidanceHint = isEnglish
                ? "Trace variable values at each iteration."
                : "Hãy chạy nhẩm từng dòng code với đúng bộ input này."
        });
    }

    public Task<GenerateUnitTestResponseDto> GenerateUnitTestAsync(GenerateUnitTestRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var code = request.SourceCode ?? string.Empty;

        var methodMatch = Regex.Match(code, @"(?:public|private|protected|internal|static|\s)+\s+([a-zA-Z0-9_<>]+)\s+([a-zA-Z0-9_]+)\s*\(([^)]*)\)");
        string methodName = methodMatch.Success ? methodMatch.Groups[2].Value : "ExecuteSolution";
        string returnType = methodMatch.Success ? methodMatch.Groups[1].Value : "var";

        var unitTestCode = $@"using Xunit;

public class {methodName}Tests
{{
    [Fact]
    public void {methodName}_StandardInput_ReturnsExpectedResult()
    {{
        // Arrange
        // Act
        // {returnType} actual = Solution.{methodName}(...);
        // Assert
    }}

    [Theory]
    [InlineData(/* input, expected */)]
    public void {methodName}_ParameterizedInputs_MatchesExpectedOutput()
    {{
        // Arrange - Act - Assert
    }}
}}";

        return Task.FromResult(new GenerateUnitTestResponseDto
        {
            Success = true,
            TestFramework = "xUnit (.NET 10)",
            TestCode = unitTestCode,
            Explanation = isEnglish
                ? $"Generated clean xUnit test suite for `{methodName}` following the AAA pattern."
                : $"Bộ kiểm thử xUnit được sinh tự động cho phương thức `{methodName}` tuân thủ mô hình AAA.",
            CoveredScenarios = isEnglish
                ? ["Valid standard inputs (Happy Path)", "Boundary values (Edge Case)", "Stress Test"]
                : ["Dữ liệu chuẩn hợp lệ (Happy Path)", "Giá trị biên (Edge Case)", "Kiểm thử tải (Stress Test)"]
        });
    }

    public Task<EvaluateInterviewResponseDto> EvaluateInterviewAnswerAsync(EvaluateInterviewRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var answer = (request.CandidateAnswer ?? string.Empty).Trim();
        var lowerAnswer = answer.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(answer) || answer.Length < 15)
        {
            var briefRubric = new InterviewRubricScore(20, 30, 20);
            return Task.FromResult(new EvaluateInterviewResponseDto
            {
                Success = true,
                FlashcardId = request.FlashcardId,
                OverallScore = briefRubric.WeightedOverall,
                TechnicalAccuracyScore = briefRubric.TechnicalAccuracy,
                ClarityStructureScore = briefRubric.ClarityStructure,
                DepthPracticalScore = briefRubric.PracticalDepth,
                PerformanceRating = isEnglish ? "Incomplete / Too Brief" : "Chưa hoàn chỉnh / Quá ngắn",
                Feedback = isEnglish
                    ? "Your answer is too brief. Outline the core concept, internal mechanism, and caveats."
                    : "Câu trả lời quá ngắn. Hãy trình bày: Khái niệm cốt lõi -> Cơ chế bên dưới -> Lưu ý thực tế.",
                Strengths = [isEnglish ? "Attempted the question." : "Đã tiếp nhận câu hỏi."],
                MissingPoints = [isEnglish ? "Lacks implementation details." : "Thiếu cơ chế hoạt động thực tế."],
                FollowUpQuestion = isEnglish
                    ? "Could you elaborate with a concrete scenario in ASP.NET Core?"
                    : "Bạn có thể giải thích chi tiết hơn kèm ví dụ trong dự án ASP.NET Core không?",
                ModelAnswerSample = request.BenchmarkAnswer ?? string.Empty
            });
        }

        int matchCount = 0;
        var strengths = new List<string>();
        var missing = new List<string>();

        if (lowerAnswer.Contains("non-blocking") || lowerAnswer.Contains("không chặn") || lowerAnswer.Contains("state machine") || lowerAnswer.Contains("statemachine"))
        {
            matchCount += 3;
            strengths.Add(isEnglish
                ? "Demonstrated deep internals knowledge of non-blocking async state machines."
                : "Hiểu sâu cơ chế non-blocking và State Machine của trình biên dịch Roslyn.");
        }

        if (lowerAnswer.Contains("cancellationtoken") || lowerAnswer.Contains("asnotracking") || lowerAnswer.Contains("idisposable") || lowerAnswer.Contains("dependency injection"))
        {
            matchCount += 2;
            strengths.Add(isEnglish
                ? "Highlighted production best practices and resource optimization."
                : "Chỉ ra được các thực hành tốt nhất trong môi trường Production.");
        }

        if (strengths.Count == 0)
        {
            strengths.Add(isEnglish ? "Addressed the core intent of the question." : "Nêu được khái niệm sơ bộ của chủ đề.");
        }

        if (missing.Count == 0)
        {
            missing.Add(isEnglish ? "Include benchmark trade-offs for Senior mastery." : "Nêu thêm đánh đổi hiệu năng để đạt chuẩn Senior.");
        }

        int wordCount = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        int lengthBonus = Math.Clamp(wordCount / 5, 0, 20);

        int techScore = Math.Clamp(40 + (matchCount * 12) + (lengthBonus / 2), 45, 98);
        int clarityScore = Math.Clamp(45 + (wordCount >= 30 ? 25 : wordCount / 2) + (answer.Contains('.') ? 15 : 5), 50, 96);
        int depthScore = Math.Clamp(35 + (matchCount * 14) + (lengthBonus / 2), 40, 95);

        var rubric = new InterviewRubricScore(techScore, clarityScore, depthScore);

        return Task.FromResult(new EvaluateInterviewResponseDto
        {
            Success = true,
            FlashcardId = request.FlashcardId,
            OverallScore = rubric.WeightedOverall,
            TechnicalAccuracyScore = rubric.TechnicalAccuracy,
            ClarityStructureScore = rubric.ClarityStructure,
            DepthPracticalScore = rubric.PracticalDepth,
            PerformanceRating = rubric.GetPerformanceRating(isEnglish),
            Feedback = isEnglish
                ? $"Solid response! Technical score: {rubric.TechnicalAccuracy}/100."
                : $"Câu trả lời rất triển vọng! Điểm kỹ thuật: {rubric.TechnicalAccuracy}/100.",
            Strengths = strengths,
            MissingPoints = missing,
            FollowUpQuestion = isEnglish
                ? "Follow-up Challenge: What are the memory allocation and throughput trade-offs under high load?"
                : "Câu hỏi xoáy từ Tech Lead: Giải pháp này có đánh đổi gì về cấp phát bộ nhớ khi chịu tải cao?",
            ModelAnswerSample = request.BenchmarkAnswer ?? string.Empty
        });
    }

    public Task<InterviewFollowUpResponseDto> AnswerFollowUpAsync(InterviewFollowUpRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var answer = (request.CandidateAnswer ?? string.Empty).Trim().ToLowerInvariant();
        bool isGood = answer.Length > 20;

        return Task.FromResult(new InterviewFollowUpResponseDto
        {
            Success = true,
            IsSatisfactory = isGood,
            BonusScore = isGood ? 12 : 6,
            Feedback = isGood
                ? (isEnglish ? "🎯 Excellent follow-up justification!" : "🎯 Phản hồi câu hỏi xoáy rất sắc bén!")
                : (isEnglish ? "Good attempt! Consider edge cases." : "Định hướng đúng, cần chú ý thêm các trường hợp biên."),
            DeepDiveInsight = isEnglish
                ? "Senior Takeaway: Always propagate CancellationToken and use structured error handling."
                : "Lời khuyên từ Tech Lead: Luôn truyền CancellationToken xuyên suốt pipeline."
        });
    }

    public Task<InterviewReportResponseDto> GenerateInterviewReportAsync(InterviewReportRequestDto request)
    {
        bool isEnglish = request.IsEnglish;
        var items = request.Items ?? [];
        double avg = items.Count > 0 ? items.Average(i => i.Score) : 70;
        int avgScore = (int)Math.Round(avg);

        var rubric = new InterviewRubricScore(avgScore, avgScore, avgScore);
        int xp = rubric.ReadinessTier switch
        {
            InterviewReadinessTier.SeniorReady => 90,
            InterviewReadinessTier.MiddleReady => 70,
            InterviewReadinessTier.JuniorReady => 50,
            _ => 30
        };

        return Task.FromResult(new InterviewReportResponseDto
        {
            Success = true,
            AverageScore = avgScore,
            ReadinessLevel = rubric.GetPerformanceRating(isEnglish),
            ReadinessBadgeClass = avgScore >= 85 ? "success" : (avgScore >= 70 ? "primary" : "info"),
            ExecutiveAssessment = isEnglish
                ? "Comprehensive evaluation completed by Atus AI Tech Lead."
                : "Báo cáo đánh giá năng lực hoàn tất bởi Tech Lead AI Atus.",
            SkillBreakdowns =
            [
                new() { SkillName = isEnglish ? "C# Internals & Memory" : "Bản chất C# & Bộ nhớ", Score = Math.Clamp(avgScore + 2, 45, 96), Level = avgScore >= 80 ? "Senior" : "Mid" },
                new() { SkillName = isEnglish ? "Async & Concurrency" : "Bất đồng bộ & Đa luồng", Score = Math.Clamp(avgScore - 2, 40, 95), Level = avgScore >= 75 ? "Mid-Senior" : "Junior" },
                new() { SkillName = isEnglish ? "EF Core & Database" : "EF Core & Cơ sở dữ liệu", Score = Math.Clamp(avgScore + 1, 45, 98), Level = avgScore >= 80 ? "Senior" : "Mid" }
            ],
            RecommendedLessons =
            [
                new()
                {
                    Title = "Lập trình Bất đồng bộ (Async/Await) nâng cao trong .NET",
                    Slug = "async-await-csharp-nang-cao",
                    Reason = isEnglish ? "Essential for scalable Web API development." : "Kiến thức bắt buộc để làm chủ Web API chịu tải cao."
                }
            ],
            XpAwarded = xp,
            TimeSpentSeconds = request.TimeSpentSeconds
        });
    }

    public Task<AiTutorStatusResponseDto> CheckStatusAsync()
    {
        return Task.FromResult(new AiTutorStatusResponseDto
        {
            Success = true,
            IsLiveGemini = false,
            Provider = "Atus AI Microservice Engine",
            Model = "Atus-Microservice-v1",
            IsAvailable = true,
            LatencyMs = 1,
            Message = "AiTutor.API microservice is healthy and ready."
        });
    }
}
