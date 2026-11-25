# TV4 Báo cáo module TCP Server nhận và lưu file

Bản bàn giao này dành cho nhánh refactor/project-structure, theo cấu trúc solution chung của nhóm. ServerEngine đặt tại Code/ServerCore/ServerEngine.cs; project cùng thư mục tự biên dịch file này. Đây là bản chuyển từ mã TV4 ở main, có tích hợp và kiểm thử lại với Shared, Server GUI, Headless và harness hiện hữu của nhánh nhóm.

## Nhận và lưu file

Server gửi WELCOME, đọc UPLOAD và xác thực tên/kích thước trước khi giữ tên và trả READY. Gói điều khiển gồm prefix 4 byte big-endian và JSON UTF-8 tối đa 16 KiB. Payload là đúng số byte khai báo. END phải có đúng id và SHA-256.

Dữ liệu được ghi vào .udm10-incoming/udm10-GUID.part. Server chỉ gửi COMPLETE sau khi nhận đủ dữ liệu, đối chiếu hash, flush, đóng file và File.Move thành công. Tên đã tồn tại hoặc đang được giữ bởi phiên khác trả DUPLICATE; file cũ không bị ghi đè. EOF sớm, timeout, hash sai hoặc lỗi commit không tạo file hoàn chỉnh và phải dọn file tạm.

## Nhiều phiên và sự kiện

AcceptLoop tách khỏi task xử lý từng client. SemaphoreSlim giới hạn phiên; client dư nhận BUSY. Lock bảo vệ tên giữ chỗ để ngăn hai client cùng tên đồng thời được chấp nhận.

StopAsync hủy listener/phiên, chờ kết thúc rồi giải phóng log và khóa thư mục. Lời gọi dừng lặp chờ cùng Task; khởi động lại bằng instance mới. Log và Changed cô lập lỗi từng subscriber. ServerForm của TV6 chuyển callback về UI thread.

## Tích hợp vào cấu trúc nhóm

Giữ nguyên Headless serve/send, ServerForm, ClientForm, Shared và các ca test chung. Bổ sung Code/Tests/TV4ReceiveChecks.cs và hai dòng dispatch --tv4 tại Main của harness chung; không thay thế bộ test nhóm bằng bộ 21 ca trước đó. Tools/Test-TV4.ps1 build solution và chạy cả hai chế độ.

## Kiểm thử thực tế trên nhánh refactor

Ngày 25/11/2026 lúc 22:58 (+07:00), Windows 10.0.26300, SDK 10.0.404, runtime .NET 10.0.15:

- Sáu project trong UDM10.sln build thành công, 0 error. Lượt build đầy đủ có 720 warning CA1416 từ mã GUI của nhánh chung; giữ nguyên log, không báo thành 0 warning.
- Harness chung TC_09–TC_36 đạt 28/28, gồm queue, server, stress, lost ACK và client/server chạy khác tiến trình.
- Harness bổ sung TV4 đạt 21/21, gồm callback lỗi, commit I/O, file rỗng, payload chia nhỏ, nhiều client và dừng giữa upload.
- Hai bộ có nội dung chồng lặp; không cộng thành 49 ca nghiệm thu độc lập. TC_01–TC_08 và GUI/LAN chưa được xác nhận.

Minh chứng: [kết quả chung](MinhChung/refactor-20261125-225824/official-console.txt), [kết quả TV4](MinhChung/refactor-20261125-225824/tv4-console.txt), [môi trường](MinhChung/refactor-20261125-225824/environment.txt), [hash nguồn](MinhChung/refactor-20261125-225824/source-hashes.txt), [log build](MinhChung/refactor-20261125-225824/build.txt). JSON kết quả và log server được giữ trong cùng thư mục.

## Nguồn mã và giới hạn

Mã nhận file gốc đã có trong repo; commit chuyển nhánh không coi toàn bộ nguồn là đóng góp mới. Các lần chạy 22:30, 22:31, 22:37 trên main được giữ làm lịch sử. Lượt đầu có lỗi harness đọc log trên Windows, đã sửa FileShare.ReadWrite; không xóa dấu vết FAIL.

Lỗi I/O được kiểm tra bằng việc tạo file đích sau READY, chưa mô phỏng đĩa đầy/quyền ghi. Chưa nghiệm thu GUI, LAN, review trực tiếp TV3 hoặc video cá nhân. DOCX/TV4.docx vẫn là Word cũ, chưa đồng bộ trong lần chuyển nhánh này.
