# Sản phẩm bàn giao: GUI Log Server

## Phần việc đã thực hiện

Mình đã xây dựng một ứng dụng có giao diện để bật, tắt máy chủ và theo dõi hoạt động qua nhật ký. Máy chủ sử dụng Kestrel, lắng nghe trên các giao diện mạng của máy tính để các thành viên trong cùng mạng LAN có thể kết nối. Giao diện cho phép chọn cổng và thư mục nhận; khi bật máy chủ, địa chỉ IPv4 được hiển thị để chia sẻ với nhóm.

Các chức năng kiểm thử tuần trước vẫn được giữ lại: kiểm tra phản hồi bình thường, dữ liệu không hợp lệ, mất kết nối và hai mức tải. Tuần này mình bổ sung nhận file vào thư mục đã chọn, ghi log kết quả nhận, đối chiếu kích thước/SHA-256 giữa file nguồn và file đích, cùng hướng dẫn clone và build.

## Giao diện

Phần trên cùng hiển thị cổng, nút bật/tắt máy chủ, trạng thái hoạt động, số lượng yêu cầu và địa chỉ IP. Bên dưới có ô chọn thư mục nhận.

Bên dưới là các nút kiểm thử của tuần trước, nút **Đối chiếu nguồn–đích** và nút lưu bằng chứng. Nhật ký hiển thị các lần nhận file cùng kích thước và SHA-256.

Phần còn lại hiển thị nhật ký hoạt động. Mỗi dòng có thời gian, mức độ như `PASS`, `WARN` hoặc `FAIL`, cùng nội dung kết quả để nhóm tiện theo dõi.

## Kế hoạch kiểm thử

1. Bật máy chủ và chạy **Test thành công**. Kết quả mong đợi: HTTP 200 và nội dung `hello`.
2. Chạy **Test dữ liệu sai** với nội dung rỗng hoặc dài hơn 4096 ký tự. Kết quả mong đợi: HTTP 400.
3. Chạy **Test mất kết nối** bằng cách ngắt kết nối khi nội dung POST chưa được gửi đầy đủ. Sau đó, máy chủ vẫn phải trả lời yêu cầu `/health`.
4. Chạy **Tải thấp (10)**: gửi 10 yêu cầu, tối đa 5 yêu cầu cùng lúc.
5. Chạy **Tải cao (100)**: gửi 100 yêu cầu, tối đa 25 yêu cầu cùng lúc.
6. Chạy **So sánh 2 file** với một cặp tệp có nội dung giống nhau và một cặp có nội dung khác nhau. Nhật ký sẽ cho biết kết quả so sánh bằng SHA-256.
7. Gửi file bằng `POST /upload?filename=<tên-file>` với nội dung file ở dạng raw bytes. Server lưu file vào thư mục nhận và trả về kích thước cùng SHA-256. File trùng tên không bị ghi đè.
8. Chạy bộ kiểm tra tự động bằng `dotnet run --project .\tests\FileIntegrityChecks\FileIntegrityChecks.csproj`. Bộ kiểm tra gồm file text, ảnh PNG, file 0 byte, tên tiếng Việt, file lớn 64 MiB và các tình huống dữ liệu không khớp.

## Ghi lại kết quả và phối hợp nhóm

Sau mỗi lượt kiểm thử, xem nhật ký để xác định kết quả `PASS` hoặc `FAIL`, rồi chọn **Lưu bằng chứng** để xuất nhật ký. Nếu gặp lỗi, ghi lại các bước thực hiện và chụp ảnh giao diện nếu cần. Nhóm nên thống nhất nơi lưu bằng chứng để mọi người cùng truy cập.

Mỗi thành viên nên kiểm tra `git diff --staged` trước khi commit phần việc của mình và dùng thông điệp commit ngắn gọn, mô tả đúng nội dung thay đổi.

Listener TV4 chưa có trong repository tại thời điểm làm phần này nên chưa thể ghép và kiểm tra giao thức trực tiếp. Phần ghi file được tách thành `FileReceiveService` để listener có thể gọi khi sẵn sàng; README ghi rõ cách nối stream và độ dài file. Máy chủ hiện chưa có chức năng đăng nhập hoặc mã hóa, vì vậy chỉ nên sử dụng trong mạng tin cậy. Nếu Windows Firewall chặn kết nối, chỉ cho phép cổng ở hồ sơ mạng **Private**; không mở máy chủ ra mạng công cộng hoặc Internet.

## Tình trạng hiện tại

Ứng dụng và bộ kiểm tra file đã build/chạy thành công. Các case tuần trước vẫn được giữ nguyên và đã chạy lại đạt. Việc kết nối từ máy khác trong LAN và ghép thực tế với listener TV4 vẫn cần xác nhận khi có môi trường/mã TV4.
