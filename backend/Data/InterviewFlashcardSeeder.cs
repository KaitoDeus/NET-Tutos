using Microsoft.EntityFrameworkCore;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Data;

public static class InterviewFlashcardSeeder
{
    public static async Task EnsureInterviewFlashcardTablesExistAsync(AppDbContext context)
    {
        var isSqlite = context.Database.IsSqlite();
        if (isSqlite)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS [InterviewFlashcards] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [Slug] TEXT NOT NULL,
                    [Topic] INTEGER NOT NULL,
                    [Difficulty] INTEGER NOT NULL,
                    [QuestionVi] TEXT NOT NULL,
                    [QuestionEn] TEXT NOT NULL,
                    [QuickHintVi] TEXT NULL,
                    [QuickHintEn] TEXT NULL,
                    [AnswerMarkdownVi] TEXT NOT NULL,
                    [AnswerMarkdownEn] TEXT NOT NULL,
                    [InterviewTipVi] TEXT NULL,
                    [InterviewTipEn] TEXT NULL,
                    [OrderIndex] INTEGER NOT NULL DEFAULT 1,
                    [ViewCount] INTEGER NOT NULL DEFAULT 0,
                    [CreatedAt] TEXT NOT NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS [IX_InterviewFlashcards_Slug] ON [InterviewFlashcards] ([Slug]);
                CREATE INDEX IF NOT EXISTS [IX_InterviewFlashcards_Topic] ON [InterviewFlashcards] ([Topic]);
                CREATE INDEX IF NOT EXISTS [IX_InterviewFlashcards_Difficulty] ON [InterviewFlashcards] ([Difficulty]);

                CREATE TABLE IF NOT EXISTS [UserFlashcardProgresses] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [UserId] TEXT NOT NULL,
                    [FlashcardId] INTEGER NOT NULL,
                    [BoxLevel] INTEGER NOT NULL DEFAULT 1,
                    [ReviewCount] INTEGER NOT NULL DEFAULT 0,
                    [LastRating] INTEGER NOT NULL DEFAULT 1,
                    [LastReviewedAt] TEXT NOT NULL,
                    [NextReviewDate] TEXT NOT NULL,
                    [IsMastered] INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                    FOREIGN KEY ([FlashcardId]) REFERENCES [InterviewFlashcards] ([Id]) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS [IX_UserFlashcardProgresses_UserId_FlashcardId] ON [UserFlashcardProgresses] ([UserId], [FlashcardId]);
                CREATE INDEX IF NOT EXISTS [IX_UserFlashcardProgresses_NextReviewDate] ON [UserFlashcardProgresses] ([NextReviewDate]);
            ");
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InterviewFlashcards')
                BEGIN
                    CREATE TABLE [InterviewFlashcards] (
                        [Id] int NOT NULL IDENTITY,
                        [Slug] nvarchar(200) NOT NULL,
                        [Topic] int NOT NULL,
                        [Difficulty] int NOT NULL,
                        [QuestionVi] nvarchar(500) NOT NULL,
                        [QuestionEn] nvarchar(500) NOT NULL,
                        [QuickHintVi] nvarchar(300) NULL,
                        [QuickHintEn] nvarchar(300) NULL,
                        [AnswerMarkdownVi] nvarchar(max) NOT NULL,
                        [AnswerMarkdownEn] nvarchar(max) NOT NULL,
                        [InterviewTipVi] nvarchar(max) NULL,
                        [InterviewTipEn] nvarchar(max) NULL,
                        [OrderIndex] int NOT NULL DEFAULT 1,
                        [ViewCount] int NOT NULL DEFAULT 0,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_InterviewFlashcards] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_InterviewFlashcards_Slug] ON [InterviewFlashcards] ([Slug]);
                    CREATE INDEX [IX_InterviewFlashcards_Topic] ON [InterviewFlashcards] ([Topic]);
                    CREATE INDEX [IX_InterviewFlashcards_Difficulty] ON [InterviewFlashcards] ([Difficulty]);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserFlashcardProgresses')
                BEGIN
                    CREATE TABLE [UserFlashcardProgresses] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [FlashcardId] int NOT NULL,
                        [BoxLevel] int NOT NULL DEFAULT 1,
                        [ReviewCount] int NOT NULL DEFAULT 0,
                        [LastRating] int NOT NULL DEFAULT 1,
                        [LastReviewedAt] datetime2 NOT NULL,
                        [NextReviewDate] datetime2 NOT NULL,
                        [IsMastered] bit NOT NULL DEFAULT 0,
                        CONSTRAINT [PK_UserFlashcardProgresses] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserFlashcardProgresses_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_UserFlashcardProgresses_InterviewFlashcards_FlashcardId] FOREIGN KEY ([FlashcardId]) REFERENCES [InterviewFlashcards] ([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_UserFlashcardProgresses_UserId_FlashcardId] ON [UserFlashcardProgresses] ([UserId], [FlashcardId]);
                    CREATE INDEX [IX_UserFlashcardProgresses_NextReviewDate] ON [UserFlashcardProgresses] ([NextReviewDate]);
                END
            ");
        }
    }

    public static async Task SeedInterviewFlashcardsAsync(AppDbContext context)
    {
        if (await context.InterviewFlashcards.AnyAsync()) return;

        var cards = new List<InterviewFlashcard>
        {
            // ==========================================
            // TOPIC 1: CSHARP CORE (1 - 5)
            // ==========================================
            new()
            {
                Slug = "value-type-vs-reference-type",
                Topic = FlashcardTopic.CSharpCore,
                Difficulty = DifficultyLevel.Beginner,
                OrderIndex = 1,
                QuestionVi = "Phân biệt Value Type và Reference Type trong C#? Chúng được cấp phát ở đâu trên bộ nhớ và khi nào một Value Type được cấp phát trên Heap?",
                QuestionEn = "What is the difference between Value Types and Reference Types in C#? Where are they allocated, and when can a Value Type end up on the Heap?",
                QuickHintVi = "Stack vs Heap, struct vs class, Boxing, và biến cục bộ so với trường của class.",
                QuickHintEn = "Stack vs Heap, struct vs class, Boxing, and local variables vs class fields.",
                AnswerMarkdownVi = @"### 1. Phân biệt cốt lõi
- **Value Type** (chứa trực tiếp giá trị): Các kiểu số nguyên (`int`, `long`), số thực (`float`, `double`), `bool`, `char`, `struct`, và `enum`.
- **Reference Type** (chứa con trỏ tham chiếu đến vùng nhớ dữ liệu): `class`, `interface`, `delegate`, `string`, `object`, `record class`, và mảng.

### 2. Vị trí cấp phát bộ nhớ
- **Stack**: Lưu trữ biến cục bộ của Value Type và con trỏ địa chỉ của Reference Type. Vùng nhớ Stack được quản lý tự động, tốc độ cấp phát và giải phóng tức thì theo phạm vi hàm.
- **Heap (Managed Heap)**: Lưu trữ dữ liệu thực sự của Reference Type, được quản lý và thu hồi bởi Garbage Collector (GC).

### 3. Khi nào Value Type nằm trên Heap?
Một `struct` (Value Type) **KHÔNG** phải lúc nào cũng nằm trên Stack. Nó sẽ nằm trên Heap trong 3 trường hợp:
1. **Là trường (field) của một class/record**: Toàn bộ dữ liệu của class nằm trên Heap, do đó các trường struct bên trong nó cũng nằm trên Heap.
2. **Boxing**: Khi ép kiểu Value Type thành `object` hoặc `interface`.
3. **Nằm trong Closure hoặc State Machine**: Biến cục bộ trong biểu thức lambda hoặc phương thức `async`/`iterator` (`yield return`) được compiler bọc vào một state machine class sinh ngầm trên Heap.",
                AnswerMarkdownEn = @"### 1. Core Differences
- **Value Types** (hold data directly): Primitive numerics (`int`, `double`), `bool`, `struct`, `enum`.
- **Reference Types** (hold memory pointer to data): `class`, `interface`, `delegate`, `string`, `object`, `record class`, arrays.

### 2. Memory Allocation
- **Stack**: Fast, automatic push/pop with function scope. Holds local value types and reference pointers.
- **Managed Heap**: Holds reference objects, inspected and reclaimed by the Garbage Collector (GC).

### 3. When does a Value Type reside on the Heap?
A `struct` is **NOT** guaranteed to live on the Stack. It lives on the Heap when:
1. **Field of a Reference Type**: A struct field inside a class lives within the class allocation on the Heap.
2. **Boxing**: Casting a value type to `object` or an interface.
3. **Closures & State Machines**: Captured variables in lambdas or `async`/iterator methods.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Tránh trả lời máy móc 'Value Type luôn nằm trên Stack'. Hãy giải thích 3 kịch bản struct nằm trên Heap ở trên để ghi điểm tuyệt đối với Tech Lead!",
                InterviewTipEn = "💡 Interview Tip: Never say 'value types are always on the stack'. Explicitly mention class fields, boxing, and async closures to impress the interviewer!"
            },

            new()
            {
                Slug = "boxing-and-unboxing-in-csharp",
                Topic = FlashcardTopic.CSharpCore,
                Difficulty = DifficultyLevel.Beginner,
                OrderIndex = 2,
                QuestionVi = "Boxing và Unboxing trong C# là gì? Tác động tiêu cực của nó đến hiệu năng bộ nhớ và cách phòng tránh trong code hiện đại?",
                QuestionEn = "What are Boxing and Unboxing in C#? What is their performance impact, and how can they be avoided in modern code?",
                QuickHintVi = "Chuyển đổi giữa struct và object/interface, chi phí phân bổ Heap và áp lực GC.",
                QuickHintEn = "Converting between struct and object/interface, Heap allocation cost and GC pressure.",
                AnswerMarkdownVi = @"### 1. Bản chất cơ chế
- **Boxing**: Quá trình chuyển đổi ngầm định một Value Type thành Reference Type (`object` hoặc `interface`). CLR cấp phát một khối bộ nhớ mới trên Managed Heap, sao chép giá trị của Value Type vào đó và trả về con trỏ tham chiếu.
- **Unboxing**: Quá trình chuyển đổi tường minh từ `object` trở lại Value Type ban đầu. CLR kiểm tra kiểu dữ liệu an toàn và sao chép giá trị từ Heap trở lại Stack.

```csharp
int x = 42;
object obj = x;        // Boxing: Cấp phát mới trên Heap!
int y = (int)obj;      // Unboxing: Kiểm tra type + copy về Stack
```

### 2. Tác hại hiệu năng
- **Tăng áp lực GC**: Mỗi lần boxing tạo ra một object tạm thời trên Gen 0 của Heap, gây kích hoạt GC thường xuyên.
- **Tiêu tốn chu kỳ CPU**: Phải thực hiện cấp phát bộ nhớ, sao chép byte và kiểm tra kiểu runtime.

### 3. Cách phòng tránh trong C# hiện đại
- Sử dụng **Generics** (`List<int>` thay vì `ArrayList`, `Dictionary<int, string>` thay vì `Hashtable`).
- Tránh các interface trên struct trừ khi gọi qua Generic Constraint `where T : IComparable<T>`.
- Sử dụng chuỗi nội suy `$""{x}""` hoặc `ReadOnlySpan<char>` thay cho `string.Format(""{0}"", x)`.",
                AnswerMarkdownEn = @"### 1. Underlying Mechanism
- **Boxing**: Implicit conversion of a value type to `object` or interface. Allocates a new box object on the Heap and copies the value.
- **Unboxing**: Explicit extraction of the value pointer from the heap box back onto the Stack.

### 2. Performance Penalty
- **GC Overhead**: Frequent boxing floods Generation 0 with short-lived objects.
- **CPU Overhead**: Allocation, byte copying, and type verification at runtime.

### 3. Modern Prevention
- Always use **Generics** (`List<int>` instead of `ArrayList`).
- Use generic constraints `where T : struct, ISomething` rather than calling interfaces on struct directly.
- Prefer string interpolation and `Span<char>` over non-generic APIs.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy nhấn mạnh rằng Generics trong C# (từ .NET 2.0) được thiết kế chủ yếu để giải quyết triệt để vấn nạn Boxing/Unboxing.",
                InterviewTipEn = "💡 Interview Tip: Emphasize that C# generics generate specialized machine code for value types, completely eliminating boxing."
            },

            new()
            {
                Slug = "class-struct-record-differences",
                Topic = FlashcardTopic.CSharpCore,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 3,
                QuestionVi = "So sánh sự khác nhau giữa `class`, `struct` và `record` trong C#? Khi nào nên sử dụng `record struct`?",
                QuestionEn = "Compare the differences between `class`, `struct`, and `record` in C#. When should you choose `record struct`?",
                QuickHintVi = "Reference Equality vs Value Equality, tính bất biến Immutability, từ khóa with, DTO.",
                QuickHintEn = "Reference vs Value Equality, immutability, with expression, DTOs.",
                AnswerMarkdownVi = @"### 1. Bảng so sánh tổng quan

| Đặc tính | `class` | `struct` | `record` (class) | `record struct` |
|---|---|---|---|---|
| Kiểu bộ nhớ | Reference Type (Heap) | Value Type (Stack/Heap) | Reference Type (Heap) | Value Type (Stack/Heap) |
| So sánh `==` | So sánh địa chỉ con trỏ | So sánh giá trị (Reflection) | So sánh giá trị (Tự động sinh) | So sánh giá trị (Tự động sinh) |
| Tính kế thừa | Có hỗ trợ | Không hỗ trợ | Có hỗ trợ (từ record khác) | Không hỗ trợ |
| Cú pháp `with` | Không hỗ trợ | Không hỗ trợ | Hỗ trợ non-destructive mutation | Hỗ trợ |

### 2. Bản chất của Record
`record` là một cú pháp tiện lợi (syntactic sugar) giúp trình biên dịch tự động sinh:
- `Equals()`, `GetHashCode()`, toán tử `==` và `!=` dựa trên giá trị của các thuộc tính.
- Phương thức sao chép với biểu thức `with`.
- Hàm `ToString()` hiển thị toàn bộ thuộc tính rõ ràng.

### 3. Khi nào nên dùng `record struct`?
Từ C# 10, sử dụng `record struct` (hoặc `readonly record struct`) cho:
- DTO nhỏ, đối tượng Value Object (Coordinates, Money, ComplexNumber) cần so sánh giá trị nhanh mà KHÔNG tốn cấp phát bộ nhớ trên Heap.",
                AnswerMarkdownEn = @"### 1. Comparison
- `class`: Reference type, reference equality by default, supports inheritance.
- `struct`: Value type, value equality, no inheritance, stack allocated (when local).
- `record (class)`: Reference type with compiler-synthesized value-based equality and `with` expression.
- `record struct`: Value type with compiler-synthesized value equality and `with` expression.

### 2. When to use `record struct`?
Use `readonly record struct` for small, immutable Domain Value Objects (Money, Coordinates, Identifiers) where you need zero-allocation performance with value equality.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy nhấn mạnh rằng `record` mặc định là một `class` (Reference Type), trừ khi bạn khai báo tường minh là `record struct`.",
                InterviewTipEn = "💡 Interview Tip: Emphasize that `record` without modifiers is a reference type (`record class`), and only `record struct` is a value type."
            },

            new()
            {
                Slug = "garbage-collection-generations",
                Topic = FlashcardTopic.CSharpCore,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 4,
                QuestionVi = "Cơ chế hoạt động của Garbage Collection (GC) trong .NET? Thế hệ Gen 0, Gen 1, Gen 2 và Large Object Heap (LOH) khác nhau thế nào?",
                QuestionEn = "How does .NET Garbage Collection (GC) work? Explain Generation 0, 1, 2 and the Large Object Heap (LOH).",
                QuickHintVi = "Giả thuyết thế hệ (Generational Hypothesis), đối tượng sống ngắn vs sống lâu, ngưỡng 85.000 bytes.",
                QuickHintEn = "Generational Hypothesis, short vs long lived objects, 85,000 bytes threshold.",
                AnswerMarkdownVi = @"### 1. Nguyên lý Generational GC
Dựa trên **Generational Hypothesis**: Các đối tượng vừa được tạo ra thường có xu hướng chết rất nhanh (biến cục bộ, DTO tạm). Đối tượng càng sống lâu thì càng có xu hướng tiếp tục sống.

### 2. Các thế hệ bộ nhớ
- **Generation 0**: Nơi các đối tượng mới được cấp phát đầu tiên. Vùng nhớ nhỏ, GC quét định kỳ cực nhanh (< 1ms). Các đối tượng sống sót qua lần quét sẽ được thăng hạng (promoted) lên Gen 1.
- **Generation 1**: Vùng đệm chuyển tiếp giữa đối tượng sống ngắn và sống lâu.
- **Generation 2**: Chứa các đối tượng sống lâu (Singleton services, Cache, Static fields). Lần dọn dẹp Gen 2 gọi là **Full GC**, tốn nhiều tài nguyên CPU và có thể tạm dừng luồng (Stop-The-World).

### 3. Large Object Heap (LOH)
- Chứa các đối tượng có kích thước $\ge 85,000$ bytes (chủ yếu là mảng lớn như `byte[]`).
- LOH được gom cùng đợt với Gen 2 và **không được dọn dẹp nén (defragmented)** theo mặc định để tránh tốn thời gian sao chép dữ liệu lớn.
- Giải pháp: Sử dụng `ArrayPool<T>.Shared` để tái sử dụng buffer lớn thay vì `new` liên tục.",
                AnswerMarkdownEn = @"### 1. Generational Hypothesis
Most newly allocated objects become unreachable very quickly. Older objects tend to stay alive longer.

### 2. Generations
- **Gen 0**: New allocations. Very fast ephemeral collection (< 1ms). Survivors promote to Gen 1.
- **Gen 1**: Buffer between short-lived and long-lived objects.
- **Gen 2**: Long-lived objects (Singletons, Cache, Statics). Gen 2 GC is a 'Full GC' and expensive.

### 3. Large Object Heap (LOH)
Objects $\ge 85,000$ bytes bypass Gen 0 and go straight to LOH. Collected during Gen 2, rarely compacted. Mitigate using `ArrayPool<T>`.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Đề cập đến `ArrayPool<byte>.Shared.Rent()` để tránh phân mảnh LOH. Đây là câu trả lời mang tầm kiến trúc sư .NET!",
                InterviewTipEn = "💡 Interview Tip: Mentioning `ArrayPool<T>` to avoid LOH fragmentation proves senior-level memory awareness."
            },

            new()
            {
                Slug = "ref-out-in-parameters",
                Topic = FlashcardTopic.CSharpCore,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 5,
                QuestionVi = "Phân biệt `ref`, `out` và `in` trong tham số hàm C#? Khi nào nên dùng `in` với `readonly struct`?",
                QuestionEn = "What is the difference between `ref`, `out`, and `in` parameter modifiers in C#? When should you use `in` with `readonly struct`?",
                QuickHintVi = "Truyền tham chiếu, bắt buộc gán trước khi trả về, tham chiếu chỉ đọc không copy.",
                QuickHintEn = "Pass by reference, must assign before return, readonly reference with zero copying.",
                AnswerMarkdownVi = @"### 1. So sánh 3 từ khóa
- `ref`: Truyền tham chiếu địa chỉ ô nhớ. Biến **bắt buộc phải được khởi tạo giá trị** trước khi truyền vào hàm. Hàm có thể đọc và ghi đè giá trị mới.
- `out`: Dùng để trả về nhiều giá trị. Biến **không cần khởi tạo** trước, nhưng hàm **BẮT BUỘC phải gán giá trị** trước khi return (ví dụ: `int.TryParse(s, out int val)`).
- `in`: Truyền theo tham chiếu nhưng là **CHỈ ĐỌC (Readonly)**. Trình biên dịch ngăn chặn mọi hành vi thay đổi giá trị của tham số bên trong hàm.

### 2. Khi nào dùng `in` với `readonly struct`?
Khi một struct có kích thước lớn (nhiều fields), việc truyền tham số thông thường sẽ sao chép toàn bộ struct trên Stack.
Dùng `in` kết hợp `readonly struct`:
- Chỉ truyền địa chỉ con trỏ (8 bytes) $\rightarrow$ Tối ưu hóa hiệu năng tối đa.
- Không lo compiler tạo bản sao phòng thủ (defensive copy).",
                AnswerMarkdownEn = @"### 1. Comparison
- `ref`: Pass by reference. Must be initialized before calling. Callee can read and write.
- `out`: Callee MUST assign before returning. Caller doesn't need to initialize.
- `in`: Pass by reference as read-only. Callee cannot modify the parameter.

### 2. Why `in` with `readonly struct`?
Prevents copying large struct data on the stack while avoiding compiler defensive copying.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Cảnh báo rằng dùng `in` với struct thông thường (không có `readonly`) có thể gây phản tác dụng vì compiler sinh 'defensive copy'!",
                InterviewTipEn = "💡 Interview Tip: Warn against using `in` with mutable structs due to hidden defensive copying penalties."
            },

            // ==========================================
            // TOPIC 2: OOP & DESIGN PATTERNS (6 - 8)
            // ==========================================
            new()
            {
                Slug = "dependency-injection-lifetimes",
                Topic = FlashcardTopic.OopDesignPatterns,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 6,
                QuestionVi = "Phân biệt vòng đời của dịch vụ trong ASP.NET Core DI: `Transient`, `Scoped`, và `Singleton`? Hiểm họa 'Captive Dependency' là gì?",
                QuestionEn = "Explain service lifetimes in ASP.NET Core DI: `Transient`, `Scoped`, and `Singleton`. What is a 'Captive Dependency'?",
                QuickHintVi = "Tạo mới mỗi lần gọi, tạo mới theo mỗi HTTP request, tạo 1 lần duy nhất toàn ứng dụng.",
                QuickHintEn = "New instance per request, instance per HTTP request scope, single instance forever.",
                AnswerMarkdownVi = @"### 1. Vòng đời dịch vụ (Service Lifetimes)
- **Transient** (`AddTransient`): Tạo một instance mới **mỗi khi có yêu cầu tiêm phụ thuộc**. Phù hợp cho dịch vụ nhẹ, không lưu trạng thái (stateless).
- **Scoped** (`AddScoped`): Tạo **duy nhất một instance trong suốt một HTTP Request**. Tất cả component trong cùng 1 request dùng chung instance này. Điển hình là `AppDbContext` (EF Core).
- **Singleton** (`AddSingleton`): Tạo **duy nhất một instance cho toàn bộ vòng đời ứng dụng**. Phù hợp cho Memory Cache, SignalR Hub connections, Background Worker.

### 2. Hiểm họa Captive Dependency
Xảy ra khi một dịch vụ có **vòng đời dài** (như Singleton) tiêm phụ thuộc một dịch vụ có **vòng đời ngắn** (như Scoped `DbContext`).
- Hậu quả: Dịch vụ Scoped bị 'bắt cóc' và giữ sống mãi mãi trong Singleton!
- Với EF Core, `DbContext` không an toàn đa luồng (not thread-safe), dẫn đến lỗi tranh chấp luồng và rò rỉ bộ nhớ nghiêm trọng.

### 3. Cách khắc phục
Bật kiểm tra `ValidateScopes = true` trong `Program.cs`. Hoặc tiêm `IServiceScopeFactory` vào Singleton để tự tạo scope khi cần.",
                AnswerMarkdownEn = @"### 1. Lifetimes
- **Transient**: New instance created every time it is requested.
- **Scoped**: One instance per HTTP Request scope. Ideal for EF Core `DbContext`.
- **Singleton**: Single instance created once for the entire application lifetime.

### 2. Captive Dependency Pitfall
Occurs when a longer-lived service (Singleton) injects a shorter-lived service (Scoped).
- The Scoped service becomes trapped as a Singleton, causing concurrency exceptions in EF Core and memory leaks.
- Solution: Enable `ValidateScopes` in Development, or inject `IServiceScopeFactory`.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy nhắc ngay đến từ khóa 'Captive Dependency' khi được hỏi về DI Lifetimes để thể hiện kiến thức chuyên sâu.",
                InterviewTipEn = "💡 Interview Tip: Always mention 'Captive Dependency' and `IServiceScopeFactory` to show enterprise-grade DI maturity."
            },

            new()
            {
                Slug = "solid-principles-real-world",
                Topic = FlashcardTopic.OopDesignPatterns,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 7,
                QuestionVi = "Giải thích nguyên lý Dependency Inversion (DIP) trong SOLID? Phân biệt giữa Dependency Inversion, Inversion of Control (IoC) và Dependency Injection (DI)?",
                QuestionEn = "Explain the Dependency Inversion Principle (DIP) in SOLID. How does it differ from Inversion of Control (IoC) and Dependency Injection (DI)?",
                QuickHintVi = "Nguyên lý kiến trúc vs Khái niệm thiết kế vs Kỹ thuật triển khai.",
                QuickHintEn = "Architectural principle vs Design pattern concept vs Implementation technique.",
                AnswerMarkdownVi = @"### 1. Dependency Inversion Principle (DIP)
- Các module cấp cao (Business Logic) không được phụ thuộc trực tiếp vào module cấp thấp (Database, Email Sender, Payment Gateway). Cả hai phải phụ thuộc vào sự trừu tượng (`Interface`).
- Chi tiết kỹ thuật phải phụ thuộc vào trừu tượng, không phải ngược lại.

### 2. Phân biệt DIP vs IoC vs DI
- **DIP (Nguyên lý - Principle)**: Một tư tưởng kiến trúc (High level depends on abstraction).
- **IoC (Khái niệm thiết kế - Inversion of Control)**: Đảo ngược quyền điều khiển luồng chương trình. Framework gọi code của bạn thay vì code của bạn chủ động gọi thư viện (Hollywood Principle: *'Don't call us, we'll call you'*).
- **DI (Kỹ thuật - Dependency Injection)**: Một mẫu thiết kế cụ thể để hiện thực hóa IoC và DIP bằng cách truyền dependencies từ bên ngoài vào qua Constructor thay vì dùng từ khóa `new` bên trong class.",
                AnswerMarkdownEn = @"### 1. Dependency Inversion Principle (DIP)
High-level modules should not depend on low-level modules; both should depend on abstractions. Abstractions should not depend on details.

### 2. Clarifying the Trio
- **DIP**: An architectural principle (The goal).
- **IoC**: A broader design concept (Inversion of program control flow).
- **DI**: A concrete design pattern implementing IoC (Passing dependencies via constructor).",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Rất nhiều người nhầm lẫn 3 khái niệm này. Nêu rõ: DIP là Mục tiêu, IoC là Phương pháp, DI là Công cụ triển khai.",
                InterviewTipEn = "💡 Interview Tip: Clarify that DIP is the Principle, IoC is the Strategy, and DI is the Tactical Implementation."
            },

            new()
            {
                Slug = "repository-pattern-with-efcore",
                Topic = FlashcardTopic.OopDesignPatterns,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 8,
                QuestionVi = "Có nên bọc EF Core DbContext trong Repository Pattern và Unit of Work không? Phân tích ưu và nhược điểm trong các dự án hiện đại?",
                QuestionEn = "Should you wrap EF Core DbContext inside Repository and Unit of Work patterns? What are the pros and cons in modern .NET projects?",
                QuickHintVi = "DbContext bản chất đã là Unit of Work, DbSet là Repository, trade-off giữa Leaky Abstraction và Testability.",
                QuickHintEn = "DbContext is already a Unit of Work, DbSet is a Repository, leaky abstraction trade-offs.",
                AnswerMarkdownVi = @"### 1. Bản chất của EF Core
- `DbContext` bản chất đã là một **Unit of Work** (quản lý Change Tracker, thực thi `SaveChangesAsync` nguyên tử).
- `DbSet<T>` bản chất đã là một **Repository** (cung cấp `Add`, `Remove`, `Find`, và truy vấn qua `IQueryable`).

### 2. Nhược điểm khi bọc thêm Repository/Unit of Work
- **Leaky Abstraction**: Cố gắng trừu tượng hóa nhưng cuối cùng vẫn phải để lộ `IQueryable` hoặc `Include`, làm mất đi ý nghĩa của việc bọc.
- **Mất các tính năng mạnh mẽ của EF Core**: Bị hạn chế dùng Change Tracker, Compiled Queries, Batch Updates (`ExecuteUpdateAsync`), và Split Queries.
- **Mã nguồn cồng kềnh**: Viết hàng chục lớp Repository trung gian chỉ để gọi hàm `.ToListAsync()`.

### 3. Khi nào vẫn nên dùng?
- Cần cô lập và che giấu hoàn toàn nguồn dữ liệu (ví dụ vừa gọi DB vừa gọi REST API ngoài).
- Viết Unit Test mà không muốn dựa vào InMemory Database hoặc TestContainers.
- **Xu hướng hiện đại**: Sử dụng trực tiếp `DbContext` kết hợp với Clean Architecture và MediatR CQRS Handlers.",
                AnswerMarkdownEn = @"### 1. The Reality of EF Core
`DbContext` already implements Unit of Work, and `DbSet<T>` implements Repository.

### 2. Pitfalls of Generic Repository over EF Core
- **Leaky Abstraction**: Returning `IQueryable` leaks ORM concerns anyway.
- **Lost Features**: Hides EF Core strengths like `ExecuteUpdateAsync`, compiled queries, and split queries.
- **Boilerplate**: Meaningless pass-through code.

### 3. Modern Best Practice
Inject `DbContext` directly into Vertical Slice / CQRS Handlers, or wrap only specialized domain queries.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy tự tin phân tích tính 2 mặt (trade-offs) thay vì khẳng định 'bắt buộc phải có Repository'. Nhà tuyển dụng đánh giá cực cao ứng viên hiểu bản chất này.",
                InterviewTipEn = "💡 Interview Tip: Acknowledge that DbContext is already a Unit of Work. Thoughtful trade-off discussion demonstrates senior maturity."
            },

            // ==========================================
            // TOPIC 3: ASYNC & CONCURRENCY (9 - 12)
            // ==========================================
            new()
            {
                Slug = "async-await-state-machine",
                Topic = FlashcardTopic.AsyncConcurrency,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 9,
                QuestionVi = "Bản chất bên dưới của từ khóa `async`/`await` trong C# hoạt động như thế nào? Sự khác nhau giữa `Task.Run` và Non-blocking I/O?",
                QuestionEn = "How does `async`/`await` actually work under the hood in C#? What is the difference between `Task.Run` and Non-blocking I/O?",
                QuickHintVi = "Compiler sinh ra struct IAsyncStateMachine, giải phóng thread về ThreadPool khi chờ I/O.",
                QuickHintEn = "Compiler generates an IAsyncStateMachine struct, releases thread during I/O wait.",
                AnswerMarkdownVi = @"### 1. Bản chất State Machine
Trình biên dịch C# không tạo ra luồng mới cho từ khóa `async`. Thay vào đó, nó chuyển đổi phương thức thành một cấu trúc **State Machine** (`IAsyncStateMachine` struct):
- Khi gặp từ khóa `await`, nó kiểm tra xem tác vụ đã hoàn thành chưa (`IsCompleted`).
- Nếu chưa hoàn thành: Nó lưu lại trạng thái các biến cục bộ, đăng ký một **Continuation callback**, và **ngay lập tức giải phóng Thread hiện tại về ThreadPool** để phục vụ request khác.
- Khi tác vụ I/O hoàn tất từ phần cứng, một Thread bất kỳ từ ThreadPool sẽ được đánh thức để tiếp tục thực thi phần code còn lại.

### 2. Non-blocking I/O vs Task.Run
- **Non-blocking I/O** (`HttpClient.GetAsync`, `File.ReadAllTextAsync`, EF Core `SaveChangesAsync`): Hoàn toàn **KHÔNG tốn bất kỳ CPU thread nào** trong lúc chờ mạng hay ổ đĩa (dựa trên I/O Completion Ports - IOCP của HĐH).
- **Task.Run**: Chiếm dụng một CPU Worker Thread từ ThreadPool để thực hiện công việc tính toán nặng.",
                AnswerMarkdownEn = @"### 1. State Machine Under the Hood
The compiler transforms `async` methods into an `IAsyncStateMachine` struct:
- When encountering `await`, if the task isn't done, it saves local state, registers a continuation callback, and yields the thread back to the thread pool.
- Once I/O completes, an available thread picks up the continuation.

### 2. Non-blocking I/O vs Task.Run
- **Async I/O**: ZERO threads are blocked waiting for responses (hardware DMA / IOCP).
- **Task.Run**: Queues work onto an actual thread for CPU-bound computations.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Nhấn mạnh câu 'Trong quá trình chờ I/O bất đồng bộ, không có bất kỳ luồng nào bị chiếm dụng'. Đây là cốt lõi của khả năng mở rộng (scalability).",
                InterviewTipEn = "💡 Interview Tip: Highlight 'There is no thread' during asynchronous I/O waiting. That is the secret to high-throughput ASP.NET Core."
            },

            new()
            {
                Slug = "avoid-async-void-pitfall",
                Topic = FlashcardTopic.AsyncConcurrency,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 10,
                QuestionVi = "Tại sao nên tránh dùng `async void` trong C# ngoại trừ Event Handlers? Hiểm họa tiềm ẩn là gì?",
                QuestionEn = "Why should you avoid `async void` in C# except for Event Handlers? What are the severe pitfalls?",
                QuickHintVi = "Không thể await, không bắt được Exception bằng try-catch, làm crash đột ngột cả ứng dụng.",
                QuickHintEn = "Cannot await, unhandled exceptions bypass try-catch and crash the process.",
                AnswerMarkdownVi = @"### 1. Hai hiểm họa chết người của `async void`
1. **Không thể `await` để theo dõi kết quả**: Vì không trả về `Task`, phía gọi không có cách nào biết khi nào phương thức chạy xong, không thể đồng bộ luồng.
2. **Làm sập toàn bộ ứng dụng (Process Crash)**: Bất kỳ Exception nào ném ra trong `async void` **KHÔNG THỂ bắt được bằng khối `try-catch` của phía gọi**! Nó sẽ bay thẳng lên `SynchronizationContext` hoặc ThreadPool và đánh sập toàn bộ ứng dụng (.NET unhandled exception behavior).

```csharp
// NGUY HIỂM: try-catch này KHÔNG bắt được exception bên trong!
try
{
    DoWorkAsyncVoid(); // async void
}
catch (Exception ex)
{
    // Không bao giờ chạy vào đây, ứng dụng sẽ bị CRASH!
}
```

### 2. Ngoại lệ duy nhất
Chỉ dùng `async void` cho các **UI Event Handlers** (ví dụ: `button_Click(object sender, EventArgs e)`) vì chữ ký delegate bắt buộc trả về `void`.",
                AnswerMarkdownEn = @"### 1. The Two Fatal Flaws
1. **Fire-and-forget**: Cannot be awaited; caller has no idea when it finishes.
2. **Crashes the Entire Process**: Exceptions cannot be caught by the caller's `try-catch` block. They bubble directly to the ThreadPool and terminate the process.

### 2. The Only Exception
UI Event Handlers whose delegate signature requires `void` (`EventHandler`).",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy nhắc đến câu thần chú: 'Async all the way. Luôn trả về Task hoặc Task<T>, không bao giờ dùng async void'.",
                InterviewTipEn = "💡 Interview Tip: Recite the golden rule: 'Async all the way. Always return Task or Task<T>, never async void'."
            },

            new()
            {
                Slug = "valuetask-vs-task-performance",
                Topic = FlashcardTopic.AsyncConcurrency,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 11,
                QuestionVi = "Khi nào nên sử dụng `ValueTask<T>` thay vì `Task<T>` trong .NET? Hãy chỉ ra các lưu ý sống còn khi sử dụng `ValueTask`?",
                QuestionEn = "When should you use `ValueTask<T>` instead of `Task<T>` in .NET? What pitfalls must you watch out for?",
                QuickHintVi = "Value Type struct vs Reference Type object, kịch bản trả về kết quả đồng bộ ngay lập tức từ Cache.",
                QuickHintEn = "Value type struct vs Reference type object, synchronous cache-hit scenario.",
                AnswerMarkdownVi = @"### 1. Sự khác biệt
- `Task<T>`: Là một `class` (Reference Type). Mỗi khi được khởi tạo, nó tốn một phân bổ bộ nhớ trên Heap.
- `ValueTask<T>`: Là một `struct` (Value Type). Nếu kết quả có sẵn đồng bộ ngay lập tức, nó trả về giá trị mà **không tốn bất kỳ phân bổ Heap nào (Zero Allocation)**!

### 2. Khi nào nên dùng?
Khi một hàm được gọi hàng triệu lần trong vòng lặp hoặc high-throughput server, VÀ phần lớn các lần gọi (ví dụ $\ge 80\%$) hoàn thành đồng bộ ngay lập tức (như đọc từ MemoryCache hoặc buffer).

### 3. Lưu ý sống còn khi dùng `ValueTask`
- **KHÔNG ĐƯỢC `await` nhiều lần**: Một `ValueTask` chỉ được phép await đúng 1 lần duy nhất!
- **KHÔNG gọi `.Result` hoặc `.GetAwaiter().GetResult()`** khi chưa hoàn tất.
- Nếu cần lưu trữ hoặc await nhiều lần, hãy gọi `.AsTask()` để chuyển về `Task<T>`.",
                AnswerMarkdownEn = @"### 1. The Difference
`Task<T>` is a class on the heap. `ValueTask<T>` is a struct that enables zero heap allocations when operations complete synchronously.

### 2. When to Use
High-throughput hot paths where the operation frequently completes synchronously (e.g. cache hits).

### 3. Critical Rules
- Never `await` a `ValueTask` more than once.
- Never call `.Result` before completion.
- Convert via `.AsTask()` if multiple awaits or storage are required.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Mặc định hãy luôn dùng `Task<T>`. Chỉ chuyển sang `ValueTask<T>` khi profiling chứng minh được lợi ích giảm áp lực cấp phát GC.",
                InterviewTipEn = "💡 Interview Tip: Default to `Task<T>`. Only switch to `ValueTask<T>` when profiler metrics show substantial allocation reduction."
            },

            new()
            {
                Slug = "deadlock-result-and-wait",
                Topic = FlashcardTopic.AsyncConcurrency,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 12,
                QuestionVi = "Tại sao việc gọi `.Result` hoặc `.Wait()` trên một Task bất đồng bộ có thể gây ra Deadlock?",
                QuestionEn = "Why can calling `.Result` or `.Wait()` on an asynchronous Task cause a Deadlock?",
                QuickHintVi = "SynchronizationContext, luồng bị chặn chờ đợi kết quả nhưng chính kết quả lại cần luồng đó để tiếp tục.",
                QuickHintEn = "SynchronizationContext blocking, thread waiting on task while task waits for thread.",
                AnswerMarkdownVi = @"### 1. Nguyên nhân gây Deadlock
1. Luồng UI hoặc Request Thread gọi một hàm async nhưng lại chặn bằng `.Result` hoặc `.Wait()`.
2. Hàm async chạy đến câu lệnh `await`. Vì tác vụ chưa xong, nó lưu lại `SynchronizationContext` hiện tại.
3. Khi tác vụ I/O xong, `await` cố gắng đưa continuation callback quay trở lại `SynchronizationContext` ban đầu để chạy tiếp.
4. Nhưng luồng ban đầu đang bị CHẶN cứng bởi `.Result` $\rightarrow$ **Luồng chờ Task xong, Task chờ Luồng giải phóng để chạy $\rightarrow$ DEADLOCK!**

### 2. Giải pháp triệt để
- **Async All The Way**: Thay vì `.Result`, hãy dùng `await`.
- **ConfigureAwait(false)**: Trong các class library, gọi `await Task.ConfigureAwait(false)` để báo cho runtime không cần quay lại context ban đầu.",
                AnswerMarkdownEn = @"### 1. The Deadlock Anatomy
Caller blocks a thread with `.Result`. The async callee awaits an operation and captures the `SynchronizationContext`. Upon completion, the task attempts to resume on the captured context, but the thread is stuck waiting for the task -> Deadlock!

### 2. Solution
- Use `await` end-to-end.
- Use `ConfigureAwait(false)` in reusable class libraries.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: ASP.NET Core không còn `SynchronizationContext` nên ít bị deadlock này, nhưng gọi `.Result` trong ASP.NET Core vẫn gây cạn kiệt luồng (Thread Pool Starvation)!",
                InterviewTipEn = "💡 Interview Tip: ASP.NET Core has no SynchronizationContext, but `.Result` still triggers Thread Pool Starvation."
            },

            // ==========================================
            // TOPIC 4: EF CORE & DATABASE (13 - 15)
            // ==========================================
            new()
            {
                Slug = "iqueryable-vs-ienumerable",
                Topic = FlashcardTopic.EfCoreDatabase,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 13,
                QuestionVi = "Phân biệt `IQueryable<T>` và `IEnumerable<T>` trong Entity Framework Core? Nguy cơ suy giảm hiệu năng bộ nhớ khi dùng sai?",
                QuestionEn = "Differentiate between `IQueryable<T>` and `IEnumerable<T>` in EF Core. What are the performance and memory risks of misuse?",
                QuickHintVi = "Expression Trees, SQL Translation, Server-side filtering vs In-Memory Client evaluation.",
                QuickHintEn = "Expression Trees, SQL Translation, Server-side filtering vs In-Memory Client evaluation.",
                AnswerMarkdownVi = @"### 1. Phân biệt bản chất
- **`IQueryable<T>`**: Kế thừa `IEnumerable<T>`, nhưng lưu trữ các câu lệnh LINQ dưới dạng **Cây biểu thức (Expression Tree)**. EF Core dịch toàn bộ cây biểu thức này thành câu lệnh **SQL tối ưu và thực thi trực tiếp trên Database Server**.
- **`IEnumerable<T>`**: Hoạt động dựa trên Delegate (`Func<T>`). Dữ liệu được thực thi **trên bộ nhớ RAM của ứng dụng (In-Memory)**.

### 2. Nguy cơ khi dùng sai (Memory Disaster)
Nếu bạn gọi `.ToList()` hoặc ép kiểu sang `IEnumerable` quá sớm:
```csharp
// NGUY HIỂM: Tải 1.000.000 dòng từ DB về RAM rồi mới lọc!
IEnumerable<User> users = context.Users.ToList(); 
var activeUsers = users.Where(u => u.IsActive).Take(10);

// ĐÚNG: EF Core sinh câu lệnh 'SELECT TOP 10 ... WHERE IsActive = 1'
IQueryable<User> users = context.Users;
var activeUsers = await users.Where(u => u.IsActive).Take(10).ToListAsync();
```",
                AnswerMarkdownEn = @"### 1. Core Distinction
- `IQueryable<T>`: Holds an `Expression Tree` translated into SQL by EF Core and executed on the Database server.
- `IEnumerable<T>`: In-memory evaluation in application RAM via delegates.

### 2. Severe Memory Pitfall
Calling `.ToList()` prematurely downloads the entire table into memory before filtering, causing OutOfMemory crashes.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy nhấn mạnh 'IQueryable lọc ở Database, IEnumerable lọc ở RAM'. Luôn giữ IQueryable càng lâu càng tốt cho đến khi gọi ToListAsync.",
                InterviewTipEn = "💡 Interview Tip: Concisely state: 'IQueryable filters in the Database; IEnumerable filters in RAM'."
            },

            new()
            {
                Slug = "n-plus-1-query-problem-efcore",
                Topic = FlashcardTopic.EfCoreDatabase,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 14,
                QuestionVi = "Vấn đề N+1 Query trong EF Core là gì? Làm thế nào để phát hiện và giải quyết triệt để?",
                QuestionEn = "What is the N+1 Query problem in EF Core? How do you detect and permanently resolve it?",
                QuickHintVi = "1 câu query lấy cha + N câu query lấy con trong vòng lặp lặp lại.",
                QuickHintEn = "1 query for parents + N queries for children in a loop.",
                AnswerMarkdownVi = @"### 1. Định nghĩa N+1 Query
Xảy ra khi bạn thực thi 1 câu truy vấn để lấy danh sách $N$ đối tượng cha, sau đó trong vòng lặp xử lý, bạn lại kích hoạt thêm $N$ câu truy vấn riêng lẻ để lấy dữ liệu liên quan của từng đối tượng con.
- Ví dụ: Lấy 100 Khóa học, trong vòng lặp lại đọc danh sách Bài học của từng khóa $\rightarrow$ Tổng cộng **101 câu query** gửi tới database!

### 2. Cách giải quyết
1. **Eager Loading**: Sử dụng `.Include()` và `.ThenInclude()` để nạp trước quan hệ trong 1 hoặc 2 câu query.
2. **Projection (Khuyến nghị tốt nhất)**: Dùng `.Select(...)` chiếu thẳng vào ViewModel/DTO. EF Core sẽ tự động sinh câu lệnh SQL `JOIN` chính xác những trường cần thiết:
```csharp
var courses = await context.Courses
    .Select(c => new CourseDto {
        Title = c.Title,
        LessonCount = c.Lessons.Count()
    }).ToListAsync();
```
3. **AsSplitQuery()**: Tránh hiện tượng Cartesian Explosion khi Include nhiều bảng con.",
                AnswerMarkdownEn = @"### 1. Problem Description
Executing 1 query to retrieve $N$ parent records, followed by $N$ separate queries inside a loop to retrieve child relationships. Total = $N+1$ database roundtrips.

### 2. Solutions
1. **Eager Loading**: `.Include()` and `.ThenInclude()`.
2. **Projection (Best)**: LINQ `.Select()` directly into DTOs.
3. **Split Queries**: `.AsSplitQuery()` to prevent Cartesian explosion.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Đưa ra giải pháp LINQ `.Select()` chiếu vào DTO thay vì chỉ nói `.Include()`. Phỏng vấn viên sẽ thấy bạn có tư duy tối ưu hiệu năng băng thông.",
                InterviewTipEn = "💡 Interview Tip: Prefer projection `.Select()` over `.Include()` to highlight bandwidth and memory efficiency."
            },

            new()
            {
                Slug = "asnotracking-performance",
                Topic = FlashcardTopic.EfCoreDatabase,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 15,
                QuestionVi = "Cơ chế Change Tracker trong EF Core hoạt động thế nào? Khi nào BẮT BUỘC nên sử dụng `AsNoTracking()`?",
                QuestionEn = "How does the EF Core Change Tracker work? When SHOULD you always use `AsNoTracking()`?",
                QuickHintVi = "Snapshot theo dõi thay đổi, truy vấn chỉ đọc Read-only, giảm tải RAM và CPU.",
                QuickHintEn = "Snapshot tracking, read-only queries, reducing memory and CPU overhead.",
                AnswerMarkdownVi = @"### 1. Cơ chế Change Tracker
Khi EF Core truy vấn một thực thể, nó tự động lưu trữ một bản sao (Snapshot) trạng thái ban đầu vào bộ nhớ của `DbContext`. Khi gọi `SaveChangesAsync()`, nó so sánh thực thể hiện tại với Snapshot để tạo câu lệnh SQL `UPDATE`.

### 2. Khi nào nên dùng `AsNoTracking()`?
Đối với các thao tác **CHỈ ĐỌC (Read-Only Queries)** như:
- Tìm kiếm, lọc danh sách bài học, hiển thị chi tiết bài viết, xuất báo cáo.
- Các API GET trả về JSON cho client.

### 3. Lợi ích vượt trội
- Tăng tốc độ truy vấn từ **2 đến 3 lần**.
- Tiết kiệm đáng kể dung lượng RAM vì không tạo Snapshot trong Change Tracker.",
                AnswerMarkdownEn = @"### 1. Change Tracker
Stores state snapshots of queried entities to detect modifications for `SaveChangesAsync()`.

### 2. When to use `AsNoTracking()`
For all **Read-Only operations** (GET endpoints, search, exports).

### 3. Benefits
- 2x to 3x query speedup.
- Substantially lower memory allocation by skipping snapshot creation.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy nhắc thêm về `QueryTrackingBehavior.NoTracking` ở mức DbContext options cho các microservices chỉ phục vụ việc đọc.",
                InterviewTipEn = "💡 Interview Tip: Mention setting `QueryTrackingBehavior.NoTracking` globally on read-heavy microservices."
            },

            // ==========================================
            // TOPIC 5: ASP.NET CORE & WEB API (16 - 18)
            // ==========================================
            new()
            {
                Slug = "middleware-pipeline-order",
                Topic = FlashcardTopic.AspNetCoreWebAPI,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 16,
                QuestionVi = "Pipeline Middleware trong ASP.NET Core hoạt động theo cơ chế nào? Thứ tự sắp xếp các Middleware có quan trọng không?",
                QuestionEn = "How does the ASP.NET Core Middleware pipeline work? Why is the ordering of middleware critical?",
                QuickHintVi = "Mô hình búp bê Nga, thứ tự UseExceptionHandler, UseRouting, UseCors, UseAuthentication, UseAuthorization.",
                QuickHintEn = "Russian doll model, order of UseExceptionHandler, UseRouting, UseCors, UseAuth.",
                AnswerMarkdownVi = @"### 1. Mô hình Búp bê Nga (Russian Doll)
Mỗi Middleware là một khối mã xử lý trung gian. Khi có HTTP Request đến:
- Request đi lần lượt qua từng Middleware (chiều đi vào).
- Gặp Endpoint xử lý (Controller/Minimal API).
- Response sinh ra quay ngược trở lại qua các Middleware (chiều đi ra).

### 2. Tầm quan trọng của Thứ tự Middleware
Thứ tự khai báo trong `Program.cs` là **CỰC KỲ QUAN TRỌNG**:
1. `UseExceptionHandler`: Phải ở ĐẦU TIÊN để bắt toàn bộ lỗi phát sinh từ các middleware phía sau.
2. `UseRouting`: Xác định endpoint phù hợp.
3. `UseCors`: Cho phép gọi chéo domain (phải trước Authentication).
4. `UseAuthentication`: Xác định **BẠN LÀ AI** (giải mã JWT / Cookie).
5. `UseAuthorization`: Kiểm tra **BẠN ĐƯỢC PHÉP LÀM GÌ** (Roles / Policies).
6. `MapControllers` / Endpoints: Thực thi logic.",
                AnswerMarkdownEn = @"### 1. Russian Doll Pipeline
Middlewares wrap around each other. Request traverses inward, hits the endpoint, and the response traverses back outward.

### 2. Critical Middleware Ordering
1. `UseExceptionHandler` (Must be first to catch any downstream exception).
2. `UseRouting`
3. `UseCors` (Must precede Auth).
4. `UseAuthentication` (Identifies WHO you are).
5. `UseAuthorization` (Verifies WHAT you can do).
6. `MapControllers` / Endpoint execution.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Nếu phỏng vấn viên hỏi 'Chuyện gì xảy ra nếu đặt UseAuthorization trước UseAuthentication?', hãy trả lời ngay: 'Request luôn bị từ chối 401/403 vì danh tính chưa được xác thực!'",
                InterviewTipEn = "💡 Interview Tip: Placing `UseAuthorization` before `UseAuthentication` always rejects requests because user identity has not yet been resolved."
            },

            new()
            {
                Slug = "global-exception-handling-net",
                Topic = FlashcardTopic.AspNetCoreWebAPI,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 17,
                QuestionVi = "Cách triển khai xử lý ngoại lệ toàn cục (Global Exception Handling) chuẩn mực trong ASP.NET Core hiện đại (.NET 8/9/10)?",
                QuestionEn = "How do you implement modern Global Exception Handling in ASP.NET Core (.NET 8/9/10)?",
                QuickHintVi = "IExceptionHandler interface, ProblemDetails (RFC 7807), không rải try-catch ở controller.",
                QuickHintEn = "IExceptionHandler interface, ProblemDetails RFC 7807, avoiding repetitive try-catches.",
                AnswerMarkdownVi = @"### 1. Chuẩn mực hiện đại với `IExceptionHandler` (.NET 8+)
Thay vì viết Middleware tùy biến rườm rà hoặc rải `try-catch` khắp Controller, .NET 8+ cung cấp giao diện chuẩn `IExceptionHandler`:

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = ""Server Error"",
            Detail = exception.Message,
            Type = ""https://tools.ietf.org/html/rfc7807""
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
```

### 2. Đăng ký trong `Program.cs`
```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
```",
                AnswerMarkdownEn = @"### 1. Modern `IExceptionHandler` (.NET 8+)
Implement `IExceptionHandler` and return RFC 7807 standard `ProblemDetails`.

### 2. Registration in Program.cs
```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
app.UseExceptionHandler();
```",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Đề cập đến chuẩn quốc tế RFC 7807 (ProblemDetails) và việc không để lộ StackTrace ra ngoài môi trường Production.",
                InterviewTipEn = "💡 Interview Tip: Mention RFC 7807 ProblemDetails compliance and never leaking internal StackTrace in Production."
            },

            new()
            {
                Slug = "jwt-authentication-authorization",
                Topic = FlashcardTopic.AspNetCoreWebAPI,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 18,
                QuestionVi = "Cơ chế hoạt động của JSON Web Token (JWT) và Refresh Token trong việc bảo mật RESTful Web API?",
                QuestionEn = "How do JSON Web Tokens (JWT) and Refresh Tokens work in securing RESTful Web APIs?",
                QuickHintVi = "Header.Payload.Signature, Stateless, Access Token ngắn hạn, Refresh Token dài hạn lưu DB.",
                QuickHintEn = "Header.Payload.Signature, Stateless, short-lived Access Token, long-lived Refresh Token in DB.",
                AnswerMarkdownVi = @"### 1. Cấu trúc của JWT (Stateless)
Gồm 3 phần cách nhau bởi dấu chấm: `Header.Payload.Signature`:
- **Header**: Thuật toán băm (ví dụ HMAC SHA256).
- **Payload**: Các Claims (UserId, Email, Roles, Expiration time).
- **Signature**: Chữ ký điện tử được ký bằng Secret Key của server.
- Server chỉ cần giải mã và xác minh chữ ký, không cần tra cứu database mỗi request.

### 2. Vì sao cần Refresh Token?
- Access Token mang tính Stateless nên **không thể thu hồi (revoke) tức thì** trước khi hết hạn.
- Do đó: Access Token chỉ nên sống rất ngắn (ví dụ: 15 phút).
- Khi Access Token hết hạn, client gửi **Refresh Token** (lưu an toàn trong Database và HttpOnly Cookie) lên endpoint `/refresh` để cấp Access Token mới mà người dùng không bị văng ra.",
                AnswerMarkdownEn = @"### 1. JWT Structure
`Header.Payload.Signature`. Stateless: The server verifies the signature using a private secret key without database lookup.

### 2. Why Refresh Tokens?
Access tokens cannot be easily revoked prior to expiration.
Keep access tokens short-lived (15 mins), and use a database-backed, revocable Refresh Token in an HttpOnly cookie to issue new tokens.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Khi được hỏi 'Làm sao vô hiệu hóa người dùng ngay lập tức khi dùng JWT?', hãy trả lời: 'Xóa Refresh Token trong DB và hạ thời gian sống Access Token xuống mức tối thiểu (5-15 phút)'.",
                InterviewTipEn = "💡 Interview Tip: To revoke a compromised JWT user immediately, revoke the refresh token in the database and enforce short access token TTL."
            },

            // ==========================================
            // TOPIC 6: CLEAN ARCHITECTURE & CLOUD (19 - 20)
            // ==========================================
            new()
            {
                Slug = "clean-architecture-dependency-rule",
                Topic = FlashcardTopic.ArchitectureCloud,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 19,
                QuestionVi = "Quy tắc phụ thuộc (Dependency Rule) trong Clean Architecture là gì? Vì sao tầng Domain không bao giờ được phụ thuộc vào Entity Framework Core?",
                QuestionEn = "What is the Dependency Rule in Clean Architecture? Why must the Domain layer never depend on EF Core?",
                QuickHintVi = "Mũi tên phụ thuộc luôn hướng vào trong, Domain ở lõi trung tâm, cơ sở dữ liệu chỉ là chi tiết.",
                QuickHintEn = "Dependencies point inward, Domain at center, database is merely a detail.",
                AnswerMarkdownVi = @"### 1. Quy tắc phụ thuộc cốt lõi
- **Mũi tên phụ thuộc chỉ được phép hướng vào trong**: Tầng ngoài (Infrastructure, Web API, UI) phụ thuộc vào tầng trong (Application, Domain). Tầng trong KHÔNG BIẾT GÌ về tầng ngoài.
- `Domain` là trung tâm của toàn bộ hệ thống (chứa Entities, Value Objects, Domain Events, Business Rules).

### 2. Tại sao Domain không phụ thuộc EF Core?
- **Nguyên lý độc lập công nghệ**: Nghiệp vụ của doanh nghiệp (Domain) không được trói chặt vào bất kỳ ORM hay nhà cung cấp cơ sở dữ liệu nào.
- Nếu Domain phụ thuộc `Microsoft.EntityFrameworkCore`, bạn sẽ bị cám dỗ nhét các thuộc tính gắn chặt với DB (`[Table]`, `[ForeignKey]`) vào Entity nghiệp vụ, làm mất đi tính thuần khiết (POCO) và gây khó khăn cực lớn khi viết Unit Test hoặc chuyển đổi công nghệ.",
                AnswerMarkdownEn = @"### 1. The Dependency Rule
Dependencies must point inward toward higher-level policies. The Domain is at the center and knows nothing about databases or frameworks.

### 2. Why Domain Avoids EF Core
Domain represents enterprise business rules. Binding it to `Microsoft.EntityFrameworkCore` couples business logic to database implementation details, harming testability and maintainability.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Hãy nhắc tới khái niệm 'Inversion of Control': Tầng Infrastructure triển khai Interface do Tầng Application hoặc Domain định nghĩa.",
                InterviewTipEn = "💡 Interview Tip: Mention Dependency Inversion: Infrastructure implements interfaces defined in Application/Domain."
            },

            new()
            {
                Slug = "cache-aside-pattern-and-stampede",
                Topic = FlashcardTopic.ArchitectureCloud,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 20,
                QuestionVi = "Mô hình Cache-Aside là gì? Làm thế nào để giải quyết vấn đề Cache Stampede (Dog-piling) trong hệ thống có lượng truy cập lớn?",
                QuestionEn = "What is the Cache-Aside pattern? How do you solve the Cache Stampede (Dog-piling) problem under high concurrency?",
                QuickHintVi = "Đọc cache trước, cache miss thì đọc DB rồi ghi cache, khóa phân tán SemaphoreSlim/RedLock, HybridCache .NET 9.",
                QuickHintEn = "Read cache first, fetch DB on miss, lock via SemaphoreSlim/RedLock, HybridCache in .NET 9.",
                AnswerMarkdownVi = @"### 1. Cache-Aside Pattern
1. Ứng dụng nhận yêu cầu $\rightarrow$ Kiểm tra Cache trước.
2. Nếu có trong Cache (Cache Hit): Trả về dữ liệu ngay.
3. Nếu không có (Cache Miss): Truy vấn cơ sở dữ liệu, ghi dữ liệu vào Cache kèm thời gian hết hạn (TTL), rồi trả về client.

### 2. Hiểm họa Cache Stampede (Dog-piling)
Xảy ra khi một cache key có lượng truy cập cực lớn bị hết hạn (expired). Ngay lập tức hàng nghìn request đồng thời gặp Cache Miss và cùng ùa vào truy vấn Database $\rightarrow$ Gây nghẽn và sập cơ sở dữ liệu!

### 3. Giải pháp khắc phục
1. **Khóa luồng (Locking / SemaphoreSlim)**: Chỉ cho phép request đầu tiên truy vấn DB và ghi cache; các request còn lại chờ nhận dữ liệu từ cache.
2. **Khóa phân tán (Distributed Lock với Redis RedLock)** trong môi trường nhiều server.
3. **Thư viện HybridCache (.NET 9+)**: Tự động giải quyết triệt để Cache Stampede (Stampede protection out-of-the-box).",
                AnswerMarkdownEn = @"### 1. Cache-Aside Pattern
Check cache first; on miss, query database, store into cache with TTL, and return.

### 2. Cache Stampede Pitfall
When a hot key expires under high concurrency, thousands of simultaneous misses overwhelm the database.

### 3. Solutions
- Locking with `SemaphoreSlim` (single instance) or Redis RedLock (distributed).
- `.NET 9+ HybridCache` with built-in stampede protection.",
                InterviewTipVi = "💡 Mẹo phỏng vấn: Đề cập đến tính năng mới `HybridCache` trong .NET 9/10 để thể hiện bạn luôn cập nhật công nghệ mới nhất của hệ sinh thái .NET!",
                InterviewTipEn = "💡 Interview Tip: Mentioning `HybridCache` in .NET 9/10 proves you stay cutting-edge with modern Microsoft engineering."
            }
        };

        context.InterviewFlashcards.AddRange(cards);
        await context.SaveChangesAsync();
    }
}
