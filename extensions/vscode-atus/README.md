# Atus AI — NET-Tutos .NET 10 Pair Programmer (VS Code Extension)

Trợ lý Pair-Programming cho C# / .NET 10 kết nối trực tiếp tới nền tảng **NET-Tutos** (`Web BFF` hoặc `YARP Gateway`).

## Tính năng chính

- **Sidebar Chat cùng Atus (`atus.chatView`)**: Hỏi đáp C# / ASP.NET Core / EF Core / Kafka kèm ngữ cảnh file C# hiện tại và trích dẫn từ **Multi-Source Hybrid RAG** (`Curriculum`, `Tutorials`, `Challenges`, `Flashcards`).
- **Inline Code Review & Big-O (`atus.reviewCurrentFile`)**: Đánh dấu lỗi Clean Code & hiệu năng ngay trong editor (`DiagnosticCollection`), phân tích Big-O Time/Space, và đề xuất mã `.NET 10` refactor áp dụng 1 chạm (`atus.applyLastRefactor`).
- **Explain Error / Diagnostics (`atus.explainSelectionOrDiagnostics`)**: Lightbulb Quick Fix đọc compiler diagnostics trên dòng code và đề xuất bản sửa lỗi.
- **Generate xUnit Unit Tests (`atus.generateUnitTests`)**: Sinh bộ test AAA `xUnit` sang tab mới cạnh editor hiện tại.
- **Run in NET-Tutos Roslyn Sandbox (`atus.runInSandbox`)**: Chạy nhanh đoạn mã C# ngay trong Roslyn Sandbox của NET-Tutos và xem output trong Output Channel.
- **RAG Knowledge Search (`atus.searchKnowledge`)**: QuickPick tìm kiếm nhanh 175+ tài liệu tri thức .NET trong hệ thống.

## Cài đặt & Chạy thử (Development Host)

```bash
cd extensions/vscode-atus
npm install
npm run compile
npm test
```

Sau đó mở thư mục `extensions/vscode-atus` bằng VS Code và nhấn **F5** để khởi chạy Extension Development Host.

## Cấu hình

| Setting | Mặc định | Mô tả |
|---|---|---|
| `atus.serverUrl` | `http://localhost:5001` | URL của NET-Tutos Web hoặc YARP Gateway (`http://localhost:5000`) |
| `atus.language` | `vi` | Ngôn ngữ phản hồi (`vi` hoặc `en`) |
| `atus.autoReviewOnSave` | `false` | Tự động review C# khi lưu file |
| `atus.requestTimeoutMs` | `25000` | Timeout HTTP cho mỗi request |
