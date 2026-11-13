# 99. Project Freeze & Resumption Playbook (Cẩm Nang Đóng Băng & Tái Khởi Động)

> **Trạng thái:** ✅ **DỰ ÁN ĐÃ ĐƯỢC ĐÓNG BĂNG AN TOÀN (PHASE 28 COMPLETED)**  
> **Phiên bản:** `.NET 10.0` / `C# 14` / `ASP.NET Core 10` / `.NET MAUI 10`  
> **Kiến trúc:** 28 Projects trong `NET-Tutos.slnx` + TypeScript VS Code Extension + 13 Container Docker / 16 Pods K8s  
> **Kiểm thử tự động:** **254/254 Tests Passing (100% Quality Gate)**  
> **Thời điểm niêm phong:** Tháng 11/2026 (LTS Freeze Release)

---

## 1. Tổng Quan & Mục Đích Tài Liệu

Tài liệu này là **Single Source of Truth (SSOT)** được thiết kế để bạn hoặc bất kỳ kỹ sư nào có thể **tái khởi động (resume) dự án sau 6 tháng, 1 năm hoặc nhiều năm** mà không bị mất ngữ cảnh, không mất dữ liệu, và không cần phải dò lại 28 giai đoạn phát triển trước đó.

Toàn bộ hệ thống hiện đã ở trạng thái **Turnkey / Cold Storage**:
- Mã nguồn sạch 100%, không lỗi build, không secret rò rỉ.
- Toàn bộ 254 bài test tự động (8 project test .NET + 1 suite VS Code) đều đạt kết quả xanh tuyệt đối (`0 Failed, 0 Skipped`).
- Đã xuất bản sẵn các file thành phẩm cài đặt (`.apk`, `.vsix`, `.tgz`) tại thư mục `dist/`.
- Đã lưu giữ trọn vẹn bản snapshot cơ sở dữ liệu tại `data/snapshots/`.
- Đã có sẵn CLI quản trị trung tâm một lệnh: `scripts/nettutos.ps1`.

---

## 2. Hướng Dẫn Tái Khởi Động Trong 3 Phút (3-Minute Resumption Quickstart)

Khi bạn quay lại dự án sau thời gian tạm dừng, chỉ cần mở PowerShell tại thư mục gốc `d:\NET-Tutos` và làm theo các bước sau:

### Bước 1: Kiểm tra tổng thể hiện trạng
```powershell
./scripts/nettutos.ps1 status
```
*Lệnh sẽ tự động kiểm tra cổng mạng, Docker, Kubernetes, máy ảo Android, và các file đóng gói.*

### Bước 2: Chọn phương thức khởi động mong muốn
- **Cách A: Chạy nhanh cục bộ (Web BFF + SQLite có sẵn toàn bộ dữ liệu):**
  ```powershell
  ./scripts/nettutos.ps1 up local
  ```
  *(Hệ thống sẽ bật Web BFF trên cổng `5262`, tự động mở trình duyệt tới `http://localhost:5262`).*

- **Cách B: Chạy đầy đủ cụm Microservices & Docker Compose:**
  ```powershell
  ./scripts/nettutos.ps1 up docker
  ```
  *(Khởi chạy 13 containers: Postgres 17 `pgvector`, Redis 7, Kafka KRaft, RabbitMQ, Prometheus, Grafana, Tempo, Gateway, 4 Microservices).*

- **Cách C: Chạy trên cụm Kubernetes nội bộ (Helm):**
  ```powershell
  ./scripts/nettutos.ps1 up k8s
  ```
  *(Triển khai 16 pods qua production chart `deploy/helm/nettutos`).*

- **Cách D: Khởi động App Mobile (.NET MAUI 10 trên Android Emulator):**
  ```powershell
  ./scripts/nettutos.ps1 mobile
  ```
  *(Tự động bật máy ảo `NetTutos_API34`, bật Web BFF và nạp app lên điện thoại).*

### Bước 3: Chạy lại toàn bộ bộ kiểm thử tự động
```powershell
./scripts/nettutos.ps1 test
```
*(Xác nhận lại toàn bộ 254 tests đều vượt qua).*

---

## 3. Bản Đồ Danh Mục 28 Dự Án trong Solution (`NET-Tutos.slnx`)

```
NET-Tutos.slnx (28 Projects)
│
├── 01. BuildingBlocks (Nền tảng hạ tầng dùng chung)
│   ├── NET-Tutos.BuildingBlocks.Common              (Result, Error, Guard, BaseEntity, DomainEvents)
│   ├── NET-Tutos.BuildingBlocks.OpenTelemetry       (OTLP Metrics, Traces, Tempo Exporter)
│   ├── NET-Tutos.BuildingBlocks.PrometheusMetrics   (Prometheus Custom Metrics Builder)
│   ├── NET-Tutos.BuildingBlocks.SandboxGuardrails   (Roslyn Scripting Resource Throttling)
│   └── NET-Tutos.BuildingBlocks.EventBus.Kafka      (Kafka KRaft Producer/Consumer & Analytics)
│
├── 02. Domain & Application (Nghiệp vụ cốt lõi)
│   ├── NET-Tutos.Domain                            (Entities, Value Objects, Aggregate Roots)
│   ├── NET-Tutos.Application                       (Use Cases, CQRS Interfaces, DTOs, Enums)
│   ├── NET-Tutos.CodeRunner                        (Roslyn Compiler, Memory & Execution Sandbox)
│   ├── NET-Tutos.Gamification                      (Streak, ELO Arena, Levels, Badges Engine)
│   ├── NET-Tutos.AiTutor                           (Gemini AI Agent, Big-O Analysis, Rubric Voice)
│   └── NET-Tutos.Catalog                           (Curriculum, Flashcards, Tutorials, Categories)
│
├── 03. Infrastructure (Truy xuất dữ liệu, Caching & Microservice APIs)
│   ├── NET-Tutos.Infrastructure                    (EF Core Npgsql, Redis L1/L2, pgvector RAG, SQLite)
│   ├── NET-Tutos.CodeRunner.API                    (Microservice chạy độc lập trên Port 5002)
│   ├── NET-Tutos.Gamification.API                  (Microservice chạy độc lập trên Port 5004)
│   ├── NET-Tutos.AiTutor.API                       (Microservice chạy độc lập trên Port 5006)
│   ├── NET-Tutos.Catalog.API                       (Microservice chạy độc lập trên Port 5008)
│   └── NET-Tutos.YarpGateway                       (Reverse Proxy, Distributed Redis Rate Limit Port 5000)
│
├── 04. Orchestration (Điều phối cụm)
│   └── NET-Tutos.AppHost                           (.NET Aspire 13.6.5 Distributed Application Host)
│
├── 05. Presentation (Giao diện người dùng)
│   ├── Web                                         (ASP.NET Core 10 MVC, BFF, SignalR Arena, Playground)
│   ├── NET-Tutos.Mobile.Core                       (Shared pure net10.0: Leitner engine, Offline store, SSE parser)
│   └── Mobile                                      (.NET MAUI 10 Cross-Platform Android/iOS Native Client)
│
├── 06. Tests (Kiểm thử chất lượng - 249 .NET Tests)
│   ├── NET-Tutos.Gateway.Tests                     (11 tests: YARP proxy routing, rate limit partitions)
│   ├── NET-Tutos.CodeRunner.Tests                  (13 tests: Roslyn compilation, memory guardrails)
│   ├── NET-Tutos.Gamification.Tests                (16 tests: Streak formula, level curves, badges)
│   ├── NET-Tutos.AiTutor.Tests                     (16 tests: Big-O analysis, prompt evaluation)
│   ├── NET-Tutos.Catalog.Tests                     (17 tests: Curriculum queries, category sorting)
│   ├── NET-Tutos.Mobile.Tests                      (12 tests: Leitner sync, offline queue, SSE stream parser)
│   ├── NET-Tutos.UnitTests                         (123 tests: Web BFF, Redis cache, Geo-IP, SEO, Arena)
│   └── NET-Tutos.IntegrationTests                  (41 tests: Full HTTP pipeline, /metrics, /api/mobile/*)
│
└── 07. External IDE Tooling (Công cụ ngoài)
    └── extensions/vscode-atus                      (TypeScript VS Code Extension, 5 node:test tests)
```

---

## 4. Ma Trận Cổng Mạng, Mật Khẩu & Môi Trường

| Dịch vụ / Container | Cổng Host (Port) | Giao thức / Endpoint | Ghi chú & Mật khẩu mặc định |
| :--- | :--- | :--- | :--- |
| **YARP Gateway** | `:5000` | HTTP (`/api/*`, `/healthz`) | Reverse Proxy trung tâm |
| **Web MVC / BFF** | `:5262` / `:5000` | HTTP (`/`, `/api/mobile/*`) | Giao diện Web & Mobile Backend |
| **CodeRunner API** | `:5002` | HTTP (`/api/coderunner/*`) | Roslyn Sandbox Microservice |
| **Gamification API** | `:5004` | HTTP (`/api/gamification/*`)| ELO Arena & Streak Microservice |
| **AiTutor API** | `:5006` | HTTP (`/api/aitutor/*`) | Gemini Tutor Microservice |
| **Catalog API** | `:5008` | HTTP (`/api/catalog/*`) | Curriculum Microservice |
| **PostgreSQL 17** | `:5432` | TCP (PostgreSQL `pgvector`) | User: `postgres`, Pass: `postgres`, DB: `nettutos_db` |
| **Redis 7** | `:6379` | TCP (RESP) | Cache L2 & SignalR Backplane (No password) |
| **RabbitMQ** | `:5672` / `:15672` | AMQP / HTTP Management | User: `guest`, Pass: `guest` |
| **Apache Kafka** | `:9092` | TCP (KRaft) | Real-time event streams |
| **Prometheus** | `:9090` | HTTP Web UI | Metrics scraper (`/metrics`) |
| **Grafana** | `:3000` | HTTP Web UI | User: `admin`, Pass: `admin` |
| **Grafana Tempo** | `:3200` / `:4317` | HTTP / gRPC OTLP | Distributed Tracing Collector |

---

## 5. Danh Mục Thành Phẩm Độc Lập Đã Đóng Gói (`dist/`)

Toàn bộ các gói cài đặt đã được đóng gói sẵn và bảo quản tại thư mục `dist/`:

1. **Android Signed APK (`dist/mobile/NET-Tutos-v1.0.0-Signed.apk`):**
   - Đã được ký bằng debug keystore, hỗ trợ Android 5.0 trở lên. Có thể cài trực tiếp lên điện thoại thật hoặc trình giả lập bằng lệnh:
     ```powershell
     adb install dist/mobile/NET-Tutos-v1.0.0-Signed.apk
     ```
2. **VS Code Extension (`dist/vscode/vscode-atus-0.1.0.vsix`):**
   - Gói cài đặt offline cho Visual Studio Code. Cài đặt bằng:
     ```powershell
     code --install-extension dist/vscode/vscode-atus-0.1.0.vsix
     ```
3. **Production Helm Chart (`dist/helm/nettutos-1.0.0.tgz`):**
   - Bản nén Helm release đã kiểm tra strict lint. Cài đặt lên K8s bằng:
     ```powershell
     helm upgrade --install nettutos dist/helm/nettutos-1.0.0.tgz
     ```

---

## 6. Dữ Liệu & Bản Sao Lưu Snapshot (`data/snapshots/`)

- **Tệp CSDL Snapshot:** `data/snapshots/dotnet_tutorials_freeze.db` (SQLite 3.3 MB, 37 bảng đầy đủ).
- **Tệp thống kê:** `data/snapshots/database_summary.json` (Ghi chi tiết số lượng bản ghi của từng bảng: 49 bài học, 1000 users, 8 categories, 35 quiz questions, v.v.).
- **Khôi phục nhanh CSDL khi cần:**
  ```powershell
  ./scripts/nettutos.ps1 restore
  ```

---

## 7. Kế Hoạch Cho Tương Lai (Future Resumption Backlog)

Khi bạn quyết định mở lại dự án để phát triển thêm, dưới đây là các hướng đi chiến lược có thể tiếp tục ngay:

1. **Mobile Production Release (Phase 29):**
   - Tạo keystore sản xuất (`release.keystore`) và đóng gói định dạng `.aab` (Android App Bundle) để tải lên Google Play Console.
   - Thêm target iOS (`net10.0-ios`) khi có máy tính macOS/Apple Developer account.
2. **Multi-Region & Cloud Migration (Phase 30):**
   - Đẩy Docker images lên Docker Hub hoặc AWS ECR / Azure ACR.
   - Triển khai cụm Kubernetes Helm lên AWS EKS hoặc Azure AKS có cấu hình Auto-scaling (HPA).
3. **Advanced AI Tutor & Voice Chat (Phase 31):**
   - Nâng cấp Gemini Live Audio WebSocket để cho phép đàm thoại 2 chiều trực tiếp qua micro điện thoại thời gian thực mà không cần bấm nút kết thúc transcript.

---

## 8. Lời Kết

Dự án **NET-Tutos** đã hoàn thành xuất sắc toàn bộ lộ trình qua **28 Giai đoạn**, từ nền tảng nguyên khối ban đầu cho đến một hệ thống phân tán cấp doanh nghiệp hoàn chỉnh. Mọi tài nguyên, mã nguồn, bài test, tài liệu và công cụ quản trị đều đã được đồng bộ chuẩn mực và sẵn sàng phục vụ bạn bất kỳ lúc nào!
