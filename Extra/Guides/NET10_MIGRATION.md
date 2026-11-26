# UDM_10 — migration từ .NET Framework 4.8 sang .NET 10

## Tình trạng

- **Đã chuyển source/project:** cả 6 `.csproj` sang SDK-style, target `net10.0` hoặc `net10.0-windows`.
- **Đã sửa workflow:** dùng `dotnet build`, sử dụng đường dẫn output riêng cho từng project, không gọi `csc.exe` của .NET Framework; cập nhật `TC_36` tìm Headless DLL theo thư mục build mới.
- **Đã cập nhật startup WinForms:** `ApplicationConfiguration.Initialize()`, cấu hình PerMonitorV2 từ `.csproj`, bỏ App.config .NET Framework không còn hợp lệ.
- **Chưa biên dịch/nghiệm thu trên Windows trong môi trường tạo gói:** máy tạo gói không có .NET 10 SDK hoặc môi trường GUI Windows; trạng thái kết quả test vẫn là **chưa chạy**.

## Yêu cầu môi trường

1. Windows 10/11 x64 và Visual Studio 2026 có workload `.NET desktop development`.
2. Cài **.NET 10 SDK** (không chỉ Runtime). Mở PowerShell: `dotnet --list-sdks` phải có `10.0.xxx`.
3. Mở `UDM10.sln` tại **thư mục gốc bộ project đầy đủ**, không mở Solution trong bộ TV1 từng tuần.

## Chạy lần đầu

- Mở `UDM10.sln` bằng Visual Studio 2026 → `Build` → `Rebuild Solution`.
- `Configuration Manager`: chọn `Debug | Any CPU`; đánh dấu Build cho Client, Server, Shared, ServerCore, Headless, Tests.
- Chuột phải `Udm10.Server` → `Set as Startup Project` → F5 để chạy thử Server.
- Để chạy cả hai: Solution → `Configure Startup Projects` → `Multiple startup projects`: Server=Start, Client=Start; phần còn lại=None.
- Hoặc nhấn `01_BUILD_AND_RUN.cmd`; script dùng .NET 10 SDK và chạy hai file exe đã build.
- Các bản chạy không còn nằm chung một `Build` folder: Server là `Code/Server/bin/Debug/net10.0-windows/Udm10.Server.exe`, Client tương tự.

## Kiểm thử / xác minh

- Chạy `04_BUILD_ONLY.cmd` trước.
- Chạy `02_RUN_TESTS.cmd` (28 ca tự động); ghi nhận PASSED/FAILED **theo output thực tế**, không tự điền.
- Tự chạy 8 ca GUI/LAN thủ công theo workbook `Extra/TestCases/UDM10_Test_Cases.xlsx`.
- Đặc biệt kiểm tra: WinForms mở được, kéo thả nhiều file, tiến độ/tốc độ từng file, file trùng tên, mất mạng/timeout, checksum, nhiều Client, TC_36 tiến trình độc lập.
- Nếu SDK 10 chưa có: `winget install Microsoft.DotNet.SDK.10` hoặc tải SDK chính thức từ `https://dotnet.microsoft.com/en-us/download/dotnet/10.0`.

## Commit GitHub đề xuất

Tạo nhánh riêng, ví dụ `chore/net10-migration`. Commit các thay đổi thật dưới tên công việc migrate, không ghi ngày quá khứ hoặc giả làm đóng góp của thành viên khác.

1. `build: migrate all UDM10 projects to .NET 10 SDK-style` — `.csproj`, `.sln`, `global.json`, startup, `.gitignore`.
2. `test: adapt headless process test for .NET 10 output layout` — test TC_36 và script `Tools/Build.ps1`.
3. `docs: update UDM10 setup and migration notes for .NET 10` — hướng dẫn, README, báo cáo/slides khi đã rà.

## Giữ nguyên nghiệp vụ

- Không đổi giao thức TCP (`WELCOME/UPLOAD/READY/END/COMPLETE`, 4-byte prefix + UTF-8 JSON, SHA-256).
- Không thêm Pause/Resume, database, server cloud, authentication hoặc thay đổi giới hạn queue của UDM_10.
- Cần test lại vì nâng runtime/framework có thể thay đổi hành vi và thư viện dù source biên dịch được.
