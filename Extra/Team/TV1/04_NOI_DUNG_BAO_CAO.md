# TV1 — Nội dung báo cáo để tổng hợp (tuần 4)

## Mục tiêu
Đề tài UDM_10 xây dựng ứng dụng truyền nhiều tệp qua TCP theo mô hình Client–Server. Client cho phép chọn/kéo thả nhiều tệp, theo dõi trạng thái, tiến độ và tốc độ riêng; Server tiếp nhận kết nối, kiểm tra dữ liệu, lưu tệp và cung cấp thông tin hoạt động. Mục tiêu là bảo đảm dữ liệu được truyền đúng, có cơ chế giới hạn tải và cô lập lỗi để một tệp hoặc một kết nối gặp sự cố không làm dừng toàn bộ hệ thống.

## Phạm vi và công nghệ
Sử dụng C# và Windows Forms trên .NET 10. Hai ứng dụng Client và Server là hai process độc lập, giao tiếp bằng TCP/IPv4; không xây dựng Web App, cơ sở dữ liệu hoặc chức năng đăng nhập. Mỗi Client xử lý hàng đợi tuần tự một tệp, Server mặc định giới hạn ba phiên đồng thời và cho phép cấu hình. Không hỗ trợ resume tệp, không triển khai TLS/xác thực trong phạm vi này. Kết quả mạng LAN chỉ công bố nếu thực sự có thử nghiệm trên hai máy.

## Kiến trúc và giao thức
Solution gồm Client, Server, Shared, ServerCore, Headless và Tests. ClientForm chuyển thao tác người dùng cho UploadQueue và UploadClientService; ServerForm hiển thị sự kiện từ ServerEngine. Shared định nghĩa Message, cấu hình và bộ đọc/ghi Wire. Control frame có 4 byte độ dài big-endian theo sau là JSON UTF-8; dữ liệu tệp truyền theo số byte đã khai báo. Các bước chính WELCOME, UPLOAD, READY, payload, END và COMPLETE. Server kiểm tra tên, kích thước, SHA-256; chỉ công nhận file hoàn chỉnh sau xác minh và đổi tên tệp tạm. Khi mất xác nhận cuối, Client báo kết quả chưa xác định thay vì khẳng định tệp chưa được lưu.

## Phân công (vai trò dự kiến, xác nhận thực tế trước nộp)
| Thành viên | Vai trò chính | Minh chứng cần có |
|---|---|---|
| TV1 | Yêu cầu, kiến trúc, Shared contract/codec/cấu hình, điều phối tích hợp và báo cáo | Commit/PR, đặc tả, kết quả build/review, báo cáo |
| TV2 | GUI Client | ClientForm, ảnh và test GUI |
| TV3 | Dịch vụ gửi TCP | UploadClientService, log upload/hash |
| TV4 | Nhận TCP Server | ServerEngine, kiểm thử lỗi/slot/file tạm |
| TV5 | Queue, progress, trạng thái | UploadQueue, test queue/progress |
| TV6 | GUI Server, log, kiểm thử | ServerForm, test harness, workbook và kết quả thật |

## Tổng hợp kết quả
Dẫn bảng test và minh chứng của TV2–TV6; ghi cấu hình máy, input, số Client, môi trường loopback/LAN, thời gian, byte, throughput, lỗi và bộ nhớ theo các phép đo thật. Không sử dụng số liệu của bài Research TCP Echo cho UDM_10. Kết luận phải tương ứng test đã chạy; ghi các lỗi hoặc giới hạn chưa kiểm chứng. Báo cáo tối đa 15 trang theo yêu cầu gốc UDM_10. Bổ sung tên/MSSV, link GitHub và video theo yêu cầu hiện hành của giảng viên.