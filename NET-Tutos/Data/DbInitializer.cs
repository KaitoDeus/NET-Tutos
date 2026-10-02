using Microsoft.AspNetCore.Identity;
using NET_Tutos.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace NET_Tutos.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Ensure newly added LMS Playground tables exist
        await EnsurePlaygroundTablesExistAsync(context);

        // Ensure newly added LMS Discussion & Gamification tables exist
        await EnsureDiscussionAndGamificationTablesExistAsync(context);

        // Ensure newly added LMS Streak & Daily Check-in tables exist
        await EnsureStreakTablesExistAsync(context);

        // Ensure newly added LMS Notification & Activity Feed tables exist
        await EnsureNotificationAndActivityTablesExistAsync(context);

        // Seed Roles
        string[] roles = { "Admin", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Default Admin User
        var adminEmail = "admin@nettutos.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Quản trị viên Hệ thống",
                EmailConfirmed = true,
                ExperiencePoints = 999,
                Bio = "Quản trị viên & Tác giả biên soạn nội dung NET-Tutos",
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, "AdminPassword@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Coding Challenges if none exist
        if (!await context.CodingChallenges.AnyAsync())
        {
            await SeedCodingChallengesAsync(context);
        }

        // Seed Initial Discussions and Badges if none exist
        if (!await context.DiscussionComments.AnyAsync())
        {
            await SeedInitialDiscussionsAndBadgesAsync(context, userManager);
        }

        // Check if data already exists
        if (await context.Categories.AnyAsync())
        {
            return; // DB has been seeded
        }

        #region Categories
        var catBeginner = new Category
        {
            Name = "1. C# & .NET Cơ bản",
            Slug = "csharp-co-ban",
            Description = "Làm quen với hệ sinh thái .NET, cú pháp C#, biến, kiểu dữ liệu, vòng lặp và cấu trúc dữ liệu nền tảng.",
            IconClass = "bi-laptop",
            BadgeColor = "success",
            OrderIndex = 1,
            Level = DifficultyLevel.Beginner
        };

        var catOop = new Category
        {
            Name = "2. OOP & C# Nâng cao",
            Slug = "oop-csharp-nang-cao",
            Description = "Nắm vững 4 trụ cột OOP, Interface, Generics, LINQ và lập trình bất đồng bộ async/await.",
            IconClass = "bi-layers-half",
            BadgeColor = "info",
            OrderIndex = 2,
            Level = DifficultyLevel.Intermediate
        };

        var catEf = new Category
        {
            Name = "3. Entity Framework Core & Database",
            Slug = "entity-framework-core",
            Description = "Quản lý và thao tác cơ sở dữ liệu với EF Core theo hướng tiếp cận Code-First, Migrations và tối ưu hiệu năng.",
            IconClass = "bi-database",
            BadgeColor = "warning",
            OrderIndex = 3,
            Level = DifficultyLevel.Intermediate
        };

        var catAspNetCore = new Category
        {
            Name = "4. ASP.NET Core MVC & Web API",
            Slug = "aspnet-core-mvc-api",
            Description = "Xây dựng Web App hoàn chỉnh với mô hình MVC, RESTful Web API, Middleware, Dependency Injection và Clean Architecture.",
            IconClass = "bi-globe2",
            BadgeColor = "primary",
            OrderIndex = 4,
            Level = DifficultyLevel.Advanced
        };

        context.Categories.AddRange(catBeginner, catOop, catEf, catAspNetCore);
        await context.SaveChangesAsync();
        #endregion

        #region Tutorials & Quizzes
        var tutorials = new List<Tutorial>();

        #region Tutorial 1: Tổng quan .NET
        var t1 = new Tutorial
        {
            CategoryId = catBeginner.Id,
            Title = "Bài 1: Tổng quan hệ sinh thái .NET & Cài đặt môi trường",
            Slug = "tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong",
            Summary = "Tìm hiểu lịch sử .NET Framework vs .NET Core vs .NET hiện đại (.NET 8/9/10), kiến trúc CLR, BCL và thiết lập môi trường lập trình C#.",
            EstimatedReadingMinutes = 7,
            Difficulty = DifficultyLevel.Beginner,
            OrderIndex = 1,
            IsFeatured = true,
            ContentMarkdown = @"# Tổng quan về .NET và Cài đặt môi trường

## 1. .NET là gì?
**.NET** là một nền tảng lập trình miễn phí, mã nguồn mở và đa nền tảng (cross-platform) do Microsoft phát triển. Với .NET, bạn có thể xây dựng đa dạng loại ứng dụng: Web, Mobile, Desktop, Cloud, Game, IoT và Trí tuệ nhân tạo (AI).

### Phân biệt các thuật ngữ thường gặp:
- **.NET Framework (4.x trở về trước)**: Nền tảng cũ, chỉ chạy trên hệ điều hành Windows. Hiện tại đã dừng phát triển tính năng mới, chỉ duy trì bảo mật.
- **.NET Core (1.0 - 3.1)**: Cuộc cách mạng đa nền tảng của Microsoft, chạy mượt mà trên Windows, Linux và macOS.
- **.NET hiện đại (.NET 5, 6, 7, 8, 9, 10...)**: Sự hợp nhất thống nhất của toàn bộ hệ sinh thái .NET. Tên gọi chính thức được rút gọn thành **.NET** kèm số phiên bản.

```
┌─────────────────────────────────────────────────────────────┐
│                       Ứng dụng (.NET)                       │
│  Web (ASP.NET) │ Desktop (WPF/MAUI) │ Cloud │ Mobile │ AI   │
├─────────────────────────────────────────────────────────────┤
│         Base Class Library (BCL) & Common APIs              │
├─────────────────────────────────────────────────────────────┤
│             Common Language Runtime (CLR / CoreCLR)         │
│          (Quản lý bộ nhớ GC, JIT Compiler, Luồng)           │
├─────────────────────────────────────────────────────────────┤
│             Hệ điều hành: Windows │ Linux │ macOS           │
└─────────────────────────────────────────────────────────────┘
```

## 2. Các thành phần cốt lõi của .NET
1. **CLR (Common Language Runtime)**: Máy ảo thực thi mã .NET, phụ trách dọn rác tự động (**Garbage Collector - GC**), biên dịch JIT (Just-In-Time) từ mã trung gian IL sang mã máy (Native Code), và bảo đảm an toàn kiểu (Type Safety).
2. **BCL (Base Class Library)**: Thư viện các lớp tiêu chuẩn cung cấp sẵn các tính năng thao tác chuỗi, file I/O, mạng, collections, mã hóa, đa luồng,...
3. **C# Language**: Ngôn ngữ lập trình hiện đại, mạnh mẽ, hướng đối tượng mạnh mẽ và an toàn kiểu, được dùng phổ biến nhất trên .NET.

## 3. Cài đặt môi trường lập trình
Để bắt đầu, bạn cần cài đặt:
1. **.NET SDK**: Tải tại [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).
2. **IDE / Code Editor**:
   - **Visual Studio Community**: Đầy đủ tính năng, giao diện kéo thả trực quan.
   - **VS Code**: Nhẹ, linh hoạt kết hợp với extension **C# Dev Kit**.
   - **Antigravity / JetBrains Rider**: Các công cụ lập trình hiện đại hỗ trợ AI cực mạnh.

### Kiểm tra cài đặt qua Terminal / PowerShell:
```bash
# Kiểm tra phiên bản SDK đã cài đặt
dotnet --version

# Xem thông tin chi tiết runtime và môi trường
dotnet --info
```

### Tạo dự án đầu tiên (Console App):
```bash
# Tạo ứng dụng Console mới
dotnet new console -n HelloWorld

# Di chuyển vào thư mục
cd HelloWorld

# Chạy ứng dụng
dotnet run
```

### Mã nguồn `Program.cs` đầu tiên:
```csharp
// Top-level statement trong C# hiện đại
Console.WriteLine(""Xin chào thế giới .NET! Chúc bạn học tốt!"");

int namHienTai = DateTime.Now.Year;
Console.WriteLine($""Năm nay là: {namHienTai}"");
```

> **Lời khuyên**: Hãy luôn sử dụng phiên bản .NET LTS (Long Term Support) như .NET 8 hoặc phiên bản mới nhất như .NET 10 để nhận được hiệu năng tối ưu nhất."
        };

        t1.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Thành phần nào trong kiến trúc .NET chịu trách nhiệm quản lý bộ nhớ tự động (Garbage Collection) và biên dịch mã trung gian (IL) thành mã máy?",
            OptionA = "BCL (Base Class Library)",
            OptionB = "CLR (Common Language Runtime)",
            OptionC = "MSBuild Tool",
            OptionD = "SDK CLI",
            CorrectOption = "B",
            Explanation = "CLR (Common Language Runtime) là môi trường thực thi máy ảo của .NET, đảm nhận Garbage Collection, JIT compilation và bảo vệ tính an toàn bộ nhớ.",
            Difficulty = DifficultyLevel.Beginner
        });

        t1.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Lệnh nào trong .NET CLI dùng để chạy một dự án .NET ngay từ cửa sổ dòng lệnh?",
            OptionA = "dotnet build",
            OptionB = "dotnet start",
            OptionC = "dotnet run",
            OptionD = "dotnet execute",
            CorrectOption = "C",
            Explanation = "Lệnh 'dotnet run' sẽ vừa biên dịch (nếu có thay đổi) vừa khởi chạy dự án ngay lập tức.",
            Difficulty = DifficultyLevel.Beginner
        });
        #endregion

        #region Tutorial 2: Cú pháp C# nền tảng
        var t2 = new Tutorial
        {
            CategoryId = catBeginner.Id,
            Title = "Bài 2: Cú pháp C# nền tảng - Biến, Kiểu dữ liệu & Luồng điều khiển",
            Slug = "cu-phap-csharp-nen-tang-bien-kieu-du-lieu-luong-dieu-khien",
            Summary = "Nắm chắc các kiểu dữ liệu nguyên thủy, từ khóa var, nullable types, cấu trúc if-else, switch expression và vòng lặp trong C# hiện đại.",
            EstimatedReadingMinutes = 8,
            Difficulty = DifficultyLevel.Beginner,
            OrderIndex = 2,
            IsFeatured = true,
            ContentMarkdown = @"# Cú pháp C# Nền tảng: Biến, Kiểu dữ liệu & Điều khiển Luồng

## 1. Kiểu dữ liệu và Khai báo Biến
C# là ngôn ngữ **kiểm tra kiểu tĩnh (statically typed)**. Mỗi biến phải có một kiểu dữ liệu xác định trước khi sử dụng.

### Các kiểu dữ liệu cơ bản:
- Số nguyên: `int` (32-bit), `long` (64-bit), `short` (16-bit), `byte` (8-bit)
- Số thực: `double` (chuẩn khoa học), `float`, `decimal` (độ chính xác cao, dùng trong tài chính/tiền tệ)
- Ký tự & Chuỗi: `char` ('A'), `string` (""Xin chào"")
- Luận lý: `bool` (`true` hoặc `false`)

### Ví dụ khai báo:
```csharp
int tuoi = 22;
double diemTrungBinh = 8.75;
decimal hocPhi = 15000000.50m; // Có chữ m/M ở cuối
bool laSinhVien = true;
string hoTen = ""Nguyễn Văn An"";

// Sử dụng từ khóa 'var' - Trình biên dịch tự suy luận kiểu (Type Inference)
var thanhPho = ""Hà Nội""; // Kiểu string
var soLuong = 100;        // Kiểu int
```

### Nullable Types (Kiểu cho phép null):
Trong C#, giá trị tham trị (value types) mặc định không nhận giá trị `null`. Để cho phép `null`, ta thêm dấu `?`:
```csharp
int? diemKiemTra = null; // Cho phép không có điểm
if (diemKiemTra.HasValue)
{
    Console.WriteLine($""Điểm: {diemKiemTra.Value}"");
}
else
{
    Console.WriteLine(""Chưa có điểm kiểm tra"");
}

// Toán tử Null-coalescing (??)
int diemThucTe = diemKiemTra ?? 0; // Nếu null thì lấy 0
```

## 2. Cấu trúc Điều khiển rẽ nhánh

### Cấu trúc `if - else`:
```csharp
int diem = 85;

if (diem >= 90)
{
    Console.WriteLine(""Xuất sắc"");
}
else if (diem >= 80)
{
    Console.WriteLine(""Giỏi"");
}
else
{
    Console.WriteLine(""Khá / Trung bình"");
}
```

### Switch Expression (C# hiện đại):
```csharp
string xepLoai = diem switch
{
    >= 90 => ""Xuất sắc"",
    >= 80 => ""Giỏi"",
    >= 65 => ""Khá"",
    >= 50 => ""Trung bình"",
    _ => ""Yếu"" // Giá trị mặc định (default)
};

Console.WriteLine($""Xếp loại học lực: {xepLoai}"");
```

## 3. Vòng lặp trong C#

```csharp
// 1. Vòng lặp for kinh điển
for (int i = 1; i <= 5; i++)
{
    Console.WriteLine($""Lần lặp {i}"");
}

// 2. Vòng lặp foreach duyệt mảng/danh sách
string[] ngonNgu = { ""C#"", ""F#"", ""VB.NET"", ""SQL"" };
foreach (var item in ngonNgu)
{
    Console.WriteLine($""Ngôn ngữ: {item}"");
}

// 3. Vòng lặp while và do-while
int dem = 0;
while (dem < 3)
{
    Console.WriteLine($""Đếm: {dem}"");
    dem++;
}
```

> **Ghi nhớ**: Luôn ưu tiên dùng `decimal` khi tính toán số liệu liên quan đến tiền tệ trong .NET để tránh sai số dấu phẩy động của `double`/`float`."
        };

        t2.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Trong C#, kiểu dữ liệu nào được khuyến nghị sử dụng khi tính toán tài chính, tiền tệ để tránh sai số dấu chấm động?",
            OptionA = "float",
            OptionB = "double",
            OptionC = "decimal",
            OptionD = "long",
            CorrectOption = "C",
            Explanation = "Kiểu 'decimal' có độ chính xác 128-bit cao nhất và không làm tròn nhị phân như float/double, rất phù hợp cho tiền tệ và tài chính.",
            Difficulty = DifficultyLevel.Beginner
        });
        #endregion

        #region Tutorial 3: Cấu trúc dữ liệu & Collections
        var t3 = new Tutorial
        {
            CategoryId = catBeginner.Id,
            Title = "Bài 3: Cấu trúc dữ liệu & Xử lý Chuỗi - List, Dictionary, Array & StringBuilder",
            Slug = "cau-truc-du-lieu-xu-ly-chuoi-list-dictionary-stringbuilder",
            Summary = "Khám phá cách sử dụng mảng tĩnh, danh sách động List<T>, từ điển Dictionary<TKey, TValue> và kỹ thuật ghép chuỗi hiệu năng cao với StringBuilder.",
            EstimatedReadingMinutes = 9,
            Difficulty = DifficultyLevel.Beginner,
            OrderIndex = 3,
            ContentMarkdown = @"# Cấu trúc dữ liệu & Xử lý Chuỗi trong .NET

## 1. Mảng (Array) vs Danh sách động (`List<T>`)

### Mảng tĩnh (Array):
Mảng có kích thước cố định khi khởi tạo, truy xuất phần tử theo chỉ số (Index) với độ phức tạp $O(1)$.
```csharp
int[] soNguyen = new int[3] { 10, 20, 30 };
Console.WriteLine(soNguyen[0]); // 10
Console.WriteLine($""Độ dài: {soNguyen.Length}"");
```

### Danh sách động (`List<T>`):
Trong thực tế, bạn sẽ dùng `List<T>` (nằm trong namespace `System.Collections.Generic`) nhiều nhất vì kích thước của nó có thể co giãn linh hoạt:
```csharp
List<string> danhSachTen = new List<string>();

// Thêm phần tử
danhSachTen.Add(""Nguyễn Văn An"");
danhSachTen.Add(""Trần Thị Bình"");
danhSachTen.Add(""Lê Hoàng Cường"");

// Thao tác kiểm tra và xóa
if (danhSachTen.Contains(""Nguyễn Văn An""))
{
    Console.WriteLine(""Đã tìm thấy học viên!"");
}

danhSachTen.Remove(""Lê Hoàng Cường"");
Console.WriteLine($""Số lượng học viên: {danhSachTen.Count}"");
```

## 2. Từ điển (`Dictionary<TKey, TValue>`)
Lưu trữ cặp khóa-giá trị (Key-Value) dựa trên bảng băm (Hash Table). Tìm kiếm cực nhanh theo Key với độ phức tạp trung bình $O(1)$.
```csharp
Dictionary<string, string> tuDien = new Dictionary<string, string>
{
    { ""OOP"", ""Lập trình hướng đối tượng"" },
    { ""DI"", ""Dependency Injection"" },
    { ""EF"", ""Entity Framework"" }
};

// Thêm phần tử
tuDien[""CLR""] = ""Common Language Runtime"";

// Tìm kiếm an toàn với TryGetValue
if (tuDien.TryGetValue(""OOP"", out string? dinhNghia))
{
    Console.WriteLine($""OOP nghĩa là: {dinhNghia}"");
}
```

## 3. Tối ưu Xử lý Chuỗi với `StringBuilder`
Trong C#, kiểu `string` là **bất biến (Immutable)**. Mỗi khi bạn nối chuỗi bằng toán tử `+`, một đối tượng chuỗi mới trong bộ nhớ Heap sẽ được tạo ra và làm tăng áp lực cho Garbage Collector.

Khi cần ghép nối nhiều chuỗi trong vòng lặp, hãy dùng `StringBuilder`:
```csharp
using System.Text;

var sb = new StringBuilder();
for (int i = 1; i <= 1000; i++)
{
    sb.Append($""Dòng số {i}; "");
}

string ketQua = sb.ToString();
```

> **Best Practice**: Nếu số lần nối chuỗi ít (< 4 chuỗi), dùng `string.Format` hoặc String Interpolation `$""{a} {b}""`. Khi nối chuỗi trong vòng lặp lớn, bắt buộc dùng `StringBuilder`."
        };

        t3.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Tại sao nên dùng StringBuilder thay vì nối chuỗi bằng toán tử '+' trong các vòng lặp lớn?",
            OptionA = "Vì toán tử '+' bị lỗi cú pháp trong C#",
            OptionB = "Vì kiểu string trong C# là Immutable (bất biến), nối chuỗi liên tục sẽ tạo ra nhiều đối tượng thừa gây tốn bộ nhớ",
            OptionC = "Vì StringBuilder tự động mã hóa chuỗi sang định dạng UTF-32",
            OptionD = "Vì string chỉ hỗ trợ độ dài tối đa 256 ký tự",
            CorrectOption = "B",
            Explanation = "Chuỗi trong C# là immutable. Khi dùng '+', mỗi lần nối sẽ sinh ra một vùng nhớ mới trên Heap. StringBuilder sử dụng bộ đệm động (buffer) có thể chỉnh sửa tại chỗ, giúp tối ưu bộ nhớ vượt trội.",
            Difficulty = DifficultyLevel.Beginner
        });
        #endregion

        #region Tutorial 4: 4 Trụ cột OOP
        var t4 = new Tutorial
        {
            CategoryId = catOop.Id,
            Title = "Bài 4: 4 Trụ cột Lập trình Hướng đối tượng (OOP) trong C#",
            Slug = "4-tru-cot-lap-trinh-huong-doi-tuong-oop-trong-csharp",
            Summary = "Tìm hiểu chi tiết tính Đóng gói (Encapsulation), Kế thừa (Inheritance), Đa hình (Polymorphism) và Trừu tượng (Abstraction) qua ví dụ mã nguồn thực tế.",
            EstimatedReadingMinutes = 10,
            Difficulty = DifficultyLevel.Intermediate,
            OrderIndex = 4,
            IsFeatured = true,
            ContentMarkdown = @"# 4 Trụ cột của Lập trình Hướng đối tượng (OOP) trong C#

OOP (Object-Oriented Programming) là nền tảng cốt lõi của ngôn ngữ C# và framework .NET.

## 1. Tính Đóng Gói (Encapsulation)
Ẩn giấu dữ liệu nội bộ của đối tượng và chỉ cung cấp các phương thức/thuộc tính (Property) an toàn để truy cập hoặc sửa đổi.

```csharp
public class TaiKhoanNganHang
{
    // Trường dữ liệu riêng tư (Private field)
    private decimal _soDu;

    // Thuộc tính công khai (Public property) với kiểm tra hợp lệ
    public decimal SoDu
    {
        get { return _soDu; }
        private set 
        { 
            if (value >= 0) _soDu = value; 
        }
    }

    public string SoTaiKhoan { get; }

    public TaiKhoanNganHang(string soTk, decimal soDuBanDau)
    {
        SoTaiKhoan = soTk;
        _soDu = soDuBanDau > 0 ? soDuBanDau : 0;
    }

    public void GuiTien(decimal soTien)
    {
        if (soTien <= 0)
            throw new ArgumentException(""Số tiền nạp phải lớn hơn 0"");
        _soDu += soTien;
    }
}
```

## 2. Tính Kế Thừa (Inheritance)
Cho phép lớp con (Derived Class) kế thừa thuộc tính và hành vi từ lớp cha (Base Class), tái sử dụng mã nguồn hiệu quả.

```csharp
// Lớp cha
public class Nguoi
{
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public void GioiThieu()
    {
        Console.WriteLine($""Xin chào, tôi là {HoTen}"");
    }
}

// Lớp con kế thừa từ Nguoi
public class SinhVien : Nguoi
{
    public string MaSinhVien { get; set; } = string.Empty;
    public double DiemTrungBinh { get; set; }
}
```

## 3. Tính Đa Hình (Polymorphism)
Cùng một phương thức nhưng các đối tượng thuộc các lớp khác nhau sẽ thực thi hành vi khác nhau. Trong C#, sử dụng từ khóa `virtual` ở lớp cha và `override` ở lớp con:

```csharp
public class DongVat
{
    public virtual void PhatRaAmThanh()
    {
        Console.WriteLine(""Âm thanh động vật chung..."");
    }
}

public class Cho : DongVat
{
    public override void PhatRaAmThanh()
    {
        Console.WriteLine(""Gâu gâu!"");
    }
}

public class Meo : DongVat
{
    public override void PhatRaAmThanh()
    {
        Console.WriteLine(""Meo meo!"");
    }
}

// Sử dụng tính đa hình:
List<DongVat> danhSach = new() { new Cho(), new Meo(), new DongVat() };
foreach (var dv in danhSach)
{
    dv.PhatRaAmThanh(); // Mỗi con vật phát ra âm thanh riêng biệt
}
```

## 4. Tính Trừu Tượng (Abstraction)
Tập trung vào bản chất hành động thay vì chi tiết thực hiện. Thực hiện thông qua **Abstract Class** hoặc **Interface**:

```csharp
public abstract class ThanhToan
{
    public abstract void XuLyThanhToan(decimal soTien);
}

public class ThanhToanVnPay : ThanhToan
{
    public override void XuLyThanhToan(decimal soTien)
    {
        Console.WriteLine($""Xử lý thanh toán VNPay số tiền: {soTien:N0} VNĐ qua QR Code."");
    }
}
```"
        };

        t4.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Trong C#, từ khóa nào được dùng ở phương thức của lớp cha để cho phép lớp con ghi đè (override) hành vi?",
            OptionA = "static",
            OptionB = "virtual",
            OptionC = "sealed",
            OptionD = "const",
            CorrectOption = "B",
            Explanation = "Từ khóa 'virtual' khai báo phương thức có thể được override ở lớp con bằng từ khóa 'override'.",
            Difficulty = DifficultyLevel.Intermediate
        });
        #endregion

        #region Tutorial 5: Interface & Generics
        var t5 = new Tutorial
        {
            CategoryId = catOop.Id,
            Title = "Bài 5: Interface & Generics trong C# Hiện đại",
            Slug = "interface-va-generics-trong-csharp-hien-dai",
            Summary = "Hiểu rõ hợp đồng Interface, cách giải quyết đa kế thừa hành vi, và ứng dụng Generic (T) để viết mã nguồn linh hoạt, an toàn kiểu và hiệu năng cao.",
            EstimatedReadingMinutes = 9,
            Difficulty = DifficultyLevel.Intermediate,
            OrderIndex = 5,
            ContentMarkdown = @"# Interface & Generics trong C#

## 1. Interface là gì?
**Interface** là một bản hợp đồng (Contract). Bất kỳ class nào thực thi (implement) interface đều cam kết phải cung cấp việc cài đặt cụ thể cho các phương thức, thuộc tính được khai báo trong interface đó.

### Ưu điểm của Interface:
- Cho phép một class thực thi nhiều interface (Giải quyết hạn chế đơn kế thừa class của C#).
- Tạo nên sự độc lập mã nguồn (Loose Coupling), là nền móng của **Dependency Injection**.

```csharp
public interface IGuiThongBao
{
    void Gui(string nguoiNhan, string noiDung);
}

public class EmailThongBao : IGuiThongBao
{
    public void Gui(string nguoiNhan, string noiDung)
    {
        Console.WriteLine($""Đang gửi Email tới {nguoiNhan}: {noiDung}"");
    }
}

public class SmsThongBao : IGuiThongBao
{
    public void Gui(string nguoiNhan, string noiDung)
    {
        Console.WriteLine($""Đang gửi tin nhắn SMS tới SĐT {nguoiNhan}: {noiDung}"");
    }
}
```

## 2. Generics trong C#
Generics cho phép bạn định nghĩa các class, interface, method với một tham số kiểu dữ liệu giữ chỗ (placeholder type, thường ký hiệu là `T`).

### Lợi ích của Generics:
1. **Type Safety**: Kiểm tra lỗi kiểu dữ liệu ngay tại thời điểm biên dịch (Compile-time).
2. **Performance**: Tránh hiện tượng **Boxing / Unboxing** (chuyển đổi qua lại giữa Value Type và Reference Type `object`).
3. **Reusability**: Viết code 1 lần, dùng cho mọi kiểu dữ liệu.

```csharp
// Generic Repository Pattern cơ bản
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task DeleteAsync(int id);
}

// Lớp Generic kết quả trả về API
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = ""Thành công"") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Message = error, Data = default };
}
```"
        };
        #endregion

        #region Tutorial 6: LINQ
        var t6 = new Tutorial
        {
            CategoryId = catOop.Id,
            Title = "Bài 6: LINQ (Language Integrated Query) & Lambda Expressions",
            Slug = "linq-language-integrated-query-va-lambda-expressions",
            Summary = "Làm chủ các toán tử LINQ thông dụng: Where, Select, OrderBy, GroupBy, Any, All, FirstOrDefault, Sum, Count và phân biệt Deferred Execution.",
            EstimatedReadingMinutes = 11,
            Difficulty = DifficultyLevel.Intermediate,
            OrderIndex = 6,
            IsFeatured = true,
            ContentMarkdown = @"# LINQ & Lambda Expressions từ A đến Z

## 1. LINQ là gì?
**LINQ (Language Integrated Query)** là tính năng cực kỳ mạnh mẽ của .NET cho phép bạn viết truy vấn dữ liệu đồng nhất trên nhiều nguồn dữ liệu khác nhau: bộ nhớ (`IEnumerable<T>`), cơ sở dữ liệu (`IQueryable<T>` với EF Core), XML hay JSON.

## 2. Các toán tử LINQ thông dụng nhất

Giả sử chúng ta có danh sách sản phẩm:
```csharp
public record SanPham(int Id, string Ten, decimal Gia, string DanhMuc, int SoLuongTon);

var ds = new List<SanPham>
{
    new(1, ""Laptop Dell XPS"", 32000000m, ""Laptop"", 5),
    new(2, ""MacBook Pro M3"", 45000000m, ""Laptop"", 3),
    new(3, ""Chuột Logitech MX"", 2100000m, ""Phụ kiện"", 20),
    new(4, ""Bàn phím cơ Keychron"", 1800000m, ""Phụ kiện"", 0),
    new(5, ""Màn hình Dell 4K"", 12500000m, ""Màn hình"", 8)
};
```

### 1. `Where`: Lọc dữ liệu theo điều kiện
```csharp
// Tìm các sản phẩm thuộc danh mục Laptop có giá trên 30 triệu
var laptops = ds.Where(p => p.DanhMuc == ""Laptop"" && p.Gia > 30000000m).ToList();
```

### 2. `Select`: Chiếu (Mapping/Projection) dữ liệu
```csharp
// Lấy danh sách tên sản phẩm viết hoa
var danhSachTen = ds.Select(p => p.Ten.ToUpper()).ToList();

// Tạo kiểu ẩn danh (Anonymous Type) hoặc DTO
var summary = ds.Select(p => new { p.Ten, GiaVND = $""{p.Gia:N0} đ"" }).ToList();
```

### 3. `OrderBy` & `OrderByDescending`: Sắp xếp
```csharp
// Sắp xếp giá giảm dần
var sapXepGia = ds.OrderByDescending(p => p.Gia).ToList();
```

### 4. `FirstOrDefault` & `SingleOrDefault`: Lấy một phần tử
```csharp
// Lấy sản phẩm có Id = 2, nếu không có trả về null
var sp = ds.FirstOrDefault(p => p.Id == 2);
```

### 5. `Any` & `All`: Kiểm tra logic
```csharp
// Kiểm tra xem có sản phẩm nào hết hàng (SoLuongTon == 0) không?
bool coSanPhamHetHang = ds.Any(p => p.SoLuongTon == 0); // true

// Kiểm tra xem tất cả sản phẩm đều có giá > 0 không?
bool tatCaGiaHopLe = ds.All(p => p.Gia > 0); // true
```

### 6. `GroupBy`: Gom nhóm dữ liệu
```csharp
var nhomDanhMuc = ds.GroupBy(p => p.DanhMuc)
    .Select(g => new
    {
        DanhMuc = g.Key,
        SoSanPham = g.Count(),
        TongGiaTriTon = g.Sum(p => p.Gia * p.SoLuongTon)
    });
```

## 3. Khái niệm Thực thi trì hoãn (Deferred Execution)
Truy vấn LINQ **không thực thi ngay lập tức** khi bạn khai báo. Nó chỉ thực thi khi bạn thực sự lặp qua nó (vòng lặp `foreach`) hoặc gọi các phương thức chuyển đổi như `.ToList()`, `.ToArray()`, `.Count()`, `.FirstOrDefault()`.
Điều này cực kỳ quan trọng khi làm việc với Entity Framework Core vì nó cho phép gộp các điều kiện lọc và chỉ gửi một câu lệnh SQL tối ưu duy nhất về Database Server."
        };

        t6.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Trong LINQ, phương thức nào sau đây sẽ kích hoạt thực thi truy vấn ngay lập tức (Immediate Execution)?",
            OptionA = "Where()",
            OptionB = "Select()",
            OptionC = "OrderBy()",
            OptionD = "ToList()",
            CorrectOption = "D",
            Explanation = "Where, Select, OrderBy là Deferred Execution (chưa chạy). Khi gọi ToList(), ToArray(), FirstOrDefault() hoặc lặp qua foreach thì truy vấn mới được kích hoạt chạy thực tế.",
            Difficulty = DifficultyLevel.Intermediate
        });
        #endregion

        #region Tutorial 7: Async / Await
        var t7 = new Tutorial
        {
            CategoryId = catOop.Id,
            Title = "Bài 7: Lập trình Bất đồng bộ (Async / Await) trong .NET",
            Slug = "lap-trinh-bat-dong-bo-async-await-trong-dotnet",
            Summary = "Hiểu rõ cơ chế Non-blocking I/O, cách hoạt động của Task, State Machine và quy tắc vàng tránh Deadlock khi lập trình bất đồng bộ.",
            EstimatedReadingMinutes = 8,
            Difficulty = DifficultyLevel.Intermediate,
            OrderIndex = 7,
            ContentMarkdown = @"# Lập trình Bất đồng bộ với Async / Await trong .NET

## 1. Tại sao cần Bất đồng bộ (Asynchronous)?
Khi ứng dụng thực hiện các tác vụ I/O tốn thời gian (truy vấn database, gọi API bên thứ ba, đọc file lớn trên ổ đĩa):
- **Đồng bộ (Synchronous)**: Luồng xử lý (Thread) sẽ bị chặn (**Block**) và nằm chờ, không thể xử lý yêu cầu nào khác. Ứng dụng web dễ bị nghẽn (Thread Pool Starvation).
- **Bất đồng bộ (Asynchronous)**: Luồng xử lý được giải phóng quay lại Thread Pool để phục vụ các người dùng khác. Khi tác vụ I/O hoàn tất, một luồng sẽ tiếp tục xử lý phần code còn lại.

## 2. Cú pháp `async` và `await`
```csharp
public async Task<string> LayDuLieuTuApiAsync(string url)
{
    using var client = new HttpClient();
    
    // await giải phóng luồng hiện tại trong khi mạng đang truyền tải dữ liệu
    string ketQua = await client.GetStringAsync(url);
    
    return ketQua;
}
```

## 3. Các quy tắc quan trọng:
1. **Quy tắc đặt tên**: Thêm hậu tố `Async` vào tên phương thức (ví dụ: `GetUsersAsync()`).
2. **Kiểu trả về**:
   - `Task`: Cho các phương thức không trả về giá trị (tương đương `void`).
   - `Task<T>`: Cho các phương thức trả về dữ liệu kiểu `T`.
   - **Tuyệt đối tránh `async void`** (chỉ trừ trường hợp Event Handler trong UI Desktop), vì `async void` không thể bắt ngoại lệ (Exception) bằng `try-catch` và không thể `await`.
3. **Tránh gọi `.Result` hoặc `.Wait()`**: Luôn dùng `await`. Gọi `.Result` trên tác vụ async sẽ chuyển tác vụ thành chặn đồng bộ và tiềm ẩn nguy cơ gây **Deadlock**!"
        };
        #endregion

        #region Tutorial 8: EF Core
        var t8 = new Tutorial
        {
            CategoryId = catEf.Id,
            Title = "Bài 8: Entity Framework Core - Code-First, DbContext & Migrations",
            Slug = "entity-framework-core-code-first-dbcontext-migrations",
            Summary = "Hướng dẫn chi tiết xây dựng ORM với EF Core: Thiết kế Entity, cấu hình DbContext, tạo và áp dụng Migrations lên SQL Server.",
            EstimatedReadingMinutes = 11,
            Difficulty = DifficultyLevel.Intermediate,
            OrderIndex = 8,
            IsFeatured = true,
            ContentMarkdown = @"# Entity Framework Core: Code-First, DbContext & Migrations

## 1. Entity Framework Core (EF Core) là gì?
EF Core là thư viện **ORM (Object-Relational Mapper)** chính thức của Microsoft dành cho .NET. Nó đóng vai trò cầu nối chuyển đổi giữa các đối tượng C# (Classes/Entities) và các bảng dữ liệu trong hệ quản trị cơ sở dữ liệu quan hệ (SQL Server, PostgreSQL, SQLite, MySQL,...).

## 2. Phương pháp tiếp cận Code-First
Trong Code-First, bạn định nghĩa các lớp Entity và DbContext bằng C#, sau đó dùng công cụ **Migrations** của EF Core để tự động sinh ra cấu trúc bảng, khóa chính, khóa ngoại trong SQL Server.

### Bước 1: Khai báo Entity
```csharp
public class Product
{
    public int Id { get; set; } // Tự động nhận diện làm Khóa chính (Primary Key)
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    
    // Thuộc tính điều hướng (Navigation Property)
    public Category? Category { get; set; }
}
```

### Bước 2: Tạo `DbContext`
```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
}
```

### Bước 3: Đăng ký DbContext trong `Program.cs`
```csharp
// Đăng ký SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));
```

### Bước 4: Lệnh CLI Migrations
```bash
# Tạo migration mới ghi lại thay đổi schema
dotnet ef migrations add InitialCreate

# Cập nhật schema vào SQL Server
dotnet ef database update
```

## 3. Các thao tác CRUD cơ bản với EF Core
```csharp
// 1. CREATE (Thêm mới)
var spMoi = new Product { Name = ""Bàn phím cơ"", Price = 1500000m, CategoryId = 1 };
await context.Products.AddAsync(spMoi);
await context.SaveChangesAsync();

// 2. READ (Đọc dữ liệu)
var ds = await context.Products
    .AsNoTracking() // Tăng tốc độ đọc nếu không cần cập nhật
    .Where(p => p.Price > 1000000m)
    .ToListAsync();

// 3. UPDATE (Cập nhật)
var spCanSua = await context.Products.FindAsync(1);
if (spCanSua != null)
{
    spCanSua.Price = 1400000m;
    await context.SaveChangesAsync();
}

// 4. DELETE (Xóa)
var spCanXoa = await context.Products.FindAsync(2);
if (spCanXoa != null)
{
    context.Products.Remove(spCanXoa);
    await context.SaveChangesAsync();
}
```"
        };

        t8.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Trong EF Core, phương thức nào được dùng để tối ưu hiệu năng đọc dữ liệu chỉ để hiển thị (Read-only), giúp tắt tính năng theo dõi thay đổi (Change Tracking)?",
            OptionA = "AsReadOnly()",
            OptionB = "AsNoTracking()",
            OptionC = "WithoutTracking()",
            OptionD = "DisableTracker()",
            CorrectOption = "B",
            Explanation = "AsNoTracking() thông báo cho EF Core không cần theo dõi sự thay đổi của entity trong DbContext, tiết kiệm đáng kể bộ nhớ và CPU khi đọc dữ liệu.",
            Difficulty = DifficultyLevel.Intermediate
        });
        #endregion

        #region Tutorial 9: ASP.NET Core MVC Overview
        var t9 = new Tutorial
        {
            CategoryId = catAspNetCore.Id,
            Title = "Bài 9: Kiến trúc ASP.NET Core MVC - Program.cs, Middleware & Controller",
            Slug = "kien-truc-aspnet-core-mvc-programcs-middleware-controller",
            Summary = "Khám phá vòng đời Request trong ASP.NET Core: Đi từ Kestrel Web Server qua Middleware Pipeline đến MVC Routing và Razor Views.",
            EstimatedReadingMinutes = 12,
            Difficulty = DifficultyLevel.Advanced,
            OrderIndex = 9,
            IsFeatured = true,
            ContentMarkdown = @"# Kiến trúc ASP.NET Core MVC Toàn diện

## 1. Mô hình MVC (Model - View - Controller)
- **Model**: Đại diện cho dữ liệu nghiệp vụ và các quy tắc logic (Entities, ViewModels).
- **View**: Giao diện hiển thị HTML/CSS/JavaScript gửi về cho trình duyệt (file `.cshtml` với Razor engine).
- **Controller**: Tiếp nhận HTTP Request từ người dùng, gọi các dịch vụ (Services) xử lý dữ liệu và trả về View hoặc JSON tương ứng.

```
       HTTP Request
Browser  ───────►  [Routing / Middleware Pipeline]
                       │
                       ▼
                 [Controller]  ◄───►  [Service / DbContext]
                       │
                       ▼
                 [Razor View]  ───►  Render HTML  ───►  Browser
```

## 2. Vòng đời Request và Middleware Pipeline trong `Program.cs`
Middleware là các đoạn code được xắp xếp theo thứ tự chuỗi liên tiếp (Pipeline) để xử lý Request và Response.

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các dịch vụ (Dependency Injection Container)
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(...);

var app = builder.Build();

// 2. Cấu hình chuỗi Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(""/Home/Error"");
    app.UseHsts();
}

app.UseHttpsRedirection(); // Chuyển hướng HTTP sang HTTPS
app.UseStaticFiles();       // Cho phép đọc file tĩnh (css, js, images trong wwwroot)
app.UseRouting();           // Định tuyến URL
app.UseAuthentication();    // Xác thực người dùng (Ai đây?)
app.UseAuthorization();     // Phân quyền (Được làm gì?)

// 3. Khớp định tuyến Controller
app.MapControllerRoute(
    name: ""default"",
    pattern: ""{controller=Home}/{action=Index}/{id?}"");

app.Run();
```

## 3. Tạo Controller và Action trong ASP.NET Core MVC
```csharp
public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    // Tiêm phụ thuộc DbContext qua Constructor
    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Products
    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.ToListAsync();
        return View(products); // Trả về Views/Products/Index.cshtml
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(); // Trả về mã lỗi HTTP 404
        }
        return View(product);
    }
}
```"
        };

        t9.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Trong chuỗi Middleware của ASP.NET Core, thứ tự nào sau đây là chính xác giữa Authentication và Authorization?",
            OptionA = "UseAuthorization() phải đứng trước UseAuthentication()",
            OptionB = "UseAuthentication() phải đứng trước UseAuthorization()",
            OptionC = "Hai middleware này có thể đặt ở bất kỳ thứ tự nào",
            OptionD = "Không cần gọi UseAuthentication() nếu đã gọi UseAuthorization()",
            CorrectOption = "B",
            Explanation = "Bạn phải xác thực người dùng là ai trước (Authentication) rồi mới có thể kiểm tra xem họ có quyền làm gì (Authorization).",
            Difficulty = DifficultyLevel.Advanced
        });
        #endregion

        #region Tutorial 10: RESTful Web API
        var t10 = new Tutorial
        {
            CategoryId = catAspNetCore.Id,
            Title = "Bài 10: Xây dựng RESTful Web API chuẩn quốc tế với ASP.NET Core",
            Slug = "xay-dung-restful-web-api-chuan-voi-aspnet-core",
            Summary = "Thiết kế API theo các tiêu chuẩn REST: HTTP Methods (GET, POST, PUT, DELETE), Status Codes, Model Validation và tài liệu hóa bằng OpenAPI/Swagger.",
            EstimatedReadingMinutes = 10,
            Difficulty = DifficultyLevel.Advanced,
            OrderIndex = 10,
            ContentMarkdown = @"# Xây dựng RESTful Web API với ASP.NET Core

## 1. Nguyên tắc thiết kế RESTful API
- Sử dụng danh từ số nhiều cho đường dẫn URI (ví dụ: `/api/products`, `/api/users`).
- Dùng đúng chuẩn HTTP Verbs:
  - `GET`: Lấy dữ liệu (Idempotent, Safe).
  - `POST`: Tạo mới tài nguyên.
  - `PUT`: Cập nhật toàn bộ tài nguyên.
  - `PATCH`: Cập nhật một phần tài nguyên.
  - `DELETE`: Xóa tài nguyên.
- Trả về đúng HTTP Status Code:
  - `200 OK`: Thành công với dữ liệu trả về.
  - `201 Created`: Tạo mới thành công (kèm header Location).
  - `204 No Content`: Thành công nhưng không có dữ liệu trả về (thường dùng cho DELETE).
  - `400 Bad Request`: Dữ liệu gửi lên không hợp lệ.
  - `404 Not Found`: Không tìm thấy tài nguyên.
  - `500 Internal Server Error`: Lỗi máy chủ không lường trước.

## 2. Viết ApiController chuẩn trong C#
```csharp
[ApiController]
[Route(""api/[controller]"")]
public class ProductsApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return Ok(await _context.Products.AsNoTracking().ToListAsync());
    }

    [HttpGet(""{id}"")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound(new { message = $""Không tìm thấy sản phẩm #{id}"" });
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var product = new Product { Name = dto.Name, Price = dto.Price };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpDelete(""{id}"")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
```"
        };
        #endregion

        #region Tutorial 11: DI & SOLID
        var t11 = new Tutorial
        {
            CategoryId = catAspNetCore.Id,
            Title = "Bài 11: Dependency Injection (DI) & Nguyên lý SOLID trong ASP.NET Core",
            Slug = "dependency-injection-va-nguyen-ly-solid-trong-aspnet-core",
            Summary = "Phân biệt 3 vòng đời Service Lifetime (Transient, Scoped, Singleton) trong DI container của .NET và áp dụng 5 nguyên lý SOLID vào dự án thực chiến.",
            EstimatedReadingMinutes = 11,
            Difficulty = DifficultyLevel.Advanced,
            OrderIndex = 11,
            IsFeatured = true,
            ContentMarkdown = @"# Dependency Injection & Nguyên lý SOLID trong ASP.NET Core

## 1. 3 Vòng đời (Service Lifetime) của Dependency Injection trong .NET

Khi đăng ký dịch vụ vào DI Container (`builder.Services`), bạn có 3 lựa chọn về vòng đời:

| Lifetime | Cách hoạt động | Trường hợp sử dụng điển hình |
|---|---|---|
| **Transient** (`AddTransient`) | Mỗi lần yêu cầu (inject) sẽ tạo ra một thể hiện (**instance mới tinh**) | Các dịch vụ xử lý nhẹ, không lưu trạng thái (Stateless services) |
| **Scoped** (`AddScoped`) | Tạo **1 instance duy nhất cho mỗi HTTP Request**. Mọi class trong cùng request đó sẽ dùng chung instance này | `DbContext`, Repositories, Unit of Work |
| **Singleton** (`AddSingleton`) | Tạo **1 instance duy nhất trong toàn bộ vòng đời của ứng dụng** | Cache trong bộ nhớ, cấu hình ApplicationConfig, Background Workers |

```csharp
// Đăng ký dịch vụ trong Program.cs
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITutorialService, TutorialService>();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
```

## 2. 5 Nguyên lý SOLID
1. **S - Single Responsibility Principle (Đơn trách nhiệm)**: Một class chỉ nên có duy nhất một lý do để thay đổi. Đừng gộp logic gửi email, tính toán đơn hàng và lưu database vào chung một class!
2. **O - Open/Closed Principle (Mở để mở rộng, Đóng để sửa đổi)**: Thiết kế code sao cho khi có tính năng mới, ta chỉ cần viết thêm class mới kế thừa/implement thay vì sửa lại code cũ đã chạy ổn định.
3. **L - Liskov Substitution Principle (Thay thế Liskov)**: Các đối tượng của lớp con phải có khả năng thay thế lớp cha mà không làm sai lệch tính đúng đắn của chương trình.
4. **I - Interface Segregation Principle (Phân tách Interface)**: Thà tạo nhiều interface nhỏ chuyên biệt còn hơn tạo một interface khổng lồ với nhiều phương thức thừa thãi mà class implement không cần dùng đến.
5. **D - Dependency Inversion Principle (Đảo ngược phụ thuộc)**: Các module cấp cao không nên phụ thuộc vào module cấp thấp; cả hai nên phụ thuộc vào sự trừu tượng (Abstraction / Interface)."
        };

        t11.QuizQuestions.Add(new QuizQuestion
        {
            Question = "Trong ASP.NET Core, DbContext của Entity Framework Core thường được đăng ký với vòng đời (Service Lifetime) nào?",
            OptionA = "Transient",
            OptionB = "Scoped",
            OptionC = "Singleton",
            OptionD = "Static",
            CorrectOption = "B",
            Explanation = "DbContext được khuyến nghị đăng ký là Scoped để mỗi HTTP Request sở hữu một thể hiện DbContext riêng biệt, tránh xung đột dữ liệu giữa các request đồng thời.",
            Difficulty = DifficultyLevel.Advanced
        });
        #endregion

        tutorials.AddRange(new[] { t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11 });
        context.Tutorials.AddRange(tutorials);
        await context.SaveChangesAsync();
        #endregion

        #region Useful Code Snippets
        var snippets = new List<CodeSnippet>
        {
            new CodeSnippet
            {
                Title = "LINQ Filtering & Projection",
                Description = "Lọc danh sách và ánh xạ sang kiểu DTO với LINQ",
                Language = "csharp",
                CategoryName = "C# Core",
                Code = @"// Lọc và chọn dữ liệu bằng LINQ
var ketQua = danhSach
    .Where(x => x.IsActive && x.Price > 100)
    .OrderByDescending(x => x.CreatedAt)
    .Select(x => new ProductDto 
    { 
        Id = x.Id, 
        Name = x.Name, 
        PriceFormatted = $""{x.Price:C}"" 
    })
    .ToList();"
            },
            new CodeSnippet
            {
                Title = "Async/Await Call with Timeout & Cancellation",
                Description = "Gọi tác vụ bất đồng bộ an toàn với CancellationToken",
                Language = "csharp",
                CategoryName = "C# Core",
                Code = @"public async Task<string> FetchDataWithTimeoutAsync(string url, CancellationToken ct = default)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(5)); // Timeout sau 5 giây

    using var client = new HttpClient();
    var response = await client.GetAsync(url, cts.Token);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStringAsync(cts.Token);
}"
            },
            new CodeSnippet
            {
                Title = "EF Core Code-First DbContext Registration",
                Description = "Cấu hình DbContext trong Program.cs với ConnectionString",
                Language = "csharp",
                CategoryName = "Entity Framework",
                Code = @"builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(""DefaultConnection""),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)
    ));"
            },
            new CodeSnippet
            {
                Title = "ASP.NET Core RESTful Controller Template",
                Description = "Mẫu chuẩn cho API Controller CRUD trong ASP.NET Core",
                Language = "csharp",
                CategoryName = "ASP.NET Core",
                Code = @"[ApiController]
[Route(""api/[controller]"")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _userService.GetAllAsync());

    [HttpGet(""{id:int}"")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return user != null ? Ok(user) : NotFound();
    }
}"
            }
        };

        context.CodeSnippets.AddRange(snippets);
        await context.SaveChangesAsync();
        #endregion
    }

    private static async Task SeedCodingChallengesAsync(AppDbContext context)
    {
        var catCsharp = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "csharp-co-ban");
        var catLinq = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "linq-efcore");

        var challenge1 = new CodingChallenge
        {
            Title = "1. Tính tổng hai số nguyên",
            Slug = "tinh-tong-hai-so",
            ShortDescription = "Làm quen với cấu trúc hàm C# bằng cách viết phương thức tính tổng hai số nguyên.",
            InstructionsMarkdown = @"### Đề bài
Viết hàm `Sum(int a, int b)` nhận vào hai số nguyên `a` và `b`. Trả về tổng của hai số đó.

#### Ví dụ 1:
- **Đầu vào**: `a = 5, b = 10`
- **Đầu ra**: `15`

#### Ví dụ 2:
- **Đầu vào**: `a = -3, b = 8`
- **Đầu ra**: `5`",
            InitialCode = @"public class Solution
{
    public static int Sum(int a, int b)
    {
        // Viết mã xử lý của bạn ở đây
        return a + b;
    }
}",
            SolutionCode = @"public class Solution
{
    public static int Sum(int a, int b) => a + b;
}",
            Difficulty = DifficultyLevel.Beginner,
            CategoryId = catCsharp?.Id,
            XpReward = 30,
            OrderIndex = 1,
            TestCases = new List<CodeTestCase>
            {
                new() { InputParameters = "5, 10", ExpectedOutput = "15", IsHidden = false, Explanation = "5 + 10 = 15" },
                new() { InputParameters = "-3, 8", ExpectedOutput = "5", IsHidden = false, Explanation = "-3 + 8 = 5" },
                new() { InputParameters = "100, -200", ExpectedOutput = "-100", IsHidden = true },
                new() { InputParameters = "0, 0", ExpectedOutput = "0", IsHidden = true }
            }
        };

        var challenge2 = new CodingChallenge
        {
            Title = "2. Đảo ngược chuỗi ký tự",
            Slug = "dao-nguoc-chuoi",
            ShortDescription = "Xử lý chuỗi ký tự cơ bản trong C# và làm quen với mảng ký tự hoặc LINQ.",
            InstructionsMarkdown = @"### Đề bài
Viết hàm `ReverseString(string s)` nhận vào một chuỗi ký tự `s` và trả về chuỗi đảo ngược của nó.

#### Ví dụ 1:
- **Đầu vào**: `s = ""hello""`
- **Đầu ra**: `""olleh""`

#### Ví dụ 2:
- **Đầu vào**: `s = ""csharp""`
- **Đầu ra**: `""prahsc""`",
            InitialCode = @"public class Solution
{
    public static string ReverseString(string s)
    {
        // Viết mã xử lý của bạn ở đây
        return """";
    }
}",
            SolutionCode = @"public class Solution
{
    public static string ReverseString(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        char[] arr = s.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}",
            Difficulty = DifficultyLevel.Beginner,
            CategoryId = catCsharp?.Id,
            XpReward = 40,
            OrderIndex = 2,
            TestCases = new List<CodeTestCase>
            {
                new() { InputParameters = "\"hello\"", ExpectedOutput = "olleh", IsHidden = false },
                new() { InputParameters = "\"csharp\"", ExpectedOutput = "prahsc", IsHidden = false },
                new() { InputParameters = "\"12345\"", ExpectedOutput = "54321", IsHidden = true },
                new() { InputParameters = "\"a\"", ExpectedOutput = "a", IsHidden = true }
            }
        };

        var challenge3 = new CodingChallenge
        {
            Title = "3. Lọc và sắp xếp số chẵn với LINQ",
            Slug = "loc-so-chan-linq",
            ShortDescription = "Sử dụng toán tử Where và OrderBy trong LINQ để xử lý mảng số nguyên.",
            InstructionsMarkdown = @"### Đề bài
Viết hàm `FilterEvens(int[] numbers)` nhận vào một mảng số nguyên `numbers`. Sử dụng LINQ để lọc ra các số chẵn và sắp xếp theo thứ tự tăng dần.

#### Ví dụ 1:
- **Đầu vào**: `numbers = [1, 2, 3, 4, 5, 6]`
- **Đầu ra**: `2, 4, 6`

#### Ví dụ 2:
- **Đầu vào**: `numbers = [10, 3, 8, 1, 4]`
- **Đầu ra**: `4, 8, 10`",
            InitialCode = @"using System.Linq;
using System.Collections.Generic;

public class Solution
{
    public static IEnumerable<int> FilterEvens(int[] numbers)
    {
        // Sử dụng LINQ để lọc số chẵn và sắp xếp tăng dần
        return new List<int>();
    }
}",
            SolutionCode = @"using System.Linq;
using System.Collections.Generic;

public class Solution
{
    public static IEnumerable<int> FilterEvens(int[] numbers)
    {
        return numbers.Where(n => n % 2 == 0).OrderBy(n => n);
    }
}",
            Difficulty = DifficultyLevel.Intermediate,
            CategoryId = catLinq?.Id,
            XpReward = 50,
            OrderIndex = 3,
            TestCases = new List<CodeTestCase>
            {
                new() { InputParameters = "new int[] { 1, 2, 3, 4, 5, 6 }", ExpectedOutput = "2, 4, 6", IsHidden = false },
                new() { InputParameters = "new int[] { 7, 9, 11 }", ExpectedOutput = "", IsHidden = false },
                new() { InputParameters = "new int[] { 10, 3, 8, 1, 4 }", ExpectedOutput = "4, 8, 10", IsHidden = true }
            }
        };

        var challenge4 = new CodingChallenge
        {
            Title = "4. Kiểm tra số nguyên tố",
            Slug = "kiem-tra-so-nguyen-to",
            ShortDescription = "Thuật toán kiểm tra số nguyên tố tối ưu căn bậc hai O(sqrt(n)).",
            InstructionsMarkdown = @"### Đề bài
Viết hàm `IsPrime(int n)` kiểm tra xem số nguyên $n$ có phải là số nguyên tố hay không. Trả về `true` nếu là số nguyên tố, ngược lại trả về `false`.
*Lưu ý: Số nguyên tố là số nguyên lớn hơn 1 và chỉ chia hết cho 1 và chính nó.*

#### Ví dụ 1:
- **Đầu vào**: `n = 7`
- **Đầu ra**: `True`

#### Ví dụ 2:
- **Đầu vào**: `n = 4`
- **Đầu ra**: `False`",
            InitialCode = @"public class Solution
{
    public static bool IsPrime(int n)
    {
        // Viết thuật toán kiểm tra số nguyên tố
        return false;
    }
}",
            SolutionCode = @"public class Solution
{
    public static bool IsPrime(int n)
    {
        if (n <= 1) return false;
        if (n <= 3) return true;
        if (n % 2 == 0 || n % 3 == 0) return false;
        for (int i = 5; i * i <= n; i += 6)
        {
            if (n % i == 0 || n % (i + 2) == 0) return false;
        }
        return true;
    }
}",
            Difficulty = DifficultyLevel.Intermediate,
            CategoryId = catCsharp?.Id,
            XpReward = 50,
            OrderIndex = 4,
            TestCases = new List<CodeTestCase>
            {
                new() { InputParameters = "7", ExpectedOutput = "True", IsHidden = false },
                new() { InputParameters = "4", ExpectedOutput = "False", IsHidden = false },
                new() { InputParameters = "1", ExpectedOutput = "False", IsHidden = true },
                new() { InputParameters = "29", ExpectedOutput = "True", IsHidden = true },
                new() { InputParameters = "97", ExpectedOutput = "True", IsHidden = true }
            }
        };

        var challenge5 = new CodingChallenge
        {
            Title = "5. Tìm số Fibonacci thứ n",
            Slug = "tim-so-fibonacci",
            ShortDescription = "Tính toán giá trị Fibonacci thứ n với độ phức tạp tối ưu O(n).",
            InstructionsMarkdown = @"### Đề bài
Dãy số Fibonacci được định nghĩa: $F(0) = 0$, $F(1) = 1$, và $F(n) = F(n-1) + F(n-2)$ với mọi $n \ge 2$.
Viết hàm `Fibonacci(int n)` trả về số Fibonacci thứ $n$.

#### Ví dụ 1:
- **Đầu vào**: `n = 6`
- **Đầu ra**: `8` (Dãy số: 0, 1, 1, 2, 3, 5, 8)

#### Ví dụ 2:
- **Đầu vào**: `n = 10`
- **Đầu ra**: `55`",
            InitialCode = @"public class Solution
{
    public static long Fibonacci(int n)
    {
        // Viết thuật toán tính Fibonacci
        return 0;
    }
}",
            SolutionCode = @"public class Solution
{
    public static long Fibonacci(int n)
    {
        if (n <= 0) return 0;
        if (n == 1) return 1;
        long a = 0, b = 1;
        for (int i = 2; i <= n; i++)
        {
            long temp = a + b;
            a = b;
            b = temp;
        }
        return b;
    }
}",
            Difficulty = DifficultyLevel.Advanced,
            CategoryId = catCsharp?.Id,
            XpReward = 60,
            OrderIndex = 5,
            TestCases = new List<CodeTestCase>
            {
                new() { InputParameters = "0", ExpectedOutput = "0", IsHidden = false },
                new() { InputParameters = "6", ExpectedOutput = "8", IsHidden = false },
                new() { InputParameters = "10", ExpectedOutput = "55", IsHidden = true },
                new() { InputParameters = "20", ExpectedOutput = "6765", IsHidden = true }
            }
        };

        context.CodingChallenges.AddRange(challenge1, challenge2, challenge3, challenge4, challenge5);
        await context.SaveChangesAsync();
    }

    private static async Task EnsurePlaygroundTablesExistAsync(AppDbContext context)
    {
        if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""CodingChallenges"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CodingChallenges"" PRIMARY KEY AUTOINCREMENT,
                    ""Title"" TEXT NOT NULL,
                    ""Slug"" TEXT NOT NULL,
                    ""ShortDescription"" TEXT NOT NULL,
                    ""InstructionsMarkdown"" TEXT NOT NULL,
                    ""InitialCode"" TEXT NOT NULL,
                    ""SolutionCode"" TEXT NULL,
                    ""Difficulty"" INTEGER NOT NULL,
                    ""CategoryId"" INTEGER NULL,
                    ""XpReward"" INTEGER NOT NULL,
                    ""OrderIndex"" INTEGER NOT NULL,
                    CONSTRAINT ""FK_CodingChallenges_Categories_CategoryId"" FOREIGN KEY (""CategoryId"") REFERENCES ""Categories"" (""Id"") ON DELETE SET NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CodingChallenges_Slug"" ON ""CodingChallenges"" (""Slug"");

                CREATE TABLE IF NOT EXISTS ""CodeTestCases"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CodeTestCases"" PRIMARY KEY AUTOINCREMENT,
                    ""CodingChallengeId"" INTEGER NOT NULL,
                    ""InputParameters"" TEXT NOT NULL,
                    ""ExpectedOutput"" TEXT NOT NULL,
                    ""IsHidden"" INTEGER NOT NULL,
                    ""Explanation"" TEXT NULL,
                    CONSTRAINT ""FK_CodeTestCases_CodingChallenges_CodingChallengeId"" FOREIGN KEY (""CodingChallengeId"") REFERENCES ""CodingChallenges"" (""Id"") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ""CodeSubmissions"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CodeSubmissions"" PRIMARY KEY AUTOINCREMENT,
                    ""UserId"" TEXT NOT NULL,
                    ""CodingChallengeId"" INTEGER NOT NULL,
                    ""SubmittedCode"" TEXT NOT NULL,
                    ""IsPassed"" INTEGER NOT NULL,
                    ""PassedTestsCount"" INTEGER NOT NULL,
                    ""TotalTestsCount"" INTEGER NOT NULL,
                    ""ExecutionTimeMs"" INTEGER NOT NULL,
                    ""XpEarned"" INTEGER NOT NULL,
                    ""SubmittedAt"" TEXT NOT NULL,
                    CONSTRAINT ""FK_CodeSubmissions_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_CodeSubmissions_CodingChallenges_CodingChallengeId"" FOREIGN KEY (""CodingChallengeId"") REFERENCES ""CodingChallenges"" (""Id"") ON DELETE CASCADE
                );
            ");
        }
        else if (context.Database.IsSqlServer())
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CodingChallenges')
                BEGIN
                    CREATE TABLE [CodingChallenges] (
                        [Id] int NOT NULL IDENTITY,
                        [Title] nvarchar(200) NOT NULL,
                        [Slug] nvarchar(200) NOT NULL,
                        [ShortDescription] nvarchar(500) NOT NULL,
                        [InstructionsMarkdown] nvarchar(max) NOT NULL,
                        [InitialCode] nvarchar(max) NOT NULL,
                        [SolutionCode] nvarchar(max) NULL,
                        [Difficulty] int NOT NULL,
                        [CategoryId] int NULL,
                        [XpReward] int NOT NULL,
                        [OrderIndex] int NOT NULL,
                        CONSTRAINT [PK_CodingChallenges] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CodingChallenges_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE SET NULL
                    );
                    CREATE UNIQUE INDEX [IX_CodingChallenges_Slug] ON [CodingChallenges] ([Slug]);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CodeTestCases')
                BEGIN
                    CREATE TABLE [CodeTestCases] (
                        [Id] int NOT NULL IDENTITY,
                        [CodingChallengeId] int NOT NULL,
                        [InputParameters] nvarchar(500) NOT NULL,
                        [ExpectedOutput] nvarchar(500) NOT NULL,
                        [IsHidden] bit NOT NULL,
                        [Explanation] nvarchar(500) NULL,
                        CONSTRAINT [PK_CodeTestCases] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CodeTestCases_CodingChallenges_CodingChallengeId] FOREIGN KEY ([CodingChallengeId]) REFERENCES [CodingChallenges] ([Id]) ON DELETE CASCADE
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CodeSubmissions')
                BEGIN
                    CREATE TABLE [CodeSubmissions] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [CodingChallengeId] int NOT NULL,
                        [SubmittedCode] nvarchar(max) NOT NULL,
                        [IsPassed] bit NOT NULL,
                        [PassedTestsCount] int NOT NULL,
                        [TotalTestsCount] int NOT NULL,
                        [ExecutionTimeMs] bigint NOT NULL,
                        [XpEarned] int NOT NULL,
                        [SubmittedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_CodeSubmissions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CodeSubmissions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_CodeSubmissions_CodingChallenges_CodingChallengeId] FOREIGN KEY ([CodingChallengeId]) REFERENCES [CodingChallenges] ([Id]) ON DELETE CASCADE
                    );
                END
            ");
        }
    }

    private static async Task EnsureDiscussionAndGamificationTablesExistAsync(AppDbContext context)
    {
        bool isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        if (isSqlite)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS [DiscussionComments] (
                    [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [TutorialId] INTEGER NULL,
                    [CodingChallengeId] INTEGER NULL,
                    [UserId] TEXT NOT NULL,
                    [ParentCommentId] INTEGER NULL,
                    [ContentMarkdown] TEXT NOT NULL,
                    [UpvotesCount] INTEGER NOT NULL DEFAULT 0,
                    [IsPinned] INTEGER NOT NULL DEFAULT 0,
                    [IsBestAnswer] INTEGER NOT NULL DEFAULT 0,
                    [CreatedAt] TEXT NOT NULL,
                    [UpdatedAt] TEXT NULL,
                    FOREIGN KEY ([TutorialId]) REFERENCES [Tutorials] ([Id]) ON DELETE CASCADE,
                    FOREIGN KEY ([CodingChallengeId]) REFERENCES [CodingChallenges] ([Id]) ON DELETE CASCADE,
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                    FOREIGN KEY ([ParentCommentId]) REFERENCES [DiscussionComments] ([Id]) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS [CommentUpvotes] (
                    [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [CommentId] INTEGER NOT NULL,
                    [UserId] TEXT NOT NULL,
                    [CreatedAt] TEXT NOT NULL,
                    FOREIGN KEY ([CommentId]) REFERENCES [DiscussionComments] ([Id]) ON DELETE CASCADE,
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS [IX_CommentUpvotes_CommentId_UserId] 
                ON [CommentUpvotes] ([CommentId], [UserId]);

                CREATE TABLE IF NOT EXISTS [UserBadges] (
                    [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                    [UserId] TEXT NOT NULL,
                    [BadgeCode] TEXT NOT NULL,
                    [Title] TEXT NOT NULL,
                    [Description] TEXT NULL,
                    [IconClass] TEXT NOT NULL DEFAULT 'bi-award',
                    [ColorClass] TEXT NOT NULL DEFAULT 'primary',
                    [EarnedAt] TEXT NOT NULL,
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS [IX_UserBadges_UserId_BadgeCode] 
                ON [UserBadges] ([UserId], [BadgeCode]);
            ");
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DiscussionComments')
                BEGIN
                    CREATE TABLE [DiscussionComments] (
                        [Id] int NOT NULL IDENTITY,
                        [TutorialId] int NULL,
                        [CodingChallengeId] int NULL,
                        [UserId] nvarchar(450) NOT NULL,
                        [ParentCommentId] int NULL,
                        [ContentMarkdown] nvarchar(max) NOT NULL,
                        [UpvotesCount] int NOT NULL DEFAULT 0,
                        [IsPinned] bit NOT NULL DEFAULT 0,
                        [IsBestAnswer] bit NOT NULL DEFAULT 0,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_DiscussionComments] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_DiscussionComments_Tutorials_TutorialId] FOREIGN KEY ([TutorialId]) REFERENCES [Tutorials] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_DiscussionComments_CodingChallenges_CodingChallengeId] FOREIGN KEY ([CodingChallengeId]) REFERENCES [CodingChallenges] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_DiscussionComments_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_DiscussionComments_DiscussionComments_ParentCommentId] FOREIGN KEY ([ParentCommentId]) REFERENCES [DiscussionComments] ([Id])
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommentUpvotes')
                BEGIN
                    CREATE TABLE [CommentUpvotes] (
                        [Id] int NOT NULL IDENTITY,
                        [CommentId] int NOT NULL,
                        [UserId] nvarchar(450) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_CommentUpvotes] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CommentUpvotes_DiscussionComments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [DiscussionComments] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_CommentUpvotes_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_CommentUpvotes_CommentId_UserId] ON [CommentUpvotes] ([CommentId], [UserId]);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserBadges')
                BEGIN
                    CREATE TABLE [UserBadges] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [BadgeCode] nvarchar(50) NOT NULL,
                        [Title] nvarchar(150) NOT NULL,
                        [Description] nvarchar(300) NULL,
                        [IconClass] nvarchar(50) NOT NULL DEFAULT 'bi-award',
                        [ColorClass] nvarchar(50) NOT NULL DEFAULT 'primary',
                        [EarnedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_UserBadges] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserBadges_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_UserBadges_UserId_BadgeCode] ON [UserBadges] ([UserId], [BadgeCode]);
                END
            ");
        }
    }

    private static async Task EnsureStreakTablesExistAsync(AppDbContext context)
    {
        var isSqlite = context.Database.IsSqlite();
        if (isSqlite)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE [AspNetUsers] ADD COLUMN [CurrentStreak] INTEGER NOT NULL DEFAULT 0;");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE [AspNetUsers] ADD COLUMN [LongestStreak] INTEGER NOT NULL DEFAULT 0;");
            }
            catch { }

            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE [AspNetUsers] ADD COLUMN [LastCheckInDate] TEXT NULL;");
            }
            catch { }

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS [DailyCheckIns] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [UserId] TEXT NOT NULL,
                    [CheckInDate] TEXT NOT NULL,
                    [StreakDay] INTEGER NOT NULL DEFAULT 1,
                    [XpEarned] INTEGER NOT NULL DEFAULT 10,
                    [CreatedAt] TEXT NOT NULL,
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS [IX_DailyCheckIns_UserId_CheckInDate]
                ON [DailyCheckIns] ([UserId], [CheckInDate]);
            ");
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'CurrentStreak')
                    ALTER TABLE [AspNetUsers] ADD [CurrentStreak] int NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LongestStreak')
                    ALTER TABLE [AspNetUsers] ADD [LongestStreak] int NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastCheckInDate')
                    ALTER TABLE [AspNetUsers] ADD [LastCheckInDate] datetime2 NULL;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailyCheckIns')
                BEGIN
                    CREATE TABLE [DailyCheckIns] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [CheckInDate] datetime2 NOT NULL,
                        [StreakDay] int NOT NULL DEFAULT 1,
                        [XpEarned] int NOT NULL DEFAULT 10,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_DailyCheckIns] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_DailyCheckIns_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_DailyCheckIns_UserId_CheckInDate] ON [DailyCheckIns] ([UserId], [CheckInDate]);
                END
            ");
        }
    }

    private static async Task EnsureNotificationAndActivityTablesExistAsync(AppDbContext context)
    {
        var isSqlite = context.Database.IsSqlite();
        if (isSqlite)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS [UserNotifications] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [UserId] TEXT NOT NULL,
                    [Title] TEXT NOT NULL,
                    [Message] TEXT NOT NULL,
                    [TargetUrl] TEXT NULL,
                    [Type] INTEGER NOT NULL DEFAULT 0,
                    [IconClass] TEXT NULL,
                    [ColorClass] TEXT NULL,
                    [IsRead] INTEGER NOT NULL DEFAULT 0,
                    [ReadAt] TEXT NULL,
                    [CreatedAt] TEXT NOT NULL,
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS [IX_UserNotifications_UserId_IsRead]
                ON [UserNotifications] ([UserId], [IsRead]);

                CREATE TABLE IF NOT EXISTS [ActivityFeedItems] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [UserId] TEXT NULL,
                    [UserDisplayName] TEXT NOT NULL,
                    [UserAvatar] TEXT NULL,
                    [Type] INTEGER NOT NULL DEFAULT 0,
                    [Title] TEXT NOT NULL,
                    [Description] TEXT NOT NULL,
                    [TargetUrl] TEXT NULL,
                    [XpEarned] INTEGER NOT NULL DEFAULT 0,
                    [BadgeCode] TEXT NULL,
                    [CreatedAt] TEXT NOT NULL,
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
                );

                CREATE INDEX IF NOT EXISTS [IX_ActivityFeedItems_CreatedAt]
                ON [ActivityFeedItems] ([CreatedAt]);
            ");
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserNotifications')
                BEGIN
                    CREATE TABLE [UserNotifications] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [Title] nvarchar(250) NOT NULL,
                        [Message] nvarchar(1000) NOT NULL,
                        [TargetUrl] nvarchar(500) NULL,
                        [Type] int NOT NULL DEFAULT 0,
                        [IconClass] nvarchar(50) NULL,
                        [ColorClass] nvarchar(50) NULL,
                        [IsRead] bit NOT NULL DEFAULT 0,
                        [ReadAt] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_UserNotifications] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserNotifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_UserNotifications_UserId_IsRead] ON [UserNotifications] ([UserId], [IsRead]);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ActivityFeedItems')
                BEGIN
                    CREATE TABLE [ActivityFeedItems] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NULL,
                        [UserDisplayName] nvarchar(150) NOT NULL,
                        [UserAvatar] nvarchar(500) NULL,
                        [Type] int NOT NULL DEFAULT 0,
                        [Title] nvarchar(250) NOT NULL,
                        [Description] nvarchar(500) NOT NULL,
                        [TargetUrl] nvarchar(500) NULL,
                        [XpEarned] int NOT NULL DEFAULT 0,
                        [BadgeCode] nvarchar(50) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_ActivityFeedItems] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ActivityFeedItems_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_ActivityFeedItems_CreatedAt] ON [ActivityFeedItems] ([CreatedAt]);
                END
            ");
        }
    }

    private static async Task SeedInitialDiscussionsAndBadgesAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        var admin = await userManager.FindByEmailAsync("admin@nettutos.com");
        if (admin == null) return;

        // Seed Admin badges
        var badges = new List<UserBadge>
        {
            new() { UserId = admin.Id, BadgeCode = "NEWBIE", Title = "Tân Binh .NET", Description = "Bắt đầu hành trình và hoàn thành bài học lý thuyết đầu tiên", IconClass = "bi-rocket-takeoff-fill", ColorClass = "primary", EarnedAt = DateTime.UtcNow },
            new() { UserId = admin.Id, BadgeCode = "SCHOLAR", Title = "Học Giả Chăm Chỉ", Description = "Kiên trì tích lũy kiến thức và hoàn thành từ 5 bài học", IconClass = "bi-book-half", ColorClass = "info", EarnedAt = DateTime.UtcNow },
            new() { UserId = admin.Id, BadgeCode = "CERTIFIED", Title = "Bậc Thầy Chứng Chỉ", Description = "Vượt qua kỳ thi tốt nghiệp và nhận chứng chỉ số chính thức", IconClass = "bi-award-fill", ColorClass = "success", EarnedAt = DateTime.UtcNow },
            new() { UserId = admin.Id, BadgeCode = "COMMUNITY_HERO", Title = "Người Truyền Lửa", Description = "Tích cực đóng góp lời giải và hỗ trợ cộng đồng học viên", IconClass = "bi-chat-heart-fill", ColorClass = "purple", EarnedAt = DateTime.UtcNow },
            new() { UserId = admin.Id, BadgeCode = "STREAK_3", Title = "Ngọn Lửa Bền Bỉ", Description = "Duy trì chuỗi học tập 3 ngày liên tục", IconClass = "bi-fire", ColorClass = "danger", EarnedAt = DateTime.UtcNow },
            new() { UserId = admin.Id, BadgeCode = "STREAK_7", Title = "Chiến Binh Kỷ Luật", Description = "Duy trì chuỗi học tập 7 ngày liên tiếp không nghỉ", IconClass = "bi-shield-check", ColorClass = "warning", EarnedAt = DateTime.UtcNow },
            new() { UserId = admin.Id, BadgeCode = "STREAK_30", Title = "Huyền Thoại Bất Bại", Description = "Kỷ lục 30 ngày kiên trì học tập liên tục cùng .NET", IconClass = "bi-trophy-fill", ColorClass = "primary", EarnedAt = DateTime.UtcNow }
        };

        foreach (var b in badges)
        {
            if (!await context.UserBadges.AnyAsync(ub => ub.UserId == admin.Id && ub.BadgeCode == b.BadgeCode))
            {
                context.UserBadges.Add(b);
            }
        }

        var firstTutorial = await context.Tutorials.OrderBy(t => t.OrderIndex).FirstOrDefaultAsync();
        if (firstTutorial != null)
        {
            var pinnedComment = new DiscussionComment
            {
                TutorialId = firstTutorial.Id,
                UserId = admin.Id,
                ContentMarkdown = "👋 Chào mừng bạn đến với khu vực thảo luận của bài học! Nếu gặp bất kỳ vướng mắc nào về cú pháp C# hoặc thiết lập môi trường .NET, hãy để lại câu hỏi để được giải đáp nhanh chóng nhé.",
                IsPinned = true,
                UpvotesCount = 5,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };
            context.DiscussionComments.Add(pinnedComment);

            var questionComment = new DiscussionComment
            {
                TutorialId = firstTutorial.Id,
                UserId = admin.Id,
                ContentMarkdown = "💡 **Câu hỏi tham khảo**: Cho mình hỏi sự khác nhau căn bản giữa lệnh `dotnet --info` và `dotnet --version` trong Terminal là gì?",
                UpvotesCount = 2,
                CreatedAt = DateTime.UtcNow.AddMinutes(-45)
            };
            context.DiscussionComments.Add(questionComment);
            await context.SaveChangesAsync();

            var replyComment = new DiscussionComment
            {
                TutorialId = firstTutorial.Id,
                UserId = admin.Id,
                ParentCommentId = questionComment.Id,
                ContentMarkdown = "`dotnet --version` chỉ in ra phiên bản SDK đang kích hoạt hiện tại. Trong khi `dotnet --info` sẽ liệt kê toàn bộ thông tin chi tiết về môi trường: kiến trúc hệ điều hành (x64/ARM), danh sách tất cả các .NET SDK và .NET Runtime đã cài đặt trên máy.",
                IsBestAnswer = true,
                UpvotesCount = 4,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            };
            context.DiscussionComments.Add(replyComment);
        }

        // Initialize sample streak for admin if not already set
        if (admin.CurrentStreak == 0)
        {
            admin.CurrentStreak = 5;
            admin.LongestStreak = 5;
            admin.LastCheckInDate = DateTime.UtcNow.Date;
            await userManager.UpdateAsync(admin);

            if (!await context.DailyCheckIns.AnyAsync(d => d.UserId == admin.Id))
            {
                var today = DateTime.UtcNow.Date;
                for (int i = 4; i >= 0; i--)
                {
                    context.DailyCheckIns.Add(new DailyCheckIn
                    {
                        UserId = admin.Id,
                        CheckInDate = today.AddDays(-i),
                        StreakDay = 5 - i,
                        XpEarned = 20,
                        CreatedAt = DateTime.UtcNow.AddDays(-i)
                    });
                }
            }
        }

        // Seed sample notifications for admin if none exist
        if (!await context.UserNotifications.AnyAsync(n => n.UserId == admin.Id))
        {
            context.UserNotifications.AddRange(
                new UserNotification
                {
                    UserId = admin.Id,
                    Title = "Chào mừng bạn đến với NET-Tutos! 🚀",
                    Message = "Chúc mừng bạn đã gia nhập nền tảng học tập C# & .NET chuyên sâu. Hãy bắt đầu bài học đầu tiên ngay!",
                    Type = NotificationType.General,
                    IconClass = "bi-rocket-takeoff-fill",
                    ColorClass = "text-primary",
                    TargetUrl = "/Tutorials",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                },
                new UserNotification
                {
                    UserId = admin.Id,
                    Title = "Chuỗi học tập đạt 5 ngày liên tục! 🔥",
                    Message = "Bạn đã duy trì ngọn lửa học tập kiên trì 5 ngày liên tiếp. Tiếp tục phát huy nhé!",
                    Type = NotificationType.StreakReminder,
                    IconClass = "bi-fire",
                    ColorClass = "text-danger",
                    TargetUrl = "/Streak",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new UserNotification
                {
                    UserId = admin.Id,
                    Title = "Mở khóa huy hiệu 'Huyền Thoại Bất Bại' 🏆",
                    Message = "Bạn vừa đạt thành tích vinh danh danh giá trên Bảng Xếp Hạng học viên.",
                    Type = NotificationType.BadgeEarned,
                    IconClass = "bi-trophy-fill",
                    ColorClass = "text-warning",
                    TargetUrl = "/Leaderboard",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                }
            );
        }

        // Seed sample community activities if none exist
        if (!await context.ActivityFeedItems.AnyAsync())
        {
            context.ActivityFeedItems.AddRange(
                new ActivityFeedItem
                {
                    UserId = admin.Id,
                    UserDisplayName = "Quản trị viên Hệ thống",
                    Type = ActivityType.BadgeEarned,
                    Title = "đã mở khóa huy hiệu",
                    Description = "Huyền Thoại Bất Bại (30 ngày kiên trì học tập)",
                    TargetUrl = "/Leaderboard",
                    XpEarned = 100,
                    BadgeCode = "STREAK_30",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20)
                },
                new ActivityFeedItem
                {
                    UserId = admin.Id,
                    UserDisplayName = "Quản trị viên Hệ thống",
                    Type = ActivityType.StreakAchieved,
                    Title = "đã đạt chuỗi ngọn lửa học tập",
                    Description = "5 ngày học tập liên tục không ngắt quãng 🔥",
                    TargetUrl = "/Streak",
                    XpEarned = 35,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new ActivityFeedItem
                {
                    UserId = admin.Id,
                    UserDisplayName = "Quản trị viên Hệ thống",
                    Type = ActivityType.ChallengeSolved,
                    Title = "đã giải thành công thử thách thuật toán",
                    Description = "Hai Con Số (Two Sum) - C# Algorithmic Mastery",
                    TargetUrl = "/Playground/Challenges",
                    XpEarned = 30,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new ActivityFeedItem
                {
                    UserId = admin.Id,
                    UserDisplayName = "Quản trị viên Hệ thống",
                    Type = ActivityType.LessonCompleted,
                    Title = "đã hoàn thành bài học",
                    Description = "Bài 1: Tổng quan hệ sinh thái .NET & Cài đặt môi trường",
                    TargetUrl = "/bai-hoc/tong-quan-he-sinh-thai-dotnet-cai-dat-moi-truong",
                    XpEarned = 20,
                    CreatedAt = DateTime.UtcNow.AddHours(-4)
                }
            );
        }

        await context.SaveChangesAsync();
    }
}

