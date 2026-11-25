# TV3 - Nội dung Báo cáo (Phần Client)

## Giao thức truyền file
Client thực hiện gửi file theo quy trình khép kín:
1. Kết nối TCP, nhận `WELCOME` từ Server.
2. Gửi gói `UPLOAD` chứa Tên file và Kích thước.
3. Chờ Server phản hồi `READY`.
4. Đọc file theo từng khối 64KB, gửi qua `NetworkStream`. Đồng thời tính mã SHA-256.
5. Gửi gói `END` kèm mã SHA-256.
6. Chờ Server phản hồi `COMPLETE` để xác nhận file đã lưu thành công.

## Xử lý lỗi và Timeout
- Sử dụng `SendTimeout` và `ReceiveTimeout` (30s).
- Sử dụng `CancellationToken` để hủy tiến trình khi cần.
- Phân loại lỗi rõ ràng: `LOCAL_FILE` (lỗi file), `TIMEOUT` (quá thời gian), `CONNECTION_LOST` (mất mạng), `UNKNOWN_RESULT` (mất xác nhận cuối).

## Minh chứng cần chèn
- [Link commit code `UploadClientService.cs` trên GitHub]
- [Ảnh chụp màn hình giao diện đang gửi file]
- [Ảnh chụp màn hình lỗi Timeout khi tắt Server]