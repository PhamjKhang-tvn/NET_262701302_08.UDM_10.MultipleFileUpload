# TV5 - Assigned test cases (teacher-format workbook)

These IDs refer to `Extra/TestCases/UDM10_Test_Cases.xlsx` in the shared project. Do not confuse them with similarly named rows in the earlier 22-case planning sheet.

Status in the source workbook is **NOT TEST YET**. No tests are claimed to have passed.

## TC_04 - Hai GUI — Upload và tiến độ riêng
**Goal:** Kiểm tra luồng thao tác sản phẩm, không chỉ API.
**Input / steps:** Bật Server GUI; chọn ba file khác kích thước; Upload. Quan sát từng dòng, progress, tốc độ, log; so sánh file nguồn–đích.
**Expected:** Mỗi Client chỉ gửi một file/lần; Waiting → Uploading → Completed hoặc Error. Từng file có progress/tốc độ riêng; Completed sau xác nhận Server.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_10 - File rỗng 0 byte
**Goal:** Không treo vòng đọc khi file không có payload.
**Input / steps:** Harness tạo file 0 byte và upload; so hash file rỗng.
**Expected:** Hoàn tất đúng; file đích 0 byte; hash đúng; không chia cho 0 trong progress.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_13 - Queue tốt → lỗi → tốt
**Goal:** Lỗi riêng một file không dừng cả batch.
**Input / steps:** Harness chạy queue gồm file mới, tên trùng trên Server, rồi một file mới khác.
**Expected:** Hai file mới Completed; file giữa Error; queue không dừng vì DUPLICATE.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_14 - File nguồn không tồn tại
**Goal:** Cô lập lỗi cục bộ trước kết nối.
**Input / steps:** Queue gồm đường dẫn không có thật rồi một file hợp lệ.
**Expected:** File đầu LOCAL_FILE/Error; file sau vẫn upload thành công; không crash queue.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_26 - Server offline — giữ file chờ
**Goal:** Không đánh lỗi hàng loạt khi hạ tầng không khả dụng.
**Input / steps:** Queue nhiều file tới port không lắng nghe.
**Expected:** File thử đầu báo mất kết nối; file chưa thử còn Waiting; không tự đánh Completed.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_30 - Progress đơn điệu và ACK
**Goal:** Phân biệt gửi đủ byte và hoàn tất nghiệp vụ.
**Input / steps:** Thu thập callback một lần upload; kiểm tra số byte tăng không giảm và số lần Completed.
**Expected:** Byte tiến độ không giảm; duy nhất một Completed sau xác nhận; không vượt kích thước file.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## How to record evidence
Record operating system, build revision, test data, date/time, actual result, and evidence location.
If test fails, leave FAIL in the history and write what was fixed before a new run.
Cross-review findings with assigned module owner; TV6 assembles final results.
