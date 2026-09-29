# .NET DevMaster - Nền Tảng Học & Hướng Dẫn .NET Từ Cơ Bản Đến Nâng Cao

Ứng dụng web được phát triển bằng **ASP.NET Core MVC (.NET 10)** và **Entity Framework Core**, vừa đóng vai trò là một **dự án mẫu chuẩn mực**, vừa là **cổng tài liệu hướng dẫn học tập** hệ sinh thái .NET & C# toàn diện cho người mới bắt đầu đến nâng cao.

---

## 🌟 Tính Năng Nổi Bật

1. **Lộ trình học trực quan (Roadmap 4 Giai đoạn)**:
   - **Giai đoạn 1**: Nền tảng .NET & Cú pháp C# cơ bản (Biến, Kiểu dữ liệu, Cấu trúc rẽ nhánh, Vòng lặp, Collections, StringBuilder).
   - **Giai đoạn 2**: Lập trình Hướng đối tượng (4 trụ cột OOP, Interface, Generics, LINQ & Lambda, Task Async/Await).
   - **Giai đoạn 3**: Quản trị dữ liệu với Entity Framework Core (Code-First, DbContext, Migrations, CRUD, AsNoTracking).
   - **Giai đoạn 4**: Lập trình Web ASP.NET Core MVC & RESTful API (Middleware Pipeline, Controllers, Razor, Dependency Injection 3 Lifetimes, SOLID Principles).
2. **Giao diện đọc bài viết tối ưu (Reading Experience)**:
   - Hỗ trợ Markdown rendering đầy đủ (Bảng biểu, Trích dẫn, Danh sách, Ghi chú cảnh báo).
   - Tô màu cú pháp mã nguồn (Syntax Highlighting) với **Prism.js** (chuẩn theme VS Code Tomorrow Night).
   - Nút **Sao chép mã nguồn (Copy to Clipboard)** một chạm.
   - Thanh tiến trình đọc bài (Reading Progress Indicator) trên cùng.
   - Điều hướng chuyển bài học "Bài trước / Bài tiếp theo" mượt mà.
3. **Trung tâm Trắc nghiệm kiến thức (Interactive Quiz Center)**:
   - Làm bài trắc nghiệm theo từng bài học hoặc theo cấp độ (Cơ bản / Trung cấp / Nâng cao).
   - Chấm điểm ngay lập tức, hiển thị câu đúng màu xanh, câu sai màu đỏ kèm **lời giải thích chi tiết**.
4. **Bảng tra cứu Code mẫu (Cheat Sheet)**:
   - Tổng hợp các đoạn mã thông dụng của C#, LINQ, Entity Framework Core và ASP.NET Core.
5. **Thiết kế hiện đại & Responsive**:
   - Tương thích tốt trên máy tính, máy tính bảng và điện thoại di động (Bootstrap 5).
   - Hỗ trợ chuyển đổi giao diện **Sáng / Tối (Dark / Light Mode)** tự động lưu trạng thái vào trình duyệt.

---

## 🏗️ Cấu Trúc Thư Mục Dự Án

```
d:/.ASPNET-Tutos/
├── NET-Tutos.slnx                      # Solution file (.NET 10 XML format)
├── NET-Tutos/                          # Thư mục dự án Web ASP.NET Core MVC
│   ├── NET-Tutos.csproj                # File cấu hình dự án
│   ├── Controllers/                    # Bộ điều khiển (MVC Controllers)
│   │   ├── HomeController.cs           # Trang chủ, giới thiệu, thống kê
│   │   ├── TutorialsController.cs      # Danh sách bài học, chi tiết bài viết, tìm kiếm
│   │   ├── RoadmapController.cs        # Lộ trình học 4 giai đoạn
│   │   ├── QuizController.cs           # Trung tâm trắc nghiệm và chấm điểm
│   │   └── CheatSheetController.cs     # Bảng tra cứu code snippets
│   ├── Data/                           # Tầng dữ liệu & ORM
│   │   ├── AppDbContext.cs             # Entity Framework Core DbContext
│   │   └── DbInitializer.cs            # Tự động nạp sẵn dữ liệu bài học & câu hỏi
│   ├── Models/                         # Thực thể (Entities) & ViewModels
│   │   ├── Entities/                   # Category, Tutorial, QuizQuestion, CodeSnippet
│   │   └── ViewModels/                 # HomeViewModel, TutorialDetailViewModel, v.v.
│   ├── Services/                       # Tầng dịch vụ nghiệp vụ (Business Logic)
│   │   ├── ITutorialService.cs / TutorialService.cs
│   │   └── IMarkdownService.cs / MarkdownService.cs (Markdig)
│   ├── Views/                          # Giao diện Razor (.cshtml)
│   │   ├── Home/                       # Index, About
│   │   ├── Tutorials/                  # Index, Details
│   │   ├── Roadmap/                    # Index
│   │   ├── Quiz/                       # Index, Take
│   │   ├── CheatSheet/                 # Index
│   │   └── Shared/                     # _Layout.cshtml (Navbar, Footer, Dark Mode)
│   ├── wwwroot/                        # Tệp tĩnh (CSS, JS, Fonts, Libraries)
│   │   ├── css/site.css
│   │   └── js/site.js
│   ├── appsettings.json                # Cấu hình CSDL (SQL Server / SQLite)
│   └── Program.cs                      # Cấu hình DI Container, Middleware & Khởi chạy DB
└── README.md
```

---

## 🚀 Hướng Dẫn Khởi Chạy Dự Án

### Yêu cầu hệ thống:
- Đã cài đặt **.NET 10 SDK** (hoặc .NET 8 trở lên).

### Bước 1: Mở Terminal tại thư mục dự án
```powershell
cd d:\.ASPNET-Tutos
```

### Bước 2: Chạy ứng dụng
```powershell
dotnet run --project NET-Tutos/NET-Tutos.csproj
```

Mở trình duyệt web và truy cập vào địa chỉ hiển thị trong terminal (ví dụ: `http://localhost:5262` hoặc `https://localhost:7262`).

---

## 🗄️ Cấu Hình Cơ Sở Dữ Liệu (SQL Server & SQLite)

Trong tệp `appsettings.json`:
```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DotNetTutorialsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
    "SqliteConnection": "Data Source=dotnet_tutorials.db"
  }
}
```

- **Chế độ SQL Server (LocalDB)**: Được thiết lập sẵn theo đúng chuẩn dự án doanh nghiệp.
- **Cơ chế linh hoạt thông minh (Smart Fallback)**: Nếu máy tính chưa cài đặt sẵn SQL Server LocalDB, hệ thống sẽ tự động chuyển sang chế độ SQLite lưu tại `dotnet_tutorials.db` để ứng dụng có thể chạy ngay lập tức mà không bao giờ bị lỗi kết nối!
- Khi bạn đã cài đặt SQL Server / LocalDB, ứng dụng sẽ tự động kết nối và khởi tạo CSDL trên SQL Server.
