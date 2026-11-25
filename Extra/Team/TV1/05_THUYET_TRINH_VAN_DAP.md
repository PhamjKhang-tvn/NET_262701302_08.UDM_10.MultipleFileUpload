# TV1 — Trình bày và vấn đáp

## Kịch bản trình bày phần nhóm trưởng (khoảng 2–3 phút, điều chỉnh theo buổi demo)
1. Giới thiệu mục tiêu UDM_10: upload nhiều tệp qua TCP, trạng thái/tiến độ/tốc độ riêng, xử lý lỗi.
2. Trình bày sáu project và luồng ClientForm -> Queue -> ClientService -> TCP -> ServerEngine -> ServerForm/log.
3. Giải thích frame điều khiển 4 byte length + JSON, payload N byte, SHA-256 và xác nhận COMPLETE. Nêu lý do đọc đủ byte.
4. Phân biệt queue 1 file/Client và 3 phiên Server mặc định, chính sách tên trùng, xử lý tệp dở.
5. Giới thiệu kết quả test/thực nghiệm thật, phân công và giới hạn; chuyển cho thành viên demo phần chuyên trách.

## Câu hỏi và ý trả lời
| Câu hỏi | Ý trả lời |
|---|---|
| TCP có giữ ranh giới message không? | Không. TCP là byte stream; Wire dùng prefix và ReadExactCore đọc đủ từng phần. |
| Vì sao không đọc cả file vào RAM? | Đọc/gửi khối 64 KiB hạn chế bộ nhớ phụ thuộc kích thước file; SHA-256 cập nhật theo khối. |
| READY khác COMPLETE? | READY cho phép bắt đầu gửi; COMPLETE xác nhận Server nhận đủ và xác minh/lưu thành công. |
| Gửi hết byte là thành công? | Chưa. Chờ COMPLETE hợp lệ; mất ACK cuối có thể file đã lưu (UNKNOWN_RESULT). |
| Một Client gửi 3 file cùng lúc? | Không; một file/Client theo queue. MaxSessions phía Server độc lập và mặc định 3. |
| Xử lý file trùng? | Từ chối, giữ dữ liệu cũ; giữ chỗ tên đang nhận và đổi tên không ghi đè. |
| Timeout? | Timed chạy đua với Task.Delay, đóng socket khi timeout/hủy; tránh chờ I/O vô hạn. |
| .NET 10 khác gì cấu hình cũ? | SDK-style; hai WinForms net10.0-windows, bốn project net10.0; global.json và dotnet build. |
| SHA-256 có mã hóa không? | Không; dùng đối chiếu nội dung. Không có xác thực/TLS, giới hạn mạng tin cậy. |
| Công việc thực tế TV1? | Trình bày file đã review/sửa, commit/PR, log build, ca test và minh chứng có thật. |

Chuẩn bị link/ảnh/video theo đề UDM_10 (khác bài Research). Không gán kết quả đạt hoặc việc của TV khác cho bản thân.
