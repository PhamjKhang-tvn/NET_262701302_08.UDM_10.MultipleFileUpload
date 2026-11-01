# Sản phẩm bàn giao: GUI Log Server

## Phần việc đã thực hiện

Mình đã xây dựng một ứng dụng có giao diện để bật, tắt máy chủ và theo dõi hoạt động qua nhật ký. Máy chủ sử dụng Kestrel, lắng nghe trên các giao diện mạng của máy tính để các thành viên trong cùng mạng LAN có thể kết nối. Cổng mặc định là `8080`; khi bật máy chủ, giao diện hiển thị địa chỉ IPv4 để chia sẻ với nhóm.

Ứng dụng có các chức năng kiểm tra phản hồi bình thường, dữ liệu không hợp lệ, mất kết nối và hai mức tải. Ngoài ra, nhóm có thể so sánh nội dung hai tệp và lưu nhật ký để làm bằng chứng.

## Giao diện

Phần trên cùng hiển thị tên ứng dụng, cổng, nút bật/tắt máy chủ, trạng thái hoạt động, số lượng yêu cầu đã nhận và địa chỉ IP để các thành viên khác kết nối.

Bên dưới là các nút kiểm thử: thành công, dữ liệu sai, mất kết nối, tải thấp và tải cao. Cùng khu vực này có nút so sánh hai tệp và nút lưu bằng chứng.

Phần còn lại hiển thị nhật ký hoạt động. Mỗi dòng có thời gian, mức độ như `PASS`, `WARN` hoặc `FAIL`, cùng nội dung kết quả để nhóm tiện theo dõi.

## Kế hoạch kiểm thử

1. Bật máy chủ và chạy **Test thành công**. Kết quả mong đợi: HTTP 200 và nội dung `hello`.
2. Chạy **Test dữ liệu sai** với nội dung rỗng hoặc dài hơn 4096 ký tự. Kết quả mong đợi: HTTP 400.
3. Chạy **Test mất kết nối** bằng cách ngắt kết nối khi nội dung POST chưa được gửi đầy đủ. Sau đó, máy chủ vẫn phải trả lời yêu cầu `/health`.
4. Chạy **Tải thấp (10)**: gửi 10 yêu cầu, tối đa 5 yêu cầu cùng lúc.
5. Chạy **Tải cao (100)**: gửi 100 yêu cầu, tối đa 25 yêu cầu cùng lúc.
6. Chạy **So sánh 2 file** với một cặp tệp có nội dung giống nhau và một cặp có nội dung khác nhau. Nhật ký sẽ cho biết kết quả so sánh bằng SHA-256.

## Ghi lại kết quả và phối hợp nhóm

Sau mỗi lượt kiểm thử, xem nhật ký để xác định kết quả `PASS` hoặc `FAIL`, rồi chọn **Lưu bằng chứng** để xuất nhật ký. Nếu gặp lỗi, ghi lại các bước thực hiện và chụp ảnh giao diện nếu cần. Nhóm nên thống nhất nơi lưu bằng chứng để mọi người cùng truy cập.

Mỗi thành viên nên kiểm tra `git diff --staged` trước khi commit phần việc của mình và dùng thông điệp commit ngắn gọn, mô tả đúng nội dung thay đổi.

Máy chủ hiện chưa có chức năng đăng nhập hoặc mã hóa, vì vậy chỉ nên sử dụng trong mạng tin cậy. Nếu Windows Firewall chặn kết nối, chỉ cho phép cổng ở hồ sơ mạng **Private**; không mở máy chủ ra mạng công cộng hoặc Internet.

## Tình trạng hiện tại

Ứng dụng đã build thành công. Các kiểm thử thành công, dữ liệu sai, mất kết nối và hai mức tải đã chạy đạt. Mình cũng đã gọi `/health` qua địa chỉ nội bộ và các địa chỉ IPv4 của máy để kiểm tra máy chủ. Nhóm vẫn cần thử kết nối từ một máy khác trong cùng mạng để xác nhận hoạt động thực tế. Chức năng so sánh tệp và lưu nhật ký đã có trên giao diện; nhóm có thể tự thao tác để lưu bằng chứng cho các chức năng này.