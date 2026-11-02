# Hệ thống quản lý thi trắc nghiệm

Báo cáo tổng hợp demo ngày 24/10/2026, vị trí mã nguồn, vận hành và cải thiện: [xem báo cáo](docs/BAO_CAO_DEMO_2026-10-24.md).

WinForms cho nhân sự quản trị, ASP.NET Core MVC/Razor/Bootstrap cho thí sinh; cả hai dùng một backend và một CSDL SQL Server. Phạm vi đã duyệt: chức năng bắt buộc, xUnit và xuất kết quả CSV/XLSX cơ bản. Giao diện ưu tiên thao tác rõ ràng, dễ dùng.

Hướng dẫn đầy đủ từ khởi tạo, mở ứng dụng, đăng ký, tổ chức thi đến sao lưu và xử lý lỗi: [HƯỚNG DẪN VẬN HÀNH](docs/HUONG_DAN_VAN_HANH.md).

## Dựng trên Windows

Yêu cầu: .NET SDK 10, SQL Server Express/Developer, PowerShell, kết nối NuGet cho lần restore đầu. Khi dùng máy đã kiểm tra, instance là `.\SQLEXPRESS`; trên máy khác truyền đúng instance của bạn. SQL login Windows hiện tại cần được phép tạo DB demo/test. Không cần Docker hoặc Node.js để chạy ứng dụng.

Tại thư mục solution:

```powershell
.\scripts\Initialize-Local.ps1 -SqlInstance '.\SQLEXPRESS' -RunTests
```

Lệnh tạo cấu hình bằng .NET User Secrets, sinh khóa JWT/mật khẩu demo ngẫu nhiên cục bộ, build, áp dụng migrations, seed và chạy kiểm thử SQL cách ly. Không xóa DB đang có. Dùng tên DB dành riêng cho dự án, mặc định `ExamAppDb`.

Mật khẩu chỉ nằm ở `.local/demo-credentials.txt` và User Secrets, không commit. Các tài khoản mẫu: `admin`, `lecturer`, `lecturer2`, `officer`, `officer2`, `student`, `student2`. Hậu tố `2` hỗ trợ kiểm tra phân quyền ngoài phạm vi. Dữ liệu bài thi đã làm vẫn được giữ khi chạy seed lại.

## Chạy và demo

Người dùng mới mở [Đăng ký tài khoản](http://localhost:5180/Account/Register), chọn loại tài khoản, nhập tên đăng nhập, họ tên, email, mật khẩu và xác nhận mật khẩu; mã thí sinh chỉ bắt buộc khi chọn Thí sinh. Mật khẩu từ 8 đến 100 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Tên đăng nhập/mã thí sinh không được trùng. Thí sinh đăng nhập Web và cần được cán bộ phân công đợt thi. Giảng viên, cán bộ và quản trị viên đăng ký sẽ chờ duyệt: quản trị viên hiện tại mở Desktop → Tài khoản / thí sinh → chọn tài khoản → Duyệt đăng ký. Sau khi được duyệt, nhân sự đăng nhập Desktop. Nếu đã được cấp tài khoản, đăng nhập trực tiếp để tránh tạo trùng hồ sơ.

Mở **Hướng dẫn sử dụng** ở thanh điều hướng Web (có cả trước và sau đăng nhập), hoặc [trang hướng dẫn](http://localhost:5180/Help). Desktop có **Hướng dẫn** ở màn đăng nhập và menu sau đăng nhập, nội dung theo vai trò; **Đăng ký tài khoản trên Web** mở trang đăng ký theo địa chỉ máy chủ đã nhập. Loại tài khoản nhân sự chọn trên Web là yêu cầu đăng ký, chưa cấp quyền cho đến khi quản trị viên duyệt.

Terminal thứ nhất:

```powershell
.\scripts\Start-Web.ps1
```

Mở [Web thí sinh](http://localhost:5180). Terminal thứ hai:

```powershell
.\scripts\Start-Desktop.ps1
```

Nhân sự dùng Desktop; thí sinh chỉ đăng nhập Web. Tài khoản có nhiều vai trò chọn vai trò hoạt động khi đăng nhập Desktop. Chọn “Tự động” khi tài khoản chỉ có một vai trò nhân sự. Thay đổi quyền, khóa tài khoản và đặt lại mật khẩu thu hồi các phiên đang tồn tại.

Để có một đợt mới mở ngay, không phải sửa lịch dữ liệu mẫu:

```powershell
.\scripts\New-Demo.ps1 -Minutes 5
```

Script cấp một thí sinh demo mới và đợt thi từ đề sẵn sàng, rồi ghi tên đăng nhập/ID đợt ở `.local/latest-demo.json`. Đăng nhập Web bằng tài khoản vừa tạo; làm bài, đánh dấu, tải lại và nộp. Sau 5 phút, đăng nhập Desktop bằng `officer`, chọn đúng đợt và **Công bố**; Web mới được hiện điểm và đáp án. Xuất CSV hoặc XLSX từ đợt đó. Không công bố sớm khi đợt còn mở.

Luồng tự chuẩn bị dữ liệu bằng Desktop: `officer` tạo môn → `admin` tạo tài khoản/thí sinh và phân công giảng viên → `lecturer` tạo chủ đề/câu hỏi, chọn câu và điểm từng câu, xác nhận đề sẵn sàng → `officer` tạo đợt nháp/phân công/kích hoạt → thí sinh làm bài trên Web → cán bộ công bố và xuất kết quả.

CSV nhập thí sinh dùng header `UserName,FullName,StudentCode,Email,Password`; có bước xem trước/kiểm tra rồi xác nhận. Tất cả dòng phải hợp lệ, không nhập một phần hoặc ghi đè tài khoản cũ. Tệp thật có mật khẩu phải được bảo quản cục bộ; không đưa vào Git. Excel câu hỏi và ma trận đề chưa thuộc phạm vi triển khai này.

## Kiến trúc

| Project | Trách nhiệm |
|---|---|
| Exam.Desktop | WinForms, ApiClient JWT, biểu mẫu và điều hướng |
| Exam.Web | MVC/Razor cho thí sinh và `/api/admin` cho Desktop |
| Exam.Services | Quyền/phạm vi, validation, tổ chức thi, snapshot, lưu/nộp/chấm, nhập/xuất |
| Exam.Data | EF Core, SQL Server, Identity và migrations |
| Exam.Core | DTO và các kiểu dùng chung |
| Exam.Tests | xUnit, kiểm thử trên DB SQL Server riêng cho từng ca |

Identity ánh xạ vào `Users`, `Roles`, `UserRoles`, khóa người dùng/vai trò là int. Các FK nghiệp vụ tham chiếu cùng người dùng; bảng kỹ thuật Identity và `AuthSessions` phục vụ xác thực/thu hồi phiên. Controller, Form và JavaScript không chấm điểm hoặc kết nối SQL trực tiếp. `App1` WinUI cũ được giữ nguyên để tham khảo và không thuộc solution chạy mới.

## Kiểm tra

```powershell
dotnet build '.\Exam Management Application.slnx' -m:1
.\scripts\Test-Local.ps1
.\scripts\Backup-Restore-Check.ps1
```

Khi không cấp `EXAM_TEST_CONNECTION`, ca SQL được ghi **Skipped**, không được xem là đã kiểm thử DB. `Test-Local.ps1` cấp cấu hình này; mỗi ca tạo một DB `ExamAppTests_<GUID>` riêng và chỉ dọn DB do chính ca đó tạo.

HTTP nghiệm thu trên host đang chạy có script `scripts/smoke_http.py` (Python 3, chỉ thư viện chuẩn). Script tạo dữ liệu mang tên riêng và kiểm tra tạo đề/đợt, làm bài, công bố, CSV/XLSX, phân quyền, CSRF, thu hồi phiên, CSV nhập và nhiều vai trò. Bằng chứng và các giới hạn được ghi ở [REPORT.md](REPORT.md), `artifacts/web`, `artifacts/desktop`, `artifacts/integration`.

Script triển khai SQL có thể tái chạy: `docs/schema.sql`. Nguồn thực tế của lược đồ là EF Core migrations trong `Exam.Data/Migrations`; bảng ánh xạ tên cột và 3NF xem `docs/SCHEMA.md`. Không tự đổi provider sang SQLite hoặc tạo hai DB cho hai client.

## Vận hành và xử lý lỗi

- HTTP chỉ dùng loopback khi demo Development. Ngoài môi trường local cần HTTPS, cấu hình host/bindings, khóa ký riêng và tài khoản SQL có quyền tối thiểu. Không triển khai credential demo lên mạng công cộng.
- Đợt và lượt thi dùng UTC phía máy chủ; giao diện hiển thị giờ Việt Nam. Mất mạng không gia hạn; chỉ đáp án server xác nhận mới được chấm. Worker quét hết hạn mỗi 5 giây và lúc khởi động.
- Nếu API không kết nối: khởi động Web trước và kiểm tra `http://localhost:5180`; đổi địa chỉ trong màn đăng nhập Desktop nếu dùng port khác.
- Nếu báo lỗi SQL: kiểm tra instance, quyền Windows và SQL Server đang chạy; chạy lại Initialize-Local với instance đúng.
- Nếu báo lỗi migration chưa có trong assembly: build solution trước rồi chạy `dotnet ef database update --project Exam.Data --startup-project Exam.Web`.
- Khi rebuild báo file DLL bị khóa: dừng Web/Desktop bằng Ctrl+C hoặc đóng ứng dụng, build rồi mở lại.
- Trước demo cuối: sao lưu SQL Server, thử phục hồi trên DB riêng; lưu hình chụp và giải thích phần việc từng thành viên. Chỉ báo hiệu năng đạt khi có số đo tải thật.

Quyết định: [DECISIONS.md](DECISIONS.md). Hợp đồng route/DTO: [CONTRACT.md](CONTRACT.md). Yêu cầu: [docs/YEU_CAU_DU_AN_DE_TAI_18.md](docs/YEU_CAU_DU_AN_DE_TAI_18.md). Tài liệu tham khảo/thư viện: [docs/README.md](docs/README.md).
