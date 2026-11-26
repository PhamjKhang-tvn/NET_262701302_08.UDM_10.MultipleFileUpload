# TV5 - Nhiem vu day du theo bang phan cong

Tai lieu nay duoc trich tu bang phan cong 4 tuan cua nhom. KHONG chia theo chang. Nhiem vu duoc sap theo thu tu thuc hien va phai co minh chung that.

## 1. Cong viec
Tìm hiểu queue, async/await, trạng thái, công thức tiến độ/tốc độ và sự khác nhau giữa lỗi file với Server mất kết nối.

**San pham ban giao:** Một trang ghi chú + sơ đồ trạng thái + ví dụ 3 file.

**Dieu kien nghiem thu:** Giải thích được file tốt → file bị từ chối → file tốt; nếu Server tắt thì không hứa các file còn lại vẫn truyền được.

**Nguoi review:** TV2

## 2. Cong viec
Viết UploadItem và enum trạng thái; thử queue ba phần tử; viết hàm tính phần trăm và tốc độ từ byte/thời gian.

**San pham ban giao:** Các lớp trạng thái/queue + test logic nhỏ.

**Dieu kien nghiem thu:** Thứ tự Waiting → Uploading → Completed/Error hợp lý; có nhánh file 0 byte và thời gian đo bằng 0; mô phỏng chỉ dùng để test logic.

**Nguoi review:** TV2

## 3. Cong viec
Nối số byte đã gửi với progress/tốc độ; thêm trạng thái chờ xác nhận trong xử lý nội bộ, vẫn giữ trạng thái hiển thị phù hợp.

**San pham ban giao:** Bộ cập nhật progress và kết quả từng file.

**Dieu kien nghiem thu:** Đến 100% byte gửi không tự coi Completed; tốc độ có đơn vị/chu kỳ cập nhật; không chia cho 0.

**Nguoi review:** TV2

## 4. Cong viec
Hoàn thiện queue tuần tự: gọi gửi từng file; lỗi riêng một file thì ghi Error và chuyển file tiếp; mất Server thì giữ file chờ.

**San pham ban giao:** Queue hoàn chỉnh + chuyển trạng thái.

**Dieu kien nghiem thu:** Kịch bản tốt → trùng tên → tốt chạy đến cuối; Server tắt thì file đang tải báo lỗi, file chưa gửi vẫn Waiting để chạy tiếp khi Server trở lại.

**Nguoi review:** TV2

## 5. Cong viec
Hoàn thiện chuyển trạng thái khi lỗi file/mất Server; ngăn chạy trùng queue; reset tốc độ; hỗ trợ tiếp tục các file chưa gửi.

**San pham ban giao:** Queue có nhánh lỗi và test trạng thái.

**Dieu kien nghiem thu:** File lỗi riêng không chặn các file khác khi Server còn hoạt động; chạy lại hàng đợi không phải Resume từ byte cũ; không tự gửi lại file đã Completed.

**Nguoi review:** TV2

## 6. Cong viec
Test queue tốt–lỗi–tốt, nhiều file, Server mất rồi trở lại; kiểm tra giới hạn một file/Client và progress/tốc độ.

**San pham ban giao:** Kết quả test queue/trạng thái + các phép tính kiểm tra.

**Dieu kien nghiem thu:** Không mất các file Waiting; không upload lại file Completed; đã kiểm tra tốc độ/progress với trường hợp 0 byte và cuối file.

**Nguoi review:** TV2

## 7. Cong viec
Sửa lỗi queue/progress; viết sơ đồ trạng thái, công thức tốc độ, quy tắc xử lý lỗi và giới hạn sản phẩm.

**San pham ban giao:** Module queue đã sửa + báo cáo/slide phần điều phối.

**Dieu kien nghiem thu:** TV2 kiểm tra GUI cùng trạng thái; sơ đồ phản ánh code hiện tại, không mô tả Pause/Resume chưa làm.

**Nguoi review:** TV2

## 8. Cong viec
Rà queue/progress trên bản cuối; tổng hợp câu hỏi nền tảng và câu hỏi từng module; ghi hình phần hàng đợi có mặt; vấn đáp chéo TV2.

**San pham ban giao:** Bộ câu hỏi/đáp án theo code thật + phần trình bày TV5.

**Dieu kien nghiem thu:** Cả 6 người giải thích được Client–Server, TCP, IP/port, chia khối, xác nhận và mất kết nối; không học thuộc nội dung khác code.

**Nguoi review:** TV2

**Luu y:** Nhiem vu trong bang la phan cong du kien, KHONG phai bao cao da hoan thanh. TV1 la nhom truong, khong co folder code TV1 trong goi nay.
