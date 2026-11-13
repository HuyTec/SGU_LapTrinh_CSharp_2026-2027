# Hệ thống quản lý thi trắc nghiệm

Web ASP.NET Core MVC và Desktop WinForms dùng chung API, nghiệp vụ và SQL Server. [Phân công và cách làm việc nhóm](team/README.md).

## Chạy nhanh trên Windows

Cần .NET SDK 10, PowerShell và SQL Server. Chạy lệnh từ thư mục chứa solution.

1. **Khởi tạo lần đầu với SQL Server Express:**

   ```powershell
   .\scripts\Initialize-Local.ps1 -SqlInstance '.\SQLEXPRESS'
   ```

   Script áp dụng migration và tạo dữ liệu demo. Tài khoản mẫu nằm trong `.local/demo-credentials.txt`. Nếu đã cấu hình và seed SQL Docker qua User Secrets, bỏ qua bước này.

2. **Chạy bằng Visual Studio:** mở `Exam Management Application.slnx`, đặt `Exam.Web` làm Startup Project, chọn profile `Exam.Web` rồi nhấn **F5**. Web mở tại [https://localhost:60012](https://localhost:60012). Mở Desktop từ PowerShell:

   ```powershell
   .\scripts\Start-Desktop.ps1 -ApiUrl 'https://localhost:60012'
   ```

   Nếu chạy cả hai bằng PowerShell, mở hai terminal và chạy `.\scripts\Start-Web.ps1` trước, rồi `.\scripts\Start-Desktop.ps1`. Cả hai dùng `http://localhost:5180`.

3. **Đăng nhập:** quản trị viên dùng Desktop. Giảng viên và thí sinh dùng Web. Mã đợt thi demo được in khi seed cơ sở dữ liệu mới.

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

## Cấu trúc thư mục

```text
Exam Management Application/
├── Exam Management Application.slnx
├── Exam.Web/           # Web MVC cho thí sinh, giảng viên và API cho Desktop
│   ├── Controllers/
│   ├── Views/
│   └── wwwroot/        # CSS, JavaScript và tài nguyên tĩnh
├── Exam.Desktop/       # Ứng dụng quản trị WinForms
├── Exam.Core/          # DTO và kiểu dữ liệu dùng chung
├── Exam.Services/      # Nghiệp vụ và kiểm tra quyền
├── Exam.Data/          # EF Core, Identity và SQL Server
│   └── Migrations/
├── Exam.Tests/         # xUnit và kiểm thử SQL Server cách ly
├── scripts/            # Khởi tạo, chạy và kiểm thử cục bộ
├── docs/               # Đặc tả, sơ đồ và tài liệu kỹ thuật
├── team/               # Phân công và quy trình nhóm
└── App1/               # WinUI 3 lưu để tham khảo
```
