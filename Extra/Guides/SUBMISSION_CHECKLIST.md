# Checklist trước khi nộp

## Phần phải làm trên máy thật

- [ ] Biên dịch thành công cả sáu project; lưu build-status.json và log.
- [ ] Chạy 28 test tự động; điều tra mọi FAILED, không xóa kết quả lỗi để đẹp báo cáo.
- [ ] Thực hiện TC_01–TC_08 trên Windows, trong đó có hai máy LAN nếu nhóm tuyên bố hỗ trợ LAN.
- [ ] So sánh file nguồn/đích và SHA-256; chụp GUI, log, thao tác lỗi.
- [ ] Đo hai mức tải, ghi cấu hình, dữ liệu, số Client và số liệu thật.
- [ ] Đối chiếu General Requirements và Project Details!C78; rà mọi chức năng bắt buộc.

## Hồ sơ và người thực hiện

- [ ] Điền môn/lớp/giảng viên, mã nhóm, họ tên và MSSV 6 thành viên.
- [ ] Bảng công việc phản ánh đóng góp thực tế, không chỉ phần giao việc đề xuất.
- [ ] Báo cáo DOCX tối đa 15 trang, đúng định dạng trường sau khi đối chiếu mẫu thật nếu có. Gói hiện chưa được cung cấp mẫu Word của trường.
- [ ] Chèn số liệu và ảnh thực tế, chỉnh các câu trạng thái “chưa chạy” đúng kết quả đã nghiệm thu.
- [ ] Slide PPTX khớp báo cáo; không ghi PASSED khi chưa có log.
- [ ] GitHub thật, mỗi người dùng tài khoản riêng và commit mô tả đúng thay đổi. Không tạo commit lùi ngày hoặc mạo danh.
- [ ] Video thật có cả sáu người trình bày/khuôn mặt, âm thanh/chú thích và link ở README + báo cáo.
- [ ] Kiểm tra hạn nộp trên hệ thống môn; TV1 tự đặt hạn nội bộ.

## Đóng gói

Thư mục nộp phải có Code, DOCX, PPTX, Extra, README.md và .gitignore. Loại bỏ Build, bin, obj, .vs, cache, secret và dữ liệu upload không cần thiết. Giữ bằng chứng kiểm thử đã chọn trong Extra.

Tên cuối: `CourseCode-GroupCode-UDM_10.7z` với CourseCode/GroupCode thay bằng mã thật. Chạy Tools/Package-Submission.ps1 khi đã có 7-Zip, hoặc tự tạo bằng giao diện 7-Zip. Không đổi đuôi ZIP thành 7z.

## Trạng thái gói nguồn ban đầu

Chưa có executable đã biên dịch; chưa chạy C# test/GUI/LAN; chưa có hiệu năng thật; chưa có GitHub, video và thông tin thành viên. Các bước này không được giả mạo thành hoàn thành.
