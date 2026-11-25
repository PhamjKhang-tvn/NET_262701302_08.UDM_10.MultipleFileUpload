# Giao thức UDM10 v1 và cách đọc code

## Hợp đồng truyền

TCP truyền luồng byte, không giữ ranh giới message. `Wire.ReadExactCore` lặp cho tới đủ số byte hoặc gặp EOF. Một lần Write không tương ứng một lần Read. Nguồn: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync?view=net-10.0

Control frame: `uint32 big-endian length | length bytes UTF-8 JSON`. Implementation dùng Int32, từ chối giá trị ngoài 1..16384 trước khi cấp phát body. Payload file là dữ liệu thô có độ dài khai báo trước. Dữ liệu file không được chuyển base64/JSON, tránh tăng kích thước và đọc cả file vào RAM.

```json
{"version":1,"type":"UPLOAD","name":"report.txt","size":12345}
```

`size` là Int64 không âm; tên là basename hợp lệ, không chứa thư mục. Server chuẩn hóa Unicode Form C, so sánh tên đang giữ chỗ không phân biệt hoa/thường và dùng thao tác Move không ghi đè.

| Bước | Message | Hành vi |
|---|---|---|
| 1 | WELCOME | Server nêu version và size tối đa; lúc quá tải gửi ERROR/BUSY. |
| 2 | UPLOAD | Client gửi name, size. |
| 3 | READY | Server đã kiểm tra, giữ chỗ tên và mở file tạm; trả ID. |
| 4 | Payload | Client gửi đúng size byte, chia khối 64 KiB. |
| 5 | END | Client gửi ID, SHA-256 tính từ dữ liệu đã đọc/gửi. |
| 6 | COMPLETE | Server đã nhận đủ, so hash, flush, đóng file và đổi tên; trả ID/name/size/hash. |
| 7 | TCP close | Giải phóng socket. Client không giữ một kết nối chung cho toàn queue. |

Hash được cập nhật theo khối nên không phải giữ cả file hoặc đọc trước toàn bộ file. File rỗng vẫn đi đủ UPLOAD–READY–END–COMPLETE.

## Timeout và hủy

`Wire.Timed` đua task mạng với Task.Delay. Nếu hết hạn hoặc hủy, đóng TcpClient để làm gián đoạn I/O đang chờ, đồng thời quan sát exception đến muộn. Với network I/O bất đồng bộ trên .NET 10, không chỉ dựa vào ReadTimeout để đặt deadline cho ReadAsync. Mỗi control frame có một deadline bao trùm prefix + JSON. Payload áp dụng timeout không có tiến triển theo từng lần đọc/ghi. Không áp dụng giới hạn tổng thời gian cho file lớn hợp lệ.

## Trạng thái

Waiting → Uploading → Completed hoặc Error. Tổng byte gửi và trạng thái nghiệp vụ là hai thông tin riêng: có thể 100% byte nhưng vẫn Uploading trong lúc Server kiểm tra. Lỗi nguồn/validation không dừng các file tốt khác. Lỗi kết nối toàn dịch vụ giữ những file chưa thử ở Waiting. Dừng lượt tải không làm mất file đã Completed.

## Xác nhận không chắc chắn

Giả sử Server đã Move file thành công rồi mất mạng trước khi Client nhận COMPLETE. File có thể đã tồn tại thật. Client trả UNKNOWN_RESULT và yêu cầu kiểm tra Server. Không được diễn giải việc không nhận ACK thành kết luận chắc chắn Server chưa lưu. Bản này không có cơ chế idempotency/reconciliation tự động; tải lại gặp DUPLICATE và không ghi đè. Đây là giới hạn được công bố.

## Luồng UI

Client nhận cập nhật bằng Progress<T> tạo trên UI thread. Server phát event từ worker, ServerForm chuyển về UI bằng BeginInvoke. Không chạm DataGridView từ luồng mạng. Mọi tác vụ nặng chính chạy bất đồng bộ; code GUI chủ yếu kiểm tra input, gọi service và hiển thị.

Nguồn: https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-make-thread-safe-calls

## Thứ tự đọc mã

1. `Shared/Models.cs`: Message, options, job, state và luật tên file.
2. `Shared/Wire.cs`: Serialize, 4 byte prefix, read-exact, timeout.
3. `Shared/UploadClientService.cs`: một file từ kết nối đến ACK.
4. `ServerCore/ServerEngine.cs`: accept, slot, nhận payload, hash, file tạm.
5. `Shared/UploadQueue.cs`: lặp từng job; lỗi cục bộ vs lỗi toàn kết nối.
6. `Client/ClientForm.cs`, `Server/ServerForm.cs`: GUI và marshaling.
7. `Tests/Program.cs`: cách khẳng định hành vi bằng assertion, không chỉ nhìn thanh tiến trình.

Nguồn TCP lớp cao: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes
Nguồn JSON serializer: https://learn.microsoft.com/en-us/dotnet/api/system.runtime.serialization.json.datacontractjsonserializer?view=net-10.0
