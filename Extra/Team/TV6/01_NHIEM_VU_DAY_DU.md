# TV6 - Nhiem vu day du theo bang phan cong

Tai lieu nay duoc trich tu bang phan cong 4 tuan cua nhom. KHONG chia theo chang. Nhiem vu duoc sap theo thu tu thuc hien va phai co minh chung that.

## 1. Cong viec
Tìm hiểu Git/commit/review; test chức năng, mất kết nối, so sánh file và đo tải; phác GUI/log Server.

**San pham ban giao:** Một trang ghi chú + kế hoạch test sơ bộ + phác GUI Server.

**Dieu kien nghiem thu:** Có test thành công, dữ liệu sai, mất kết nối, hai mức tải; cả nhóm hiểu cách ghi bằng chứng và commit cá nhân.

**Nguoi review:** TV1

## 2. Cong viec
Dựng GUI Server với port, thư mục nhận, Start/Stop và log; nối với listener TV4 khi sẵn sàng; viết hướng dẫn clone/build.

**San pham ban giao:** Form Server + log mẫu + checklist môi trường 6 máy.

**Dieu kien nghiem thu:** Hiện Start/Stop/connect/disconnect; mỗi thành viên tự clone/build/chạy Hello và commit bằng tài khoản riêng.

**Nguoi review:** TV1

## 3. Cong viec
Viết tiện ích C# đối chiếu kích thước/SHA-256 nguồn–đích; kiểm thử file text, ảnh, 0 byte, tên tiếng Việt và file lớn.

**San pham ban giao:** Tiện ích kiểm tra + bộ file test + kết quả thực tế.

**Dieu kien nghiem thu:** File nhận đúng nội dung; có bằng chứng cho lỗi phát hiện; log Server có thời gian, tên file và kết quả.

**Nguoi review:** TV1

## 4. Cong viec
Nối đầy đủ log vào GUI Server và file log; test 10 file, hai Client và đối chiếu nguồn–đích.

**San pham ban giao:** Log Server hoàn chỉnh + biên bản nghiệm thu tuần 2.

**Dieu kien nghiem thu:** Ghi start/connect/disconnect/upload/success/error có timestamp; không cần ghi từng byte; tất cả file thành công đối chiếu đúng.

**Nguoi review:** TV1

## 5. Cong viec
Bổ sung công cụ tạo tình huống Client ngắt giữa chừng, metadata sai hoặc peer im lặng; ghi lỗi có bước tái hiện.

**San pham ban giao:** Bộ kịch bản lỗi + log + danh sách lỗi cho từng chủ module.

**Dieu kien nghiem thu:** Phân biệt lỗi do thử nghiệm có chủ đích với lỗi chương trình; dữ liệu test giả lập, không dùng file bí mật; nhận thấy tài nguyên được giải phóng.

**Nguoi review:** TV1

## 6. Cong viec
Điều phối test hiệu năng ở 2 mức tải; thêm tải vượt giới hạn Server; tổng hợp thời gian, throughput, tỷ lệ lỗi, CPU/RAM và bằng chứng.

**San pham ban giao:** Bảng đo thật + cấu hình máy + dataset + log/ảnh + nhận xét.

**Dieu kien nghiem thu:** Mức A: 1 Client/10 file × 10 MiB; mức B: 3 Client, mỗi Client 20 file × 10 MiB. Stress: 6 Client so với giới hạn 3 phiên; ghi BUSY riêng với lỗi ngoài dự kiến.

**Nguoi review:** TV1

## 7. Cong viec
Chạy hồi quy trên bản sửa; tổng hợp kết quả/hạn chế, viết phần kiểm thử và kịch bản quay demo thành công + lỗi.

**San pham ban giao:** Báo cáo test cuối + nội dung báo cáo/slide + kịch bản demo.

**Dieu kien nghiem thu:** Số liệu có nguồn log; các lỗi đã sửa được test lại; test thất bại/chưa chạy không được đổi thành đạt để đẹp báo cáo.

**Nguoi review:** TV1

## 8. Cong viec
Nghiệm thu bản cuối bằng kịch bản thành công/lỗi; kiểm tra số liệu, ảnh và quyền truy cập link video; ghi hình kiểm thử có mặt; vấn đáp chéo TV1.

**San pham ban giao:** Biên bản nghiệm thu cuối + phần trình bày TV6.

**Dieu kien nghiem thu:** Bằng chứng cùng phiên bản nộp; không còn lỗi gây crash/sai file/mất dữ liệu trong kịch bản công bố; link mở được từ tài khoản khác có quyền.

**Nguoi review:** TV1

**Luu y:** Nhiem vu trong bang la phan cong du kien, KHONG phai bao cao da hoan thanh. TV1 la nhom truong, khong co folder code TV1 trong goi nay.
