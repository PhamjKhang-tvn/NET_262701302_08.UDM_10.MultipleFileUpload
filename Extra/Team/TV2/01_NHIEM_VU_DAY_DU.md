# TV2 - Nhiem vu day du theo bang phan cong

Tai lieu nay duoc trich tu bang phan cong 4 tuan cua nhom. KHONG chia theo chang. Nhiem vu duoc sap theo thu tu thuc hien va phai co minh chung that.

## 1. Cong viec
Tìm tài liệu WinForms, chọn file và kéo thả; phác thảo GUI Client theo đúng dữ liệu mỗi file cần hiển thị.

**San pham ban giao:** Một trang ghi chú + nguồn chính thức + bản phác GUI.

**Dieu kien nghiem thu:** Bản phác có IP/port, chọn/kéo thả nhiều file, trạng thái, tiến độ, tốc độ và lỗi; trình bày lại bằng lời của mình.

**Nguoi review:** TV5

## 2. Cong viec
Dựng form Client: IP/port, chọn/kéo thả file, bảng file. Tạm dùng dữ liệu giả chỉ để kiểm tra bố cục.

**San pham ban giao:** Form Client chạy được + ảnh giao diện.

**Dieu kien nghiem thu:** Kéo thả 3 file thật thấy đúng tên/kích thước; chặn thư mục nếu chưa hỗ trợ; không gọi dữ liệu giả là upload thành công.

**Nguoi review:** TV5

## 3. Cong viec
Nối nút upload một file với dịch vụ TV3 và dữ liệu TV5; hiển thị tiến độ/trạng thái/lỗi trên GUI.

**San pham ban giao:** GUI thực hiện upload thật một file.

**Dieu kien nghiem thu:** File được truyền qua TCP; form vẫn kéo được/nhận thao tác; không đổi thành Completed trước xác nhận Server.

**Nguoi review:** TV5

## 4. Cong viec
Hoàn thiện bảng nhiều file; mỗi dòng có tên, kích thước, trạng thái, progress, tốc độ và lỗi; khóa thao tác không hợp lệ.

**San pham ban giao:** GUI Client dùng được với danh sách nhiều file.

**Dieu kien nghiem thu:** Kéo thả nhiều file không mất dòng; progress không chạy sang file khác; nút Upload không khởi tạo hai lượt trùng nhau.

**Nguoi review:** TV5

## 5. Cong viec
Kiểm tra dữ liệu nhập và thông báo lỗi; cập nhật control đúng luồng; xử lý đóng form trong lúc upload.

**San pham ban giao:** GUI không treo; lỗi có thể hiểu và thao tác lại.

**Dieu kien nghiem thu:** IP/port sai, file mất, Server chưa bật đều không làm ứng dụng crash; không truy cập control từ luồng không phù hợp.

**Nguoi review:** TV5

## 6. Cong viec
Test GUI/chọn-kéo thả/input sai/đóng form; chạy Client trong buổi đo tải và lưu ảnh thực tế.

**San pham ban giao:** Kết quả test GUI + ảnh + lỗi có bước tái hiện.

**Dieu kien nghiem thu:** Hiển thị đúng từng file; form không treo ở kịch bản công bố; có ít nhất test file 0 byte và tên tiếng Việt.

**Nguoi review:** TV5

## 7. Cong viec
Sửa lỗi GUI được giao; viết phần thiết kế GUI/hướng dẫn thao tác; chuẩn bị slide và ảnh demo.

**San pham ban giao:** Module GUI đã sửa + nội dung báo cáo/slide riêng.

**Dieu kien nghiem thu:** Các test GUI lỗi đã được chạy lại; ảnh từ đúng bản ứng viên nộp; người khác làm theo hướng dẫn được.

**Nguoi review:** TV5

## 8. Cong viec
Ghép slide cuối và các đoạn video của 6 người; ghi hình phần GUI có khuôn mặt; kiểm tra bố cục và thao tác demo.

**San pham ban giao:** PPTX cuối + video ghép đủ thành viên + phần trình bày TV2.

**Dieu kien nghiem thu:** Video giải thích thao tác có âm thanh/chú thích; đủ 6 người trình bày phần riêng có mặt; không mất cảnh demo lỗi; không cần dựng hiệu ứng cầu kỳ.

**Nguoi review:** TV5

**Luu y:** Nhiem vu trong bang la phan cong du kien, KHONG phai bao cao da hoan thanh. TV1 la nhom truong, khong co folder code TV1 trong goi nay.
