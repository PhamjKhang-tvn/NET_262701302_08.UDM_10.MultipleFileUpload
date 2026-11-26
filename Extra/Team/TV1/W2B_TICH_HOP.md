# Ghép GUI - queue - service - Server

**Mã việc:** 2B-TV1. Đây là hợp đồng/tài liệu tích hợp và bộ script build, không phải biên bản demo đã đạt.

## Điểm ghép theo đúng source

| Luồng | File / thành phần | Phối hợp |
|---|---|---|
| GUI thu danh sách file và hiển thị tiến độ | ClientForm; Progress<TransferUpdate>; Apply | TV2 |
| Lần lượt xử lý file | UploadQueue.RunAsync | TV5 |
| Truyền một file | UploadClientService.UploadAsync | TV3 |
| Đọc/ghi control frame, cấu hình | Wire, Message, ClientOptions/ServerOptions | TV1 + TV3/TV4 |
| Nhận kết nối và file | ServerEngine.Start / StopAsync | TV4 |
| Log và trạng thái Server | ServerEngine.Log/Changed; ServerForm | TV6 + TV4 |

## Cấu hình cần công bố

| Tham số | Giá trị trong source |
|---|---|
| IPv4 / TCP port mặc định | 127.0.0.1 / 5000; có thể đổi trên GUI |
| Đồng thời mỗi Client | 1 file |
| Server MaxSessions | Mặc định 3; ServerOptions chấp nhận 1..16 |
| Timeout | Mặc định 30 giây, tùy chỉnh 1..600 |
| File tối đa | Mặc định 1 GiB; MaxFileBytes có giới hạn 0..10 GiB |
| Header / buffer | 16 KiB / 64 KiB |
| File trùng tên | Từ chối, không ghi đè |
| File dở | File .part tạm; không Completed |

MaxSessions là giới hạn phiên phía Server, không đồng nghĩa một Client upload ba file song song. Phiên probe/PING cũng có thể tạm chiếm slot.

## Tích hợp thật, không chỉ commit tài liệu

1. Mỗi người commit phần đã review bằng tài khoản cá nhân.
2. TV1 review/merge các nhánh vào nhánh tích hợp đã thống nhất; giữ lịch sử tác giả.
3. Build đủ source với `04_BUILD_ONLY.cmd`; lưu log/build commit. Script gốc build cả GUI, Headless và Tests, nên phải có đủ các file đó.
4. Chạy `01_BUILD_AND_RUN.cmd`; thử một file rồi nhiều file; ghi log/ảnh và bằng chứng đối chiếu nội dung.
5. Lưu PR/merge commit, ai review và kết quả vào bảng minh chứng. Chỉ sau đó mới đánh dấu 2B đã hoàn thành.

Mã giao thức giữ nguyên từ gói source, chưa biên dịch/chạy Windows ở môi trường chuẩn bị này. Không viết commit message “all tests passed”, “completed integration” nếu chưa có bằng chứng.
