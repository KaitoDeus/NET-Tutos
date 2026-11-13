# NET-Tutos Standalone Release Artifacts (Phase 28 Freeze)

Thư mục này chứa các gói thành phẩm độc lập đã được biên dịch và đóng gói sẵn từ mã nguồn dự án **NET-Tutos** (Phiên bản `.NET 10.0 / C# 14`), sẵn sàng để cài đặt offline hoặc triển khai lên môi trường sản xuất mà không cần cài đặt lại SDK phát triển.

---

## 1. Android Mobile App (`dist/mobile/`)

- **Tệp cài đặt:** `dist/mobile/NET-Tutos-v1.0.0-Signed.apk` (~85 MB)
- **Kiến trúc hỗ trợ:** ARM64 & x86_64 (Android 5.0 trở lên, API 21+)
- **Cách cài đặt:**
  - **Trên thiết bị Android thật:** Bật chế độ `Cài đặt ứng dụng không rõ nguồn gốc` trên điện thoại $\rightarrow$ Chép file `.apk` vào điện thoại $\rightarrow$ Nhấp mở để cài đặt.
  - **Qua ADB (Dòng lệnh):**
    ```powershell
    adb install dist/mobile/NET-Tutos-v1.0.0-Signed.apk
    ```
  - **Trên Trình Giả Lập Android:** Kéo thả trực tiếp file `.apk` vào cửa sổ máy ảo.

---

## 2. VS Code Extension (`dist/vscode/`)

- **Tệp cài đặt:** `dist/vscode/vscode-atus-0.1.0.vsix` (~33 KB)
- **Tương thích:** Visual Studio Code 1.90+ (Node.js 20+)
- **Tính năng:** Trợ lý lập trình Atus C#/.NET, Inline Code Review, tính toán độ phức tạp Big-O, tự động tạo unit test xUnit, chạy code Roslyn Sandbox.
- **Cách cài đặt:**
  - **Qua giao diện VS Code:** Mở tab Extensions (`Ctrl + Shift + X`) $\rightarrow$ Bấm menu `...` (Góc trên bên phải) $\rightarrow$ Chọn **Install from VSIX...** $\rightarrow$ Trỏ tới `dist/vscode/vscode-atus-0.1.0.vsix`.
  - **Qua Terminal:**
    ```powershell
    code --install-extension dist/vscode/vscode-atus-0.1.0.vsix
    ```

---

## 3. Kubernetes Production Helm Chart (`dist/helm/`)

- **Tệp cài đặt:** `dist/helm/nettutos-1.0.0.tgz` (~5.8 KB)
- **Tương thích:** Kubernetes 1.28+, Helm 3.12+
- **Thành phần tích hợp:** 16 Pods, YARP Gateway, Web MVC BFF, 4 Microservices, Redis 7, Postgres 17 `pgvector`, Kafka KRaft, Prometheus, Grafana, Tempo OTLP, và Cert-Manager TLS.
- **Cách triển khai:**
  ```powershell
  helm upgrade --install nettutos dist/helm/nettutos-1.0.0.tgz --namespace default
  ```
