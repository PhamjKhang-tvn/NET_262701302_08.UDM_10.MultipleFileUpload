# TV3 - Nhiệm vụ đầy đủ (Theo bảng phân công 4 tuần)

Tài liệu này tổng hợp toàn bộ nhiệm vụ của thành viên TV3 (Lê Đăng Khoa) theo tiến độ 4 tuần của nhóm. 

## Tuần 1: Kết nối và Gửi HELLO
- **Công việc:** Tìm hiểu TCP, IP/port, TcpClient, NetworkStream. Viết Client kết nối tới Server và gửi bản tin `HELLO`.
- **Sản phẩm:** Code `Program.cs` kết nối TCP, gửi/nhận bản tin.
- **Điều kiện nghiệm thu:** Kết nối được tới process Server riêng. Server chưa bật thì hiện lỗi (try-catch) chứ không bị crash.
- **Người review:** TV4.

## Tuần 2: Gửi File (Chunking) và Chờ Xác nhận
- **Công việc:** Viết hàm `SendFileAsync` gửi metadata, chờ `READY`, chia file thành khối 64KB để gửi, chờ `SUCCESS` hoặc `ERROR`.
- **Sản phẩm:** Dịch vụ `UploadClientService` có báo tiến độ.
- **Điều kiện nghiệm thu:** Không đọc toàn bộ file vào RAM. Chỉ báo Completed khi có xác nhận `SUCCESS` từ Server.
- **Người review:** TV4.

## Tuần 3: Timeout và Xử lý Hủy
- **Công việc:** Áp dụng Timeout thực tế (30s). Hủy tiến trình bằng `CancellationToken` khi đóng app. Xử lý lỗi mất phản hồi cuối.
- **Sản phẩm:** Client timeout/cancel/cleanup đã kiểm tra.
- **Điều kiện nghiệm thu:** Server im lặng không gây chờ vô hạn. Không tự động retry mù (tránh tạo file trùng).
- **Người review:** TV4.

## Tuần 4: Tích hợp, Test và Báo cáo
- **Công việc:** Sửa lỗi, viết báo cáo phần truyền file, quay video demo có mặt, clone/build sạch.
- **Sản phẩm:** Báo cáo Word, Slide, Video demo.
- **Điều kiện nghiệm thu:** Một thành viên khác chạy được Client theo README. Giải thích được luồng file đi qua code.
- **Người review:** TV4.