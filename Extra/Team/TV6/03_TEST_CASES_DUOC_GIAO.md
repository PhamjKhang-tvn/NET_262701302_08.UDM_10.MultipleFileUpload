# TV6 - Assigned test cases (teacher-format workbook)

These IDs refer to `Extra/TestCases/UDM10_Test_Cases.xlsx` in the shared project. Do not confuse them with similarly named rows in the earlier 22-case planning sheet.

Status in the source workbook is **NOT TEST YET**. No tests are claimed to have passed.

## TC_06 - Server GUI — Start/Stop và log
**Goal:** Kiểm tra cấu hình, lịch sử phiên và thư mục log.
**Input / steps:** Đổi thư mục/port; Start; Client thăm dò và gửi file; Stop rồi Start lại. Mở log tại thư mục lưu.
**Expected:** Trạng thái Server đúng; hiện peer/file/kết quả; log có thời gian/connect/disconnect/thao tác/lỗi; có thể đổi cấu hình khi đã dừng.
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

## TC_08 - Hai Client GUI — Cô lập lỗi
**Goal:** Demo một Client lỗi không làm Client khác dừng.
**Input / steps:** Hai Client gửi file lớn tới Server có giới hạn >=2. Đóng cưỡng bức Client A giữa chừng; giữ B chạy. Kiểm tra file và log.
**Expected:** A được ghi nhận lỗi, file tạm dọn; B vẫn hoàn tất nếu mạng Server còn tốt; Server tiếp tục nhận Client mới.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_09 - Upload nhị phân và SHA-256
**Goal:** Toàn vẹn dữ liệu một file nhị phân.
**Input / steps:** Harness sinh file 1 MiB, gửi qua loopback bằng dịch vụ sản phẩm, đọc file đích và so SHA-256.
**Expected:** ACK COMPLETE hợp lệ; kích thước và SHA-256 khớp; không còn file tạm của phiên.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_23 - Ba Client đồng thời
**Goal:** Các phiên độc lập trên cùng Server.
**Input / steps:** Server giới hạn ba; harness bắt đầu ba upload tên khác nhau.
**Expected:** Ba file hoàn tất và hash đúng; không lẫn dữ liệu hoặc vượt giới hạn phiên.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_24 - Vượt giới hạn phiên — BUSY
**Goal:** Từ chối có kiểm soát thay vì mở vô hạn.
**Input / steps:** Giữ một phiên trên Server giới hạn một; mở thêm Client.
**Expected:** Client thêm nhận BUSY/đóng có giới hạn; phiên đã nhận không bị phá; giải phóng slot sau khi đóng.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_31 - Hiệu năng — tải mức 1
**Goal:** Đo mức một Client bằng số liệu thực.
**Input / steps:** Harness loopback: 1 Client, 6 file x 1 MiB. Đo thời gian, byte, lỗi, throughput và bộ nhớ tiến trình.
**Expected:** Có số liệu thật trong results.json; ghi rõ loopback và môi trường. Không áp ngưỡng tốc độ bịa; file toàn vẹn.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_32 - Hiệu năng — tải mức 2
**Goal:** Đo và so sánh tải ba Client.
**Input / steps:** Harness loopback: 3 Client, mỗi Client 6 file x 1 MiB; thu cùng nhóm chỉ số như TC_31.
**Expected:** Có số liệu thật để so với mức 1; báo số lỗi/throughput/bộ nhớ. Không kết luận hiệu năng LAN từ loopback.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_33 - Stress — từ chối tải vượt giới hạn
**Goal:** Đánh giá giới hạn và phục hồi sau tải vượt mức.
**Input / steps:** Giữ ba slot, mở thêm 12 kết nối; đóng các phiên giữ rồi gửi một file hợp lệ.
**Expected:** Kết nối vượt mức bị BUSY; không crash; upload sau đó thành công khi có slot.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## TC_36 - Client–Server hai tiến trình
**Goal:** Chạy Server phụ ở tiến trình OS riêng.
**Input / steps:** Harness bật Udm10.Headless.exe, đọc port từ ready-file, upload qua loopback, so hash và gửi STOP.
**Expected:** PID Client/Server khác nhau; dữ liệu đúng; tiến trình con dừng. Không thay thế test LAN hay GUI thủ công.
**Actual:** [fill after a real test]
**Result:** NOT TEST YET
**Proof:** [screenshot/log path and timestamp]

## How to record evidence
Record operating system, build revision, test data, date/time, actual result, and evidence location.
If test fails, leave FAIL in the history and write what was fixed before a new run.
Cross-review findings with assigned module owner; TV6 assembles final results.
