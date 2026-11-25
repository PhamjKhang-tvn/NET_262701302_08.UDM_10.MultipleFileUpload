# TV1 — Công việc đầy đủ 4 tuần

TV1 là nhóm trưởng. Nguồn: `UDM10_Khung_4_Tuan_6_Thanh_Vien.xlsx`, sheet `Cong_viec`. Hạn do nhóm trưởng đặt; chưa xác nhận kết quả thực tế. Bản công nghệ đã chốt là C# / WinForms / .NET 10.

## Tuần 1 — 1A-TV1

**Nhiệm vụ:** Đọc 4 sheet của đề; tách bắt buộc/không bắt buộc; chốt phạm vi 4 tuần và tạo repository tối thiểu để cả nhóm lưu nghiên cứu.

**Sản phẩm bàn giao:** Checklist yêu cầu có địa chỉ ô nguồn; phạm vi; repository có .gitignore.

**Điều kiện nghiệm thu:** Giải thích được vì sao chọn queue; không bỏ yêu cầu chung/nộp bài; 5 thành viên còn lại được cấp quyền đóng góp.

**Phối hợp:** Không. **Review:** TV6. **Yêu cầu:** R01–R22.

Nguồn: `Cong_viec!A7:O7`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.

## Tuần 1 — 1B-TV1

**Nhiệm vụ:** Tạo Solution với Client/Server/Shared; chốt một phiên bản SDK chung; cùng TV3–TV4 viết quy ước giao thức v1 và cấu hình.

**Sản phẩm bàn giao:** Solution build được; cấu trúc Code/DOCX/Extra/PPTX; README; protocol-v1.

**Điều kiện nghiệm thu:** Có loại message, độ dài/encoding, tên/kích thước file, READY/SUCCESS/ERROR/BUSY, timeout, cách đóng kết nối; cả nhóm dùng một bản.

**Phối hợp:** 1A-TV1,1A-TV3,1A-TV4. **Review:** TV6. **Yêu cầu:** R08,R20,R22.

Nguồn: `Cong_viec!A13:O13`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.

## Tuần 2 — 2A-TV1

**Nhiệm vụ:** Triển khai bộ đọc/ghi message dùng chung theo protocol-v1 cùng TV3–TV4; kiểm tra giới hạn độ dài; ghép Client–Server sớm.

**Sản phẩm bàn giao:** Shared protocol codec + bản tích hợp một file.

**Điều kiện nghiệm thu:** Hai đầu dùng cùng định dạng; đọc thiếu header không bị coi là header đầy đủ; không để TV3 và TV4 tự phát minh hai protocol khác nhau.

**Phối hợp:** 1B-TV1,1B-TV3,1B-TV4. **Review:** TV6. **Yêu cầu:** R08,R09.

Nguồn: `Cong_viec!A19:O19`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.

## Tuần 2 — 2B-TV1

**Nhiệm vụ:** Tích hợp queue, GUI và dịch vụ; công bố giới hạn 1 file/Client, Server tối đa 3 phiên truyền; chốt quy tắc tên trùng và cấu hình.

**Sản phẩm bàn giao:** Bản upload nhiều file + README/protocol cập nhật.

**Điều kiện nghiệm thu:** Sáu phần chạy trên cùng nhánh tích hợp; giới hạn Server tách biệt với giới hạn mỗi Client; tham số được ghi rõ.

**Phối hợp:** 2A-TV1–TV6. **Review:** TV6. **Yêu cầu:** R04,R08,R22.

Nguồn: `Cong_viec!A25:O25`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.

## Tuần 3 — 3A-TV1

**Nhiệm vụ:** Rà cấu hình IP/port/timeout/giới hạn file và phiên; chốt mã lỗi, theo dõi phụ thuộc và ghép sửa lỗi vào bản chung.

**Sản phẩm bàn giao:** Cấu hình được kiểm tra + danh mục lỗi + bản tích hợp.

**Điều kiện nghiệm thu:** Không phụ thuộc đường dẫn một máy; giới hạn và timeout hiển thị/ghi rõ; thay đổi protocol được cập nhật cả hai đầu.

**Phối hợp:** 2B-TV1–TV6. **Review:** TV6. **Yêu cầu:** R08,R09,R10,R22.

Nguồn: `Cong_viec!A31:O31`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.

## Tuần 3 — 3B-TV1

**Nhiệm vụ:** Rà checklist yêu cầu với bản chạy; test cấu hình và clone/build sạch; điều phối chạy tải cùng TV6, chốt lỗi bắt buộc sửa.

**Sản phẩm bàn giao:** Ma trận yêu cầu → bằng chứng; biên bản cuối tuần 3.

**Điều kiện nghiệm thu:** Mỗi yêu cầu có người chịu trách nhiệm và test; không đánh dấu đạt khi chưa chạy; TV3/TV4/TV6 không tự ký nghiệm thu phần mình.

**Phối hợp:** 3A-TV1–TV6. **Review:** TV6. **Yêu cầu:** R08,R13,R15,R20,R22.

Nguồn: `Cong_viec!A37:O37`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.

## Tuần 4 — 4A-TV1

**Nhiệm vụ:** Chốt phạm vi, điều phối sửa lỗi ưu tiên; viết mục tiêu/phạm vi/kiến trúc/phân công; ghép báo cáo theo mẫu môn học.

**Sản phẩm bàn giao:** Bản ứng viên nộp + báo cáo đã ghép + bảng đóng góp.

**Điều kiện nghiệm thu:** Chỉ sửa lỗi bắt buộc, không thêm tính năng ngoài phạm vi; báo cáo tối đa 15 trang; nội dung chưa hoàn thành được ghi thật.

**Phối hợp:** 3B-TV1–TV6. **Review:** TV6. **Yêu cầu:** R17,R21,R22.

Nguồn: `Cong_viec!A43:O43`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.

## Tuần 4 — 4B-TV1

**Nhiệm vụ:** Khóa bản nộp; rà đủ hồ sơ/liên kết; đóng gói 7z đúng tên; ghi hình phần tổng quan có mặt; tổ chức tổng duyệt và tự nộp theo hạn mình đặt.

**Sản phẩm bàn giao:** Gói CourseCode-GroupCode-UDM_10.7z + phần trình bày TV1.

**Điều kiện nghiệm thu:** Có source sạch, danh sách công việc, DOCX/PPTX, GitHub, link video trong README và báo cáo; không có bin/obj/cache/secret thừa; kiểm tra hạn hệ thống môn học.

**Phối hợp:** 4A-TV1–TV6; phối hợp 4B-TV2–TV6. **Review:** TV6. **Yêu cầu:** R17,R18,R19,R20,R21.

Nguồn: `Cong_viec!A49:O49`. Hạn, link commit, minh chứng và kết quả điền ở `06_NHAT_KY_MINH_CHUNG.md`.
