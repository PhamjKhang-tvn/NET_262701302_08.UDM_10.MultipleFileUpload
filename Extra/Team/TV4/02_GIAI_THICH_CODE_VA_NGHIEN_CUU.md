# TV4 Cách server nhận và lưu file

Module chính là Code/NhanFileTuServer/ServerEngine.cs. Project Code/TV1/Code/ServerCore/Udm10.ServerCore.csproj liên kết trực tiếp file này, giữ một bản nguồn duy nhất. Server dùng Wire, Models và Settings trong Shared của TV1. Phần cửa sổ WinForms thuộc TV6; callback cần chuyển về UI thread bằng BeginInvoke.

## Luồng xử lý

~~~mermaid
flowchart TD
    A[Start kiểm tra cấu hình và khóa thư mục] --> B[TcpListener chờ kết nối]
    B --> C{Còn slot phiên?}
    C -->|Không| D[ERROR BUSY rồi đóng kết nối]
    C -->|Có| E[Task riêng gửi WELCOME]
    E --> F[Đọc UPLOAD kiểm tra tên và size]
    F --> G{Tên đã có hoặc đang nhận?}
    G -->|Có| H[ERROR DUPLICATE]
    G -->|Không| I[Giữ tên và mở file .part]
    I --> J[READY rồi nhận đúng số byte]
    J --> K[Kiểm tra END và SHA256]
    K --> L[Flush đóng file rồi File.Move]
    L --> M[COMPLETE]
    F -->|Lỗi| N[Log lỗi và đóng phiên]
    J -->|EOF timeout hoặc hủy| N
    K -->|Sai hash hoặc id| N
    L -->|Lỗi lưu| N
    N --> O[Xóa file tạm trả tên và slot]
    H --> O
    M --> O
~~~

## Các điểm cần giải thích

Start(ServerOptions) xác thực địa chỉ IPv4, cổng, thư mục, số phiên và dung lượng tối đa. Server giữ .udm10-server.lock với FileShare.None để ngăn hai server dùng cùng thư mục; không chấp nhận thư mục nhận/tạm/log là symlink hoặc junction trực tiếp. Không tuyên bố bảo vệ trước một tiến trình cục bộ có quyền sửa cấu trúc thư mục trong lúc server chạy.

AcceptLoop nhận client và dùng SemaphoreSlim để giới hạn phiên. Mỗi phiên xử lý riêng nên client đứt kết nối không làm vòng chờ hoặc phiên khác dừng. Hết slot trả ERROR/BUSY. Tên được giữ trong HashSet không phân biệt hoa thường, dưới cùng một lock với bước kiểm tra file đích. Hai client cùng tên không thể đồng thời nhận READY.

Wire.ReadExactCore đọc lặp cho đến đủ prefix/body. TCP là luồng byte: một ReadAsync có thể trả về ít byte hơn yêu cầu. Prefix điều khiển là 4 byte big-endian; JSON tối đa 16 KiB. Payload là đúng số byte khai báo, không phải một JSON/frame cho mỗi chunk.

FileRules.ValidateName chặn đường dẫn và các tên Windows dành riêng được liệt kê trong hàm. Size phải có giá trị, không âm và không vượt MaxFileBytes. Metadata sai bị trả ERROR trước READY. Tên Unicode được chuẩn hóa Form C.

HandleClient tạo file udm10-GUID.part trong .udm10-incoming và nhận dữ liệu bằng buffer 64 KiB. SHA-256 được tính trong lúc ghi. Chỉ khi nhận đủ byte, END có đúng id và hash, file được flush và đóng rồi mới File.Move sang tên đích. File.Move không ghi đè file đã tồn tại. Nếu lỗi, finally xóa file tạm, giải phóng tên và slot.

Khi khởi động, server chỉ dọn file tạm đúng mẫu do ứng dụng sở hữu. Một instance ServerEngine chỉ Start một lần; muốn khởi động lại sau StopAsync cần tạo instance mới. Các lời gọi StopAsync lặp dùng chung Task. Log và Changed bắt lỗi riêng cho từng subscriber để một callback hỏng không chặn callback khác.

## Nguồn chính thức

- [TCP và TcpListener](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes).
- [NetworkStream.ReadAsync](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readasync?view=net-10.0).
- [File.Move và lỗi khi file đích đã tồn tại](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0).

## Thống nhất với TV3

Gói TV4 mới và Shared dùng WELCOME/UPLOAD/READY/END/COMPLETE. Các từ HELLO/OK/SUCCESS trong phân công mô tả vai trò tương đương, không phải tên message hiện tại. ChatApp_Client và cặp FileSender/FileReceiver có giao thức riêng; cần ghép qua UploadClientService đã được kiểm thử cùng ServerEngine.
