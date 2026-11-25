# TV2 - Assigned test cases (teacher-format workbook)

These IDs refer to `Extra/TestCases/UDM10_Test_Cases.xlsx` in the shared project. Do not confuse them with similarly named rows in the earlier 22-case planning sheet.

Status in the source workbook is **NOT TEST YET**. No tests are claimed to have passed.

## TC_01 - Client — Kéo thả một file
**Goal:** Thêm một file hợp lệ vào bảng GUI.
**Input / steps:** Mở Client; kéo test.txt từ Explorer vào vùng thả file. Chưa nhấn Upload.
**Expected:** Một dòng xuất hiện, tên/kích thước đúng; trạng thái Waiting; không truyền dữ liệu trước lệnh Upload.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_02 - Client — Kéo thả nhiều file
**Goal:** Thêm nhiều file và từ chối thư mục.
**Input / steps:** Kéo ba file và một thư mục vào Client; thử thêm lại một đường dẫn đã có.
**Expected:** Các file hợp lệ được thêm; thư mục bị bỏ qua/cảnh báo; không tạo dòng trùng đường dẫn; GUI vẫn phản hồi.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_03 - Client — Kiểm tra cấu hình
**Goal:** IP/port/timeout phải được kiểm tra.
**Input / steps:** Nhập IP abc, sửa về 127.0.0.1; kiểm tra giới hạn các ô số port và timeout; nhấn kiểm tra Server.
**Expected:** IP sai có thông báo, không crash; các ô số không chấp nhận giá trị ngoài giới hạn; cấu hình hợp lệ dùng được.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_04 - Hai GUI — Upload và tiến độ riêng
**Goal:** Kiểm tra luồng thao tác sản phẩm, không chỉ API.
**Input / steps:** Bật Server GUI; chọn ba file khác kích thước; Upload. Quan sát từng dòng, progress, tốc độ, log; so sánh file nguồn–đích.
**Expected:** Mỗi Client chỉ gửi một file/lần; Waiting → Uploading → Completed hoặc Error. Từng file có progress/tốc độ riêng; Completed sau xác nhận Server.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_05 - Client — GUI không bị treo / hủy
**Goal:** UI đáp ứng trong lúc truyền và hủy hàng đợi.
**Input / steps:** Gửi file lớn; kéo cửa sổ, đổi kích thước rồi nhấn Hủy. Sau khi dừng, kiểm tra các dòng còn chờ và Server.
**Expected:** GUI tiếp tục phản hồi; file đang gửi báo lỗi/hủy hoặc chưa rõ nếu mất ACK cuối; các file chưa gửi giữ Waiting; không công nhận file dở là hoàn tất.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_07 - Hai máy LAN — Kết nối thật
**Goal:** Kiểm thử trên hai máy Windows thật; không thay bằng loopback.
**Input / steps:** Cùng LAN tin cậy. Server chọn IP LAN hoặc 0.0.0.0. Client nhập IPv4 LAN/port. Chỉ cho phép app qua Firewall mạng Private nếu cần. Upload file tổng hợp.
**Expected:** Truyền file giữa hai máy; hash nguồn/đích giống; ghi IP, OS, cấu hình máy, thời gian, log và ảnh thật. Không tắt toàn bộ Firewall.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## How to record evidence
Record operating system, build revision, test data, date/time, actual result, and evidence location.
If test fails, leave FAIL in the history and write what was fixed before a new run.
Cross-review findings with assigned module owner; TV6 assembles final results.
