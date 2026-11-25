# TV4 trên nhánh cấu trúc chung

Nhánh bàn giao: refactor/project-structure. Module nhận/lưu được đặt tại Code/ServerCore/ServerEngine.cs, đúng project ServerCore của solution UDM10.sln. Không đem các thư mục Code/TV1/Code hoặc Code/NhanFileTuServer từ main sang nhánh này.

## Cấu trúc TV4

~~~text
Code/ServerCore/ServerEngine.cs
Code/Tests/TV4ReceiveChecks.cs
Tools/Test-TV4.ps1
Extra/Team/TV4/
  01_NHIEM_VU_DAY_DU.md
  02_GIAI_THICH_CODE_VA_NGHIEN_CUU.md
  03_TEST_CASES_DUOC_GIAO.md
  04_NOI_DUNG_BAO_CAO.md
  05_THUYET_TRINH_VAN_DAP.md
  06_NHAT_KY_MINH_CHUNG.md
  MinhChung/
~~~

ServerForm, ClientForm, Shared, Headless và harness chung được giữ theo nhánh nhóm. Code/Tests/Program.cs chỉ thêm lựa chọn --tv4 để gọi harness bổ sung; đường chạy mặc định và các ca chung không bị thay thế.

## Build và kiểm thử

Tại gốc repo trong PowerShell:

~~~powershell
.\Tools\Test-TV4.ps1
~~~

Cần SDK .NET 10. Script dùng dotnet trong PATH hoặc SDK cục bộ tại ..\.tools\dotnet\dotnet.exe nếu có. Có thể chỉ định -DotnetPath để chọn SDK. Script build toàn bộ UDM10.sln, chạy harness chung và harness bổ sung TV4. Log và hash nguồn lưu dưới BuildLogs/TV4-refactor-*.

Lượt 25/11/2026 lúc 22:58 (+07:00): sáu project build thành công, 0 error; 720 cảnh báo CA1416 từ mã WinForms của nhánh chung được giữ trong log. Harness chung chạy TC_09–TC_36 đạt 28/28; harness TV4 đạt 21/21. Hai bộ có ca trùng nội dung, không diễn giải thành 49 tình huống độc lập. TC_01–TC_08 và nghiệm thu GUI/LAN chưa được xác nhận trong lượt này.

Minh chứng hiện tại nằm ở [refactor-20261125-225824](MinhChung/refactor-20261125-225824/official-console.txt). Các file lan1/lan2/lan3 ngoài thư mục đó là lịch sử kiểm thử trên cấu trúc main cũ, không thay cho minh chứng tích hợp mới.

## Demo dùng chương trình chung

Sau khi build, mở hai PowerShell tại gốc repo. Nếu SDK chưa nằm trong PATH, thay dotnet bằng đường dẫn dotnet.exe đã dùng trong script.

~~~powershell
dotnet Code/Headless/bin/Debug/net10.0/Udm10.Headless.dll serve ./UDM10_Uploads 5000 3 30
~~~

Ở cửa sổ thứ hai:

~~~powershell
'Du lieu thu TV4' | Set-Content ./BuildLogs/demo-tv4.txt
dotnet Code/Headless/bin/Debug/net10.0/Udm10.Headless.dll send 127.0.0.1 5000 ./BuildLogs/demo-tv4.txt
~~~

Gửi lại cùng tên trả DUPLICATE. Nhập STOP ở server để chờ dọn phiên và đóng listener. Chương trình Headless ở nhánh này dùng serve/send, không dùng server/upload/ping của demo cũ trên main.

Có thể mở GUI đã build tại Code/Server/bin/Debug/net10.0-windows/Udm10.Server.exe và Code/Client/bin/Debug/net10.0-windows/Udm10.Client.exe. Việc build được GUI không tự xác nhận các bước thao tác giao diện đã được kiểm thử.

## Word và nghiệm thu

DOCX/TV4.docx vẫn là báo cáo Word cũ và chưa được cập nhật trong commit chuyển nhánh này. Nội dung báo cáo hiện tại nằm ở 04_NOI_DUNG_BAO_CAO.md; Word cần được đồng bộ trước khi nghiệm thu tài liệu. Chưa xác nhận review TV3, demo LAN hoặc video cá nhân.

Giao thức chuẩn: [Extra/Guides/PROTOCOL.md](../../Guides/PROTOCOL.md). File kiểm thử chính thức: Extra/TestCases/UDM10_Test_Cases.xlsx.
