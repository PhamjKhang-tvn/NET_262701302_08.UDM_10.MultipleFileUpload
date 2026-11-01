# GUI Log Server

Ứng dụng WinForms chạy HTTP server bằng Kestrel để nhóm thực hành test chức năng, dữ liệu sai, mất kết nối, tải và so sánh file. Khi bật, server lắng nghe trên tất cả giao diện mạng của máy. GUI hiển thị các địa chỉ IPv4 để thành viên khác trong cùng mạng LAN kết nối.

## Chạy ứng dụng

Máy cần cài .NET 10 SDK và Windows. Mở PowerShell tại thư mục này rồi chạy:

```powershell
dotnet run --project .\GuiLogServer.csproj
```

Trong GUI, chọn cổng (mặc định `8080`) rồi bấm **Bật server**. Dùng địa chỉ IP hiển thị trên GUI để các máy khác trong cùng mạng gọi server, ví dụ `http://192.168.1.20:8080/health`. Địa chỉ cụ thể tùy mạng của bạn.

Server chưa có đăng nhập hay mã hóa. Chỉ chia sẻ trên mạng tin cậy; nếu Windows Firewall chặn kết nối, chỉ cho phép ứng dụng/cổng này ở hồ sơ **Private**. Không mở trên mạng công cộng và không cấu hình chuyển tiếp cổng từ Internet.

## Chức năng và tiêu chí kiểm thử

| Thao tác | Kết quả mong đợi |
|---|---|
| Test thành công | Gọi `/echo?message=hello`, nhận HTTP 200 và nội dung `hello` |
| Test dữ liệu sai | Gửi message rỗng, server trả HTTP 400 |
| Test mất kết nối | TCP client đóng socket khi POST body chưa gửi đủ; server vẫn trả lời request `/health` tiếp theo |
| Tải thấp (10) | 10 request `/health`, đồng thời tối đa 5; tất cả thành công |
| Tải cao (100) | 100 request `/health`, đồng thời tối đa 25; tất cả thành công |
| So sánh 2 file | So sánh nội dung bằng SHA-256; log kết quả và hash |
| Lưu bằng chứng | Xuất thời gian, mức độ và nội dung log ra file `.txt` |

Các endpoint cũng có thể được gọi thủ công:

- `GET http://127.0.0.1:8080/health`
- `GET http://127.0.0.1:8080/echo?message=hello`
- `POST http://127.0.0.1:8080/echo` với nội dung text không rỗng

Các máy trong LAN thay `127.0.0.1` bằng địa chỉ IPv4 đang hiển thị trên GUI server.

## Ghi bằng chứng và commit cá nhân

1. Chạy từng kịch bản và kiểm tra log có `PASS`; nếu có `FAIL`, lưu log và ghi lại bước tái hiện cùng thông báo lỗi.
2. Bấm **Lưu bằng chứng** sau mỗi lượt; đặt tên file có ngày giờ và người thực hiện, ví dụ `evidence-20261031-an.txt`. Có thể chụp thêm ảnh GUI thể hiện trạng thái và log.
3. Mỗi thành viên chỉ stage phần mình thay đổi, xem lại bằng `git diff --staged`, rồi commit với thông điệp nêu rõ nội dung, ví dụ `test: add invalid-input evidence`.
4. Không đưa file bằng chứng, dữ liệu cá nhân hoặc file lớn vào commit nếu nhóm không yêu cầu; thống nhất nơi lưu bằng chứng chung.
