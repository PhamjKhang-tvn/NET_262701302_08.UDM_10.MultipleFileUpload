# Sản phẩm bàn giao: GUI Log Server

## phần tui làm

Tui đã làm một ứng dụng GUI đơn giản để bật/tắt server và theo dõi hoạt động qua log. Server dùng Kestrel và lắng nghe trên các giao diện mạng của máy để các bạn trong cùng mạng LAN có thể kết nối; cổng mặc định là `8080`. Khi bật server, GUI hiện địa chỉ IPv4 để gửi cho các thành viên.

Trên giao diện có các nút kiểm tra server trả lời bình thường, xử lý dữ liệu không hợp lệ, hoạt động sau khi client mất kết nối, và chịu hai mức tải. Ngoài ra còn có chức năng so sánh nội dung hai file và lưu log ra file để làm bằng chứng.

## Giao diện

Phần trên cùng hiển thị tên ứng dụng, ô chọn cổng, nút bật/tắt server, trạng thái hoạt động, bộ đếm request và địa chỉ IP để thành viên khác kết nối.

Bên dưới là các nút chạy test: kiểm tra thành công, dữ liệu sai, mất kết nối, tải thấp và tải cao. Cùng khu vực này có nút so sánh hai file và lưu log làm bằng chứng.

Phần còn lại dành cho nhật ký hoạt động. Mỗi dòng ghi thời gian, mức độ như `PASS`, `WARN` hoặc `FAIL`, cùng nội dung kết quả để nhóm dễ theo dõi.

## Cách nhóm kiểm tra

1. Bật server, chạy **Test thành công**. Kết quả mong đợi là HTTP 200 và nội dung `hello`.
2. Chạy **Test dữ liệu sai**. Message rỗng hoặc dài hơn 4096 ký tự phải nhận HTTP 400.
3. Chạy **Test mất kết nối**. Client ngắt giữa lúc gửi nội dung POST; sau đó server vẫn cần trả lời được request `/health`.
4. Chạy **Tải thấp (10)**: gửi 10 request, tối đa 5 request cùng lúc.
5. Chạy **Tải cao (100)**: gửi 100 request, tối đa 25 request cùng lúc.
6. Thử **So sánh 2 file** với một cặp file giống nội dung và một cặp khác nội dung. Log sẽ báo giống hoặc khác dựa trên SHA-256.

## Ghi lại kết quả

Sau khi chạy test, xem log để biết case nào `PASS` hoặc `FAIL`, rồi bấm **Lưu bằng chứng** để xuất log. Nếu có lỗi, ghi lại các bước gây lỗi và chụp ảnh giao diện nếu cần. Nhóm nên thống nhất chỗ lưu bằng chứng để mọi người cùng tìm được.

Server chưa có đăng nhập hay mã hóa nên chỉ dùng trên mạng tin cậy. Nếu Windows Firewall chặn kết nối, chỉ cho phép cổng ở hồ sơ mạng **Private**; không mở server ra mạng công cộng hoặc Internet.

Mỗi thành viên commit phần việc của mình: kiểm tra `git diff --staged` trước khi commit và dùng thông điệp ngắn mô tả thay đổi.

## Tình trạng hiện tại
Ứng dụng đã build thành công. Các test thành công, dữ liệu sai, mất kết nối và hai mức tải đã chạy đạt. Mình cũng đã gọi `/health` qua loopback và các địa chỉ IPv4 của máy để xác nhận listener LAN. Cần nhờ một máy khác trong cùng mạng thử kết nối thực tế; firewall có thể cần cho phép cổng ở hồ sơ **Private**. Chức năng so sánh file và lưu log đã có trên giao diện; nhóm có thể tự thao tác để lưu bằng chứng của các chức năng này.

# chúc anh em làm tốt ^.^