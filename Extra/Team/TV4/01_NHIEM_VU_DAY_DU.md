# TV4 - Nhiem vu day du theo bang phan cong

Tai lieu nay duoc trich tu bang phan cong 4 tuan cua nhom. KHONG chia theo chang. Nhiem vu duoc sap theo thu tu thuc hien va phai co minh chung that.

## 1. Cong viec
Tìm hiểu TcpListener, FileStream, nhận đủ byte, lưu tạm và xử lý tên trùng/tên không hợp lệ.

**San pham ban giao:** Một trang ghi chú + sơ đồ Server + nguồn chính thức.

**Dieu kien nghiem thu:** Mô tả được việc nhận thiếu dữ liệu, từ chối file trùng tên và không ghi ra ngoài thư mục nhận.

**Nguoi review:** TV3

## 2. Cong viec
Viết bộ lắng nghe Server, nhận HELLO và trả OK; tách xử lý kết nối khỏi vòng chờ Client mới.

**San pham ban giao:** Demo TCP Server + điểm gọi Start/Stop cho GUI.

**Dieu kien nghiem thu:** Server nhận liên tiếp các kết nối; đóng một Client không làm Server dừng; có Start/Stop rõ ràng.

**Nguoi review:** TV3

## 3. Cong viec
Nhận/kiểm tra metadata, gửi READY, đọc đúng kích thước và ghi file tạm; hoàn tất/đóng file rồi đổi tên và gửi SUCCESS.

**San pham ban giao:** Dịch vụ nhận/lưu một file; quy tắc từ chối tên trùng.

**Dieu kien nghiem thu:** File chưa đủ byte không mang tên hoàn chỉnh; tên trùng không ghi đè; nhận thiếu hoặc ghi lỗi không gửi SUCCESS.

**Nguoi review:** TV3

## 4. Cong viec
Cô lập lỗi từng phiên; hỗ trợ nhiều Client trong giới hạn; trả BUSY rõ khi hết chỗ; tránh ghi đè khi hai Client gửi cùng tên.

**San pham ban giao:** Server nhiều phiên có giới hạn và xử lý trùng tên an toàn.

**Dieu kien nghiem thu:** Hai Client truyền được; một Client lỗi không làm phiên khác dừng; file đích cũ được giữ nguyên.

**Nguoi review:** TV3

## 5. Cong viec
Kiểm tra tên file/đường dẫn/size/message; xử lý ghi đĩa lỗi, thiếu byte, Client rời mạng; dọn file tạm theo chính sách công bố.

**San pham ban giao:** Server validation + cleanup + lỗi rõ ràng.

**Dieu kien nghiem thu:** Không ghi ngoài thư mục nhận; không overwrite; file thiếu không được công nhận; một phiên lỗi không làm Server/phiên khác dừng.

**Nguoi review:** TV3

## 6. Cong viec
Test input độc hại ở mức dữ liệu giả lập, trùng tên từ hai Client, lỗi lưu và Client rời; theo dõi Server khi đo tải.

**San pham ban giao:** Kết quả test Server + bằng chứng cô lập lỗi.

**Dieu kien nghiem thu:** File cũ không thay đổi; đường dẫn bị chặn; tắt Client A khi B đang tải không làm B dừng; Server không crash.

**Nguoi review:** TV3

## 7. Cong viec
Sửa lỗi Server; viết phần kiểm tra input, file tạm, tên trùng, nhiều Client và giải phóng tài nguyên.

**San pham ban giao:** Module Server đã sửa + báo cáo/slide phần nhận file.

**Dieu kien nghiem thu:** TV3 kiểm tra tương thích; test file thiếu/trùng tên/cô lập lỗi chạy lại đạt.

**Nguoi review:** TV3

## 8. Cong viec
Chuẩn bị Server/thư mục demo và dữ liệu giả lập; thử Start/Stop/khởi động lại; ghi hình phần nhận file có mặt; vấn đáp chéo TV3.

**San pham ban giao:** Môi trường Server demo + phần trình bày TV4.

**Dieu kien nghiem thu:** Demo chạy lại được từ trạng thái sạch; không dùng file cá nhân nhạy cảm; giải thích file tạm, tên trùng và cô lập Client lỗi.

**Nguoi review:** TV3

**Luu y:** Nhiem vu trong bang la phan cong du kien, KHONG phai bao cao da hoan thanh. Gói TV4 riêng không chứa project chung; nhánh refactor/project-structure đặt project chung trực tiếp trong Code/. Trạng thái thực hiện nằm ở 06_NHAT_KY_MINH_CHUNG.md.
