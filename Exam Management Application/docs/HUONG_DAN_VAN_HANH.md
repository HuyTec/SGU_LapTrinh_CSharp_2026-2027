# Hướng dẫn vận hành hệ thống quản lý thi trắc nghiệm

Tài liệu dành cho người dựng dự án, người trình bày demo và người sử dụng. Các lệnh dưới đây chạy bằng **PowerShell 7 trên Windows**, tại thư mục gốc dự án.

## 1. Các thành phần cần chạy

| Thành phần | Vai trò | Cách truy cập |
|---|---|---|
| SQL Server | Lưu chung tài khoản, đề, đợt thi và kết quả | Mặc định instance `.\SQLEXPRESS`, database `ExamAppDb` |
| Exam.Web | Web thí sinh và API phục vụ Desktop | `http://localhost:5180` |
| Exam.Desktop | WinForms dành cho nhân sự | Cửa sổ ứng dụng Windows |

**Luôn khởi động Web/backend trước Desktop.** Desktop gọi API của Web, không kết nối SQL trực tiếp. Thí sinh dùng trình duyệt; quản trị viên, giảng viên và cán bộ quản lý thi dùng Desktop. `App1` là WinUI cũ được giữ tham khảo, không phải ứng dụng cần chạy.

## 2. Chuẩn bị lần đầu

Cần .NET SDK 10, PowerShell 7, SQL Server Express hoặc Developer đang hoạt động và mạng để restore NuGet lần đầu. Tài khoản Windows chạy lệnh cần quyền tạo database demo/test trên SQL Server. `sqlcmd` cần cho bước kiểm tra SQL và sao lưu/phục hồi; Python 3 chỉ cần khi chạy các script kiểm tra HTTP. Không cần Node.js hoặc Docker để sử dụng ứng dụng.

Mở PowerShell 7 và chuyển tới dự án:

```powershell
cd "E:\Project\LapTrinh_CSharp_2026-2027\Exam Management Application"
dotnet --version
```

Nếu đặt dự án ở thư mục khác, thay đường dẫn ở lệnh `cd`. Kiểm tra SQL Express khi cần:

```powershell
Get-Service -Name 'MSSQL$SQLEXPRESS'
sqlcmd -S '.\SQLEXPRESS' -E -C -Q "SELECT 1 AS ConnectionOK;"
```

Dựng cấu hình, database, dữ liệu mẫu và chạy kiểm thử:

```powershell
.\scripts\Initialize-Local.ps1 -SqlInstance '.\SQLEXPRESS' -RunTests
```

Script restore package/tool, build solution, áp dụng migration, seed dữ liệu và tạo cấu hình cục bộ bằng .NET User Secrets. Mật khẩu mẫu và khóa JWT được sinh cục bộ; script không xóa database đang có. Nên dùng database riêng cho dự án; trên instance khác truyền tên thực tế, ví dụ:

```powershell
.\scripts\Initialize-Local.ps1 -SqlInstance 'localhost' -Database 'ExamAppDemo' -RunTests
```

Chỉ dùng ví dụ này nếu SQL Server của bạn thực sự chạy ở `localhost`. Thay đổi tham số sẽ thay cấu hình kết nối Web của người dùng Windows hiện tại. Không cần chạy khởi tạo lại mỗi lần mở ứng dụng.

## 3. Mở Web và ứng dụng Desktop

**Terminal 1 — khởi động Web/API:**

```powershell
.\scripts\Start-Web.ps1
```

Giữ terminal này mở. Dùng các địa chỉ:

| Trang | Địa chỉ |
|---|---|
| Đăng nhập | [localhost:5180/Account/Login](http://localhost:5180/Account/Login) |
| Đăng ký tài khoản | [localhost:5180/Account/Register](http://localhost:5180/Account/Register) |
| Hướng dẫn trong Web | [localhost:5180/Help](http://localhost:5180/Help) |
| Đợt thi sau đăng nhập | [localhost:5180/Sessions](http://localhost:5180/Sessions) |

**Terminal 2 — mở WinForms:**

```powershell
cd "E:\Project\LapTrinh_CSharp_2026-2027\Exam Management Application"
.\scripts\Start-Desktop.ps1
```

Địa chỉ máy chủ ở màn đăng nhập Desktop mặc định là `http://localhost:5180`. Nếu cần đổi cổng, khởi động hai thành phần với địa chỉ tương ứng:

```powershell
# Terminal 1
.\scripts\Start-Web.ps1 -Port 5181

# Terminal 2
.\scripts\Start-Desktop.ps1 -ApiUrl 'http://localhost:5181'
```

Chỉ khởi động một Web trên mỗi cổng. Khi dùng cổng 5181, đổi địa chỉ trình duyệt và tham số `-BaseUrl` của script tạo demo theo cùng cổng.

## 4. Tài khoản và đăng ký

### 4.1. Tài khoản dữ liệu mẫu

| Tên đăng nhập | Vai trò | Nơi đăng nhập |
|---|---|---|
| `admin` | Quản trị viên | Desktop |
| `lecturer`, `lecturer2` | Giảng viên | Desktop |
| `officer`, `officer2` | Cán bộ quản lý thi | Desktop |
| `student`, `student2` | Thí sinh | Web |

Mật khẩu xem tại dòng `password=` trong tệp `.local/demo-credentials.txt`. Không đưa tệp này, khóa JWT hoặc tệp cấu hình chứa secret lên Git hay vào báo cáo. Tài khoản có nhiều vai trò nhân sự chọn vai trò hoạt động khi đăng nhập Desktop; lựa chọn này không cấp quyền mới.

### 4.2. Tự đăng ký trên Web

1. Chọn **Đăng ký tài khoản** ở trang đăng nhập, rồi chọn loại tài khoản.
2. Nhập tên đăng nhập, họ tên, email, mật khẩu và nhập lại mật khẩu. Chỉ tài khoản Thí sinh cần mã thí sinh do nhà trường cấp.
3. Mật khẩu từ 8 đến 100 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Tên đăng nhập tối đa 60 ký tự, họ tên 120, mã thí sinh 30, email 256. Tên đăng nhập và mã thí sinh không được trùng tài khoản đã có.
4. Chọn **Đăng ký tài khoản**; thành công sẽ trở về trang đăng nhập.
5. Thí sinh đăng nhập Web bằng tài khoản mới. Nếu chưa thấy đợt thi, liên hệ cán bộ để được phân công. Nhân sự cần chờ duyệt theo mục 4.3.

Đăng ký **Thí sinh** tạo tài khoản hoạt động nhưng không tự thêm lịch thi. Đăng ký **Giảng viên**, **Cán bộ quản lý thi** hoặc **Quản trị viên** tạo hồ sơ chờ duyệt; chưa có quyền và chưa đăng nhập được. Nếu đã được cấp tài khoản, đăng nhập trực tiếp; không tạo hồ sơ khác để thay tài khoản cũ. Quản trị viên có thể tạo tài khoản hoặc nhập danh sách thí sinh từ Desktop.

### 4.3. Duyệt đăng ký Giảng viên, Cán bộ hoặc Quản trị viên

1. Người đăng ký chọn đúng loại tài khoản trên Web, điền thông tin và gửi. Tài khoản nhân sự không cần mã thí sinh.
2. Quản trị viên hiện tại đăng nhập Desktop bằng tài khoản được cấp (máy demo có `admin`).
3. Mở **Tài khoản / thí sinh**, tìm tên đăng nhập và xem cột **Vai trò đăng ký**.
4. Chọn dòng → **Duyệt đăng ký** → xác nhận. Hệ thống cấp vai trò được đề nghị, kích hoạt tài khoản và xóa trạng thái chờ duyệt.
5. Người đăng ký mở Desktop, chọn vai trò đã được duyệt và đăng nhập bằng mật khẩu lúc đăng ký. Với giảng viên, quản trị viên cần phân công thêm môn để quản lý câu hỏi/đề.

Không dùng **Mở khóa** thay cho duyệt hồ sơ mới. Tài khoản quản trị mới cũng cần quản trị viên hiện tại duyệt; lần dựng đầu tiên dùng tài khoản `admin` do script khởi tạo tạo ra. Nếu cửa sổ Desktop đang mở từ trước khi cập nhật tính năng, đăng xuất/đóng và mở lại để thấy nút duyệt mới.

## 5. Quy trình sử dụng từ đầu đến cuối

### Bước 1 — Cán bộ tạo môn học

Đăng nhập Desktop bằng `officer` → **Danh mục môn học** → **Thêm** → nhập mã, tên và trạng thái sử dụng → lưu.

### Bước 2 — Quản trị viên chuẩn bị người dùng và phân công

Đăng nhập `admin` → **Tài khoản / thí sinh** để tạo/sửa tài khoản và gán vai trò. Với thí sinh, điền mã thí sinh duy nhất. Tài khoản tự đăng ký trên Web đã có trong cùng danh sách này.

Mở **Phân công giảng viên**, chọn giảng viên và môn đã tạo. Nếu giảng viên chưa được phân công, họ không được sửa ngân hàng câu hỏi của môn đó.

Khóa tài khoản, đặt lại mật khẩu hoặc thay đổi quyền có thể thu hồi phiên; người dùng cần đăng nhập lại. Để trống mật khẩu khi sửa tài khoản nếu muốn giữ mật khẩu cũ.

### Bước 3 — Giảng viên chuẩn bị đề

Đăng nhập `lecturer` và thực hiện:

1. **Chủ đề** → thêm chủ đề trong môn được phân công.
2. **Ngân hàng câu hỏi** → thêm nội dung, chủ đề, độ khó, bốn phương án khác nhau và chọn đúng một đáp án đúng.
3. **Xây dựng đề thi** → chọn môn, đặt tên đề, chọn câu hỏi và nhập điểm từng câu.
4. Kiểm tra tổng điểm bằng **10**, lưu đề và chọn **Xác nhận sẵn sàng**.

Đề/câu hỏi đã được sử dụng có thể bị khóa sửa. Nếu báo dữ liệu đã thay đổi, đóng biểu mẫu rồi mở lại để tải phiên bản mới. Ngừng sử dụng câu hỏi cũ không làm thay đổi nội dung và điểm của lượt thi đã tạo.

### Bước 4 — Cán bộ tổ chức đợt thi

Đăng nhập `officer`:

1. **Tổ chức đợt thi** → **Thêm** → chọn đề sẵn sàng, tên đợt, giờ mở/đóng, thời lượng, tùy chọn trộn câu/phương án và chính sách xem kết quả.
2. **Phân công thí sinh** → chọn đợt nháp và thí sinh. Hệ thống từ chối phân công trùng hoặc giao lịch.
3. Quay lại đợt thi, chọn đúng dòng → **Kích hoạt**.

Giờ nhập/hiển thị là giờ Việt Nam; máy chủ lưu và tính bằng UTC. Chỉ hoãn/hủy trước lần mở đầu và khi chưa có lượt thi. Kích hoạt khóa cấu hình; chuẩn bị đầy đủ trước thao tác này.

### Bước 5 — Thí sinh làm bài trên Web

1. Đăng nhập → **Đợt thi** → tìm đợt được phân công → **Xem chi tiết**.
2. Đọc lịch và hướng dẫn → **Bắt đầu làm bài** khi đợt đang mở.
3. Chọn một đáp án mỗi câu; chờ thông báo **Đã lưu tất cả thay đổi**. Nếu lưu lỗi, kiểm tra mạng và chọn **Thử lưu lại**.
4. Dùng đánh dấu để xem lại câu cần cân nhắc. Tải lại trang hoặc đăng nhập lại sẽ tiếp tục lượt cũ với dữ liệu đã lưu.
5. Chọn **Nộp bài** → **Xác nhận nộp bài**. Sau khi nộp không sửa được đáp án.

Mỗi thí sinh chỉ có một lượt trong mỗi đợt. Đóng trình duyệt hoặc mất mạng không dừng đồng hồ. Hạn làm bài là thời điểm sớm hơn giữa giờ bắt đầu cộng thời lượng và giờ đóng đợt; chỉ đáp án được máy chủ xác nhận trước hạn mới được chấm. Worker tự kết thúc lượt quá hạn, kể cả khi thí sinh đã đóng trang.

### Bước 6 — Công bố và xuất kết quả

Cán bộ mở **Theo dõi / kết quả** để xem trạng thái. Sau khi đợt đóng và tất cả lượt phát sinh đã chấm xong, chọn đợt → **Công bố kết quả**. Thí sinh tải lại **Lịch sử & kết quả** để xem điểm; mức xem bài/đáp án phụ thuộc chính sách công bố.

Nhân sự có quyền chọn đúng đợt hoặc dòng theo dõi → **Xuất CSV** hoặc **Xuất Excel** → chọn nơi lưu. Thí sinh chưa thi được phân biệt với người làm bài đạt 0 điểm.

## 6. Demo nhanh bằng dữ liệu có sẵn

Khi Web đang chạy, mở terminal khác:

```powershell
.\scripts\New-Demo.ps1 -Minutes 5
```

Script tạo một tài khoản thí sinh và đợt mới từ đề sẵn sàng, phân công rồi kích hoạt. Thông tin được ghi trong `.local/latest-demo.json`; mật khẩu dùng dòng `password=` của `.local/demo-credentials.txt`.

Đăng nhập Web bằng tài khoản script vừa tạo, làm bài và nộp. **Đợi đợt đóng**, sau đó dùng `officer` trên Desktop công bố và xuất kết quả. Nộp sớm không có nghĩa có thể công bố trước giờ đóng. Có thể dùng `-Minutes 10` để có thêm thời gian trình bày; mỗi lần chạy tạo dữ liệu mới, không dùng lại thời gian của đợt đã hết hạn.

Nếu đổi cổng:

```powershell
.\scripts\New-Demo.ps1 -BaseUrl 'http://localhost:5181' -Minutes 5
```

## 7. Nhập danh sách thí sinh

Đăng nhập `admin` → **Tài khoản / thí sinh** → **Nhập CSV thí sinh** → **Lưu mẫu CSV**. Header:

```csv
UserName,FullName,StudentCode,Email,Password
```

Điền dữ liệu thật trong tệp cục bộ, lưu UTF-8 rồi **Chọn file CSV UTF-8**. Giới hạn 500 thí sinh/1 MB. Kiểm tra bản xem trước, thực hiện kiểm tra trên máy chủ rồi xác nhận nhập. Nếu có dòng lỗi, sửa tệp và kiểm tra lại; hệ thống không nhập một phần hoặc ghi đè tài khoản có sẵn. Tệp này chứa mật khẩu, không gửi lên Git hoặc dùng làm ảnh minh họa báo cáo.

Đây là nhập **thí sinh CSV**; nhập ngân hàng câu hỏi Excel và sinh đề theo ma trận chưa được triển khai.

## 8. Dừng, cập nhật và mở lại

Kết thúc lượt thi/demo trước khi bảo trì. Đóng Desktop, sau đó nhấn `Ctrl+C` ở terminal Web. Dừng host giữa một đợt đang thi sẽ làm gián đoạn lưu đáp án; thời gian thi vẫn trôi.

Sau khi cập nhật mã, dừng các ứng dụng đang giữ DLL rồi chạy:

```powershell
dotnet restore '.\Exam Management Application.slnx'
dotnet build '.\Exam Management Application.slnx' --no-restore -m:1
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet tool restore
dotnet ef database update --project Exam.Data --startup-project Exam.Web --no-build
```

Mở Web và Desktop lại theo mục 3. Không cần chạy seed hoặc tạo database mới khi chỉ khởi động hằng ngày. Không xóa database để sửa lỗi đăng nhập hoặc lỗi build.

## 9. Kiểm thử và sao lưu

Kiểm thử xUnit với SQL Server thật:

```powershell
.\scripts\Test-Local.ps1 -SqlInstance '.\SQLEXPRESS'
```

Mỗi ca SQL dùng database riêng `ExamAppTests_<GUID>` và chỉ dọn database của ca đó. Nếu tự chạy `dotnet test` mà không cấp `EXAM_TEST_CONNECTION`, các ca SQL có thể bị **Skipped**; không xem kết quả đó là đã kiểm tra database.

Kiểm tra HTTP khi Web đang chạy, nếu máy đã có Python 3:

```powershell
python .\scripts\registration_http.py
python .\scripts\smoke_http.py
```

Các script HTTP tạo dữ liệu mới trong database đang cấu hình; chỉ chạy trên database demo/test. Bằng chứng kiểm tra nằm trong `artifacts/integration`, `artifacts/tests`, `artifacts/web` và `artifacts/desktop`. Kết quả đã thực hiện cùng giới hạn kiểm tra xem [REPORT.md](../REPORT.md).

Trước demo cuối, khi không có người đang ghi dữ liệu, kiểm tra sao lưu/phục hồi:

```powershell
.\scripts\Backup-Restore-Check.ps1 -SqlInstance '.\SQLEXPRESS' -Database 'ExamAppDb'
```

Thay database nếu đã cấu hình tên khác. Script dùng `sqlcmd`, backup COPY_ONLY có checksum, phục hồi sang database riêng `ExamAppRestore_<GUID>`, đối chiếu số người dùng và chỉ xóa database phục hồi đó. SQL Server service cần quyền đọc/ghi thư mục `.local/backups` trên máy chạy script; quy trình này dành cho SQL Server local của bản demo.

Tệp `.local/backups/ExamAppDb-verification.bak` được giữ lại nhưng **bị ghi đè ở lần kiểm tra kế tiếp**; sao chép thành bản có ngày giờ nếu cần lưu nhiều phiên. Script không thay database nguồn bằng bản backup. Kết quả kiểm tra ở `artifacts/integration/backup-restore.txt`.

## 10. Xử lý lỗi thường gặp

| Hiện tượng | Kiểm tra và cách xử lý |
|---|---|
| PowerShell báo không cho chạy `.ps1` | Mở PowerShell 7. Nếu execution policy chặn script local đã kiểm tra, dùng `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass` cho terminal hiện tại rồi chạy lại; thiết lập này không đổi policy toàn máy. |
| Desktop không kết nối máy chủ | Khởi động Web trước, mở trang Login bằng trình duyệt, đối chiếu địa chỉ/cổng ở Desktop. |
| Cổng 5180 đã được dùng | Kiểm tra terminal Web đang chạy; không mở host thứ hai trên cùng cổng. Hoặc dùng cổng khác theo mục 3. |
| SQL không kết nối | Kiểm tra service SQL, instance và quyền Windows; không tự chuyển provider sang SQLite. |
| Thiếu connection string hoặc khóa JWT | Chạy khởi tạo lần đầu bằng đúng tài khoản Windows; User Secrets của tài khoản Windows khác không tự dùng chung. |
| DLL bị khóa khi build | Đóng Desktop, dừng Web bằng `Ctrl+C`, build rồi mở lại. |
| Không tìm thấy migration mới | Build trước khi chạy `database update --no-build`. Không dùng DLL cũ sau khi vừa thêm migration. |
| Không thấy môn ở tài khoản giảng viên | Quản trị viên kiểm tra phân công giảng viên–môn; tải lại hoặc đăng nhập lại. |
| Vừa tạo chủ đề nhưng chưa thấy trong danh sách chọn | Bấm Làm mới hoặc chuyển lại màn Ngân hàng câu hỏi; bản hiện hành tải lại danh mục từ máy chủ. Kiểm tra chủ đề thuộc môn đã phân công và bộ lọc đang chọn. Nếu ứng dụng đang mở bằng bản cũ, đóng rồi chạy lại Start-Desktop.ps1 để nhận bản sửa. |
| Thí sinh mới không thấy đợt thi | Cán bộ phải thêm phân công; kiểm tra đúng tài khoản và trạng thái đợt. |
| Không bắt đầu được | Kiểm tra lịch mở/đóng, phân công, đợt đã kích hoạt và lượt thi đã dùng hay chưa. |
| Lưu thất bại | Kiểm tra mạng, đọc thông báo, thử lưu lại trước hạn; không giả định đáp án trên màn hình đã lên máy chủ. |
| Không công bố được | Đợi giờ đóng đợt và worker chấm hết lượt; làm mới danh sách. |
| Không thấy điểm | Bài đã nộp có thể chưa được công bố; cán bộ kiểm tra đúng đợt. |
| Tài khoản bị khóa hoặc phiên hết hạn | Đăng nhập lại nếu hết hạn; liên hệ quản trị viên nếu bị khóa. Không tạo tài khoản trùng mã thí sinh. |
| Form báo dữ liệu đã thay đổi | Đóng Form, mở lại và sửa trên dữ liệu mới để tránh ghi đè thay đổi của người khác. |

## 11. Phạm vi môi trường

Các lệnh trên phục vụ chạy local/demo. HTTP ở `localhost` không phải cấu hình triển khai công khai. Khi chuyển sang máy chủ hoặc cho nhiều máy truy cập, cần cấu hình HTTPS, địa chỉ host, kết nối SQL, quyền mạng và secret theo môi trường; `localhost` trên mỗi máy luôn chỉ chính máy đó. Chưa coi kết quả tải loopback là bằng chứng tải qua LAN/Internet.

Trong ứng dụng luôn có nút **Hướng dẫn** để xem quy trình theo vai trò. Tài liệu liên quan: [README](../README.md), [hợp đồng API](../CONTRACT.md), [lược đồ SQL](SCHEMA.md), [yêu cầu dự án](YEU_CAU_DU_AN_DE_TAI_18.md).
