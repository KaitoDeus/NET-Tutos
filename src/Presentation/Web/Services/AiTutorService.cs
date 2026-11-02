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

        if (error.Contains("CS1061"))
        {
            var methodMatch = Regex.Match(error, "'(?<type>[^']+)' does not contain a definition for '(?<name>[^']+)'");
            var typeName = methodMatch.Success ? methodMatch.Groups["type"].Value : "kiểu dữ liệu";
            var memberName = methodMatch.Success ? methodMatch.Groups["name"].Value : "phương thức";

            return new AiExplainErrorResponse
            {
                ErrorCode = "CS1061",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? $"'{typeName}' does not contain a definition for '{memberName}'" : $"'{typeName}' không chứa định nghĩa cho '{memberName}'",
                Explanation = isEnglish
                    ? $"Type `{typeName}` does not have a member named `{memberName}`. You may have mistyped the method name, called a method on an incorrect type, or missed a LINQ extension method that requires `using System.Linq;`."
                    : $"Kiểu dữ liệu `{typeName}` không có thuộc tính hoặc phương thức mang tên `{memberName}`. Nguyên nhân thường gặp: gõ sai tên hàm, gọi hàm trên sai đối tượng, hoặc quên import `using System.Linq;` để dùng các hàm mở rộng LINQ.",
                HowToFix = isEnglish
                    ? $"1. Verify spelling of `{memberName}`.\n2. If using LINQ (e.g. `.Where()`, `.Select()`), add `using System.Linq;` at the top.\n3. Check if `{typeName}` exposes this member publicly."
                    : $"1. Kiểm tra chính tả của `{memberName}` (viết hoa/thường).\n2. Nếu dùng hàm LINQ (như `.Where()`, `.Select()`, `.ToList()`), thêm `using System.Linq;` ở đầu file.\n3. Kiểm tra xem class `{typeName}` đã khai báo hàm này với phạm vi `public` chưa.",
                SuggestedFixCode = "using System.Linq; // Bổ sung thư viện LINQ nếu gọi các hàm tập hợp",
                ProTip = isEnglish ? "Extension methods require their defining namespace to be imported via 'using'." : "Các Extension Method (như LINQ) bắt buộc phải import namespace chứa nó thì mới hiển thị trong gợi ý mã!"
            };
        }

        if (error.Contains("CS1503"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "CS1503",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Argument Type Mismatch" : "Sai lệch kiểu dữ liệu tham số đầu vào (Argument Type Mismatch)",
                Explanation = isEnglish
                    ? "The parameter type expected by the method does not match the argument you supplied. C# does not implicitly convert incompatible types."
                    : "Kiểu dữ liệu của đối số bạn truyền vào không khớp với kiểu dữ liệu mà phương thức yêu cầu. C# không tự động ép kiểu giữa các kiểu dữ liệu không tương thích.",
                HowToFix = isEnglish
                    ? "Check the method signature and convert your argument explicitly (e.g., `.ToString()`, `int.Parse()`, or `(TargetType)`)."
                    : "Xem kỹ định nghĩa phương thức và thực hiện ép kiểu rõ ràng (ví dụ: dùng `.ToString()`, `int.Parse()`, hoặc `Convert.ToInt32()`).",
                SuggestedFixCode = "// Ví dụ chuyển đổi kiểu dữ liệu tương thích:\nvar result = MyMethod(int.Parse(inputString));",
                ProTip = isEnglish ? "Hover over the method name in the editor to inspect the required parameter types." : "Rê chuột vào tên phương thức trong editor để xem đúng kiểu dữ liệu của các tham số yêu cầu."
            };
        }

        if (error.Contains("CS0161"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "CS0161",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Not all code paths return a value" : "Không phải mọi nhánh rẽ đều trả về giá trị (Missing return)",
                Explanation = isEnglish
                    ? "Your method declares a non-void return type, but there is at least one execution path (such as an `if/else` branch or after a loop) that terminates without a `return` statement."
                    : "Phương thức của bạn khai báo kiểu trả về (không phải `void`), nhưng có ít nhất một nhánh rẽ logic (như nhánh `if/else` hoặc sau khi kết thúc vòng lặp) kết thúc mà không có lệnh `return`.",
                HowToFix = isEnglish
                    ? "Ensure every possible logical path terminates with a `return` statement, or add a default fallback `return` at the bottom of the method."
                    : "Đảm bảo mọi nhánh `if/else` đều có lệnh `return`, hoặc đặt một giá trị `return` mặc định ở cuối hàm.",
                SuggestedFixCode = "// Đặt return mặc định ở cuối hàm phòng trường hợp không lọt vào if:\nreturn defaultValue;",
                ProTip = isEnglish ? "If a path should never be reached, you can throw an exception: 'throw new InvalidOperationException();'" : "Nếu một nhánh logic không bao giờ xảy ra, bạn có thể ném ngoại lệ: 'throw new InvalidOperationException();'"
            };
        }

        if (error.Contains("CS0246"))
        {
            var typeMatch = Regex.Match(error, "'(?<type>[^']+)'");
            var missingType = typeMatch.Success ? typeMatch.Groups["type"].Value : "Type";

            return new AiExplainErrorResponse
            {
                ErrorCode = "CS0246",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? $"Type or namespace '{missingType}' could not be found" : $"Không tìm thấy kiểu hoặc namespace '{missingType}'",
                Explanation = isEnglish
                    ? $"The compiler cannot find the declaration for `{missingType}`. Are you missing a `using` directive or is there a spelling mistake?"
                    : $"Trình biên dịch không tìm thấy định nghĩa cho `{missingType}`. Nguyên nhân thường do thiếu câu lệnh `using` hoặc viết sai chính tả tên Class/Interface.",
                HowToFix = isEnglish
                    ? $"1. Verify the spelling of `{missingType}`.\n2. Add the corresponding `using` directive (e.g., `using System.Collections.Generic;`)."
                    : $"1. Kiểm tra chính tả `{missingType}`.\n2. Bổ sung `using` tương ứng (ví dụ: `using System.Collections.Generic;` cho List, Dictionary).",
                SuggestedFixCode = $"using System.Collections.Generic;\nusing System.Text;",
                ProTip = isEnglish ? "Check standard namespaces: System, System.Collections.Generic, System.Linq, System.Text." : "Các namespace chuẩn hay dùng nhất trong C#: System, System.Collections.Generic, System.Linq, System.Text."
            };
        }

        if (error.Contains("IndexOutOfRangeException"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "Runtime: IndexOutOfRangeException",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Index was outside the bounds of the array" : "Chỉ số mảng vượt ngoài phạm vi (IndexOutOfRangeException)",
                Explanation = isEnglish
                    ? "You tried to access an element of an array or list using an index that is negative or greater than or equal to `Length` / `Count`."
                    : "Bạn đang cố gắng truy cập phần tử mảng hoặc danh sách bằng một chỉ số (index) âm hoặc lớn hơn hoặc bằng độ dài `Length` / `Count` của mảng.",
                HowToFix = isEnglish
                    ? "In C#, array indexing is 0-based (0 to Length - 1). Check loop bounds: use `i < array.Length` instead of `i <= array.Length`."
                    : "Trong C#, mảng tính từ chỉ số 0 đến Length - 1. Kiểm tra điều kiện vòng lặp: dùng `i < array.Length` thay vì `i <= array.Length`.",
                SuggestedFixCode = "// Sửa điều kiện vòng lặp duyệt mảng:\nfor (int i = 0; i < array.Length; i++) { ... }",
                ProTip = isEnglish ? "Avoid manual index loops when possible: use 'foreach' or LINQ to completely prevent off-by-one errors." : "Ưu tiên dùng 'foreach' hoặc LINQ khi có thể để tránh hoàn toàn lỗi sai lệch chỉ số off-by-one."
            };
        }

        if (error.Contains("DivideByZeroException"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "Runtime: DivideByZeroException",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Attempted to divide by zero" : "Phép chia cho số 0 (DivideByZeroException)",
                Explanation = isEnglish
                    ? "Integer division by zero (`x / 0` or `x % 0`) throws a `DivideByZeroException` in C#."
                    : "Trong C#, thực hiện phép chia lấy phần nguyên hoặc chia lấy dư cho số 0 (`x / 0` hoặc `x % 0`) với số nguyên sẽ gây lỗi nghiêm trọng.",
                HowToFix = isEnglish
                    ? "Validate that the denominator is not zero before performing the division: `if (denominator != 0)`."
                    : "Kiểm tra mẫu số khác 0 trước khi thực hiện phép chia: `if (denominator != 0) { ... }`.",
                SuggestedFixCode = "int result = denominator != 0 ? numerator / denominator : 0;",
                ProTip = isEnglish ? "Floating point division (double/float) returns Infinity or NaN instead of throwing, but integer division always throws!" : "Phép chia với kiểu float/double sẽ trả về Infinity thay vì ném lỗi, nhưng với số nguyên int/long sẽ luôn crash chương trình!"
            };
        }

        if (error.Contains("Timeout") || error.Contains("timed out") || error.Contains("quá thời gian"))
        {
            return new AiExplainErrorResponse
            {
                ErrorCode = "Runtime: Execution Timeout",
                LineNumber = lineNumber,
                ErrorTitle = isEnglish ? "Code Execution Timed Out (Possible Infinite Loop)" : "Quá thời gian thực thi (Nghi vấn Vòng lặp Vô tận)",
                Explanation = isEnglish
                    ? "Your code took longer than the maximum allowed execution threshold (3000ms). This almost always indicates an infinite `while` loop or un-terminating recursion."
                    : "Chương trình của bạn chạy quá thời gian cho phép (3000ms). Điều này gần như chắc chắn do vòng lặp `while` không có điều kiện dừng hoặc hàm đệ quy không có điểm neo dừng (base case).",
                HowToFix = isEnglish
                    ? "1. Check your `while` loop condition to make sure the loop variable is incremented/decremented.\n2. Ensure recursive methods have a base case that is always reached."
                    : "1. Kiểm tra vòng lặp `while`: đảm bảo biến điều kiện được tăng/giảm ở mỗi lượt lặp.\n2. Kiểm tra hàm đệ quy: đảm bảo có điều kiện dừng (base case) hợp lệ trước khi gọi đệ quy tiếp.",
                SuggestedFixCode = "// Đảm bảo biến đếm được cập nhật trong while loop:\nwhile (left < right) {\n    left++; // Đừng quên cập nhật con trỏ!\n}",
                ProTip = isEnglish ? "Always trace loop variables on small inputs with pencil and paper to ensure termination." : "Luôn nhẩm chạy thử vòng lặp với 1 ví dụ nhỏ (2-3 phần tử) để chắc chắn điều kiện dừng hoạt động chính xác."
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

    public async Task<AiComplexityResponse> AnalyzeComplexityAsync(AiComplexityRequest request, bool isEnglish)
    {
        await Task.CompletedTask;
        var code = request.SourceCode ?? string.Empty;

        // Detect loops, nesting, sorting, recursion
        int loopCount = Regex.Matches(code, @"\b(for|while|foreach)\b").Count;
        bool hasNestedLoop = Regex.IsMatch(code, @"(for|while|foreach)[^{]*\{[^{}]*(for|while|foreach)");
        bool hasSorting = code.Contains(".OrderBy") || code.Contains(".Sort(") || code.Contains("Array.Sort");
        bool hasRecursion = Regex.IsMatch(code, @"\b([a-zA-Z0-9_]+)\s*\([^)]*\)[^{]*\{[^{}]*\b\1\s*\(");
        bool hasHashLookup = code.Contains("Dictionary<") || code.Contains("HashSet<");
        bool hasArrayAllocation = code.Contains("new int[") || code.Contains("new string[") || code.Contains("new List<");

        string timeComplexity;
        string spaceComplexity;
        int score = 85;
        string summary;
        string detailedExplanation;
        string? optimizationTip = null;
        string? optimalComparison = null;

        if (hasNestedLoop)
        {
            timeComplexity = "O(N²)";
            score = 65;
            summary = isEnglish
                ? "Quadratic Time Complexity detected due to nested iterations."
                : "Phát hiện độ phức tạp thời gian bậc hai O(N²) do vòng lặp lồng nhau (nested loops).";
            detailedExplanation = isEnglish
                ? "The algorithm contains an outer loop that iterates over the collection and an inner loop for each element, leading to N * N operations in the worst case."
                : "Thuật toán chứa một vòng lặp ngoài duyệt qua các phần tử và một vòng lặp trong quét lại tập dữ liệu, dẫn đến khoảng N * N phép tính trong trường hợp xấu nhất.";
            optimizationTip = isEnglish
                ? "Can you trade space for time? Using a `Dictionary<TKey, TValue>` or `HashSet<T>` allows you to reduce lookups from O(N) to O(1), bringing total time down to O(N)."
                : "Bạn có thể đánh đổi bộ nhớ để lấy tốc độ: Sử dụng `Dictionary<TKey, TValue>` hoặc `HashSet<T>` để giảm thời gian tìm kiếm từ O(N) xuống O(1), đưa toàn bộ thuật toán về O(N).";
            optimalComparison = isEnglish
                ? "Optimal solution for this problem usually achieves O(N) time with O(N) hash-table space."
                : "Giải pháp tối ưu cho dạng bài này thường đạt độ phức tạp O(N) thời gian kết hợp O(N) không gian bảng băm.";
        }
        else if (hasSorting)
        {
            timeComplexity = "O(N log N)";
            score = 80;
            summary = isEnglish
                ? "Log-linear Time Complexity detected due to sorting operation."
                : "Độ phức tạp thời gian O(N log N) do sử dụng thao tác sắp xếp (Sorting).";
            detailedExplanation = isEnglish
                ? "Sorting the collection requires O(N log N) time in .NET using QuickSort/IntroSort under the hood."
                : "Thao tác sắp xếp mảng/danh sách trong .NET sử dụng thuật toán IntroSort/QuickSort có độ phức tạp trung bình là O(N log N).";
            optimizationTip = isEnglish
                ? "If the array is already sorted or if values are in a bounded range, a two-pointer technique or frequency map might avoid sorting."
                : "Nếu mảng đã được sắp xếp trước hoặc miền giá trị nhỏ, kỹ thuật hai con trỏ (Two Pointers) hoặc mảng đếm tần suất có thể giúp giảm xuống O(N).";
        }
        else if (hasRecursion)
        {
            timeComplexity = "O(2ⁿ) hoặc O(N)";
            score = 70;
            summary = isEnglish
                ? "Recursive Call Tree detected."
                : "Phát hiện hàm đệ quy (Recursion).";
            detailedExplanation = isEnglish
                ? "Without memoization, tree-based recursion can grow exponentially O(2ⁿ). If using tail recursion or linear recursion, it is O(N)."
                : "Nếu là đệ quy nhánh mà không dùng ghi nhớ (Memoization/Quy hoạch động), số phép tính có thể bùng nổ hàm mũ O(2ⁿ). Nếu đệ quy tuyến tính thì đạt O(N).";
            optimizationTip = isEnglish
                ? "Consider dynamic programming (tabulation or memoization) to store subproblem answers."
                : "Cân nhắc áp dụng Quy hoạch động (Dynamic Programming - lưu mảng kết quả tạm thời) để tránh tính lặp các bài toán con.";
        }
        else if (loopCount > 0)
        {
            timeComplexity = "O(N)";
            score = 92;
            summary = isEnglish
                ? "Linear Time Complexity — highly efficient single pass!"
                : "Độ phức tạp thời gian tuyến tính O(N) — thuật toán duyệt 1 lần rất tối ưu!";
            detailedExplanation = isEnglish
                ? "The algorithm scans the collection in a single pass. For an input of size N, the number of operations scales linearly."
                : "Thuật toán quét qua tập dữ liệu trong một lượt duyệt duy nhất. Khi kích thước dữ liệu tăng N lần, số phép tính chỉ tăng tương ứng N lần.";
            optimalComparison = isEnglish
                ? "This is already in the optimal time complexity tier for this problem class."
                : "Đây đã là cấp độ thời gian tối ưu hàng đầu cho dạng bài toán này.";
        }
        else
        {
            timeComplexity = "O(1)";
            score = 98;
            summary = isEnglish
                ? "Constant Time Complexity — instantaneous execution!"
                : "Độ phức tạp thời gian hằng số O(1) — tốc độ tức thì!";
            detailedExplanation = isEnglish
                ? "The code performs a fixed set of mathematical or bitwise operations without dependent loops."
                : "Đoạn code chỉ thực hiện các phép toán hoặc logic kiểm tra trực tiếp mà không phụ thuộc vào độ dài dữ liệu.";
        }

        // Space complexity calculation
        if (hasHashLookup || hasArrayAllocation)
        {
            spaceComplexity = "O(N)";
            if (hasNestedLoop) score = Math.Max(score, 60);
        }
        else
        {
            spaceComplexity = "O(1)";
            score += 5;
        }

        score = Math.Clamp(score, 50, 100);

        return new AiComplexityResponse
        {
            TimeComplexity = timeComplexity,
            SpaceComplexity = spaceComplexity,
            EfficiencyScore = score,
            Summary = summary,
            DetailedExplanation = detailedExplanation,
            OptimizationTip = optimizationTip,
            OptimalComparison = optimalComparison
        };
    }

    public async Task<AiDiagnoseTestFailureResponse> DiagnoseTestFailureAsync(AiDiagnoseTestFailureRequest request, bool isEnglish)
    {
        await Task.CompletedTask;
        var code = request.SourceCode ?? string.Empty;
        var input = request.InputParameters ?? string.Empty;
        var expected = request.ExpectedOutput ?? string.Empty;
        var actual = request.ActualOutput ?? string.Empty;

        string category = "Logic Flaw";
        string title;
        string analysis;
        string hint;
        string? snippet = null;

        // Pattern 1: Empty or boundary input
        if (input.Contains("[]") || input.Contains("\"\"") || input == "0" || input == "null")
        {
            category = "Edge Case";
            title = isEnglish ? "Unhandled Boundary / Empty Input Edge Case" : "Chưa xử lý trường hợp biên (Mảng rỗng / Giá trị 0 / Chuỗi rỗng)";
            analysis = isEnglish
                ? $"The test case provides an edge case input `{input}`, but your code produced `{actual}` instead of `{expected}`. Boundary conditions often bypass loop bodies or cause unexpected default returns."
                : $"Bộ test case đưa vào trường hợp biên `{input}`, nhưng code của bạn lại trả về `{actual}` thay vì kỳ vọng `{expected}`. Các trường hợp biên thường không đi vào vòng lặp hoặc trả về giá trị mặc định chưa đúng.";
            hint = isEnglish
                ? "Add guard clauses at the beginning of your method to handle empty collections, nulls, or zero values explicitly."
                : "Hãy thêm câu lệnh bảo vệ (Guard Clause) ở ngay đầu phương thức để kiểm tra mảng rỗng, chuỗi rỗng hoặc số 0.";
            snippet = isEnglish
                ? "// Guard clause for edge case:\nif (input == null || input.Length == 0) return default;"
                : "// Kiểm tra trường hợp biên ngay đầu hàm:\nif (input == null || input.Length == 0) return default;";
        }
        // Pattern 2: Off by one (Difference of 1 in count or indexing)
        else if (int.TryParse(expected, out int expNum) && int.TryParse(actual, out int actNum) && Math.Abs(expNum - actNum) == 1)
        {
            category = "Off-by-one";
            title = isEnglish ? "Off-by-one Loop Boundary Error" : "Lỗi sai lệch 1 đơn vị (Off-by-one Boundary Error)";
            analysis = isEnglish
                ? $"The actual output `{actual}` differs from expected `{expected}` by exactly 1. This is a classic off-by-one indexing or loop counter mistake (e.g., using `<=` instead of `<` or starting from `1` instead of `0`)."
                : $"Kết quả thực tế `{actual}` chỉ chênh lệch đúng 1 đơn vị so với kỳ vọng `{expected}`. Đây là lỗi kinh điển 'Off-by-one' trong vòng lặp hoặc điều kiện đếm (ví dụ: dùng `<=` thay vì `<`, hoặc bắt đầu từ `1` thay vì `0`).";
            hint = isEnglish
                ? "Check your loop termination condition: `i < length` vs `i <= length`, and whether the counter should start at 0 or 1."
                : "Kiểm tra lại điều kiện dừng của vòng lặp: `i < n` hay `i <= n`, cũng như biến đếm bắt đầu từ 0 hay 1.";
            snippet = isEnglish
                ? "// Check boundary operator:\nfor (int i = 0; i < n; i++) // Not i <= n"
                : "// Kiểm tra toán tử biên vòng lặp:\nfor (int i = 0; i < n; i++) // Thay vì i <= n";
        }
        // Pattern 3: Case sensitivity in string operations
        else if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase) && !expected.Equals(actual))
        {
            category = "Case Sensitivity";
            title = isEnglish ? "String Case Sensitivity Mismatch" : "Sai lệch chữ hoa / chữ thường trong chuỗi";
            analysis = isEnglish
                ? $"Your output matches the expected result except for letter casing. Expected: `{expected}`, but got `{actual}`."
                : $"Kết quả của bạn khớp với kỳ vọng nhưng bị sai lệch về chữ hoa/chữ thường. Kỳ vọng: `{expected}`, nhưng thực tế ra: `{actual}`.";
            hint = isEnglish
                ? "Normalize strings using `.ToLowerInvariant()` or `.ToUpperInvariant()` before comparing or building the return value."
                : "Hãy chuẩn hóa chuỗi bằng `.ToLowerInvariant()` hoặc dùng `StringComparison.OrdinalIgnoreCase` khi so sánh.";
            snippet = "text.ToLowerInvariant();";
        }
        // Pattern 4: Duplicate element handling (e.g., Two Sum reusing same index)
        else if (code.Contains("==") && (code.Contains("nums") || code.Contains("array")))
        {
            category = "Duplicate Elements";
            title = isEnglish ? "Duplicate Elements or Index Reuse" : "Xử lý phần tử trùng lặp hoặc tái sử dụng cùng vị trí index";
            analysis = isEnglish
                ? $"With input `{input}`, expected output was `{expected}`, but received `{actual}`. Check if your logic accidentally pairs an element with itself or fails when duplicate numbers appear in the input."
                : $"Với dữ liệu `{input}`, kết quả mong muốn là `{expected}` nhưng lại ra `{actual}`. Rất có thể thuật toán đang vô tình ghép đôi một phần tử với chính nó (`i == j`), hoặc chưa xử lý trường hợp 2 số trùng nhau trong mảng.";
            hint = isEnglish
                ? "Ensure `i != j` when searching for pairs, or when using a Dictionary, verify you don't return the same index as the current element."
                : "Đảm bảo điều kiện `i != j` khi tìm kiếm cặp phần tử, hoặc khi dùng Dictionary, kiểm tra `map[complement] != i`.";
            snippet = "if (map.ContainsKey(complement) && map[complement] != i) {\n    return [map[complement], i];\n}";
        }
        else
        {
            title = isEnglish ? $"Logic Discrepancy on Test #{request.TestIndex}" : $"Sai lệch logic xử lý tại Test Case #{request.TestIndex}";
            analysis = isEnglish
                ? $"For input `{input}`, the problem expects `{expected}`, but your code produced `{actual}`. This points to a mismatch in the core condition or state update inside your algorithm."
                : $"Với đầu vào `{input}`, bài toán yêu cầu kết quả `{expected}`, nhưng code của bạn tính ra `{actual}`. Điều này chứng tỏ điều kiện rẽ nhánh hoặc biến cập nhật trạng thái trong thuật toán chưa bao quát hết kịch bản này.";
            hint = isEnglish
                ? "Trace your code step-by-step with this specific input. Write down variable values at each iteration to observe where the logic deviates."
                : "Hãy chạy nhẩm từng dòng code với đúng bộ input này. Ghi lại giá trị các biến ở từng vòng lặp để xem biến bắt đầu lệch khỏi kỳ vọng ở dòng nào.";
            snippet = null;
        }

        return new AiDiagnoseTestFailureResponse
        {
            FailureCategory = category,
            DiagnosticTitle = title,
            Analysis = analysis,
            LikelyFaultyCodeSegment = null,
            GuidanceHint = hint,
            SuggestedFixSnippet = snippet
        };
    }

    public async Task<AiGenerateUnitTestResponse> GenerateUnitTestAsync(AiGenerateUnitTestRequest request, bool isEnglish)
    {
        await Task.CompletedTask;
        var code = request.SourceCode ?? string.Empty;

        // Extract method name
        var methodMatch = Regex.Match(code, @"(?:public|private|protected|internal|static|\s)+\s+([a-zA-Z0-9_<>]+)\s+([a-zA-Z0-9_]+)\s*\(([^)]*)\)");
        string methodName = methodMatch.Success ? methodMatch.Groups[2].Value : "ExecuteSolution";
        string returnType = methodMatch.Success ? methodMatch.Groups[1].Value : "var";

        var scenarios = isEnglish
            ? new List<string>
            {
                "Valid standard inputs (Happy Path)",
                "Empty collection or boundary values (Edge Case)",
                "Large numbers or negative values (Stress Test)"
            }
            : new List<string>
            {
                "Trường hợp dữ liệu chuẩn hợp lệ (Happy Path)",
                "Trường hợp mảng rỗng hoặc giá trị biên (Edge Case)",
                "Trường hợp số âm hoặc giá trị cực trị (Stress Test)"
            };

        var unitTestCode = $@"using Xunit;

public class {methodName}Tests
{{
    [Fact]
    public void {methodName}_StandardInput_ReturnsExpectedResult()
    {{
        // 1. Arrange (Chuẩn bị dữ liệu)
        // TODO: Khai báo tham số đầu vào

        // 2. Act (Thực thi hàm)
        // {returnType} actual = Solution.{methodName}(...);

        // 3. Assert (Kiểm tra kết quả)
        // Assert.NotNull(actual);
    }}

    [Theory]
    [InlineData(/* input1, expected1 */)]
    [InlineData(/* input2, expected2 */)]
    public void {methodName}_ParameterizedInputs_MatchesExpectedOutput()
    {{
        // Kiểm thử tham số hóa cho các kịch bản biên
    }}

    [Fact]
    public void {methodName}_NullOrEmptyBoundary_HandlesGracefully()
    {{
        // Đảm bảo không ném ngoại lệ ngoài ý muốn khi input rỗng
    }}
}}";

        return new AiGenerateUnitTestResponse
        {
            TestFramework = "xUnit (.NET 10)",
            TestCode = unitTestCode,
            Explanation = isEnglish
                ? $"Generated clean xUnit test suite for `{methodName}` following the AAA (Arrange-Act-Assert) pattern and data-driven `[Theory]` attributes."
                : $"Bộ kiểm thử xUnit được sinh tự động cho phương thức `{methodName}` tuân thủ mô hình AAA (Arrange-Act-Assert) và kiểm thử tham số hóa `[Theory]`.",
            CoveredScenarios = scenarios
        };
    }

    public async Task<AiInterviewEvaluationResponse> EvaluateInterviewAnswerAsync(AiInterviewEvaluationRequest request, bool isEnglish)
    {
        var flashcard = request.FlashcardId > 0
            ? await _context.InterviewFlashcards.FirstOrDefaultAsync(f => f.Id == request.FlashcardId)
            : null;

        var answer = (request.CandidateAnswer ?? string.Empty).Trim();
        var lowerAnswer = answer.ToLowerInvariant();
        var question = flashcard != null ? (isEnglish ? flashcard.QuestionEn : flashcard.QuestionVi) : request.Question;
        var benchmarkAnswer = flashcard != null ? (isEnglish ? flashcard.AnswerMarkdownEn : flashcard.AnswerMarkdownVi) : string.Empty;

        // Base checks
        if (string.IsNullOrWhiteSpace(answer) || answer.Length < 15)
        {
            return new AiInterviewEvaluationResponse
            {
                Success = true,
                FlashcardId = request.FlashcardId,
                OverallScore = 25,
                TechnicalAccuracyScore = 20,
                ClarityStructureScore = 30,
                DepthPracticalScore = 20,
                PerformanceRating = isEnglish ? "Incomplete / Too Brief" : "Chưa hoàn chỉnh / Quá ngắn",
                Feedback = isEnglish
                    ? "Your answer is too brief or empty. In a real technical interview, you should outline the core concept, internal working mechanism, and practical caveats."
                    : "Câu trả lời của bạn quá ngắn hoặc chưa nêu được luận điểm. Trong phỏng vấn thực tế, hãy trình bày: Khái niệm cốt lõi -> Cơ chế bên dưới -> Kinh nghiệm / lưu ý thực tế.",
                Strengths = isEnglish
                    ? new List<string> { "Attempted the question." }
                    : new List<string> { "Đã tiếp nhận câu hỏi của phỏng vấn viên." },
                MissingPoints = isEnglish
                    ? new List<string> { "Did not explain core technical definitions.", "Lacks implementation details or real-world example.", "No edge case or trade-off analysis." }
                    : new List<string> { "Chưa định nghĩa rõ bản chất kỹ thuật.", "Thiếu ví dụ code hoặc cơ chế hoạt động thực tế.", "Chưa nêu các bẫy hiệu năng hoặc trường hợp biên." },
                FollowUpQuestion = isEnglish
                    ? "Could you elaborate with a concrete scenario in ASP.NET Core?"
                    : "Bạn có thể giải thích chi tiết hơn kèm theo một ví dụ tình huống thực tế trong dự án ASP.NET Core không?",
                ModelAnswerSample = benchmarkAnswer
            };
        }

        // Domain Keyword Matcher
        int matchCount = 0;
        var detectedStrengths = new List<string>();
        var missingAspects = new List<string>();

        // Domain-specific concept checks
        var topic = flashcard?.Topic.ToString() ?? request.Topic;
        if (topic.Contains("Async", StringComparison.OrdinalIgnoreCase) || lowerAnswer.Contains("task") || lowerAnswer.Contains("async"))
        {
            if (lowerAnswer.Contains("non-blocking") || lowerAnswer.Contains("không chặn") || lowerAnswer.Contains("không block"))
            { matchCount++; detectedStrengths.Add(isEnglish ? "Accurately noted that async/await is non-blocking." : "Nhận định chính xác async/await là cơ chế non-blocking (không chặn thread UI/Request)."); }
            else { missingAspects.Add(isEnglish ? "Should emphasize non-blocking nature for thread pool scalability." : "Nên nhấn mạnh cơ chế giải phóng thread về ThreadPool để tăng khả năng chịu tải."); }

            if (lowerAnswer.Contains("state machine") || lowerAnswer.Contains("máy trạng thái") || lowerAnswer.Contains("statemachine"))
            { matchCount += 2; detectedStrengths.Add(isEnglish ? "Demonstrated deep internals knowledge: compiler generates an async StateMachine struct." : "Hiểu sâu kiến trúc nội tại: Compiler sinh struct State Machine để điều phối các await continuation."); }
            else { missingAspects.Add(isEnglish ? "Mentioning compiler State Machine generation demonstrates Senior depth." : "Nên đề cập đến việc Roslyn Compiler dịch async thành một struct State Machine."); }

            if (lowerAnswer.Contains("cancellationtoken") || lowerAnswer.Contains("configureawait") || lowerAnswer.Contains("deadlock") || lowerAnswer.Contains("result"))
            { matchCount += 2; detectedStrengths.Add(isEnglish ? "Highlighted real-world caveats (CancellationToken / deadlock prevention / ConfigureAwait)." : "Chỉ ra được kinh nghiệm thực chiến: xử lý CancellationToken hoặc phòng chống Deadlock khi dùng .Result."); }
            else { missingAspects.Add(isEnglish ? "Consider discussing CancellationToken propagation and why `.Result` / `.Wait()` causes deadlocks." : "Cần lưu ý thêm việc truyền CancellationToken và lý do tại sao không bao giờ nên dùng `.Result` / `.Wait()`."); }
        }
        else if (topic.Contains("EfCore", StringComparison.OrdinalIgnoreCase) || lowerAnswer.Contains("query") || lowerAnswer.Contains("ef") || lowerAnswer.Contains("database"))
        {
            if (lowerAnswer.Contains("asnotracking") || lowerAnswer.Contains("tracking") || lowerAnswer.Contains("theo dõi"))
            { matchCount += 2; detectedStrengths.Add(isEnglish ? "Correctly explained EF Core Change Tracker and AsNoTracking performance gains." : "Nắm rõ cơ chế Change Tracker và lợi ích giảm cấp phát bộ nhớ khi dùng AsNoTracking."); }
            else { missingAspects.Add(isEnglish ? "Highlight AsNoTracking for read-only queries to prevent memory overhead." : "Cần nhấn mạnh việc dùng AsNoTracking cho các truy vấn chỉ đọc (read-only) để tránh overhead."); }

            if (lowerAnswer.Contains("n+1") || lowerAnswer.Contains("include") || lowerAnswer.Contains("eager") || lowerAnswer.Contains("lazy"))
            { matchCount += 2; detectedStrengths.Add(isEnglish ? "Addressed relational loading strategies (Eager vs Lazy vs Explicit) and N+1 pitfall." : "Phân tích sắc sảo chiến lược nạp dữ liệu (Eager Loading với Include) và cách tránh bẫy N+1."); }
            else { missingAspects.Add(isEnglish ? "Explain how to prevent the N+1 query problem using `.Include()` or projection." : "Nên giải thích cách phòng tránh lỗi N+1 Query bằng `.Include()` hoặc Projection (`.Select()`)."); }

            if (lowerAnswer.Contains("iqueryable") || lowerAnswer.Contains("sql") || lowerAnswer.Contains("server"))
            { matchCount++; detectedStrengths.Add(isEnglish ? "Clear distinction between IQueryable (server-side evaluation) and IEnumerable." : "Phân biệt rõ IQueryable thực thi phía SQL Server vs IEnumerable xử lý in-memory."); }
        }
        else if (topic.Contains("CSharp", StringComparison.OrdinalIgnoreCase) || lowerAnswer.Contains("memory") || lowerAnswer.Contains("heap") || lowerAnswer.Contains("stack"))
        {
            if (lowerAnswer.Contains("heap") && lowerAnswer.Contains("stack"))
            { matchCount += 2; detectedStrengths.Add(isEnglish ? "Clearly distinguished Stack (short-lived, LIFO) and Heap (managed memory, GC)." : "Phân biệt rành mạch giữa bộ nhớ Stack (vòng đời ngắn, LIFO) và Heap (bộ nhớ được GC quản lý)."); }
            else { missingAspects.Add(isEnglish ? "Elaborate on Stack vs Heap allocation differences." : "Cần làm rõ sự khác biệt giữa Stack (Value Types) và Managed Heap (Reference Types)."); }

            if (lowerAnswer.Contains("gc") || lowerAnswer.Contains("garbage collection") || lowerAnswer.Contains("thu gom"))
            { matchCount++; detectedStrengths.Add(isEnglish ? "Correctly referenced .NET Garbage Collection lifecycle." : "Liên hệ đúng với vòng đời dọn rác của .NET Garbage Collector (Gen 0, 1, 2)."); }

            if (lowerAnswer.Contains("idisposable") || lowerAnswer.Contains("using") || lowerAnswer.Contains("unmanaged"))
            { matchCount += 2; detectedStrengths.Add(isEnglish ? "Strong handling of unmanaged resources via IDisposable and `using` pattern." : "Nhấn mạnh xử lý tài nguyên unmanaged với IDisposable và cú pháp using / IAsyncDisposable."); }
            else { missingAspects.Add(isEnglish ? "Mention IDisposable pattern for deterministic resource cleanup." : "Nên nhắc tới pattern IDisposable để dọn dẹp tài nguyên unmanaged tức thì."); }
        }
        else
        {
            // General .NET & Architecture
            if (lowerAnswer.Contains("interface") || lowerAnswer.Contains("dependency injection") || lowerAnswer.Contains("di") || lowerAnswer.Contains("loosely"))
            { matchCount += 2; detectedStrengths.Add(isEnglish ? "Expressed good grasp of Inversion of Control and Dependency Injection." : "Nắm vững nguyên lý Dependency Injection và Loose Coupling."); }

            if (lowerAnswer.Contains("solid") || lowerAnswer.Contains("single responsibility") || lowerAnswer.Contains("clean"))
            { matchCount++; detectedStrengths.Add(isEnglish ? "Applied SOLID software design principles in rationale." : "Vận dụng đúng các nguyên lý thiết kế SOLID."); }
        }

        // Word count and elaboration bonus
        int wordCount = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        int lengthBonus = Math.Clamp(wordCount / 5, 0, 20);

        int techScore = Math.Clamp(40 + (matchCount * 12) + (lengthBonus / 2), 45, 98);
        int clarityScore = Math.Clamp(45 + (wordCount >= 30 ? 25 : wordCount / 2) + (answer.Contains('.') ? 15 : 5), 50, 96);
        int depthScore = Math.Clamp(35 + (matchCount * 14) + (lengthBonus / 2), 40, 95);

        // Weighted Overall
        int overallScore = (int)Math.Round((techScore * 0.5) + (clarityScore * 0.25) + (depthScore * 0.25));

        string rating;
        if (overallScore >= 85) rating = isEnglish ? "Senior .NET Specialist (Outstanding)" : "Trình độ Senior .NET (Xuất sắc)";
        else if (overallScore >= 72) rating = isEnglish ? "Strong Middle Developer (Confident)" : "Trình độ Middle .NET (Khá vững)";
        else if (overallScore >= 55) rating = isEnglish ? "Junior / Developing Knowledge" : "Trình độ Junior (Đang phát triển)";
        else rating = isEnglish ? "Fundamental Gaps Detected" : "Cần củng cố kiến thức nền tảng";

        // Fallback default strengths / missing if empty
        if (!detectedStrengths.Any())
        {
            detectedStrengths.Add(isEnglish ? "Addressed the core intent of the question." : "Nêu được khái niệm sơ bộ của chủ đề.");
            detectedStrengths.Add(isEnglish ? "Good conversational tone for interview setting." : "Diễn đạt tự nhiên, phù hợp với phong thái phỏng vấn.");
        }
        if (!missingAspects.Any())
        {
            missingAspects.Add(isEnglish ? "Add a concrete code snippet or benchmark comparison to stand out as a Lead/Senior." : "Nêu thêm số liệu đo lường Benchmark hoặc ví dụ code trực quan để gây ấn tượng mạnh với Lead.");
        }

        // Tailored Follow-up Question
        string followUp;
        if (topic.Contains("Async", StringComparison.OrdinalIgnoreCase))
        {
            followUp = isEnglish
                ? "Follow-up Challenge: What occurs if an unhandled exception is thrown inside `async void` versus `async Task`, and how does ASP.NET Core process it?"
                : "Câu hỏi xoáy từ Tech Lead: Điều gì sẽ xảy ra khi một ngoại lệ (exception) bị ném ra bên trong `async void` so với `async Task`, và ASP.NET Core xử lý ngoại lệ này như thế nào?";
        }
        else if (topic.Contains("EfCore", StringComparison.OrdinalIgnoreCase))
        {
            followUp = isEnglish
                ? "Follow-up Challenge: In a high-traffic endpoint, how would you handle optimistic concurrency conflicts when multiple users update the same record simultaneously?"
                : "Câu hỏi xoáy từ Tech Lead: Trong một API chịu tải cao, bạn sẽ xử lý xung đột đồng thời lạc quan (Optimistic Concurrency) như thế nào khi hai người dùng cùng cập nhật 1 bản ghi?";
        }
        else
        {
            followUp = isEnglish
                ? "Follow-up Challenge: Can you discuss the performance and memory allocation trade-offs of this approach under high-throughput conditions?"
                : "Câu hỏi xoáy từ Tech Lead: Trong hệ thống chịu tải cao hàng triệu request/ngày, giải pháp này có đánh đổi gì về mặt cấp phát bộ nhớ (Memory Allocation) và CPU overhead?";
        }

        string feedback = isEnglish
            ? $"Solid response! You demonstrated good technical competence ({techScore}/100). Focus on detailing edge cases and runtime trade-offs to firmly establish Senior-level mastery."
            : $"Câu trả lời rất triển vọng! Bạn thể hiện sự hiểu biết tốt về khái niệm ({techScore}/100 điểm kỹ thuật). Nếu đào sâu thêm các bẫy runtime và xử lý biên thì câu trả lời sẽ đạt chuẩn Senior tuyệt đối.";

        return new AiInterviewEvaluationResponse
        {
            Success = true,
            FlashcardId = request.FlashcardId,
            OverallScore = overallScore,
            TechnicalAccuracyScore = techScore,
            ClarityStructureScore = clarityScore,
            DepthPracticalScore = depthScore,
            PerformanceRating = rating,
            Feedback = feedback,
            Strengths = detectedStrengths,
            MissingPoints = missingAspects,
            FollowUpQuestion = followUp,
            ModelAnswerSample = benchmarkAnswer
        };
    }

    public Task<AiInterviewFollowUpResponse> AnswerFollowUpAsync(AiInterviewFollowUpRequest request, bool isEnglish)
    {
        var answer = (request.CandidateAnswer ?? string.Empty).Trim().ToLowerInvariant();
        bool isGood = answer.Length > 20 && (
            answer.Contains("exception") || answer.Contains("crash") || answer.Contains("deadlock") ||
            answer.Contains("task") || answer.Contains("token") || answer.Contains("rowversion") ||
            answer.Contains("concurrency") || answer.Contains("catch") || answer.Contains("context") ||
            answer.Contains("bộ nhớ") || answer.Contains("bảo vệ") || answer.Contains("xung đột")
        );

        int bonus = isGood ? 12 : 6;
        string feedback = isGood
            ? (isEnglish
                ? "🎯 Excellent response to the follow-up! You demonstrated solid architectural insight and handled the edge case precisely as expected by Senior Interviewers."
                : "🎯 Phản hồi câu hỏi xoáy rất sắc bén! Bạn đã xử lý chính xác tình huống biên và thể hiện bản lĩnh của một kỹ sư giàu kinh nghiệm thực tế.")
            : (isEnglish
                ? "Good attempt! While on the right track, remember to also account for process termination or synchronization context handling in this scenario."
                : "Định hướng của bạn đúng, nhưng cần chú ý thêm cơ chế ngắt tiến trình hoặc xung đột SynchronizationContext để câu trả lời hoàn thiện hơn.");

        string deepDive = isEnglish
            ? "Senior Takeaway: In production .NET applications, always prefer structured error boundaries, pass CancellationToken throughout async pipelines, and use explicit optimistic locking tokens (`RowVersion`)."
            : "Lời khuyên từ Tech Lead: Trong hệ thống thực tế, luôn luôn truyền CancellationToken xuyên suốt pipeline, tránh async void và sử dụng Token khóa lạc quan (`RowVersion`) để bảo toàn tính toàn vẹn dữ liệu.";

        return Task.FromResult(new AiInterviewFollowUpResponse
        {
            Success = true,
            IsSatisfactory = isGood,
            Feedback = feedback,
            BonusScore = bonus,
            DeepDiveInsight = deepDive
        });
    }

    public async Task<AiInterviewReportResponse> GenerateInterviewReportAsync(AiInterviewReportRequest request, bool isEnglish)
    {
        var items = request.Items ?? new List<AiInterviewReportItem>();
        int totalQuestions = items.Count;
        double avg = totalQuestions > 0 ? items.Average(i => i.Score) : 70;
        int avgScore = (int)Math.Round(avg);

        string readiness;
        string badgeClass;
        string assessment;
        int xp = 50;

        if (avgScore >= 85)
        {
            readiness = isEnglish ? "Senior .NET Specialist - Fully Ready" : "Sẵn sàng Phỏng vấn Senior .NET Developer";
            badgeClass = "success";
            assessment = isEnglish
                ? "Exceptional technical depth across runtime internals, asynchronous patterns, and high-performance design. You possess clear communication and architectural maturity."
                : "Năng lực chuyên môn xuất sắc! Bạn nắm rất vững bản chất runtime .NET, cơ chế async và tối ưu hiệu năng. Phong thái tự tin và lập luận sắc sảo chuẩn Senior Architect.";
            xp = 90;
        }
        else if (avgScore >= 70)
        {
            readiness = isEnglish ? "Confident Middle Developer - Interview Ready" : "Sẵn sàng Phỏng vấn Middle .NET Developer";
            badgeClass = "primary";
            assessment = isEnglish
                ? "Solid grasp of modern C# and ASP.NET Core principles. With a few refinements on memory allocations and edge case concurrency, you will easily pass Senior rounds."
                : "Nền tảng vững vàng về Modern C# và ASP.NET Core. Chỉ cần đào sâu thêm một chút về quản lý bộ nhớ Heap/Stack và xử lý đồng thời, bạn sẽ tự tin vượt qua mọi vòng kỹ thuật.";
            xp = 70;
        }
        else if (avgScore >= 50)
        {
            readiness = isEnglish ? "Junior Ready - Good Foundation" : "Đạt chuẩn Phỏng vấn Junior .NET Developer";
            badgeClass = "info";
            assessment = isEnglish
                ? "Good familiarity with standard syntax and web basics. We recommend practicing more real-world problem scenarios in EF Core and Async programming."
                : "Nắm tốt cú pháp cơ bản và luồng xử lý Web API. Cần rèn luyện thêm các kịch bản thực chiến về tối ưu truy vấn EF Core và xử lý bất đồng bộ.";
            xp = 50;
        }
        else
        {
            readiness = isEnglish ? "Foundational Study Needed" : "Cần Ôn Luyện Thêm Kiến Thức";
            badgeClass = "warning";
            assessment = isEnglish
                ? "Some fundamental concepts were unclear. Review our interactive tutorials and flashcards to build robust theoretical and practical grounding."
                : "Một số khái niệm nền tảng còn chưa rõ. Hãy xem lại các bài học trọng điểm và ôn luyện flashcard hàng ngày để củng cố gốc rễ.";
            xp = 30;
        }

        // Skill Breakdown
        var breakdowns = new List<AiInterviewSkillBreakdown>
        {
            new() { SkillName = isEnglish ? "C# Internals & Memory Architecture" : "Bản chất C# & Quản lý Bộ nhớ", Score = Math.Clamp(avgScore + 2, 45, 96), Level = avgScore >= 80 ? "Senior" : "Mid" },
            new() { SkillName = isEnglish ? "Async & High-Throughput Concurrency" : "Lập trình Bất đồng bộ & Xử lý Đồng thời", Score = Math.Clamp(avgScore - 3, 40, 95), Level = avgScore >= 75 ? "Mid-Senior" : "Junior" },
            new() { SkillName = isEnglish ? "Entity Framework Core & SQL Optimization" : "Entity Framework Core & Tối ưu Truy vấn", Score = Math.Clamp(avgScore + 1, 45, 98), Level = avgScore >= 80 ? "Senior" : "Mid" },
            new() { SkillName = isEnglish ? "ASP.NET Core & Web API Architecture" : "Kiến trúc ASP.NET Core & Web API", Score = Math.Clamp(avgScore + 4, 50, 99), Level = "Solid" },
            new() { SkillName = isEnglish ? "Clean Architecture & Design Patterns" : "Clean Architecture & Mẫu Thiết kế SOLID", Score = Math.Clamp(avgScore - 1, 45, 95), Level = avgScore >= 75 ? "Proficient" : "Foundational" }
        };

        // Query tutorials for actionable study recommendations
        var recommended = new List<AiInterviewRecommendedLesson>();
        try
        {
            var tutorials = await _context.Tutorials
                .OrderByDescending(t => t.ViewCount)
                .Take(3)
                .ToListAsync();

            foreach (var t in tutorials)
            {
                recommended.Add(new AiInterviewRecommendedLesson
                {
                    Title = t.Title,
                    Slug = t.Slug,
                    Reason = isEnglish ? "Core syllabus tutorial to strengthen foundational and practical depth." : "Bài học trọng tâm giúp củng cố kiến thức thực chiến chuyên sâu."
                });
            }
        }
        catch
        {
            // Fallback safe recommendations
            recommended.Add(new AiInterviewRecommendedLesson
            {
                Title = "Lập trình Bất đồng bộ (Async/Await) nâng cao trong .NET",
                Slug = "async-await-csharp-nang-cao",
                Reason = isEnglish ? "Essential for scalable Web API development." : "Kiến thức bắt buộc để làm chủ Web API chịu tải cao."
            });
            recommended.Add(new AiInterviewRecommendedLesson
            {
                Title = "Tối ưu hiệu năng Entity Framework Core trong dự án lớn",
                Slug = "entity-framework-core-toi-uu-truy-van",
                Reason = isEnglish ? "Eliminate N+1 queries and avoid memory bloat." : "Loại bỏ triệt để lỗi N+1 và giảm thiểu hao tổn RAM."
            });
        }

        return new AiInterviewReportResponse
        {
            Success = true,
            AverageScore = avgScore,
            ReadinessLevel = readiness,
            ReadinessBadgeClass = badgeClass,
            ExecutiveAssessment = assessment,
            SkillBreakdowns = breakdowns,
            RecommendedLessons = recommended,
            XpAwarded = xp,
            TimeSpentSeconds = request.TimeSpentSeconds
        };
    }
}


