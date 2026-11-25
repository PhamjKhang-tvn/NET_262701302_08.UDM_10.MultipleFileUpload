# TV3 - Danh sách Test Cases (Theo giáo trình)

*Ghi chú: Trạng thái mặc định là NOT TEST YET. Sinh viên phải chạy thử và điền kết quả thực tế.*

## TC_09 - Upload file nhị phân và kiểm tra SHA-256
- **Mục tiêu:** Đảm bảo file gửi đi không bị lỗi byte nào.
- **Cách làm:** Gửi file ảnh hoặc file nén 1MB. Kiểm tra mã SHA-256 ở Client và Server.
- **Kết quả mong đợi:** Mã SHA-256 khớp nhau. File nhận được mở được bình thường.

## TC_20 - Client ngắt kết nối giữa chừng
- **Mục tiêu:** Server không lưu file hỏng.
- **Cách làm:** Đang gửi file (được 50%) thì tắt Client đột ngột.
- **Kết quả mong đợi:** Server báo lỗi, file tạm (.tmp) bị xóa, không có file rác trong thư mục nhận.

## TC_21 - Timeout khi không có dữ liệu
- **Mục tiêu:** Đảm bảo Client không chờ vô hạn.
- **Cách làm:** Bật Server nhưng chặn không cho gửi dữ liệu (hoặc tắt Server khi đang chờ).
- **Kết quả mong đợi:** Sau 30s, Client báo lỗi `TimeoutException` và dừng lại.

## TC_35 - Mất xác nhận cuối (UNKNOWN_RESULT)
- **Mục tiêu:** Không báo thành công sai.
- **Cách làm:** Server nhận xong file nhưng ngắt kết nối trước khi gửi `COMPLETE` về Client.
- **Kết quả mong đợi:** Client báo lỗi `UNKNOWN_RESULT`, khuyên người dùng kiểm tra Server trước khi gửi lại.

*(Các test case khác: TC_30, TC_34, TC_36 làm tương tự)*