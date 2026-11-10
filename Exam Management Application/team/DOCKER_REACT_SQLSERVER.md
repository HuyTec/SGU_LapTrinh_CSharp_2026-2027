# Kết nối Docker, ASP.NET Core, React và SQL Server

Trạng thái 10/11/2026: Docker Engine chưa truy cập được; React chưa có trong repository. Hướng dẫn dưới đây chưa được kiểm chứng chạy container. Không thay thế ứng dụng MVC/WinForms đã duyệt. Thời gian người thực hiện dự kiến: 20–40 phút nếu Docker hoạt động và tải image bình thường; chưa tính xây API React còn thiếu.

## Bước 1 — Khởi động Docker

Mở Docker Desktop, chọn Linux containers, chờ engine chạy. Tại thư mục solution:

```powershell
docker version
docker info --format '{{.OSType}}'
docker compose version
```

Chỉ tiếp tục khi có Server và OSType là `linux`. Lỗi pipe `dockerDesktopLinuxEngine` hiện gặp nghĩa là client chưa nối được engine; cài thêm npm package không giải quyết lỗi này. Nếu Docker Desktop báo WSL/virtualization, xử lý lỗi cụ thể trong Docker trước.

## Bước 2 — Chạy SQL riêng cho nhóm

File `team/docker/compose.sql.yml` dùng SQL Server 2022 Developer, chỉ cho phát triển. Cổng host 14333 tránh trùng SQL Server local 1433. Named volume giữ dữ liệu khi thay container. Tag `2022-latest` có thể thay đổi; khi nhóm chốt bản demo, ghi image digest để dựng lại cùng bản.

Nhập một mật khẩu mạnh mới, không dùng mật khẩu thật của hệ thống khác. Chạy trong cùng terminal PowerShell 7:

```powershell
$env:EXAM_SQL_PASSWORD = Read-Host 'Mat khau SQL local (manh, toi thieu 8 ky tu)' -MaskInput
docker compose -f team/docker/compose.sql.yml config --quiet
docker compose -f team/docker/compose.sql.yml up -d sql
docker compose -f team/docker/compose.sql.yml logs --tail 50 sql
```

Chờ log `SQL Server is now ready for client connections`. Không chia sẻ output `docker inspect` hoặc `compose config` đầy đủ vì có thể chứa secret. Compose chỉ tạo SQL, chưa tạo schema ứng dụng. Không chạy `down -v` nếu cần giữ dữ liệu.

## Bước 3 — Kết nối backend C# chạy trên Windows

Trong terminal trên, đặt cấu hình chỉ cho tiến trình con; không thay User Secrets SQL Express đang có. Dùng SqlConnectionStringBuilder để tránh lỗi mật khẩu chứa ký tự đặc biệt:

```powershell
Add-Type -AssemblyName System.Data
$examConnection = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$examConnection['Data Source'] = 'localhost,14333'
$examConnection['Initial Catalog'] = 'ExamAppTeamDocker'
$examConnection['User ID'] = 'sa'
$examConnection['Password'] = $env:EXAM_SQL_PASSWORD
$examConnection['Encrypt'] = $true
$examConnection['TrustServerCertificate'] = $true
$env:ConnectionStrings__ExamDb = $examConnection.ConnectionString
$env:Jwt__Key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:Seed__Password = Read-Host 'Mat khau tai khoan demo (hoa/thuong/so/ky tu dac biet)' -MaskInput
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet tool restore
dotnet build 'Exam Management Application.slnx' -m:1
dotnet ef database update --project Exam.Data --startup-project Exam.Web --no-build
dotnet run --project Exam.Web --no-build --no-launch-profile -- --seed
dotnet run --project Exam.Web --no-build --no-launch-profile --urls http://localhost:5180
```

Chạy từng lệnh; nếu lệnh lỗi thì dừng ở đó. Migration tác động DB mới `ExamAppTeamDocker`; không đổi sang DB dùng chung/production. `sa` chỉ dùng bootstrap local ở hướng dẫn này; triển khai thật cần tài khoản quyền tối thiểu và quản lý secret/TLS riêng. `TrustServerCertificate=True` chỉ phục vụ local.

Mở `http://localhost:5180`; terminal khác chạy `scripts/Start-Desktop.ps1`, địa chỉ API `http://localhost:5180`. Tài khoản seed: admin/lecturer/officer/student với mật khẩu vừa nhập. Giữ JWT key ổn định trong terminal đang chạy; đổi key làm token cũ hết hiệu lực.

Không dùng `Initialize-Local.ps1 -SqlInstance localhost,14333` cho cách này: script hiện dùng Windows Integrated Security, còn SQL container ở đây dùng SQL login. Mở terminal mới phải cấp lại biến môi trường; không dán secret vào chat.

## Bước 4 — Nối React sau khi có đúng source

**Chưa thực hiện:** phải xác định thư mục React, lockfile, phiên bản Node và API cần gọi. Không tạo frontend mới chỉ để khớp hướng dẫn. Chạy React trên host trước, sau đó container hóa khi luồng HTTP đã hoạt động.

Với dự án React/Vite đã có, cấu hình proxy mẫu (ghép vào cấu hình hiện có, không ghi đè plugins):

```ts
server: {
  host: '0.0.0.0',
  port: 5173,
  strictPort: true,
  proxy: {
    '/api': { target: process.env.EXAM_API_PROXY ?? 'http://localhost:5180' }
  }
}
```

Trình duyệt gọi `fetch('/api/admin/catalog', {headers: {Authorization: 'Bearer ' + token}})`. `/api/auth/login` trả JWT nhân sự; không dùng cho Student. Không hard-code token, không lưu mật khẩu, không để SQL secret trong biến `VITE_*`. Nếu mở rộng auth sang React phải ghi lại hợp đồng và cách quản lý token trước.

Proxy chỉ chuyển HTTP request, không biến route Razor thành JSON. SPA thí sinh vẫn cần API JSON phù hợp; nếu dùng cookie thì phải cấp/gửi antiforgery token và giữ xác thực phía server. Không bật CORS wildcard để chữa sai đường dẫn.

Sau khi xác định đúng thư mục React và đã có `package-lock.json`, có thể đặt compose dev **trong thư mục React đó**. Mẫu này chưa phải file chạy của repository hiện tại; thay `<NODE_MAJOR_FROM_PROJECT>` bằng major phù hợp `engines`/lockfile:

```yaml
services:
  react:
    image: node:<NODE_MAJOR_FROM_PROJECT>-bookworm-slim
    working_dir: /app
    command: sh -c "npm ci && npm run dev -- --host 0.0.0.0"
    ports:
      - "127.0.0.1:5173:5173"
    environment:
      EXAM_API_PROXY: http://host.docker.internal:5180
    volumes:
      - .:/app
      - react_modules:/app/node_modules
volumes:
  react_modules:
```

Với Vite trong container và API trên Windows, API phải nghe interface mà container truy cập được. Dùng `--urls http://0.0.0.0:5180` **chỉ trên máy phát triển với firewall giới hạn mạng tin cậy**, hoặc cấu hình HTTPS đúng; bind loopback `localhost` có thể không đủ. Không mở firewall Internet. Nên ưu tiên React chạy host ở bước đầu để tránh thêm vấn đề mạng khi debug.

| Nơi request phát sinh | Đích đúng |
|---|---|
| Browser → Vite | localhost:5173, dùng URL tương đối `/api` |
| Vite chạy host → API host | localhost:5180 |
| Vite container → API host Docker Desktop | host.docker.internal:5180, API bind phù hợp |
| API host → SQL container | localhost,14333 |
| API container → SQL cùng compose (nếu bổ sung sau) | sql,1433 |

`localhost` bên trong container là chính container đó. Browser không resolve tên service `sql`/`api` của Docker như container cùng network.

## Bước 5 — Nghiệm thu kết nối

1. Desktop tạo dữ liệu có tên riêng → Web nhìn thấy dữ liệu phù hợp quyền trong cùng `ExamAppTeamDocker`.
2. Kiểm tra đăng nhập/sai mật khẩu, 401/403, giới hạn môn/đợt và dữ liệu validation; ghi status thật.
3. Làm bài → lưu → tải lại → nộp → cán bộ công bố sau khi đóng đợt → xem/xuất kết quả.
4. Nếu có React: DevTools Network phải thấy request tới API đúng và JSON/ProblemDetails đúng; ảnh tab không chứng minh DB đã lưu.
5. Chạy SQL tests cách ly với `EXAM_TEST_CONNECTION` phù hợp trong terminal riêng; mỗi test tạo DB `ExamAppTests_<GUID>`. Lưu bằng chứng vào thư mục local không chứa secret. Dừng SQL bằng `docker compose -f team/docker/compose.sql.yml stop`; volume vẫn giữ.

Tài liệu chính thức đã đối chiếu: [SQL Server Docker](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/quickstart-install-docker?view=sql-server-ver17), [Vite server/proxy](https://vite.dev/config/server-options.html), [Docker Desktop networking](https://docs.docker.com/desktop/features/networking/).
