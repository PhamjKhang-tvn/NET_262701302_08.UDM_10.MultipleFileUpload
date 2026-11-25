# TV4 Báo cáo module TCP Server nhận và lưu file

Phần TV4 tiếp nhận kết nối TCP, kiểm tra yêu cầu upload và lưu file vào thư mục đã chọn. Mã ServerEngine từ gói bàn giao đã có tại Code/NhanFileTuServer/ServerEngine.cs ở baseline f07c115. Lần làm việc này nối mã vào ServerCore, bổ sung demo, bộ kiểm thử và sửa callback/StopAsync. Không coi toàn bộ mã gốc là phần mới tự viết.

## Nhận và kiểm tra file

Server gửi WELCOME, kiểm tra UPLOAD gồm tên và kích thước, giữ chỗ tên rồi gửi READY. Tên chứa đường dẫn, tên cấm, size âm và size vượt giới hạn bị từ chối. Dữ liệu được ghi vào file .part, đồng thời tính SHA-256. END phải đúng id và hash. Server đóng file tạm, đổi sang tên cuối bằng File.Move rồi mới gửi COMPLETE.

Tên đã tồn tại hoặc đang được phiên khác sử dụng trả DUPLICATE. File cũ được giữ nguyên, không tự thêm số thứ tự. Nếu client mất kết nối, timeout hoặc hash sai, server không tạo file hoàn chỉnh và dọn file tạm của phiên.

## Nhiều client và tài nguyên

TcpListener tách vòng accept khỏi từng phiên nhận. SemaphoreSlim giới hạn số phiên, client dư nhận BUSY. Lock bảo vệ tên dự trữ. StopAsync hủy listener và phiên, chờ hoàn tất rồi đóng log và khóa thư mục; các lần gọi lặp chờ chung một Task. Để khởi động lại, GUI tạo instance mới.

Mỗi subscriber Changed/Log được cô lập lỗi. Đã bổ sung sự kiện bắt đầu nhận ở mức 0 byte, kể cả file rỗng, để giao diện có trạng thái nhận trước khi hoàn tất.

## Kiểm thử thực tế ngày 25 tháng 11 năm 2026

Môi trường: Windows 10.0.26300, SDK 10.0.404, runtime .NET 10.0.15, múi giờ +07:00. Build Headless và Tests cùng Shared/ServerCore đạt 0 warning, 0 error. Bộ test dùng TCP thật trên 127.0.0.1, thư mục riêng và dữ liệu giả lập.

Lượt 22:30 đạt 20/21. TC_21 bị lỗi ở harness khi đọc log đang được ghi trên Windows. Sửa mở log với FileShare.ReadWrite, lượt 22:31 đạt 21/21. Gồm 15 test ID được giao và 6 ca bổ sung: header chia nhỏ/file rỗng, cô lập client lỗi, lỗi commit I/O, END sai, callback lỗi và UploadClientService.

Lúc 22:33, demo CLI trả PONG, nhận file 17 byte với SHA-256 khớp và từ chối gửi lại cùng tên. Tiến trình demo được dừng cưỡng bức sau kiểm tra; kiểm tra dừng có dọn phiên thực hiện trong TC_27 bằng StopAsync.

Minh chứng: [lượt đầu](MinhChung/20261125-lan1.txt), [lượt đạt](MinhChung/20261125-lan2.txt), [build](MinhChung/build.txt), [môi trường](MinhChung/environment.txt), [hash nguồn](MinhChung/source-hashes.txt), [log demo](MinhChung/demo-server.txt).

## Giới hạn và phần nhóm cần thực hiện

Chưa kiểm thử GUI WinForms, truyền qua LAN, đĩa đầy hoặc lỗi quyền ghi hệ điều hành. Ca COMMIT_IO_FAILURE tạo file đích sau READY để kiểm tra đường lỗi I/O và bảo toàn file cũ. Chưa build toàn bộ solution do các project Client/Server còn thiếu mã UI tại đường dẫn tham chiếu. Chưa có xác nhận review TV3, video có mặt, slide nhóm hoặc commit/PR mới. Cần hoàn thành các hạng mục đó trước nghiệm thu toàn bộ bài nhóm.


## Xác minh cuối lúc 22 giờ 37

Tăng độ chặt TC_23 bằng ba payload khác nhau để phát hiện lẫn dữ liệu, và cho TC_27 gửi một phần payload trước khi Stop. Build lại và chạy toàn bộ đạt 21/21, 0 warning/error. Minh chứng cuối: [kết quả](MinhChung/20261125-lan3.txt), [build](MinhChung/build-final.txt), [hash nguồn cuối](MinhChung/source-hashes-final.txt). Những thay đổi sau lượt này chỉ cập nhật tài liệu và chép minh chứng.
