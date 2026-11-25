# TV4 - Assigned test cases (teacher-format workbook)

These IDs refer to `Extra/TestCases/UDM10_Test_Cases.xlsx` in the shared project. Do not confuse them with similarly named rows in the earlier 22-case planning sheet.

Các ID dưới đây lấy từ gói TV4; workbook gốc được mô tả chưa có trong repo hiện tại. Harness chạy thực tế lúc 22:37 ngày 25/11/2026 (+07:00): 15/15 ID được giao PASS, toàn bộ harness 21/21 PASS. Xem MinhChung/20261125-lan3.txt. TC_21 từng FAIL ở harness, giữ lịch sử tại MinhChung/20261125-lan1.txt.

## TC_12 - Tên trùng — không ghi đè
**Goal:** Thực thi chính sách từ chối tên đã tồn tại.
**Input / steps:** Gửi file A; sửa nội dung nguồn cùng tên rồi gửi lại; đọc file đích.
**Expected:** Lần sau trả DUPLICATE; nội dung file cũ nguyên vẹn; không có ghi đè im lặng.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_15 - Giới hạn kích thước file
**Goal:** Từ chối file vượt mức cấu hình.
**Input / steps:** Server đặt giới hạn nhỏ; Client gửi file lớn hơn giới hạn.
**Expected:** TOO_LARGE; không tạo file hoàn chỉnh; Server vẫn hoạt động.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_16 - Tên nguy hiểm / thiết bị Windows
**Goal:** Chặn đường dẫn thoát thư mục và tên cấm.
**Input / steps:** Harness gửi metadata tên ../outside.txt, đường dẫn tuyệt đối và tên thiết bị dành riêng.
**Expected:** INVALID_NAME; không tạo file ngoài thư mục được chọn; không ghi vào thiết bị Windows.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_17 - JSON không hợp lệ
**Goal:** Giới hạn ảnh hưởng gói điều khiển sai.
**Input / steps:** Harness gửi frame có độ dài hợp lệ nhưng body JSON lỗi.
**Expected:** Phiên bị từ chối/đóng có log; không làm sập Server; phiên hợp lệ sau đó vẫn dùng được.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_18 - Độ dài frame vượt giới hạn
**Goal:** Không cấp phát bộ nhớ theo header bất kỳ.
**Input / steps:** Gửi prefix khai báo body lớn hơn 16 KiB.
**Expected:** BAD_LENGTH/đóng kết nối; không đọc/cấp phát theo độ dài quá mức.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_19 - Kích thước âm
**Goal:** Kiểm tra metadata size trước nhận dữ liệu.
**Input / steps:** Gửi UPLOAD với size=-1.
**Expected:** BAD_SIZE; không trả READY cho dữ liệu không hợp lệ; không tạo file đích.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_20 - Client đứt kết nối giữa payload
**Goal:** Nhận thiếu không được công nhận hoàn tất.
**Input / steps:** Khai báo file lớn, chỉ gửi một phần rồi đóng socket. Chờ server xử lý.
**Expected:** Không có file hoàn chỉnh; file tạm phiên được xóa; slot và tên dự trữ được giải phóng.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_21 - Timeout khi không gửi thêm dữ liệu
**Goal:** Chờ mạng có giới hạn.
**Input / steps:** Server timeout ngắn; nhận READY nhưng peer không gửi payload; theo dõi kết thúc phiên.
**Expected:** Phiên timeout/đóng; file dở không được lưu cuối; Server có thể phục vụ phiên khác.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_22 - SHA-256 không khớp
**Goal:** Không commit dữ liệu khi trailer hash sai.
**Input / steps:** Gửi đủ N byte rồi END với SHA-256 sai.
**Expected:** HASH_MISMATCH; không có file cuối; log lỗi và dọn file tạm.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_23 - Ba Client đồng thời
**Goal:** Các phiên độc lập trên cùng Server.
**Input / steps:** Server giới hạn ba; harness bắt đầu ba upload tên khác nhau.
**Expected:** Ba file hoàn tất và hash đúng; không lẫn dữ liệu hoặc vượt giới hạn phiên.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_24 - Vượt giới hạn phiên — BUSY
**Goal:** Từ chối có kiểm soát thay vì mở vô hạn.
**Input / steps:** Giữ một phiên trên Server giới hạn một; mở thêm Client.
**Expected:** Client thêm nhận BUSY/đóng có giới hạn; phiên đã nhận không bị phá; giải phóng slot sau khi đóng.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_25 - Hai phiên cùng tên đang truyền
**Goal:** Ngăn race ghi cùng tên.
**Input / steps:** Phiên A nhận READY và giữ tên; B xin cùng tên; đóng A rồi gửi lại.
**Expected:** B ban đầu bị DUPLICATE; sau khi A kết thúc lỗi và giải phóng tên, phiên mới có thể gửi.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_27 - Stop Server khi đang upload
**Goal:** Giải phóng tài nguyên khi dừng.
**Input / steps:** Bắt đầu nhận một phần file rồi gọi StopAsync trên Server.
**Expected:** Đóng listener và phiên đang chạy; file dở được dọn; không còn giữ khóa thư mục.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_28 - Dọn file tạm sau crash
**Goal:** Chỉ dọn file do ứng dụng sở hữu.
**Input / steps:** Tạo file tạm đúng mẫu udm10-GUID.part và một file không đúng mẫu trong thư mục tạm; khởi động Server.
**Expected:** Chỉ file tạm đúng mẫu được dọn; file khác không bị xóa.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## TC_29 - Hai Server cùng thư mục
**Goal:** Khóa thư mục chống hai tiến trình ghi chồng.
**Input / steps:** Bật hai Server cùng thư mục lưu; Server thứ hai dùng port khác.
**Expected:** Server thứ hai bị từ chối khóa thư mục; Server đầu hoạt động bình thường.
**Actual:** Harness thực hiện ca tương ứng, các assertion đạt như Expected trong lượt 22:37 ngày 25/11/2026 (+07:00).
**Result:** PASS (TCP loopback, module test)
**Proof:** MinhChung/20261125-lan3.txt và log/file thử tại BuildLogs/TV4/20261125-223707-9f7839e6.

## How to record evidence
Record operating system, build revision, test data, date/time, actual result, and evidence location.
If test fails, leave FAIL in the history and write what was fixed before a new run.
Cross-review findings with assigned module owner; TV6 assembles final results.
