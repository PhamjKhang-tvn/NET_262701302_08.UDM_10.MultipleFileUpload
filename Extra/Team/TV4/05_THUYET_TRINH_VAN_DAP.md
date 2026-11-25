# TV4 Kịch bản demo và vấn đáp

Đây là nội dung chuẩn bị, chưa phải bằng chứng đã quay video hoặc đã vấn đáp. Lệnh chạy nằm ở Extra/Team/TV4/README.md.

## Kịch bản trình bày

1. Giới thiệu TcpListener, vòng accept và giới hạn ba phiên mặc định. Chỉ ra Start và StopAsync.
2. Dùng thư mục nhận sạch và dữ liệu thử tên mới, bật Headless bằng serve và dùng send gửi file thử.
3. Upload file, quan sát UPLOAD_START và UPLOAD_COMMIT; so sánh SHA-256 nguồn/đích.
4. Gửi lại tên cũ để thấy DUPLICATE và file cũ không thay đổi.
5. Chạy Test-TV4.ps1, giải thích TC_20 mất kết nối, TC_21 timeout, TC_25 cùng tên, TC_27 dừng và TC_28 dọn file tạm. Mở log thật, kể cả lỗi harness ban đầu.
6. Chỉ ra Log, Changed và UploadClientService; nêu GUI/LAN chưa được nghiệm thu trong lần kiểm tra module này.

## Câu hỏi luyện tập

**Vì sao cần file .part?** File nhận dở không xuất hiện dưới tên hoàn chỉnh. Lỗi thì dọn file tạm; chỉ đổi sang tên thật sau khi nhận và xác thực thành công.

**Vì sao một ReadAsync không đủ?** TCP trả về số byte hiện có, có thể ít hơn yêu cầu. Đọc prefix/body phải lặp đến đủ; EOF sớm là lỗi.

**Khi nào gửi COMPLETE?** Sau khi đủ payload, END/id/hash đúng, file đóng và File.Move thành công.

**Hai client cùng tên thì sao?** Một phiên giữ tên và nhận READY; phiên còn lại nhận DUPLICATE. File đã có cũng bị từ chối, không ghi đè.

**Tắt client A có ảnh hưởng B?** Task, socket và file tạm của mỗi phiên riêng biệt. CLIENT_ISOLATION kiểm tra B hoàn tất khi A đứt kết nối.

**Vì sao giới hạn phiên?** Để giới hạn socket, bộ nhớ, file handle và I/O. Hết slot trả BUSY.

**Dừng rồi bật lại?** Chờ StopAsync hoàn tất, tạo ServerEngine mới rồi Start. Không tái dùng instance đã dừng.

**Đã thử đĩa đầy chưa?** Chưa. Đã thử lỗi commit khi file đích xuất hiện sau READY, xác nhận không ghi đè, trả IO_ERROR và dọn file tạm.

**Phần nào mới bổ sung?** Trên nhánh cấu trúc chung: ServerEngine đặt trong ServerCore, harness TV4 bổ sung vào Tests qua tùy chọn --tv4, script Tools/Test-TV4.ps1, tài liệu và minh chứng tích hợp. Headless/GUI/harness chung được giữ theo nhánh nhóm; mã nhận file gốc đã có trong repo.

## Trước khi nộp

Nhóm cần ghép GUI đúng giao thức Shared, nhờ TV3 review, quay video cá nhân theo yêu cầu môn học và bổ sung commit/PR thật. Không đánh dấu các việc này hoàn tất chỉ vì harness PASS.
