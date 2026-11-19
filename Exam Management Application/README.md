# Hệ thống quản lý thi trắc nghiệm

Web ASP.NET Core MVC và Desktop WinForms dùng chung API, nghiệp vụ và SQL Server. [Phân công và cách làm việc nhóm](team/README.md).

## Chạy nhanh trên Windows

Cần .NET SDK 10, PowerShell và Docker Desktop ở chế độ Linux containers. SQL Server phải chạy trong Docker tại `tcp:127.0.0.1,14333`; không dùng SQL Express/LocalDB. Web và Desktop chạy bằng .NET trên Windows. Chạy lệnh từ thư mục chứa solution.

1. **Mở Docker Desktop, khởi tạo lần đầu:**

   ```powershell
   .\scripts\Initialize-Local.ps1
   ```

   Script khởi động SQL container, chờ healthcheck, lưu cấu hình Docker vào User Secrets, áp dụng migration và tạo dữ liệu demo trong `ExamAppDocker`. Lần đầu nhập mật khẩu `sa`; nếu volume đã có, nhập đúng mật khẩu cũ. Mật khẩu SQL được lưu cục bộ trong `.local/docker-sql-password.txt` (đã bỏ qua trong Git). Tài khoản app mẫu nằm trong `.local/demo-credentials.txt`. Không tự chuyển dữ liệu từ SQL Express sang Docker.
2. **Chạy bằng Visual Studio:** mở `Exam Management Application.slnx`, đặt `Exam.Web` làm Startup Project, chọn profile `Exam.Web` rồi nhấn **F5**. Web mở tại [https://localhost:60012](https://localhost:60012). Mở Desktop từ PowerShell:

   ```powershell
   .\scripts\Start-Desktop.ps1 -ApiUrl 'https://localhost:60012'
   ```

   Những lần sau mở Docker Desktop và chạy `.\scripts\Start-SqlDocker.ps1` trước. Nếu chạy cả hai bằng PowerShell, mở hai terminal và chạy `.\scripts\Start-Web.ps1` trước, rồi `.\scripts\Start-Desktop.ps1`. Cả hai dùng `http://localhost:5180`.

   Script chạy client kiểm tra SQL container đang chạy. Khi mở Web bằng F5 hoặc `dotnet run`, Web kiểm tra endpoint Docker và kết nối database trước khi nhận request; cấu hình SQLEXPRESS hoặc DB không kết nối được sẽ chặn khởi động. Nếu dừng container khi Web đang chạy, các thao tác cần DB thất bại; Web không tự chuyển sang database khác.
3. **Đăng nhập:** quản trị viên dùng Desktop. Giảng viên và thí sinh dùng Web. Mã đợt thi demo được in khi seed cơ sở dữ liệu mới.

## Kiểm tra

```powershell
dotnet build '.\Exam Management Application.slnx' -m:1
.\scripts\Test-Local.ps1
```

`Test-Local.ps1` chạy test SQL trên DB Docker riêng từng ca. Chạy `dotnet test` trực tiếp mà thiếu `EXAM_TEST_CONNECTION` sẽ bỏ qua các ca SQL.

Để dừng SQL mà giữ dữ liệu: `docker compose -f team/docker/compose.sql.yml stop`. Chạy lại `Start-SqlDocker.ps1` để mở. Không xóa volume khi muốn giữ dữ liệu. Nếu shell có `ConnectionStrings__ExamDb` cũ, bỏ biến đó trước khi chạy; biến môi trường có thể ghi đè User Secrets.

[Tài liệu kỹ thuật](docs/README.md) · [Hợp đồng API](CONTRACT.md) · [Quyết định kiến trúc](DECISIONS.md)

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

Sau khi dữ liệu demo và thành viên lớp đã được tạo, thí sinh đăng nhập → **Lớp của tôi** để xem lớp được gán; vào thi bằng mã `THI-NV10` nếu đủ điều kiện. Không tự tham gia lớp bằng mã. QT03/QT04 là các task quản trị lớp/thành viên; trạng thái hoàn thành cần đối chiếu mã nguồn và kiểm thử thực tế.

Link draw.io sơ đồ: [drive.google.com/file/d/1SQcn9H-fTRdWMS4bJIXEEjlklIxWqY_b/view?usp=sharing](https://drive.google.com/file/d/1SQcn9H-fTRdWMS4bJIXEEjlklIxWqY_b/view?usp=sharing)

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
