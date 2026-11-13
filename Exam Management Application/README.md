# Hệ thống quản lý thi trắc nghiệm

Web ASP.NET Core MVC và Desktop WinForms dùng chung API, nghiệp vụ và SQL Server. [Phân công và cách làm việc nhóm](team/README.md).

## Chạy trên Windows

Cần .NET SDK 10, PowerShell và SQL Server Express/Developer. Chạy từ thư mục chứa solution. Thay `.\SQLEXPRESS` bằng instance của bạn nếu khác.

```powershell
.\scripts\Initialize-Local.ps1 -SqlInstance '.\SQLEXPRESS' -RunTests
```

Script build, áp dụng migration, tạo dữ liệu demo và chạy test. Tài khoản mẫu ở `.local/demo-credentials.txt`. Giữ file này và User Secrets ngoài Git.

Mở hai terminal, chạy Web trước rồi chạy Desktop:

```powershell
.\scripts\Start-Web.ps1
```

```powershell
.\scripts\Start-Desktop.ps1
```

Khi chạy bằng hai script trên, Web và Desktop dùng [http://localhost:5180](http://localhost:5180). Nếu chạy Exam.Web bằng launch profile HTTPS của Visual Studio, nhập `https://localhost:60012` trong phần **Cài đặt máy chủ** của Desktop.

Quản trị viên đăng nhập Desktop để quản lý môn và phân công giảng viên. Giảng viên đăng nhập Web để xem môn được phân công. Thí sinh đăng nhập Web để tham gia đợt thi được cấp quyền. Dữ liệu demo trên cơ sở dữ liệu mới có sẵn đợt thi và mã tham gia được in lúc khởi tạo.

Các màn Web quản lý học liệu thuộc HL01/HL02. Tạo và sửa đợt nháp thuộc DT01, gán lớp và kích hoạt thuộc DT03, theo dõi và công bố kết quả thuộc DT04. Script `New-Demo.ps1` dùng API của luồng cán bộ cũ nên không dùng với bản phân quyền QT01.

## Kiểm tra

```powershell
dotnet build '.\Exam Management Application.slnx' -m:1
.\scripts\Test-Local.ps1 -SqlInstance '.\SQLEXPRESS'
```

`Test-Local.ps1` chạy test SQL trên DB riêng từng ca. Chạy `dotnet test` trực tiếp mà thiếu `EXAM_TEST_CONNECTION` sẽ bỏ qua các ca SQL.

[Tài liệu kỹ thuật và bằng chứng demo](docs/README.md) · [Hợp đồng API](CONTRACT.md) · [Quyết định kiến trúc](DECISIONS.md)

Link báo cáo: GG Docs: 

[docs.google.com/document/d/1tU_RzXl9BvWJLrHWJVXhnDzs7SAR4NL5jfF3XFDrr_Q/edit?usp=sharing](https://docs.google.com/document/d/1tU_RzXl9BvWJLrHWJVXhnDzs7SAR4NL5jfF3XFDrr_Q/edit?usp=sharing)[
](https://docs.google.com/document/d/1tU_RzXl9BvWJLrHWJVXhnDzs7SAR4NL5jfF3XFDrr_Q/edit?usp=sharing)Link phân công GG Sheets:

[docs.google.com/spreadsheets/d/1hyF5GWGogZLRShDBVxrGsSs6v8rBqPNGVxIJW5_Qfq8/edit?usp=sharing](https://docs.google.com/spreadsheets/d/1hyF5GWGogZLRShDBVxrGsSs6v8rBqPNGVxIJW5_Qfq8/edit?usp=sharing)

Link báo cáo Nghiệp vụ GG Sheets:

[docs.google.com/spreadsheets/d/10sSsgNXNt2c-S9SLn9rWQej7txwzoXuFAWfPI9gzFOc/edit?usp=sharing](https://docs.google.com/spreadsheets/d/10sSsgNXNt2c-S9SLn9rWQej7txwzoXuFAWfPI9gzFOc/edit?usp=sharing)
