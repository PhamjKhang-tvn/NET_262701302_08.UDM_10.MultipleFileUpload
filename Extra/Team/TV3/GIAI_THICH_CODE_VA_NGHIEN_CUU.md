# TV3 - Giải thích Source Code và Tài liệu nghiên cứu

**Module chính:** TCP Client - Dịch vụ truyền file (`UploadClientService.cs`)

## 1. Vị trí file code
- File chứa logic gửi file: `Code/Shared/UploadClientService.cs`.

## 2. Các hàm quan trọng trong code
- `Connect(...)`: Khởi tạo `TcpClient`, thiết lập Timeout kết nối, chờ Server gửi `WELCOME`.
- `UploadAsync(...)`: Hàm chính thực hiện gửi file. Đọc file theo từng khối (buffer), tính SHA-256, gửi dữ liệu và chờ phản hồi.
- `Check(...)`: Kiểm tra mã phản hồi từ Server (READY, COMPLETE, ERROR).
- `Publish(...)`: Cập nhật tiến trình (số byte đã gửi, % hoàn thành, tốc độ) về cho giao diện.

## 3. Các khái niệm kỹ thuật cần nắm
- **TCP là luồng byte (byte stream):** Một lần gửi (Write) không có nghĩa là một lần nhận (Read) sẽ nhận đủ. Phải dùng hàm đọc chính xác (`ReadExactlyAsync`) để đảm bảo đủ byte.
- **Chunking (Chia khối):** Đọc file bằng `FileStream` theo từng khối 64KB để tránh tràn RAM.
- **SHA-256:** Mã băm dùng để kiểm tra tính toàn vẹn của file sau khi truyền.
- **CancellationToken:** Cơ chế dừng tiến trình gửi file ngay lập tức khi người dùng bấm Hủy.

## 4. Tài liệu tham khảo chính thức
- [TcpClient - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient)
- [NetworkStream.ReadAsync](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync)
- [Lập trình bất đồng bộ C#](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)