# ClientGui – Giao diện Client (TV2)

WinForms (net10.0-windows, cùng bản với GuiLogServer của TV6). Chỉ làm giao diện: IP/port, chọn/kéo thả file,
bảng file (tên, kích thước, trạng thái, tiến độ, tốc độ, lỗi), tiến độ tổng, Bắt đầu gửi/Hủy.
IP/port mặc định 127.0.0.1:8888 (theo Client của TV3); đổi theo protocol-v1 của TV1 khi có.

## Chạy thử
```
cd Code/ClientGui
dotnet build
dotnet run
```
- "Nạp dữ liệu giả" chỉ để xem bố cục; dòng mang nhãn [DEMO] và (giả lập), không gửi được.
- Bấm "Bắt đầu gửi" hiện chưa nối dịch vụ gửi (làm ở chặng 2A cùng TV3, TV5).

## Điểm nối cho TV3, TV5 (chặng 2A)
- Sự kiện `StartRequested` (Host, Port, Items), `CancelRequested`.
- `SetUploading(bool)`, `HienThongBao(text, loi)`: gọi được từ luồng nền.
- `UploadItem` (Status, Progress, SpeedBytesPerSec, ErrorMessage) là mô hình hiển thị của GUI; sẽ ánh xạ từ
  `FileTaskItem` của TV5 (Pending→Waiting, Connecting/Sending→Uploading, Completed, Failed→Error, Cancelled→Canceled).
- Từ chặng 2A: đặt `HienNutDuLieuGia = false` trong MainForm.cs.
