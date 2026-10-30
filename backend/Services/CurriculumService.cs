using NET_Tutos.Data;
using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace NET_Tutos.Services;

public class CurriculumService : ICurriculumService
{
    private readonly AppDbContext _context;
    private static readonly Lazy<List<CurriculumSectionViewModel>> _sectionsCache = new(BuildAllSections);
    private static readonly Lazy<List<CurriculumLessonViewModel>> _lessonsCache = new(() => 
        _sectionsCache.Value.SelectMany(s => s.Lessons).OrderBy(l => l.LessonNumber).ToList());

    public CurriculumService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CurriculumIndexViewModel> GetCurriculumOverviewAsync(int? sectionNumber, DifficultyLevel? difficulty, string? search)
    {
        var sections = _sectionsCache.Value;
        var allLessons = _lessonsCache.Value;

        var query = allLessons.AsEnumerable();

        if (sectionNumber.HasValue)
        {
            query = query.Where(l => l.SectionNumber == sectionNumber.Value);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(l => l.Difficulty == difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(l => 
                l.TitleVi.ToLowerInvariant().Contains(term) ||
                l.TitleEn.ToLowerInvariant().Contains(term) ||
                l.SummaryVi.ToLowerInvariant().Contains(term) ||
                l.SummaryEn.ToLowerInvariant().Contains(term) ||
                l.KeyTags.Any(t => t.ToLowerInvariant().Contains(term)) ||
                l.LessonNumber.ToString() == term);
        }

        var filteredLessons = query.ToList();

        // Check matching tutorials in DB to wire direct links
        var dbSlugs = await _context.Tutorials
            .Select(t => t.Slug)
            .ToListAsync();

        var slugSet = new HashSet<string>(dbSlugs, StringComparer.OrdinalIgnoreCase);
        foreach (var item in filteredLessons)
        {
            if (item.MatchingTutorialSlug != null && slugSet.Contains(item.MatchingTutorialSlug))
            {
                // Has direct database tutorial
            }
        }

        return new CurriculumIndexViewModel
        {
            Sections = sections,
            FilteredLessons = filteredLessons,
            SelectedSectionNumber = sectionNumber,
            SelectedDifficulty = difficulty,
            SearchQuery = search,
            TotalLessonsCount = allLessons.Count,
            TotalSectionsCount = sections.Count,
            TotalEstimatedHours = sections.Sum(s => s.TotalEstimatedHours)
        };
    }

    public Task<List<CurriculumSectionViewModel>> GetAllSectionsAsync()
    {
        return Task.FromResult(_sectionsCache.Value);
    }

    public Task<CurriculumLessonViewModel?> GetLessonByNumberAsync(int lessonNumber)
    {
        var lesson = _lessonsCache.Value.FirstOrDefault(l => l.LessonNumber == lessonNumber);
        return Task.FromResult(lesson);
    }

    public Task<CurriculumLessonViewModel?> GetLessonBySlugAsync(string slug)
    {
        var lesson = _lessonsCache.Value.FirstOrDefault(l => 
            string.Equals(l.Slug, slug, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(l.MatchingTutorialSlug, slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(lesson);
    }

    public async Task<CurriculumLessonDetailViewModel?> GetLessonDetailAsync(int lessonNumber)
    {
        var lesson = _lessonsCache.Value.FirstOrDefault(l => l.LessonNumber == lessonNumber);
        if (lesson == null) return null;

        var prev = _lessonsCache.Value.FirstOrDefault(l => l.LessonNumber == lessonNumber - 1);
        var next = _lessonsCache.Value.FirstOrDefault(l => l.LessonNumber == lessonNumber + 1);

        bool hasTutorial = false;
        if (!string.IsNullOrEmpty(lesson.MatchingTutorialSlug))
        {
            hasTutorial = await _context.Tutorials.AnyAsync(t => t.Slug == lesson.MatchingTutorialSlug);
        }

        return new CurriculumLessonDetailViewModel
        {
            Lesson = lesson,
            PreviousLesson = prev,
            NextLesson = next,
            HasExistingTutorial = hasTutorial
        };
    }

    public Task<List<CurriculumLessonViewModel>> GetAllLessonsAsync()
    {
        return Task.FromResult(_lessonsCache.Value);
    }

    #region Curriculum Builder (101 Lessons across 7 Sections)
    private static List<CurriculumSectionViewModel> BuildAllSections()
    {
        var sections = new List<CurriculumSectionViewModel>();

        // ----------------------------------------------------
        // PHẦN 1: NHẬP MÔN C# (Bài 1 -> Bài 21)
        // ----------------------------------------------------
        var s1 = new CurriculumSectionViewModel
        {
            SectionNumber = 1,
            TitleVi = "Phần 1 - Nhập môn C#",
            TitleEn = "Part 1 - Introduction to C#",
            DescriptionVi = "Xây dựng nền móng tư duy lập trình với cú pháp C#, biến, kiểu dữ liệu, cấu trúc điều kiện, vòng lặp, mảng, hàm, lớp và các khái niệm OOP sơ khởi.",
            DescriptionEn = "Build your programming foundation with C# syntax, variables, data types, control flow, loops, arrays, methods, classes, and fundamental OOP concepts.",
            BadgeColor = "success",
            Icon = "bi-mortarboard-fill",
            Level = DifficultyLevel.Beginner,
            TotalEstimatedHours = 20
        };

        s1.Lessons.AddRange(new List<CurriculumLessonViewModel>
        {
            new()
            {
                LessonNumber = 1,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Cài đặt môi trường & Viết chương trình C# đầu tiên",
                TitleEn = "Environment Setup & Your First C# Program",
                Slug = "cai-dat-chuong-trinh-csharp-dau-tien",
                MatchingTutorialSlug = "tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong",
                SummaryVi = "Hướng dẫn cài đặt .NET SDK, trình biên tập VS Code / Visual Studio, cấu trúc file dự án (.csproj) và chạy ứng dụng Console đầu tiên.",
                SummaryEn = "Guide to installing .NET SDK, VS Code / Visual Studio, understanding project files (.csproj), and executing your first Console application.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { ".NET SDK", "CLI", "dotnet new", "Hello World" },
                SampleCode = @"// Chạy bằng lệnh: dotnet run
Console.WriteLine(""Xin chào C# và .NET 10!"");
Console.WriteLine($""Thời gian hiện tại: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"");",
                LearningOutcomesVi = new() { "Nắm rõ quy trình cài đặt .NET SDK", "Biết sử dụng lệnh dotnet new, dotnet build, dotnet run", "Hiểu cấu trúc tập tin mã nguồn Program.cs" },
                LearningOutcomesEn = new() { "Understand .NET SDK setup", "Master dotnet CLI commands", "Understand Program.cs source layout" }
            },
            new()
            {
                LessonNumber = 2,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Top-level statement trong lập trình C# hiện đại (.NET 6+)",
                TitleEn = "Top-Level Statements in Modern C# (.NET 6+)",
                Slug = "top-level-statement-trong-lap-trinh-csharp",
                SummaryVi = "Tìm hiểu cú pháp Top-level statement loại bỏ boilerplate class Program và static void Main, giúp viết code ngắn gọn, súc tích.",
                SummaryEn = "Explore top-level statements that eliminate boilerplate class Program and static void Main, resulting in clean, concise code.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 15,
                KeyTags = new() { "Top-level Statements", "Clean Code", "C# 10+" },
                SampleCode = @"// Không cần namespace hay class Program nữa!
string greeting = ""Chào mừng bạn đến với C# hiện đại!"";
Console.WriteLine(greeting);",
                LearningOutcomesVi = new() { "Phân biệt cú pháp truyền thống và Top-level statements", "Hiểu cách trình biên dịch C# tự động sinh Main ngầm định" },
                LearningOutcomesEn = new() { "Distinguish between classic Main and Top-level statements", "Learn how C# compiler generates entry points under the hood" }
            },
            new()
            {
                LessonNumber = 3,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Biến, Hằng số, Kiểu dữ liệu và Nhập/Xuất Console",
                TitleEn = "Variables, Constants, Data Types, and Console I/O",
                Slug = "bien-hang-so-kieu-du-lieu-va-nhap-xuat",
                MatchingTutorialSlug = "bien-kieu-du-lieu-toan-tu-trong-csharp",
                SummaryVi = "Tìm hiểu hệ thống kiểu dữ liệu nguyên thủy (int, double, decimal, bool, char), khai báo biến với var, hằng số const và nhập xuất qua Console.ReadLine().",
                SummaryEn = "Explore primitive data types (int, double, decimal, bool, char), implicit typing with var, const constants, and console input via Console.ReadLine().",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "Variables", "Data Types", "Type Inference", "Console I/O" },
                SampleCode = @"Console.Write(""Nhập tên của bạn: "");
string? name = Console.ReadLine();
const double PI = 3.14159;
int age = 22;
Console.WriteLine($""Xin chào {name}, tuổi của bạn là {age}, số PI = {PI}"");",
                LearningOutcomesVi = new() { "Làm chủ các kiểu dữ liệu số nguyên, số thực, chuỗi", "Biết cách ép kiểu an toàn với int.TryParse()", "Sử dụng biến ngầm định var chính xác" },
                LearningOutcomesEn = new() { "Master primitive numeric, text, and boolean types", "Learn safe parsing with int.TryParse()", "Appropriately apply var" }
            },
            new()
            {
                LessonNumber = 4,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Toán tử số học, Toán tử gán và Phép tăng giảm",
                TitleEn = "Arithmetic, Assignment, and Increment/Decrement Operators",
                Slug = "cac-toan-tu-so-hoc-va-gan",
                SummaryVi = "Khảo sát các toán tử toán học (+, -, *, /, %), toán tử chia lấy dư, toán tử gán mở rộng (+=, -=) và sự khác biệt giữa tiền tố ++i vs hậu tố i++.",
                SummaryEn = "Examine arithmetic operators (+, -, *, /, %), modulus, compound assignments (+=, -=), and prefix ++i vs postfix i++ behavior.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 15,
                KeyTags = new() { "Operators", "Arithmetic", "Pre-increment", "Post-increment" },
                SampleCode = @"int a = 10, b = 3;
Console.WriteLine($""Phép chia nguyên: {a / b}"");      // 3
Console.WriteLine($""Chia lấy dư: {a % b}"");            // 1
Console.WriteLine($""Chia số thực: {(double)a / b:F2}""); // 3.33",
                LearningOutcomesVi = new() { "Tránh lỗi chia nguyên dẫn đến mất phần thập phân", "Hiểu thứ tự ưu tiên của các toán tử số học" },
                LearningOutcomesEn = new() { "Prevent integer division truncation bugs", "Understand operator precedence" }
            },
            new()
            {
                LessonNumber = 5,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Toán tử so sánh, Logic và Cấu trúc rẽ nhánh if - else, switch",
                TitleEn = "Comparison, Logical Operators, and If - Else / Switch Statements",
                Slug = "toan-tu-so-sanh-logic-va-if-switch",
                MatchingTutorialSlug = "cau-truc-dieu-khien-re-nhanh-vong-lap",
                SummaryVi = "Làm chủ cấu trúc rẽ nhánh với if-else if-else, toán tử ba ngôi (?:), lệnh switch-case truyền thống và Switch Expression hiện đại trong C#.",
                SummaryEn = "Master control flow branching using if-else if-else, ternary operator (?:), classic switch statements, and modern C# pattern switch expressions.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "if-else", "switch expression", "Pattern Matching", "Logic" },
                SampleCode = @"int score = 85;
string rank = score switch
{
    >= 90 => ""Xuất sắc (A)"",
    >= 80 => ""Giỏi (B)"",
    >= 65 => ""Khá (C)"",
    _ => ""Trung bình / Yếu""
};
Console.WriteLine($""Xếp loại: {rank}"");",
                LearningOutcomesVi = new() { "Viết biểu thức điều kiện tối ưu và dễ đọc", "Ứng dụng Switch Expression và Pattern Matching trong C#" },
                LearningOutcomesEn = new() { "Write clean conditional logic", "Utilize switch expressions and pattern matching" }
            },
            new()
            {
                LessonNumber = 6,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Vòng lặp trong C#: for, while, do-while, break và continue",
                TitleEn = "Loops in C#: for, while, do-while, break, and continue",
                Slug = "vong-lap-for-while-do-while",
                SummaryVi = "Nắm vững nguyên lý hoạt động của các vòng lặp lặp đi lặp lại khối lệnh, kiểm soát luồng với từ khóa break (ngắt) và continue (bỏ qua bước hiện tại).",
                SummaryEn = "Master loop constructs, loop control statements (break to exit, continue to skip current iteration), and avoiding infinite loops.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "for loop", "while loop", "break", "continue" },
                SampleCode = @"for (int i = 1; i <= 10; i++)
{
    if (i % 2 == 0) continue; // Bỏ qua số chẵn
    if (i > 7) break;         // Ngắt khi vượt quá 7
    Console.WriteLine($""Số lẻ: {i}"");
}",
                LearningOutcomesVi = new() { "Chọn đúng loại vòng lặp theo từng bài toán", "Kiểm soát vòng lặp không bị rơi vào treo vô tận" },
                LearningOutcomesEn = new() { "Select proper loop type per use case", "Safeguard against infinite loops" }
            },
            new()
            {
                LessonNumber = 7,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Cấu trúc Mảng trong C# (Array 1D, Đa chiều, Jagged Array)",
                TitleEn = "Arrays in C# (1D, Multidimensional, Jagged Arrays)",
                Slug = "mang-trong-lap-trinh-csharp",
                SummaryVi = "Tìm hiểu mảng 1 chiều, mảng đa chiều hình chữ nhật int[,] và mảng zic-zac jagged int[][], các phương thức tiện ích Array.Sort(), Array.Find().",
                SummaryEn = "Understand single-dimensional arrays, rectangular multi-dimensional arrays int[,], jagged arrays int[][], and utility methods like Array.Sort().",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "Array", "Jagged Array", "Array.Sort", "Memory Index" },
                SampleCode = @"int[] numbers = [12, 5, 27, 8, 90];
Array.Sort(numbers);
foreach (var n in numbers)
{
    Console.Write($""{n} "");
}",
                LearningOutcomesVi = new() { "Hiểu cách lưu trữ mảng liên tục trên Heap", "Làm chủ cú pháp collection expression mới [..] trong C# 12+" },
                LearningOutcomesEn = new() { "Understand contiguous heap memory layout", "Adopt C# 12+ collection expressions" }
            },
            new()
            {
                LessonNumber = 8,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Phương thức (Method), Tham số mặc định và Nạp chồng (Overloading)",
                TitleEn = "Methods, Default Parameters, and Method Overloading",
                Slug = "phuong-thuc-method-nap-chong",
                MatchingTutorialSlug = "ham-va-phuong-thuc-trong-csharp",
                SummaryVi = "Khai báo phương thức, kiểu trả về, tham số truyền vào, tham số tùy chọn (optional parameters), nạp chồng hàm và biểu thức thân hàm (expression-bodied members).",
                SummaryEn = "Declaring methods, return types, parameters, optional default values, method overloading, and expression-bodied method syntax.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Method", "Overloading", "Optional Params", "DRY" },
                SampleCode = @"int Sum(int a, int b) => a + b;
int Sum(int a, int b, int c) => a + b + c;

Console.WriteLine(Sum(10, 20));     // 30
Console.WriteLine(Sum(10, 20, 30)); // 60",
                LearningOutcomesVi = new() { "Tổ chức code theo nguyên tắc DRY (Don't Repeat Yourself)", "Khai báo phương thức nạp chồng rõ ràng" },
                LearningOutcomesEn = new() { "Structure clean, reusable modular code", "Properly design overloaded methods" }
            },
            new()
            {
                LessonNumber = 9,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Bài tập ứng dụng: Chuyển đổi số thành chữ tiếng Việt",
                TitleEn = "Practical Workshop: Converting Numbers to Words",
                Slug = "bai-tap-chuyen-so-thanh-chu",
                SummaryVi = "Xây dựng thuật toán phân tích hàng trăm, hàng chục, hàng đơn vị và nghìn, triệu, tỷ để chuyển đổi số tiền hoặc số nguyên thành văn bản chữ chuẩn tiếng Việt.",
                SummaryEn = "Build an algorithmic parser converting integers and currency figures into natural Vietnamese text (hundreds, thousands, millions, billions).",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "Algorithm", "String Builder", "Practical Project" },
                SampleCode = @"// Chuyển đổi số 1250000 -> Một triệu hai trăm năm mươi nghìn đồng
Console.WriteLine(""Ví dụ giải thuật phân tách từng cụm 3 chữ số..."");",
                LearningOutcomesVi = new() { "Rèn luyện tư duy phân tách thuật toán", "Kết hợp mảng, chuỗi và cấu trúc rẽ nhánh để giải quyết bài toán nghiệp vụ" },
                LearningOutcomesEn = new() { "Exercise procedural problem-solving skills", "Combine arrays, strings, and conditionals in business logic" }
            },
            new()
            {
                LessonNumber = 10,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Lớp (Class), Thuộc tính (Property) và Đối tượng (Object)",
                TitleEn = "Classes, Properties, and Objects",
                Slug = "lop-class-thuoc-tinh-property-doi-tuong",
                MatchingTutorialSlug = "lap-trinh-huong-doi-tuong-oop-co-ban",
                SummaryVi = "Làm quen với khái niệm Class (bản thiết kế) và Object (thực thể), trường dữ liệu (field), thuộc tính tự động (auto-property) và tính đóng gói (encapsulation).",
                SummaryEn = "Introduction to Classes and Objects, field encapsulation, auto-properties, access modifiers (public, private, protected), and information hiding.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "Class", "Object", "Property", "Encapsulation" },
                SampleCode = @"public class Student
{
    public string Name { get; set; } = string.Empty;
    public double Gpa { get; set; }
    public bool IsPass => Gpa >= 5.0;
}

var s = new Student { Name = ""Nam"", Gpa = 8.5 };
Console.WriteLine($""{s.Name} qua môn: {s.IsPass}"");",
                LearningOutcomesVi = new() { "Hiểu rõ sự khác biệt giữa Class và Object", "Biết tạo auto-property và computed property" },
                LearningOutcomesEn = new() { "Differentiate blueprint class vs instance object", "Declare auto and computed properties" }
            },
            new()
            {
                LessonNumber = 11,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Phương thức khởi tạo (Constructor), Static Constructor và từ khóa this",
                TitleEn = "Constructors, Static Constructors, and the 'this' Keyword",
                Slug = "phuong-thuc-khoi-tao-constructor",
                SummaryVi = "Tìm hiểu hàm khởi tạo mặc định, hàm khởi tạo có tham số, constructor chaining với từ khóa this(), và hàm khởi tạo tĩnh (static constructor) khởi tạo tài nguyên chung.",
                SummaryEn = "Examine default & parameterized constructors, constructor chaining via this(), and static constructors initializing type-level data.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Constructor", "this()", "Static Constructor" },
                SampleCode = @"public class Product
{
    public string Code { get; }
    public decimal Price { get; }

    public Product(string code, decimal price)
    {
        Code = code;
        Price = price;
    }
}",
                LearningOutcomesVi = new() { "Khởi tạo dữ liệu hợp lệ ngay khi đối tượng được sinh ra", "Tận dụng Constructor Chaining để tái sử dụng mã khởi tạo" },
                LearningOutcomesEn = new() { "Ensure invariant initialization", "Leverage constructor chaining" }
            },
            new()
            {
                LessonNumber = 12,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Kiểu giá trị (Value Type), Kiểu tham chiếu (Reference Type) và tham số ref, out",
                TitleEn = "Value Types, Reference Types, and ref/out Parameters",
                Slug = "kieu-gia-tri-va-kieu-tham-chieu-ref-out",
                SummaryVi = "Phân biệt sâu sắc cơ chế cấp phát bộ nhớ Stack (Value Type) vs Heap (Reference Type), truyền tham số theo giá trị vs tham chiếu với ref, out và in.",
                SummaryEn = "Deep dive into Stack vs Heap memory allocation, Value Types vs Reference Types, passing arguments by reference using ref, out, and in.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "Stack vs Heap", "Value Type", "Reference Type", "ref", "out" },
                SampleCode = @"static void Swap(ref int x, ref int y)
{
    (x, y) = (y, x); // C# Tuple swap
}

int a = 1, b = 2;
Swap(ref a, ref b);
Console.WriteLine($""a = {a}, b = {b}""); // a = 2, b = 1",
                LearningOutcomesVi = new() { "Tránh các lỗi sai bộ nhớ phổ biến", "Biết khi nào nên dùng ref/out để tối ưu và trả về nhiều kết quả" },
                LearningOutcomesEn = new() { "Understand value vs reference mutation", "Correctly apply ref, out, and in keywords" }
            },
            new()
            {
                LessonNumber = 13,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Chuỗi ký tự trong C# (String, StringBuilder, Nội suy chuỗi)",
                TitleEn = "String Manipulation in C# (String, StringBuilder, Interpolation)",
                Slug = "chuoi-ky-tu-string-va-stringbuilder",
                SummaryVi = "Tính chất bất biến (immutability) của string, các phương thức xử lý chuỗi phổ biến (SubString, Split, Replace), tối ưu phép nối chuỗi vòng lặp với StringBuilder.",
                SummaryEn = "String immutability, standard string APIs (SubString, Split, Replace), and memory-efficient concatenation using StringBuilder.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "String", "StringBuilder", "Immutability", "Interpolation" },
                SampleCode = @"var sb = new System.Text.StringBuilder();
for (int i = 1; i <= 5; i++)
{
    sb.AppendLine($""Dòng thứ {i}"");
}
Console.WriteLine(sb.ToString());",
                LearningOutcomesVi = new() { "Hiểu tại sao string là immutable và hậu quả của phép cộng chuỗi + trong vòng lặp lớn", "Sử dụng StringBuilder đúng thời điểm" },
                LearningOutcomesEn = new() { "Understand string immutability and GC allocation overhead", "Effectively use StringBuilder" }
            },
            new()
            {
                LessonNumber = 14,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Cấu trúc Struct và Kiểu liệt kê Enum",
                TitleEn = "Structs and Enumerations (Enum)",
                Slug = "cau-truc-struct-va-kieu-liet-ke-enum",
                SummaryVi = "Định nghĩa struct cho các kiểu dữ liệu nhỏ lưu trên Stack, sự khác biệt giữa struct và class, định nghĩa enum tăng tính tường minh cho trạng thái.",
                SummaryEn = "Defining lightweight stack-allocated structs, comparing class vs struct, and leveraging enums for self-documenting domain states.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "struct", "enum", "Value Type", "Readonly Struct" },
                SampleCode = @"public enum OrderStatus { Pending, Processing, Shipped, Delivered }

public readonly struct Point(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;
}",
                LearningOutcomesVi = new() { "Biết khi nào thiết kế struct thay vì class", "Sử dụng enum làm mã trạng thái an toàn thay cho magic numbers" },
                LearningOutcomesEn = new() { "Determine struct vs class design trade-offs", "Replace magic numbers with enums" }
            },
            new()
            {
                LessonNumber = 15,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Tính kế thừa trong C# (Inheritance, từ khóa base, sealed)",
                TitleEn = "Inheritance in C# (Base Class, 'base' Keyword, sealed)",
                Slug = "tinh-ke-thua-trong-lap-trinh-csharp",
                SummaryVi = "Mô hình hóa quan hệ 'is-a' với kế thừa đơn, gọi hàm khởi tạo lớp cha với base(), ngăn chặn mở rộng lớp với từ khóa sealed.",
                SummaryEn = "Model 'is-a' relationships via single inheritance, invoke base constructors using base(), and prohibit inheritance with sealed.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Inheritance", "base()", "sealed", "OOP" },
                SampleCode = @"public class Animal
{
    public string Name { get; set; } = string.Empty;
    public void Eat() => Console.WriteLine($""{Name} đang ăn..."");
}

public class Dog : Animal
{
    public void Bark() => Console.WriteLine($""{Name} gâu gâu!"");
}",
                LearningOutcomesVi = new() { "Tái sử dụng thuộc tính và phương thức qua kế thừa", "Hiểu cách phân cấp đối tượng trong C#" },
                LearningOutcomesEn = new() { "Reuse behavior through class inheritance", "Understand hierarchy design" }
            },
            new()
            {
                LessonNumber = 16,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Không gian tên (Namespace) và Quản lý phụ thuộc trong C#",
                TitleEn = "Namespaces and Dependency Organization in C#",
                Slug = "namespace-trong-csharp-net-core",
                SummaryVi = "Nguyên lý tổ chức mã nguồn theo module bằng namespace, cú pháp File-scoped namespace trong C# 10+, từ khóa using, alias và global using.",
                SummaryEn = "Modular code organization via namespaces, modern file-scoped namespaces, using aliases, and global using directives.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 15,
                KeyTags = new() { "namespace", "global using", "File-scoped", "Clean Architecture" },
                SampleCode = @"namespace MyCompany.Core.Services;

public class PaymentService
{
    // File-scoped namespace giúp code không bị lùi 1 tab thụt dòng!
}",
                LearningOutcomesVi = new() { "Tổ chức cấu trúc thư mục và namespace khoa học", "Sử dụng Global Using để giảm bớt using lặp lại ở từng file" },
                LearningOutcomesEn = new() { "Structure folders and namespaces cleanly", "Leverage global using declarations" }
            },
            new()
            {
                LessonNumber = 17,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Partial Type (Lớp phân chia) và Nested Type (Lớp lồng nhau)",
                TitleEn = "Partial Types and Nested Types in C#",
                Slug = "partial-type-va-nested-type",
                SummaryVi = "Chia nhỏ định nghĩa class/struct/interface thành nhiều tệp mã nguồn với từ khóa partial, và khai báo lớp nội bộ ẩn bên trong với nested class.",
                SummaryEn = "Split classes/structs/interfaces across multiple physical files using partial, and encapsulate private helper classes using nested types.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 15,
                KeyTags = new() { "partial class", "nested class", "Source Generators" },
                SampleCode = @"// File1: User.Core.cs
public partial class User { public int Id { get; set; } }

// File2: User.Auth.cs
public partial class User { public bool Authenticate() => true; }",
                LearningOutcomesVi = new() { "Tách các phần code do công cụ tự sinh ra và code tùy biến", "Hiểu cơ chế hoạt động của C# Source Generators" },
                LearningOutcomesEn = new() { "Separate auto-generated code from custom logic", "Understand Source Generator foundations" }
            },
            new()
            {
                LessonNumber = 18,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Lập trình Generic: Lớp Generic, Phương thức Generic và Ràng buộc",
                TitleEn = "Generics: Generic Classes, Methods, and Type Constraints",
                Slug = "su-dung-generic-lop-va-phuong-thuc",
                SummaryVi = "Khái niệm tham số kiểu T, loại bỏ hoàn toàn boxing/unboxing, thiết kế Repository pattern generic và áp dụng generic constraint (where T : class, new()).",
                SummaryEn = "Generic type parameters T, eliminating boxing overhead, designing generic repositories, and applying type constraints (where T : class, new()).",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Generics", "Type Safety", "Generic Constraints", "where T" },
                SampleCode = @"public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}",
                LearningOutcomesVi = new() { "Viết mã nguồn tái sử dụng cao mà vẫn bảo toàn kiểm tra kiểu tĩnh (Type Safety)", "Đặt ràng buộc where T chính xác" },
                LearningOutcomesEn = new() { "Write reusable type-safe logic", "Apply generic type constraints accurately" }
            },
            new()
            {
                LessonNumber = 19,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Kiểu vô danh (Anonymous Types) và Kiểu động (dynamic)",
                TitleEn = "Anonymous Types and the 'dynamic' Keyword",
                Slug = "kieu-vo-danh-va-kieu-dong-dynamic",
                SummaryVi = "Tạo đối tượng tạm thời nhanh chóng không cần khai báo class (thường dùng trong phép chiếu LINQ Select), và xử lý runtime binding với dynamic / DLR.",
                SummaryEn = "Fast temporary object modeling with anonymous types (LINQ projections), and late runtime binding using dynamic and the DLR.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "Anonymous Type", "dynamic", "DLR", "LINQ Projection" },
                SampleCode = @"var person = new { Name = ""Alice"", Age = 28 };
Console.WriteLine($""Tên: {person.Name}, Tuổi: {person.Age}"");",
                LearningOutcomesVi = new() { "Sử dụng Anonymous Types để chiếu dữ liệu DTO tạm thời", "Nhận thức nguy cơ runtime exception khi lạm dụng dynamic" },
                LearningOutcomesEn = new() { "Project lightweight DTOs with anonymous types", "Understand runtime risks of the dynamic keyword" }
            },
            new()
            {
                LessonNumber = 20,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Null, Kiểu Nullable và Null-safety trong C# hiện đại",
                TitleEn = "Null, Nullable Types, and Null Safety in Modern C#",
                Slug = "null-nullable-va-null-safety",
                SummaryVi = "Tìm hiểu Nullable<T> (int?), bật tính năng Nullable Reference Types, toán tử null-coalescing (??), null-conditional (?.) và toán tử gán null (??=).",
                SummaryEn = "Explore Nullable<T> (int?), Nullable Reference Types (#nullable enable), null-coalescing (??), null-conditional (?.), and null-assign (??=).",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "Nullable", "Null-Safety", "??", "?.", "NRT" },
                SampleCode = @"string? title = null;
string display = title ?? ""Chưa có tiêu đề"";
int length = title?.Length ?? 0;
Console.WriteLine($""Hiển thị: {display}, độ dài: {length}"");",
                LearningOutcomesVi = new() { "Loại bỏ hoàn toàn nỗi ám ảnh NullReferenceException", "Tự tin sử dụng toán tử ?., ??, ??= trong dự án" },
                LearningOutcomesEn = new() { "Eradicate NullReferenceException occurrences", "Confidently use null-safety operators" }
            },
            new()
            {
                LessonNumber = 21,
                SectionNumber = 1,
                SectionTitleVi = s1.TitleVi,
                SectionTitleEn = s1.TitleEn,
                SectionBadgeColor = s1.BadgeColor,
                SectionIcon = s1.Icon,
                TitleVi = "Tính đa hình: Phương thức ảo (virtual/override), Lớp trừu tượng (abstract) & Interface",
                TitleEn = "Polymorphism: Virtual/Override, Abstract Classes, and Interfaces",
                Slug = "tinh-da-hinh-abstract-interface",
                MatchingTutorialSlug = "da-hinh-abstract-va-interface-trong-csharp",
                SummaryVi = "Làm chủ trụ cột Đa hình (Polymorphism) qua Late Binding, phương thức virtual, ghi đè override, lớp thuần lý thuyết abstract class và hợp đồng Interface.",
                SummaryEn = "Master Polymorphism via late binding, virtual methods, override mechanics, abstract base classes, and Interface contracts.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "Polymorphism", "virtual/override", "abstract", "interface" },
                SampleCode = @"public interface IShape { double CalculateArea(); }

public class Circle(double radius) : IShape
{
    public double CalculateArea() => Math.PI * radius * radius;
}",
                LearningOutcomesVi = new() { "Phân biệt rạch ròi Abstract Class vs Interface", "Lập trình dựa trên Interface Contract chuẩn thiết kế hướng đối tượng" },
                LearningOutcomesEn = new() { "Clearly distinguish abstract class vs interface", "Program against abstractions and contracts" }
            }
        });
        sections.Add(s1);

        // ----------------------------------------------------
        // PHẦN 2: C# NÂNG CAO (Bài 22 -> Bài 43)
        // ----------------------------------------------------
        var s2 = new CurriculumSectionViewModel
        {
            SectionNumber = 2,
            TitleVi = "Phần 2 - C# Nâng cao",
            TitleEn = "Part 2 - Advanced C#",
            DescriptionVi = "Nâng tầm kỹ năng với Delegate, Lambda, Event, LINQ, Collections, IDisposable, Exception, Reflection, Async/Await đa luồng và Dependency Injection.",
            DescriptionEn = "Advance your skills with Delegates, Lambdas, Events, LINQ, Collections, IDisposable, Exception Handling, Reflection, Async/Await concurrency, and Dependency Injection.",
            BadgeColor = "info",
            Icon = "bi-layers-half",
            Level = DifficultyLevel.Intermediate,
            TotalEstimatedHours = 25
        };

        s2.Lessons.AddRange(new List<CurriculumLessonViewModel>
        {
            new()
            {
                LessonNumber = 22,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Phương thức - Delegate (Hàm ủy quyền, Action, Func, Predicate)",
                TitleEn = "Delegates: Custom Delegates, Action, Func, and Predicate",
                Slug = "su-dung-delegate-ham-uy-quyen",
                MatchingTutorialSlug = "delegate-event-va-lambda-trong-csharp",
                SummaryVi = "Khái niệm con trỏ hàm kiểu an toàn trong C#, khai báo delegate tự định nghĩa, và bộ ba delegate dựng sẵn kinh điển: Action<T>, Func<T, TResult>, Predicate<T>.",
                SummaryEn = "Type-safe function pointers, declaring custom delegates, and the essential built-in generic delegates: Action, Func, and Predicate.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Delegate", "Action", "Func", "Predicate" },
                SampleCode = @"Func<int, int, int> add = (x, y) => x + y;
Action<string> print = msg => Console.WriteLine(msg);

print($""Kết quả: {add(5, 7)}"");",
                LearningOutcomesVi = new() { "Hiểu cách truyền phương thức như một tham số", "Sử dụng thành thạo Func và Action trong code hiện đại" },
                LearningOutcomesEn = new() { "Pass methods as first-class parameters", "Proficiently apply Func and Action" }
            },
            new()
            {
                LessonNumber = 23,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Biểu thức Lambda và Phương thức vô danh (Anonymous Methods)",
                TitleEn = "Lambda Expressions and Anonymous Methods",
                Slug = "bieu-thuc-lambda-trong-csharp",
                SummaryVi = "Cú pháp toán tử mũi tên =>, lambda dạng biểu thức (expression lambda) vs lambda dạng khối lệnh (statement lambda), biến bị đóng (closures) và biến cục bộ.",
                SummaryEn = "Lambda syntax =>, expression lambdas vs statement lambdas, closures over outer variables, and variable capturing mechanics.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "Lambda", "=>", "Closure", "Functional Programming" },
                SampleCode = @"List<int> numbers = [1, 2, 3, 4, 5, 6];
var evens = numbers.Where(n => n % 2 == 0);
Console.WriteLine(string.Join("", "", evens)); // 2, 4, 6",
                LearningOutcomesVi = new() { "Viết mã nguồn ngắn gọn, mang phong cách lập trình hàm", "Hiểu cơ chế Closure và tránh bẫy rò rỉ bộ nhớ" },
                LearningOutcomesEn = new() { "Write clean functional-style C#", "Understand closure capturing and avoid memory leaks" }
            },
            new()
            {
                LessonNumber = 24,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Sự kiện (Event) và Mô hình Publisher - Subscriber trong .NET",
                TitleEn = "Events and Publisher-Subscriber Pattern in .NET",
                Slug = "event-trong-csharp-net",
                SummaryVi = "Từ khóa event bảo vệ delegate, quy ước EventHandler<TEventArgs> chuẩn của Microsoft, cơ chế đăng ký (+=) và hủy đăng ký (-=) sự kiện.",
                SummaryEn = "Event keyword encapsulation, standard EventHandler<TEventArgs> conventions, event subscription (+=) and unsubscription (-=).",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Event", "Publisher-Subscriber", "EventHandler", "EventArgs" },
                SampleCode = @"public class Clock
{
    public event EventHandler? SecondTicked;
    public void Tick() => SecondTicked?.Invoke(this, EventArgs.Empty);
}",
                LearningOutcomesVi = new() { "Xây dựng hệ thống giảm phụ thuộc (Loose Coupling)", "Biết cách giải phóng event handler để tránh rò rỉ bộ nhớ" },
                LearningOutcomesEn = new() { "Design loosely coupled publisher-subscriber architectures", "Unsubscribe events to prevent leaks" }
            },
            new()
            {
                LessonNumber = 25,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Phương thức mở rộng (Extension Methods)",
                TitleEn = "Extension Methods in C#",
                Slug = "cac-phuong-thuc-mo-rong-extension-methods",
                SummaryVi = "Bổ sung phương thức mới cho các lớp đã có (kể cả string, int, IEnumerable) mà không cần can thiệp mã nguồn hay kế thừa, bằng từ khóa this ở tham số đầu.",
                SummaryEn = "Extending existing classes (including system sealed types) without source code modification using static classes and 'this' first parameters.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 15,
                KeyTags = new() { "Extension Method", "this parameter", "Fluent API" },
                SampleCode = @"public static class StringExtensions
{
    public static bool IsValidEmail(this string input) => input.Contains('@');
}

bool valid = ""dev@domain.com"".IsValidEmail();",
                LearningOutcomesVi = new() { "Viết tiện ích mở rộng chuẩn ngữ nghĩa C#", "Hiểu cách LINQ được xây dựng từ tập hợp các Extension Methods" },
                LearningOutcomesEn = new() { "Create ergonomic domain extensions", "Understand how LINQ is built entirely on extension methods" }
            },
            new()
            {
                LessonNumber = 26,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Hàm hủy, Quá tải toán tử (Operator Overload), Indexer và Thành viên tĩnh",
                TitleEn = "Finalizers, Operator Overloading, Indexers, and Static Members",
                Slug = "ham-huy-qua-tai-toan-tu-indexer-static",
                SummaryVi = "Cơ chế Finalizer (~Class), quá tải toán tử số học/so sánh cho kiểu dữ liệu tự tạo, cú pháp indexer this[int index] và biến/phương thức static.",
                SummaryEn = "Finalizer teardown, overloading binary & comparison operators, custom collection indexers this[int index], and static class members.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Operator Overloading", "Indexer", "Finalizer", "Static" },
                SampleCode = @"public class Box
{
    private readonly string[] _items = new string[10];
    public string this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }
}",
                LearningOutcomesVi = new() { "Tạo kiểu dữ liệu tùy biến hoạt động tự nhiên như kiểu nguyên thủy", "Tạo indexer truy cập mảng thanh thoát" },
                LearningOutcomesEn = new() { "Create types with natural operator syntax", "Implement intuitive indexer APIs" }
            },
            new()
            {
                LessonNumber = 27,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Xử lý ngoại lệ: try, catch, finally, when và Custom Exception",
                TitleEn = "Exception Handling: try, catch, finally, filters, and Custom Exceptions",
                Slug = "xu-ly-ngoai-le-exception-try-catch",
                SummaryVi = "Bắt lỗi runtime an toàn với khối try-catch, luôn dọn dẹp tài nguyên với finally, lọc ngoại lệ với từ khóa when, và tự định nghĩa Exception chuyên biệt theo domain.",
                SummaryEn = "Runtime error resilience with try-catch, guaranteed resource cleanup in finally, exception filtering with 'when', and custom domain exception modeling.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Exception", "try-catch-finally", "when filter", "Custom Exception" },
                SampleCode = @"try
{
    int val = int.Parse(""abc"");
}
catch (FormatException ex) when (ex.Message != null)
{
    Console.WriteLine($""Lỗi định dạng: {ex.Message}"");
}
finally
{
    Console.WriteLine(""Khối finally luôn luôn được thực thi!"");
}",
                LearningOutcomesVi = new() { "Không bắt Exception chung chung (catch Exception)", "Xử lý lỗi graceful và log đầy đủ StackTrace" },
                LearningOutcomesEn = new() { "Avoid swallowed catch-all exceptions", "Perform graceful degradation and robust logging" }
            },
            new()
            {
                LessonNumber = 28,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Quản lý giải phóng tài nguyên: Giao diện IDisposable và câu lệnh using",
                TitleEn = "Resource Management: IDisposable Pattern and the 'using' Statement",
                Slug = "giao-dien-idisposable-va-using",
                SummaryVi = "Tài nguyên không được GC quản lý (Unmanaged Resources: file handle, socket, database connection), triển khai phương thức Dispose() và cú pháp using declaration trong C# 8+.",
                SummaryEn = "Managing unmanaged OS resources (file handles, network sockets, DB connections), implementing IDisposable, and concise 'using var' declarations.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "IDisposable", "Dispose()", "using declaration", "Unmanaged Resources" },
                SampleCode = @"using var reader = new System.IO.StreamReader(""data.txt"");
string? line = reader.ReadLine();
// reader tự động gọi Dispose() khi ra khỏi scope!",
                LearningOutcomesVi = new() { "Loại bỏ hoàn toàn rò rỉ kết nối CSDL và file handle", "Hiểu bản chất của using dưới dạng try-finally ẩn" },
                LearningOutcomesEn = new() { "Prevent file handle and connection pool leaks", "Understand using as syntactic sugar over try-finally" }
            },
            new()
            {
                LessonNumber = 29,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Làm việc với File cơ bản: File, Directory, Path và đọc ghi văn bản",
                TitleEn = "File I/O Basics: File, Directory, Path, and Text Read/Write",
                Slug = "lam-viec-voi-file-co-ban",
                SummaryVi = "Thao tác hệ thống tệp tin: tạo thư mục, kiểm tra tồn tại với File.Exists(), kết hợp đường dẫn an toàn với Path.Combine(), đọc và ghi text file bất đồng bộ.",
                SummaryEn = "File system operations: Directory management, File.Exists(), safe cross-platform path resolution via Path.Combine(), and async text I/O.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "File", "Directory", "Path.Combine", "I/O" },
                SampleCode = @"string path = Path.Combine(Directory.GetCurrentDirectory(), ""notes.txt"");
await File.WriteAllTextAsync(path, ""Lập trình .NET 10 thật tuyệt!"");
string content = await File.ReadAllTextAsync(path);
Console.WriteLine(content);",
                LearningOutcomesVi = new() { "Xử lý đường dẫn tương thích đa nền tảng Windows/Linux/macOS", "Đọc ghi tệp tin bất đồng bộ chuẩn hiệu năng" },
                LearningOutcomesEn = new() { "Resolve cross-platform paths reliably", "Read and write text files asynchronously" }
            },
            new()
            {
                LessonNumber = 30,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Luồng dữ liệu Stream trong C#: FileStream, MemoryStream và BufferedStream",
                TitleEn = "Data Streams in C#: FileStream, MemoryStream, and BufferedStream",
                Slug = "stream-filestream-va-memorystream",
                SummaryVi = "Xử lý tệp tin dung lượng lớn từng byte/chunk mà không làm tràn bộ nhớ RAM, cơ chế buffering, đọc ghi nhị phân với BinaryReader/BinaryWriter.",
                SummaryEn = "Streaming large datasets chunk-by-chunk without RAM exhaustion, stream buffering, and binary serialization using BinaryReader/BinaryWriter.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Stream", "FileStream", "MemoryStream", "Buffer" },
                SampleCode = @"using var fs = new FileStream(""sample.bin"", FileMode.Create, FileAccess.Write);
byte[] data = [1, 2, 3, 4, 5];
await fs.WriteAsync(data);",
                LearningOutcomesVi = new() { "Đọc và xử lý tệp dung lượng gigabyte mà không bị OutOfMemoryException", "Hiểu cấu trúc luồng tuần hoàn của Stream" },
                LearningOutcomesEn = new() { "Handle gigabyte-sized files without OOM exceptions", "Understand stream cursor mechanics" }
            },
            new()
            {
                LessonNumber = 31,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Tập hợp dữ liệu Generic: Collection và Danh sách List<T>",
                TitleEn = "Generic Collections: Lists (List<T>) and Collections",
                Slug = "collection-va-list-trong-csharp",
                MatchingTutorialSlug = "collections-list-dictionary-linq-co-ban",
                SummaryVi = "Cấu trúc dữ liệu mảng động List<T>, cơ chế tự động nhân đôi Capacity, các thao tác Add, Remove, Insert, Sort, BinarySearch và tối ưu dung lượng ban đầu.",
                SummaryEn = "Dynamic array data structures with List<T>, internal capacity resizing mechanics, Add/Remove/Sort operations, and capacity preallocation tuning.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "List<T>", "Capacity", "Dynamic Array", "Generics" },
                SampleCode = @"var cities = new List<string>(10) { ""Hà Nội"", ""Đà Nẵng"", ""TP.HCM"" };
cities.Add(""Cần Thơ"");
Console.WriteLine($""Số phần tử: {cities.Count}, Dung lượng: {cities.Capacity}"");",
                LearningOutcomesVi = new() { "Hiểu chi phí hiệu năng khi List<T> resize bộ nhớ", "Sử dụng List<T> linh hoạt cho hầu hết nhu cầu lưu trữ tuần tự" },
                LearningOutcomesEn = new() { "Understand resizing allocations in dynamic lists", "Choose List<T> correctly for sequential workflows" }
            },
            new()
            {
                LessonNumber = 32,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Danh sách sắp xếp SortedList<TKey, TValue> và SortedDictionary",
                TitleEn = "Sorted Collections: SortedList<TKey, TValue> and SortedDictionary",
                Slug = "sortedlist-va-sorteddictionary",
                SummaryVi = "Tự động sắp xếp các phần tử theo Key mỗi khi thêm mới, phân biệt giữa cấu trúc mảng đôi SortedList (ít tốn RAM) vs Cây đỏ đen SortedDictionary (thêm xóa nhanh).",
                SummaryEn = "Key-ordered collections, comparing two-array SortedList (low memory) vs red-black tree SortedDictionary (faster insertions/deletions).",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "SortedList", "SortedDictionary", "Binary Search", "Red-Black Tree" },
                SampleCode = @"var sorted = new SortedList<int, string>
{
    { 3, ""Ba"" },
    { 1, ""Một"" },
    { 2, ""Hai"" }
};
// Tự động in theo thứ tự key: 1, 2, 3",
                LearningOutcomesVi = new() { "Chọn đúng cấu trúc dữ liệu theo tần suất đọc vs ghi", "Hiểu độ phức tạp thuật toán O(log N)" },
                LearningOutcomesEn = new() { "Select proper sorted collection based on read vs write ratios", "Understand O(log N) lookup complexity" }
            },
            new()
            {
                LessonNumber = 33,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Hàng đợi Queue<T> (FIFO) và Ngăn xếp Stack<T> (LIFO)",
                TitleEn = "Queues (FIFO Queue<T>) and Stacks (LIFO Stack<T>)",
                Slug = "queue-hang-doi-va-stack-ngan-xep",
                SummaryVi = "Nguyên lý Vào trước Ra trước (FIFO - Enqueue/Dequeue) trong xử lý hàng đợi tác vụ, và Vào sau Ra trước (LIFO - Push/Pop) trong Undo/Redo và duyệt ngăn xếp.",
                SummaryEn = "FIFO Queue semantics (Enqueue/Dequeue) for job scheduling, and LIFO Stack semantics (Push/Pop) for backtracking, undo systems, and evaluation.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Queue", "Stack", "FIFO", "LIFO" },
                SampleCode = @"var queue = new Queue<string>();
queue.Enqueue(""Ticket #1"");
queue.Enqueue(""Ticket #2"");
Console.WriteLine(queue.Dequeue()); // Ticket #1

var stack = new Stack<int>();
stack.Push(10);
stack.Push(20);
Console.WriteLine(stack.Pop()); // 20",
                LearningOutcomesVi = new() { "Ứng dụng hàng đợi cho Message Processing", "Ứng dụng ngăn xếp giải quyết các bài toán duyệt cây hoặc ngoặc hợp lệ" },
                LearningOutcomesEn = new() { "Apply queues for background job processing", "Use stacks for parenthesis matching and tree traversal" }
            },
            new()
            {
                LessonNumber = 34,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Danh sách liên kết đôi: LinkedList<T> và LinkedListNode<T>",
                TitleEn = "Doubly Linked Lists: LinkedList<T> and LinkedListNode<T>",
                Slug = "linkedlist-danh-sach-lien-ket",
                SummaryVi = "Cấu trúc dữ liệu danh sách liên kết đôi cho phép chèn và xóa phần tử ở đầu/cuối hoặc giữa danh sách với tốc độ tức thời O(1) mà không cần dời chuyển mảng.",
                SummaryEn = "Doubly linked list architecture enabling O(1) insertions and removals without memory shifting, using explicit LinkedListNode references.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "LinkedList", "O(1) Insertion", "Pointers", "Data Structure" },
                SampleCode = @"var list = new LinkedList<string>();
var first = list.AddFirst(""Đầu"");
list.AddLast(""Cuối"");
list.AddAfter(first, ""Giữa"");",
                LearningOutcomesVi = new() { "Hiểu sự đánh đổi giữa Cache Locality (List) vs O(1) insert (LinkedList)", "Thao tác trên con trỏ Next/Previous" },
                LearningOutcomesEn = new() { "Analyze CPU cache locality vs linked node trade-offs", "Traverse node pointers cleanly" }
            },
            new()
            {
                LessonNumber = 35,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Bảng băm: Dictionary<TKey, TValue> và Tập hợp duy nhất HashSet<T>",
                TitleEn = "Hash Tables: Dictionary<TKey, TValue> and HashSet<T>",
                Slug = "dictionary-va-hashset-trong-csharp",
                SummaryVi = "Nguyên lý hàm băm GetHashCode() và so sánh Equals(), tốc độ tra cứu tức thời O(1), ngăn ngừa trùng lặp với HashSet, và phương thức an toàn TryGetValue().",
                SummaryEn = "Hash codes (GetHashCode) and equality contracts (Equals), O(1) average lookup speeds, duplicate elimination via HashSet, and safe TryGetValue API.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Dictionary", "HashSet", "Hash Table", "O(1) Lookup" },
                SampleCode = @"var dict = new Dictionary<string, int> { [""A""] = 100, [""B""] = 200 };
if (dict.TryGetValue(""A"", out int val))
{
    Console.WriteLine($""Tìm thấy: {val}"");
}

var set = new HashSet<int> { 1, 2, 2, 3 };
Console.WriteLine(set.Count); // 3 (không có trùng lặp)",
                LearningOutcomesVi = new() { "Luôn dùng TryGetValue thay vì kiểm tra ContainsKey rồi mới truy cập qua indexer", "Hiểu cách Override GetHashCode & Equals khi dùng class làm Key" },
                LearningOutcomesEn = new() { "Favor TryGetValue over double indexing", "Implement proper GetHashCode and Equals on custom key classes" }
            },
            new()
            {
                LessonNumber = 36,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Tập hợp tự động thông báo giao diện: ObservableCollection<T>",
                TitleEn = "Change-Notifying Collections: ObservableCollection<T>",
                Slug = "observablecollection-trong-csharp-net",
                SummaryVi = "Triển khai INotifyCollectionChanged, tự động phát sinh sự kiện CollectionChanged khi thêm/sửa/xóa phần tử, nền tảng cốt lõi trong lập trình GUI WPF, MAUI và Blazor.",
                SummaryEn = "Implementing INotifyCollectionChanged, notifying UI bindings on item mutations, standard foundation for WPF, .NET MAUI, and Blazor architectures.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "ObservableCollection", "INotifyCollectionChanged", "WPF", "Blazor" },
                SampleCode = @"using System.Collections.ObjectModel;

var list = new ObservableCollection<string>();
list.CollectionChanged += (s, e) => Console.WriteLine($""Có thay đổi: {e.Action}"");
list.Add(""Sản phẩm mới"");",
                LearningOutcomesVi = new() { "Hiểu cơ chế data-binding hai chiều giữa Model và UI", "Ứng dụng trong các ứng dụng desktop hoặc web client-side" },
                LearningOutcomesEn = new() { "Understand bidirectional UI data binding", "Apply reactive list updates in desktop/mobile apps" }
            },
            new()
            {
                LessonNumber = 37,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Truy vấn dữ liệu với LINQ (Language Integrated Query)",
                TitleEn = "Language Integrated Query (LINQ) Mastery",
                Slug = "linq-trong-lap-trinh-csharp",
                MatchingTutorialSlug = "linq-to-objects-linq-to-entities-nang-cao",
                SummaryVi = "Cú pháp truy vấn Query Syntax vs Phương thức mở rộng Method Syntax, cơ chế thực thi trì hoãn (Deferred Execution), các toán tử Where, Select, OrderBy, GroupBy, Join, Any, All.",
                SummaryEn = "Query syntax vs method syntax, Deferred Execution mechanics, core query operators (Where, Select, OrderBy, GroupBy, Join, Any, All), and IEnumerable pipelines.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "LINQ", "Deferred Execution", "Select", "Where", "GroupBy" },
                SampleCode = @"var products = new[] {
    new { Name = ""Laptop"", Price = 1200 },
    new { Name = ""Mouse"", Price = 25 },
    new { Name = ""Keyboard"", Price = 75 }
};

var expensive = products.Where(p => p.Price > 50).OrderByDescending(p => p.Price);
foreach (var p in expensive) Console.WriteLine($""{p.Name}: ${p.Price}"");",
                LearningOutcomesVi = new() { "Viết truy vấn dữ liệu ngắn gọn, thanh lịch không cần vòng lặp for thủ công", "Nắm vững nguyên lý Deferred Execution để tránh lỗi truy vấn lặp lại nhiều lần" },
                LearningOutcomesEn = new() { "Write clean declarative queries eliminating procedural loops", "Master deferred execution to avoid multiple enumerations" }
            },
            new()
            {
                LessonNumber = 38,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Lập trình bất đồng bộ: Task, Task<T>, async và await",
                TitleEn = "Asynchronous Programming: Task, Task<T>, async, and await",
                Slug = "lap-trinh-bat-dong-bo-async-await",
                MatchingTutorialSlug = "lap-trinh-bat-dong-bo-async-await-multithreading",
                SummaryVi = "Mô hình Task-based Asynchronous Pattern (TAP), giải phóng luồng máy chủ không bị nghẽn (non-blocking I/O), ConfigureAwait(false), Task.WhenAll và xử lý ngoại lệ bất đồng bộ.",
                SummaryEn = "Task-based Asynchronous Pattern (TAP), non-blocking I/O thread pooling, Task.WhenAll orchestration, and safe async exception propagation.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 35,
                KeyTags = new() { "async/await", "Task", "Non-blocking", "TAP", "Thread Pool" },
                SampleCode = @"static async Task<string> DownloadContentAsync(string url)
{
    using var http = new HttpClient();
    return await http.GetStringAsync(url);
}",
                LearningOutcomesVi = new() { "Hiểu tại sao async/await không tạo ra thread mới cho I/O bound", "Tránh triệt để Deadlock khi gọi .Result hoặc .Wait()" },
                LearningOutcomesEn = new() { "Understand OS completion ports behind non-blocking I/O", "Avoid synchronous-over-async deadlocks (.Result / .Wait())" }
            },
            new()
            {
                LessonNumber = 39,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Khảo sát siêu dữ liệu: Lớp Type và Kỹ thuật Reflection",
                TitleEn = "Metadata Inspection: The Type Class and Reflection",
                Slug = "lop-type-va-ky-thuat-reflection",
                SummaryVi = "Đọc siêu dữ liệu (Metadata) lúc runtime: kiểm tra type, duyệt danh sách thuộc tính/phương thức, tạo instance động với Activator.CreateInstance(), và invoke method linh hoạt.",
                SummaryEn = "Runtime metadata introspection: exploring properties/methods, dynamic object activation with Activator.CreateInstance(), and method invocation via reflection.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 30,
                KeyTags = new() { "Reflection", "Type", "Activator", "Metadata" },
                SampleCode = @"var type = typeof(string);
Console.WriteLine($""Tên kiểu: {type.FullName}"");
foreach (var prop in typeof(DateTime).GetProperties().Take(3))
{
    Console.WriteLine($""Property: {prop.Name} ({prop.PropertyType.Name})"");
}",
                LearningOutcomesVi = new() { "Hiểu cách các framework ORM (EF Core) và JSON Serializer hoạt động", "Cân nhắc yếu tố hiệu năng khi dùng Reflection" },
                LearningOutcomesEn = new() { "Understand how ORMs and serializers inspect object schemas", "Assess reflection performance overhead" }
            },
            new()
            {
                LessonNumber = 40,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Thuộc tính chú thích: Attribute và Data Annotations",
                TitleEn = "Attributes and Data Annotations in C#",
                Slug = "attribute-va-data-annotations",
                SummaryVi = "Gắn nhãn metadata lên class/property với Attribute ([Required], [MaxLength], [JsonPropertyName]), và tự xây dựng Custom Attribute kế thừa từ System.Attribute.",
                SummaryEn = "Declaring declarative metadata with Attributes ([Required], [MaxLength], [JsonPropertyName]), and authoring Custom Attribute types.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Attribute", "DataAnnotation", "Validation", "Custom Attribute" },
                SampleCode = @"[AttributeUsage(AttributeTargets.Property)]
public class AuditLogAttribute : Attribute { }

public class Account
{
    [AuditLog]
    public decimal Balance { get; set; }
}",
                LearningOutcomesVi = new() { "Sử dụng Data Annotations để kiểm tra tính hợp lệ dữ liệu Model Validation", "Viết Custom Attribute đánh dấu các tính năng bảo mật hoặc logging" },
                LearningOutcomesEn = new() { "Apply validation attributes for model state checks", "Author custom marker attributes for aspect logging" }
            },
            new()
            {
                LessonNumber = 41,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Dependency Injection (DI) với Microsoft ServiceCollection",
                TitleEn = "Dependency Injection with Microsoft ServiceCollection",
                Slug = "dependency-injection-di-servicecollection",
                MatchingTutorialSlug = "dependency-injection-va-service-lifetimes-trong-aspnet-core",
                SummaryVi = "Nguyên lý Đảo ngược phụ thuộc (Inversion of Control - IoC), 3 vòng đời dịch vụ cốt lõi: Transient, Scoped, Singleton, và xây dựng Service Provider trong C# Console / Web.",
                SummaryEn = "Inversion of Control (IoC) principles, the 3 foundational service lifetimes (Transient, Scoped, Singleton), and configuring IServiceCollection.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 35,
                KeyTags = new() { "DI", "IoC", "ServiceCollection", "Singleton", "Scoped", "Transient" },
                SampleCode = @"using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddTransient<IMessageService, EmailService>();
var provider = services.BuildServiceProvider();
var svc = provider.GetRequiredService<IMessageService>();",
                LearningOutcomesVi = new() { "Nắm vững nguyên lý SOLID - Chữ D (Dependency Inversion)", "Chọn đúng Service Lifetime để tránh rò rỉ bộ nhớ hoặc lỗi concurrency" },
                LearningOutcomesEn = new() { "Master the 'D' in SOLID principles", "Select proper service lifetimes to avoid concurrency race conditions" }
            },
            new()
            {
                LessonNumber = 42,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Lập trình song song: Parallel.For, Parallel.ForEach và PLINQ",
                TitleEn = "Parallel Programming: Parallel.For, Parallel.ForEach, and PLINQ",
                Slug = "lap-trinh-song-song-parallel-plinq",
                SummaryVi = "Tận dụng tối đa tất cả các nhân CPU (Multi-core CPU) cho các tác vụ tính toán nặng (CPU-bound) với Parallel.ForEach và Parallel LINQ (AsParallel), tránh race conditions.",
                SummaryEn = "Harnessing multi-core CPU architectures for compute-intensive tasks via Parallel.ForEach, Parallel LINQ (AsParallel), and thread safety precautions.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 30,
                KeyTags = new() { "Parallel", "PLINQ", "Multi-core", "CPU-bound", "AsParallel" },
                SampleCode = @"var numbers = Enumerable.Range(1, 1000000).ToList();
var primes = numbers.AsParallel()
                    .Where(n => n % 2 != 0)
                    .Count();
Console.WriteLine($""Đã xử lý song song: {primes}"");",
                LearningOutcomesVi = new() { "Phân biệt rõ ràng tác vụ CPU-bound (dùng Parallel) vs I/O-bound (dùng async/await)", "Biết cách giới hạn MaxDegreeOfParallelism" },
                LearningOutcomesEn = new() { "Distinguish CPU-bound parallelism from I/O-bound async tasks", "Tune MaxDegreeOfParallelism" }
            },
            new()
            {
                LessonNumber = 43,
                SectionNumber = 2,
                SectionTitleVi = s2.TitleVi,
                SectionTitleEn = s2.TitleEn,
                SectionBadgeColor = s2.BadgeColor,
                SectionIcon = s2.Icon,
                TitleVi = "Đóng gói Class Library & Xuất bản gói thư viện lên NuGet.org",
                TitleEn = "Class Libraries & Publishing NuGet Packages",
                Slug = "dong-goi-class-library-nuget",
                SummaryVi = "Tạo dự án thư viện dùng chung (.NET Class Library), cấu hình thông tin PackageId, Version, Author trong file .csproj, đóng gói dotnet pack và đẩy lên NuGet.",
                SummaryEn = "Authoring reusable .NET class libraries, configuring PackageId, Version, and Release Notes in .csproj, packing with dotnet pack, and publishing to NuGet.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Class Library", "NuGet", "dotnet pack", "Publish" },
                SampleCode = @"<!-- Trong file .csproj -->
<PropertyGroup>
  <PackageId>MyAwesomeLib.Utilities</PackageId>
  <Version>1.0.0</Version>
  <Authors>Developer</Authors>
</PropertyGroup>",
                LearningOutcomesVi = new() { "Tái sử dụng mã nguồn giữa nhiều dự án nội bộ hoặc cộng đồng", "Biết quy trình Semantic Versioning (SemVer)" },
                LearningOutcomesEn = new() { "Distribute code across multi-repository organizations", "Apply Semantic Versioning standards" }
            }
        });
        sections.Add(s2);

        // ----------------------------------------------------
        // PHẦN 3: NETWORKING (Bài 44 -> Bài 48)
        // ----------------------------------------------------
        var s3 = new CurriculumSectionViewModel
        {
            SectionNumber = 3,
            TitleVi = "Phần 3 - Lập trình Mạng (Networking)",
            TitleEn = "Part 3 - Networking with C# .NET",
            DescriptionVi = "Làm việc với giao thức mạng, DNS, Ping, gửi nhận HTTP Request/Response với HttpClient, viết Custom Handler và lập trình Socket TCP.",
            DescriptionEn = "Working with network protocols, DNS resolution, ICMP Ping, HTTP communications with HttpClient, message handlers, and TCP sockets.",
            BadgeColor = "danger",
            Icon = "bi-wifi",
            Level = DifficultyLevel.Intermediate,
            TotalEstimatedHours = 12
        };

        s3.Lessons.AddRange(new List<CurriculumLessonViewModel>
        {
            new()
            {
                LessonNumber = 44,
                SectionNumber = 3,
                SectionTitleVi = s3.TitleVi,
                SectionTitleEn = s3.TitleEn,
                SectionBadgeColor = s3.BadgeColor,
                SectionIcon = s3.Icon,
                TitleVi = "Networking cơ bản: Lớp Uri, Dns, Ping và IPAddress trong .NET",
                TitleEn = "Networking Basics: Uri, Dns, Ping, and IPAddress Classes",
                Slug = "networking-uri-dns-ping-ipaddress",
                SummaryVi = "Phân tích địa chỉ web với lớp Uri, tra cứu địa chỉ IP qua Dns.GetHostAddressesAsync(), và kiểm tra độ trễ server với Ping.",
                SummaryEn = "Deconstructing URLs with the Uri class, resolving host names using Dns.GetHostAddressesAsync(), and measuring latency with Ping.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Uri", "Dns", "Ping", "IPAddress", "Networking" },
                SampleCode = @"var ping = new System.Net.NetworkInformation.Ping();
var reply = await ping.SendPingAsync(""google.com"");
Console.WriteLine($""Ping status: {reply.Status}, Roundtrip: {reply.RoundtripTime}ms"");",
                LearningOutcomesVi = new() { "Hiểu cấu trúc các thành phần Host, Scheme, Port, Query trong một Uri", "Tạo tiện ích kiểm tra trạng thái sống của máy chủ" },
                LearningOutcomesEn = new() { "Parse URL schemes, hosts, ports, and queries", "Build server health check pingers" }
            },
            new()
            {
                LessonNumber = 45,
                SectionNumber = 3,
                SectionTitleVi = s3.TitleVi,
                SectionTitleEn = s3.TitleEn,
                SectionBadgeColor = s3.BadgeColor,
                SectionIcon = s3.Icon,
                TitleVi = "Lập trình HTTP Client: Gửi truy vấn HTTP GET, POST và JSON API",
                TitleEn = "HTTP Programming: GET, POST, and REST APIs with HttpClient",
                Slug = "networking-httpclient-rest-api",
                SummaryVi = "Sử dụng HttpClient đúng cách (tránh cạn kiệt socket), gửi GET/POST request, truyền header và giải mã dữ liệu JSON với System.Net.Http.Json.",
                SummaryEn = "Safe HttpClient socket reuse, executing GET/POST requests, custom headers, and seamless JSON serialization using System.Net.Http.Json.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "HttpClient", "GET/POST", "REST API", "JSON" },
                SampleCode = @"using System.Net.Http.Json;

using var client = new HttpClient();
var todo = await client.GetFromJsonAsync<TodoItem>(""https://jsonplaceholder.typicode.com/todos/1"");
Console.WriteLine($""Task: {todo?.Title}"");

record TodoItem(int Id, string Title, bool Completed);",
                LearningOutcomesVi = new() { "Giao tiếp với bất kỳ REST API bên thứ 3 nào", "Hiểu tại sao không nên new HttpClient() liên tục trong vòng lặp" },
                LearningOutcomesEn = new() { "Consume any third-party RESTful API", "Prevent TCP socket exhaustion by reusing HttpClient instances" }
            },
            new()
            {
                LessonNumber = 46,
                SectionNumber = 3,
                SectionTitleVi = s3.TitleVi,
                SectionTitleEn = s3.TitleEn,
                SectionBadgeColor = s3.BadgeColor,
                SectionIcon = s3.Icon,
                TitleVi = "Tùy biến luồng HTTP: HttpMessageHandler và DelegatingHandler",
                TitleEn = "HTTP Pipeline Customization: HttpMessageHandler & DelegatingHandler",
                Slug = "networking-httpmessagehandler",
                SummaryVi = "Xây dựng pipeline chặn và can thiệp HTTP Request trước khi gửi đi: tự động gắn Bearer Token, ghi log thời gian phản hồi, hoặc tích hợp Polly retry.",
                SummaryEn = "Authoring DelegatingHandler interceptors for automated Bearer token attachment, timing logging, and resilient Polly retry policies.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 25,
                KeyTags = new() { "HttpMessageHandler", "DelegatingHandler", "IHttpClientFactory", "Polly" },
                SampleCode = @"public class AuthHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        req.Headers.Add(""X-Custom-Header"", ""Antigravity-LMS"");
        return await base.SendAsync(req, ct);
    }
}",
                LearningOutcomesVi = new() { "Tạo các bộ chặn (Interceptors) tập trung cho toàn bộ ứng dụng", "Đăng ký Typed Client với IHttpClientFactory" },
                LearningOutcomesEn = new() { "Create centralized client-side middleware for HTTP calls", "Register typed clients with IHttpClientFactory" }
            },
            new()
            {
                LessonNumber = 47,
                SectionNumber = 3,
                SectionTitleVi = s3.TitleVi,
                SectionTitleEn = s3.TitleEn,
                SectionBadgeColor = s3.BadgeColor,
                SectionIcon = s3.Icon,
                TitleVi = "Xây dựng máy chủ Web HTTP tối giản với lớp HttpListener",
                TitleEn = "Minimal HTTP Web Server with HttpListener",
                Slug = "networking-httplistener-mini-server",
                SummaryVi = "Tự xây dựng một HTTP Web Server mini lắng nghe request từ trình duyệt, đọc URL yêu cầu, và gửi về mã HTML phản hồi bằng HttpListener.",
                SummaryEn = "Building a lightweight HTTP web server accepting browser requests, inspecting requested routes, and streaming HTML responses via HttpListener.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "HttpListener", "Web Server", "HTTP Protocol", "Listen" },
                SampleCode = @"var server = new System.Net.HttpListener();
server.Prefixes.Add(""http://localhost:5050/"");
server.Start();
Console.WriteLine(""Server đang lắng nghe tại http://localhost:5050/..."");
var context = await server.GetContextAsync();
byte[] msg = System.Text.Encoding.UTF8.GetBytes(""<h1>Xin chào từ HttpListener!</h1>"");
context.Response.OutputStream.Write(msg);
server.Stop();",
                LearningOutcomesVi = new() { "Hiểu bản chất cách máy chủ Web tiếp nhận và phân phối HTTP Request", "Tạo các máy chủ dịch vụ mini chạy ngầm" },
                LearningOutcomesEn = new() { "Understand raw HTTP server socket listeners", "Implement lightweight headless service daemons" }
            },
            new()
            {
                LessonNumber = 48,
                SectionNumber = 3,
                SectionTitleVi = s3.TitleVi,
                SectionTitleEn = s3.TitleEn,
                SectionBadgeColor = s3.BadgeColor,
                SectionIcon = s3.Icon,
                TitleVi = "Lập trình mạng tầng Transport: Giao thức TCP với TcpListener và TcpClient",
                TitleEn = "Transport Layer Networking: TCP with TcpListener and TcpClient",
                Slug = "networking-tcp-tcplistener-tcpclient",
                SummaryVi = "Lập trình Socket mạng TCP: tạo kết nối hai chiều Client - Server ổn định, trao đổi gói tin dạng byte buffer, ứng dụng làm phòng Chat hoặc truyền file.",
                SummaryEn = "Raw TCP Socket programming: establishing bidirectional reliable client-server communication, byte streams, and building a simple chatroom.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 35,
                KeyTags = new() { "TCP", "TcpListener", "TcpClient", "Socket", "Buffer" },
                SampleCode = @"var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 8888);
listener.Start();
// Sẵn sàng nhận kết nối từ TcpClient...",
                LearningOutcomesVi = new() { "Hiểu sự khác biệt giữa giao vận TCP tin cậy vs UDP tốc độ cao", "Tự tay viết ứng dụng Client-Server socket đơn giản" },
                LearningOutcomesEn = new() { "Contrast TCP reliable byte streams vs UDP datagrams", "Author socket-level client-server network applications" }
            }
        });
        sections.Add(s3);

        // ----------------------------------------------------
        // PHẦN 4: DATABASE & EF CORE (Bài 49 -> Bài 57)
        // ----------------------------------------------------
        var s4 = new CurriculumSectionViewModel
        {
            SectionNumber = 4,
            TitleVi = "Phần 4 - Cơ sở dữ liệu: ADO.NET & Entity Framework Core",
            TitleEn = "Part 4 - Databases: ADO.NET & Entity Framework Core",
            DescriptionVi = "Kết nối SQL Server, thực thi ADO.NET truyền thống và làm chủ Entity Framework Core: Code-First, Fluent API, Migrations, LINQ to Entities và tối ưu hiệu năng.",
            DescriptionEn = "Connecting to SQL Server, classic ADO.NET data access, and mastering EF Core: Code-First, Fluent API, Migrations, LINQ queries, and performance profiling.",
            BadgeColor = "warning",
            Icon = "bi-database-fill",
            Level = DifficultyLevel.Intermediate,
            TotalEstimatedHours = 22
        };

        s4.Lessons.AddRange(new List<CurriculumLessonViewModel>
        {
            new()
            {
                LessonNumber = 49,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "ADO.NET: Tổng quan và Kết nối SQL Server với SqlConnection",
                TitleEn = "ADO.NET: Overview and SQL Server Connection via SqlConnection",
                Slug = "ado-net-sqlconnection-ket-noi-database",
                SummaryVi = "Kiến trúc truy cập dữ liệu tầng thấp ADO.NET, chuỗi kết nối Connection String, mở kết nối OpenAsync() và giải phóng tài nguyên an toàn.",
                SummaryEn = "Low-level data access fundamentals with ADO.NET, Connection String configuration, OpenAsync lifecycle, and connection pooling best practices.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "ADO.NET", "SqlConnection", "Connection String", "SQL Server" },
                SampleCode = @"using Microsoft.Data.SqlClient;

string connStr = ""Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;"";
await using var conn = new SqlConnection(connStr);
await conn.OpenAsync();
Console.WriteLine($""Trạng thái kết nối: {conn.State}"");",
                LearningOutcomesVi = new() { "Cấu hình chuỗi kết nối SQL Server chính xác", "Hiểu cách cơ chế Connection Pooling hoạt động dưới nền" },
                LearningOutcomesEn = new() { "Configure robust SQL Server connection strings", "Understand connection pooling dynamics under the hood" }
            },
            new()
            {
                LessonNumber = 50,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "ADO.NET: Thực thi truy vấn và Đọc dữ liệu với SqlCommand & SqlDataReader",
                TitleEn = "ADO.NET: Executing Queries with SqlCommand and SqlDataReader",
                Slug = "ado-net-sqlcommand-sqldatareader",
                SummaryVi = "Thực thi lệnh SQL: ExecuteNonQuery (thêm/sửa/xóa), ExecuteScalar (lấy giá trị đơn), và đọc dòng dữ liệu stream với SqlDataReader, phòng chống SQL Injection bằng Parameters.",
                SummaryEn = "Executing SQL commands: ExecuteNonQuery for updates, ExecuteScalar for aggregates, streaming results via SqlDataReader, and parameterized queries against SQL Injection.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "SqlCommand", "SqlDataReader", "SQL Injection", "Parameters" },
                SampleCode = @"using var cmd = conn.CreateCommand();
cmd.CommandText = ""SELECT Id, Name FROM Users WHERE Status = @status"";
cmd.Parameters.AddWithValue(""@status"", ""Active"");

await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    Console.WriteLine($""ID: {reader[""Id""]}, Tên: {reader[""Name""]}"");
}",
                LearningOutcomesVi = new() { "Tuyệt đối không cộng chuỗi vào câu lệnh SQL để triệt tiêu SQL Injection", "Đọc luồng dữ liệu tiến tới nhanh chóng với DataReader" },
                LearningOutcomesEn = new() { "Prevent SQL Injection vulnerabilities through parameters", "Stream forward-only data records efficiently" }
            },
            new()
            {
                LessonNumber = 51,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "ADO.NET: Quản lý tập dữ liệu ngắt kết nối với DataAdapter, DataSet và DataTable",
                TitleEn = "ADO.NET: Disconnected Datasets with DataAdapter, DataSet, and DataTable",
                Slug = "ado-net-dataadapter-dataset-datatable",
                SummaryVi = "Mô hình làm việc Disconnected: nạp dữ liệu vào bộ nhớ RAM máy tính bằng SqlDataAdapter.Fill(), duyệt DataTable và đồng bộ ngược về DB với Update().",
                SummaryEn = "Disconnected in-memory caching model: populating DataTables via SqlDataAdapter.Fill(), working offline, and syncing changes back using Update().",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "DataAdapter", "DataTable", "DataSet", "Disconnected" },
                SampleCode = @"var da = new SqlDataAdapter(""SELECT * FROM Products"", conn);
var dt = new System.Data.DataTable();
da.Fill(dt);
Console.WriteLine($""Số bản ghi tải về: {dt.Rows.Count}"");",
                LearningOutcomesVi = new() { "Hiểu mô hình dữ liệu ngắt kết nối kinh điển của .NET Framework", "Phân biệt ưu nhược điểm so với ORM hiện đại" },
                LearningOutcomesEn = new() { "Understand classic disconnected dataset models", "Assess trade-offs against modern ORMs" }
            },
            new()
            {
                LessonNumber = 52,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "Entity Framework Core: Tổng quan ORM và Cấu hình DbContext",
                TitleEn = "EF Core: ORM Overview and Configuring DbContext",
                Slug = "ef-core-tong-quan-va-dbcontext",
                MatchingTutorialSlug = "entity-framework-core-co-ban-code-first",
                SummaryVi = "Khái niệm Object-Relational Mapping (ORM), cài đặt package Microsoft.EntityFrameworkCore.SqlServer, kế thừa DbContext, và đăng ký DbSet<T>.",
                SummaryEn = "Object-Relational Mapping concepts, NuGet package installation, subclassing DbContext, and declaring DbSet<T> entity tables.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "EF Core", "ORM", "DbContext", "DbSet" },
                SampleCode = @"public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
    public DbSet<Product> Products => Set<Product>();
}",
                LearningOutcomesVi = new() { "Thiết lập thành công DbContext đầu tiên", "Hiểu cách EF Core biến class C# thành các bảng CSDL" },
                LearningOutcomesEn = new() { "Configure your foundational DbContext", "Learn how EF Core maps entities to relational tables" }
            },
            new()
            {
                LessonNumber = 53,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "EF Core: Thiết kế Data Models và Data Annotations",
                TitleEn = "EF Core: Entity Modeling and Data Annotations",
                Slug = "ef-core-thiet-ke-model-data-annotations",
                SummaryVi = "Quy ước đặt tên khóa chính (Id / [Key]), đặt tên bảng [Table], ràng buộc độ dài [MaxLength], trường bắt buộc [Required], và khóa ngoại Foreign Key.",
                SummaryEn = "Primary key conventions ([Key]), custom table mapping ([Table]), length bounds ([MaxLength]), not-null constraints ([Required]), and Foreign Keys.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Data Annotations", "EF Model", "[Key]", "Foreign Key" },
                SampleCode = @"[Table(""Tbl_Articles"")]
public class Article
{
    [Key]
    public int ArticleId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
}",
                LearningOutcomesVi = new() { "Thiết kế model chuẩn kiểm soát schema CSDL", "Tránh tạo các cột NVARCHAR(MAX) không cần thiết" },
                LearningOutcomesEn = new() { "Design schemas matching database constraints", "Prevent inadvertent NVARCHAR(MAX) bloat" }
            },
            new()
            {
                LessonNumber = 54,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "EF Core: Cấu hình quan hệ nâng cao (1-1, 1-N, N-N) với Fluent API",
                TitleEn = "EF Core: Relationships (1-1, 1-N, N-N) with Fluent API",
                Slug = "ef-core-fluent-api-cau-hinh-quan-he",
                MatchingTutorialSlug = "fluent-api-cau-hinh-quan-he-ef-core",
                SummaryVi = "Cấu hình trong OnModelCreating: HasOne().WithMany(), HasMany().WithMany(), đặt hành vi xóa OnDelete(Cascade / Restrict / SetNull), và tạo Index duy nhất.",
                SummaryEn = "OnModelCreating configuration: HasOne().WithMany(), many-to-many navigation, delete behaviors (Cascade, Restrict, SetNull), and composite indexes.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "Fluent API", "Relationships", "1-N", "N-N", "Cascade Delete" },
                SampleCode = @"modelBuilder.Entity<Course>()
    .HasMany(c => c.Lessons)
    .WithOne(l => l.Course)
    .HasForeignKey(l => l.CourseId)
    .OnDelete(DeleteBehavior.Cascade);",
                LearningOutcomesVi = new() { "Làm chủ Fluent API thay vì chỉ phụ thuộc Data Annotations", "Xử lý quan hệ N-N tự động hoặc có bảng trung gian" },
                LearningOutcomesEn = new() { "Prefer clean Fluent API over intrusive model attributes", "Model complex many-to-many join structures" }
            },
            new()
            {
                LessonNumber = 55,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "EF Core: Truy vấn LINQ to Entities và Tối ưu hiệu năng đọc",
                TitleEn = "EF Core: LINQ to Entities Queries and Performance Tuning",
                Slug = "ef-core-truy-van-linq-to-entities",
                SummaryVi = "Eager Loading với .Include(), Explicit Loading, tối ưu tốc độ đọc với AsNoTracking(), phân trang với Skip() và Take(), xử lý vấn đề N+1 queries.",
                SummaryEn = "Eager Loading via .Include(), Explicit Loading, read-only speedups via AsNoTracking(), pagination via Skip()/Take(), and resolving N+1 query traps.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "LINQ to Entities", "Include()", "AsNoTracking", "N+1 Problem" },
                SampleCode = @"var posts = await context.Posts
    .AsNoTracking()
    .Include(p => p.Author)
    .Where(p => p.IsPublished)
    .OrderByDescending(p => p.CreatedAt)
    .Take(10)
    .ToListAsync();",
                LearningOutcomesVi = new() { "Luôn dùng AsNoTracking() cho các trang chỉ hiển thị đọc dữ liệu", "Phát hiện và loại bỏ triệt để lỗi N+1 Query" },
                LearningOutcomesEn = new() { "Always apply AsNoTracking() on read-only endpoints", "Diagnose and eradicate N+1 query execution bottlenecks" }
            },
            new()
            {
                LessonNumber = 56,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "EF Core Database-First: Sinh Entity tự động bằng Scaffold (Reverse Engineering)",
                TitleEn = "EF Core Database-First: Reverse Engineering via Scaffold",
                Slug = "ef-core-scaffold-database-first",
                SummaryVi = "Sử dụng công cụ dòng lệnh dotnet ef dbcontext scaffold để tự động đọc lược đồ CSDL có sẵn và sinh ra toàn bộ Entity Classes cùng DbContext tương ứng.",
                SummaryEn = "Using dotnet ef dbcontext scaffold command to inspect existing legacy databases and automatically generate C# entities and matching DbContext.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "Database-First", "dotnet ef scaffold", "Reverse Engineering" },
                SampleCode = @"dotnet ef dbcontext scaffold ""Server=.;Database=Northwind;Trusted_Connection=True;"" Microsoft.EntityFrameworkCore.SqlServer -o Models",
                LearningOutcomesVi = new() { "Làm việc với các dự án đã có sẵn Database từ trước", "Cập nhật model khi CSDL thay đổi" },
                LearningOutcomesEn = new() { "Work seamlessly on legacy pre-existing databases", "Update entity mappings when database schema changes" }
            },
            new()
            {
                LessonNumber = 57,
                SectionNumber = 4,
                SectionTitleVi = s4.TitleVi,
                SectionTitleEn = s4.TitleEn,
                SectionBadgeColor = s4.BadgeColor,
                SectionIcon = s4.Icon,
                TitleVi = "EF Core Code-First: Quản lý biến đổi lược đồ với Migrations",
                TitleEn = "EF Core Code-First: Schema Migrations Lifecycle",
                Slug = "ef-core-migrations-code-first",
                MatchingTutorialSlug = "migrations-va-database-update-trong-ef-core",
                SummaryVi = "Quy trình dotnet ef migrations add, xem mã Up() và Down(), áp dụng vào DB với dotnet ef database update, và xuất mã SQL Script cho môi trường Production.",
                SummaryEn = "Migration lifecycle with dotnet ef migrations add, inspecting Up()/Down() migration scripts, applying changes via database update, and idempotent script generation.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Migrations", "Code-First", "database update", "Idempotent Script" },
                SampleCode = @"dotnet ef migrations add AddUserAvatarUrl
dotnet ef database update",
                LearningOutcomesVi = new() { "Quản lý phiên bản CSDL đồng bộ với Git mã nguồn", "Rollback về phiên bản CSDL trước đó an toàn" },
                LearningOutcomesEn = new() { "Version-control database schemas alongside code", "Safely roll back problematic schema versions" }
            }
        });
        sections.Add(s4);

        // ----------------------------------------------------
        // PHẦN 5: ASP.NET CORE NỀN TẢNG (Bài 58 -> Bài 66)
        // ----------------------------------------------------
        var s5 = new CurriculumSectionViewModel
        {
            SectionNumber = 5,
            TitleVi = "Phần 5 - Nền tảng Web ASP.NET Core",
            TitleEn = "Part 5 - ASP.NET Core Architecture & Pipeline",
            DescriptionVi = "Tìm hiểu kiến trúc Web Host, Request Pipeline, Middleware, xử lý HTTP Context, Session, cấu hình appsettings.json, Gửi Mail và quản lý thư viện client-side.",
            DescriptionEn = "Explore Web Host architecture, HTTP Request Pipeline, custom Middleware, HttpContext handling, Sessions, configuration Options, Email services, and LibMan.",
            BadgeColor = "primary",
            Icon = "bi-globe2",
            Level = DifficultyLevel.Intermediate,
            TotalEstimatedHours = 20
        };

        s5.Lessons.AddRange(new List<CurriculumLessonViewModel>
        {
            new()
            {
                LessonNumber = 58,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Kiến trúc ứng dụng Web ASP.NET Core: Host, Program.cs & Ứng dụng đầu tiên",
                TitleEn = "ASP.NET Core Web Architecture: Host, Program.cs & First Web App",
                Slug = "aspnet-core-kien-truc-web-host",
                MatchingTutorialSlug = "tong-quan-aspnet-core-va-cau-truc-du-an",
                SummaryVi = "Khởi tạo WebApplication.CreateBuilder(), cấu hình máy chủ Kestrel tích hợp, phân biệt phần xây dựng dịch vụ builder.Services và cấu hình pipeline app.Run().",
                SummaryEn = "Bootstrapping WebApplication.CreateBuilder(), embedded Kestrel server, separating builder.Services registration from app pipeline execution.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "ASP.NET Core", "WebApplication", "Kestrel", "Program.cs" },
                SampleCode = @"var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet(""/"", () => ""Xin chào từ ASP.NET Core Web!"");

app.Run();",
                LearningOutcomesVi = new() { "Hiểu cấu trúc luồng khởi động trong file Program.cs", "Biết máy chủ web Kestrel nhận kết nối từ đâu" },
                LearningOutcomesEn = new() { "Understand Program.cs bootstrapper lifecycle", "Learn how Kestrel handles inbound requests" }
            },
            new()
            {
                LessonNumber = 59,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Middleware nâng cao: Tạo Custom Middleware và Đăng ký vào Pipeline",
                TitleEn = "Advanced Middleware: Custom Middleware and Request Pipeline",
                Slug = "aspnet-core-custom-middleware-pipeline",
                MatchingTutorialSlug = "middleware-va-request-pipeline-trong-aspnet-core",
                SummaryVi = "Khái niệm ống dẫn (Pipeline), cơ chế chuyển tiếp với RequestDelegate next(), viết Custom Middleware class và phương thức mở rộng UseMyMiddleware().",
                SummaryEn = "Pipeline pipeline design, delegation via RequestDelegate next(), authoring custom Middleware classes, and clean Use...() extension conventions.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "Middleware", "Pipeline", "RequestDelegate", "UseMiddleware" },
                SampleCode = @"public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await next(context);
        sw.Stop();
        logger.LogInformation($""{context.Request.Path} xử lý trong {sw.ElapsedMilliseconds}ms"");
    }
}",
                LearningOutcomesVi = new() { "Hiểu thứ tự quan trọng của Middleware (Authentication trước Authorization, v.v.)", "Xây dựng các bộ lọc ghi log hoặc bảo mật chung" },
                LearningOutcomesEn = new() { "Master middleware ordering rules", "Build cross-cutting logging or security filters" }
            },
            new()
            {
                LessonNumber = 60,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Xử lý Request & Response: Đọc route, headers, upload file và trả về JSON",
                TitleEn = "Request & Response Handling: Routes, Headers, File Uploads, and JSON",
                Slug = "aspnet-core-request-response-upload-file",
                SummaryVi = "Thao tác trực tiếp trên đối tượng HttpContext: đọc QueryString, Headers, Cookies, tiếp nhận Multipart Form Upload file, và cấu hình mã trạng thái Response.StatusCode.",
                SummaryEn = "Interacting with HttpContext directly: Query parameters, Headers, Cookie jars, multipart form uploads, and Response status code formatting.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "HttpContext", "HttpRequest", "HttpResponse", "Cookies" },
                SampleCode = @"app.MapPost(""/api/echo"", async (HttpRequest req) => {
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Ok(new { Length = body.Length, Echo = body });
});",
                LearningOutcomesVi = new() { "Hiểu cách dữ liệu HTTP thực sự được vận chuyển qua mạng", "Xử lý tệp đính kèm và đọc header thủ công" },
                LearningOutcomesEn = new() { "Understand raw HTTP payload transport", "Handle uploaded files and read custom authorization headers" }
            },
            new()
            {
                LessonNumber = 61,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Đăng ký dịch vụ DI vào IServiceCollection & Rẽ nhánh Pipeline với Map/MapWhen",
                TitleEn = "Service DI Registration & Pipeline Branching with Map and MapWhen",
                Slug = "aspnet-core-servicecollection-mapwhen",
                SummaryVi = "Kỹ thuật rẽ nhánh luồng xử lý theo URL hoặc điều kiện đặc biệt với MapWhen(), và đóng gói các nhóm dịch vụ mở rộng (IServiceCollection extensions).",
                SummaryEn = "Branching pipeline workflows conditionally with MapWhen(), and factoring clean IServiceCollection extension methods for service clusters.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "MapWhen", "Pipeline Branching", "IServiceCollection" },
                SampleCode = @"app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments(""/admin""), adminApp => {
    adminApp.Use(async (ctx, next) => {
        // Kiểm tra quyền Admin trước khi đi tiếp
        await next();
    });
});",
                LearningOutcomesVi = new() { "Tách biệt luồng xử lý giữa các module khác nhau", "Giữ file Program.cs luôn tinh gọn và module hóa" },
                LearningOutcomesEn = new() { "Isolate pipeline behavior between subsystems", "Keep Program.cs maintainable and modular" }
            },
            new()
            {
                LessonNumber = 62,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Quản lý trạng thái phiên làm việc: Cookie và Session (ISession)",
                TitleEn = "State Management: Cookies and Sessions (ISession)",
                Slug = "aspnet-core-session-va-cookie",
                SummaryVi = "Bản chất Stateless của giao thức HTTP, cấu hình AddDistributedMemoryCache(), AddSession(), lưu trữ giỏ hàng hoặc thông tin tạm của user trong ISession.",
                SummaryEn = "Stateless HTTP fundamentals, configuring distributed memory caches and Session middleware, and persisting cart states in ISession.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "Session", "Cookie", "ISession", "State Management" },
                SampleCode = @"builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opts => opts.IdleTimeout = TimeSpan.FromMinutes(30));
app.UseSession();

// Trong controller:
HttpContext.Session.SetString(""UserCartId"", Guid.NewGuid().ToString());",
                LearningOutcomesVi = new() { "Hiểu cơ chế liên kết Cookie SessionId với bộ nhớ Server", "Biết khi nào nên dùng Session vs Database" },
                LearningOutcomesEn = new() { "Understand SessionId cookie correlation", "Decide between in-memory sessions vs persistent databases" }
            },
            new()
            {
                LessonNumber = 63,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Cấu hình ứng dụng linh hoạt: appsettings.json và Options Pattern (IOptions<T>)",
                TitleEn = "App Configuration: appsettings.json and Options Pattern (IOptions<T>)",
                Slug = "aspnet-core-configuration-options-pattern",
                SummaryVi = "Đọc file cấu hình đa môi trường (appsettings.Development.json vs Production), ánh xạ section cấu hình thành C# strongly-typed class với IOptions<T> và IOptionsSnapshot<T>.",
                SummaryEn = "Multi-environment config resolution, mapping JSON configuration blocks to strongly-typed classes via IOptions<T> and IOptionsSnapshot<T>.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "appsettings.json", "IOptions", "Configuration", "Environment" },
                SampleCode = @"// Định nghĩa class
public class SmtpSettings { public string Host { get; set; } = """"; }

// Trong Program.cs
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(""Smtp""));",
                LearningOutcomesVi = new() { "Tránh hardcode thông số hệ thống trong code", "Tự động reload cấu hình khi file json thay đổi mà không cần restart app" },
                LearningOutcomesEn = new() { "Eliminate hardcoded system constants", "Reload settings live using IOptionsSnapshot" }
            },
            new()
            {
                LessonNumber = 64,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Dịch vụ gửi Email tự động trong ASP.NET Core (MailKit / SmtpClient)",
                TitleEn = "Automated Email Delivery in ASP.NET Core (MailKit / SMTP)",
                Slug = "aspnet-core-gui-mail-tu-dong",
                SummaryVi = "Tích hợp dịch vụ gửi mail kích hoạt tài khoản hoặc đặt lại mật khẩu bằng thư viện MailKit / MimeKit, gửi HTML email qua SMTP server (Gmail, SendGrid, Amazon SES).",
                SummaryEn = "Authoring email dispatch services with modern MailKit / MimeKit, sending formatted HTML notifications via Gmail, SendGrid, or SES SMTP.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Email", "MailKit", "MimeKit", "SMTP" },
                SampleCode = @"public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}",
                LearningOutcomesVi = new() { "Xây dựng dịch vụ gửi email bất đồng bộ đáng tin cậy", "Sử dụng template HTML đẹp mắt cho email thông báo" },
                LearningOutcomesEn = new() { "Build reliable async email delivery services", "Author responsive HTML email notification templates" }
            },
            new()
            {
                LessonNumber = 65,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Tích hợp Gulp.js để tự động biên dịch SASS/SCSS sang CSS trong Visual Studio",
                TitleEn = "Asset Automation: Compiling SASS/SCSS with Gulp.js",
                Slug = "aspnet-core-gulp-sass-scss-build",
                SummaryVi = "Cấu hình package.json, gulpfile.js, tự động theo dõi (watch) và biên dịch các file SCSS thành CSS nén minify khi lập trình giao diện Web.",
                SummaryEn = "Configuring package.json, gulpfile.js, asset pipelines, and auto-watching SCSS transformations to minified CSS stylesheets.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "Gulp", "SCSS", "SASS", "Asset Pipeline" },
                SampleCode = @"// gulpfile.js ví dụ
const gulp = require('gulp');
const sass = require('gulp-sass')(require('sass'));

gulp.task('sass', function() {
  return gulp.src('Styles/*.scss').pipe(sass()).pipe(gulp.dest('wwwroot/css'));
});",
                LearningOutcomesVi = new() { "Tự động hóa build các tài nguyên front-end trong dự án .NET", "Hiểu cách tổ chức file SCSS theo module" },
                LearningOutcomesEn = new() { "Automate client asset builds inside .NET projects", "Modularize style declarations with SASS" }
            },
            new()
            {
                LessonNumber = 66,
                SectionNumber = 5,
                SectionTitleVi = s5.TitleVi,
                SectionTitleEn = s5.TitleEn,
                SectionBadgeColor = s5.BadgeColor,
                SectionIcon = s5.Icon,
                TitleVi = "Quản lý thư viện Client-side với Microsoft LibMan (Library Manager)",
                TitleEn = "Client-Side Library Management with Microsoft LibMan",
                Slug = "aspnet-core-libman-quan-ly-thu-vien",
                SummaryVi = "Thay thế Bower/NPM cồng kềnh bằng công cụ LibMan nhẹ nhàng của Microsoft: tải Bootstrap, Font Awesome, jQuery trực tiếp vào thư mục wwwroot qua libman.json.",
                SummaryEn = "Lightweight asset management with LibMan: downloading Bootstrap, Font Awesome, and jQuery into wwwroot via libman.json without Node.js bloat.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 15,
                KeyTags = new() { "LibMan", "Bootstrap", "wwwroot", "Client Libraries" },
                SampleCode = @"<!-- libman.json -->
{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""bootstrap@5.3.3"",
      ""destination"": ""wwwroot/lib/bootstrap/""
    }
  ]
}",
                LearningOutcomesVi = new() { "Quản lý version của các thư viện CSS/JS độc lập", "Không cần cài đặt Node.js vẫn có thể cập nhật thư viện web" },
                LearningOutcomesEn = new() { "Version-control front-end CSS/JS libraries cleanly", "Fetch modern UI libraries without full Node.js installations" }
            }
        });
        sections.Add(s5);

        // ----------------------------------------------------
        // PHẦN 6: ASP.NET RAZOR PAGES & IDENTITY (Bài 67 -> Bài 89)
        // ----------------------------------------------------
        var s6 = new CurriculumSectionViewModel
        {
            SectionNumber = 6,
            TitleVi = "Phần 6 - ASP.NET Razor Pages & Identity",
            TitleEn = "Part 6 - Razor Pages & Identity Security",
            DescriptionVi = "Xây dựng ứng dụng hướng trang với Razor Pages, Cú pháp Razor, Layout, Partial, ViewComponent, TagHelper, Model Binding, Validation và hệ thống bảo mật Identity toàn diện.",
            DescriptionEn = "Page-focused web development with Razor Pages, Razor Syntax, Layouts, Partials, ViewComponents, TagHelpers, Model Binding, Validation, and end-to-end Identity authentication & authorization.",
            BadgeColor = "secondary",
            Icon = "bi-file-earmark-code",
            Level = DifficultyLevel.Intermediate,
            TotalEstimatedHours = 35
        };

        s6.Lessons.AddRange(new List<CurriculumLessonViewModel>
        {
            new()
            {
                LessonNumber = 67,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Khởi tạo dự án Razor Pages và Cơ chế định tuyến Route",
                TitleEn = "Razor Pages Initialization and File-Based Routing",
                Slug = "aspnet-razor-khoi-tao-va-route",
                SummaryVi = "Quy ước thư mục /Pages, directive @page, định tuyến tự động theo tên file và tùy biến đường dẫn URL (@page \"/bai-viet/{id:int}\").",
                SummaryEn = "The /Pages directory convention, the @page directive, file-based routing, and custom route templates (@page \"/articles/{id:int}\").",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Razor Pages", "@page", "Routing", "URL Template" },
                SampleCode = @"@page ""/khoa-hoc/{slug}""
@model CourseDetailModel
<h1>Khóa học: @Model.CourseSlug</h1>",
                LearningOutcomesVi = new() { "Hiểu sự đơn giản của kiến trúc hướng trang (Page-focused)", "Thêm route constraint kiểm tra kiểu dữ liệu tham số trên URL" },
                LearningOutcomesEn = new() { "Understand page-centric web architectures", "Add route parameter constraints on URL paths" }
            },
            new()
            {
                LessonNumber = 68,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Cú pháp Razor: @code, biểu thức điều kiện @if, vòng lặp @foreach",
                TitleEn = "Razor Syntax: @code, Conditionals (@if), and Loops (@foreach)",
                Slug = "cu-phap-trong-trang-razor-page",
                SummaryVi = "Kết hợp C# và HTML mượt mà bằng ký tự @, phân biệt mã code khối @{ ... } vs biểu thức @(...), cơ chế tự động HTML encode chống tấn công XSS.",
                SummaryEn = "Seamless blend of C# and HTML via the @ token, code blocks @{ ... } vs expressions @(...), and automatic HTML encoding protecting against XSS.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Razor Syntax", "@if", "@foreach", "XSS Protection" },
                SampleCode = @"@if (Model.IsAdmin)
{
    <span class=""badge bg-danger"">Quản trị viên</span>
}
<ul>
@foreach (var item in Model.Items)
{
    <li>@item</li>
}
</ul>",
                LearningOutcomesVi = new() { "Viết template giao diện động linh hoạt", "Tránh hiển thị dữ liệu chưa qua encode với @Html.Raw() trừ khi tuyệt đối an toàn" },
                LearningOutcomesEn = new() { "Author dynamic template views", "Avoid unsafe @Html.Raw() usage without sanitization" }
            },
            new()
            {
                LessonNumber = 69,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Xây dựng khung giao diện đồng nhất với Layout trong ASP.NET Core",
                TitleEn = "Master Layouts and View Hierarchy in ASP.NET Core",
                Slug = "aspnet-razor-layout-va-renderbody",
                SummaryVi = "Tạo khung chung Header/Footer trong _Layout.cshtml, chỉ định layout mặc định trong _ViewStart.cshtml, nhập namespace chung trong _ViewImports.cshtml, RenderBody() và RenderSection().",
                SummaryEn = "Shared Master Layouts (_Layout.cshtml), _ViewStart defaulting, _ViewImports global namespaces, RenderBody(), and conditional RenderSectionAsync().",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 20,
                KeyTags = new() { "Layout", "_Layout.cshtml", "RenderBody", "RenderSection" },
                SampleCode = @"<!-- Trong _Layout.cshtml -->
<header>...</header>
<main class=""container"">
    @RenderBody()
</main>
@await RenderSectionAsync(""Scripts"", required: false)",
                LearningOutcomesVi = new() { "Tái sử dụng toàn bộ khung HTML trang web", "Nhúng các đoạn mã JavaScript riêng cho từng trang con qua section" },
                LearningOutcomesEn = new() { "Reuse identical master layouts across all pages", "Inject page-specific JavaScript via sections" }
            },
            new()
            {
                LessonNumber = 70,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Tái sử dụng giao diện với Partial Pages & Partial Views",
                TitleEn = "Reusing UI Snippets with Partial Pages and Partial Views",
                Slug = "aspnet-razor-partial-page-partial-view",
                SummaryVi = "Chia nhỏ các khối HTML lặp lại (thẻ khóa học, phân trang, hộp thoại xác nhận) thành các tập tin _Partial.cshtml và nhúng bằng thẻ <partial name=\"...\" model=\"...\" />.",
                SummaryEn = "Decomposing repeating UI blocks (cards, pagination, dialogs) into _Partial.cshtml files and rendering via <partial name=\"...\" model=\"...\" />.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 15,
                KeyTags = new() { "Partial View", "<partial>", "UI Component", "DRY" },
                SampleCode = @"<partial name=""_ProductCard"" model=""item"" />",
                LearningOutcomesVi = new() { "Duy trì nguyên tắc DRY trong thiết kế giao diện", "Dễ dàng sửa đổi một thành phần UI tại một file duy nhất" },
                LearningOutcomesEn = new() { "Maintain DRY principles in HTML UI design", "Refactor shared widgets in a single isolated template" }
            },
            new()
            {
                LessonNumber = 71,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Đóng gói logic giao diện độc lập với ViewComponent",
                TitleEn = "Self-Contained UI Modules with ViewComponents",
                Slug = "aspnet-razor-viewcomponent",
                SummaryVi = "Khắc phục hạn chế của Partial View bằng ViewComponent: vừa có lớp C# xử lý truy vấn dữ liệu độc lập (InvokeAsync), vừa có file .cshtml hiển thị, hoàn hảo cho Giỏ hàng mini hay Menu động.",
                SummaryEn = "Supercharging Partials with ViewComponents: independent C# controller-like query execution (InvokeAsync) combined with isolated template views, ideal for mini-carts and dynamic menus.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "ViewComponent", "InvokeAsync", "Component", "Modular" },
                SampleCode = @"public class CategoryMenuViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cats = await db.Categories.ToListAsync();
        return View(cats);
    }
}",
                LearningOutcomesVi = new() { "Tạo các widget giao diện có khả năng tự truy vấn dữ liệu mà không phụ thuộc Controller cha", "Gọi ViewComponent bằng cú pháp <vc:category-menu />" },
                LearningOutcomesEn = new() { "Build self-fetching widgets decoupled from parent controllers", "Render components cleanly using <vc:... /> tag helpers" }
            },
            new()
            {
                LessonNumber = 72,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Tag Helpers tích hợp và Tự viết Custom Tag Helper",
                TitleEn = "Built-in Tag Helpers and Authoring Custom Tag Helpers",
                Slug = "aspnet-razor-taghelper-custom",
                SummaryVi = "Sức mạnh của cú pháp HTML-friendly Tag Helpers (asp-controller, asp-action, asp-for, asp-route-id), và tự viết một Custom Tag Helper kế thừa từ TagHelper.",
                SummaryEn = "The HTML-first ergonomics of built-in Tag Helpers (asp-controller, asp-for, asp-route), and authoring custom TagHelper classes inheriting from TagHelper.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "TagHelper", "asp-for", "asp-route", "Custom TagHelper" },
                SampleCode = @"[HtmlTargetElement(""email-link"")]
public class EmailLinkTagHelper : TagHelper
{
    public string Address { get; set; } = """";
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = ""a"";
        output.Attributes.SetAttribute(""href"", $""mailto:{Address}"");
    }
}",
                LearningOutcomesVi = new() { "Thay thế hoàn toàn cú pháp xấu xí @Html.ActionLink", "Viết các thẻ HTML tùy biến cho dự án của mình" },
                LearningOutcomesEn = new() { "Replace clunky @Html.ActionLink helpers with HTML-first tags", "Author project-specific domain tag helpers" }
            },
            new()
            {
                LessonNumber = 73,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Sử dụng HtmlHelper để sinh các phần tử HTML Form",
                TitleEn = "HtmlHelper API for Form Elements and Displays",
                Slug = "aspnet-razor-lop-htmlhelper",
                SummaryVi = "Tìm hiểu các phương thức HtmlHelper truyền thống: Html.TextBoxFor, Html.DropDownListFor, Html.ValidationMessageFor, và so sánh với Tag Helpers hiện đại.",
                SummaryEn = "Classic HtmlHelper methods (TextBoxFor, DropDownListFor, ValidationMessageFor), and comparative analysis with modern Tag Helpers.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 15,
                KeyTags = new() { "HtmlHelper", "TextBoxFor", "DropDownListFor" },
                SampleCode = @"@Html.DropDownListFor(m => m.CategoryId, Model.CategoryList, ""-- Chọn danh mục --"", new { @class = ""form-select"" })",
                LearningOutcomesVi = new() { "Đọc hiểu các dự án cũ sử dụng HtmlHelper", "Hiểu tại sao ASP.NET Core khuyến nghị chuyển sang Tag Helper" },
                LearningOutcomesEn = new() { "Read and maintain legacy codebases relying on HtmlHelper", "Understand the benefits of moving to Tag Helpers" }
            },
            new()
            {
                LessonNumber = 74,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "PageModel Code-Behind và các Handler OnGet, OnPost",
                TitleEn = "PageModel Architecture and Handler Methods (OnGet, OnPost)",
                Slug = "aspnet-razor-pagemodel-onget-onpost",
                SummaryVi = "Mô hình Code-Behind với lớp kế thừa PageModel, phân định rõ ràng giữa giao diện .cshtml và logic xử lý .cshtml.cs, các handler OnGetAsync(), OnPostAsync() và Named Handlers (asp-page-handler).",
                SummaryEn = "Code-Behind patterns with PageModel subclasses, separation of markup vs behavior, standard OnGetAsync/OnPostAsync handlers, and Named Handlers (asp-page-handler).",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "PageModel", "OnGet", "OnPost", "Named Handlers" },
                SampleCode = @"public class EditModel : PageModel
{
    public void OnGet(int id) { /* Load data */ }
    public async Task<IActionResult> OnPostAsync() { /* Save data */ return RedirectToPage(""Index""); }
}",
                LearningOutcomesVi = new() { "Tổ chức code một trang độc lập khép kín (Cohesion cao)", "Xử lý nhiều form trên cùng một trang với Named Handlers" },
                LearningOutcomesEn = new() { "Structure high-cohesion single-page components", "Handle multiple form actions via Named Handlers" }
            },
            new()
            {
                LessonNumber = 75,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Cơ chế ánh xạ dữ liệu Model Binding tự động",
                TitleEn = "Automated Model Binding Mechanisms",
                Slug = "aspnet-razor-model-binding-tu-dong",
                SummaryVi = "Thuộc tính [BindProperty], thứ tự tìm kiếm nguồn dữ liệu (Form -> Route Data -> Query String), [FromQuery], [FromRoute], [FromHeader], [FromBody] và custom model binders.",
                SummaryEn = "[BindProperty] mechanics, value provider evaluation sequence (Form, Route, QueryString, Body), and explicit source attributes ([FromQuery], [FromRoute]).",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "Model Binding", "[BindProperty]", "FromQuery", "FromRoute" },
                SampleCode = @"[BindProperty]
public UserRegisterDto Input { get; set; } = new();",
                LearningOutcomesVi = new() { "Không cần trích xuất thủ công Request.Form[\"...\"]", "Ánh xạ trực tiếp dữ liệu người dùng submit vào Object strongly-typed" },
                LearningOutcomesEn = new() { "Eliminate tedious manual Request.Form parsing", "Map inbound form data directly to strongly-typed objects" }
            },
            new()
            {
                LessonNumber = 76,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "HTML Form và Kiểm tra tính hợp lệ dữ liệu (Validation & ModelState)",
                TitleEn = "HTML Forms, Data Validation, and ModelState",
                Slug = "aspnet-razor-html-form-va-validation",
                SummaryVi = "Kiểm tra tính hợp lệ dữ liệu Server-side với ModelState.IsValid, hiển thị thông báo lỗi asp-validation-for, và tích hợp Client-side validation với jQuery Validation Unobtrusive.",
                SummaryEn = "Server-side data verification using ModelState.IsValid, error rendering with asp-validation-for, and instant client-side validation via Unobtrusive JS.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "Validation", "ModelState.IsValid", "asp-validation-for", "Client-side" },
                SampleCode = @"if (!ModelState.IsValid)
{
    return Page(); // Trả về trang kèm thông báo lỗi
}
// Xử lý lưu dữ liệu...",
                LearningOutcomesVi = new() { "Luôn kiểm tra ModelState ở Server trước khi lưu vào CSDL", "Tăng trải nghiệm người dùng với báo lỗi tức thời ở trình duyệt" },
                LearningOutcomesEn = new() { "Always enforce server-side ModelState guarantees", "Enhance UX with instantaneous client-side validation feedback" }
            },
            new()
            {
                LessonNumber = 77,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Tiếp nhận và Lưu trữ File Upload với giao diện IFormFile",
                TitleEn = "Receiving and Storing Uploaded Files with IFormFile",
                Slug = "aspnet-razor-upload-file-iformfile",
                SummaryVi = "Cấu hình form enctype=\"multipart/form-data\", tiếp nhận IFormFile, kiểm tra định dạng đuôi file cho phép, giới hạn dung lượng tải lên, và lưu vào ổ cứng với CopyToAsync().",
                SummaryEn = "Configuring enctype=\"multipart/form-data\", receiving IFormFile, validating extensions and file signature headers, size caps, and async saving with CopyToAsync().",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "IFormFile", "File Upload", "Security", "CopyToAsync" },
                SampleCode = @"[BindProperty]
public IFormFile? UploadedFile { get; set; }

if (UploadedFile != null)
{
    var path = Path.Combine(env.WebRootPath, ""uploads"", UploadedFile.FileName);
    using var stream = System.IO.File.Create(path);
    await UploadedFile.CopyToAsync(stream);
}",
                LearningOutcomesVi = new() { "Phòng chống các lỗ hổng tải lên file mã độc (.exe, .php, .dll)", "Đặt tên file ngẫu nhiên bằng Guid để tránh ghi đè" },
                LearningOutcomesEn = new() { "Protect against malicious executable file uploads", "Sanitize and randomize stored file names via GUIDs" }
            },
            new()
            {
                LessonNumber = 78,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Tích hợp Entity Framework Core thực hiện CRUD trong Razor Pages",
                TitleEn = "Integrating EF Core for Complete CRUD in Razor Pages",
                Slug = "aspnet-razor-crud-voi-ef-core",
                SummaryVi = "Xây dựng trọn bộ 5 trang quản lý thực thể: Danh sách (Index), Xem chi tiết (Details), Tạo mới (Create), Chỉnh sửa (Edit) và Xóa (Delete) liên kết CSDL qua EF Core.",
                SummaryEn = "Building an end-to-end entity management suite: List (Index), Details, Create, Edit, and Delete linked to SQL Server via EF Core.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "CRUD", "EF Core", "Razor Pages", "Database" },
                SampleCode = @"public async Task<IActionResult> OnPostDeleteAsync(int id)
{
    var item = await db.Products.FindAsync(id);
    if (item != null) { db.Products.Remove(item); await db.SaveChangesAsync(); }
    return RedirectToPage(""Index"");
}",
                LearningOutcomesVi = new() { "Tự tay làm chủ quy trình CRUD chuẩn cho bất kỳ dự án nào", "Xử lý xung đột đồng thời Concurrency khi cập nhật dữ liệu" },
                LearningOutcomesEn = new() { "Master standard enterprise CRUD workflows", "Handle optimistic concurrency updates" }
            },
            new()
            {
                LessonNumber = 79,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Xây dựng giải thuật phân trang dữ liệu (Paging) với LINQ & Bootstrap",
                TitleEn = "Data Pagination Algorithm with LINQ and Bootstrap",
                Slug = "aspnet-razor-phan-trang-linq-bootstrap",
                SummaryVi = "Tính toán tổng số trang Math.Ceiling, sử dụng .Skip((page - 1) * pageSize).Take(pageSize), và xây dựng component giao diện phân trang đẹp mắt bằng Bootstrap 5.",
                SummaryEn = "Total pages calculation with Math.Ceiling, LINQ .Skip().Take() slicing, and responsive Bootstrap 5 pagination bar rendering.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Paging", "Pagination", "Skip/Take", "Bootstrap" },
                SampleCode = @"int pageSize = 10;
var items = await db.Articles
    .OrderByDescending(a => a.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();",
                LearningOutcomesVi = new() { "Tránh tải toàn bộ hàng triệu dòng dữ liệu vào RAM của web server", "Tạo thanh phân trang linh hoạt có nút Previous, Next và số trang" },
                LearningOutcomesEn = new() { "Avoid loading full tables into memory", "Author reusable pagination bars with Previous/Next controls" }
            },
            new()
            {
                LessonNumber = 80,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "Bảo mật danh tính ASP.NET Core Identity (1): Đăng ký, Đăng nhập và Đăng xuất",
                TitleEn = "ASP.NET Core Identity (1): Register, Login, and Logout",
                Slug = "aspnet-razor-identity-register-login-logout",
                MatchingTutorialSlug = "aspnet-core-identity-authentication-co-ban",
                SummaryVi = "Giới thiệu IdentityUser, UserManager<TUser>, SignInManager<TUser>, mã hóa mật khẩu PBKDF2 an toàn, cơ chế Cookie Authentication và thiết lập Remember Me.",
                SummaryEn = "IdentityUser entity, UserManager<TUser>, SignInManager<TUser>, PBKDF2 password hashing, secure auth cookies, and persistent Remember Me login.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "Identity", "UserManager", "SignInManager", "Authentication" },
                SampleCode = @"var result = await userManager.CreateAsync(user, password);
if (result.Succeeded)
{
    await signInManager.SignInAsync(user, isPersistent: false);
}",
                LearningOutcomesVi = new() { "Không bao giờ tự lưu mật khẩu dạng plaintext", "Làm chủ bộ API bảo mật chính thức của Microsoft" },
                LearningOutcomesEn = new() { "Never store plaintext credentials", "Master Microsoft's official security APIs" }
            },
            new()
            {
                LessonNumber = 81,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (2): Khóa tài khoản (Lockout) và Đặt lại mật khẩu (Reset Password)",
                TitleEn = "ASP.NET Core Identity (2): Account Lockout and Password Reset",
                Slug = "aspnet-razor-identity-lockout-reset-password",
                SummaryVi = "Chống tấn công Brute Force bằng tính năng tự động khóa tài khoản sau 5 lần nhập sai, tạo token an toàn GeneratePasswordResetTokenAsync và gửi link đổi pass qua email.",
                SummaryEn = "Brute-force protection via automatic lockout after failed attempts, cryptographically secure token generation, and email-based password reset flows.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "Lockout", "Password Reset", "Token", "Brute Force" },
                SampleCode = @"// Khóa 15 phút nếu đăng nhập sai 5 lần liên tiếp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.MaxFailedAccessAttempts = 5;",
                LearningOutcomesVi = new() { "Bảo vệ hệ thống trước các công cụ dò mật khẩu tự động", "Quy trình khôi phục mật khẩu bảo mật theo tiêu chuẩn OWASP" },
                LearningOutcomesEn = new() { "Defend systems against automated credential stuffing", "Implement OWASP-compliant password reset flows" }
            },
            new()
            {
                LessonNumber = 82,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (3): Đăng nhập một chạm với Google Authentication (OAuth 2.0)",
                TitleEn = "ASP.NET Core Identity (3): Google OAuth 2.0 Authentication",
                Slug = "aspnet-razor-identity-google-login",
                SummaryVi = "Cấu hình Google Cloud Console (OAuth Client ID & Client Secret), tích hợp package Microsoft.AspNetCore.Authentication.Google, và xử lý callback đăng nhập ngoài.",
                SummaryEn = "Google Cloud Console setup (Client ID & Secret), Microsoft.AspNetCore.Authentication.Google integration, and external login callback handling.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "Google Login", "OAuth 2.0", "External Auth" },
                SampleCode = @"builder.Services.AddAuthentication()
    .AddGoogle(opts => {
        opts.ClientId = builder.Configuration[""Authentication:Google:ClientId""]!;
        opts.ClientSecret = builder.Configuration[""Authentication:Google:ClientSecret""]!;
    });",
                LearningOutcomesVi = new() { "Cho phép người dùng đăng nhập không cần nhớ mật khẩu mới", "Liên kết nhiều tài khoản mạng xã hội vào cùng một hồ sơ người dùng" },
                LearningOutcomesEn = new() { "Enable seamless passwordless logins for users", "Link multiple external providers to a unified user record" }
            },
            new()
            {
                LessonNumber = 83,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (4): Đăng nhập bằng tài khoản mạng xã hội Facebook",
                TitleEn = "ASP.NET Core Identity (4): Facebook Social Login Integration",
                Slug = "aspnet-razor-identity-facebook-login",
                SummaryVi = "Đăng ký ứng dụng Meta for Developers, cấu hình App ID và App Secret, tích hợp package Microsoft.AspNetCore.Authentication.Facebook và xử lý Claim từ Facebook.",
                SummaryEn = "Meta for Developers app configuration, App ID and App Secret wiring, Microsoft.AspNetCore.Authentication.Facebook integration, and profile claims extraction.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Facebook Login", "Meta Developers", "OAuth", "Claims" },
                SampleCode = @"builder.Services.AddAuthentication()
    .AddFacebook(opts => {
        opts.AppId = builder.Configuration[""Authentication:Facebook:AppId""]!;
        opts.AppSecret = builder.Configuration[""Authentication:Facebook:AppSecret""]!;
    });",
                LearningOutcomesVi = new() { "Mở rộng tùy chọn đăng nhập cho tệp người dùng Facebook", "Đọc thông tin email, họ tên, avatar từ tài khoản xã hội" },
                LearningOutcomesEn = new() { "Broaden login choices for consumers", "Extract name and avatar claims from Facebook profiles" }
            },
            new()
            {
                LessonNumber = 84,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (5): Các trang quản lý tài khoản cá nhân, Đổi mật khẩu, Email",
                TitleEn = "ASP.NET Core Identity (5): Profile Management, Email & Password Change",
                Slug = "aspnet-razor-identity-trang-quan-ly-profile",
                SummaryVi = "Mở rộng bảng User với các thuộc tính tùy biến (FullName, AvatarUrl, Bio), xây dựng các trang Profile cập nhật thông tin cá nhân, đổi mật khẩu và quản lý đăng nhập.",
                SummaryEn = "Extending ApplicationUser with custom columns (FullName, AvatarUrl, Bio), building profile update forms, password changing, and session management.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "ApplicationUser", "Profile", "Change Password", "Identity" },
                SampleCode = @"public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = """";
    public string? AvatarUrl { get; set; }
}",
                LearningOutcomesVi = new() { "Tùy biến bảng User theo đúng nghiệp vụ của dự án mà không phá vỡ Identity", "Bảo vệ thông tin cá nhân của thành viên" },
                LearningOutcomesEn = new() { "Extend Identity tables safely without breaking internal schemas", "Safeguard user profile mutations" }
            },
            new()
            {
                LessonNumber = 85,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (6): Quản trị Vai trò (Role Management) và Gán vai trò cho User",
                TitleEn = "ASP.NET Core Identity (6): Role Management and User Role Assignment",
                Slug = "aspnet-razor-identity-quan-ly-role",
                SummaryVi = "Sử dụng IdentityRole và RoleManager<IdentityRole>, tạo các vai trò (Admin, Manager, Student), gán vai trò vào người dùng với userManager.AddToRoleAsync().",
                SummaryEn = "Managing IdentityRole with RoleManager<IdentityRole>, seeding system roles (Admin, Manager, Student), and assigning roles via userManager.AddToRoleAsync().",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Roles", "RoleManager", "AddToRoleAsync", "RBAC" },
                SampleCode = @"if (!await roleManager.RoleExistsAsync(""Admin""))
{
    await roleManager.CreateAsync(new IdentityRole(""Admin""));
}
await userManager.AddToRoleAsync(user, ""Admin"");",
                LearningOutcomesVi = new() { "Xây dựng hệ thống phân quyền nhiều cấp", "Tự động Seed các quyền hạn mặc định khi ứng dụng khởi động" },
                LearningOutcomesEn = new() { "Implement multi-tier role hierarchies", "Seed default roles reliably on startup" }
            },
            new()
            {
                LessonNumber = 86,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (7): Phân quyền truy cập theo vai trò với [Authorize(Roles = \"...\")]",
                TitleEn = "ASP.NET Core Identity (7): Role-Based Authorization ([Authorize])",
                Slug = "aspnet-razor-identity-role-based-authorization",
                MatchingTutorialSlug = "authorization-phan-quyen-role-claim-policy-trong-aspnet-core",
                SummaryVi = "Bảo vệ trang web và Controller với thuộc tính [Authorize], chặn người dùng chưa đăng nhập, và giới hạn chỉ cho phép các Role cụ thể ([Authorize(Roles = \"Admin\")]).",
                SummaryEn = "Securing endpoints and pages using the [Authorize] attribute, redirecting unauthenticated visitors, and enforcing role boundaries ([Authorize(Roles = \"Admin\")]).",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 20,
                KeyTags = new() { "[Authorize]", "Roles", "Access Denied", "RBAC" },
                SampleCode = @"[Authorize(Roles = ""Admin,Manager"")]
public class AdminDashboardModel : PageModel
{
    // Chỉ có Admin hoặc Manager mới vào được!
}",
                LearningOutcomesVi = new() { "Ngăn chặn các truy cập trái phép vào các trang quản trị", "Tùy biến trang AccessDenied thông báo từ chối truy cập thân thiện" },
                LearningOutcomesEn = new() { "Shield administrative endpoints from privilege escalation", "Configure user-friendly AccessDenied feedback pages" }
            },
            new()
            {
                LessonNumber = 87,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (8): Phân quyền nâng cao theo Claim và Policy",
                TitleEn = "ASP.NET Core Identity (8): Claims-Based and Policy-Based Authorization",
                Slug = "aspnet-razor-identity-claim-va-policy",
                SummaryVi = "Vượt qua giới hạn của Role với Claim (họ tên, ngày sinh, phòng ban), định nghĩa Policy trong Program.cs bằng AddPolicy, và áp dụng [Authorize(Policy = \"...\")].",
                SummaryEn = "Surpassing role rigidity with Claims (Department, Badges, Level), registering security Policies via AddPolicy, and applying [Authorize(Policy = \"...\")].",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 30,
                KeyTags = new() { "Claims", "Policy", "AddPolicy", "Authorization" },
                SampleCode = @"builder.Services.AddAuthorization(opts => {
    opts.AddPolicy(""CanEditTutorial"", p => p.RequireClaim(""Permission"", ""Tutorial.Edit""));
});",
                LearningOutcomesVi = new() { "Xây dựng hệ thống phân quyền chi tiết (Fine-grained Permissions)", "Tránh sự bùng nổ quá nhiều Role phức tạp" },
                LearningOutcomesEn = new() { "Design fine-grained permission control systems", "Avoid role explosion antipatterns" }
            },
            new()
            {
                LessonNumber = 88,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (9): Viết Custom Authorization Handler và Requirement",
                TitleEn = "ASP.NET Core Identity (9): Custom Authorization Handlers & Requirements",
                Slug = "aspnet-razor-identity-custom-authorization-handler",
                SummaryVi = "Xử lý các điều kiện nghiệp vụ phức tạp (ví dụ: chỉ cho phép chỉnh sửa bài viết nếu người dùng chính là tác giả bài viết đó) bằng AuthorizationHandler<TRequirement, TResource>.",
                SummaryEn = "Resource-based authorization for domain logic (e.g., users can only edit articles they authored) using AuthorizationHandler<TRequirement, TResource>.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 35,
                KeyTags = new() { "AuthorizationHandler", "IAuthorizationRequirement", "Resource-based Auth" },
                SampleCode = @"public class DocumentOwnerHandler : AuthorizationHandler<OwnerRequirement, Article>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext ctx, OwnerRequirement req, Article doc)
    {
        if (ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) == doc.AuthorId)
            ctx.Succeed(req);
        return Task.CompletedTask;
    }
}",
                LearningOutcomesVi = new() { "Bảo vệ tài nguyên theo ngữ cảnh sở hữu (Resource-based Authorization)", "Đạt chứng chỉ kiến trúc bảo mật cấp doanh nghiệp" },
                LearningOutcomesEn = new() { "Enforce contextual resource-level ownership constraints", "Achieve enterprise-grade authorization patterns" }
            },
            new()
            {
                LessonNumber = 89,
                SectionNumber = 6,
                SectionTitleVi = s6.TitleVi,
                SectionTitleEn = s6.TitleEn,
                SectionBadgeColor = s6.BadgeColor,
                SectionIcon = s6.Icon,
                TitleVi = "ASP.NET Core Identity (10): Sử dụng IAuthorizationService kiểm tra quyền trong code",
                TitleEn = "ASP.NET Core Identity (10): Dynamic In-Code Checks via IAuthorizationService",
                Slug = "aspnet-razor-identity-iauthorizationservice",
                SummaryVi = "Kiểm tra quyền hạn linh hoạt ngay trong code C# hoặc file giao diện .cshtml bằng IAuthorizationService.AuthorizeAsync(User, resource, \"PolicyName\"), ẩn hiện nút bấm theo quyền.",
                SummaryEn = "Dynamic runtime authorization evaluation in C# controllers or inside .cshtml Razor views using IAuthorizationService.AuthorizeAsync to conditionally render UI actions.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 25,
                KeyTags = new() { "IAuthorizationService", "AuthorizeAsync", "Conditional UI" },
                SampleCode = @"@inject IAuthorizationService AuthService
@if ((await AuthService.AuthorizeAsync(User, Model.Post, ""CanEditPost"")).Succeeded)
{
    <button class=""btn btn-primary"">Chỉnh sửa bài</button>
}",
                LearningOutcomesVi = new() { "Ẩn hiện các nút chức năng (Sửa, Xóa) chính xác theo quyền của người xem", "Bảo đảm đồng bộ 100% giữa giao diện và kiểm tra nghiệp vụ backend" },
                LearningOutcomesEn = new() { "Conditionally toggle action buttons based on live user claims", "Guarantee synchronization between frontend UI and backend security gates" }
            }
        });
        sections.Add(s6);

        // ----------------------------------------------------
        // PHẦN 7: ASP.NET CORE MVC & DỰ ÁN THỰC TẾ (Bài 90 -> Bài 101)
        // ----------------------------------------------------
        var s7 = new CurriculumSectionViewModel
        {
            SectionNumber = 7,
            TitleVi = "Phần 7 - ASP.NET MVC & Xây dựng Website Hoàn chỉnh",
            TitleEn = "Part 7 - ASP.NET MVC & Full-Stack Projects",
            DescriptionVi = "Xây dựng ứng dụng hoàn chỉnh với mô hình MVC, Routing chuyên sâu, Editor Summernote, Giỏ hàng Session, Quản lý file elFinder, SB Admin 2 và Triển khai lên Linux Server.",
            DescriptionEn = "Build full-fledged applications with MVC architecture, deep Routing, Summernote WYSIWYG, Session-based Cart, elFinder file manager, SB Admin 2 dashboard, and Linux production publishing.",
            BadgeColor = "success",
            Icon = "bi-boxes",
            Level = DifficultyLevel.Advanced,
            TotalEstimatedHours = 35
        };

        s7.Lessons.AddRange(new List<CurriculumLessonViewModel>
        {
            new()
            {
                LessonNumber = 90,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "ASP.NET Core MVC: Mô hình Controller & View đầu tiên",
                TitleEn = "ASP.NET Core MVC: Controllers and Views Fundamentals",
                Slug = "aspnet-core-mvc-controller-view-co-ban",
                MatchingTutorialSlug = "aspnet-core-mvc-controllers-views-viewmodels",
                SummaryVi = "Mô hình kiến trúc Model - View - Controller, kế thừa Microsoft.AspNetCore.Mvc.Controller, trả về IActionResult (View, Content, Json), truyền dữ liệu với ViewData, ViewBag và ViewModel.",
                SummaryEn = "The Model-View-Controller pattern, subclassing Controller, returning IActionResult types (View, Content, Json), and passing state via ViewData, ViewBag, and strongly-typed ViewModels.",
                Difficulty = DifficultyLevel.Beginner,
                EstimatedMinutes = 25,
                KeyTags = new() { "MVC", "Controller", "View", "ViewModel", "IActionResult" },
                SampleCode = @"public class HomeController : Controller
{
    public IActionResult Index()
    {
        var model = new HomeViewModel { Welcome = ""Chào mừng đến với MVC!"" };
        return View(model);
    }
}",
                LearningOutcomesVi = new() { "Hiểu vai trò điều phối của Controller", "Ưu tiên sử dụng strongly-typed ViewModel thay vì ViewBag yếu kiểu" },
                LearningOutcomesEn = new() { "Understand Controller mediation roles", "Prefer strongly-typed ViewModels over untyped ViewBag" }
            },
            new()
            {
                LessonNumber = 91,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Định tuyến chuyên sâu trong ASP.NET MVC (Convention vs Attribute Routing)",
                TitleEn = "Advanced Routing in ASP.NET MVC: Convention vs Attribute Routing",
                Slug = "aspnet-core-mvc-routing-chuyen-sau",
                SummaryVi = "So sánh định tuyến quy ước chung app.MapControllerRoute() vs định tuyến theo thuộc tính [Route], [HttpGet], định nghĩa URL thân thiện SEO (Friendly Slugs).",
                SummaryEn = "Comparing conventional routes (app.MapControllerRoute) vs attribute routes ([Route], [HttpGet]), custom token parameters, and SEO-friendly slug routes.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "Routing", "Attribute Routing", "SEO Slugs", "MapControllerRoute" },
                SampleCode = @"[Route(""san-pham"")]
public class ProductController : Controller
{
    [HttpGet(""{slug}-{id:int}.html"")]
    public IActionResult Details(string slug, int id) => View();
}",
                LearningOutcomesVi = new() { "Thiết kế URL chuẩn SEO chuẩn Google", "Sử dụng Route Constraints để bắt đúng kiểu dữ liệu" },
                LearningOutcomesEn = new() { "Design clean SEO-friendly URLs", "Enforce URL path parameters using Route Constraints" }
            },
            new()
            {
                LessonNumber = 92,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Tích hợp Entity Framework Core và Identity vào mô hình MVC",
                TitleEn = "Integrating EF Core and Identity into the MVC Pattern",
                Slug = "aspnet-core-mvc-tich-hop-ef-identity",
                SummaryVi = "Inject DbContext và UserManager trực tiếp vào constructor của Controller, truy vấn danh sách dữ liệu và bảo vệ Action bằng [Authorize].",
                SummaryEn = "Constructor-injecting DbContext and UserManager into MVC controllers, querying database entities, and locking down sensitive actions via [Authorize].",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "EF Core", "Identity", "Dependency Injection", "MVC" },
                SampleCode = @"public class BlogController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Articles.ToListAsync());
}",
                LearningOutcomesVi = new() { "Kết hợp sức mạnh giữa ORM, Quản lý tài khoản và mô hình MVC", "Viết code sạch và dễ kiểm thử với DI" },
                LearningOutcomesEn = new() { "Harmonize ORM, Identity, and MVC paradigms", "Write testable decoupled controller code with DI" }
            },
            new()
            {
                LessonNumber = 93,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Model Binding, Validation và Phòng chống tấn công CSRF ([ValidateAntiForgeryToken])",
                TitleEn = "Model Binding, Validation, and CSRF Protection ([ValidateAntiForgeryToken])",
                Slug = "aspnet-core-mvc-binding-validation-csrf",
                SummaryVi = "Tiếp nhận dữ liệu form submit trong Action Post, kiểm tra ModelState.IsValid, và bảo vệ form chống giả mạo request với thuộc tính [ValidateAntiForgeryToken].",
                SummaryEn = "Form submit binding in POST actions, ModelState validation verification, and defending against Cross-Site Request Forgery via [ValidateAntiForgeryToken].",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 25,
                KeyTags = new() { "CSRF", "ValidateAntiForgeryToken", "Model Binding", "Security" },
                SampleCode = @"[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductCreateViewModel model)
{
    if (!ModelState.IsValid) return View(model);
    // Lưu sản phẩm an toàn...
    return RedirectToAction(nameof(Index));
}",
                LearningOutcomesVi = new() { "Hiểu cơ chế tấn công CSRF và cách token chống giả mạo hoạt động", "Bảo vệ 100% các request làm thay đổi dữ liệu (POST, PUT, DELETE)" },
                LearningOutcomesEn = new() { "Understand CSRF attack mechanics and anti-forgery tokens", "Protect all state-mutating endpoints against CSRF" }
            },
            new()
            {
                LessonNumber = 94,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Xây dựng Website (1): Thiết kế CSDL và Module Quản lý Danh mục Blog",
                TitleEn = "Real-World Project (1): Database Design & Blog Category Module",
                Slug = "aspnet-core-mvc-du-an-danh-muc-blog",
                SummaryVi = "Bắt đầu dự án Website thực tế: tạo bảng Category cha-con (phân cấp cây danh mục), viết Controller và View quản lý danh mục bài viết blog.",
                SummaryEn = "Kicking off the comprehensive project: modeling hierarchical parent-child categories, building management controllers, and category management tree views.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "Real-World Project", "Category Tree", "Hierarchical Data", "Blog" },
                SampleCode = @"public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = """";
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}",
                LearningOutcomesVi = new() { "Mô hình hóa dữ liệu danh mục nhiều cấp chuẩn thực tế", "Xây dựng giao diện quản lý danh mục thân thiện" },
                LearningOutcomesEn = new() { "Model hierarchical multi-tier tree categories in EF Core", "Design category management interfaces" }
            },
            new()
            {
                LessonNumber = 95,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Xây dựng Website (2): Tích hợp trình soạn thảo WYSIWYG Summernote",
                TitleEn = "Real-World Project (2): Integrating Summernote WYSIWYG Editor",
                Slug = "aspnet-core-mvc-tich-hop-summernote",
                SummaryVi = "Nhúng trình soạn thảo bài viết phong phú Summernote vào thẻ textarea, xử lý upload chèn ảnh trực tiếp qua AJAX kéo thả và lưu trữ trên server.",
                SummaryEn = "Embedding the rich Summernote WYSIWYG editor on textareas, handling drag-and-drop AJAX inline image uploads, and storing assets safely on the server.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "Summernote", "WYSIWYG", "AJAX Image Upload", "Editor" },
                SampleCode = @"$('#Content').summernote({
    height: 300,
    callbacks: {
        onImageUpload: function(files) { uploadImageAjax(files[0]); }
    }
});",
                LearningOutcomesVi = new() { "Tạo trải nghiệm soạn thảo bài viết chuyên nghiệp như Word", "Xử lý AJAX upload file ảnh độc lập" },
                LearningOutcomesEn = new() { "Deliver professional WYSIWYG content authoring experiences", "Handle asynchronous image uploads cleanly" }
            },
            new()
            {
                LessonNumber = 96,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Xây dựng Website (3): Quản lý bài viết Blog, Gắn thẻ Tag và Lịch xuất bản",
                TitleEn = "Real-World Project (3): Blog Post Management, Tags, and Scheduling",
                Slug = "aspnet-core-mvc-quan-ly-bai-viet-post",
                SummaryVi = "Tạo module quản lý bài viết đầy đủ: Slug tự động tạo từ tiêu đề, quan hệ nhiều-nhiều với Tag bài viết, trạng thái nháp (Draft) hoặc xuất bản (Published).",
                SummaryEn = "Complete blog post management: automatic slugification from titles, many-to-many Tag assignments, and Draft vs Published workflow states.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "Blog Post", "Slug Generation", "Tags", "N-N" },
                SampleCode = @"string slug = Regex.Replace(title.ToLowerInvariant(), @""[^a-z0-9\s-]"", """");
slug = Regex.Replace(slug, @""\s+"", ""-"");",
                LearningOutcomesVi = new() { "Tự động sinh URL Slug chuẩn tiếng Việt có dấu chuyển thành không dấu", "Quản lý quan hệ nhiều-nhiều giữa Bài viết và Thẻ Tag" },
                LearningOutcomesEn = new() { "Generate Vietnamese diacritic-stripped URL slugs", "Manage many-to-many Post-to-Tag relationships" }
            },
            new()
            {
                LessonNumber = 97,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Xây dựng Website (4): Trang chủ Blog, Chi tiết bài viết, Tìm kiếm và Phân trang",
                TitleEn = "Real-World Project (4): Blog Homepage, Post Details, Search & Pagination",
                Slug = "aspnet-core-mvc-trang-chu-blog-tim-kiem",
                SummaryVi = "Hoàn thiện giao diện phía người dùng (Front-end): danh sách bài mới nhất, bộ lọc tìm kiếm theo từ khóa, lọc theo danh mục, trang chi tiết và bài viết liên quan.",
                SummaryEn = "Finishing customer-facing frontend experiences: latest article grids, search filters, category filters, detail views, and related articles.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 30,
                KeyTags = new() { "Search", "Blog Homepage", "Filtering", "Frontend" },
                SampleCode = @"var query = db.Articles.Include(a => a.Category).Where(a => a.IsPublished);
if (!string.IsNullOrEmpty(keyword))
    query = query.Where(a => a.Title.Contains(keyword) || a.Summary.Contains(keyword));",
                LearningOutcomesVi = new() { "Kết hợp tìm kiếm và phân trang mượt mà không mất tham số URL", "Tăng trải nghiệm đọc bài cho độc giả" },
                LearningOutcomesEn = new() { "Combine multi-criteria search and pagination statefully", "Provide polished reading experiences" }
            },
            new()
            {
                LessonNumber = 98,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Xây dựng Website (5): Module Giỏ hàng (Cart) lưu trữ bằng Session",
                TitleEn = "Real-World Project (5): Shopping Cart Module using Session Storage",
                Slug = "aspnet-core-mvc-gio-hang-cart-session",
                SummaryVi = "Lập trình giỏ hàng điện tử: Thêm sản phẩm vào giỏ, cập nhật số lượng, xóa sản phẩm, tính tổng tiền, lưu trữ giỏ hàng dạng JSON serialize vào ISession.",
                SummaryEn = "E-commerce shopping cart mechanics: Add to cart, update quantity, remove line items, calculate totals, and serialize cart objects to ISession.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "Shopping Cart", "Session", "E-commerce", "JSON Serialize" },
                SampleCode = @"public class CartItem { public int ProductId { get; set; } public int Quantity { get; set; } public decimal Price { get; set; } }

// Lưu vào Session
HttpContext.Session.SetString(""CART_KEY"", JsonSerializer.Serialize(cartItems));",
                LearningOutcomesVi = new() { "Xây dựng module bán hàng thương mại điện tử thực tế", "Xử lý Session an toàn khi khách chưa đăng nhập" },
                LearningOutcomesEn = new() { "Build production-ready e-commerce cart pipelines", "Handle guest shopping carts seamlessly" }
            },
            new()
            {
                LessonNumber = 99,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Tích hợp trình quản lý tệp tin mã nguồn mở elFinder vào ASP.NET Core",
                TitleEn = "Integrating elFinder Open-Source File Manager into ASP.NET Core",
                Slug = "aspnet-core-mvc-tich-hop-elfinder",
                SummaryVi = "Tích hợp elFinder (quản lý file dạng Windows Explorer trên trình duyệt web): duyệt thư mục, upload ảnh/video hàng loạt, đổi tên, xóa file trực tiếp trong admin.",
                SummaryEn = "Embedding elFinder (browser-based file explorer) in ASP.NET Core: folder management, bulk image uploads, renaming, and backend storage security.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 30,
                KeyTags = new() { "elFinder", "File Manager", "Connector", "Upload" },
                SampleCode = @"// Cấu hình elFinder connector ánh xạ vào wwwroot/uploads",
                LearningOutcomesVi = new() { "Trang bị cho admin công cụ quản lý thư viện hình ảnh toàn diện", "Giới hạn quyền thao tác file tránh phá hoại hệ thống" },
                LearningOutcomesEn = new() { "Equip admins with a complete media management studio", "Enforce strict sandbox security on root upload volumes" }
            },
            new()
            {
                LessonNumber = 100,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Tích hợp giao diện quản trị Admin Template SB Admin 2 / Bootstrap Dashboard",
                TitleEn = "Integrating SB Admin 2 / Bootstrap Dashboard Template",
                Slug = "aspnet-core-mvc-tich-hop-sb-admin-2",
                SummaryVi = "Chuyển đổi giao diện Admin template HTML tĩnh SB Admin 2 thành Master Layout Razor chuyên nghiệp: Sidebar thu phóng, Topbar thông báo, biểu đồ Chart.js và Datatables.",
                SummaryEn = "Converting static SB Admin 2 templates into a responsive Razor admin master layout: collapsible sidebar, notifications dropdown, Chart.js, and Datatables.",
                Difficulty = DifficultyLevel.Intermediate,
                EstimatedMinutes = 35,
                KeyTags = new() { "SB Admin 2", "Dashboard", "Admin Layout", "Chart.js" },
                SampleCode = @"<!-- _AdminLayout.cshtml -->
<div id=""wrapper"">
    <partial name=""_AdminSidebar"" />
    <div id=""content-wrapper"">@RenderBody()</div>
</div>",
                LearningOutcomesVi = new() { "Biến bất kỳ HTML template nào thành dự án ASP.NET Core chuyên nghiệp", "Tách layout Admin riêng biệt hoàn toàn với layout Client" },
                LearningOutcomesEn = new() { "Convert any commercial HTML theme into an ASP.NET Core layout", "Completely isolate admin dashboard from public portals" }
            },
            new()
            {
                LessonNumber = 101,
                SectionNumber = 7,
                SectionTitleVi = s7.TitleVi,
                SectionTitleEn = s7.TitleEn,
                SectionBadgeColor = s7.BadgeColor,
                SectionIcon = s7.Icon,
                TitleVi = "Đóng gói (Publish) và Triển khai Website lên Linux Server (Kestrel, Nginx / Apache, Docker)",
                TitleEn = "Publishing and Deploying ASP.NET Core to Linux (Kestrel, Nginx, Docker)",
                Slug = "aspnet-core-mvc-trien-khai-publish-linux-kestrel-nginx",
                MatchingTutorialSlug = "trien-khai-ung-dung-aspnet-core-len-production-iis-docker",
                SummaryVi = "Đóng gói ứng dụng với dotnet publish -c Release, cấu hình Kestrel chạy service systemd trên Linux Ubuntu, thiết lập Nginx làm Reverse Proxy, chứng chỉ SSL Let's Encrypt và Docker container.",
                SummaryEn = "Compiling with dotnet publish -c Release, running Kestrel as a systemd service on Ubuntu Linux, configuring Nginx reverse proxy, free SSL with Let's Encrypt, and Dockerization.",
                Difficulty = DifficultyLevel.Advanced,
                EstimatedMinutes = 45,
                KeyTags = new() { "Deployment", "dotnet publish", "Linux Ubuntu", "Nginx", "Kestrel", "Docker", "SSL" },
                SampleCode = @"# Lệnh đóng gói
dotnet publish -c Release -o /var/www/myweb

# Cấu hình Nginx Reverse Proxy
server {
    listen 80;
    server_name example.com;
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}",
                LearningOutcomesVi = new() { "Tự tin đưa website thực tế lên internet cho hàng triệu người dùng", "Nắm vững quy trình DevOps chuẩn của một .NET Backend Engineer" },
                LearningOutcomesEn = new() { "Confidently ship production web apps to real internet users", "Master modern .NET backend DevOps workflows" }
            }
        });
        sections.Add(s7);

        return sections;
    }
    #endregion
}
