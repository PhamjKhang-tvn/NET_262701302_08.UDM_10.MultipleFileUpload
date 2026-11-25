# TV4 TCP Server nhận và lưu file

Phần bàn giao gồm `ServerEngine.cs`, project ServerCore được nối với mã nguồn này, chương trình console demo và bộ kiểm thử bằng TCP thật trên loopback. Mã gốc trong gói `TV4_TCP_Server_Receive` đã có trong repo; các thay đổi bổ sung tập trung vào build, demo, kiểm thử và cô lập callback lỗi.

## Chạy kiểm thử

Tại thư mục gốc repository, chạy PowerShell:

```powershell
.\Code\NhanFileTuServer\Test-TV4.ps1
```

Script ưu tiên SDK cục bộ ở `..\.tools\dotnet\dotnet.exe` nếu có, hoặc dùng `dotnet` trong PATH. Có thể chỉ định `-DotnetPath 'C:\duong-dan\dotnet.exe'`. Cần .NET 10 SDK. Script build các project Headless, Tests và các dependency Shared, ServerCore; mã thoát khác 0 nghĩa là build hoặc test thất bại. Log build, hash nguồn và môi trường được lưu trong `BuildLogs/TV4-build-*`; từng lượt test có `results.txt`, file nhận và log server trong `BuildLogs/TV4/*`. Các dữ liệu này đã được gitignore; giữ trên máy khi cần đối chiếu minh chứng.

## Demo nhận file

Sau khi script build xong, mở hai cửa sổ PowerShell tại gốc repo. Nếu chưa cài SDK vào PATH, thay `dotnet` bằng `& 'D:\Nam\.tools\dotnet\dotnet.exe'` trên máy hiện tại.

Server:

```powershell
dotnet Code/TV1/Code/Headless/bin/Debug/net10.0/Udm10.Headless.dll server ./UDM10_Uploads 5000
```

Client dùng dữ liệu thử tự tạo:

```powershell
'Du lieu thu TV4' | Set-Content ./BuildLogs/demo.txt
dotnet Code/TV1/Code/Headless/bin/Debug/net10.0/Udm10.Headless.dll ping 127.0.0.1 5000
dotnet Code/TV1/Code/Headless/bin/Debug/net10.0/Udm10.Headless.dll upload 127.0.0.1 5000 ./BuildLogs/demo.txt
```

Gửi lại cùng tên sẽ bị `DUPLICATE`. Ctrl+C dừng server và chờ dọn phiên. Chạy lại lệnh server để tạo instance mới. Mặc định chỉ nghe `127.0.0.1`; nếu demo LAN, thêm địa chỉ IPv4 của card mạng vào cuối lệnh server.

## Giao thức đang dùng

Gói TV4 được gửi sau dùng `Udm10.Shared/Wire.cs`: mỗi gói điều khiển có prefix **4 byte big-endian** và JSON UTF-8 tối đa 16 KiB, `version=1`. Mỗi kết nối truyền một file hoặc một lần PING.

```text
Server -> WELCOME (size = giới hạn)
Client -> UPLOAD (name, size)
Server -> READY (id) hoặc ERROR (code)
Client -> đúng size byte dữ liệu thô
Client -> END (id, sha256)
Server -> COMPLETE (id, name, size, sha256) hoặc ERROR
```

Server không gửi COMPLETE trước khi nhận đủ byte, kiểm tra END/hash, đóng file tạm và `File.Move` thành công. Tên đã tồn tại hoặc đang được giữ bởi phiên khác bị từ chối, không tự thêm số vào tên. Quy tắc này theo gói phân công mới; khác ví dụ cũ trong `DOCX/TV4.docx`.

`FileReceiver.cs`/`FileSender.cs` dùng header little-endian và ACK một byte; `ChatApp_Client/Program.cs` dùng frame type + length. Hai giao thức đó không tương thích với ServerEngine này. Demo và test hiện dùng `UploadClientService` trong Shared. TV3 cần thống nhất điểm gọi này khi ghép GUI.

## Điểm nối cho TV6 và TV5

```csharp
var server = new Udm10.ServerCore.ServerEngine();
server.Log += line => { /* chuyển về UI bằng BeginInvoke rồi hiển thị log */ };
server.Changed += update => { /* Id, Name, State, Received, Total, BytesPerSecond, Detail */ };
server.Start(new Udm10.Shared.ServerOptions {
    Address = "127.0.0.1", Port = 5000, Folder = @"D:\ThuMucNhan",
    MaxSessions = 3, TimeoutSeconds = 30, MaxFileBytes = 1024L * 1024 * 1024
});
// Trong luồng bất đồng bộ của nút Dừng/FormClosing:
await server.StopAsync();
// Tạo ServerEngine mới nếu muốn khởi động lại.
```

Các sự kiện chạy trên worker thread; handler cần xử lý nhanh và chuyển về UI. Từng subscriber được bắt lỗi riêng. `StopAsync` trả cùng một Task cho các lời gọi lặp trong quá trình dừng. Theo dõi `IsRunning`, `ActiveCount`, `BoundPort` và `LogPath` khi cần.

## Phạm vi xác minh

Bộ test này kiểm tra module nhận/lưu, không xác nhận GUI của TV2/TV6 hay toàn bộ `UDM10.sln`. Các project Client/Server trong solution còn tham chiếu file UI chưa có tại vị trí đó. Ca lỗi I/O tạo file đích sau READY để buộc bước commit thất bại; chưa mô phỏng đĩa đầy hoặc mất quyền ghi của hệ điều hành. Demo LAN, quay video và review trực tiếp với TV3 cần được nhóm thực hiện riêng.

Nguồn chính thức: [TCP và TcpListener](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes), [File.Move và lỗi khi file đích đã tồn tại](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0), [SDK .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
