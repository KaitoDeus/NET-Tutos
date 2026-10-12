# 03. TÀI LIỆU ĐẶC TẢ YÊU CẦU CHỨC NĂNG (BUSINESS REQUIREMENTS DOCUMENT - BRD)

> **Tài liệu đặc tả yêu cầu nghiệp vụ chi tiết (Functional Requirements Specification)**  
> **Chức danh thực hiện:** Senior Business Analyst  
> **Hệ thống:** Nền tảng Học tập & Đào tạo Lập trình .NET Toàn diện (.NET Tutos Platform)  
> **Mã tài liệu:** BRD-NET-TUTOS-2026-V2  

---

## 1. Ma trận phân loại yêu cầu (Requirement Classification Matrix)

Hệ thống được chuẩn hóa thành 7 phân hệ chức năng chính:

| Mã phân hệ | Tên phân hệ nghiệp vụ | Phạm vi nền tảng | Mức độ ưu tiên |
| :--- | :--- | :--- | :--- |
| **MOD-LMS** | Quản trị Giáo trình & Bài học LMS | Web App & Mobile App | P0 (Must Have) |
| **MOD-PLAY** | Trình Thực hành Code Trực tuyến (Monaco Playground) | Web App | P0 (Must Have) |
| **MOD-AI** | Trợ lý AI & Lập trình Tương tác (AI Mentor) | Web App | P0 (Must Have) |
| **MOD-INT** | Phòng Phỏng vấn Kỹ thuật Giả lập AI (AI Mock Interview) | Web App | P1 (High) |
| **MOD-GAM** | Hệ thống Động lực Gamification, Streak & Xếp hạng | Web App & Mobile App | P1 (High) |
| **MOD-MOB** | Ứng dụng Di động Client (.NET MAUI) | Mobile (Android/iOS) | P0 (Must Have) |
| **MOD-ADM** | Bảng điều khiển Quản trị LMS Admin | Web App | P1 (High) |

---

## 2. Chi tiết yêu cầu chức năng (Detailed Functional Requirements)

### Phân hệ 1: Quản trị Giáo trình & Bài học LMS (MOD-LMS)

#### `REQ-LMS-01`: Cấu trúc giáo trình 7 Module chuẩn Microsoft .NET
- **Mô tả:** Hệ thống cung cấp cấu trúc 101 bài học chuẩn hóa chia làm 7 module tuần tự:
  1. *C# Fundamentals & Object-Oriented Programming*
  2. *Advanced C# & Memory Management*
  3. *LINQ & Collections Deep Dive*
  4. *Asynchronous Programming (async/await, Task, Threading)*
  5. *Entity Framework Core & Database Performance*
  6. *ASP.NET Core Web API & Modern Architecture*
  7. *Clean Architecture, Microservices & Cloud Patterns*
- **User Story:** Là một học viên, tôi muốn xem lộ trình từ Zero đến Senior có thứ tự logic để tôi biết rõ mình đang ở đâu và cần học gì tiếp theo.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **Given** người dùng truy cập trang Giáo trình (`/curriculum` hoặc `/roadmap`).
  - **When** người dùng xem từng module.
  - **Then** hệ thống hiển thị danh sách bài học, thời gian đọc ước tính, cấp độ khó (Beginner, Intermediate, Advanced) và trạng thái hoàn thành (Đã học / Chưa học).

#### `REQ-LMS-02`: Hiển thị nội dung bài học Markdown & Code Highlighting
- **Mô tả:** Nội dung bài học được biên soạn dưới dạng Markdown giàu tính năng (Gutenberg/GitHub style), hỗ trợ syntax highlighting cho C#, ghi chú chú giải (Alert callouts), bảng biểu và sơ đồ.
- **Tiêu chí chấp nhận:**
  - Tự động đánh chỉ mục mục lục (Table of Contents) theo các thẻ `H2`, `H3`.
  - Có nút "Lưu ngoại tuyến" (Save Offline) để đọc khi mất mạng qua Service Worker PWA.
  - Có thanh điều hướng bài trước / bài sau (Next / Previous Navigation).

#### `REQ-LMS-03`: Hệ thống Quiz trắc nghiệm đánh giá kiến thức
- **Mô tả:** Mỗi bài học có bộ câu hỏi trắc nghiệm tự đánh giá. Khi hoàn thành đúng trên 80%, bài học được tính điểm kinh nghiệm thưởng.

---

### Phân hệ 2: Trình Thực hành Code Trực tuyến (MOD-PLAY)

#### `REQ-PLAY-01`: Trình soạn thảo Monaco Editor chuyên nghiệp
- **Mô tả:** Tích hợp bộ soạn thảo Monaco Editor phiên bản 0.45.0 trên trình duyệt Web.
- **Tiêu chí chấp nhận:**
  - Hỗ trợ Auto-completion, Syntax Highlighting cho C# 14.
  - Hỗ trợ phím tắt chuẩn: `Ctrl + Enter` (Chạy mã), `Ctrl + S` (Tự động format code), `Ctrl + I` (Hỏi trợ lý AI).
  - Tùy biến linh hoạt trong Menu Cài đặt: Đổi phông chữ (`Fira Code`, `JetBrains Mono`, `Cascadia Code`), cỡ chữ (12px - 20px), thụt đầu dòng (2/4 spaces), Ligatures (`=>`, `!=`, `==`), Minimap, Word Wrap.
  - Đồng bộ màu sắc Dark/Light Theme tự động theo hệ thống hoặc tùy chọn thủ công (`vs-dark`, `vs`, `hc-black`).

#### `REQ-PLAY-02`: Roslyn Scripting Execution Sandbox
- **Mô tả:** Biên dịch và thực thi script C# trong môi trường sandbox an toàn trên máy chủ.
- **Tiêu chí chấp nhận:**
  - Bắt toàn bộ luồng xuất `Console.WriteLine()` và hiển thị lên cửa sổ Terminal Console giả lập.
  - Đo đạc chính xác thời gian thực thi (Execution Time tính bằng millisecond `ms`).
  - Nếu gặp lỗi biên dịch (Compile Error), trích xuất mã lỗi (`CSxxxx`), vị trí dòng bị lỗi và thông điệp lỗi trực quan.
  - Tự động ngắt (Timeout) nếu mã lệnh chạy quá 3000ms nhằm phòng chống vòng lặp vô tận.

#### `REQ-PLAY-03`: Coding Challenges & Judge Engine tự động
- **Mô tả:** Hệ thống thử thách lập trình thuật toán và tình huống thực tế.
- **Tiêu chí chấp nhận:**
  - Học viên viết mã hàm `Solution` và bấm "Nộp bài (Submit Solution)".
  - Judge Engine chạy mã qua danh sách `CodeTestCase` (cả test công khai và test ẩn).
  - Nếu đúng 100%: Ghi nhận trạng thái `Accepted`, hiển thị tổng thời gian thực thi, cộng điểm XP thưởng và cập nhật tiến độ.
  - Nếu sai: Hiển thị bảng chi tiết các test case bị lỗi (Input, Actual Output, Expected Output) kèm nút **"AI Chẩn đoán"** trên từng dòng fail.

---

### Phân hệ 3: Trợ lý AI & Lập trình Tương tác (MOD-AI)

#### `REQ-AI-01`: Đo lường & Phân tích độ phức tạp thuật toán Big-O (`AnalyzeComplexity`)
- **Mô tả:** Phân tích thuật toán C# trong editor để xác định độ phức tạp Big-O thời gian (Time Complexity: $O(1), O(\log N), O(N), O(N \log N), O(N^2), O(2^N)$) và không gian (Space Complexity: $O(1), O(N)$).
- **Tiêu chí chấp nhận:**
  - Hiển thị huy hiệu màu sắc trực quan (Xanh: $O(1), O(N)$; Đỏ: $O(N^2), O(2^N)$).
  - Thang điểm hiệu năng (Efficiency Score từ 1 đến 100).
  - Chỉ rõ các điểm nghẽn (Bottlenecks) như vòng lặp lồng nhau, cộng chuỗi không dùng `StringBuilder`.
  - Đưa ra đề xuất cải tiến tối ưu (Optimizations) cụ thể.

#### `REQ-AI-02`: Chẩn đoán nguyên nhân rớt Test Case (`DiagnoseTestFailure`)
- **Mô tả:** Khi nộp bài Coding Challenge mà bị kết quả sai (Wrong Answer), người học bấm nút **"AI Chẩn đoán"** ngay tại hàng test case bị sai.
- **Tiêu chí chấp nhận:**
  - Phân loại chính xác nguyên nhân gốc rễ (Root Cause Category):
    - *Edge Case / Null Handling*
    - *Off-by-one Indexing*
    - *Case Sensitivity*
    - *Duplicate Element / Index Reuse*
    - *Logic Flaw / State Mutation*
  - Giải thích rõ tại sao lại ra kết quả thực tế và hướng dẫn cách khắc phục.
  - Cung cấp đoạn mã sửa mẫu kèm 2 nút 1-click: **"Sao chép (Copy)"** và **"Áp dụng vào Editor (Apply to Editor)"**.

#### `REQ-AI-03`: Tự động sinh bộ kiểm thử xUnit (`GenerateUnitTest`)
- **Mô tả:** Tự động tạo bộ kiểm thử đơn vị chuẩn xUnit cho đoạn mã đang viết trong editor.
- **Tiêu chí chấp nhận:**
  - Tuân thủ nghiêm ngặt mô hình AAA (Arrange – Act – Assert).
  - Sinh cả kiểm thử trường hợp chuẩn (`[Fact]`) và kiểm thử tham số hóa trường hợp biên (`[Theory]`, `[InlineData]`).
  - Hỗ trợ nút sao chép và nút chèn trực tiếp vào trình soạn thảo.

#### `REQ-AI-04`: Phím tắt `Ctrl+I` & Context Menu chuột phải trong Monaco
- **Mô tả:** Cho phép bôi đen bất kỳ đoạn mã nào trong Monaco Editor và nhấn `Ctrl+I` hoặc click chuột phải chọn:
  - 🤖 *AI Mentor: Giải thích đoạn mã đã chọn*
  - ⚡ *AI Mentor: Đo độ phức tạp Big-O*
  - 🧪 *AI Mentor: Tạo bộ kiểm thử xUnit*

---

### Phân hệ 4: Phòng Phỏng vấn Kỹ thuật Giả lập AI (MOD-INT)

#### `REQ-INT-01`: Đóng vai AI Tech Lead sát hạch trực tiếp
- **Mô tả:** Mô phỏng phòng phỏng vấn trực tiếp với AI đóng vai Lead Architect / Senior Interviewer.
- **Tiêu chí chấp nhận:**
  - Tạo bộ đề 5 câu hỏi tình huống ngẫu nhiên theo trình độ (Junior, Middle, Senior) hoặc chủ đề chuyên sâu (C# Internals, Async, EF Core, ASP.NET Core, Architecture).
  - Có đồng hồ đếm ngược thông minh (10 phút mặc định).
  - Có thanh Stepper tròn số (1 đến 5) giúp người học nhảy nhanh hoặc theo dõi câu hỏi đang làm.

#### `REQ-INT-02`: Chấm điểm câu trả lời thực tế theo 3 tiêu chuẩn tuyển dụng
- **Mô tả:** AI phân tích câu trả lời văn bản thực tế của ứng viên và chấm điểm 3 tiêu chí:
  1. **Technical Accuracy (Độ chính xác kỹ thuật - 0 đến 100):** Khái niệm, cơ chế CLR, bộ nhớ.
  2. **Clarity & Structure (Diễn đạt & Cấu trúc - 0 đến 100):** Mạch lạc, đi thẳng vào trọng tâm.
  3. **Practical Depth (Độ sâu thực chiến - 0 đến 100):** Nhận diện các bẫy thực tế (deadlock, N+1, unmanaged resource).
- **Tiêu chí chấp nhận:**
  - Phân loại trình độ: `Senior Level`, `Strong Mid`, `Junior Level`, `Needs Revision`.
  - Trích xuất danh sách **Điểm sáng (Key Strengths)** trong câu trả lời.
  - Trích xuất danh sách **Ý còn thiếu (Missing Points)** cần bổ sung để đạt chuẩn Senior.
  - Cung cấp **Câu trả lời tham chiếu chuẩn Senior (Model Benchmark Answer)**.

#### `REQ-INT-03`: Thử thách câu hỏi xoáy (Follow-Up Challenge) tương tác
- **Mô tả:** AI Tech Lead tự động đặt ra một câu hỏi xoáy đào sâu tình huống thực tế cho từng câu hỏi.
- **Tiêu chí chấp nhận:**
  - Ứng viên có thể nhập câu trả lời phản hồi cho câu hỏi xoáy ngay tại chỗ.
  - AI phản hồi đánh giá và cộng điểm thưởng **Bonus XP** vào kết quả.

#### `REQ-INT-04`: Nhận diện giọng nói Web Speech API (Speech-to-Text)
- **Mô tả:** Tích hợp nút Micro **"Nói câu trả lời"** bên cạnh ô nhập văn bản.
- **Tiêu chí chấp nhận:**
  - Hỗ trợ cả tiếng Việt (`vi-VN`) và tiếng Anh (`en-US`) tự động theo ngôn ngữ trang.
  - Hiển thị hiệu ứng trạng thái đang ghi âm; chữ nói ra được tự động điền vào khung trả lời.

#### `REQ-INT-05`: Báo cáo sát hạch toàn diện & Đề xuất lộ trình học tập
- **Mô tả:** Sau khi hoàn thành buổi phỏng vấn, hệ thống xuất báo cáo năng lực chi tiết.
- **Tiêu chí chấp nhận:**
  - Xếp loại năng lực tổng thể: `Senior Ready`, `Middle Ready`, `Junior Ready`.
  - Bảng đánh giá 5 kỹ năng cốt lõi .NET dưới dạng progress bars.
  - Nhận xét tổng quan của Hội đồng phỏng vấn (Executive Assessment).
  - Đề xuất danh sách các bài học (`Tutorials`) cần ôn tập kèm đường link truy cập trực tiếp.
  - Tự động cộng điểm kinh nghiệm XP thưởng và ghi nhận chuỗi ngày học Streak.

---

### Phân hệ 5: Gamification, Streak & Cộng đồng (MOD-GAM)

#### `REQ-GAM-01`: Chuỗi học tập liên tục (Daily Learning Streak)
- **Mô tả:** Ghi nhận chuỗi ngày học liên tiếp khi học viên có ít nhất 1 hoạt động (đọc bài, nộp code challenge, làm quiz, phỏng vấn thử).
- **Quy tắc nghiệp vụ:** Nếu quá 24h từ ngày hôm trước mà không có hoạt động, chuỗi Streak reset về 1.

#### `REQ-GAM-02`: Hệ thống Điểm kinh nghiệm (XP) & Cấp bậc (Level)
- **Quy tắc tính điểm XP:**
  - Đọc xong bài học: `+15 XP`
  - Hoàn thành Quiz: `+20 XP`
  - Giải đúng Coding Challenge: `+30 XP`
  - Hoàn thành phiên Mock Interview: `+50 XP` đến `+90 XP`
  - Điểm danh hàng ngày: `+10 XP`
- **Công thức tính cấp độ:** $\text{Level} = \lfloor \frac{\text{XP}}{100} \rfloor + 1$.

#### `REQ-GAM-03`: Bảng vinh danh (Hall of Fame / Leaderboard)
- **Mô tả:** Bảng xếp hạng học viên xuất sắc theo Tuần, Tháng và Toàn thời gian dựa trên tổng số XP và chuỗi Streak.

#### `REQ-GAM-04`: Diễn đàn thảo luận thời gian thực (SignalR Discussion)
- **Mô tả:** Cho phép học viên đặt câu hỏi dưới từng bài học, bình luận, upvote câu trả lời hay và nhận thông báo realtime qua SignalR.

---

### Phân hệ 6: Khách hàng Di động .NET MAUI Client (MOD-MOB)

#### `REQ-MOB-01`: Đồng bộ tài khoản và đăng nhập thống nhất
- **Mô tả:** Người dùng sử dụng cùng tài khoản Web để đăng nhập vào Mobile App qua API `/api/mobile/auth/login`.
- **Tiêu chí chấp nhận:** Token JWT được lưu trữ an toàn trong `Preferences`; thông tin họ tên, avatar, XP, level và streak được đồng bộ tự động.

#### `REQ-MOB-02`: Đọc bài học & Đánh dấu hoàn thành
- **Mô tả:** Hiển thị danh mục giáo trình, danh sách bài học và chi tiết nội dung trên màn hình điện thoại mượt mà.
- **Tiêu chí chấp nhận:** Bấm nút "Hoàn thành bài học" trên Mobile sẽ gọi API ghi nhận vào Database chung, tự động cộng XP và cập nhật tiến độ trên Web.

#### `REQ-MOB-03`: Động cơ giao diện đa chế độ (Theme Engine)
- **Mô tả:** Hỗ trợ 3 chế độ hiển thị: Tự động theo hệ điều hành điện thoại, Chế độ Sáng (Light) và Chế độ Tối (Dark).

#### `REQ-MOB-04`: Cấu hình địa chỉ máy chủ API linh hoạt
- **Mô tả:** Ngay tại màn hình Đăng nhập và Cài đặt, có ô cấu hình Server URL giúp học viên hoặc lập trình viên dễ dàng chuyển đổi IP máy chủ (ví dụ `10.0.2.2:5000` trên Emulator hoặc `192.168.1.x:5000` trên thiết bị thật).
