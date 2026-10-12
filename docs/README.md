# BỘ HỒ SƠ ĐẶC TẢ YÊU CẦU & TỔNG QUAN HỆ THỐNG .NET TUTOS
## (SYSTEM REQUIREMENTS SPECIFICATIONS & ARCHITECTURAL OVERVIEW)

> **Tác giả:** Senior Business Analyst / Enterprise Solution Specialist  
> **Dự án:** Hệ sinh thái Đào tạo & Thực hành Lập trình .NET (.NET Tutos Platform)  
> **Phạm vi:** Web App (ASP.NET Core 10), Mobile App (.NET MAUI 10), AI Tutor Engine, Roslyn Sandbox  
> **Phiên bản:** Release 2.0 (Tháng 10/2026)  
> **Trạng thái:** Đã kiểm định & Sẵn sàng bàn giao (Verified & Ready for Production)  

---

## 1. Giới thiệu bộ tài liệu (Document Suite Overview)

Bộ hồ sơ đặc tả này được thiết lập bởi **Senior Business Analyst**, đóng vai trò là **Nguồn Sự Thật Duy Nhất (Single Source of Truth - SSOT)** cho toàn bộ đội ngũ phát triển sản phẩm (Product Owners, Solution Architects, Lead Developers, QA Engineers, DevOps và Stakeholders).

Tài liệu chuẩn hóa toàn diện từ góc độ bài toán kinh doanh, chân dung học viên, kiến trúc hệ sinh thái đa nền tảng, yêu cầu chức năng (BRD), yêu cầu phi chức năng (NFR), cấu trúc cơ sở dữ liệu cho đến các giao thức API kết nối giữa Web App và Mobile App.

---

## 2. Mục lục bộ hồ sơ đặc tả (Table of Contents)

Các tài liệu chi tiết được cấu trúc theo chuẩn quốc tế trong thư mục này:

```
d:\.ASPNET-Tutos\docs\
├── README.md                           # Bản đồ tài liệu & Ma trận truy vết (Tài liệu này)
├── 01_business_overview.md             # Tổng quan kinh doanh, Đề xuất giá trị & Chân dung người dùng
├── 02_system_architecture_overview.md  # Kiến trúc hệ sinh thái, Sơ đồ tầng & Luồng dữ liệu (Data Flow)
├── 03_functional_requirements_brd.md   # Đặc tả yêu cầu chức năng (BRD) 7 phân hệ cốt lõi
├── 04_non_functional_requirements.md   # Đặc tả yêu cầu phi chức năng (NFR) về hiệu năng, bảo mật, PWA
└── 05_data_models_and_api_specs.md     # Mô hình dữ liệu ERD & Đặc tả giao tiếp REST API Web/Mobile
```

### Chi tiết nội dung từng văn bản:

1. [**01. Tổng quan Kinh doanh & Đề xuất Giá trị (Business Overview & Value Proposition)**](file:///d:/.ASPNET-Tutos/docs/01_business_overview.md)
   - Tóm tắt điều hành dự án (Executive Summary).
   - Phân tích nỗi đau thị trường & 4 bài toán cốt lõi.
   - Đề xuất giá trị độc nhất (USP) và mục tiêu chiến lược (KPIs).
   - 3 Chân dung người dùng mục tiêu (Sinh viên/Fresher, Junior Developer, LMS Instructor).
   - Bản đồ hành trình học viên (User Journey Map).

2. [**02. Kiến trúc Hệ thống Tổng thể (System Architecture Overview)**](file:///d:/.ASPNET-Tutos/docs/02_system_architecture_overview.md)
   - Sơ đồ cấu trúc hệ sinh thái phân tầng (Presentation, API Gateway, Service Core, Data Access, Persistence).
   - Bảng thông số kỹ thuật công nghệ (.NET 10, C# 14, .NET MAUI, EF Core, Roslyn, SignalR, Monaco Editor).
   - Sơ đồ luồng dữ liệu tuần tự (Sequence Diagrams) cho:
     - Biên dịch & Chấm điểm bài tập trong Monaco Playground Sandbox.
     - Phỏng vấn kỹ thuật giả lập tương tác với AI Tech Lead.
     - Đồng bộ dữ liệu hai chiều giữa Mobile App và Web App.
   - Chiến lược bảo mật cách ly Sandbox và kiểm soát rủi ro an toàn thông tin.

3. [**03. Đặc tả Yêu cầu Chức năng (Business Requirements Document - BRD)**](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md)
   - Đặc tả chi tiết 7 phân hệ nghiệp vụ với User Stories và Acceptance Criteria:
     - **MOD-LMS:** 101 bài học chuẩn Microsoft .NET, Markdown rendering, Quiz trắc nghiệm.
     - **MOD-PLAY:** Monaco Editor trực tuyến, phím tắt `Ctrl+Enter`, `Ctrl+S`, `Ctrl+I`, Coding Challenges Judge Engine.
     - **MOD-AI:** Đo độ phức tạp thuật toán Big-O ($O(1) \dots O(2^N)$), chẩn đoán rớt test case, sinh xUnit tests.
     - **MOD-INT:** Sát hạch phỏng vấn AI Tech Lead, chấm điểm 3 tiêu chí, Speech-to-Text, xuất báo cáo radar năng lực.
     - **MOD-GAM:** Chuỗi ngày học Streak, điểm kinh nghiệm XP, bảng vinh danh Hall of Fame, SignalR Discussion.
     - **MOD-MOB:** Client di động .NET MAUI, đọc bài học, đồng bộ tiến độ, theme sáng/tối, cấu hình IP server.
     - **MOD-ADM:** Bảng điều khiển quản trị LMS Admin.

4. [**04. Đặc tả Yêu cầu Phi Chức năng (Non-Functional Requirements - NFR)**](file:///d:/.ASPNET-Tutos/docs/04_non_functional_requirements.md)
   - Chỉ số hiệu năng (SLAs/SLOs): Thời gian tải trang $< 1.2\text{s}$, phản hồi API $< 150\text{ms}$, Sandbox timeout $\le 3\text{s}$.
   - Khả năng chịu tải đồng thời $\ge 1.000$ người dùng, tối ưu bộ nhớ qua `AsNoTracking()` và phân trang.
   - Bảo mật OWASP: Chống XSS, CSRF Token, Parameterized Queries chống SQL Injection, mã hóa mật khẩu PBKDF2.
   - Khả năng hoạt động ngoại tuyến với Service Worker PWA và độ tương thích đa nền tảng (Responsive, Dark/Light Mode).

5. [**05. Mô hình Dữ liệu & Đặc tả Giao tiếp API (Data Models & API Specifications)**](file:///d:/.ASPNET-Tutos/docs/05_data_models_and_api_specs.md)
   - Sơ đồ quan hệ thực thể dữ liệu (ERD Diagram).
   - Đặc tả các bảng dữ liệu: `ApplicationUser`, `Tutorial`, `CodingChallenge`, `CodeTestCase`, `InterviewFlashcard`, `UserLessonProgress`.
   - Đặc tả hợp đồng API RESTful (Request/Response JSON):
     - Xác thực và đồng bộ tài khoản Mobile (`/api/mobile/auth/*`).
     - Đánh dấu hoàn thành bài học và đồng bộ XP/Streak (`/api/mobile/tutorials/*`).
     - Đo độ phức tạp Big-O, chẩn đoán lỗi và sinh xUnit (`/AiTutor/*`).
     - Chấm điểm phỏng vấn, câu hỏi xoáy và báo cáo năng lực (`/Interview/*`).

---

## 3. Ma trận truy vết yêu cầu (Requirements Traceability Matrix - RTM)

| Mục tiêu kinh doanh (Business Goal) | Phân hệ chức năng (Module ID) | Tệp mã nguồn thực thi chính (Implementation Components) | Kiểm chứng chất lượng (Verification) |
| :--- | :--- | :--- | :--- |
| **Code không rào cản trên Web** | `MOD-PLAY` | `PlaygroundController.cs`, `CodeExecutionService.cs`, `Index.cshtml` (Playground) | Unit tests, Roslyn Scripting sandbox, Monaco Editor |
| **Trợ lý AI hướng dẫn trực tiếp** | `MOD-AI` | `AiTutorController.cs`, `AiTutorService.cs`, `AiTutorViewModels.cs` | Big-O analysis, Diagnose failure, Generate xUnit |
| **Sát hạch tuyển dụng chuẩn Senior** | `MOD-INT` | `InterviewController.cs`, `AiTutorService.cs`, `Mock.cshtml` | 3-criteria evaluation, Speech-to-Text, Radar report |
| **Học tập đa nền tảng liên tục** | `MOD-MOB` | `MobileAuthController.cs`, `ApiService.cs` (.NET MAUI), `LoginPage.xaml` | JWT Bearer, Preferences cache, Sync XP & Streak |
| **Giữ chân học viên & thi đua** | `MOD-GAM` | `StreakService.cs`, `LeaderboardService.cs`, `ActivityFeedService.cs` | Daily check-in, Level formula, Hall of Fame |

---

## 4. Hướng dẫn khai thác tài liệu theo vai trò (Role-based Guide)

- **Dành cho Product Owner / Stakeholder:** Xem [Tài liệu 01](file:///d:/.ASPNET-Tutos/docs/01_business_overview.md) và [Tài liệu 03](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md) để nắm lộ trình phát triển sản phẩm, tính năng và mục tiêu đo lường kinh doanh.
- **Dành cho Solution Architect / Tech Lead:** Xem [Tài liệu 02](file:///d:/.ASPNET-Tutos/docs/02_system_architecture_overview.md) và [Tài liệu 05](file:///d:/.ASPNET-Tutos/docs/05_data_models_and_api_specs.md) để nắm cấu trúc tầng, sơ đồ luồng dữ liệu và thiết kế cơ sở dữ liệu.
- **Dành cho Software Developers (Web & Mobile):** Xem [Tài liệu 03](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md) để nắm Acceptance Criteria và [Tài liệu 05](file:///d:/.ASPNET-Tutos/docs/05_data_models_and_api_specs.md) để tích hợp đúng các API Contracts.
- **Dành cho QA / Test Engineers:** Dựa vào Acceptance Criteria trong [Tài liệu 03](file:///d:/.ASPNET-Tutos/docs/03_functional_requirements_brd.md) và các chỉ số SLAs/SLOs trong [Tài liệu 04](file:///d:/.ASPNET-Tutos/docs/04_non_functional_requirements.md) để viết Test Cases kiểm thử chức năng và kiểm thử tải.
