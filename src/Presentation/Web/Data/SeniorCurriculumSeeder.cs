using Microsoft.EntityFrameworkCore;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Data;

public static class SeniorCurriculumSeeder
{
    public static async Task SeedSeniorCurriculumAsync(AppDbContext context)
    {
        // ---------------------------------------------------------------------------------
        // 1. Ensure the 8 Comprehensive Catalogs (Categories) Exist & Have Sequential Order
        // ---------------------------------------------------------------------------------
        var cat1 = await EnsureCategoryAsync(context,
            slug: "csharp-co-ban",
            name: "1. C# & .NET Core Nền tảng Chuyên sâu",
            description: "Làm chủ kiến trúc .NET Runtime (CLR, JIT, IL Code), kiểu dữ liệu nguyên thủy, bộ nhớ Stack & Heap, quản lý giá trị Nullable, luồng điều khiển và xử lý ngoại lệ kiên cố.",
            iconClass: "bi-terminal-fill",
            badgeColor: "success",
            orderIndex: 1,
            level: DifficultyLevel.Beginner);

        var cat2 = await EnsureCategoryAsync(context,
            slug: "oop-csharp-nang-cao",
            name: "2. OOP & Kỹ thuật C# Nâng cao",
            description: "Khai phá tư duy hướng đối tượng thực chiến, Generic Constraints, Delegate, Event, LINQ internals, Lập trình Bất đồng bộ Async/Await và Modern C# (C# 10 - 14).",
            iconClass: "bi-layers-fill",
            badgeColor: "info",
            orderIndex: 2,
            level: DifficultyLevel.Intermediate);

        var cat3 = await EnsureCategoryAsync(context,
            slug: "csharp-dsa-leetcode",
            name: "3. Cấu trúc Dữ liệu, Giải thuật & LeetCode với C#",
            description: "Chuyên đề độc lập rèn luyện tư duy giải thuật, phân tích Big-O, cấu trúc dữ liệu kinh điển và các dạng bài LeetCode (Easy/Medium/Hard) được giải tối ưu bằng C# hiện đại.",
            iconClass: "bi-diagram-3-fill",
            badgeColor: "secondary",
            orderIndex: 3,
            level: DifficultyLevel.Intermediate);

        var cat4 = await EnsureCategoryAsync(context,
            slug: "design-patterns-architecture",
            name: "4. Design Patterns & Kiến trúc Phần mềm .NET",
            description: "Chuyên đề làm chủ mẫu thiết kế kinh điển (GoF Patterns) và kiến trúc doanh nghiệp: Dependency Injection, CQRS, Mediator, Outbox, Repository, Clean Architecture.",
            iconClass: "bi-boxes",
            badgeColor: "primary",
            orderIndex: 4,
            level: DifficultyLevel.Advanced);

        var cat5 = await EnsureCategoryAsync(context,
            slug: "entity-framework-core",
            name: "5. Entity Framework Core, Dapper & Cơ sở Dữ liệu Hiệu năng cao",
            description: "Nắm vững ORM Code-First, cấu hình quan hệ phức tạp với Fluent API, loại bỏ lỗi N+1, tối ưu AsNoTracking, Split Query, giao dịch ACID và tích hợp Dapper Micro-ORM.",
            iconClass: "bi-database-fill-gear",
            badgeColor: "warning",
            orderIndex: 5,
            level: DifficultyLevel.Intermediate);

        var cat6 = await EnsureCategoryAsync(context,
            slug: "aspnet-core-mvc-api",
            name: "6. ASP.NET Core Web API, Minimal APIs & Enterprise Backend",
            description: "Thiết kế RESTful Web API chuẩn quốc tế, khai thác Minimal APIs siêu tốc độ, Dependency Injection chuyên sâu, bảo mật xác thực JWT/RBAC và kiểm thử tự động.",
            iconClass: "bi-globe2",
            badgeColor: "danger",
            orderIndex: 6,
            level: DifficultyLevel.Advanced);

        var cat7 = await EnsureCategoryAsync(context,
            slug: "microservices-distributed-systems",
            name: "7. Microservices, Distributed Systems & Event-Driven (.NET 10)",
            description: "Xây dựng hệ sinh thái vi dịch vụ đám mây, YARP API Gateway, Message Bus với RabbitMQ & Apache Kafka, gRPC tốc độ cao, distributed resilience và tracing.",
            iconClass: "bi-hdd-network-fill",
            badgeColor: "dark",
            orderIndex: 7,
            level: DifficultyLevel.Advanced);

        var cat8 = await EnsureCategoryAsync(context,
            slug: "performance-diagnostics-devops",
            name: "8. Tối ưu Hiệu năng CLR, Diagnostics & Cloud-Native DevOps",
            description: "Chuyên sâu tối ưu hóa cấp thấp: CLR Garbage Collection, Span<T>/Memory<T>, BenchmarkDotNet, phân tích Memory Leak với dotnet-dump/trace và đóng gói Docker/Kubernetes.",
            iconClass: "bi-cpu-fill",
            badgeColor: "info",
            orderIndex: 8,
            level: DifficultyLevel.Advanced);

        await context.SaveChangesAsync();

        // ---------------------------------------------------------------------------------
        // 2. Re-align Titles & OrderIndex for Categories 1, 2, 5, 6 (Strict Sequential 1..N)
        // ---------------------------------------------------------------------------------
        await AlignCategory1TutorialsAsync(context, cat1.Id);
        await AlignCategory2TutorialsAsync(context, cat2.Id);
        await AlignCategory5TutorialsAsync(context, cat5.Id);
        await AlignCategory6TutorialsAsync(context, cat6.Id);

        // ---------------------------------------------------------------------------------
        // 3. Seed Specialized Catalogs: DSA/LeetCode, Design Patterns, Microservices, DevOps
        // ---------------------------------------------------------------------------------
        await SeedDsaLeetCodeTutorialsAsync(context, cat3.Id);
        await SeedDesignPatternTutorialsAsync(context, cat4.Id);
        await SeedAdditionalEfCoreTutorialsAsync(context, cat5.Id);
        await SeedMicroservicesTutorialsAsync(context, cat7.Id);
        await SeedPerformanceDevOpsTutorialsAsync(context, cat8.Id);

        await context.SaveChangesAsync();
    }

    private static async Task<Category> EnsureCategoryAsync(
        AppDbContext context,
        string slug,
        string name,
        string description,
        string iconClass,
        string badgeColor,
        int orderIndex,
        DifficultyLevel level)
    {
        var cat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (cat == null)
        {
            cat = new Category
            {
                Slug = slug,
                Name = name,
                Description = description,
                IconClass = iconClass,
                BadgeColor = badgeColor,
                OrderIndex = orderIndex,
                Level = level
            };
            context.Categories.Add(cat);
        }
        else
        {
            cat.Name = name;
            cat.Description = description;
            cat.IconClass = iconClass;
            cat.BadgeColor = badgeColor;
            cat.OrderIndex = orderIndex;
            cat.Level = level;
        }
        return cat;
    }

    private static async Task AlignCategory1TutorialsAsync(AppDbContext context, int categoryId)
    {
        var map = new Dictionary<string, (int OrderIndex, string Title)>
        {
            ["tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong"] =
                (1, "Bài 1: Tổng quan Kiến trúc .NET Runtime (CLR, JIT) & Cài đặt Môi trường"),
            ["cu-phap-csharp-nen-tang-bien-kieu-du-lieu-luong-dieu-khien"] =
                (2, "Bài 2: Cú pháp C# Nền tảng - Biến, Kiểu Dữ liệu & Luồng Điều khiển"),
            ["value-types-reference-types-struct-enum-nullable"] =
                (3, "Bài 3: Value Types, Reference Types, Cơ chế Bộ nhớ Stack vs Heap & Nullable"),
            ["cau-truc-du-lieu-xu-ly-chuoi-list-dictionary-stringbuilder"] =
                (4, "Bài 4: Cấu trúc Dữ liệu Nền tảng - Array, List<T>, Dictionary & StringBuilder"),
            ["phuong-thuc-va-tham-so-trong-csharp-ref-out-in-params"] =
                (5, "Bài 5: Phương thức (Methods) & Kỹ thuật Truyền Tham số (ref, out, in, params)"),
            ["xu-ly-ngoai-le-chuyen-sau-try-catch-exception-filters"] =
                (6, "Bài 6: Xử lý Ngoại lệ Chuyên sâu - try-catch-finally, Exception Filters & Custom Exceptions")
        };

        foreach (var kvp in map)
        {
            var tut = await context.Tutorials.FirstOrDefaultAsync(t => t.Slug == kvp.Key);
            if (tut != null)
            {
                tut.CategoryId = categoryId;
                tut.OrderIndex = kvp.Value.OrderIndex;
                tut.Title = kvp.Value.Title;
            }
        }
    }

    private static async Task AlignCategory2TutorialsAsync(AppDbContext context, int categoryId)
    {
        var map = new Dictionary<string, (int OrderIndex, string Title)>
        {
            ["4-tru-cot-lap-trinh-huong-doi-tuong-oop-trong-csharp"] =
                (1, "Bài 1: 4 Trụ cột Lập trình Hướng đối tượng (OOP) Thực chiến trong C#"),
            ["interface-va-generics-trong-csharp-hien-dai"] =
                (2, "Bài 2: Interface Segregation, Abstract Class & Generics Nâng cao"),
            ["delegate-event-action-func-trong-csharp"] =
                (3, "Bài 3: Delegate, Event & Built-in Action / Func trong C# Hiện đại"),
            ["linq-language-integrated-query-va-lambda-expressions"] =
                (4, "Bài 4: LINQ (Language Integrated Query) & Biểu thức Lambda Internals"),
            ["lap-trinh-bat-dong-bo-async-await-trong-dotnet"] =
                (5, "Bài 5: Lập trình Bất đồng bộ (Async / Await) trong .NET & Tránh Deadlock"),
            ["da-luong-song-song-task-parallel-library-channels-cancellation"] =
                (6, "Bài 6: Lập trình Song song & Đồng thời - TPL, Channels & CancellationToken"),
            ["modern-csharp-records-pattern-matching-switch-expressions"] =
                (7, "Bài 7: Modern C# - Records, Pattern Matching & Switch Expressions (C# 10 - 14)")
        };

        foreach (var kvp in map)
        {
            var tut = await context.Tutorials.FirstOrDefaultAsync(t => t.Slug == kvp.Key);
            if (tut != null)
            {
                tut.CategoryId = categoryId;
                tut.OrderIndex = kvp.Value.OrderIndex;
                tut.Title = kvp.Value.Title;
            }
        }
    }

    private static async Task AlignCategory5TutorialsAsync(AppDbContext context, int categoryId)
    {
        var map = new Dictionary<string, (int OrderIndex, string Title)>
        {
            ["entity-framework-core-code-first-dbcontext-migrations"] =
                (1, "Bài 1: Entity Framework Core - Code-First Approach, DbContext & Migrations"),
            ["thiet-ke-quan-he-du-lieu-ef-core-fluent-api"] =
                (2, "Bài 2: Thiết kế Quan hệ Dữ liệu với EF Core Fluent API (1-1, 1-N, N-N)"),
            ["toi-uu-truy-van-hieu-nang-ef-core-asnotracking-loading"] =
                (3, "Bài 3: Tối ưu Truy vấn & Hiệu năng EF Core - AsNoTracking & Split Queries"),
            ["transactions-concurrency-control-repository-pattern-ef-core"] =
                (4, "Bài 4: Transactions, Concurrency Control & Optimistic Locking trong EF Core")
        };

        foreach (var kvp in map)
        {
            var tut = await context.Tutorials.FirstOrDefaultAsync(t => t.Slug == kvp.Key);
            if (tut != null)
            {
                tut.CategoryId = categoryId;
                tut.OrderIndex = kvp.Value.OrderIndex;
                tut.Title = kvp.Value.Title;
            }
        }
    }

    private static async Task AlignCategory6TutorialsAsync(AppDbContext context, int categoryId)
    {
        var map = new Dictionary<string, (int OrderIndex, string Title)>
        {
            ["kien-truc-aspnet-core-mvc-programcs-middleware-controller"] =
                (1, "Bài 1: Kiến trúc ASP.NET Core - Program.cs, Middleware & Controller Pipeline"),
            ["xay-dung-restful-web-api-chuan-voi-aspnet-core"] =
                (2, "Bài 2: Xây dựng RESTful Web API Chuẩn Quốc tế & ProblemDetails (RFC 7807)"),
            ["dependency-injection-va-nguyen-ly-solid-trong-aspnet-core"] =
                (3, "Bài 3: Dependency Injection (DI) Chuyên sâu & 5 Nguyên lý SOLID"),
            ["minimal-apis-aspnet-core-high-performance-endpoints"] =
                (4, "Bài 4: Minimal APIs trong ASP.NET Core - High-Performance Endpoints & TypedResults"),
            ["xac-thuc-phan-quyen-aspnet-core-jwt-bearer-identity-rbac"] =
                (5, "Bài 5: Xác thực & Phân quyền Web API - JWT Bearer, Identity & RBAC"),
            ["logging-caching-toi-uu-hieu-nang-web-api-serilog-redis"] =
                (6, "Bài 6: Logging, Caching & Tối ưu Hiệu năng Web API - Serilog & Redis")
        };

        foreach (var kvp in map)
        {
            var tut = await context.Tutorials.FirstOrDefaultAsync(t => t.Slug == kvp.Key);
            if (tut != null)
            {
                tut.CategoryId = categoryId;
                tut.OrderIndex = kvp.Value.OrderIndex;
                tut.Title = kvp.Value.Title;
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // Catalog 3: Data Structures, Algorithms & LeetCode in C#
    // ---------------------------------------------------------------------------------
    private static async Task SeedDsaLeetCodeTutorialsAsync(AppDbContext context, int categoryId)
    {
        #region Bài 1: Big-O & .NET Collections Internals
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "dsa-big-o-complexity-va-net-collections-internals"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 1: Phân tích Độ phức tạp Thuật toán (Big-O) & Bản chất .NET Collections",
                Slug = "dsa-big-o-complexity-va-net-collections-internals",
                Summary = "Phân tích Big-O Time & Space Complexity trong .NET, cơ chế cấp phát bộ nhớ nội tại của List<T>, Dictionary<TKey, TValue>, HashSet<T> và chi phí GC.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 1,
                IsFeatured = true,
                ContentMarkdown = @"# Phân tích Độ phức tạp Thuật toán (Big-O) & Bản chất .NET Collections

Trong kỹ thuật phần mềm cấp cao (Senior Engineer), việc lựa chọn cấu trúc dữ liệu không chỉ dừng lại ở tính năng tiện lợi mà là sự đánh đổi giữa **Thời gian tính toán (Time Complexity)**, **Dung lượng bộ nhớ (Space Complexity)** và **Áp lực lên bộ thu gom rác (GC Pressure)**.

---

## 1. Bảng tra cứu Độ phức tạp các Collection cốt lõi trong .NET

| Cấu trúc dữ liệu | Truy cập ngẫu nhiên | Tìm kiếm (Search) | Thêm (Insert) | Xóa (Delete) | Cơ chế mở rộng ngầm |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`Array T[]`** | `O(1)` | `O(N)` | N/A (Cố định) | N/A | Không thể mở rộng |
| **`List<T>`** | `O(1)` | `O(N)` | `O(1)` Amortized | `O(N)` | Tự động nhân đôi kích thước (x2) |
| **`Dictionary<TKey, TValue>`** | N/A | `O(1)` Average | `O(1)` Average | `O(1)` Average | Mở rộng bảng băm đến số nguyên tố kế tiếp |
| **`HashSet<T>`** | N/A | `O(1)` Average | `O(1)` Average | `O(1)` Average | Bảng băm không lưu Value |
| **`LinkedList<T>`** | `O(N)` | `O(N)` | `O(1)` khi có Node | `O(1)` khi có Node | Node riêng lẻ trên Managed Heap |
| **`Queue<T>` / `Stack<T>`** | N/A | `O(N)` | `O(1)` Push/Enqueue | `O(1)` Pop/Dequeue | Vòng lặp đệm circular buffer (x2) |

---

## 2. Giải phẫu cấu trúc `List<T>` trong CoreCLR

`List<T>` thực chất là một lớp bọc (wrapper) xung quanh mảng tĩnh `_items`.
Khi bạn gọi `list.Add(item)` và `_size == _items.Length`:

1. CLR sẽ cấp phát một mảng mới trên Managed Heap với dung lượng gấp đôi:
   `NewCapacity = (Capacity == 0) ? 4 : Capacity * 2;`
2. Gọi hàm nội tại `Array.Copy` để chuyển toàn bộ phần tử sang mảng mới.
3. Mảng cũ trở thành rác (Garbage), chờ GC thu gom.

```csharp
// KÉM TỐI ƯU: Gây ra 10 lần cấp phát mảng mới và xả rác cho GC khi thêm 100.000 phần tử
var badList = new List<int>();
for (int i = 0; i < 100_000; i++)
{
    badList.Add(i);
}

// CHUẨN SENIOR: Cấp phát đúng 1 lần duy nhất, zero reallocations!
var goodList = new List<int>(capacity: 100_000);
for (int i = 0; i < 100_000; i++)
{
    goodList.Add(i);
}
```

---

## 3. Kiến trúc Bên trong của `Dictionary<TKey, TValue>`

`Dictionary` trong .NET sử dụng kỹ thuật **Chaining with an Array** (không dùng con trỏ LinkedList mà dùng chỉ số index mảng để tối ưu CPU Cache Locality). Nó bao gồm 2 mảng chính:
* `int[] _buckets`: Lưu vị trí đầu tiên của chuỗi băm.
* `Entry[] _entries`: Lưu thông tin thực tế:
  ```csharp
  private struct Entry
  {
      public uint hashCode;
      public int next; // Trỏ tới phần tử tiếp theo nếu xảy ra Hash Collision
      public TKey key;
      public TValue value;
  }
  ```

> [!IMPORTANT]
> **Quy tắc vàng:** Luôn cung cấp `IEqualityComparer<TKey>` tùy chỉnh hoặc cài đặt `IEquatable<T>` cho kiểu Key tùy biến để tránh Boxing/Unboxing và suy giảm hiệu năng băm về $O(N)$.
"
            });
        }
        #endregion

        #region Bài 2: Two Pointers & Sliding Window
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "dsa-two-pointers-sliding-window-prefix-sum-csharp"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 2: Mảng & Chuỗi LeetCode - Kỹ thuật Two Pointers, Sliding Window & Prefix Sum",
                Slug = "dsa-two-pointers-sliding-window-prefix-sum-csharp",
                Summary = "Làm chủ 3 kỹ thuật kinh điển giúp giảm độ phức tạp từ O(N^2) xuống O(N) khi giải quyết các bài toán mảng và chuỗi trên LeetCode với C#.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 2,
                IsFeatured = true,
                ContentMarkdown = @"# Kỹ thuật Two Pointers, Sliding Window & Prefix Sum trong C#

Các bài toán xử lý mảng và chuỗi chiếm hơn 40% câu hỏi phỏng vấn thuật toán tại các công ty công nghệ lớn. Việc sử dụng 2 vòng lặp lồng nhau $O(N^2)$ thường dẫn đến lỗi **Time Limit Exceeded (TLE)**.

---

## 1. Kỹ thuật Two Pointers (Hai con trỏ)

### Trường hợp 1: Hai con trỏ đối hướng (Left & Right)
Áp dụng khi mảng đã được sắp xếp. Ta đặt con trỏ `left = 0` và `right = arr.Length - 1`.

**Bài toán mẫu (LeetCode 167 - Two Sum II):**
Tìm 2 số có tổng bằng `target` trong mảng đã sắp xếp.

```csharp
public static int[] TwoSumSorted(int[] numbers, int target)
{
    int left = 0;
    int right = numbers.Length - 1;

    while (left < right)
    {
        int sum = numbers[left] + numbers[right];
        if (sum == target)
        {
            return [left + 1, right + 1]; // Trả về 1-based index
        }
        else if (sum < target)
        {
            left++; // Cần tăng tổng -> dịch con trỏ trái sang phải
        }
        else
        {
            right--; // Cần giảm tổng -> dịch con trỏ phải sang trái
        }
    }

    return [];
}
```
* **Time Complexity:** `O(N)`
* **Space Complexity:** `O(1)`

---

## 2. Kỹ thuật Sliding Window (Cửa sổ trượt)

Áp dụng cho các bài toán tìm dãy con liên tiếp (contiguous subarray/substring) thỏa mãn điều kiện độ dài lớn nhất, nhỏ nhất hoặc chứa các ký tự không trùng lặp.

**Bài toán mẫu (LeetCode 3 - Longest Substring Without Repeating Characters):**
Tìm độ dài chuỗi con dài nhất không chứa ký tự lặp lại.

```csharp
public static int LengthOfLongestSubstring(string s)
{
    if (string.IsNullOrEmpty(s)) return 0;

    var lastIndexMap = new Dictionary<char, int>();
    int maxLength = 0;
    int windowStart = 0;

    for (int windowEnd = 0; windowEnd < s.Length; windowEnd++)
    {
        char currentChar = s[windowEnd];

        // Nếu ký tự đã xuất hiện trong cửa sổ hiện tại, co cửa sổ lại
        if (lastIndexMap.TryGetValue(currentChar, out int prevIndex) && prevIndex >= windowStart)
        {
            windowStart = prevIndex + 1;
        }

        lastIndexMap[currentChar] = windowEnd;
        maxLength = Math.Max(maxLength, windowEnd - windowStart + 1);
    }

    return maxLength;
}
```

---

## 3. Kỹ thuật Mảng Tiền Tố (Prefix Sum)

Prefix Sum cho phép bạn truy vấn tổng các phần tử trong khoảng $[L, R]$ với thời gian `O(1)` sau khi tiền xử lý `O(N)`.

`Prefix[i] = arr[0] + arr[1] + ... + arr[i-1]`
`Sum(L, R) = Prefix[R + 1] - Prefix[L]`
"
            });
        }
        #endregion

        #region Bài 3: Monotonic Stack, Queue & PriorityQueue
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "dsa-monotonic-stack-queue-priorityqueue-csharp"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 3: Monotonic Stack, Queue & Hàng đợi Ưu tiên (PriorityQueue trong C# 10+)",
                Slug = "dsa-monotonic-stack-queue-priorityqueue-csharp",
                Summary = "Tìm hiểu Monotonic Stack tìm phần tử lớn hơn tiếp theo O(N) và ứng dụng cấu trúc PriorityQueue<TElement, TPriority> trong .NET 10 cho bài toán Top K.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 3,
                IsFeatured = false,
                ContentMarkdown = @"# Monotonic Stack & PriorityQueue trong .NET Hiện Đại

---

## 1. Monotonic Stack (Ngăn xếp Đơn điệu)

Monotonic Stack là ngăn xếp mà các phần tử bên trong luôn duy trì thứ tự tăng dần hoặc giảm dần. Nó là chìa khóa để giải quyết bài toán tìm **Phần tử lớn hơn/nhỏ hơn gần nhất (Next Greater Element)** trong thời gian tuyến tính $O(N)$ thay vì $O(N^2)$.

```csharp
public static int[] NextGreaterElements(int[] nums)
{
    int n = nums.Length;
    int[] result = new int[n];
    Array.Fill(result, -1);
    var stack = new Stack<int>(); // Lưu index

    for (int i = 0; i < n; i++)
    {
        while (stack.Count > 0 && nums[stack.Peek()] < nums[i])
        {
            int topIndex = stack.Pop();
            result[topIndex] = nums[i];
        }
        stack.Push(i);
    }

    return result;
}
```

---

## 2. `PriorityQueue<TElement, TPriority>` trong C# 10 - 14

Trước .NET 6, C# không có sẵn Min-Heap/Max-Heap chuẩn. Các kỹ sư phải dùng `SortedSet<T>` (dựa trên Red-Black Tree với overhead lớn). Từ .NET 6+, CoreCLR đã tích hợp `PriorityQueue<TElement, TPriority>` dựng trên 4-ary Min-Heap có hiệu năng vượt trội.

```csharp
// Tìm K phần tử có tần suất xuất hiện nhiều nhất (Top K Frequent Elements)
public static IList<int> TopKFrequent(int[] nums, int k)
{
    var frequencies = new Dictionary<int, int>();
    foreach (var num in nums)
    {
        frequencies[num] = frequencies.GetValueOrDefault(num, 0) + 1;
    }

    // Min-Heap lưu trữ phần tử với độ ưu tiên là tần suất xuất hiện
    var minHeap = new PriorityQueue<int, int>();

    foreach (var (val, count) in frequencies)
    {
        minHeap.Enqueue(val, count);
        if (minHeap.Count > k)
        {
            minHeap.Dequeue(); // Loại bỏ phần tử có tần suất thấp nhất
        }
    }

    var result = new List<int>(k);
    while (minHeap.Count > 0)
    {
        result.Add(minHeap.Dequeue());
    }
    return result;
}
```
"
            });
        }
        #endregion

        #region Bài 4: LinkedList, Binary Tree & BFS/DFS
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "dsa-linkedlist-binary-tree-bfs-dfs-csharp"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 4: Danh sách Liên kết (LinkedList), Cây Nhị phân & Duyệt BFS/DFS trong C#",
                Slug = "dsa-linkedlist-binary-tree-bfs-dfs-csharp",
                Summary = "Kỹ thuật đảo ngược LinkedList, phát hiện vòng lặp Floyd, kiểm tra cây nhị phân tìm kiếm (BST) và thuật toán duyệt Breadth-First & Depth-First Search.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 4,
                IsFeatured = false,
                ContentMarkdown = @"# Cấu trúc Cây Nhị phân & Kỹ thuật Duyệt BFS/DFS trong C#

---

## 1. Định nghĩa Node Cây Nhị Phân Hiện Đại trong C#

```csharp
public sealed class TreeNode<T>
{
    public T Value { get; set; }
    public TreeNode<T>? Left { get; set; }
    public TreeNode<T>? Right { get; set; }

    public TreeNode(T value, TreeNode<T>? left = null, TreeNode<T>? right = null)
    {
        Value = value;
        Left = left;
        Right = right;
    }
}
```

---

## 2. Duyệt Cây theo Chiều rộng (Breadth-First Search - BFS)

BFS duyệt cây theo từng tầng (Level-order). Cấu trúc dữ liệu thiết yếu là **`Queue<T>`**.

```csharp
public static List<List<T>> LevelOrderTraversal<T>(TreeNode<T>? root)
{
    var result = new List<List<T>>();
    if (root == null) return result;

    var queue = new Queue<TreeNode<T>>();
    queue.Enqueue(root);

    while (queue.Count > 0)
    {
        int levelSize = queue.Count;
        var currentLevel = new List<T>(levelSize);

        for (int i = 0; i < levelSize; i++)
        {
            var node = queue.Dequeue();
            currentLevel.Add(node.Value);

            if (node.Left != null) queue.Enqueue(node.Left);
            if (node.Right != null) queue.Enqueue(node.Right);
        }

        result.Add(currentLevel);
    }

    return result;
}
```

---

## 3. Duyệt Cây theo Chiều sâu (Depth-First Search - DFS)

DFS đi sâu đến các node lá trước khi quay lui (Backtracking). Có 3 thứ tự duyệt chính:
* **Pre-order:** Root → Left → Right
* **In-order (quan trọng cho Binary Search Tree):** Left → Root → Right (cho ra dãy tăng dần!)
* **Post-order:** Left → Right → Root
"
            });
        }
        #endregion

        #region Bài 5: Dynamic Programming in C#
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "dsa-dynamic-programming-memoization-tabulation-csharp"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 5: Quy hoạch Động (Dynamic Programming) - Từ Memoization đến Tabulation",
                Slug = "dsa-dynamic-programming-memoization-tabulation-csharp",
                Summary = "Làm chủ phương pháp giải toán Quy hoạch Động trong C#: Nhận diện bài toán con gối nhau, kỹ thuật Top-Down với Memoization và Bottom-Up Tabulation O(1) space.",
                EstimatedReadingMinutes = 13,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 5,
                IsFeatured = false,
                ContentMarkdown = @"# Nghệ Thuật Quy Hoạch Động (Dynamic Programming) trong C#

Quy hoạch động (DP) là phương pháp tối ưu hóa bài toán bằng cách chia bài toán lớn thành các bài toán con gối nhau (Overlapping Subproblems) và có cấu trúc con tối ưu (Optimal Substructure).

---

## 1. Hai Cách Tiếp Cận Cốt Lõi

```
┌────────────────────────────────────────────────────────┐
│               QUY HOẠCH ĐỘNG (DP)                      │
├──────────────────────────┬─────────────────────────────┤
│ 1. Top-Down (Memoization)│ Đệ quy có nhớ kết quả       │
│ 2. Bottom-Up (Tabulation)│ Vòng lặp điền bảng dữ liệu │
└──────────────────────────┴─────────────────────────────┘
```

---

## 2. Bài toán Đổi Tiền (LeetCode 322 - Coin Change)

Cho các đồng xu mệnh giá khác nhau và số tiền `amount`. Tìm số lượng đồng xu ít nhất để đổi được số tiền đó.

```csharp
public static int CoinChange(int[] coins, int amount)
{
    // dp[i] lưu số lượng đồng xu tối thiểu để đổi ra giá trị i
    int[] dp = new int[amount + 1];
    Array.Fill(dp, amount + 1); // Khởi tạo giá trị lớn vô cực
    dp[0] = 0; // Để đổi ra 0 đồng cần 0 đồng xu

    for (int i = 1; i <= amount; i++)
    {
        foreach (int coin in coins)
        {
            if (i - coin >= 0)
            {
                dp[i] = Math.Min(dp[i], dp[i - coin] + 1);
            }
        }
    }

    return dp[amount] > amount ? -1 : dp[amount];
}
```
* **Time Complexity:** `O(N * amount)` với N là số đồng xu.
* **Space Complexity:** `O(amount)`.
"
            });
        }
        #endregion

        #region Bài 6: Graph Algorithms in C#
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "dsa-graph-algorithms-dijkstra-topological-sort-dsu-csharp"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 6: Thuật toán Đồ thị Nâng cao - Dijkstra, Topological Sort & Disjoint Set Union (DSU)",
                Slug = "dsa-graph-algorithms-dijkstra-topological-sort-dsu-csharp",
                Summary = "Triển khai thuật toán Dijkstra tìm đường đi ngắn nhất với PriorityQueue, sắp xếp thứ tự phụ thuộc (Topological Sort) và cấu trúc Disjoint Set Union trong C#.",
                EstimatedReadingMinutes = 14,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 6,
                IsFeatured = true,
                ContentMarkdown = @"# Thuật toán Đồ thị Thực Chiến trong .NET 10

---

## 1. Biểu diễn Đồ thị trong C#

Trong C# doanh nghiệp, cách phổ biến nhất để biểu diễn đồ thị là **Danh sách kề (Adjacency List)** sử dụng `Dictionary<TNode, List<(TNode Neighbor, int Weight)>>`.

---

## 2. Thuật toán Dijkstra tìm Đường đi ngắn nhất

Sử dụng `PriorityQueue<int, int>` của .NET 10 giúp giảm độ phức tạp xuống `O((E + V) * log V)`.

```csharp
public static int[] Dijkstra(int totalNodes, List<(int U, int V, int Weight)> edges, int startNode)
{
    var graph = new Dictionary<int, List<(int To, int Weight)>>();
    for (int i = 0; i < totalNodes; i++) graph[i] = [];

    foreach (var (u, v, w) in edges)
    {
        graph[u].Add((v, w));
        graph[v].Add((u, w)); // Đồ thị vô hướng
    }

    int[] distances = new int[totalNodes];
    Array.Fill(distances, int.MaxValue);
    distances[startNode] = 0;

    var pq = new PriorityQueue<int, int>();
    pq.Enqueue(startNode, 0);

    while (pq.Count > 0)
    {
        pq.TryDequeue(out int u, out int currentDist);
        if (currentDist > distances[u]) continue;

        foreach (var (v, weight) in graph[u])
        {
            if (distances[u] + weight < distances[v])
            {
                distances[v] = distances[u] + weight;
                pq.Enqueue(v, distances[v]);
            }
        }
    }

    return distances;
}
```
"
            });
        }
        #endregion
    }

    // ---------------------------------------------------------------------------------
    // Catalog 4: Design Patterns & Software Architecture in .NET
    // ---------------------------------------------------------------------------------
    private static async Task SeedDesignPatternTutorialsAsync(AppDbContext context, int categoryId)
    {
        #region Bài 1: Creational Patterns
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "design-patterns-creational-singleton-factory-builder-dotnet"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 1: GoF Creational Patterns - Singleton Thread-safe, Factory Method & Fluent Builder",
                Slug = "design-patterns-creational-singleton-factory-builder-dotnet",
                Summary = "Thiết kế Singleton an toàn đa luồng với Lazy<T>, Factory Method linh hoạt và Fluent Builder Pattern khởi tạo đối tượng phức tạp trong C#.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 1,
                IsFeatured = true,
                ContentMarkdown = @"# Mẫu Thiết Kế Khởi Tạo (Creational Patterns) trong .NET

---

## 1. Thread-safe Singleton với `Lazy<T>`

Trong .NET, cách triển khai Singleton thanh lịch và an toàn đa luồng nhất mà không cần cơ chế `lock` cồng kềnh là sử dụng kiểu `System.Lazy<T>`.

```csharp
public sealed class CacheManager
{
    private static readonly Lazy<CacheManager> _instance = 
        new(() => new CacheManager(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static CacheManager Instance => _instance.Value;

    private CacheManager()
    {
        // Khởi tạo tài nguyên cache nặng
    }

    public void Set(string key, object value) { /* ... */ }
}
```

---

## 2. Fluent Builder Pattern

Builder tách biệt việc xây dựng một đối tượng phức tạp khỏi biểu diễn thực tế của nó, giúp mã nguồn dễ đọc hơn hàng chục lần so với truyền 10 tham số vào Constructor.

```csharp
public sealed class HttpClientRequest
{
    public string Url { get; init; } = string.Empty;
    public HttpMethod Method { get; init; } = HttpMethod.Get;
    public Dictionary<string, string> Headers { get; init; } = [];
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed class RequestBuilder
{
    private readonly HttpClientRequest _req = new();

    public RequestBuilder To(string url)
    {
        return this;
    }

    public RequestBuilder WithHeader(string key, string value)
    {
        _req.Headers[key] = value;
        return this;
    }

    public RequestBuilder WithTimeout(TimeSpan timeout)
    {
        return this;
    }

    public HttpClientRequest Build() => _req;
}
```
"
            });
        }
        #endregion

        #region Bài 2: Structural Patterns
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "design-patterns-structural-adapter-decorator-facade-proxy"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 2: GoF Structural Patterns - Adapter, Decorator, Facade & Proxy",
                Slug = "design-patterns-structural-adapter-decorator-facade-proxy",
                Summary = "Áp dụng Decorator Pattern bổ sung Caching/Logging tự động, Adapter bọc tích hợp thanh toán legacy và Facade tinh gọn luồng dịch vụ phức tạp.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 2,
                IsFeatured = false,
                ContentMarkdown = @"# Mẫu Thiết Kế Cấu Trúc (Structural Patterns) trong .NET

---

## 1. Decorator Pattern với Dependency Injection

Decorator cho phép bạn thêm hành vi mới vào một đối tượng mà không làm thay đổi mã nguồn gốc (tuân thủ Open/Closed Principle).

Ví dụ: Bọc dịch vụ `IWeatherService` bằng một lớp đệm Caching:

```csharp
public interface IWeatherService
{
    Task<string> GetForecastAsync(string city);
}

public class WeatherService : IWeatherService
{
    public async Task<string> GetForecastAsync(string city)
    {
        // Gọi API thời tiết thực tế
        return await Task.FromResult($""Nắng đẹp tại {city}"");
    }
}

// LỚP DECORATOR CACHING:
public class CachedWeatherService : IWeatherService
{
    private readonly IWeatherService _inner;
    private readonly IMemoryCache _cache;

    public CachedWeatherService(IWeatherService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<string> GetForecastAsync(string city)
    {
        return await _cache.GetOrCreateAsync($""weather:{city}"", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _inner.GetForecastAsync(city);
        }) ?? string.Empty;
    }
}
```
"
            });
        }
        #endregion

        #region Bài 3: Behavioral Patterns
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "design-patterns-behavioral-strategy-observer-command"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 3: GoF Behavioral Patterns - Strategy, Observer, Command & Chain of Responsibility",
                Slug = "design-patterns-behavioral-strategy-observer-command",
                Summary = "Xóa bỏ câu lệnh if-else lồng nhau với Strategy Pattern qua Keyed DI của .NET 10, Observer Pattern và Pipeline Chain of Responsibility.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 3,
                IsFeatured = false,
                ContentMarkdown = @"# Mẫu Thiết Kế Hành Vi (Behavioral Patterns)

---

## 1. Strategy Pattern với Keyed Services trong .NET 8/9/10

Thay vì viết câu lệnh `switch` dài hàng trăm dòng để xử lý phương thức thanh toán, ta dùng Strategy Pattern:

```csharp
public interface IPaymentStrategy
{
    Task<bool> ProcessPaymentAsync(decimal amount);
}

public class MoMoPaymentStrategy : IPaymentStrategy { /* ... */ }
public class VnpayPaymentStrategy : IPaymentStrategy { /* ... */ }
public class StripePaymentStrategy : IPaymentStrategy { /* ... */ }

// Đăng ký Keyed DI trong Program.cs:
builder.Services.AddKeyedScoped<IPaymentStrategy, MoMoPaymentStrategy>(""momo"");
builder.Services.AddKeyedScoped<IPaymentStrategy, VnpayPaymentStrategy>(""vnpay"");
builder.Services.AddKeyedScoped<IPaymentStrategy, StripePaymentStrategy>(""stripe"");

// Tiêm thẳng Strategy vào Controller:
app.MapPost(""/pay"", async ([FromKeyedServices(""momo"")] IPaymentStrategy payment, decimal amount) =>
{
    return await payment.ProcessPaymentAsync(amount);
});
```
"
            });
        }
        #endregion

        #region Bài 4: Enterprise Repository & Unit of Work
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "enterprise-patterns-repository-unit-of-work-specification"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 4: Enterprise Data Patterns - Repository Pattern, Unit of Work & Specification",
                Slug = "enterprise-patterns-repository-unit-of-work-specification",
                Summary = "Đánh giá khách quan Repository Pattern trên EF Core: Khi nào là hữu ích, khi nào là anti-pattern và cách ứng dụng Specification Pattern tái sử dụng LINQ.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 4,
                IsFeatured = false,
                ContentMarkdown = @"# Repository, Unit of Work & Specification Pattern

---

## 1. Bản chất `DbContext` trong EF Core đã là Unit of Work!

`DbContext` trong EF Core vốn dĩ đã là một sự kết hợp hoàn chỉnh của:
* **Repository:** `DbSet<T>` đóng vai trò như một kho chứa dữ liệu trừu tượng.
* **Unit of Work:** `DbContext.SaveChangesAsync()` gom toàn bộ thay đổi thành 1 giao dịch cơ sở dữ liệu duy nhất.

Khi nào cần bọc thêm Repository? Khi bạn cần che giấu EF Core hoàn toàn khỏi Domain Layer hoặc viết Unit Tests với Mock dữ liệu thuần túy trong RAM.
"
            });
        }
        #endregion

        #region Bài 5: CQRS & Mediator Pattern
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "cqrs-mediator-pattern-mediatr-pipeline-behaviors"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 5: Mẫu CQRS & Mediator Pattern - Tách biệt Command/Query với MediatR Pipeline",
                Slug = "cqrs-mediator-pattern-mediatr-pipeline-behaviors",
                Summary = "Triển khai Command Query Responsibility Segregation (CQRS) kết hợp MediatR, tạo Pipeline Behaviors tự động hóa Validation, Logging và Transactions.",
                EstimatedReadingMinutes = 13,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 5,
                IsFeatured = true,
                ContentMarkdown = @"# Kiến Trúc CQRS & Mediator Pattern với MediatR

---

## 1. CQRS là gì?

**Command Query Responsibility Segregation (CQRS)** phân chia rõ ràng hai luồng xử lý:
* **Commands:** Các thao tác làm thay đổi trạng thái hệ thống (Create, Update, Delete) - không nên trả về dữ liệu phức tạp.
* **Queries:** Các thao tác đọc dữ liệu thuần túy (Read-only) - không bao giờ thay đổi trạng thái hệ thống.

```
                  ┌──────────────┐
                  │ HTTP Request │
                  └──────┬───────┘
                         │
          ┌──────────────┴──────────────┐
          ▼                             ▼
   ┌─────────────┐               ┌─────────────┐
   │   COMMAND   │               │    QUERY    │
   │ (Ghi/Xóa)   │               │   (Đọc dữ   │
   └──────┬──────┘               │    liệu)    │
          │                      └──────┬──────┘
          ▼                             ▼
   ┌─────────────┐               ┌─────────────┐
   │ MediatR     │               │ AsNoTracking│
   │ Handler     │               │ hoặc Dapper │
   └─────────────┘               └─────────────┘
```
"
            });
        }
        #endregion

        #region Bài 6: Clean Architecture & DDD
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "clean-architecture-domain-driven-design-ddd-dotnet"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 6: Clean Architecture & Domain-Driven Design (DDD) trong Doanh nghiệp",
                Slug = "clean-architecture-domain-driven-design-ddd-dotnet",
                Summary = "Thiết kế hệ thống phần mềm chuẩn Enterprise với Clean Architecture: Phân tách rõ rệt Domain, Application, Infrastructure và Web API.",
                EstimatedReadingMinutes = 14,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 6,
                IsFeatured = true,
                ContentMarkdown = @"# Kiến Trúc Sạch (Clean Architecture) & Domain-Driven Design (DDD)

---

## 1. Các Tầng Trong Clean Architecture

Mọi sự phụ thuộc (Dependencies) luôn hướng vào trong tầng trung tâm:

```
[ Presentation (Web / API) ] ────────┐
                                     ▼
[ Infrastructure (EF Core, Redis) ] ───► [ Application (UseCases, CQRS) ]
                                                      │
                                                      ▼
                                           [ Domain (Entities, Value Objects) ]
```

* **Domain Core:** Chứa các Entities, Value Objects, Domain Events và Enums thuần C#. Không tham chiếu bất kỳ thư viện bên ngoài hay cơ sở dữ liệu nào.
* **Application Layer:** Chứa nghiệp vụ cụ thể (Use Cases, CQRS Handlers, DTOs, Interfaces).
* **Infrastructure Layer:** Cài đặt các Interface được khai báo ở Application (EF Core DbContext, Redis Cache, RabbitMQ Bus, Email Sender).
* **Presentation Layer:** Điểm đón nhận Request (Minimal APIs, Controllers, SignalR Hubs).
"
            });
        }
        #endregion
    }

    // ---------------------------------------------------------------------------------
    // Catalog 5 Additions: Hybrid Dapper & Advanced EF Core
    // ---------------------------------------------------------------------------------
    private static async Task SeedAdditionalEfCoreTutorialsAsync(AppDbContext context, int categoryId)
    {
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "ket-hop-efcore-va-dapper-micro-orm-high-throughput"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 5: Kết hợp EF Core & Dapper Micro-ORM - Tăng tốc Đọc Ghi Dữ liệu Hàng triệu Bản ghi",
                Slug = "ket-hop-efcore-va-dapper-micro-orm-high-throughput",
                Summary = "Mô hình lai chuẩn Senior: Dùng EF Core cho Migrations và nghiệp vụ ghi phức tạp, dùng Dapper Micro-ORM để stream và query hàng triệu record tốc độ cao.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 5,
                IsFeatured = true,
                ContentMarkdown = @"# Kiến Trúc Lai: EF Core (Write Path) & Dapper (High-Throughput Read Path)

---

## 1. Vì sao nên kết hợp EF Core & Dapper?

EF Core xuất sắc trong việc quản lý Migration, theo dõi thay đổi (Change Tracking) và thực thi logic ghi dữ liệu có tính toàn vẹn cao. Tuy nhiên, đối với các câu lệnh báo cáo phức tạp, lọc hàng chục bảng hoặc stream hàng trăm ngàn dòng về Client, Dapper mang lại tốc độ gần tiệm cận ADO.NET thuần túy.

```csharp
public sealed class OrderReportingRepository
{
    private readonly AppDbContext _context;

    public OrderReportingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderReportDto>> GetTopOrdersAsync(int limit)
    {
        // Tái sử dụng chính DbConnection từ EF Core
        var connection = _context.Database.GetDbConnection();

        const string sql = @""
            SELECT o.Id, o.TotalAmount, c.FullName, o.CreatedAt
            FROM Orders o
            INNER JOIN Customers c ON o.CustomerId = c.Id
            ORDER BY o.TotalAmount DESC
            LIMIT @Limit"";

        return await connection.QueryAsync<OrderReportDto>(sql, new { Limit = limit });
    }
}
```
"
            });
        }

        if (!await context.Tutorials.AnyAsync(t => t.Slug == "efcore-interceptors-soft-delete-audit-logging-multitenancy"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 6: Interceptors, Soft Delete, Audit Logging & Multi-Tenancy trong EF Core",
                Slug = "efcore-interceptors-soft-delete-audit-logging-multitenancy",
                Summary = "Xây dựng SaveChangesInterceptor tự động ghi nhận CreatedAt/CreatedBy, thiết lập Global Query Filters cho Soft Delete và cô lập dữ liệu Multi-Tenant an toàn.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 6,
                IsFeatured = false,
                ContentMarkdown = @"# Nâng Cao EF Core: Interceptors, Audit Logging & Multi-Tenancy

---

## 1. SaveChangesInterceptor tự động điền thông tin Audit

```csharp
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries<IAuditableEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```
"
            });
        }
    }

    // ---------------------------------------------------------------------------------
    // Catalog 7: Microservices, Distributed Systems & Event-Driven (.NET 10)
    // ---------------------------------------------------------------------------------
    private static async Task SeedMicroservicesTutorialsAsync(AppDbContext context, int categoryId)
    {
        #region Bài 1: Microservices Architecture & YARP
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "kien-truc-microservices-bounded-contexts-yarp-gateway"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 1: Kiến trúc Microservices Thực chiến - Bounded Contexts & YARP API Gateway",
                Slug = "kien-truc-microservices-bounded-contexts-yarp-gateway",
                Summary = "Chiến lược phân rã Microservices, phân định Bounded Contexts và cấu hình YARP Reverse Proxy làm cổng Gateway thống nhất cho hệ sinh thái vi dịch vụ.",
                EstimatedReadingMinutes = 13,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 1,
                IsFeatured = true,
                ContentMarkdown = @"# Kiến Trúc Microservices & YARP API Gateway trong .NET 10

---

## 1. Vì sao chuyển dịch từ Monolith sang Microservices?

Microservices giúp các đội ngũ kỹ thuật triển khai độc lập (Independent Deployment), cô lập lỗi (Fault Isolation) và mở rộng linh hoạt theo từng dịch vụ riêng biệt.

---

## 2. Cấu hình YARP (Yet Another Reverse Proxy) trong .NET 10

YARP là thư viện Reverse Proxy hiệu năng cao do Microsoft phát triển trực tiếp trên Kestrel.

```json
{
  ""ReverseProxy"": {
    ""Routes"": {
      ""catalog-route"": {
        ""ClusterId"": ""catalog-cluster"",
        ""Match"": {
          ""Path"": ""/api/v1/catalog/{**catch-all}""
        }
      }
    },
    ""Clusters"": {
      ""catalog-cluster"": {
        ""Destinations"": {
          ""catalog-srv-1"": {
            ""Address"": ""http://catalog-service:8080""
          }
        }
      }
    }
  }
}
```
"
            });
        }
        #endregion

        #region Bài 2: RabbitMQ vs Kafka
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "event-driven-architecture-rabbitmq-kafka-trieu-request"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 2: Event-Driven Architecture - RabbitMQ vs Apache Kafka cho Tải Triệu Request",
                Slug = "event-driven-architecture-rabbitmq-kafka-trieu-request",
                Summary = "So sánh chuyên sâu cơ chế nội tại của RabbitMQ (AMQP broker) và Apache Kafka (Append-only commit log), thiết lập kiến trúc Hybrid nuốt tải hàng triệu sự kiện/giây.",
                EstimatedReadingMinutes = 14,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 2,
                IsFeatured = true,
                ContentMarkdown = @"# Kiến Trúc Hướng Sự Kiện: RabbitMQ vs Apache Kafka

Khi quy mô hệ thống tăng lên hàng triệu người dùng và hàng triệu request mỗi phút, việc lựa chọn Message Broker quyết định sự sống còn của toàn bộ hạ tầng.

---

## 1. Bảng So Sánh Kỹ Thuật Chuyên Sâu

| Tiêu chí | RabbitMQ (Message Broker) | Apache Kafka (Event Streaming Platform) |
| :--- | :--- | :--- |
| **Kiến trúc cốt lõi** | Quản lý Queue trong RAM, trạng thái lưu theo từng message | Append-only Commit Log ghi tuần tự vào đĩa |
| **Throughput** | 20.000 - 50.000 msg/sec | 500.000 - 2.000.000+ msg/sec |
| **Độ trễ (Latency)** | Cực thấp (Microseconds / Sub-millisecond) | Thấp (Khoảng 2 - 10 milliseconds do gom batch) |
| **Tiêu thụ tin nhắn** | Xóa tin nhắn khỏi Queue ngay sau khi Consumer ACK | Lưu trữ theo thời gian (Retention Policy), tua lại đọc lại tùy thích |
| **Khả năng định tuyến** | Rất linh hoạt (Direct, Fanout, Topic, Headers Exchange) | Định tuyến theo Partition Key đơn giản |
| **Ứng dụng tối ưu** | Xử lý tác vụ nghiệp vụ, nộp bài, cộng điểm, cấp bằng | Telemetry, Clickstream, Logs, Keystrokes, IoT Stream |

---

## 2. Mô hình Hybrid Architecture Chuẩn Doanh Nghiệp

* **Kafka:** Đóng vai trò Ingestion Gateway - nuốt trọn toàn bộ stream dữ liệu thô (Logs, Metrics, Telemetry).
* **RabbitMQ:** Điều phối tác vụ nghiệp vụ chính xác, có Dead-Letter Queue (DLQ) và retry từng giao dịch quan trọng.
"
            });
        }
        #endregion

        #region Bài 3: MassTransit & Outbox Pattern
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "masstransit-microservices-pub-sub-dlq-outbox-pattern"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 3: MassTransit trong Microservices - Publish/Subscribe, DLQ & Outbox Pattern",
                Slug = "masstransit-microservices-pub-sub-dlq-outbox-pattern",
                Summary = "Tích hợp MassTransit, cấu hình chính sách Retry lũy tiến, xử lý Dead-Letter Queue và Transactional Outbox Pattern tránh mất mát dữ liệu khi mạng gặp sự cố.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 3,
                IsFeatured = false,
                ContentMarkdown = @"# MassTransit & Transactional Outbox Pattern

---

## 1. Vấn Đề Dual-Write trong Hệ Thống Phân Tán

Khi thực hiện thao tác lưu dữ liệu vào Database và bắn Event lên Message Broker:
* Nếu DB ghi thành công nhưng Broker sập → Mất Event!
* Nếu bắn Event thành công nhưng DB lỗi → Dữ liệu không nhất quán!

## 2. Giải Pháp: Transactional Outbox Pattern

Outbox Pattern lưu Message vào bảng `OutboxMessages` ngay trong cùng một Transaction với dữ liệu nghiệp vụ. Một Background Worker sẽ quét bảng Outbox và publish lên Broker với cam kết **At-Least-Once Delivery**.
"
            });
        }
        #endregion

        #region Bài 4: Inter-service gRPC
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "inter-service-grpc-protobuf-streaming-dotnet10"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 4: Giao tiếp Liên Dịch vụ Tốc độ cao với gRPC & Protobuf Streaming trong .NET 10",
                Slug = "inter-service-grpc-protobuf-streaming-dotnet10",
                Summary = "Định nghĩa Protocol Buffers (.proto), xây dựng gRPC Client/Server, tận dụng giao thức HTTP/2 đa kênh (Multiplexing) và Server/Client Streaming.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 4,
                IsFeatured = false,
                ContentMarkdown = @"# Giao Tiếp Hiệu Năng Cao Với gRPC trong .NET 10

---

## 1. Vì sao gRPC nhanh hơn REST JSON từ 5 đến 10 lần?

* **Giao thức nhị phân Protobuf:** Dữ liệu được nén thành chuỗi byte nhỏ gọn hơn nhiều so với chuỗi văn bản JSON.
* **HTTP/2 Multiplexing:** Hàng ngàn request có thể chạy đồng thời trên duy nhất một kết nối TCP mà không bị nghẽn Head-of-line blocking.
"
            });
        }
        #endregion

        #region Bài 5: Distributed Resilience với Polly
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "distributed-resilience-polly-circuit-breaker-backoff"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 5: Distributed Resilience & Fault Tolerance - Polly Circuit Breaker, Exponential Backoff",
                Slug = "distributed-resilience-polly-circuit-breaker-backoff",
                Summary = "Xây dựng hệ thống chống sập dây chuyền (Cascading Failure) với Polly v8: Thử lại với Jitter ngẫu nhiên, Circuit Breaker tự ngắt mạch và Timeout.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 5,
                IsFeatured = false,
                ContentMarkdown = @"# Xây Dựng Tính Kiên Cường (Resilience) Với Polly v8

---

## 1. Ba Trạng Thái của Circuit Breaker

```
   [ CLOSED ] ──(Lỗi vượt ngưỡng)──► [ OPEN ] (Ngắt mạch, chặn request ngay lập tức)
       ▲                                 │
       │                                 ▼ (Hết thời gian nghỉ ngơi)
   (Thành công) ◄── [ HALF-OPEN ] ◄──────┘
```
"
            });
        }
        #endregion

        #region Bài 6: Distributed Observability
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "distributed-observability-opentelemetry-prometheus-grafana"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 6: Quan sát Phân tán (Distributed Observability) - OpenTelemetry, Prometheus & Grafana",
                Slug = "distributed-observability-opentelemetry-prometheus-grafana",
                Summary = "Triển khai Bộ ba Trụ cột Giám sát (Metrics, Traces, Logs): Tích hợp ActivitySource chuẩn W3C TraceContext, xuất Metrics sang Prometheus và Grafana Dashboard.",
                EstimatedReadingMinutes = 13,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 6,
                IsFeatured = true,
                ContentMarkdown = @"# Quan Sát Phân Tán (Distributed Observability) với OpenTelemetry

---

## 1. Khung Tiêu Chuẩn Giám Sát Phân Tán

* **Tracing:** Theo dõi vòng đời của một Request đi xuyên qua YARP Gateway → Microservices → Database → Message Broker bằng một `TraceId` duy nhất.
* **Metrics:** Đo lường các chỉ số định lượng: Requests Per Second, Error Rate, GC Pauses, Queue Depth.
* **Logging:** Nhật ký có cấu trúc (Structured Logging) với Serilog gắn kèm TraceId.
"
            });
        }
        #endregion
    }

    // ---------------------------------------------------------------------------------
    // Catalog 8: Performance, Diagnostics & Cloud-Native DevOps
    // ---------------------------------------------------------------------------------
    private static async Task SeedPerformanceDevOpsTutorialsAsync(AppDbContext context, int categoryId)
    {
        #region Bài 1: CLR GC Internals
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "quan-ly-bo-nho-gc-clr-internals-gen0-gen1-gen2-loh"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 1: Quản lý Bộ nhớ & Kiến trúc CLR Garbage Collection - Gen 0/1/2, LOH & POH",
                Slug = "quan-ly-bo-nho-gc-clr-internals-gen0-gen1-gen2-loh",
                Summary = "Đi sâu vào kiến trúc máy ảo .NET CLR: Phân vùng thế hệ bộ nhớ (Gen 0, 1, 2), Large Object Heap (>=85KB), Pinned Object Heap và cơ chế Non-blocking Background GC.",
                EstimatedReadingMinutes = 13,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 1,
                IsFeatured = true,
                ContentMarkdown = @"# Bản Chất Bộ Nhớ CLR & Thu Gom Rác (Garbage Collection)

---

## 1. Kiến trúc Các Thế Hệ Bộ Nhớ (Generational GC)

* **Generation 0 (Gen 0):** Chứa các đối tượng sống ngắn hạn mới được cấp phát (biến cục bộ hàm, chuỗi tạm). Thu gom siêu nhanh (< 1ms).
* **Generation 1 (Gen 1):** Vùng đệm chứa các đối tượng sống sót sau lần dọn rác của Gen 0.
* **Generation 2 (Gen 2):** Chứa các đối tượng sống lâu dài (Singletons, App caches, DbContext pools). Thu gom tốn kém nhất (Full GC).
* **Large Object Heap (LOH):** Chứa các mảng/đối tượng có dung lượng ≥ 85.000 bytes. Mặc định không được dồn nén bộ nhớ (Compaction) để tránh tốn CPU.
"
            });
        }
        #endregion

        #region Bài 2: High-Performance C# (Span, Memory, ArrayPool)
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "high-performance-csharp-span-memory-ref-struct-arraypool"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 2: Kỹ thuật High-Performance C# - Span<T>, Memory<T>, ref struct & ArrayPool<T>",
                Slug = "high-performance-csharp-span-memory-ref-struct-arraypool",
                Summary = "Lập trình không cấp phát bộ nhớ (Zero-Allocation): Khai thác Span<T> và ReadOnlySpan<T> cắt chuỗi không tạo bản sao và tái sử dụng bộ đệm với ArrayPool<T>.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 2,
                IsFeatured = true,
                ContentMarkdown = @"# Lập Trình Không Cấp Phát Bộ Nhớ (Zero-Allocation) trong C#

---

## 1. So Sánh `string.Substring()` vs `ReadOnlySpan<char>`

```csharp
string text = ""2026-11-04T22:30:00"";

// Cách cũ: Gây ra cấp phát 3 chuỗi mới trên Managed Heap
string yearStr = text.Substring(0, 4);

// Cách Senior: Zero Allocation, trỏ thẳng vào bộ nhớ gốc!
ReadOnlySpan<char> span = text.AsSpan();
ReadOnlySpan<char> yearSpan = span.Slice(0, 4);
int year = int.Parse(yearSpan);
```
"
            });
        }
        #endregion

        #region Bài 3: Micro-benchmarking với BenchmarkDotNet
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "micro-benchmarking-voi-benchmarkdotnet-toi-uu-thuat-toan"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 3: Đo lường & Vi chuẩn hóa Mã nguồn (Micro-benchmarking) với BenchmarkDotNet",
                Slug = "micro-benchmarking-voi-benchmarkdotnet-toi-uu-thuat-toan",
                Summary = "Cách viết bài kiểm thử hiệu năng chuẩn khoa học: Đo thời gian chạy tính theo nano-giây, phân tích số byte bộ nhớ được cấp phát và số lần Gen 0/1/2 GC kích hoạt.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 3,
                IsFeatured = false,
                ContentMarkdown = @"# Vi Chuẩn Hóa Hiệu Năng Với BenchmarkDotNet

Khi tối ưu hóa thuật toán hoặc thư viện cốt lõi, không bao giờ dùng `Stopwatch` vì kết quả bị ảnh hưởng bởi JIT Warmup và luồng nền của hệ điều hành.

```csharp
[MemoryDiagnoser]
public class StringConcatBenchmark
{
    [Benchmark(Baseline = true)]
    public string UsePlusOperator()
    {
        string s = """";
        for (int i = 0; i < 100; i++) s += i.ToString();
        return s;
    }

    [Benchmark]
    public string UseStringBuilder()
    {
        var sb = new StringBuilder(500);
        for (int i = 0; i < 100; i++) sb.Append(i);
        return sb.ToString();
    }
}
```
"
            });
        }
        #endregion

        #region Bài 4: Production Diagnostics (dotnet-dump, trace)
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "chan-doan-production-dotnet-dump-trace-memory-leak-investigation"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 4: Chẩn đoán Sự cố Production - Điều tra Memory Leak với dotnet-dump & trace",
                Slug = "chan-doan-production-dotnet-dump-trace-memory-leak-investigation",
                Summary = "Kỹ năng sống còn của Senior Engineer: Xuất và phân tích file Memory Dump (.dmp), tìm gốc rễ rò rỉ bộ nhớ (GC Root) và phát hiện Deadlock trên môi trường Production.",
                EstimatedReadingMinutes = 13,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 4,
                IsFeatured = true,
                ContentMarkdown = @"# Chẩn Đoán Sự Cố Production Với Các Công Cụ .NET CLI

---

## 1. Bộ Công Cụ Chẩn Đoán Tiêu Chuẩn

* **`dotnet-counters`:** Giám sát thời gian thực CPU %, GC Heap size, ThreadPool count.
* **`dotnet-dump`:** Bắt toàn bộ trạng thái bộ nhớ của process mà không cần dừng ứng dụng.
* **`dotnet-trace`:** Ghi lại vết CPU call-tree để điều tra điểm nghẽn hiệu năng.

## 2. Các Lệnh SOS Thường Dùng Khi Mở File Dump

```bash
# Phân tích các kiểu đối tượng chiếm nhiều dung lượng nhất trên Heap:
!dumpheap -stat

# Tìm chuỗi giữ tham chiếu khiến đối tượng không thể bị thu gom (Nguyên nhân rò rỉ):
!gcroot <địa_chỉ_bộ_nhớ>
```
"
            });
        }
        #endregion

        #region Bài 5: Reflection & Source Generators
        // Move or ensure reflection tutorial is here
        var existingRefl = await context.Tutorials.FirstOrDefaultAsync(t => t.Slug == "reflection-attributes-source-generators-csharp");
        if (existingRefl != null)
        {
            existingRefl.CategoryId = categoryId;
            existingRefl.OrderIndex = 5;
            existingRefl.Title = "Bài 5: Reflection, Custom Attributes & C# Roslyn Source Generators";
        }

        // Clean up redundant old GC record now superseded by Bài 1 & Bài 2 in Category 8
        var oldGc = await context.Tutorials.FirstOrDefaultAsync(t => t.Slug == "quan-ly-bo-nho-gc-idisposable-span-memory");
        if (oldGc != null)
        {
            context.Tutorials.Remove(oldGc);
        }
        #endregion

        #region Bài 6: Docker & Kubernetes Helm
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "container-hoa-docker-trien-khai-kubernetes-helm-chart"))
        {
            context.Tutorials.Add(new Tutorial
            {
                CategoryId = categoryId,
                Title = "Bài 6: Container hóa Đa tầng (Multi-stage Docker) & Triển khai Kubernetes Helm Chart",
                Slug = "container-hoa-docker-trien-khai-kubernetes-helm-chart",
                Summary = "Đóng gói ứng dụng .NET 10 siêu nhỏ gọn và an toàn với Chiseled Ubuntu/Alpine, cấu hình Health Checks, Readiness Probes và triển khai lên cụm Kubernetes với Helm Chart.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 6,
                IsFeatured = true,
                ContentMarkdown = @"# Container Hóa & Triển Khai Kubernetes Helm trong .NET 10

---

## 1. Multi-stage Dockerfile Tối Ưu Cho .NET 10

```dockerfile
# Giai đoạn Build với đầy đủ SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Giai đoạn Runtime siêu nhẹ với Chiseled Ubuntu (Không có shell, an toàn bảo mật)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT [""dotnet"", ""web.dll""]
```
"
            });
        }
        #endregion
    }
}
