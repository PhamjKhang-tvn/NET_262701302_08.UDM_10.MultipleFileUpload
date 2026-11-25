# Kịch bản demo và video nhóm

## Chuẩn bị

Chạy build và test trước, chọn thư mục Server mới dành riêng demo, tạo dữ liệu giả bằng 03_CREATE_DEMO_FILES.cmd, mở Client và Server thành hai cửa sổ/tiến trình. Ghi cấu hình máy, mạng, port, slot và giới hạn file. Không dùng file riêng tư. Kịch bản là đề xuất, không phải video đã được quay.

## Trình tự

**TV1 — Mở bài:** giới thiệu UDM_10, kiến trúc Client–Server và phạm vi. Công bố 1 file/Client, mặc định 3 phiên Server, không Pause/Resume, không TLS. Cho thấy IP/port cấu hình được.

**TV2 — GUI:** bật Server, kiểm tra kết nối, kéo 3 file vào Client. Chỉ từng cột: size, progress, tốc độ, status, lỗi. Nhấn tải. Thao tác di chuyển cửa sổ trong lúc truyền để kiểm tra độ phản hồi. Chụp màn hình thật cho TC_01–TC_04.

**TV3 — Truyền file:** giải thích WELCOME, UPLOAD, READY, payload, END, COMPLETE. Cho xem thư mục nhận và so SHA-256 bằng Get-FileHash. Nhấn mạnh progress 100% chưa đồng nghĩa ACK đã về.

**TV4 — Server:** mở log, chỉ ID phiên và SHA-256. Chuẩn bị một file đã tồn tại, gửi lại để xuất hiện DUPLICATE. Giải thích vì sao dữ liệu cũ được giữ và không đổi tên âm thầm.

**TV5 — Queue/lỗi:** chuẩn bị ba file theo thứ tự tên tốt A, tên đã tồn tại B, tên tốt C. Chứng minh B lỗi nhưng C vẫn tải. Dừng một lượt đang truyền để thấy file sau giữ Waiting. Không gọi thao tác này là Resume file.

**TV6 — Test:** trình bày kết quả thật trong workbook và log. Cho xem ít nhất một ca ngắt kết nối hoặc tắt Client giữa file lớn; Server không được crash, file chưa đủ không xuất hiện như file hoàn chỉnh. Trình bày hai mức tải và nói rõ phép đo nào loopback, phép đo nào LAN.

## Video

Mỗi người phải xuất hiện khuôn mặt và trực tiếp giải thích phần mình. Có âm thanh hoặc chú thích. Dán link chia sẻ thật vào README và báo cáo. Nhóm tự kiểm tra quyền xem bằng cửa sổ không đăng nhập. Không thay video bằng ảnh giao diện dựng hoặc log giả.
