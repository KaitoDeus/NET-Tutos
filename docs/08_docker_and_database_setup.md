# 🐳 Hướng Dẫn Kiến Trúc PostgreSQL 17 & Docker Compose (.NET Tutos)

Document Type: Infrastructure, Database Architecture & DevOps Specification  
System: **.NET Tutos Platform**  
Database Engine: **PostgreSQL 17 (Alpine)**  
ORM Provider: **`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3**  
Container Orchestration: **Docker & Docker Compose**  
Status: **Đã triển khai & Xác minh Production Ready**

---

## 1. Tổng Quan Kiến Trúc Cơ Sở Dữ Liệu

Nhằm nâng cao tính khả chuyển (portability), hiệu năng truy vấn đồng thời và khả năng triển khai độc lập môi trường (máy phát triển, CI/CD, máy chủ Cloud), hệ thống .NET Tutos đã chuyển đổi cơ sở dữ liệu chính sang **PostgreSQL 17** điều phối qua **Docker Compose**.

```mermaid
graph TD
    subgraph HostMachine ["Máy Phát Triển / Host Machine (Windows / Linux / macOS)"]
        VSCode["VS Code (Database Client / SQLTools)"]
        DotnetLocal["dotnet run (web/web.csproj)"]
        Browser["Trình duyệt Web (http://localhost:5000)"]
    end

    subgraph DockerCompose ["Môi Trường Docker Compose (net-tutos_default)"]
        subgraph WebContainer ["Container: nettutos-web"]
            WebApp["ASP.NET Core 10 MVC & REST API<br/>Cổng nội bộ: 5000"]
        end

        subgraph PostgresContainer ["Container: nettutos-postgres (Healthy)"]
            PostgresDB[("PostgreSQL 17 Alpine<br/>Database: nettutos_db<br/>31 Tables & Sequences")]
        end

        subgraph PgAdminContainer ["Container: nettutos-pgadmin"]
            PgAdmin["pgAdmin 4 Web GUI<br/>Cổng: 5050"]
        end

        Volume[("Volume: postgres_data")]
    end

    Browser -->|Port 5000| WebApp
    VSCode -->|Port 5432 / Trust Auth| PostgresDB
    DotnetLocal -->|Host=localhost:5432| PostgresDB
    WebApp -->|Host=postgres:5432| PostgresDB
    PgAdmin -->|Host=postgres:5432| PostgresDB
    PostgresDB --- Volume
```

---

## 2. Đặc Tả Dịch Vụ Trong `docker-compose.yml`

Hệ thống điều phối 3 dịch vụ liên kết chặt chẽ trong tệp [`docker-compose.yml`](file:///d:/NET-Tutos/docker-compose.yml):

| Tên Dịch Vụ | Image / Build | Cổng Mở (Host:Container) | Vai Trò & Cấu Hình |
| :--- | :--- | :--- | :--- |
| **`postgres`** | `postgres:17-alpine` | `5432:5432` | Cung cấp CSDL PostgreSQL chính. Lưu trữ bền vững qua volume `postgres_data`. Kèm healthcheck `pg_isready`. |
| **`web`** | `build: . (Dockerfile)` | `5000:5000` | Ứng dụng web ASP.NET Core 10. Tự động chờ `postgres` đạt trạng thái *healthy* trước khi khởi chạy. |
| **`pgadmin`** | `dpage/pgadmin4:latest` | `5050:80` | Giao diện đồ họa web quản lý database trực quan (Email: `admin@nettutos.com`, Mật khẩu: `admin`). |

---

## 3. Cấu Hình & Tương Thích Entity Framework Core (Npgsql)

### 3.1. Package NuGet
Dự án [`web/web.csproj`](file:///d:/NET-Tutos/web/web.csproj) được tích hợp gói:
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
```

### 3.2. Cơ chế Tự Động Thích Ứng (Resilient Provider Auto-Fallback)
Trong [`web/Program.cs`](file:///d:/NET-Tutos/web/Program.cs), ứng dụng triển khai cơ chế kiểm tra kết nối chủ động:
1. **Ưu tiên PostgreSQL**: Thử kết nối tới PostgreSQL với `Timeout = 3s`. Nếu thành công, kích hoạt `options.UseNpgsql(postgreSqlConnection)`.
2. **Tự động Fallback sang SQLite**: Nếu Docker chưa bật, ứng dụng tự động chuyển sang `dotnet_tutorials.db` (SQLite) để developer luôn chạy được ứng dụng ngay lập tức mà không gặp lỗi crash.
3. **Legacy Timestamp Behavior**: Kích hoạt `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` để hỗ trợ linh hoạt các kiểu `DateTime` (UTC, Local, Unspecified) mà không bị xung đột múi giờ.

### 3.3. Danh Mục 31 Bảng Dữ Liệu Chuẩn Hóa
Hệ thống quản lý 31 bảng quan hệ trong schema `public`:
- **Identity & Phân quyền**: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.
- **LMS Khóa học & Bài học**: `Categories`, `Tutorials`, `QuizQuestions`, `CodeSnippets`, `UserLessonProgresses`, `CourseEnrollments`, `QuizAttempts`, `Certificates`.
- **Roslyn Playground & Coding Challenges**: `CodingChallenges`, `CodeTestCases`, `CodeSubmissions`.
- **Cộng đồng & Thảo luận**: `DiscussionComments`, `CommentUpvotes`.
- **Gamification & Hoạt động**: `UserBadges`, `DailyCheckIns`, `UserNotifications`, `ActivityFeedItems`.
- **Đồ án Capstone**: `CapstoneProjects`, `ProjectSubmissions`.
- **Phỏng vấn Kỹ thuật & Ôn tập**: `InterviewFlashcards`, `UserFlashcardProgresses`.
- **Kế hoạch Học tập & Portfolio**: `StudyPlans`, `StudyPlanItems`, `DeveloperProfiles`.

---

## 4. Hướng Dẫn Vận Hành Hệ Thống

### Cách 1: Khởi Chạy Toàn Bộ Hệ Thống (Docker Compose)
Dành cho việc kiểm thử trọn gói hoặc triển khai lên môi trường máy chủ:
```bash
docker compose up -d
```
- **Web App**: [http://localhost:5000](http://localhost:5000)
- **Swagger REST API**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **pgAdmin 4 GUI**: [http://localhost:5050](http://localhost:5050)

Dừng hệ thống:
```bash
docker compose down
```

---

### Cách 2: Lập Trình Cục Bộ (.NET SDK + Postgres Container)
Dành cho lập trình viên muốn code C# / Razor / APIs và debug trực tiếp trên máy:
```powershell
# 1. Khởi động riêng PostgreSQL container
docker compose up -d postgres

# 2. Chạy ứng dụng web bằng .NET CLI (hoặc F5 trong IDE)
dotnet run --project web/web.csproj
```

---

## 5. Kết Nối Database Từ Công Cụ Bên Ngoài (VS Code / DBeaver)

### Thông số kết nối:
- **Host**: `localhost` (hoặc `127.0.0.1`)
- **Port**: `5432`
- **Database**: `nettutos_db`
- **User**: `postgres`
- **Password**: `postgres`
- **SSL Mode**: `disable` (hoặc `false`)
- **Connection URI**: `postgresql://postgres:postgres@localhost:5432/nettutos_db`

### Cấu hình sẵn cho VS Code:
Dự án đã đính kèm sẵn tệp [`.vscode/settings.json`](file:///d:/NET-Tutos/.vscode/settings.json) với kết nối `NET-Tutos PostgreSQL (Docker)`. Bạn chỉ cần mở extension **SQLTools** hoặc **Database Client** và nhấn **Connect**.
