# 🏛️ KẾ HOẠCH TÁCH RỜI BACKEND & FRONTEND (DECOUPLING BLUEPRINT)

Dự án: **NET-Tutos**  
Mô hình đích: **ASP.NET Core 10 Web API (`backend/`) + Next.js 15 React TypeScript (`frontend/`) + .NET MAUI (`mobile/`)**  
Nguyên tắc cốt lõi: **Giữ nguyên 100% logic ứng dụng Mobile (`mobile/`), kế thừa toàn bộ Database/Services hiện có, đặt tên thư mục tinh gọn không tiền tố.**

---

## 1. SO SÁNH KIẾN TRÚC: HIỆN TẠI VS TƯƠNG LAI

```
=== KIẾN TRÚC HIỆN TẠI (Monolith Hybrid) ===
┌─────────────────────────────────────────────────────────────┐
│                    NET-Tutos.WebApp                         │
│  ┌───────────────────────┐       ┌───────────────────────┐  │
│  │ Razor Views + MVC     │       │ API Controllers       │  │
│  │ (Server-Side Render)  │       │ (/api/mobile, /api)   │  │
│  └───────────┬───────────┘       └───────────┬───────────┘  │
│              └───────────────┬───────────────┘              │
│                     Services & AppDbContext                 │
└──────────────────────────────┼──────────────────────────────┘
                               │
            ┌──────────────────┴──────────────────┐
            ▼                                     ▼
     Trình duyệt Web                               mobile
   (HTML/CSS từ Razor)                          (.NET MAUI)


=== KIẾN TRÚC MỤC TIÊU (Decoupled Micro-Frontend / Headless API) ===
┌─────────────────────────┐               ┌─────────────────────────┐
│        frontend/        │               │         mobile/         │
│     Next.js 15 Web      │               │       (.NET MAUI)       │
│  • Monaco C# Editor     │               │  • Offline sync         │
│  • Markdown & KaTeX     │               │  • Native Android/iOS   │
│  • SSR / SSG SEO        │               │  • Tiêu thụ REST API    │
└────────────┬────────────┘               └────────────┬────────────┘
             │ JSON / REST + SignalR                   │ JSON / REST
             └───────────────────┬─────────────────────┘
                                 ▼
┌───────────────────────────────────────────────────────────────────┐
│                      backend/ (ASP.NET Core 10 Web API)           │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ Middleware Pipeline: CORS, RateLimit, JWT Bearer, Exception │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │ Controllers: Auth, Tutorials, Playground, AI, Flashcards    │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │ SignalR Hubs: DiscussionHub (/hubs/discussion)              │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │ Business Services: Roslyn Execution, AI Tutor, Streak, XP   │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │ Data Layer: Entity Framework Core 10 (SQL Server)           │  │
│  └─────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────┬─────────────────────────────────┘
                                  ▼
                       Microsoft SQL Server Database
```

---

## 2. TỔ CHỨC CẤU TRÚC THƯ MỤC TINH GỌN (SOLUTION STRUCTURE)

```
d:/NET-Tutos/
│
├── NET-Tutos.slnx                      # Solution quản lý các project .NET
│
├── backend/                            # [MỚI] Backend thuần Web API (.NET 10)
│   ├── Controllers/                    # Toàn bộ RESTful API Controllers
│   │   ├── AuthController.cs           # Login/Register/Profile/Refresh JWT
│   │   ├── TutorialsController.cs      # Bài học, Danh mục, Đánh dấu hoàn thành
│   │   ├── PlaygroundController.cs     # Roslyn Sandbox & Coding Challenge
│   │   ├── AiTutorController.cs        # Hỏi đáp trợ lý AI & Giải thích lỗi
│   │   ├── FlashcardsController.cs     # Thẻ ôn tập phỏng vấn & Spaced Repetition
│   │   ├── DiscussionController.cs     # Hỏi đáp, Bình luận & Upvote
│   │   ├── RoadmapController.cs        # Lộ trình học tập & Curriculum
│   │   ├── ProfileController.cs        # Hồ sơ học viên, Showcase & Portfolio
│   │   ├── LeaderboardController.cs    # Bảng xếp hạng XP & Chuỗi ngày học
│   │   └── Api/Mobile/                 # Kế thừa trọn vẹn API cho Mobile
│   ├── Data/                           # AppDbContext & Migrations
│   ├── Models/                         # Entities, DTOs & ViewModels
│   ├── Services/                       # Toàn bộ Business Logic đã tối ưu
│   ├── Hubs/                           # SignalR Realtime Hubs
│   ├── Program.cs                      # Cấu hình CORS, JWT, Rate Limiter, Swagger
│   └── appsettings.json                # Chuỗi kết nối DB & JWT Secret Key
│
├── mobile/                             # [ĐÃ ĐỔI TÊN] .NET MAUI Client (100% nguyên vẹn)
│   ├── Platforms/                      # Android, iOS, Windows, MacCatalyst
│   ├── Resources/                      # Icons, Splash, Fonts, Images
│   ├── Services/ApiService.cs          # Kết nối vào http://localhost:5000/api/mobile
│   ├── ViewModels/                     # MVVM logic
│   ├── Views/                          # XAML pages
│   └── NET_Tutos.Mobile.csproj
│
├── frontend/                           # [MỚI] Next.js 15 Web Application
│   ├── package.json
│   ├── tsconfig.json
│   ├── tailwind.config.ts
│   ├── src/
│   │   ├── app/                        # Next.js App Router (SSR & SSG)
│   │   │   ├── (auth)/                 # Login, Register
│   │   │   ├── tutorials/              # Danh sách bài giảng & Chi tiết bài học
│   │   │   │   └── [slug]/page.tsx     # SSR + SEO metadata
│   │   │   ├── playground/             # C# Sandbox với Monaco Code Editor
│   │   │   ├── ai-tutor/               # Giao diện Chatbot AI Gia Sư
│   │   │   ├── flashcards/             # Lật thẻ ôn tập Spaced Repetition
│   │   │   ├── roadmap/                # Sơ đồ tương tác lộ trình học
│   │   │   ├── leaderboard/            # Bảng vàng vinh danh học viên
│   │   │   └── profile/                # Trang cá nhân & Showcase
│   │   ├── components/                 # UI components tái sử dụng
│   │   │   ├── editor/MonacoEditor.tsx # Trình gõ code C#
│   │   │   ├── markdown/Renderer.tsx   # Parse Markdown + KaTeX + Prism syntax
│   │   │   └── ui/                     # Nút, Modal, Card, Dropdown
│   │   ├── lib/                        # Axios instance, SignalR client, Auth helpers
│   │   └── types/                      # TypeScript Interfaces khớp DTOs từ Backend
│
├── NET-Tutos.WebApp/                   # [TẠM GIỮ] Web MVC cũ (để đối chiếu / fallback)
└── docs/                               # Toàn bộ tài liệu kiến trúc & BRD/NFR/ERD
```

---

## 3. THIẾT KẾ BỘ API CONTRACTS (CHUẨN HÓA CHO FRONTEND & MOBILE)

### 3.1. Authentication & User Management
| Method | Endpoint | Mô tả | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Đăng nhập tài khoản, trả về JWT Access Token + Refresh Token | Public |
| `POST` | `/api/auth/register` | Đăng ký tài khoản mới | Public |
| `POST` | `/api/auth/refresh-token` | Làm mới JWT token mà không cần đăng nhập lại | Public |
| `GET` | `/api/auth/me` | Lấy thông tin user hiện tại (XP, Level, Streak, Roles) | Bearer Token |
| `POST` | `/api/auth/logout` | Hủy session/thu hồi token | Bearer Token |

### 3.2. Tutorials, Curriculum & Roadmap
| Method | Endpoint | Mô tả | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/categories` | Lấy danh sách danh mục và thống kê số lượng bài | Public |
| `GET` | `/api/tutorials` | Lấy danh sách bài viết (Search, Category, Pagination) | Public |
| `GET` | `/api/tutorials/{slug}` | Lấy chi tiết bài học (Markdown, Quizzes, Next/Prev) | Public (Auth để lấy status hoàn thành) |
| `POST` | `/api/tutorials/{id}/complete` | Đánh dấu/hủy hoàn thành bài học, tích luỹ +20 XP | Bearer Token |
| `GET` | `/api/roadmap` | Dữ liệu sơ đồ lộ trình học tập .NET Master Roadmap | Public |

### 3.3. C# Code Execution Sandbox (Roslyn)
| Method | Endpoint | Mô tả | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/playground/execute` | Biên dịch và chạy mã C# trong Sandbox cách ly | Public / Rate-limited |
| `GET` | `/api/playground/challenges` | Danh sách các bài tập thuật toán C# | Public |
| `POST` | `/api/playground/challenges/submit` | Nộp bài giải, chấm tự động qua TestCases & cấp XP | Bearer Token |

### 3.4. AI Tutor & Trợ lý học tập
| Method | Endpoint | Mô tả | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/aitutor/chat` | Hỏi đáp kiến thức C#/.NET với AI Tutor | Public / Token |
| `POST` | `/api/aitutor/explain-error` | Phân tích mã lỗi biên dịch C# (CS0103, CS0029,...) | Public / Token |

### 3.5. Flashcards & Spaced Repetition
| Method | Endpoint | Mô tả | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/flashcards/topics` | Thống kê số lượng thẻ và tiến độ thuộc bài theo chủ đề | Public / Token |
| `GET` | `/api/flashcards` | Lấy danh sách thẻ ôn tập theo chủ đề & độ khó | Public / Token |
| `POST` | `/api/flashcards/review` | Nộp đánh giá hộp nhớ Leitner Box & nhận thưởng XP | Bearer Token |

### 3.6. Discussion & Realtime
| Method | Endpoint | Mô tả | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/discussions/comments` | Lấy cây bình luận của bài viết / bài tập | Public |
| `POST` | `/api/discussions/comments` | Đăng bình luận mới (hỗ trợ reply phân cấp) | Bearer Token |
| `POST` | `/api/discussions/vote` | Upvote/Downvote câu hỏi hoặc câu trả lời | Bearer Token |
| `WS` | `/hubs/discussion` | Kênh SignalR nhận thông báo bình luận tức thời | WebSocket |

---

## 4. CẤU HÌNH BẢO MẬT & KẾT NỐI (CORS & AUTH)

### 4.1. Cấu hình CORS tại Backend (`backend/Program.cs`)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",      // Next.js Local Dev
                "https://yourdomain.com"      // Next.js Production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();             // Cho phép cookie và SignalR auth
    });
});
```

### 4.2. Chiến lược Xác thực
* **Next.js Web (`frontend/`)**: Lưu Access Token trong bộ nhớ/Header kết hợp Refresh Token (HttpOnly Cookie) chống XSS.
* **Mobile App (`mobile/`)**: Tiếp tục dùng `Bearer Token` lưu trong `Preferences` của thiết bị thông qua [`mobile/Services/ApiService.cs`](file:///d:/NET-Tutos/mobile/Services/ApiService.cs).

---

## 5. LỘ TRÌNH THỰC HIỆN TỪNG BƯỚC (ROADMAP)

### 📍 Giai đoạn 1: Chuẩn bị Backend API độc lập (`backend/`)
1. Tạo thư mục và project `backend/` dạng ASP.NET Core Web API trong Solution.
2. Sao chép và tổ chức các thư mục lõi: `Data/`, `Models/`, `Services/`, `Hubs/`.
3. Chuẩn hóa các Controller API (Auth, Tutorials, Playground, AI, Flashcards) trả về JSON chuẩn DTO.
4. Bật Swagger/OpenAPI để kiểm thử tất cả các endpoints bằng UI trực quan.
5. Kiểm tra chạy thử để đảm bảo `mobile/` kết nối mượt mà vào API mới.

### 📍 Giai đoạn 2: Thiết lập dự án Frontend Next.js 15 (`frontend/`)
1. Khởi tạo dự án `frontend/` bằng `create-next-app` với TypeScript, Tailwind CSS, App Router.
2. Cài đặt các thư viện then chốt:
   * `@monaco-editor/react`: Trình biên dịch mã C# tương tác.
   * `react-markdown`, `remark-gfm`, `rehype-katex`: Trình hiển thị bài giảng, code highlight và công thức toán.
   * `@microsoft/signalr`: Kết nối thảo luận thời gian thực.
   * `lucide-react` / `radix-ui`: Thư viện icon và UI component hiện đại.
3. Tạo HTTP Client (Axios / Fetch Wrapper) với baseURL trỏ tới `http://localhost:5000`.

### 📍 Giai đoạn 3: Triển khai các màn hình cốt lõi trên Next.js
1. **Trang chủ & Lộ trình (Home & Roadmap)**: Banner giới thiệu, lộ trình học tương tác.
2. **Chi tiết bài học (Tutorial Reader)**: Đọc bài giảng Markdown, câu hỏi trắc nghiệm đính kèm, nút hoàn thành bài học tích luỹ XP.
3. **C# Playground**: Tích hợp Monaco Editor, chọn template code, gửi mã lên Roslyn Sandbox và nhận kết quả tức thì.
4. **Trợ lý AI Gia Sư**: Giao diện chat trực quan, nút gợi ý chủ đề (Dark/Light mode mượt mà).

### 📍 Giai đoạn 4: Hoàn thiện tính năng phụ trợ & Đóng gói
1. Ôn tập Flashcards với hiệu ứng lật thẻ 3D (Framer Motion).
2. Hệ thống Gamification: Chuỗi ngày học Streak, Bảng xếp hạng Leaderboard, Showcase Portfolio cá nhân.
3. Thảo luận cộng đồng Real-time qua SignalR.
4. Triển khai: Đóng gói Docker hoặc Deploy Backend lên Cloud/VPS và Frontend lên Vercel/Netlify.
