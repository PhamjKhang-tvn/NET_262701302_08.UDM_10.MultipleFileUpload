# TV3 - Kịch bản Thuyết trình và Vấn đáp

## Kịch bản Demo
1. Giới thiệu IP/Port, chỉ rõ tiến trình Client và Server là 2 process riêng biệt.
2. Mở file `UploadClientService.cs`, chỉ vào hàm `UploadAsync`.
3. Demo gửi 1 file thành công (thanh tiến trình chạy).
4. Demo lỗi: Tắt Server khi đang gửi -> Client báo lỗi Timeout/Connection Lost.

## Câu hỏi Vấn đáp (Kèm câu trả lời)
1. **Tại sao dùng TCP?**
   -> TCP đảm bảo dữ liệu đến đúng thứ tự, không mất mát, phù hợp để truyền file.
2. **Tại sao không dùng `File.ReadAllBytes`?**
   -> Nếu file 1GB sẽ tràn RAM. Dùng Chunking (đọc 64KB) giúp RAM luôn ổn định.
3. **Mã `READY` để làm gì?**
   -> Để Server xác nhận đã sẵn sàng nhận file, tránh việc Client gửi ồ ạt làm nghẽn.
4. **Tại sao cần SHA-256?**
   -> Để kiểm tra file sau khi truyền có bị lỗi byte nào không.
5. **Nếu mất gói tin `COMPLETE` cuối cùng thì sao?**
   -> Client sẽ báo `UNKNOWN_RESULT` (không rõ kết quả), khuyên người dùng kiểm tra Server trước khi gửi lại để tránh trùng lặp.