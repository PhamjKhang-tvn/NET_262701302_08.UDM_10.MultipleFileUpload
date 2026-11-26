# Phạm vi và phân ranh trách nhiệm

## Quyết định thiết kế của nhóm

- C# + WinForms + .NET 10; Client và Server là hai process.
- Queue giới hạn một file đang upload trên mỗi Client.
- Server mặc định ba phiên; tham số cấu hình được. Số ba là lựa chọn nhóm, không phải yêu cầu thầy.
- Từ chối file trùng tên; không ghi đè. File dở lưu tạm, chưa được tính là hoàn tất.
- Gửi dữ liệu theo khối; không đọc cả file lớn vào RAM.
- Không đăng nhập/database/cloud/Pause-Resume. SHA-256 là kiểm tra nội dung, không phải mã hóa hay xác thực người dùng.
- Dùng trong mạng LAN tin cậy; không công bố bản chưa có xác thực/TLS ra Internet.

## Ranh giới commit

| Phần | Trách nhiệm chính |
|---|---|
| Yêu cầu, Solution/csproj, kiểu dữ liệu, cấu hình, framing, tài liệu tích hợp | TV1; phần chung cần TV3/TV4/TV5 review |
| ClientForm.cs, Client/Program.cs | TV2 |
| UploadClientService.cs | TV3 |
| ServerCore/ServerEngine.cs | TV4 |
| UploadQueue.cs | TV5 |
| ServerForm.cs, Server/Program.cs, Tests/Program.cs | TV6; test do cả nhóm thực hiện |

`Models.cs` chứa contract dùng chung, bao gồm cả DTO trạng thái và luật file. TV1 không nhận công độc quyền cho phần phối hợp; ghi review/thay đổi thực tế.

## Cách chọn queue

Queue phù hợp phạm vi bốn tuần và yêu cầu cho phép. Một file lỗi cục bộ không dừng file sau. Khi Server không khả dụng, chưa thể truyền file tiếp; giữ file chưa thử ở Waiting và thử tiếp khi Server hoạt động trở lại.

## Nguồn

`ProjectList_LTMang(1).xlsx`, `Project Details!C78`; `General Requirements!C9:C27,C41`.
Các lựa chọn cụ thể trên được đối chiếu với bộ mã nguồn đã bàn giao, không coi là quy định bắt buộc của giảng viên.
