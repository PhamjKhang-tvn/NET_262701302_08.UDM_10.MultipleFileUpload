# TV4 Nhật ký thay đổi và minh chứng thực tế

Ngày thực hiện 25/11/2026, giờ Việt Nam (+07:00). Baseline f07c115. Các thay đổi và lượt kiểm thử được thực hiện trong phiên làm việc với Codex; chưa có commit/PR mới hoặc xác nhận review từ TV3.

| Thời điểm | Công việc thực tế | Kết quả | Minh chứng |
|---|---|---|---|
| 22:24–22:30 | Đọc gói TV4, so nguồn, nối ServerCore, thêm Headless và Tests | Build được các module cần cho demo/test | MinhChung/build.txt và diff |
| 22:30 | Chạy 21 ca TCP lần đầu | 20 PASS, TC_21 FAIL vì harness mở log với share mode không phù hợp | MinhChung/20261125-lan1.txt |
| 22:31 | Sửa FileShare.ReadWrite rồi chạy lại | Build 0 warning/error, 21/21 PASS | MinhChung/20261125-lan2.txt |
| 22:33 | CLI ping, upload, so SHA-256, trùng tên | PONG, nhận 17 byte đúng hash, từ chối tên trùng | MinhChung/demo-server.txt |

## Nguồn và thay đổi

ServerEngine có sẵn ở Code/NhanFileTuServer/ServerEngine.cs, khớp nội dung gói TV4 (khác kiểu xuống dòng). Giữ nguồn này tại chỗ và dùng Compile Link thay vì chép thêm bản dễ lệch nhau.

Sửa để các subscriber Changed/Log không chặn nhau khi ném exception, thông báo bắt đầu nhận cho cả file rỗng, kiểm tra config null và dùng chung Task StopAsync. Bổ sung harness TCP thật, CLI và script ghi môi trường/build/hash. Hash server/harness tại thời điểm chạy ở MinhChung/source-hashes.txt.

Log server và file thử gốc ở BuildLogs/TV4/20261125-223008-f3721e3a, BuildLogs/TV4/20261125-223124-8bbfac1f và BuildLogs/TV4-demo-20261125-223310. BuildLogs bị gitignore; bản kết quả thật đã chép vào MinhChung để review cùng thay đổi. Dữ liệu thử không phải file cá nhân.

## Trạng thái bàn giao

- [x] Nối TV4 vào project hiện có, có demo và hướng dẫn.
- [x] Build Shared, ServerCore, Headless, Tests bằng SDK .NET 10 trên Windows.
- [x] Chạy 15 test ID được giao và 6 ca bổ sung; giữ lượt FAIL và PASS.
- [x] Viết ghi chú, sơ đồ, báo cáo và câu hỏi vấn đáp.
- [ ] Build và demo toàn bộ solution/GUI đã tích hợp.
- [ ] Kiểm tra LAN, đĩa đầy và quyền ghi.
- [ ] Nhận review tương thích trực tiếp từ TV3.
- [ ] Quay video có mặt, ghép slide và bàn giao TV1/TV2.
- [ ] Tạo commit/PR rồi ghi link thật.

Các mục chưa đánh dấu không được coi là hoàn thành. Không thay ngày tháng hay kết quả bằng thông tin chưa có minh chứng.


## Lượt xác minh cuối

Lúc 22:37 ngày 25/11/2026 (+07:00), tăng độ chặt test ba client bằng ba payload khác nhau và gửi một phần payload trước Stop. Chạy lại đạt 21/21, build không warning/error. Giữ kết quả tại MinhChung/20261125-lan3.txt, build tại MinhChung/build-final.txt, hash server/harness cuối tại MinhChung/source-hashes-final.txt. Log gốc tại BuildLogs/TV4/20261125-223707-9f7839e6. Các minh chứng lan1/lan2 và hash trước đó được giữ nguyên để đối chiếu lịch sử.
