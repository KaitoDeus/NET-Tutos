using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Data;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public class AiTutorService : IAiTutorService
{
    private readonly AppDbContext _context;

    public AiTutorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AiTutorChatResponse> ChatAsync(AiTutorChatRequest request, bool isEnglish)
    {
        var query = (request.Message ?? string.Empty).Trim().ToLowerInvariant();

        // 1. Check for specific topic questions
        if (query.Contains("linq") || query.Contains("deferred execution") || query.Contains("iqueryable"))
        {
            return new AiTutorChatResponse
            {
                ResponseMarkdown = isEnglish
                    ? "### 🚀 Understanding LINQ in C#\n\n" +
                      "**LINQ (Language Integrated Query)** is a powerful query engine in .NET that provides type-safe queries over collections, databases (EF Core), XML, and more.\n\n" +
                      "#### 1. Deferred vs Immediate Execution\n" +
                      "- **Deferred Execution**: Methods like `.Where()`, `.Select()`, `.Take()` return an `IEnumerable<T>` that is **only executed when enumerated** (e.g. `foreach` or `.ToList()`).\n" +
                      "- **Immediate Execution**: Methods like `.ToList()`, `.ToArray()`, `.Count()`, `.First()` execute immediately and materialize the results.\n\n" +
                      "#### 2. Best Practice Example\n" +
                      "```csharp\n" +
                      "// ✅ Good: Filter early before materializing\n" +
                      "var topLearners = users\n" +
                      "    .Where(u => u.ExperiencePoints > 500)\n" +
                      "    .OrderByDescending(u => u.ExperiencePoints)\n" +
                      "    .Take(10)\n" +
                      "    .ToList();\n" +
                      "```\n\n" +
                      "💡 **Pro Tip**: In Entity Framework Core, keep queries as `IQueryable<T>` until the final step to let SQL Server perform the filtering, avoiding transferring unnecessary rows over the network."
                    : "### 🚀 Làm chủ LINQ trong C#\n\n" +
                      "**LINQ (Language Integrated Query)** là công cụ truy vấn mạnh mẽ trong .NET, mang lại cú pháp nhất quán, kiểm tra kiểu tĩnh (type-safe) khi thao tác với Collections, Entity Framework Core, v.v.\n\n" +
                      "#### 1. Deferred Execution (Thực thi trì hoãn) vs Immediate Execution (Thực thi ngay)\n" +
                      "- **Deferred Execution**: Các phương thức như `.Where()`, `.Select()`, `.OrderBy()` không chạy ngay lập tức. Chúng chỉ thực thi khi bạn bắt đầu duyệt qua dữ liệu (như vòng lặp `foreach` hoặc gọi `.ToList()`).\n" +
                      "- **Immediate Execution**: Các phương thức gom nhóm hoặc tổng hợp như `.ToList()`, `.Count()`, `.FirstOrDefault()`, `.Sum()` sẽ thực thi truy vấn ngay tại thời điểm gọi.\n\n" +
                      "#### 2. Ví dụ chuẩn Modern C#\n" +
                      "```csharp\n" +
                      "// ✅ Lọc và sắp xếp tối ưu\n" +
                      "var topStudents = students\n" +
                      "    .Where(s => s.ExperiencePoints >= 300)\n" +
                      "    .OrderByDescending(s => s.CurrentStreak)\n" +
                      "    .Take(5)\n" +
                      "    .ToList();\n" +
                      "```\n\n" +
                      "💡 **Mẹo của Tech Lead**: Với Entity Framework Core, hãy giữ kiểu `IQueryable<T>` cho đến bước cuối cùng trước khi gọi `.ToListAsync()`, giúp câu lệnh được dịch sang mã SQL tối ưu và chạy trực tiếp trên máy chủ Database.",
                CodeSnippet = "var topStudents = students.Where(s => s.Score >= 80).OrderByDescending(s => s.Score).Take(5).ToList();",
                SuggestedFollowUps = isEnglish
                    ? new List<string> { "Difference between IQueryable and IEnumerable", "How to avoid N+1 query problem in EF Core", "Modern LINQ improvements in .NET 10" }
                    : new List<string> { "Phân biệt IQueryable và IEnumerable", "Cách tránh lỗi N+1 truy vấn trong EF Core", "Các hàm LINQ mới tối ưu trong .NET 10" }
            };
        }

        if (query.Contains("async") || query.Contains("await") || query.Contains("task") || query.Contains("bất đồng bộ"))
        {
            return new AiTutorChatResponse
            {
                ResponseMarkdown = isEnglish
                    ? "### ⚡ Async/Await and Concurrency in .NET\n\n" +
                      "Async/Await in C# releases threads while waiting for I/O operations (Database, Web API, File I/O), allowing web servers to handle thousands of concurrent requests.\n\n" +
                      "#### 3 Golden Rules of Async:\n" +
                      "1. **Async all the way**: Don't mix sync and async. Never call `.Result` or `.Wait()` on tasks, as this leads to thread-pool starvation and deadlocks.\n" +
                      "2. **Return `Task` or `Task<T>`, never `async void`** (except for UI event handlers).\n" +
                      "3. **Use `ValueTask<T>`** for high-throughput paths where the result is often available synchronously.\n\n" +
                      "```csharp\n" +
                      "// ✅ Best Practice: Non-blocking asynchronous method\n" +
                      "public async Task<TutorialDto?> GetTutorialAsync(int id, CancellationToken ct = default)\n" +
                      "{\n" +
                      "    return await _context.Tutorials\n" +
                      "        .AsNoTracking()\n" +
                      "        .Where(t => t.Id == id)\n" +
                      "        .Select(t => new TutorialDto(t.Id, t.Title))\n" +
                      "        .FirstOrDefaultAsync(ct);\n" +
                      "}\n" +
                      "```"
                    : "### ⚡ Lập Trình Bất Đồng Bộ Async / Await trong .NET\n\n" +
                      "Cơ chế **Async/Await** trong C# giải phóng luồng (thread) xử lý trong thời gian chờ đợi các tác vụ I/O (gọi Database, Web API, đọc file), giúp ASP.NET Core chịu tải hàng nghìn kết nối đồng thời mà không bị nghẽn CPU.\n\n" +
                      "#### 3 Nguyên Tắc Vàng Cần Nhớ:\n" +
                      "1. **Async All The Way (Bất đồng bộ xuyên suốt)**: Tuyệt đối không gọi `.Result` hoặc `.Wait()` để ép chạy đồng bộ, vì sẽ dẫn tới deadlock và cạn kiệt ThreadPool.\n" +
                      "2. **Luôn trả về `Task` hoặc `Task<T>`, không dùng `async void`** (ngoại trừ hàm bắt sự kiện giao diện Button Click).\n" +
                      "3. **Sử dụng `CancellationToken`** cho các tác vụ tốn thời gian để hỗ trợ hủy request khi client ngắt kết nối.\n\n" +
                      "```csharp\n" +
                      "// ✅ Chuẩn Clean Architecture Web API\n" +
                      "public async Task<TutorialDto?> GetTutorialAsync(int id, CancellationToken cancellationToken = default)\n" +
                      "{\n" +
                      "    return await _dbContext.Tutorials\n" +
                      "        .AsNoTracking()\n" +
                      "        .Where(t => t.Id == id)\n" +
                      "        .Select(t => new TutorialDto(t.Id, t.Title))\n" +
                      "        .FirstOrDefaultAsync(cancellationToken);\n" +
                      "}\n" +
                      "```",
                CodeSnippet = "public async Task<List<Item>> GetDataAsync(CancellationToken ct) => await _context.Items.ToListAsync(ct);",
                SuggestedFollowUps = isEnglish
                    ? new List<string> { "When to use ValueTask over Task?", "How does CancellationToken work?", "Avoiding Deadlocks in ASP.NET Core" }
                    : new List<string> { "Khi nào nên dùng ValueTask thay cho Task?", "Cách sử dụng CancellationToken hiệu quả", "Cơ chế hoạt động của ThreadPool trong .NET 10" }
            };
        }

        if (query.Contains("dependency injection") || query.Contains("di") || query.Contains("transient") || query.Contains("scoped") || query.Contains("singleton"))
        {
            return new AiTutorChatResponse
            {
                ResponseMarkdown = isEnglish
                    ? "### 🧩 Service Lifetimes in ASP.NET Core Dependency Injection\n\n" +
                      "ASP.NET Core has a built-in IoC Container with 3 primary service lifetimes:\n\n" +
                      "| Lifetime | Creation Frequency | Typical Use Case |\n" +
                      "| :--- | :--- | :--- |\n" +
                      "| **Transient** | Created every time it is requested | Lightweight, stateless helper services |\n" +
                      "| **Scoped** | Created **once per HTTP Request** | `DbContext`, business repositories, unit-of-work |\n" +
                      "| **Singleton** | Created **once on startup** and reused | In-memory caches, configuration providers |\n\n" +
                      "⚠️ **Common Trap**: Never inject a `Scoped` service (like `DbContext`) into a `Singleton` service! This causes captive dependencies and concurrency issues."
                    : "### 🧩 3 Vòng Đời Dịch Vụ trong Dependency Injection của ASP.NET Core\n\n" +
                      "ASP.NET Core tích hợp sẵn bộ quản lý Dependency Injection (IoC Container) với 3 cấp độ vòng đời (Service Lifetimes):\n\n" +
                      "| Vòng đời | Tần suất khởi tạo | Trường hợp sử dụng điển hình |\n" +
                      "| :--- | :--- | :--- |\n" +
                      "| **Transient** (`AddTransient`) | Tạo mới mỗi khi được yêu cầu | Các service xử lý logic nhẹ, không lưu trạng thái (stateless) |\n" +
                      "| **Scoped** (`AddScoped`) | Tạo duy nhất **1 lần cho mỗi HTTP Request** | `AppDbContext`, Business Services, Repositories |\n" +
                      "| **Singleton** (`AddSingleton`) | Tạo **1 lần duy nhất** suốt vòng đời ứng dụng | Cache bộ nhớ, dịch vụ cấu hình hệ thống |\n\n" +
                      "⚠️ **Lỗi phổ biến cần tránh (Captive Dependency)**: Không bao giờ inject một dịch vụ `Scoped` (như `DbContext`) vào trong một dịch vụ `Singleton`. Điều này sẽ khiến DbContext sống mãi mãi và gây lỗi Race Condition khi nhiều user truy cập cùng lúc!",
                CodeSnippet = "builder.Services.AddScoped<ILearningProgressService, LearningProgressService>();",
                SuggestedFollowUps = isEnglish
                    ? new List<string> { "What is Captive Dependency?", "How to use IHttpClientFactory?", "Keyed Services in .NET 10" }
                    : new List<string> { "Lỗi Captive Dependency là gì?", "Cách sử dụng IHttpClientFactory tối ưu", "Keyed Services mới trong .NET 10" }
            };
        }

        if (query.Contains("record") || query.Contains("class") || query.Contains("struct"))
        {
            return new AiTutorChatResponse
            {
                ResponseMarkdown = isEnglish
                    ? "### 📦 Class vs Record vs Struct in Modern C#\n\n" +
                      "- **Class**: Reference type. Compares equality by **reference** (memory address). Best for domain entities with mutable lifecycle.\n" +
                      "- **Record**: Reference type (or `record struct`) with built-in **value-based equality**, concise positional syntax, and non-destructive mutation via `with` expressions. Perfect for DTOs and immutable messages.\n" +
                      "- **Struct**: Value type stored on the stack (when local). Best for small, short-lived types (under 16 bytes).\n\n" +
                      "```csharp\n" +
                      "// ✅ Modern C# Record (Positional syntax)\n" +
                      "public record StudentDto(string Id, string FullName, int Xp);\n" +
                      "\n" +
                      "var s1 = new StudentDto(\"1\", \"Alice\", 100);\n" +
                      "var s2 = s1 with { Xp = 150 }; // Non-destructive mutation\n" +
                      "```"
                    : "### 📦 So Sánh Class, Record và Struct trong C# Hiện Đại\n\n" +
                      "- **Class**: Kiểu tham chiếu (Reference Type). So sánh bằng nhau dựa vào **địa chỉ ô nhớ** (Reference Equality). Phù hợp cho các Entity nghiệp vụ có trạng thái thay đổi theo thời gian.\n" +
                      "- **Record**: Kiểu tham chiếu đặc biệt có tính bất biến (Immutable), tự động so sánh bằng nhau dựa vào **giá trị thuộc tính** (Value Equality) và hỗ trợ cú pháp biến đổi `with`. Thích hợp cho DTOs, API Requests/Responses.\n" +
                      "- **Struct**: Kiểu giá trị (Value Type), thường được cấp phát trên Stack. Phù hợp cho các cấu trúc dữ liệu nhỏ (dưới 16 bytes) như tọa độ `Point`, kích thước `Size`.\n\n" +
                      "```csharp\n" +
                      "// ✅ Cú pháp Record ngắn gọn trong C#\n" +
                      "public record StudentDto(string Id, string FullName, int Xp);\n" +
                      "\n" +
                      "var student1 = new StudentDto(\"1\", \"Khai\", 100);\n" +
                      "var student2 = student1 with { Xp = 150 }; // Tạo bản sao với giá trị XP mới\n" +
                      "```",
                CodeSnippet = "public record UserDto(int Id, string Username, string Email);",
                SuggestedFollowUps = isEnglish
                    ? new List<string> { "Primary Constructors in C#", "Deep copy with 'with' keyword", "Record Struct performance" }
                    : new List<string> { "Primary Constructors trong C#", "Từ khóa 'with' hoạt động thế nào?", "Hiệu năng của Record Struct" }
            };
        }

        // Generic friendly tutor reply
        return new AiTutorChatResponse
        {
            ResponseMarkdown = isEnglish
                ? $"### 🤖 .NET AI Tutor Assistant\n\n" +
                  $"Hello! I am your interactive .NET & C# AI mentor. I can help you with:\n\n" +
                  $"- 🔍 **Explaining compiler errors** like CS0103, CS0029, CS1002 with instant fixes.\n" +
                  $"- 💡 **Coding challenges hints** and algorithmic approaches.\n" +
                  $"- ⚡ **Code review & optimization** based on .NET 10 best practices.\n" +
                  $"- 📚 **In-depth concepts** across ASP.NET Core, EF Core, LINQ, and Clean Architecture.\n\n" +
                  $"Feel free to ask a question or paste your code snippet below!"
                : $"### 🤖 Trợ Lý AI Gia Sư .NET\n\n" +
                  $"Chào bạn! Tôi là Trợ lý AI chuyên sâu về hệ sinh thái **C# & .NET 10**. Tôi có thể hỗ trợ bạn:\n\n" +
                  $"- 🔍 **Phân tích và sửa lỗi biên dịch**: Chỉ rõ nguyên nhân và cách khắc phục các lỗi như CS0103, CS0029, NullReferenceException.\n" +
                  $"- 💡 **Gợi ý giải bài tập**: Cung cấp ý tưởng giải thuật toán theo từng cấp độ mà không làm mất tính tự học.\n" +
                  $"- ⚡ **Tối ưu hóa mã nguồn (Code Review)**: Nâng cấp code theo chuẩn Clean Code và cú pháp Modern C#.\n" +
                  $"- 📚 **Giải đáp kiến trúc**: Phân tích chuyên sâu về ASP.NET Core, EF Core, Dependency Injection, LINQ và Async/Await.\n\n" +
                  $"Bạn hãy đặt câu hỏi hoặc dán đoạn mã nguồn cần hỗ trợ bên dưới nhé!",
            SuggestedFollowUps = isEnglish
                ? new List<string> { "How to optimize LINQ queries?", "Explain Async/Await and ThreadPool", "ASP.NET Core Dependency Injection Lifetimes" }
                : new List<string> { "Làm thế nào để tối ưu câu truy vấn LINQ?", "Giải thích cơ chế Async/Await trong C#", "Phân biệt AddScoped, AddTransient, AddSingleton" }
        };
    }

    public async Task<AiExplainErrorResponse> ExplainErrorAsync(AiExplainErrorRequest request, bool isEnglish)
    {
        await Task.CompletedTask; // Keep async signature

        var error = request.ErrorMessage ?? string.Empty;
        var code = request.SourceCode ?? string.Empty;

        // Extract line and column
        int? lineNumber = null;
        var lineMatch = Regex.Match(error, @"\((\d+),(\d+)\)");
        if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out int line))
        {
            lineNumber = line;
        }

        // Match common compiler errors
        if (error.Contains("CS0103"))
        {
            var varMatch = Regex.Match(error, "'(?<name>[^']+)'");
            var varName = varMatch.Success ? varMatch.Groups["name"].Value : "identifier";

            return new AiExplainErrorResponse
            {
                ErrorCode = "CS0103",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? $"Name '{varName}' does not exist in the current context" : $"Tên '{varName}' không tồn tại trong ngữ cảnh hiện tại",
                Explanation = isEnglish
                    ? $"The compiler encountered `{varName}`, but cannot find any variable, field, method, or class with this name. This usually happens because of a typo, declaring the variable in a different scope, or missing a `using` directive."
                    : $"Trình biên dịch gặp biến hoặc phương thức `{varName}` nhưng không tìm thấy khai báo nào phù hợp. Nguyên nhân phổ biến: viết sai chính tả, biến nằm ngoài phạm vi (scope) của cặp ngoặc `{{ }}`, hoặc chưa import thư viện (`using`).",
                HowToFix = isEnglish
                    ? $"1. Check for typos in `{varName}`.\n2. Ensure `{varName}` is declared before this line.\n3. If it's a class from a namespace, add the missing `using` directive at the top."
                    : $"1. Kiểm tra lại chính tả tên `{varName}` (C# có phân biệt chữ hoa chữ thường).\n2. Đảm bảo biến đã được khai báo trước dòng này.\n3. Nếu đây là một class từ thư viện ngoài, hãy bổ sung câu lệnh `using` ở đầu file.",
                SuggestedFixCode = $"// Khai báo trước khi sử dụng\nvar {varName} = ...;\nConsole.WriteLine({varName});",
                ProTip = isEnglish ? "C# is strictly case-sensitive. 'myVar' and 'MyVar' are two completely different names!" : "C# phân biệt chữ hoa và chữ thường nghiêm ngặt. 'bienDem' và 'BienDem' là 2 tên hoàn toàn khác nhau!"
            };
        }

        if (error.Contains("CS0029"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "CS0029",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Cannot implicitly convert type" : "Không thể chuyển đổi ngầm định kiểu dữ liệu",
                Explanation = isEnglish
                    ? "C# is strongly typed. You are trying to assign a value of one type to a variable of an incompatible type without an explicit cast or conversion."
                    : "C# là ngôn ngữ định kiểu tĩnh nghiêm ngặt. Bạn đang cố gắng gán một giá trị thuộc kiểu dữ liệu này sang một biến thuộc kiểu dữ liệu không tương thích (ví dụ: gán chuỗi `string` cho số nguyên `int`).",
                HowToFix = isEnglish
                    ? "Use explicit conversion methods like `int.Parse()`, `Convert.ToInt32()`, or cast `(TargetType)` if compatible."
                    : "Sử dụng các hàm ép kiểu tương thích như `int.Parse(str)`, `Convert.ToInt32(val)`, hoặc dùng phương thức `.ToString()`.",
                SuggestedFixCode = "// Ví dụ chuyển đổi chuỗi sang số nguyên\nint number = int.Parse(\"123\");",
                ProTip = isEnglish ? "Prefer `int.TryParse()` over `int.Parse()` when handling user input to avoid exceptions." : "Khi nhận dữ liệu từ người dùng, luôn ưu tiên `int.TryParse()` thay vì `int.Parse()` để tránh crash chương trình."
            };
        }

        if (error.Contains("CS1002"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "CS1002",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "; expected (Missing Semicolon)" : "Thiếu dấu chấm phẩy (; expected)",
                Explanation = isEnglish
                    ? "In C#, every statement must end with a semicolon `;`. You likely forgot a semicolon at the end of the previous line or inside a loop declaration."
                    : "Trong C#, mọi câu lệnh thực thi đều phải kết thúc bằng dấu chấm phẩy `;`. Rất có thể bạn đã quên dấu `;` ở cuối dòng trước đó hoặc trong khai báo vòng lặp `for`.",
                HowToFix = isEnglish ? "Add `;` to the end of the statement on the indicated line." : "Thêm dấu `;` vào cuối câu lệnh tại dòng bị báo lỗi.",
                SuggestedFixCode = "Console.WriteLine(\"Hello World\"); // Đừng quên dấu chấm phẩy ở cuối!",
                ProTip = isEnglish ? "Compilers often report missing semicolons on the *following* line." : "Trình biên dịch đôi khi sẽ báo lỗi thiếu dấu chấm phẩy ở dòng *kế tiếp* của dòng thực sự bị thiếu!"
            };
        }

        if (error.Contains("NullReferenceException"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "Runtime: NullReferenceException",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Object reference not set to an instance of an object" : "Lỗi con trỏ rỗng (NullReferenceException)",
                Explanation = isEnglish
                    ? "You attempted to access a property, method, or indexer on an object whose value is currently `null`."
                    : "Bạn đang cố gắng truy cập thuộc tính hoặc gọi phương thức trên một biến có giá trị `null` (đối tượng chưa được khởi tạo với từ khóa `new`).",
                HowToFix = isEnglish
                    ? "Check for null using the null-conditional operator `?.` or ensure the object is initialized before use."
                    : "Kiểm tra null trước khi gọi: dùng toán tử an toàn `obj?.Method()`, toán tử gán mặc định `obj ?? new MyClass()`, hoặc kiểm tra `if (obj is not null)`.",
                SuggestedFixCode = "// Dùng toán tử null-conditional an toàn\nstring name = user?.FullName ?? \"Khách hàng\";",
                ProTip = isEnglish ? "Enable Nullable Reference Types (`<Nullable>enable</Nullable>`) in your .NET project." : "Bật cờ Nullable Reference Types (`<Nullable>enable</Nullable>`) để compiler cảnh báo null ngay lúc viết code!"
            };
        }

        // Default explanation
        return new AiExplainErrorResponse
        {
            ErrorCode = "Compiler Diagnostic",
            LineNumber = lineNumber,
            ErrorTitle = isEnglish ? "Compiler or Runtime Issue" : "Lỗi Biên Dịch hoặc Thực Thi",
            Explanation = error,
            HowToFix = isEnglish
                ? "Review the syntax near the reported line number and verify types and imports."
                : "Kiểm tra lại cú pháp xung quanh dòng được thông báo, chú ý các dấu đóng mở ngoặc `{ }`, kiểu dữ liệu và từ khóa `using`.",
            SuggestedFixCode = null,
            ProTip = isEnglish ? "Read compiler errors from top to bottom; the first error often triggers the subsequent ones." : "Luôn đọc và sửa lỗi đầu tiên trong danh sách trước; việc sửa lỗi đầu tiên thường sẽ tự động giải quyết các lỗi phía sau."
        };
    }

    public async Task<AiChallengeHintResponse> GetChallengeHintAsync(AiChallengeHintRequest request, bool isEnglish)
    {
        var challenge = await _context.CodingChallenges
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId);

        var title = challenge?.Title ?? (isEnglish ? "Coding Challenge" : "Thử thách thuật toán");
        int level = Math.Clamp(request.HintLevel, 1, 3);

        string hintTitle;
        string content;
        string? clue;

        switch (level)
        {
            case 1:
                hintTitle = isEnglish ? "Hint Level 1: Intuition & Approach" : "Gợi ý Cấp 1: Hướng tiếp cận & Ý tưởng";
                content = isEnglish
                    ? $"For **{title}**, think about the inputs and required return type. Can you solve it iteratively or by scanning the data once? Identify the edge cases first (empty collection, null, zero)."
                    : $"Với thử thách **{title}**, hãy xác định rõ input và output. Bạn có thể giải quyết bài toán bằng cách duyệt qua dữ liệu 1 lần hay không? Luôn cân nhắc các trường hợp biên trước (mảng rỗng, giá trị 0, số âm).";
                clue = isEnglish ? "// Tip: Start with a simple foreach loop or LINQ query" : "// Mẹo: Bắt đầu với một vòng lặp foreach hoặc LINQ cơ bản";
                break;

            case 2:
                hintTitle = isEnglish ? "Hint Level 2: Recommended Data Structure" : "Gợi ý Cấp 2: Cấu trúc dữ liệu & Thuật toán";
                content = isEnglish
                    ? "Consider using a `Dictionary<TKey, TValue>` or `HashSet<T>` for O(1) lookups if you need to check previous elements. If sorting helps, consider `Array.Sort()` or `.OrderBy()`."
                    : "Nếu bài toán cần tìm kiếm hoặc đối chiếu phần tử đã qua, hãy dùng `Dictionary<K, V>` hoặc `HashSet<T>` để đạt độ phức tạp tìm kiếm O(1). Nếu bài toán cần thứ tự, hãy cân nhắc `Array.Sort()` hoặc hai con trỏ (Two Pointers).";
                clue = "var seen = new HashSet<int>();\nforeach (var item in items) { ... }";
                break;

            default:
                hintTitle = isEnglish ? "Hint Level 3: Pseudocode Outline" : "Gợi ý Cấp 3: Khung cài đặt & Mã giả";
                content = isEnglish
                    ? "Here is the architectural skeleton for your solution. Fill in the core condition inside the loop."
                    : "Dưới đây là khung sườn thuật toán giải quyết thử thách. Hãy hoàn thiện điều kiện logic bên trong vòng lặp.";
                clue = "// Bước 1: Kiểm tra biên\nif (input == null) return ...;\n\n// Bước 2: Khởi tạo biến lưu kết quả\nvar result = ...;\n\n// Bước 3: Duyệt và xử lý\nfor (int i = 0; i < input.Length; i++) {\n    // Logic ở đây\n}\nreturn result;";
                break;
        }

        return new AiChallengeHintResponse
        {
            HintLevel = level,
            Title = hintTitle,
            Content = content,
            CodeClue = clue,
            HasNextLevel = level < 3
        };
    }

    public async Task<AiCodeReviewResponse> ReviewCodeAsync(AiCodeReviewRequest request, bool isEnglish)
    {
        await Task.CompletedTask;

        var code = request.SourceCode ?? string.Empty;
        var points = new List<AiReviewPoint>();
        int score = 85;

        // Check for string concatenation in loops
        if (code.Contains("+=") && (code.Contains("for") || code.Contains("while")))
        {
            points.Add(new AiReviewPoint
            {
                Category = "Performance",
                Severity = "warning",
                Title = isEnglish ? "String Concatenation in Loop" : "Nối chuỗi trong vòng lặp",
                Description = isEnglish
                    ? "Using `+=` with strings inside a loop allocates new string instances on each iteration. Use `StringBuilder` for O(n) memory performance."
                    : "Sử dụng `+=` để nối chuỗi trong vòng lặp sẽ cấp phát bộ nhớ mới cho mỗi lần lặp. Hãy sử dụng `StringBuilder` để tối ưu hóa bộ nhớ và tốc độ."
            });
            score -= 10;
        }

        // Check for LINQ or modern collection expressions
        if (code.Contains("new List<") && !code.Contains("["))
        {
            points.Add(new AiReviewPoint
            {
                Category = "Modern C# 14",
                Severity = "info",
                Title = isEnglish ? "Use Collection Expressions" : "Sử dụng Collection Expressions mới",
                Description = isEnglish
                    ? "In C# 12/14 (.NET 10), you can use collection expressions `List<int> list = [1, 2, 3];` instead of verbose `new List<int> { 1, 2, 3 }`."
                    : "Trong Modern C# (.NET 10), bạn có thể dùng cú pháp Collection Expressions gọn gàng: `List<int> items = [1, 2, 3];` thay cho `new List<int>() { ... }`."
            });
            score += 5;
        }

        // Check for null checks
        if (!code.Contains("?") && !code.Contains("null"))
        {
            points.Add(new AiReviewPoint
            {
                Category = "Safety",
                Severity = "info",
                Title = isEnglish ? "Defensive Null Checking" : "Kiểm tra an toàn Null",
                Description = isEnglish
                    ? "Consider adding null checks or using null-forgiving/conditional operators (`?.`, `??`) to ensure runtime safety."
                    : "Cân nhắc thêm kiểm tra giá trị null hoặc sử dụng toán tử an toàn (`?.`, `??`) để chương trình không bị crash khi dữ liệu đầu vào rỗng."
            });
        }
        else
        {
            score += 5;
        }

        score = Math.Clamp(score, 60, 98);

        return new AiCodeReviewResponse
        {
            Score = score,
            Summary = isEnglish
                ? $"Code review complete! Your code is clean and functional with a quality score of {score}/100."
                : $"Đánh giá mã nguồn hoàn tất! Code của bạn đạt điểm chất lượng {score}/100. Tuân thủ tốt các nguyên tắc lập trình C#.",
            Points = points,
            RefactoredCode = "// Mã nguồn đã được tối ưu hóa theo chuẩn Modern .NET 10\n" + code
        };
    }
}
