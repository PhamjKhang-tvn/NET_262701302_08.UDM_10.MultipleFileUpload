# TV1 — Kiến trúc, cấu hình và protocol

## Công nghệ và cấu trúc
- Visual Studio 2026, workload .NET desktop development, SDK .NET 10; `global.json` định hướng SDK 10.0.100, latestFeature. Bản source không còn `Code/App.config` cũ.
- `UDM10.sln` chứa sáu project. Client và Server target `net10.0-windows`, bốn project Shared/ServerCore/Headless/Tests target `net10.0`. `BuildInfo.cs` và app.manifest là metadata dùng chung. `Client/Program.cs`, `Server/Program.cs` là bootstrap WinForms .NET 10; phối hợp TV2/TV6 khi cập nhật, không nhận công riêng cho GUI.
- TV1 chịu trách nhiệm kiến trúc, cấu hình, contract và codec dùng chung. TV2: ClientForm/Theme; TV3: UploadClientService; TV4: ServerEngine; TV5: UploadQueue; TV6: ServerForm/Headless/Tests. TV1 tích hợp, review chứ không thay quyền tác giả.

## Đọc mã TV1 theo thứ tự
| File | Thành phần / ý nghĩa |
|---|---|
| `Code/Shared/Models.cs` | Message version 1, Type, Name, Size, Id, Sha256, Code, Text; ClientOptions/ServerOptions, luật tên file, DTO progress/job/result. |
| `Code/Shared/Settings.cs` | AppSettings.Load/Save; cấu hình được lưu ở LocalApplicationData/UDM10 theo role, dùng file tạm rồi Replace/Move. Khi đọc lỗi dùng mặc định; kiểm tra cấu hình ở GUI/options. |
| `Code/Shared/Wire.cs` | Serialize/Deserialize JSON; ReadExactCore lặp đến đủ byte hoặc EOF; ReadMessage/WriteMessage đóng gói 4 byte big-endian; Timed đua I/O và Task.Delay, đóng socket khi hết thời gian/hủy. |
| `Tools/Build.ps1` | Kiểm tra SDK 10, build đủ 6 project, lưu log và xác nhận output; có tùy chọn Run/Test. |
| `Tools/Package-Submission.ps1` | Gói 7z thật với mã học phần/nhóm và kiểm tra archive. |

## Giao thức UDM10 v1
TCP là byte stream, không phải message stream. Frame điều khiển: length 4 byte big-endian + JSON UTF-8 (1..16384 byte); payload file là N byte thô theo size Int64, đọc/gửi từng khối 64 KiB. Không cấp phát toàn bộ file.

Server -> WELCOME; Client -> UPLOAD(name,size); Server -> READY(id); Client -> payload đúng N byte; Client -> END(id,sha256); Server -> COMPLETE(id,name,size,sha256); đóng kết nối. Nhánh PING riêng phục vụ kiểm tra kết nối. Server có thể trả ERROR/BUSY. Client chỉ đánh Completed khi xác nhận cuối hợp lệ; gửi đủ byte không đủ để kết luận thành công. Mất ACK cuối -> UNKNOWN_RESULT, cần kiểm tra file phía Server. Không có resume hoặc idempotency tự động.

## Cấu hình và phạm vi
IP 127.0.0.1, port 5000, timeout 30 giây, max file 1024 MiB, max sessions 3 mặc định. Port 1..65535; timeout 1..600 giây; giới hạn file 0..10 GiB; sessions 1..16. Một Client gửi một file mỗi thời điểm; Server phục vụ nhiều phiên độc lập. Từ chối tên trùng, file .part chỉ được đổi tên sau kiểm tra size/hash. SHA-256 kiểm tra toàn vẹn, không thay thế xác thực hay TLS. Dùng mạng tin cậy.

## Tài liệu
- `Extra/Guides/PROTOCOL.md`, `NET10_MIGRATION.md` trong bộ nguồn chung.
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync?view=net-10.0
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props-desktop

Tài liệu mô tả mã nguồn cung cấp, không xác nhận đã chạy trên máy nhóm.
