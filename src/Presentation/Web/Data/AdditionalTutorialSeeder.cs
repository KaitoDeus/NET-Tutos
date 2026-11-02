using Microsoft.EntityFrameworkCore;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Data;

public static class AdditionalTutorialSeeder
{
    public static async Task SeedAdditionalTutorialsAsync(AppDbContext context)
    {
        var catBeginner = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "csharp-co-ban");
        var catOop = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "oop-csharp-nang-cao");
        var catEf = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "entity-framework-core");
        var catAspNetCore = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "aspnet-core-mvc-api");

        if (catBeginner == null || catOop == null || catEf == null || catAspNetCore == null)
        {
            return;
        }

        var newTutorials = new List<Tutorial>();

        #region Bài 12: Phương thức (Methods) & Tham số trong C# (ref, out, in, params)
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "phuong-thuc-va-tham-so-trong-csharp-ref-out-in-params"))
        {
            var t12 = new Tutorial
            {
                CategoryId = catBeginner.Id,
                Title = "Bài 12: Phương thức (Methods) & Kỹ thuật Truyền Tham số trong C#",
                Slug = "phuong-thuc-va-tham-so-trong-csharp-ref-out-in-params",
                Summary = "Làm chủ cách khai báo method, phân biệt tham trị (by value) vs tham chiếu (by reference), tối ưu với in, trả về nhiều giá trị với out và mảng linh hoạt với params.",
                EstimatedReadingMinutes = 9,
                Difficulty = DifficultyLevel.Beginner,
                OrderIndex = 12,
                IsFeatured = true,
                ContentMarkdown = @"# Phương thức (Methods) & Kỹ thuật Truyền Tham số trong C#

Phương thức (Method) là một khối lệnh thực thi một nhiệm vụ cụ thể, giúp tái sử dụng mã nguồn và chia nhỏ chương trình thành các mô-đun mạch lạc. Trong C#, cơ chế truyền tham số rất phong phú với các từ khóa `ref`, `out`, `in` và `params`.

---

## 1. Cấu trúc khai báo phương thức chuẩn

```csharp
[Access_Modifier] [Modifiers] Return_Type Method_Name(Parameter_List)
{
    // Thân phương thức
    return value; // nếu Return_Type khác void
}
```

Ví dụ cơ bản:
```csharp
public class Calculator
{
    // Phương thức truyền tham trị thông thường
    public int Cong(int a, int b)
    {
        return a + b;
    }

    // Expression-bodied method (cú pháp rút gọn C# hiện đại)
    public double Chia(double a, double b) => b != 0 ? a / b : throw new DivideByZeroException();
}
```

---

## 2. Truyền tham trị (Pass by Value) vs Truyền tham chiếu (Pass by Reference)

Mặc định trong C#, mọi đối số được truyền theo **Tham trị (By Value)**. Bản sao của dữ liệu được tạo ra và truyền vào phương thức. Mọi thay đổi bên trong phương thức không ảnh hưởng đến biến gốc bên ngoài.

Khi muốn phương thức thay đổi trực tiếp giá trị của biến gốc bên ngoài, ta sử dụng **Truyền tham chiếu (By Reference)**.

```
┌──────────────────────────────────────────────────────────────┐
│                    CÁC TỪ KHÓA THAM SỐ C#                    │
├────────┬─────────────────────────────┬───────────────────────┤
│ Từ khóa│ Khởi tạo trước khi truyền?  │ Phương thức bắt buộc? │
├────────┼─────────────────────────────┼───────────────────────┤
│ ref    │ BẮT BUỘC có giá trị trước   │ Đọc và ghi tự do      │
│ out    │ KHÔNG BẮT BUỘC có giá trị   │ BẮT BUỘC gán giá trị  │
│ in     │ BẮT BUỘC có giá trị trước   │ CHỈ ĐỌC (Read-only)   │
│ params │ Danh sách hoặc mảng linh hoạt│ Không giới hạn số lượng│
└────────┴─────────────────────────────┴───────────────────────┘
```

---

## 3. Từ khóa `ref` (Reference)

Dùng khi biến đã có giá trị ban đầu và bạn muốn phương thức có thể đọc cũng như ghi đè giá trị mới:

```csharp
void HoanDoi(ref int a, ref int b)
{
    int temp = a;
    a = b;
    b = temp;
}

int x = 10, y = 20;
HoanDoi(ref x, ref y);
Console.WriteLine($""x = {x}, y = {y}""); // Output: x = 20, y = 10
```

---

## 4. Từ khóa `out` (Output Parameter)

Thường dùng khi phương thức cần trả về nhiều hơn một giá trị, ví dụ điển hình là các hàm kiểm tra và chuyển đổi kiểu dữ liệu (`int.TryParse`):

```csharp
bool TinhToan(int a, int b, out int tong, out int tich)
{
    tong = a + b; // Bắt buộc phải gán giá trị trước khi return
    tich = a * b; // Bắt buộc phải gán giá trị
    return true;
}

// C# cho phép khai báo biến out ngay tại vị trí gọi:
if (TinhToan(5, 3, out int sum, out int product))
{
    Console.WriteLine($""Tổng: {sum}, Tích: {product}""); // Output: Tổng: 8, Tích: 15
}
```

---

## 5. Từ khóa `in` (Read-only Reference)

Từ khóa `in` truyền tham chiếu nhưng **cấm** phương thức sửa đổi giá trị. Nó đặc biệt hữu dụng khi truyền các `struct` kích thước lớn để tránh chi phí sao chép bộ nhớ trên Stack mà vẫn đảm bảo tính bất biến:

```csharp
public struct ToaDo3D
{
    public double X, Y, Z;
}

// Truyền 'in' giúp tiết kiệm bộ nhớ sao chép struct
double TinhKhoangCach(in ToaDo3D p1, in ToaDo3D p2)
{
    // p1.X = 100; // LỖI BIÊN DỊCH: Biến 'in' là read-only!
    double dx = p1.X - p2.X;
    double dy = p1.Y - p2.Y;
    double dz = p1.Z - p2.Z;
    return Math.Sqrt(dx * dx + dy * dy + dz * dz);
}
```

---

## 6. Từ khóa `params` (Mảng tham số linh hoạt)

Cho phép phương thức nhận số lượng đối số tùy ý cách nhau bằng dấu phẩy:

```csharp
int TinhTongTatCa(params int[] numbers)
{
    int total = 0;
    foreach (var n in numbers) total += n;
    return total;
}

// Gọi linh hoạt:
int k1 = TinhTongTatCa(1, 2, 3);          // 6
int k2 = TinhTongTatCa(10, 20, 30, 40);   // 100
int k3 = TinhTongTatCa();                 // 0
```

---

## 7. Local Functions (Hàm cục bộ trong hàm)

Từ C# 7.0, bạn có thể khai báo một hàm nằm ngay bên trong một hàm khác. Kỹ thuật này rất hay dùng để kiểm tra tính hợp lệ của tham số ngay lập tức trước khi thực thi tác vụ nặng:

```csharp
public IEnumerable<int> LayDanhSachSoChan(int batDau, int ketThuc)
{
    if (batDau > ketThuc) 
        throw new ArgumentException(""Điểm bắt đầu phải nhỏ hơn kết thúc!"");

    return ThucThiIterator();

    // Local function cô lập logic yield return
    IEnumerable<int> ThucThiIterator()
    {
        for (int i = batDau; i <= ketThuc; i++)
        {
            if (i % 2 == 0) yield return i;
        }
    }
}
```"
            };

            t12.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Sự khác biệt cốt lõi giữa từ khóa 'ref' và 'out' trong C# là gì?",
                OptionA = "'ref' yêu cầu biến phải được khởi tạo giá trị trước khi truyền vào; 'out' bắt buộc phương thức phải gán giá trị trước khi thoát.",
                OptionB = "'out' truyền tham trị còn 'ref' truyền tham chiếu.",
                OptionC = "'ref' chỉ dùng cho struct còn 'out' chỉ dùng cho class.",
                OptionD = "Cả hai hoàn toàn giống nhau, chỉ khác tên gọi cú pháp.",
                CorrectOption = "A",
                Explanation = "Biến truyền vào qua 'ref' bắt buộc phải có giá trị từ trước. Ngược lại, biến 'out' không cần khởi tạo trước nhưng phương thức nhận tham số bắt buộc phải gán giá trị cho nó trước khi return.",
                Difficulty = DifficultyLevel.Beginner
            });

            t12.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Từ khóa 'in' khi truyền tham số có tác dụng chính là gì?",
                OptionA = "Cho phép phương thức thay đổi giá trị của biến gốc.",
                OptionB = "Truyền tham chiếu chỉ đọc (Read-only Reference), tránh sao chép dữ liệu struct lớn nhưng không cho phép phương thức sửa đổi dữ liệu.",
                OptionC = "Tự động gán giá trị mặc định nếu tham số bị null.",
                OptionD = "Biến đổi tham số thành danh sách params.",
                CorrectOption = "B",
                Explanation = "'in' truyền bằng tham chiếu (địa chỉ bộ nhớ) giúp tiết kiệm tài nguyên khi truyền struct lớn, đồng thời trình biên dịch đảm bảo giá trị này không thể bị sửa đổi trong thân hàm.",
                Difficulty = DifficultyLevel.Beginner
            });

            newTutorials.Add(t12);
        }
        #endregion

        #region Bài 13: Xử lý Ngoại lệ Chuyên sâu (try-catch-finally & Exception Filters)
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "xu-ly-ngoai-le-chuyen-sau-try-catch-exception-filters"))
        {
            var t13 = new Tutorial
            {
                CategoryId = catBeginner.Id,
                Title = "Bài 13: Xử lý Ngoại lệ Chuyên sâu - try-catch-finally, Exception Filters & Custom Exceptions",
                Slug = "xu-ly-ngoai-le-chuyen-sau-try-catch-exception-filters",
                Summary = "Xây dựng ứng dụng kiên cố với hệ phân cấp Exception chuẩn .NET, khối try-catch-finally, mệnh đề lọc when và cách tạo Custom Exception chuyên nghiệp.",
                EstimatedReadingMinutes = 10,
                Difficulty = DifficultyLevel.Beginner,
                OrderIndex = 13,
                IsFeatured = true,
                ContentMarkdown = @"# Xử lý Ngoại lệ Chuyên sâu trong C#

Ngoại lệ (Exception) là các sự kiện bất thường xảy ra trong quá trình chương trình chạy, làm gián đoạn luồng thực thi thông thường. Viết code an toàn không phải là cầu mong lỗi không xảy ra, mà là dự đoán và xử lý lỗi một cách duyên dáng (Graceful Degradation).

---

## 1. Hệ phân cấp Exception trong .NET

Mọi ngoại lệ trong .NET đều kế thừa từ lớp cha `System.Exception`:

```
                    ┌─────────────────────────┐
                    │    System.Exception     │
                    └────────────┬────────────┘
                                 │
          ┌──────────────────────┴──────────────────────┐
          │                                             │
┌─────────────────────────┐                 ┌─────────────────────────┐
│     SystemException     │                 │   (Domain Exceptions)   │
└────────────┬────────────┘                 └─────────────────────────┘
             │
   ┌─────────┴─────────┬───────────────────┬────────────────────┐
   │                   │                   │                    │
┌──────────────┐ ┌──────────────┐ ┌──────────────────┐ ┌────────────────┐
│ NullReference│ │ ArgumentNull │ │ InvalidOperation │ │ FormatException│
└──────────────┘ └──────────────┘ └──────────────────┘ └────────────────┘
```

---

## 2. Cú pháp `try - catch - finally`

- **`try`**: Chứa đoạn mã có nguy cơ phát sinh lỗi.
- **`catch`**: Bắt và xử lý lỗi cụ thể.
- **`finally`**: Luôn luôn được thực thi dù có lỗi hay không (rất quan trọng để đóng file, ngắt kết nối mạng).

```csharp
FileStream? fs = null;
try
{
    fs = File.Open(""du_lieu.txt"", FileMode.Open);
    // Đọc dữ liệu...
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($""Không tìm thấy tệp tin: {ex.FileName}"");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($""Không có quyền truy cập tệp: {ex.Message}"");
}
finally
{
    // Luôn luôn giải phóng tài nguyên
    fs?.Dispose();
    Console.WriteLine(""Đã dọn dẹp tài nguyên tệp tin."");
}
```

---

## 3. Exception Filters với từ khóa `when`

C# cho phép thêm điều kiện lọc vào khối `catch` bằng từ khóa `when`. Khối catch chỉ được kích hoạt khi điều kiện trả về `true`. Điều này giúp giữ nguyên Stack Trace gốc:

```csharp
try
{
    GoiApiThanhToan(soTien: -500);
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine(""Endpoint thanh toán không tồn tại (404)."");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    Console.WriteLine(""Chưa đăng nhập hoặc Token hết hạn (401)."");
}
catch (ArgumentOutOfRangeException ex) when (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
{
    Console.WriteLine(""Hôm nay chủ nhật hệ thống bảo trì!"");
}
```

---

## 4. Phân biệt `throw;` vs `throw ex;` (CỰC KỲ QUAN TRỌNG)

Một trong những sai lầm phổ biến nhất của lập trình viên là viết `throw ex;` khi muốn ném lại ngoại lệ:

```csharp
try
{
    ThucHienGiaoDich();
}
catch (Exception ex)
{
    GhiLog(ex);
    
    // SAI LẦM NGUY HIỂM:
    // throw ex; // Stack trace sẽ bị xóa sạch, vị trí phát sinh lỗi gốc biến mất!
    
    // ĐÚNG CHUẨN:
    throw; // Giữ nguyên toàn bộ Call Stack từ nguồn gốc phát sinh lỗi
}
```

---

## 5. Tự tạo Custom Exception trong ứng dụng Doanh nghiệp

Khi xây dựng các hệ thống Clean Architecture, ta thường tạo các Domain Exception mang ý nghĩa nghiệp vụ:

```csharp
public class SoDuKhongDuException : Exception
{
    public decimal SoDuHienTai { get; }
    public decimal SoTienRut { get; }

    public SoDuKhongDuException(decimal soDu, decimal soTienRut)
        : base($""Số dư {soDu:C} không đủ để thực hiện giao dịch rút {soTienRut:C}!"")
    {
        SoDuHienTai = soDu;
        SoTienRut = soTienRut;
    }
}

// Sử dụng trong nghiệp vụ:
public void RutTien(decimal soTien)
{
    if (soTien > this.SoDu)
    {
        throw new SoDuKhongDuException(this.SoDu, soTien);
    }
    this.SoDu -= soTien;
}
```"
            };

            t13.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Trong khối catch của C#, tại sao nên dùng lệnh 'throw;' thay vì 'throw ex;'?",
                OptionA = "'throw;' tốn ít bộ nhớ hơn.",
                OptionB = "'throw;' giữ nguyên toàn bộ Stack Trace từ điểm lỗi gốc; trong khi 'throw ex;' sẽ đặt lại Stack Trace tính từ dòng gọi đó, làm mất dấu nguồn gốc lỗi.",
                OptionC = "'throw ex;' chỉ dùng được trong môi trường Debug.",
                OptionD = "Hai câu lệnh này hoàn toàn giống nhau.",
                CorrectOption = "B",
                Explanation = "Sử dụng 'throw ex;' sẽ làm reset Call Stack Trace, khiến bạn khó điều tra lỗi trên môi trường Production vì không biết lỗi thực sự phát sinh ở hàm con nào.",
                Difficulty = DifficultyLevel.Beginner
            });

            t13.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Từ khóa nào trong C# cho phép gắn điều kiện kiểm tra bổ sung vào khối catch?",
                OptionA = "if",
                OptionB = "when",
                OptionC = "where",
                OptionD = "filter",
                CorrectOption = "B",
                Explanation = "Cú pháp Exception Filter trong C# sử dụng từ khóa 'catch (Exception ex) when (condition)' để quyết định xem khối catch có xử lý lỗi hay không.",
                Difficulty = DifficultyLevel.Beginner
            });

            newTutorials.Add(t13);
        }
        #endregion

        #region Bài 14: Value Types, Reference Types, Struct & Nullable
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "value-types-reference-types-struct-enum-nullable"))
        {
            var t14 = new Tutorial
            {
                CategoryId = catBeginner.Id,
                Title = "Bài 14: Value Types, Reference Types, Struct, Enum & Nullable trong C#",
                Slug = "value-types-reference-types-struct-enum-nullable",
                Summary = "Phân biệt bản chất bộ nhớ Stack và Heap, khi nào dùng struct thay vì class, cờ bitwise enum, và hệ thống Nullable Reference Types chống lỗi NullReferenceException.",
                EstimatedReadingMinutes = 9,
                Difficulty = DifficultyLevel.Beginner,
                OrderIndex = 14,
                IsFeatured = false,
                ContentMarkdown = @"# Value Types, Reference Types, Struct, Enum & Nullable

Hiểu rõ cách .NET tổ chức dữ liệu trên bộ nhớ (Stack vs Heap) là nền tảng để viết mã nguồn hiệu năng cao, tránh lỗi rò rỉ bộ nhớ và `NullReferenceException`.

---

## 1. Stack vs Heap trong .NET

- **Stack**: Vùng nhớ tuyến tính, siêu nhanh, tự động thu hồi khi thoát khỏi phạm vi hàm. Lưu trữ các **Value Types** và địa chỉ con trỏ của Reference Types.
- **Heap**: Vùng nhớ động, linh hoạt, được quản lý bởi **Garbage Collector (GC)**. Lưu trữ thực thể của **Reference Types**.

```
    STACK MEMORY                     HEAP MEMORY
┌──────────────────┐             ┌─────────────────────────┐
│ int tuoi = 25    │             │                         │
├──────────────────┤             │                         │
│ Point p = (1, 2) │             │  (Object SinhVien)      │
├──────────────────┤   trỏ tới   │  {                      │
│ SinhVien sv ─────┼────────────►│     Ten: ""Alex"",        │
└──────────────────┘             │     Diem: 9.5           │
                                 │  }                      │
                                 └─────────────────────────┘
```

---

## 2. So sánh Struct (Value Type) vs Class (Reference Type)

| Tiêu chí | Struct (Value Type) | Class (Reference Type) |
| :--- | :--- | :--- |
| **Vị trí lưu trữ** | Thường trên Stack (hoặc inline trong class cha) | Luôn trên Managed Heap |
| **Gán dữ liệu (`=`)** | Sao chép toàn bộ giá trị (Copy value) | Sao chép địa chỉ tham chiếu (Copy reference) |
| **Kế thừa** | Không thể kế thừa struct khác (chỉ implement Interface) | Hỗ trợ đơn kế thừa đa tầng |
| **Khi nào dùng?** | Dữ liệu nhỏ (< 16 bytes), vòng đời ngắn, bất biến | Mô hình thực thể nghiệp vụ phức tạp |

```csharp
// Struct bất biến hiệu năng cao trong C# hiện đại
public readonly struct Vector2D
{
    public double X { get; }
    public double Y { get; }

    public Vector2D(double x, double y) => (X, Y) = (x, y);

    public double DoDai => Math.Sqrt(X * X + Y * Y);
}
```

---

## 3. Enum và cờ nhị phân `[Flags]`

`enum` định nghĩa tập hằng số có tên rõ ràng. Sử dụng thuộc tính `[Flags]` cho phép kết hợp nhiều quyền hạn bằng toán tử Bitwise OR (`|`):

```csharp
[Flags]
public enum QuyenNguoiDung
{
    KhongCo   = 0,
    Xem       = 1 << 0, // 1
    Them      = 1 << 1, // 2
    Sua       = 1 << 2, // 4
    Xoa       = 1 << 3, // 8
    QuanTri   = Xem | Them | Sua | Xoa // 15
}

var userRole = QuyenNguoiDung.Xem | QuyenNguoiDung.Them;

// Kiểm tra quyền hạn bằng HasFlag:
bool coQuyenXoa = userRole.HasFlag(QuyenNguoiDung.Xoa); // false
bool coQuyenXem = userRole.HasFlag(QuyenNguoiDung.Xem); // true
```

---

## 4. Nullable Reference Types & Các toán tử an toàn

Từ C# 8.0, trình biên dịch bật tính năng **Nullable Reference Types** để phát hiện lỗi null ngay thời điểm viết code:

```csharp
string chuoiKhongNull = ""Hello""; // Không được phép gán null
string? chuoiCoTheNull = null;     // Cho phép null rõ ràng

// Toán tử Null-Conditional (?.)
int? doDai = chuoiCoTheNull?.Length;

// Toán tử Null-Coalescing (??) - Cung cấp giá trị fallback
string tenHienThi = chuoiCoTheNull ?? ""Khách vô danh"";

// Toán tử Null-Assignment (??=) - Gán nếu đang null
chuoiCoTheNull ??= ""Mặc định"";

// Toán tử Null-Forgiving (!) - Báo trình biên dịch rằng tôi chắc chắn biến không null
string ketQuaChacChan = chuoiCoTheNull!;
```"
            };

            t14.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Khi gán struct A cho struct B (struct B = A;), điều gì diễn ra trong bộ nhớ?",
                OptionA = "B sao chép địa chỉ tham chiếu trỏ tới cùng ô nhớ với A.",
                OptionB = "B tạo ra một bản sao giá trị độc lập hoàn toàn của A trên bộ nhớ.",
                OptionC = "Cả A và B đều bị chuyển lên Heap.",
                OptionD = "Trình biên dịch báo lỗi vì struct không gán được bằng dấu bằng.",
                CorrectOption = "B",
                Explanation = "Struct là Value Type. Phép gán sẽ sao chép toàn bộ giá trị các trường dữ liệu của A sang ô nhớ mới của B. Mọi thay đổi trên B sau đó không ảnh hưởng tới A.",
                Difficulty = DifficultyLevel.Beginner
            });

            t14.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Toán tử '??=' trong C# có ý nghĩa gì?",
                OptionA = "So sánh bằng tuyệt đối giữa hai giá trị.",
                OptionB = "Gán giá trị bên phải cho biến bên trái NẾU biến bên trái đang có giá trị null.",
                OptionC = "Ép kiểu dữ liệu về Nullable.",
                OptionD = "Kiểm tra xem biến có khác null hay không.",
                CorrectOption = "B",
                Explanation = "'x ??= y;' tương đương với 'if (x == null) x = y;'. Nó chỉ thực hiện phép gán khi biến x đang mang giá trị null.",
                Difficulty = DifficultyLevel.Beginner
            });

            newTutorials.Add(t14);
        }
        #endregion

        #region Bài 15: Delegate, Event & Action / Func
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "delegate-event-action-func-trong-csharp"))
        {
            var t15 = new Tutorial
            {
                CategoryId = catOop.Id,
                Title = "Bài 15: Delegate, Event & Built-in Action / Func trong C# Hiện đại",
                Slug = "delegate-event-action-func-trong-csharp",
                Summary = "Khám phá con trỏ hàm an toàn kiểu (Type-safe Function Pointer), Multicast Delegates, mô hình Publisher-Subscriber qua Event, và các delegate chuẩn Action/Func.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 15,
                IsFeatured = true,
                ContentMarkdown = @"# Delegate, Event & Action / Func trong C# Hiện đại

Trong lập trình hướng đối tượng và lập trình hàm (Functional Programming), khả năng truyền một hàm (method) như một tham số vào hàm khác là cực kỳ quan trọng. C# thực hiện điều này một cách an toàn kiểu tuyệt đối thông qua **Delegate**.

---

## 1. Delegate là gì?

**Delegate** là một kiểu dữ liệu đại diện cho các phương thức có cùng chữ ký (signature) gồm kiểu trả về và danh sách tham số. Nó tương đương với Function Pointer trong C/C++ nhưng hoàn toàn an toàn kiểu (Type-Safe).

```csharp
// 1. Khai báo delegate
public delegate void ThongBaoDelegate(string message);

public class Program
{
    public static void GuiEmail(string msg) => Console.WriteLine($""[Email] {msg}"");
    public static void GuiSMS(string msg) => Console.WriteLine($""[SMS] {msg}"");

    public static void Main()
    {
        // 2. Gán hàm vào delegate
        ThongBaoDelegate notify = GuiEmail;
        notify(""Đăng ký thành công!"");

        // 3. Multicast Delegate (Nhiều hàm cùng lắng nghe)
        notify += GuiSMS;
        notify(""Tài khoản của bạn được nạp 500k.""); // Cả GuiEmail và GuiSMS cùng chạy!
    }
}
```

---

## 2. Các Generic Delegate dựng sẵn: `Action<T>` và `Func<T, TResult>`

Thay vì phải tự định nghĩa delegate thủ công bằng từ khóa `delegate`, .NET cung cấp sẵn bộ thư viện Generic Delegates đáp ứng 99% nhu cầu:

```
┌─────────────────────────────────────────────────────────────┐
│               GENERIC DELEGATES DỰNG SẴN                    │
├───────────────────┬─────────────────────────────────────────┤
│ Action<T1, T2...> │ Đại diện phương thức KHÔNG trả về (void)│
│ Func<T1, TResult> │ Đại diện phương thức CÓ trả về giá trị  │
│ Predicate<T>      │ Tương đương Func<T, bool> (dùng kiểm tra)│
└───────────────────┴─────────────────────────────────────────┘
```

```csharp
// Action: Không trả về giá trị (void)
Action<string, int> inThongTin = (ten, tuoi) => Console.WriteLine($""{ten} - {tuoi} tuổi"");
inThongTin(""Kaito"", 25);

// Func: Tham số cuối cùng luôn là kiểu giá trị trả về
Func<int, int, int> phepNhan = (a, b) => a * b;
int tich = phepNhan(6, 7); // 42

// Predicate: Kiểm tra điều kiện logic (trả về bool)
Predicate<int> laSoDuong = x => x > 0;
bool check = laSoDuong(10); // true
```

---

## 3. Event và Mô hình Publisher - Subscriber

**Event** là cơ chế bọc ngoài delegate nhằm áp dụng nguyên lý đóng gói (Encapsulation). Khi một class khai báo Event, các class bên ngoài chỉ được phép đăng ký (`+=`) hoặc hủy đăng ký (`-=`), **không thể** tự ý kích hoạt hoặc gán đè (`= null`) làm mất danh sách người nghe.

### Chuẩn thiết kế Event chuẩn của Microsoft:

```csharp
// Lớp chứa dữ liệu sự kiện
public class DonHangEventArgs : EventArgs
{
    public string MaDonHang { get; set; } = string.Empty;
    public decimal TongTien { get; set; }
}

// Publisher (Bên phát sự kiện)
public class HeThongBanHang
{
    // Khai báo sự kiện chuẩn dùng EventHandler<T>
    public event EventHandler<DonHangEventArgs>? OnDonHangDaTao;

    public void TaoDonHang(string maDon, decimal tien)
    {
        Console.WriteLine($""Đã tạo đơn {maDon}"");

        // Kích hoạt sự kiện an toàn (thread-safe invocation)
        OnDonHangDaTao?.Invoke(this, new DonHangEventArgs 
        { 
            MaDonHang = maDon, 
            TongTien = tien 
        });
    }
}

// Subscriber (Bên đăng ký lắng nghe sự kiện)
public class KhoHang
{
    public void XuLyKhiCoDon(object? sender, DonHangEventArgs e)
    {
        Console.WriteLine($""[Kho] Giữ hàng trong kho cho đơn: {e.MaDonHang}"");
    }
}
```"
            };

            t15.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Sự khác biệt cơ bản giữa Action<T> và Func<T, TResult> trong C# là gì?",
                OptionA = "Action chỉ dùng trong async còn Func chỉ dùng đồng bộ.",
                OptionB = "Action đại diện cho phương thức trả về kiểu void, còn Func luôn có kiểu trả về (tham số cuối cùng).",
                OptionC = "Action chỉ nhận tối đa 1 tham số.",
                OptionD = "Func không thể dùng biểu thức Lambda.",
                CorrectOption = "B",
                Explanation = "Action<T...> dùng cho các hàm trả về void. Func<T1, T2, ..., TResult> dùng cho các hàm có giá trị trả về, trong đó kiểu generic cuối cùng chỉ định kiểu dữ liệu trả về.",
                Difficulty = DifficultyLevel.Intermediate
            });

            t15.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Tại sao nên dùng từ khóa 'event' bọc quanh delegate thay vì để public delegate thông thường?",
                OptionA = "Event giúp code chạy nhanh hơn gấp 10 lần.",
                OptionB = "Event giới hạn quyền truy cập từ bên ngoài: chỉ cho phép đăng ký (+=) hoặc hủy đăng ký (-=), ngăn chặn việc bên ngoài tự ý kích hoạt (Invoke) hoặc gán đè (= null) làm xóa sạch các subscriber khác.",
                OptionC = "Delegate không hỗ trợ đăng ký nhiều hàm.",
                OptionD = "Event tự động lưu vào cơ sở dữ liệu.",
                CorrectOption = "B",
                Explanation = "Event cung cấp tính đóng gói cho delegate. Các đối tượng bên ngoài chỉ có thể lắng nghe hoặc ngừng lắng nghe, chứ không thể tự ý kích nổ sự kiện hay xóa mất các subscriber khác.",
                Difficulty = DifficultyLevel.Intermediate
            });

            newTutorials.Add(t15);
        }
        #endregion

        #region Bài 16: Modern C# - Records & Pattern Matching
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "modern-csharp-records-pattern-matching-switch-expressions"))
        {
            var t16 = new Tutorial
            {
                CategoryId = catOop.Id,
                Title = "Bài 16: Modern C# - Records, Pattern Matching & Switch Expressions (C# 10 - 14)",
                Slug = "modern-csharp-records-pattern-matching-switch-expressions",
                Summary = "Tiếp cận các tính năng hiện đại nhất của C#: Records bất biến (Immutability), toán tử with, Type Pattern, Property Pattern, Relational Pattern và List Pattern.",
                EstimatedReadingMinutes = 10,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 16,
                IsFeatured = true,
                ContentMarkdown = @"# Modern C# - Records, Pattern Matching & Switch Expressions

Các phiên bản C# hiện đại (từ C# 9 đến C# 14) đã bổ sung hàng loạt tính năng mượn từ mô hình lập trình hàm (Functional Programming), giúp code ngắn hơn 50%, an toàn hơn trước các lỗi đột biến trạng thái (State Mutation).

---

## 1. C# Records & Tính Bất Biến (Immutability)

`record` là kiểu dữ liệu tham chiếu (hoặc `record struct`) được thiết kế riêng cho việc lưu trữ dữ liệu (Data Transfer Objects - DTOs, Value Objects).

### Các điểm ưu việt của Record:
1. **Value Equality**: Hai record khác nhau về ô nhớ nhưng nếu toàn bộ giá trị thuộc tính bằng nhau thì `Equals` trả về `true`!
2. **Concise Syntax**: Khai báo 1 dòng tạo sẵn Constructor, Properties, `Deconstruct`, `ToString` hiển thị đẹp.
3. **Non-destructive Mutation với toán tử `with`**: Tạo bản sao có sửa đổi mà không làm thay đổi bản gốc.

```csharp
// Khai báo Positional Record chỉ với 1 dòng code:
public record NhanVien(string Id, string HoTen, decimal Luong, string PhongBan);

var nv1 = new NhanVien(""NV01"", ""Nguyen Van An"", 20_000_000, ""IT"");
var nv2 = new NhanVien(""NV01"", ""Nguyen Van An"", 20_000_000, ""IT"");

Console.WriteLine(nv1 == nv2); // Output: True (So sánh giá trị, không so sánh địa chỉ!)

// Toán tử with: Tạo nhân viên mới giữ nguyên thông tin, chỉ đổi phòng ban
var nv3 = nv1 with { PhongBan = ""R&D"", Luong = 25_000_000 };
Console.WriteLine(nv3); // Output: NhanVien { Id = NV01, HoTen = Nguyen Van An, Luong = 25000000, PhongBan = R&D }
```

---

## 2. Pattern Matching & Switch Expressions

Thay vì viết các khối `if-else` hoặc `switch-case` cồng kềnh với nhiều từ khóa `break`, C# cung cấp **Switch Expression** siêu ngắn:

```csharp
// Mô hình đơn hàng
public abstract record PhuongThucVanChuyen;
public record GiaoHangTietKiem(int KhoangCachKm) : PhuongThucVanChuyen;
public record GiaoHangHoaToc(int KhoangCachKm, bool GiaoBanDem) : PhuongThucVanChuyen;
public record NhanTaiCuaHang : PhuongThucVanChuyen;

decimal TinhPhiShip(PhuongThucVanChuyen pt) => pt switch
{
    NhanTaiCuaHang => 0m,
    
    // Property pattern kết hợp Relational pattern
    GiaoHangTietKiem { KhoangCachKm: <= 5 } => 15_000m,
    GiaoHangTietKiem { KhoangCachKm: > 5 and <= 20 } => 30_000m,
    GiaoHangTietKiem { KhoangCachKm: > 20 } k => 30_000m + (k.KhoangCachKm - 20) * 2_000m,

    // Kiểm tra nhiều thuộc tính cùng lúc
    GiaoHangHoaToc { GiaoBanDem: true } h => 50_000m + h.KhoangCachKm * 5_000m,
    GiaoHangHoaToc { GiaoBanDem: false } h => 35_000m + h.KhoangCachKm * 3_000m,

    // Mặc định (Discard pattern _)
    _ => throw new NotSupportedException(""Phương thức không hỗ trợ"")
};
```

---

## 3. List Pattern trong C# Hiện đại

C# hỗ trợ so khớp cấu trúc mảng/danh sách với List Patterns:

```csharp
string PhanTichLenh(string[] tokens) => tokens switch
{
    [""GET"", var url] => $""Gửi request GET tới {url}"",
    [""POST"", var url, var body] => $""Gửi request POST tới {url} với dữ liệu {body}"",
    [""AUTH"", .., ""ADMIN""] => ""Lệnh bảo mật yêu cầu quyền Admin cuối cùng"",
    [] => ""Không có lệnh nào"",
    _ => ""Lệnh không hợp lệ""
};
```"
            };

            t16.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Điểm khác biệt chính giữa toán tử so sánh bằng (==) của 'class' thông thường và 'record' trong C# là gì?",
                OptionA = "Class so sánh bằng giá trị, còn record so sánh bằng địa chỉ con trỏ.",
                OptionB = "Record so sánh theo giá trị các trường dữ liệu (Value Equality), trong khi Class mặc định so sánh theo địa chỉ tham chiếu (Reference Equality).",
                OptionC = "Record không thể dùng toán tử ==.",
                OptionD = "Class tự động tạo hàm GetHashCode theo giá trị.",
                CorrectOption = "B",
                Explanation = "Record được trình biên dịch tự sinh phương thức Equals và operator == dựa trên giá trị của các thuộc tính, nên hai thực thể độc lập có dữ liệu giống nhau sẽ bằng nhau.",
                Difficulty = DifficultyLevel.Intermediate
            });

            t16.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Toán tử 'with' dùng với Record có mục đích gì?",
                OptionA = "Xóa bỏ record khỏi bộ nhớ.",
                OptionB = "Tạo một bản sao mới không làm thay đổi bản gốc (Non-destructive mutation) và cho phép sửa đổi một số thuộc tính được chọn.",
                OptionC = "Kế thừa từ một record khác.",
                OptionD = "Ép kiểu dữ liệu về class cha.",
                CorrectOption = "B",
                Explanation = "Toán tử 'with' tạo ra một bản copy mới với các trường được chỉ định thay đổi, giữ nguyên tính bất biến (immutability) của record gốc.",
                Difficulty = DifficultyLevel.Intermediate
            });

            newTutorials.Add(t16);
        }
        #endregion

        #region Bài 17: Quản lý Bộ nhớ & Tối ưu Hiệu năng (GC, IDisposable, Span<T>)
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "quan-ly-bo-nho-gc-idisposable-span-memory"))
        {
            var t17 = new Tutorial
            {
                CategoryId = catOop.Id,
                Title = "Bài 17: Quản lý Bộ nhớ & Tối ưu Hiệu năng - GC, IDisposable, Span<T> & Memory<T>",
                Slug = "quan-ly-bo-nho-gc-idisposable-span-memory",
                Summary = "Hiểu sâu cơ chế bộ thu gom rác .NET (GC Gen 0/1/2/LOH), chuẩn Dispose Pattern giải phóng tài nguyên unmanaged và kỹ thuật Zero-allocation với Span<T>.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 17,
                IsFeatured = true,
                ContentMarkdown = @"# Quản lý Bộ nhớ & Tối ưu Hiệu năng trong .NET

Hiệu năng cao là một trong những lý do lớn nhất mà các tập đoàn tài chính, thương mại điện tử và đám mây chọn .NET. Để khai thác tối đa sức mạnh của .NET, bạn cần hiểu cơ chế thu gom rác (GC) và kỹ thuật lập trình Zero-Allocation.

---

## 1. Cơ chế Garbage Collector (GC) trong .NET

.NET GC chia các đối tượng trên Managed Heap thành **3 thế hệ (Generations)** và phân vùng **LOH (Large Object Heap)**:

```
┌─────────────────────────────────────────────────────────────┐
│                      MANAGED HEAP                           │
├───────────────┬─────────────────┬────────────────┬──────────┤
│ Gen 0         │ Gen 1           │ Gen 2          │ LOH      │
│ (Đối tượng mới)│ (Vùng đệm sống) │ (Sống lâu năm) │ (>= 85KB)│
│ Thu gom cực   │ Thu gom vừa     │ Thu gom nặng   │ Ít khi   │
│ nhanh (~1ms)  │ phải            │ (Full GC Pause)│ Compact  │
└───────────────┴─────────────────┴────────────────┴──────────┘
```

- **Gen 0**: Chứa các đối tượng ngắn hạn vừa được tạo (`new`). Được dọn dẹp thường xuyên nhất và cực nhanh.
- **Gen 1**: Vùng đệm chứa các đối tượng sống sót qua đợt dọn rác Gen 0.
- **Gen 2**: Chứa các đối tượng sống lâu năm (Static objects, Singleton services, Cache). Khi Gen 2 thu gom (Full GC), ứng dụng có thể bị dừng (Stop-the-world pause).
- **LOH (Large Object Heap)**: Chứa các mảng hoặc chuỗi có dung lượng từ **85.000 bytes** trở lên. Việc thu dọn LOH rất tốn kém vì mặc định không dồn bộ nhớ (No Compaction).

---

## 2. Giải phóng Tài nguyên Unmanaged với `IDisposable`

Tài nguyên unmanaged (Database connection, Network socket, File OS handle) không được GC dọn tự động. Lập trình viên phải hiện thực `IDisposable` và dùng cú pháp `using`:

```csharp
// Cú pháp using declaration hiện đại trong C#
public void XuLyFile(string duongDan)
{
    using var stream = File.OpenRead(duongDan);
    using var reader = new StreamReader(stream);

    string noiDung = reader.ReadToEnd();
    Console.WriteLine(noiDung);
    // stream và reader tự động được Dispose ngay khi thoát khỏi hàm!
}
```

---

## 3. Lập trình Zero-Allocation với `Span<T>` và `ReadOnlySpan<T>`

Trước đây, khi muốn cắt một chuỗi (`string.Substring`), .NET buộc phải cấp phát một vùng nhớ mới trên Heap. Khi xử lý hàng triệu chuỗi mỗi giây, GC sẽ quá tải.

`Span<T>` là một `ref struct` lưu trữ trên Stack, trỏ trực tiếp vào một vùng nhớ liên tục (mảng, chuỗi, hoặc bộ nhớ unmanaged) mà **hoàn toàn không cấp phát Heap (Zero Allocation)**:

```csharp
string duLieu = ""Order:109458,Amount:4500000,Status:Success"";

// Cách cũ tạo ra nhiều chuỗi rác trên Heap:
// string amountStr = duLieu.Substring(20, 7); // Cấp phát Heap!

// Cách hiện đại dùng ReadOnlySpan không tốn 1 byte Heap nào:
ReadOnlySpan<char> span = duLieu.AsSpan();

int idxAmount = span.IndexOf(""Amount:"") + 7;
int idxComma = span.Slice(idxAmount).IndexOf(',');
ReadOnlySpan<char> amountSpan = span.Slice(idxAmount, idxComma);

if (long.TryParse(amountSpan, out long soTien))
{
    Console.WriteLine($""Số tiền trích xuất không tốn GC: {soTien:N0} đ"");
}
```"
            };

            t17.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Các đối tượng có kích thước từ bao nhiêu bytes trở lên sẽ được cấp phát trực tiếp vào phân vùng Large Object Heap (LOH) của .NET?",
                OptionA = "1.024 bytes (1 KB)",
                OptionB = "85.000 bytes (~83 KB)",
                OptionC = "1.048.576 bytes (1 MB)",
                OptionD = "Mọi đối tượng mảng đều vào LOH.",
                CorrectOption = "B",
                Explanation = "Trong .NET, bất kỳ đối tượng nào có kích thước từ 85.000 bytes trở lên đều được đưa vào Large Object Heap (LOH) để tránh chi phí sao chép di chuyển dữ liệu lớn trong các đợt dọn rác thông thường.",
                Difficulty = DifficultyLevel.Advanced
            });

            t17.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Ưu điểm đột phá nhất của Span<T> so với mảng hoặc chuỗi Substring truyền thống là gì?",
                OptionA = "Span tự động lưu vào đĩa cứng.",
                OptionB = "Span<T> cho phép trích xuất lát cắt bộ nhớ (Slice) mà không cấp phát thêm bất kỳ vùng nhớ nào trên Managed Heap (Zero-allocation), giảm tải triệt để cho Garbage Collector.",
                OptionC = "Span có thể lưu trữ dữ liệu không giới hạn kích thước.",
                OptionD = "Span tương thích ngược với .NET Framework 2.0.",
                CorrectOption = "B",
                Explanation = "Span<T> chỉ là một view cửa sổ trỏ vào vùng nhớ hiện có. Các thao tác Slice không tạo ra bản sao trên Heap, giúp tối ưu hiệu năng và throughput của hệ thống.",
                Difficulty = DifficultyLevel.Advanced
            });

            newTutorials.Add(t17);
        }
        #endregion

        #region Bài 18: Reflection, Attributes & Source Generators
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "reflection-attributes-source-generators-csharp"))
        {
            var t18 = new Tutorial
            {
                CategoryId = catOop.Id,
                Title = "Bài 18: Reflection, Custom Attributes & C# Roslyn Source Generators",
                Slug = "reflection-attributes-source-generators-csharp",
                Summary = "Khám phá cách đọc và thao tác Metadata tại runtime qua Reflection, tự tạo Custom Attribute, và bước chuyển mình sang Compile-time Roslyn Source Generators.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 18,
                IsFeatured = false,
                ContentMarkdown = @"# Reflection, Custom Attributes & Source Generators

**Reflection** cung cấp các đối tượng mô tả Assembly, Module và Kiểu dữ liệu (Types). Nó cho phép ứng dụng đọc siêu dữ liệu (Metadata), kiểm tra thuộc tính và triệu gọi hàm động tại thời điểm ứng dụng đang chạy (Runtime).

---

## 1. Sử dụng Reflection cơ bản

```csharp
using System.Reflection;

public class SanPham
{
    public string Ten { get; set; } = """";
    public decimal Gia { get; set; }

    public void GiamGia(decimal phanTram) => Gia -= Gia * (phanTram / 100);
}

// Kiểm tra Metadata tại Runtime:
Type t = typeof(SanPham);
Console.WriteLine($""Tên kiểu: {t.FullName}"");

// Lấy danh sách thuộc tính
foreach (PropertyInfo prop in t.GetProperties())
{
    Console.WriteLine($""Property: {prop.Name} (Kiểu: {prop.PropertyType.Name})"");
}

// Khởi tạo đối tượng động và gọi hàm:
object instance = Activator.CreateInstance(t)!;
MethodInfo method = t.GetMethod(""GiamGia"")!;
method.Invoke(instance, new object[] { 10m });
```

---

## 2. Tạo Custom Attributes trong Doanh nghiệp

Attributes gắn các nhãn siêu dữ liệu vào class, property hoặc method:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class BatBuocAttribute : Attribute
{
    public string ThongBaoLoi { get; }
    public BatBuocAttribute(string loi) => ThongBaoLoi = loi;
}

public class NguoiDungDto
{
    [BatBuoc(""Tên đăng nhập không được để trống!"")]
    public string Username { get; set; } = """";
}

// Validator sử dụng Reflection đọc Attribute:
public static class Validator
{
    public static List<string> KiemTra(object obj)
    {
        var errors = new List<string>();
        var props = obj.GetType().GetProperties();

        foreach (var p in props)
        {
            var attr = p.GetCustomAttribute<BatBuocAttribute>();
            if (attr != null)
            {
                var val = p.GetValue(obj) as string;
                if (string.IsNullOrWhiteSpace(val))
                {
                    errors.Add(attr.ThongBaoLoi);
                }
            }
        }
        return errors;
    }
}
```

---

## 3. Xu hướng hiện đại: Roslyn Source Generators

Nhược điểm lớn nhất của Reflection là **chậm** và **không tương thích hoàn hảo với Native AOT** (Ahead-of-Time compilation).

Microsoft đã giới thiệu **Source Generators**: Trình biên dịch C# phân tích code và sinh mã nguồn C# thực tế ngay tại thời điểm Build time. Bạn nhận được hiệu năng tối đa tương đương viết code bằng tay mà không tốn 1ms nào của Reflection tại runtime!"
            };

            t18.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Nhược điểm lớn nhất của việc lạm dụng Reflection trong ứng dụng thực tế là gì?",
                OptionA = "Reflection chỉ chạy được trên hệ điều hành Linux.",
                OptionB = "Reflection bỏ qua kiểm tra an toàn kiểu tại compile-time và có chi phí hiệu năng (performance overhead) chậm hơn đáng kể so với gọi code trực tiếp.",
                OptionC = "Reflection không hỗ trợ class trừu tượng.",
                OptionD = "Reflection bắt buộc phải có quyền Admin trên máy.",
                CorrectOption = "B",
                Explanation = "Reflection phải tìm kiếm Metadata dạng chuỗi tại Runtime nên tốn CPU và không được trình biên dịch kiểm tra lỗi lúc build, đồng thời cản trở tối ưu hóa Native AOT.",
                Difficulty = DifficultyLevel.Advanced
            });

            t18.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Lợi ích vượt trội của Roslyn Source Generators so với Reflection là gì?",
                OptionA = "Source Generators sinh mã C# tại Compile-Time, mang lại tốc độ thực thi tối đa và hỗ trợ tuyệt đối cho Native AOT.",
                OptionB = "Source Generators có thể sửa file DLL đã biên dịch.",
                OptionC = "Source Generators tự động sửa lỗi logic nghiệp vụ.",
                OptionD = "Source Generators thay thế hoàn toàn cơ sở dữ liệu.",
                CorrectOption = "A",
                Explanation = "Source Generators tạo mã tại thời điểm biên dịch (Compile-time), mã sinh ra có thể debug được, an toàn kiểu và đạt tốc độ thực thi cao nhất mà không cần Reflection tại Runtime.",
                Difficulty = DifficultyLevel.Advanced
            });

            newTutorials.Add(t18);
        }
        #endregion

        #region Bài 19: Lập trình Song song & Đồng thời (TPL, Channels, CancellationToken)
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "da-luong-song-song-task-parallel-library-channels-cancellation"))
        {
            var t19 = new Tutorial
            {
                CategoryId = catOop.Id,
                Title = "Bài 19: Lập trình Song song & Đồng thời - TPL, Parallel.ForEach, Channels & CancellationToken",
                Slug = "da-luong-song-song-task-parallel-library-channels-cancellation",
                Summary = "Tận dụng tối đa CPU đa nhân với Task Parallel Library, xử lý hủy tác vụ an toàn với CancellationToken và mô hình Producer-Consumer hiệu năng cao với Channels.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 19,
                IsFeatured = true,
                ContentMarkdown = @"# Lập trình Song song & Đồng thời trong .NET

Tận dụng kiến trúc vi xử lý đa nhân (Multi-core CPUs) là chìa khóa để xây dựng các backend xử lý hàng triệu bản ghi hoặc dữ liệu lớn với thông lượng cao.

---

## 1. Concurrency (Đồng thời) vs Parallelism (Song song)

- **Concurrency**: Nhiều tác vụ cùng tiến triển xen kẽ nhau (ví dụ: `async/await` chờ I/O mạng hoặc database).
- **Parallelism**: Nhiều tác vụ thực sự chạy cùng một lúc trên các lõi CPU vật lý khác nhau (CPU-bound tasks).

---

## 2. Task Parallel Library (TPL) & `Parallel.ForEachAsync`

Khi cần xử lý hàng ngàn tác vụ nặng cùng lúc có giới hạn số luồng đồng thời:

```csharp
var danhSachUrls = new List<string> { ""url1"", ""url2"", ""url3"", /* ... 1000 urls */ };

// Giới hạn tối đa 4 tác vụ song song cùng thời điểm (tránh nghẽn mạng):
var parallelOptions = new ParallelOptions 
{ 
    MaxDegreeOfParallelism = 4 
};

await Parallel.ForEachAsync(danhSachUrls, parallelOptions, async (url, token) =>
{
    using var client = new HttpClient();
    var response = await client.GetStringAsync(url, token);
    Console.WriteLine($""Đã tải xong {url} ({response.Length} ký tự)"");
});
```

---

## 3. Hủy bỏ tác vụ an toàn với `CancellationToken`

Trong ứng dụng web, khi người dùng hủy request hoặc đóng trình duyệt, tác vụ ngầm phía server nên dừng lại ngay để giải phóng tài nguyên CPU/RAM:

```csharp
public async Task XuLyDuLieuNangAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i < 100; i++)
    {
        // Kiểm tra xem đã có yêu cầu hủy hay chưa:
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(500, cancellationToken);
        Console.WriteLine($""Đã xử lý bước {i + 1}/100"");
    }
}

// Phía gọi có thể hủy sau 2 giây:
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    await XuLyDuLieuNangAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine(""Tác vụ đã được hủy bỏ an toàn!"");
}
```

---

## 4. Mô hình Producer-Consumer siêu tốc với `System.Threading.Channels`

`Channel<T>` là giải pháp hàng đợi hàng đầu trong .NET để chuyển giao dữ liệu an toàn giữa các luồng:

```csharp
using System.Threading.Channels;

// Tạo Channel có giới hạn sức chứa 100 phần tử (Backpressure)
var channel = Channel.CreateBounded<int>(100);

// Producer (Bên nạp dữ liệu)
var producerTask = Task.Run(async () =>
{
    for (int i = 1; i <= 10; i++)
    {
        await channel.Writer.WriteAsync(i);
        Console.WriteLine($""[Producer] Đã nạp item {i}"");
    }
    channel.Writer.Complete(); // Báo hiệu đã xong
});

// Consumer (Bên xử lý dữ liệu)
var consumerTask = Task.Run(async () =>
{
    await foreach (var item in channel.Reader.ReadAllAsync())
    {
        Console.WriteLine($""[Consumer] Đã xử lý item {item}"");
        await Task.Delay(100);
    }
});

await Task.WhenAll(producerTask, consumerTask);
```"
            };

            t19.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Mục đích quan trọng nhất của việc truyền CancellationToken vào các phương thức bất đồng bộ là gì?",
                OptionA = "Giúp tăng tốc độ kết nối internet.",
                OptionB = "Cho phép dừng ngay lập tức các tác vụ ngầm khi người dùng hủy request hoặc quá thời gian timeout, tiết kiệm tài nguyên CPU và bộ nhớ của server.",
                OptionC = "Tự động bắt mọi ngoại lệ phát sinh.",
                OptionD = "Khóa dữ liệu database chống ghi đè.",
                CorrectOption = "B",
                Explanation = "CancellationToken mang tín hiệu hủy từ client đến server, giúp dừng xử lý kịp thời khi request bị ngắt kết nối hoặc hết hạn thời gian chờ.",
                Difficulty = DifficultyLevel.Advanced
            });

            t19.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Trong ParallelOptions, thuộc tính 'MaxDegreeOfParallelism' có ý nghĩa gì?",
                OptionA = "Giới hạn thời gian chạy tối đa tính bằng mili giây.",
                OptionB = "Giới hạn số lượng tác vụ hoặc luồng tối đa được phép thực thi song song đồng thời.",
                OptionC = "Số lần thử lại khi gặp lỗi.",
                OptionD = "Kích thước của bộ nhớ đệm.",
                CorrectOption = "B",
                Explanation = "MaxDegreeOfParallelism kiểm soát số lượng luồng đồng thời chạy song song, tránh việc tạo quá nhiều luồng làm kiệt quệ tài nguyên máy chủ hoặc áp đảo database.",
                Difficulty = DifficultyLevel.Advanced
            });

            newTutorials.Add(t19);
        }
        #endregion

        #region Bài 20: Thiết kế Quan hệ Dữ liệu với EF Core Fluent API
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "thiet-ke-quan-he-du-lieu-ef-core-fluent-api"))
        {
            var t20 = new Tutorial
            {
                CategoryId = catEf.Id,
                Title = "Bài 20: Thiết kế Quan hệ Dữ liệu với EF Core Fluent API (1-1, 1-N, N-N & Cascade Delete)",
                Slug = "thiet-ke-quan-he-du-lieu-ef-core-fluent-api",
                Summary = "Định nghĩa quan hệ thực thể chuẩn xác với Fluent API: HasOne/WithMany, HasMany/WithMany, khóa ngoại, bảng liên kết trung gian và hành vi Cascade Delete.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 20,
                IsFeatured = true,
                ContentMarkdown = @"# Thiết kế Quan hệ Dữ liệu với EF Core Fluent API

Trong các dự án phần mềm doanh nghiệp, mối quan hệ giữa các bảng cơ sở dữ liệu (Relationships) rất phức tạp. Dù Data Annotations (`[ForeignKey]`) tiện lợi, **Fluent API** trong phương thức `OnModelCreating` mới là chuẩn mực tối thượng để cấu hình quan hệ tách rời khỏi Domain Model.

---

## 1. Quan hệ Một - Nhiều (One-to-Many: 1-N)

Đây là mối quan hệ phổ biến nhất (ví dụ: Một Danh mục có Nhiều Khóa học):

```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = """";
    public ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
}

public class Tutorial
{
    public int Id { get; set; }
    public string Title { get; set; } = """";
    
    public int CategoryId { get; set; } // Khóa ngoại
    public Category Category { get; set; } = null!;
}

// Cấu hình trong DbContext:
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Tutorial>()
        .HasOne(t => t.Category)
        .WithMany(c => c.Tutorials)
        .HasForeignKey(t => t.CategoryId)
        .OnDelete(DeleteBehavior.Restrict); // Chống xóa danh mục nếu còn bài học
}
```

---

## 2. Quan hệ Nhiều - Nhiều (Many-to-Many: N-N)

Từ EF Core 5.0+, quan hệ Nhiều - Nhiều không cần phải tạo entity trung gian thủ công nếu không cần thêm trường dữ liệu:

```csharp
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = """";
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = """";
    public ICollection<Student> Students { get; set; } = new List<Student>();
}

// EF Core tự động sinh bảng trung gian CourseStudent:
modelBuilder.Entity<Student>()
    .HasMany(s => s.Courses)
    .WithMany(c => c.Students);
```

Khi bảng trung gian cần thêm cột (ví dụ: `NgayDangKy`, `DiemSo`), hãy tạo Entity trung gian tường minh:

```csharp
public class Enrollment
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}

// Cấu hình khóa chính hỗn hợp (Composite Key):
modelBuilder.Entity<Enrollment>()
    .HasKey(e => new { e.StudentId, e.CourseId });
```

---

## 3. Các hành vi Cascade Delete (`DeleteBehavior`)

| DeleteBehavior | Hành vi khi bản ghi cha bị xóa |
| :--- | :--- |
| **`Cascade`** | Tự động xóa sạch toàn bộ các bản ghi con liên quan. |
| **`Restrict`** | Chặn không cho xóa bản ghi cha nếu còn tồn tại bản ghi con. |
| **`SetNull`** | Đặt giá trị khóa ngoại của các bản ghi con về `null`. |
| **`NoAction`** | Không can thiệp, để cơ sở dữ liệu quyết định ném lỗi ngoại lệ. |"
            };

            t20.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Trong EF Core, DeleteBehavior.Restrict có tác dụng gì khi xóa bản ghi cha?",
                OptionA = "Tự động xóa luôn các bản ghi con.",
                OptionB = "Ngăn chặn việc xóa bản ghi cha nếu vẫn còn các bản ghi con liên kết với nó, giúp bảo toàn tính toàn vẹn dữ liệu.",
                OptionC = "Chuyển bản ghi cha vào thùng rác tạm.",
                OptionD = "Gán khóa ngoại của các bản ghi con về null.",
                CorrectOption = "B",
                Explanation = "DeleteBehavior.Restrict bảo vệ dữ liệu bằng cách không cho phép xóa bản ghi cha (như Danh mục) khi đang có bản ghi con (như Bài học) tham chiếu đến.",
                Difficulty = DifficultyLevel.Intermediate
            });

            t20.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Phương thức nào trong DbContext là nơi chuẩn mực nhất để viết Fluent API cấu hình quan hệ giữa các bảng?",
                OptionA = "OnConfiguring()",
                OptionB = "OnModelCreating()",
                OptionC = "SaveChanges()",
                OptionD = "Dispose()",
                CorrectOption = "B",
                Explanation = "OnModelCreating(ModelBuilder modelBuilder) là phương thức của EF Core chuyên dùng để cấu hình schema, quan hệ thực thể, chỉ mục index và ràng buộc dữ liệu.",
                Difficulty = DifficultyLevel.Intermediate
            });

            newTutorials.Add(t20);
        }
        #endregion

        #region Bài 21: Tối ưu Truy vấn & Hiệu năng EF Core
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "toi-uu-truy-van-hieu-nang-ef-core-asnotracking-loading"))
        {
            var t21 = new Tutorial
            {
                CategoryId = catEf.Id,
                Title = "Bài 21: Tối ưu Truy vấn & Hiệu năng EF Core - AsNoTracking, Split Queries & Eager Loading",
                Slug = "toi-uu-truy-van-hieu-nang-ef-core-asnotracking-loading",
                Summary = "Xóa bỏ lỗi N+1 Query kinh điển, tăng tốc đọc dữ liệu với AsNoTracking, phân biệt Eager/Lazy Loading và kỹ thuật Split Queries chống bùng nổ dữ liệu.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Intermediate,
                OrderIndex = 21,
                IsFeatured = true,
                ContentMarkdown = @"# Tối ưu Truy vấn & Hiệu năng EF Core

ORM mang lại sự tiện lợi vượt bậc nhưng nếu không hiểu cách EF Core biên dịch LINQ sang SQL, ứng dụng của bạn sẽ bị chậm chạp, ngốn RAM và quá tải cơ sở dữ liệu.

---

## 1. Tối ưu truy vấn chỉ đọc với `.AsNoTracking()`

Mặc định, EF Core theo dõi trạng thái (Change Tracking) của mọi thực thể tải lên để chuẩn bị cho `SaveChanges()`. Việc này tốn CPU và RAM để lưu bản chụp (Snapshot).

Đối với các API hoặc trang web chỉ hiển thị dữ liệu:

```csharp
// Tăng tốc độ truy vấn từ 2x đến 4x, tiết kiệm 50% RAM:
var danhSach = await context.Tutorials
    .AsNoTracking()
    .Where(t => t.IsFeatured)
    .ToListAsync();
```

---

## 2. Vấn đề N+1 Query và Giải pháp Eager Loading

Lỗi N+1 xảy ra khi bạn tải 1 danh mục (1 query), sau đó duyệt qua từng bài học để lấy thêm chi tiết (N queries phụ):

```csharp
// SAI LẦM N+1 QUERY NGUY HIỂM:
var categories = await context.Categories.ToListAsync(); // 1 query
foreach (var c in categories)
{
    // MỖI VÒNG LẶP GỬI 1 QUERY XUỐNG DATABASE! (N queries)
    Console.WriteLine(c.Tutorials.Count); 
}

// GIẢI PHÁP: EAGER LOADING VỚI .Include()
// Chỉ gửi duy nhất 1 câu SQL JOIN:
var categoriesToiUu = await context.Categories
    .Include(c => c.Tutorials)
        .ThenInclude(t => t.QuizQuestions)
    .AsNoTracking()
    .ToListAsync();
```

---

## 3. Chống bùng nổ dữ liệu với Split Queries (`.AsSplitQuery()`)

Khi bạn `Include` nhiều bảng con nhiều-nhiều, câu lệnh `SQL JOIN` sẽ nhân bản số dòng theo tích Descartes (Cartesian Product), khiến lượng dữ liệu tải qua mạng phình to gấp 100 lần.

```csharp
// EF Core tách thành nhiều câu SELECT nhỏ rành mạch:
var orders = await context.Orders
    .Include(o => o.OrderItems)
    .Include(o => o.ShippingAddresses)
    .AsSplitQuery() // Tách thành 3 câu query nhanh và nhỏ gọn!
    .ToListAsync();
```

---

## 4. DTO Projection với `.Select()`

Cách tối ưu nhất không phải là tải toàn bộ entity rồi giấu bớt trường, mà là chỉ SELECT đúng các cột giao diện cần:

```csharp
// Chỉ sinh mã SQL: SELECT Id, Title FROM Tutorials
var dtoResult = await context.Tutorials
    .Where(t => t.CategoryId == 1)
    .Select(t => new TutorialSummaryDto
    {
        Id = t.Id,
        Title = t.Title
    })
    .ToListAsync();
```"
            };

            t21.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Tại sao nên gọi phương thức .AsNoTracking() khi thực hiện các truy vấn chỉ đọc (Read-only) trong EF Core?",
                OptionA = "Vì nó bỏ qua cơ chế Change Tracker, không phải lưu snapshot đối tượng trong bộ nhớ, giúp tăng tốc độ đọc và giảm tải RAM đáng kể.",
                OptionB = "Vì nó tự động mã hóa dữ liệu trả về.",
                OptionC = "Vì nó xóa sạch dữ liệu khỏi bảng.",
                OptionD = "Vì nó bắt buộc phải dùng cho SQL Server.",
                CorrectOption = "A",
                Explanation = ".AsNoTracking() thông báo cho EF Core không cần theo dõi sự thay đổi của thực thể, từ đó tiết kiệm tài nguyên bộ nhớ và tăng tốc độ xử lý câu lệnh SQL.",
                Difficulty = DifficultyLevel.Intermediate
            });

            t21.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Phương thức .AsSplitQuery() trong EF Core giải quyết vấn đề gì khi JOIN nhiều bảng quan hệ 1-N?",
                OptionA = "Giải quyết lỗi chia cho số 0.",
                OptionB = "Tránh hiện tượng bùng nổ dữ liệu tích Descartes (Cartesian Explosion) do một câu lệnh SQL JOIN quá nhiều bảng sinh ra lượng dòng dữ liệu trùng lặp khổng lồ.",
                OptionC = "Tự động phân trang dữ liệu theo từng 10 dòng.",
                OptionD = "Đổi kết nối sang database phụ.",
                CorrectOption = "B",
                Explanation = ".AsSplitQuery() chia một câu lệnh JOIN phức tạp thành nhiều câu lệnh SELECT nhỏ riêng lẻ cho từng bảng con, ngăn chặn bùng nổ kích thước dữ liệu chuyển tải qua mạng.",
                Difficulty = DifficultyLevel.Intermediate
            });

            newTutorials.Add(t21);
        }
        #endregion

        #region Bài 22: Transactions, Concurrency Control & Repository Pattern
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "transactions-concurrency-control-repository-pattern-ef-core"))
        {
            var t22 = new Tutorial
            {
                CategoryId = catEf.Id,
                Title = "Bài 22: Transactions, Concurrency Control & Repository Pattern trong EF Core",
                Slug = "transactions-concurrency-control-repository-pattern-ef-core",
                Summary = "Đảm bảo tính toàn vẹn dữ liệu ACID với Database Transactions, xử lý xung đột ghi đồng thời (Optimistic Concurrency) và đánh giá khách quan Repository Pattern.",
                EstimatedReadingMinutes = 11,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 22,
                IsFeatured = false,
                ContentMarkdown = @"# Transactions, Concurrency Control & Repository Pattern trong EF Core

Trong các hệ thống tài chính, ngân hàng và bán vé, việc hai người dùng cùng cập nhật một bản ghi cùng lúc là chuyện xảy ra hàng ngày. 

---

## 1. Database Transactions thủ công với EF Core

Mặc định, phương thức `SaveChangesAsync()` của EF Core đã tự động bọc mọi thao tác vào một Transaction nguyên tử (Atomic). Tuy nhiên, khi bạn cần phối hợp nhiều lần SaveChanges hoặc nhiều nguồn dữ liệu:

```csharp
using var transaction = await context.Database.BeginTransactionAsync();
try
{
    // 1. Trừ tiền tài khoản người gửi
    taiKhoanA.SoDu -= 500_000;
    await context.SaveChangesAsync();

    // 2. Cộng tiền tài khoản người nhận
    taiKhoanB.SoDu += 500_000;
    await context.SaveChangesAsync();

    // 3. Cam kết giao dịch (Commit)
    await transaction.CommitAsync();
}
catch (Exception)
{
    // Tự động hoàn tác nếu có lỗi bất kỳ (Rollback)
    await transaction.RollbackAsync();
    throw;
}
```

---

## 2. Xử lý xung đột đồng thời (Optimistic Concurrency Control)

Tránh hiện tượng ghi đè mù quáng (Lost Update) bằng cách sử dụng trường phiên bản `RowVersion`:

```csharp
public class SanPham
{
    public int Id { get; set; }
    public string Ten { get; set; } = """";
    public int SoLuongTon { get; set; }

    // Cờ kiểm soát phiên bản tự tăng trong SQL:
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}

// Khi cập nhật:
try
{
    sp.SoLuongTon -= 1;
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    Console.WriteLine(""Sản phẩm vừa được người khác mua trước! Hãy tải lại trang."");
}
```

---

## 3. Có nên dùng Repository & Unit of Work Pattern với EF Core?

`DbContext` và `DbSet` trong EF Core về bản chất **ĐÃ LÀ** một Unit of Work và Repository Pattern hoàn chỉnh. Việc viết thêm một lớp Repository bọc ngoài thường dẫn đến hiện tượng trừu tượng hóa dư thừa (Over-abstraction) và làm mất đi các tính năng mạnh mẽ của LINQ (`IQueryable`). Khuyến nghị hiện đại là dùng trực tiếp DbContext hoặc viết Extension Methods."
            };

            t22.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Optimistic Concurrency Control trong EF Core hoạt động dựa trên nguyên lý nào?",
                OptionA = "Khóa cứng toàn bộ bảng database không cho ai đọc dữ liệu.",
                OptionB = "Giả định xung đột hiếm khi xảy ra; khi lưu sẽ so sánh trường phiên bản (RowVersion/Timestamp), nếu phát hiện bản ghi đã bị thay đổi bởi người khác thì ném lỗi DbUpdateConcurrencyException.",
                OptionC = "Người gửi request đầu tiên luôn bị hủy.",
                OptionD = "Ghi đè dữ liệu của người đến sau lên người đến trước.",
                CorrectOption = "B",
                Explanation = "Optimistic Concurrency không khóa dữ liệu khi đọc mà kiểm tra giá trị phiên bản lúc lưu. Nếu RowVersion bị thay đổi bởi giao dịch khác, EF Core sẽ ném ngoại lệ DbUpdateConcurrencyException để ứng dụng xử lý.",
                Difficulty = DifficultyLevel.Advanced
            });

            t22.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Mặc định khi gọi context.SaveChangesAsync(), EF Core có tự động tạo Transaction hay không?",
                OptionA = "Không bao giờ, bắt buộc phải tự gọi BeginTransactionAsync.",
                OptionB = "Có, EF Core mặc định tự động bọc toàn bộ các lệnh Insert, Update, Delete trong một Transaction nguyên tử (ACID).",
                OptionC = "Chỉ tạo khi thao tác trên SQL Server.",
                OptionD = "Chỉ tạo khi có trên 100 bản ghi.",
                CorrectOption = "B",
                Explanation = "Mỗi lời gọi SaveChangesAsync() mặc định được EF Core bọc trong một Database Transaction, đảm bảo tính nguyên tử (tất cả cùng thành công hoặc tất cả cùng rollback).",
                Difficulty = DifficultyLevel.Advanced
            });

            newTutorials.Add(t22);
        }
        #endregion

        #region Bài 23: Minimal APIs trong ASP.NET Core
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "minimal-apis-aspnet-core-high-performance-endpoints"))
        {
            var t23 = new Tutorial
            {
                CategoryId = catAspNetCore.Id,
                Title = "Bài 23: Minimal APIs trong ASP.NET Core - High-Performance Endpoints & TypedResults",
                Slug = "minimal-apis-aspnet-core-high-performance-endpoints",
                Summary = "Xây dựng RESTful API siêu tốc độ với kiến trúc Minimal APIs hiện đại: Endpoint Routing, TypedResults, Endpoint Filters và Grouping.",
                EstimatedReadingMinutes = 10,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 23,
                IsFeatured = true,
                ContentMarkdown = @"# Minimal APIs trong ASP.NET Core

Từ .NET 6 đến .NET 10, **Minimal APIs** đã trở thành lựa chọn hàng đầu cho các Microservices và Cloud-native APIs nhờ cấu trúc gọn gàng, khởi động nhanh và hiệu năng xử lý request vượt trội hơn Controllers truyền thống.

---

## 1. So sánh Minimal APIs vs Controllers

- **Controllers**: Mô hình truyền thống dựa trên kế thừa `ControllerBase`, nặng hơn về mặt khởi tạo và tốn nhiều file mẫu.
- **Minimal APIs**: Khai báo endpoint trực tiếp trên `WebApplication` bằng Lambda expressions, hiệu năng cao hơn ~20-30%, cực kỳ thích hợp cho Microservices.

---

## 2. Cú pháp CRUD với Minimal APIs

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// GET danh sách
app.MapGet(""/api/todos"", async (AppDbContext db) =>
    await db.Todos.ToListAsync());

// GET theo ID
app.MapGet(""/api/todos/{id:int}"", async (int id, AppDbContext db) =>
    await db.Todos.FindAsync(id) is Todo item 
        ? Results.Ok(item) 
        : Results.NotFound());

// POST tạo mới
app.MapPost(""/api/todos"", async (Todo todo, AppDbContext db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($""/api/todos/{todo.Id}"", todo);
});

app.Run();
```

---

## 3. Trả về kết quả chuẩn kiểu với `TypedResults`

Trong .NET 8/9/10, thay vì dùng `Results`, hãy sử dụng `TypedResults` để hỗ trợ Unit Testing trực tiếp và tự động sinh OpenAPI/Swagger schemas chuẩn xác:

```csharp
app.MapGet(""/users/{id}"", async Task<Results<Ok<UserDto>, NotFound>> (int id, IUserService service) =>
{
    var user = await service.GetByIdAsync(id);
    return user != null ? TypedResults.Ok(user) : TypedResults.NotFound();
});
```

---

## 4. Tổ chức Route với `MapGroup()`

Để tránh file `Program.cs` bị phình to hàng ngàn dòng, sử dụng Route Groups để nhóm các endpoint cùng tiền tố:

```csharp
var productGroup = app.MapGroup(""/api/products"")
    .RequireAuthorization() // Bảo vệ toàn bộ nhóm bằng Auth
    .WithTags(""Products""); // Nhóm trên Swagger

productGroup.MapGet("""", GetAllProducts);
productGroup.MapPost("""", CreateProduct);
```"
            };

            t23.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Lợi ích vượt trội của việc dùng TypedResults so với Results trong Minimal APIs của .NET là gì?",
                OptionA = "TypedResults lưu thẳng vào Redis cache.",
                OptionB = "TypedResults chỉ định rõ ràng các kiểu trả về (Strongly Typed), giúp viết Unit Test dễ dàng mà không cần ép kiểu và tự động sinh Swagger tài liệu chuẩn xác.",
                OptionC = "TypedResults chỉ dùng được cho mobile.",
                OptionD = "TypedResults tự động nén ảnh.",
                CorrectOption = "B",
                Explanation = "TypedResults cung cấp kiểu trả về tường minh (ví dụ: Results<Ok<T>, NotFound>), giúp kiểm thử Unit Test trực tiếp và hỗ trợ OpenAPI sinh tài liệu schema chính xác.",
                Difficulty = DifficultyLevel.Advanced
            });

            t23.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Phương thức nào trong Minimal APIs cho phép gom nhóm các endpoint có chung tiền tố URL và chính sách bảo mật?",
                OptionA = "app.MapGroup()",
                OptionB = "app.UseRouting()",
                OptionC = "app.CombineEndpoints()",
                OptionD = "app.CreateSubModule()",
                CorrectOption = "A",
                Explanation = "app.MapGroup(\"/prefix\") cho phép gom nhóm các routes có cùng tiền tố, áp dụng chung Middleware, Authentication hoặc Tag tài liệu Swagger.",
                Difficulty = DifficultyLevel.Advanced
            });

            newTutorials.Add(t23);
        }
        #endregion

        #region Bài 24: Xác thực & Phân quyền (JWT Bearer & Identity)
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "xac-thuc-phan-quyen-aspnet-core-jwt-bearer-identity-rbac"))
        {
            var t24 = new Tutorial
            {
                CategoryId = catAspNetCore.Id,
                Title = "Bài 24: Xác thực & Phân quyền trong ASP.NET Core - JWT Bearer, Identity & RBAC",
                Slug = "xac-thuc-phan-quyen-aspnet-core-jwt-bearer-identity-rbac",
                Summary = "Bảo mật Web API chuẩn doanh nghiệp với JSON Web Tokens (JWT), cấu hình Authentication/Authorization Middleware, Claim-based và Role-based Authorization.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 24,
                IsFeatured = true,
                ContentMarkdown = @"# Xác thực & Phân quyền trong ASP.NET Core

Bảo mật là tiêu chí sống còn của bất kỳ hệ thống phần mềm nào. Trong ASP.NET Core, hai khái niệm này được phân định rạch ròi:
- **Authentication (Xác thực)**: Xác định danh tính người dùng (Bạn là ai?).
- **Authorization (Phân quyền)**: Xác định quyền hạn truy cập (Bạn được phép làm gì?).

---

## 1. Cấu trúc JSON Web Token (JWT)

JWT là tiêu chuẩn mở (RFC 7519) định dạng JSON nhỏ gọn và an toàn, gồm 3 phần ngăn cách bởi dấu chấm (`.`):

```
┌─────────────────────────────────────────────────────────────┐
│                    CẤU TRÚC JSON WEB TOKEN                  │
├─────────────────┬───────────────────┬───────────────────────┤
│ Header          │ Payload           │ Signature             │
├─────────────────┼───────────────────┼───────────────────────┤
│ Thuật toán mã   │ Dữ liệu Claims    │ Chữ ký số bí mật      │
│ hóa (HS256)     │ (UserId, Email,   │ (HMACSHA256 với Secret│
│                 │ Roles, Expire)    │ Key của Server)       │
└─────────────────┴───────────────────┴───────────────────────┘
```

---

## 2. Cấu hình JWT Bearer trong `Program.cs`

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration[""Jwt:Issuer""],
            ValidAudience = builder.Configuration[""Jwt:Audience""],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration[""Jwt:Key""]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication(); // BẮT BUỘC ĐỨNG TRƯỚC UseAuthorization
app.UseAuthorization();
```

---

## 3. Phân quyền theo vai trò (Role-Based Access Control - RBAC)

Sử dụng thuộc tính `[Authorize]` trên Controller hoặc Minimal API:

```csharp
[ApiController]
[Route(""api/[controller]"")]
public class AdminController : ControllerBase
{
    // Bất kỳ ai đăng nhập đều vào được
    [Authorize]
    [HttpGet(""thong-tin-ca-nhan"")]
    public IActionResult GetProfile() => Ok(User.Identity?.Name);

    // CHỈ VAI TRÒ ADMIN MỚI ĐƯỢC PHÉP TRUY CẬP
    [Authorize(Roles = ""Admin"")]
    [HttpDelete(""xoa-nguoi-dung/{id}"")]
    public IActionResult DeleteUser(int id) => Ok($""Đã xóa user {id}"");
}
```"
            };

            t24.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Trong pipeline của ASP.NET Core, thứ tự sắp xếp giữa UseAuthentication và UseAuthorization như thế nào là đúng?",
                OptionA = "UseAuthorization phải đứng trước UseAuthentication.",
                OptionB = "UseAuthentication phải đứng trước UseAuthorization (Xác thực xem người dùng là ai trước khi kiểm tra quyền hạn của họ).",
                OptionC = "Thứ tự nào cũng được không quan trọng.",
                OptionD = "Chỉ cần dùng UseAuthorization là đủ cả hai.",
                CorrectOption = "B",
                Explanation = "Quy tắc bắt buộc: Hệ thống phải xác định được danh tính người dùng là ai (Authentication) trước khi có thể kiểm tra xem họ có quyền thực hiện thao tác đó hay không (Authorization).",
                Difficulty = DifficultyLevel.Advanced
            });

            t24.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Phần nào trong Token JWT đảm bảo rằng token không bị giả mạo hoặc chỉnh sửa trái phép bởi hacker?",
                OptionA = "Header",
                OptionB = "Payload",
                OptionC = "Signature (Chữ ký số được tạo từ Header + Payload kết hợp Secret Key của Server)",
                OptionD = "Base64 encoding",
                CorrectOption = "C",
                Explanation = "Signature được tạo bằng cách băm Header và Payload với Secret Key mà chỉ server biết. Nếu hacker sửa dữ liệu trong Payload, chữ ký sẽ không khớp và server từ chối ngay lập tức.",
                Difficulty = DifficultyLevel.Advanced
            });

            newTutorials.Add(t24);
        }
        #endregion

        #region Bài 25: Logging, Caching & Tối ưu Hiệu năng Web API
        if (!await context.Tutorials.AnyAsync(t => t.Slug == "logging-caching-toi-uu-hieu-nang-web-api-serilog-redis"))
        {
            var t25 = new Tutorial
            {
                CategoryId = catAspNetCore.Id,
                Title = "Bài 25: Logging, Caching & Tối ưu Hiệu năng Web API - Serilog, Redis & Output Caching",
                Slug = "logging-caching-toi-uu-hieu-nang-web-api-serilog-redis",
                Summary = "Tối ưu hiệu năng và giám sát hệ thống production với Structured Logging qua Serilog, In-Memory Caching, Distributed Cache (Redis) và Output Caching trong .NET 8/9/10.",
                EstimatedReadingMinutes = 12,
                Difficulty = DifficultyLevel.Advanced,
                OrderIndex = 25,
                IsFeatured = true,
                ContentMarkdown = @"# Logging, Caching & Tối ưu Hiệu năng Web API

Khi đưa ứng dụng lên môi trường Production chịu tải hàng triệu người dùng, hai yếu tố quyết định sự sống còn của hệ thống là **Khả năng quan sát (Observability qua Logging)** và **Tốc độ phản hồi (Caching)**.

---

## 1. Structured Logging với Serilog

Lệnh `Console.WriteLine` truyền thống xuất ra chuỗi vô định hình, không thể tìm kiếm hay thống kê trên các công cụ tập trung như Seq, Elasticsearch hay Grafana. **Structured Logging** lưu trữ log dưới dạng các thuộc tính JSON có cấu trúc:

```csharp
// ĐÚNG CHUẨN STRUCTURED LOGGING:
logger.LogInformation(""Người dùng {UserId} đã đặt đơn hàng {OrderId} với số tiền {TongTien:C}"", 
    user.Id, order.Id, order.TongTien);
```

---

## 2. Caching: In-Memory vs Distributed Cache (Redis)

```
┌─────────────────────────────────────────────────────────────┐
│                    CHIẾN LƯỢC CACHING .NET                  │
├───────────────────┬─────────────────────────────────────────┤
│ IMemoryCache      │ Lưu trong RAM của Web Server hiện tại.  │
│                   │ Cực nhanh (< 1ms) nhưng mất khi restart │
│                   │ Không đồng bộ được khi chạy nhiều server│
├───────────────────┼─────────────────────────────────────────┤
│ IDistributedCache │ Lưu trên cụm Redis độc lập.             │
│ (Redis)           │ Dữ liệu tồn tại độc lập với Web Server. │
│                   │ Chuẩn mực cho Microservices & K8s.     │
└───────────────────┴─────────────────────────────────────────┘
```

Sử dụng `IMemoryCache`:
```csharp
public async Task<List<Product>> GetProductsAsync(IMemoryCache cache, AppDbContext db)
{
    return await cache.GetOrCreateAsync(""danh_sach_san_pham"", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        entry.SlidingExpiration = TimeSpan.FromMinutes(2);
        return await db.Products.AsNoTracking().ToListAsync();
    }) ?? new();
}
```

---

## 3. Output Caching trong .NET 8/9/10

Tính năng **Output Caching** cho phép cache toàn bộ HTTP Response mà không cần qua Controller:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(p => p.Expire(TimeSpan.FromMinutes(5)));
});

var app = builder.Build();
app.UseOutputCache();

// Endpoint này được cache toàn bộ response trong 60 giây:
app.MapGet(""/api/tin-tuc"", () => LayTinTucMoiNhat())
   .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(60)));
```"
            };

            t25.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Sự khác biệt căn bản giữa In-Memory Cache và Distributed Cache (như Redis) là gì?",
                OptionA = "In-Memory Cache chỉ dùng được cho ngôn ngữ Java.",
                OptionB = "In-Memory Cache lưu trên RAM của chính máy chủ web (nhanh nhưng không chia sẻ giữa nhiều server); còn Distributed Cache lưu trên hệ thống lưu trữ tập trung riêng, chia sẻ dữ liệu chung cho cụm nhiều máy chủ web.",
                OptionC = "Distributed Cache chỉ dùng được cho file ảnh.",
                OptionD = "Cả hai hoàn toàn giống nhau.",
                CorrectOption = "B",
                Explanation = "In-Memory Cache lưu trực tiếp trong process của web app nên siêu nhanh nhưng không chia sẻ được khi scale out nhiều server. Distributed Cache (Redis) là kho lưu trữ chung ở tầng ngoài phục vụ cụm nhiều instance web app cùng chia sẻ cache.",
                Difficulty = DifficultyLevel.Advanced
            });

            t25.QuizQuestions.Add(new QuizQuestion
            {
                Question = "Tính năng Output Caching trong ASP.NET Core có công dụng nổi bật gì?",
                OptionA = "Tự động gửi email thông báo khi server khởi động.",
                OptionB = "Tự động lưu trữ và trả về toàn bộ kết quả HTTP Response của endpoint mà không cần thực thi lại mã xử lý bên trong, giúp giảm tải tối đa cho máy chủ.",
                OptionC = "Xóa toàn bộ cookie của người dùng.",
                OptionD = "Chặn kết nối từ các IP nước ngoài.",
                CorrectOption = "B",
                Explanation = "Output Caching lưu lại toàn bộ payload HTTP response đã sinh ra, khi có request tiếp theo phù hợp sẽ trả về ngay lập tức mà không phải chạy lại controller hay database query.",
                Difficulty = DifficultyLevel.Advanced
            });

            newTutorials.Add(t25);
        }
        #endregion

        if (newTutorials.Any())
        {
            context.Tutorials.AddRange(newTutorials);
            await context.SaveChangesAsync();
        }
    }
}
