# TV1 — Kiểm thử được giao và nghiệm thu tích hợp (tuần 3)

Đối chiếu hồ sơ chính thức `Extra/TestCases/UDM10_Test_Cases.xlsx`, không dùng mã TC của sheet Kiem_thu cũ. Các test dưới đây được giao TV1 hoặc TV1 phối hợp. Không điền PASS trước khi chạy.

| Mã | Phối hợp | Thao tác / kết quả mong đợi |
|---|---|---|
| TC_03 | TV1 | Nhập IP abc, kiểm tra port/timeout ngoài giới hạn, sửa về 127.0.0.1:5000. GUI báo lỗi hoặc chặn giá trị, không crash. |
| TC_07 | TV3, TV4 | Hai máy Windows cùng LAN; Client dùng IP LAN Server; truyền file và so SHA-256. Chụp IP, cấu hình, log; nếu không có hai máy ghi NOT TEST YET và giới hạn. |
| TC_17 | TV4 | Harness gửi JSON lỗi; phiên bị từ chối/đóng, Server vẫn phục vụ phiên hợp lệ. |
| TC_18 | TV4 | Prefix >16 KiB; Server từ chối BAD_LENGTH/đóng, không cấp phát theo length sai. |
| TC_31 | TV6 | Harness loopback 1 Client, 6 file x 1 MiB; ghi byte, thời gian, throughput, lỗi, working set từ results.json. |
| TC_32 | TV6 | 3 Client, mỗi Client 6 file x 1 MiB; so cùng các chỉ số, không gọi là benchmark LAN. |
| TC_34 | TV3, TV4 | Chia header/payload thành nhiều mảnh; Server đọc đủ, hash đúng. |
| TC_36 | TV6 | Headless ở process OS khác; upload loopback, đối chiếu hash, STOP. Không thay thế test GUI/LAN. |

## Chạy kiểm thử
Tại gốc repo **đầy đủ** (đã tích hợp TV2–TV6), mở PowerShell:

```powershell
dotnet --list-sdks
.\04_BUILD_ONLY.cmd
.\02_RUN_TESTS.cmd
```

Script test của TV6 chạy 28 ca tự động (TC_09..TC_36). TC_01..TC_08 là thủ công. Kết quả thật nằm ở `Extra/TestResults/<timestamp>/results.json` và workbook sau khi import; nếu Excel đang mở, đóng trước khi import lại. Không đánh dấu thủ công PASS từ kết quả dự kiến.

## Tuần 3: rà soát cấu hình và lỗi
- Xem ClientOptions/ServerOptions và cấu hình GUI: IP, port, timeout, giới hạn file, max sessions; thư mục nhận phải có quyền ghi.
- Đối chiếu các lỗi: BAD_LENGTH, BAD_JSON, BAD_PROTOCOL (Wire); DUPLICATE, INVALID_NAME, BAD_SIZE, TOO_LARGE, BUSY, HASH_MISMATCH (Server); UNKNOWN_RESULT khi không xác nhận cuối. Mã và hành vi chi tiết phải đối chiếu log/source thật; timeout có thể kết thúc kết nối thay vì luôn trả frame lỗi.
- Clone sạch tại thư mục khác nếu có GitHub; build đúng commit được nghiệm thu. Không thay đổi repo đang làm việc chỉ để thử clone.
- TV1 điều phối TV6 lưu kết quả, TC_31/32/33, review ma trận 22 yêu cầu `W1A_YEU_CAU.md` và ghi lỗi cần sửa vào `06_NHAT_KY_MINH_CHUNG.md`.

Mỗi ca: ghi ngày, commit, môi trường, input, expected, actual, status, link bằng chứng. Nếu thất bại, lưu bước tái hiện và chạy lại sau khi sửa.
