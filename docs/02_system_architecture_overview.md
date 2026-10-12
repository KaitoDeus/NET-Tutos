# 02. KIẾN TRÚC HỆ THỐNG TỔNG THỂ (SYSTEM ARCHITECTURE OVERVIEW)

> **Tài liệu đặc tả kiến trúc kỹ thuật & giải pháp (Technical Solution Architecture)**  
> **Chức danh thực hiện:** Senior Business Analyst / Solution Specialist  
> **Hệ thống:** Nền tảng Học tập & Đào tạo Lập trình .NET Toàn diện (.NET Tutos Platform)  
> **Phiên bản:** 2.0  

---

## 1. Cấu trúc hệ sinh thái tổng thể (System Ecosystem Topology)

Hệ sinh thái **.NET Tutos** được thiết kế theo mô hình kiến trúc phân tầng (Multi-tier N-Layered Architecture) kết hợp kiến trúc dịch vụ kết nối (Service-Oriented Core) để phục vụ đồng thời Web App và Mobile App trên cùng một nền tảng dữ liệu thống nhất:

```mermaid
graph TB
    subgraph ClientLayer ["1. CLIENT PRESENTATION LAYER"]
        WebBrowser["🌐 Web App Client<br/>(Bootstrap 5, Monaco Editor, Vanilla JS, SignalR Client, PWA)"]
        MobileClient["📱 Mobile App Client<br/>(.NET MAUI 10 Multi-targeting: Android / iOS / Windows)"]
    end

    subgraph GatewayAndApi ["2. CONTROLLER & API LAYER (ASP.NET Core 10)"]
        WebControllers["Web MVC Controllers<br/>(Tutorials, Playground, Interview, Leaderboard, Admin)"]
        MobileApiControllers["RESTful Mobile APIs<br/>(/api/mobile/auth, /api/mobile/tutorials)"]
        SignalRHubs["Realtime SignalR Hubs<br/>(DiscussionHub, NotificationHub)"]
    end

    subgraph ServiceCore ["3. DOMAIN APPLICATION SERVICES LAYER"]
        AiService["🧠 AI Tutor & Interview Service<br/>(AiTutorService)"]
        RoslynSandbox["⚡ Roslyn Code Sandbox Engine<br/>(CodeExecutionService)"]
        LmsService["📚 Curriculum & Learning Progress<br/>(CurriculumService, LearningProgressService)"]
        GamificationService["🏆 Gamification Engine<br/>(StreakService, LeaderboardService)"]
        FlashcardServiceCore["🗂️ Flashcard & Spaced Repetition<br/>(FlashcardService)"]
    end

    subgraph DataAccessLayer ["4. DATA ACCESS LAYER & ORM"]
        EFCore["Entity Framework Core 10<br/>(AppDbContext with IdentityDbContext)"]
    end

    subgraph DatabaseLayer ["5. PERSISTENCE STORAGE LAYER"]
        SQLDb[("🗄️ Relational Database<br/>(SQL Server / SQLite)")]
        LocalCache["📦 Client Preferences & Secure Storage<br/>(Mobile & Browser LocalStorage)"]
    end

    WebBrowser -->|HTTPS / HTML / WSS| WebControllers
    WebBrowser -->|WSS / SignalR| SignalRHubs
    MobileClient -->|HTTPS REST JSON / Bearer JWT| MobileApiControllers
    MobileClient -.->|Offline Sync| LocalCache

    WebControllers --> ServiceCore
    MobileApiControllers --> ServiceCore
    SignalRHubs --> ServiceCore

    ServiceCore --> EFCore
    EFCore --> SQLDb
```

---

## 2. Công nghệ cốt lõi & Tiêu chuẩn kỹ thuật (Tech Stack Specifications)

| Thành phần | Công nghệ / Framework | Phiên bản | Vai trò & Mục đích sử dụng |
| :--- | :--- | :--- | :--- |
| **Backend Framework** | ASP.NET Core MVC | .NET 10 | Máy chủ ứng dụng trung tâm, xử lý rendering giao diện Web và cung cấp REST API cho Mobile. |
| **Mobile Framework** | .NET MAUI (Multi-platform App UI) | .NET 10 | Ứng dụng di động đa nền tảng (Android, iOS, Windows) viết bằng C# và XAML. |
| **Data Access / ORM** | Entity Framework Core | 10.0 | Thao tác cơ sở dữ liệu hướng đối tượng, quản lý Migration, tối ưu hóa truy vấn qua `IQueryable` và `AsNoTracking`. |
| **Cơ sở dữ liệu** | Microsoft SQL Server / SQLite | 2022 / 3.x | Lưu trữ toàn bộ dữ liệu người dùng, bài học, quiz, tiến độ, coding challenge, điểm số và bài nộp. |
| **Trình soạn thảo Code** | Microsoft Monaco Editor | 0.45.0 | Trình soạn thảo mã nguồn trực tuyến trong Web (tương đương lõi VS Code), hỗ trợ C# syntax, phím tắt `Ctrl+I`, formatting. |
| **Biên dịch & Sandbox** | Roslyn Scripting API (`Microsoft.CodeAnalysis.CSharp.Scripting`) | 4.12+ | Trình biên dịch mã nguồn C# động trên bộ nhớ RAM, bắt lỗi compiler và thực thi sandbox có giới hạn an toàn. |
| **Giao tiếp Realtime** | ASP.NET Core SignalR | .NET 10 | Truyền thông thời gian thực hai chiều cho bảng tin hoạt động (Activity Feed) và thảo luận trực tiếp (Discussion Hub). |
| **Xác thực & Phân quyền** | ASP.NET Core Identity & JWT Bearer | .NET 10 | Quản lý người dùng, mật khẩu mã hóa, phân quyền Role (Learner, Admin), cấp phát JWT token an toàn cho Mobile. |
| **Giao diện Web** | Bootstrap 5, Bootstrap Icons, CSS Modern Variables | 5.3.3 | Giao diện chuẩn Responsive hiện đại, hỗ trợ Dark / Light theme toàn diện. |
| **Nhận diện giọng nói** | Web Speech API (`SpeechRecognition`) | W3C Standard | Chuyển giọng nói tiếng Việt và tiếng Anh thành văn bản trong phòng phỏng vấn kỹ thuật giả lập (AI Mock Interview). |

---

## 3. Kiến trúc luồng dữ liệu (Data Flow Diagrams)

### Luồng 1: Biên dịch và chấm điểm bài tập (Playground & Challenge Judge Engine)

```mermaid
sequenceDiagram
    autonumber
    actor HọcViên as Học viên
    participant Editor as Monaco Editor (Browser)
    participant Controller as PlaygroundController
    participant Sandbox as CodeExecutionService
    participant Roslyn as Roslyn Scripting Engine
    participant AI as AiTutorService
    participant DB as AppDbContext

    HọcViên->>Editor: Viết mã nguồn C# và nhấn "Nộp bài (Submit)"
    Editor->>Controller: POST /Playground/SubmitChallenge { challengeId, code }
    Controller->>DB: Lấy danh sách CodeTestCase (Input, ExpectedOutput)
    Controller->>Sandbox: ExecuteWithTestCasesAsync(code, testCases)
    
    alt Biên dịch thất bại (Compilation Error)
        Sandbox-->>Controller: CompileError (CS0103, CS1061, ...)
        Controller-->>Editor: Trả về mã lỗi biên dịch
        Editor->>HọcViên: Hiển thị banner đỏ kèm nút "Giải thích lỗi với AI"
        HọcViên->>AI: Yêu cầu AI chẩn đoán nguyên nhân & cách khắc phục
        AI-->>Editor: Phân tích lỗi, chỉ ra dòng code sai, hướng dẫn fix
    else Biên dịch thành công (Runtime Evaluation)
        loop Duyệt qua từng Test Case
            Sandbox->>Roslyn: Nạp tham số và chạy hàm Solution
            Roslyn-->>Sandbox: Trả về kết quả ActualOutput
            Sandbox->>Sandbox: So khớp ActualOutput vs ExpectedOutput
        end
        alt Vượt qua 100% Test Cases
            Sandbox-->>Controller: AllPassed = True, ExecutionTimeMs
            Controller->>DB: Cộng XP, đánh dấu Challenge hoàn thành
            Controller-->>Editor: Thông báo Chấp thuận (Accepted) + Thưởng XP
        else Rớt một số Test Case
            Sandbox-->>Controller: AllPassed = False, Chi tiết test fail
            Controller-->>Editor: Hiển thị bảng kết quả + Nút "AI Chẩn đoán" trên từng dòng lỗi
            HọcViên->>AI: Bấm "AI Chẩn đoán" cho Test Case #x
            AI-->>Editor: Trả về phân loại lỗi (Off-by-one, Edge Case, ...) kèm Code Fix
        end
    end
```

### Luồng 2: Phỏng vấn kỹ thuật giả lập tương tác với AI (AI Mock Technical Interview)

```mermaid
sequenceDiagram
    autonumber
    actor ỨngViên as Ứng viên
    participant WebUI as Giao diện Phỏng Vấn (Mock.cshtml)
    participant Speech as Web Speech API (Microphone)
    participant InterviewCtrl as InterviewController
    participant AiTutor as AiTutorService
    participant DB as AppDbContext & Identity

    ỨngViên->>WebUI: Bấm "Bắt đầu Phỏng vấn thử với AI"
    WebUI->>InterviewCtrl: GET /Interview/Mock?count=5
    InterviewCtrl->>DB: Truy vấn ngẫu nhiên 5 câu hỏi Flashcard theo trình độ
    InterviewCtrl-->>WebUI: Hiển thị phòng phỏng vấn, khởi động đồng hồ đếm ngược
    
    ỨngViên->>Speech: Bấm "Nói câu trả lời" và trình bày qua micro
    Speech-->>WebUI: Chuyển đổi giọng nói thành văn bản vào TextArea
    ỨngViên->>WebUI: Nhấn "Gửi cho AI Tech Lead chấm điểm"
    
    WebUI->>InterviewCtrl: POST /Interview/EvaluateAiAnswer { flashcardId, candidateAnswer }
    InterviewCtrl->>AiTutor: EvaluateInterviewAnswerAsync(request)
    AiTutor->>AiTutor: Phân tích 3 tiêu chí: Kỹ thuật, Mạch lạc, Chiều sâu thực chiến
    AiTutor->>AiTutor: Trích xuất Điểm sáng, Ý còn thiếu, Câu hỏi xoáy, Đáp án mẫu
    AiTutor-->>InterviewCtrl: AiInterviewEvaluationResponse
    InterviewCtrl-->>WebUI: Trả về đánh giá chi tiết
    WebUI->>ỨngViên: Hiển thị 3 thước đo %, card điểm mạnh/yếu & câu hỏi xoáy
    
    opt Trả lời câu hỏi xoáy (Follow-up Challenge)
        ỨngViên->>WebUI: Nhập phản hồi câu hỏi xoáy
        WebUI->>InterviewCtrl: POST /Interview/AnswerAiFollowUp
        InterviewCtrl->>AiTutor: AnswerFollowUpAsync
        AiTutor-->>WebUI: Phản hồi của Tech Lead + Điểm thưởng Bonus XP
    end
    
    ỨngViên->>WebUI: Nhấn "Hoàn thành & Xuất Báo Cáo"
    WebUI->>InterviewCtrl: POST /Interview/SubmitAiMockReport
    InterviewCtrl->>AiTutor: GenerateInterviewReportAsync(items)
    AiTutor->>DB: Lọc bài học Tutorials đề xuất theo lỗ hổng kiến thức
    AiTutor-->>InterviewCtrl: Báo cáo năng lực 5 kỹ năng cốt lõi
    InterviewCtrl->>DB: Cộng XP vào tài khoản, cập nhật chuỗi Streak
    InterviewCtrl-->>WebUI: Hiển thị Báo cáo sát hạch toàn diện
```

### Luồng 3: Đồng bộ dữ liệu giữa Mobile App và Web App

```mermaid
sequenceDiagram
    autonumber
    actor HọcViên as Học viên
    participant Mobile as Mobile App (.NET MAUI)
    participant MobileApi as MobileAuthController / MobileTutorialsController
    participant DB as Database chung (AppDbContext)
    participant Web as Web App (ASP.NET Core)

    HọcViên->>Mobile: Đăng nhập bằng Email/Password đã tạo trên Web
    Mobile->>MobileApi: POST /api/mobile/auth/login
    MobileApi->>DB: Kiểm tra tài khoản trong bảng AspNetUsers
    MobileApi-->>Mobile: Trả về JWT Bearer Token, Thông tin User (XP, Streak, Level)
    Mobile->>Mobile: Lưu token vào Preferences an toàn
    
    HọcViên->>Mobile: Đọc bài học và nhấn "Hoàn thành bài học"
    Mobile->>MobileApi: POST /api/mobile/tutorials/complete { tutorialId } (Bearer Token)
    MobileApi->>DB: Lưu UserLessonProgress (IsCompleted = true)
    MobileApi->>DB: Cộng +15 XP vào AspNetUsers
    MobileApi->>DB: Cập nhật DailyCheckIn & Streak
    MobileApi-->>Mobile: Trả về kết quả thành công + XP mới
    
    HọcViên->>Web: Đăng nhập hoặc mở lại Web App trên trình duyệt
    Web->>DB: Truy vấn thông tin người dùng & bài học
    DB-->>Web: Trả về tổng XP mới, chuỗi Streak mới, bài học đã tích xanh
    Web->>HọcViên: Dữ liệu hiển thị đồng bộ 100% với phiên học trên điện thoại
```

---

## 4. Chiến lược bảo mật & kiểm soát rủi ro (Security & Isolation Strategy)

1. **Cô lập môi trường chạy mã (Roslyn Sandbox Execution Isolation):**
   - Không cho phép gọi các thư viện nguy hiểm (`System.IO`, `System.Diagnostics.Process`, `System.Reflection`, `System.Net.Sockets`).
   - Giới hạn thời gian thực thi (Execution Timeout $\le 3000\text{ms}$) để triệt tiêu các vòng lặp vô hạn hoặc đệ quy tràn bộ nhớ StackOverflow.
   - Bắt gọn `Console.SetOut()` vào luồng `StringWriter` để ngăn chặn rò rỉ dữ liệu hệ thống.
2. **Bảo mật xác thực & phân quyền (Authentication Security):**
   - Web App áp dụng Cookie Authentication kết hợp Token bảo vệ chống tấn công giả mạo yêu cầu chéo trang (CSRF Protection via AntiForgeryToken).
   - Mobile API áp dụng chuẩn JWT Bearer Token với thuật toán ký bí mật HMAC-SHA256, thời hạn hợp lệ rõ ràng.
3. **Bảo vệ toàn vẹn dữ liệu (Data Protection):**
   - Mã hóa mật khẩu chuẩn PBKDF2 thông qua ASP.NET Core Identity.
   - Chống tấn công SQL Injection bằng cách sử dụng 100% truy vấn tham số hóa (Parameterized Queries) trong Entity Framework Core.
   - Xử lý làm sạch nội dung hiển thị (XSS Sanitization) trên toàn bộ bài viết Markdown và mã HTML đầu ra.
