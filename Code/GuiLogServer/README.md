# GUI Log Server

Ứng dụng WinForms chạy HTTP server bằng Kestrel để nhóm thực hành test chức năng, dữ liệu sai, mất kết nối, tải và so sánh file. Khi bật, server lắng nghe trên tất cả giao diện mạng của máy. GUI hiển thị các địa chỉ IPv4 để thành viên khác trong cùng mạng LAN kết nối.

## Chạy ứng dụng

Máy cần cài .NET 10 SDK và Windows. Clone repository rồi mở PowerShell tại thư mục dự án:

```powershell
git clone https://github.com/PhamjKhang-tvn/NET_262701302_08.UDM_10.MultipleFileUpload.git
cd .\NET_262701302_08.UDM_10.MultipleFileUpload
git switch tv6/gui-log-server
cd .\Code\GuiLogServer
dotnet build .\GuiLogServer.csproj
dotnet run --project .\GuiLogServer.csproj
```

Trong GUI, chọn cổng, thư mục nhận (mặc định trong Documents), rồi bấm **Bật server**. Bấm **Tắt server** để ngừng nhận file. Dùng địa chỉ IP hiển thị trên GUI để các máy khác trong cùng mạng gọi server, ví dụ `http://192.168.1.20:8080/health`. Địa chỉ cụ thể tùy mạng của bạn.

Server chưa có đăng nhập hay mã hóa. Chỉ chia sẻ trên mạng tin cậy; nếu Windows Firewall chặn kết nối, chỉ cho phép ứng dụng/cổng này ở hồ sơ **Private**. Không mở trên mạng công cộng và không cấu hình chuyển tiếp cổng từ Internet.

## Chức năng và tiêu chí kiểm thử

| Thao tác | Kết quả mong đợi |
|---|---|
| Test thành công | Gọi `/echo?message=hello`, nhận HTTP 200 và nội dung `hello` |
| Test dữ liệu sai | Gửi message rỗng, server trả HTTP 400 |
| Test mất kết nối | TCP client đóng socket khi POST body chưa gửi đủ; server vẫn trả lời request `/health` tiếp theo |
| Tải thấp (10) | 10 request `/health`, đồng thời tối đa 5; tất cả thành công |
| Tải cao (100) | 100 request `/health`, đồng thời tối đa 25; tất cả thành công |
| Nhận file | `POST /upload?filename=<tên-file>`; ghi vào thư mục nhận theo luồng, trả kích thước và SHA-256 |
| So sánh nguồn–đích | Đối chiếu kích thước và SHA-256 theo luồng; không nạp toàn bộ file lớn vào bộ nhớ |
| Lưu bằng chứng | Xuất thời gian, mức độ và nội dung log ra file `.txt` |

Các endpoint cũng có thể được gọi thủ công:

- `GET http://127.0.0.1:8080/health`
- `GET http://127.0.0.1:8080/echo?message=hello`
- `POST http://127.0.0.1:8080/echo` với nội dung text không rỗng
- `POST http://127.0.0.1:8080/upload?filename=ghi-chu.txt` với raw bytes của file trong request body. Ví dụ PowerShell:

```powershell
$file = Get-Item ".\ghi-chu.txt"
$url = "http://127.0.0.1:8080/upload?filename=$([uri]::EscapeDataString($file.Name))"
Invoke-WebRequest -Uri $url -Method Post -InFile $file.FullName -ContentType "application/octet-stream"
```

Server từ chối tên đường dẫn/ tên dành riêng của Windows và file trùng tên (HTTP 409), không ghi đè file cũ. Tệp được ghi vào file tạm rồi đổi tên sau khi nhận đủ; file tối đa 2 GiB. Kết quả upload là HTTP 201 cùng kích thước và SHA-256.

Các máy trong LAN thay `127.0.0.1` bằng địa chỉ IPv4 đang hiển thị trên GUI server.

## Kiểm thử nhận và đối chiếu file

Chạy bộ kiểm tra không cần cài thêm package:

```powershell
dotnet run --project .\tests\FileIntegrityChecks\FileIntegrityChecks.csproj
```

Bộ kiểm tra tạo tệp text, ảnh PNG, tệp 0 byte, tên tệp tiếng Việt và tệp lớn 64 MiB; sau đó nhận tệp, so sánh kích thước/SHA-256 và xác minh nội dung khác nhau, tên đường dẫn không hợp lệ, file trùng tên và luồng bị ngắt sớm.

## Tích hợp listener TV4

Trong repository hiện chưa có mã listener TV4 để ghép và kiểm tra giao thức trực tiếp. Phần ghi file được tách thành `FileReceiveService.SaveAsync(Stream, destinationDirectory, fileName, expectedLength, cancellationToken)`. Khi có listener TV4, listener có thể gọi dịch vụ này với stream chứa đúng phần payload và độ dài file; lớp dịch vụ sẽ ghi theo luồng, tính SHA-256 và không chiếm thêm bộ nhớ theo kích thước file. Nếu giao thức TV4 chưa cung cấp kích thước, cần bổ sung ranh giới/độ dài payload trước khi nối để không đọc lấn sang file tiếp theo.

## Ghi bằng chứng, Git và review

1. Chạy từng kịch bản và kiểm tra log có `PASS`; nếu có `FAIL`, lưu log và ghi lại bước tái hiện cùng thông báo lỗi.
2. Bấm **Lưu bằng chứng** sau mỗi lượt; đặt tên file có ngày giờ và người thực hiện, ví dụ `evidence-20261031-an.txt`. Có thể chụp thêm ảnh GUI thể hiện trạng thái và log.
3. Tạo branch riêng cho phần việc, cập nhật từ branch nhóm trước khi làm và chỉ stage các file thuộc phạm vi của mình. Trước khi commit, kiểm tra `git status --short`, `git diff` và `git diff --staged` để chắc chắn không đưa nhầm file hoặc dữ liệu vào commit.
4. Chạy build và các test liên quan, rồi commit với thông điệp ngắn gọn mô tả thay đổi, ví dụ `test: add invalid-input checks`. Không commit file bằng chứng, dữ liệu cá nhân hoặc file lớn nếu nhóm không yêu cầu.
5. Push branch lên remote và mở Pull Request (PR) vào branch đã thống nhất của nhóm. Mô tả mục tiêu, cách kiểm thử và kết quả; đính kèm bằng chứng khi phù hợp.
6. Người review kiểm tra phạm vi thay đổi, tính đúng đắn, xử lý lỗi và kết quả test; để lại nhận xét cụ thể trên PR. Tác giả xử lý nhận xét, chạy lại test cần thiết và cập nhật PR. Chỉ merge sau khi các nhận xét đã được giải quyết và nhóm chấp thuận.
7. Nếu nhóm không dùng PR, áp dụng các bước kiểm tra diff và review chéo trước khi tích hợp bằng quy trình nhóm đã thống nhất. Thống nhất nơi lưu bằng chứng chung; không đưa bằng chứng vào repository nếu không cần thiết.
