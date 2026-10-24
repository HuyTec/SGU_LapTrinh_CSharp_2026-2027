# Hướng dẫn làm việc chung - Exam Management Application

## Trạng thái hiện tại

Repository hiện có **WinUI 3 desktop application** (`ExamApp`) và dự án đóng gói
MSIX (`ExamApp (Package)`). Đây là client quản trị. Web, API, thư viện nghiệp vụ
chung, migration và SQL Server schema chưa được tạo; chúng sẽ được thêm sau khi
team chốt thiết kế.

Đọc `AGENTS.md` trước khi thêm chức năng. Đồ án cuối cùng phải có Desktop quản
trị, Web cho người dùng và Microsoft SQL Server dùng chung dữ liệu nghiệp vụ.

## 1. Cài môi trường trên Windows

Cài các thành phần sau trên từng máy phát triển:

1. **Visual Studio 2022 hoặc mới hơn** với workload **.NET desktop development**.
2. Thành phần **Windows application packaging project** / **Desktop Bridge**.
   Nó cần thiết để build project `ExamApp (Package)`; thiếu thành phần này sẽ
   báo lỗi `Microsoft.DesktopBridge.props`.
3. **Windows 10 SDK** phiên bản 10.0.19041 trở lên.
4. **.NET 8 SDK**.
5. **SQL Server Express, Developer, hoặc LocalDB** và SQL Server Management
   Studio (SSMS) khi bắt đầu phần cơ sở dữ liệu.

Kiểm tra SDK:

```powershell
dotnet --list-sdks
```

## 2. Clone và mở solution

```powershell
git clone <repository-url>
cd "Exam Management Application/ExamApp"
dotnet restore ExamApp.slnx
```

Mở `ExamApp.slnx` bằng Visual Studio. Chọn cấu hình `Debug | x64`, đặt
`ExamApp (Package)` làm Startup Project, rồi nhấn `F5` để chạy bản desktop đóng
gói.

Nếu chỉ cần kiểm tra code desktop không đóng gói:

```powershell
dotnet build .\ExamApp\ExamApp.csproj
```

Lệnh build solution qua .NET CLI chỉ chạy được sau khi đã cài Desktop Bridge:

```powershell
dotnet build .\ExamApp.slnx
```

## 3. Cấu hình cục bộ và secrets

Không commit mật khẩu SQL Server, connection string thật, API key, certificate
hay file cấu hình môi trường cá nhân.

Khi API được tạo, dùng `appsettings.Development.json` (đã bị ignore) hoặc .NET
User Secrets. Mỗi thành viên tự tạo cấu hình local từ một file mẫu đã commit,
ví dụ `appsettings.Development.example.json`.

Mẫu connection string khi dùng SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=ExamAppDb;Trusted_Connection=True;TrustServerCertificate=True
```

## 4. Quy trình Git của team

1. Đồng bộ nhánh chính: `git pull origin main`.
2. Tạo nhánh theo phần việc: `git switch -c feature/<ten-ngan-gon>`.
3. Chỉ commit các thay đổi thuộc phần việc của mình; không commit `bin/`, `obj/`,
   `.vs/`, file database local hay secrets.
4. Build phần thay đổi trước khi commit.
5. Push nhánh và tạo Pull Request để một thành viên khác review trước khi merge.

Commit message nên theo Conventional Commits:

```text
feat: add exam creation flow
fix: validate question score
docs: add database setup guide
```

## 5. Kiểm tra trước khi gửi Pull Request

```powershell
dotnet build .\ExamApp\ExamApp.csproj
git status
git diff --check
```

Khi Web/API/SQL Server đã có, PR liên quan nghiệp vụ phải bổ sung bằng chứng
rằng desktop và web đọc/ghi cùng dữ liệu SQL Server, cùng với kiểm tra quyền và
luồng nghiệp vụ chính.

## Troubleshooting

| Vấn đề | Nguyên nhân thường gặp | Cách xử lý |
| --- | --- | --- |
| Thiếu `Microsoft.DesktopBridge.props` | Chưa cài Desktop Bridge/packaging tools | Cài thành phần ở mục 1, rồi mở và build lại solution bằng Visual Studio. |
| Build app project thành công nhưng package thất bại | Packaging workload hoặc Windows SDK thiếu | Kiểm tra Visual Studio Installer và Windows SDK. |
| Không kết nối được SQL Server | Instance/connection string local khác nhau | Kiểm tra instance bằng SSMS và dùng local configuration, không sửa config chung. |
| Git báo `dubious ownership` trong môi trường sandbox | Khác Windows user ownership | Dùng Visual Studio hoặc Git của máy chủ; không thay đổi global Git config của người khác. |

## Khi bổ sung solution sau thiết kế

Tạo các project Web/API/domain/data theo thiết kế đã duyệt, cập nhật solution,
thêm hướng dẫn chạy tương ứng vào tài liệu này, và giữ `AGENTS.md` đồng bộ với
quyết định kiến trúc.
