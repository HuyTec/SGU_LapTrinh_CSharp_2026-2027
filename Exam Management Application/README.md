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

Web ở [localhost:5180](http://localhost:5180). Muốn tạo đợt mở ngay trên DB demo, chạy:

```powershell
.\scripts\New-Demo.ps1 -Minutes 5
```

Thí sinh demo đăng nhập Web, làm và nộp bài. Hết cửa sổ thi, dùng tài khoản `officer` trên Desktop để công bố và xuất kết quả. Điểm hiển thị trên Web sau khi công bố. Đây là luồng của code hiện tại; kiến trúc đích chuyển tổ chức thi sang Web Giảng viên và bỏ `ExamOfficer`.

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

### Thử lớp trên Web thí sinh

Chỉ chạy trên SQL Docker local, database **ExamAppDocker**, sau migration và seed tài khoản demo:

```powershell
sqlcmd -S 'tcp:127.0.0.1,14333' -U sa -d ExamAppDocker -C -b -i .\scripts\Seed-StudentClassDemo.sql
```

Đăng nhập thí sinh → **Lớp của tôi** → nhập `DEMO-NV10` → vào thi bằng `THI-NV10`.
Tự tham gia lớp thử chỉ bật trong Development. QT03 quản lý danh mục lớp trên Desktop, QT04 quản lý thành viên, UC-19 gán lớp vào đợt thi trên Web giảng viên.
