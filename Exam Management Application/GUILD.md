# Hướng dẫn làm việc chung

Sản phẩm hiện hành là Exam.Desktop (WinForms) và Exam.Web (MVC/API), dùng chung Exam.Services/Exam.Data và SQL Server. App1 WinUI được giữ làm tài liệu lịch sử. Quyết định đã duyệt xem DECISIONS.md.

## Dựng và chạy

Đọc README.md. Cài .NET SDK 10 và SQL Server; mỗi thành viên tự chạy scripts/Initialize-Local.ps1 với instance của mình. Không chia sẻ hoặc commit .local, User Secrets, mật khẩu thí sinh, JWT key hoặc connection string thật.

Khởi động Web bằng scripts/Start-Web.ps1; mở Desktop bằng scripts/Start-Desktop.ps1. scripts/New-Demo.ps1 tạo đợt demo mới và tài khoản thí sinh riêng. Chỉ dùng HTTP trên loopback của máy phát triển; môi trường khác cần HTTPS.

## Phân chia kỹ thuật

| Phần | Quyền sửa |
|---|---|
| Backend | Core/Data/Services và controller/auth/worker của Web |
| Desktop | Exam.Desktop, client API, Form và control |
| Web giao diện | Views/wwwroot của Exam.Web |
| Tích hợp | Solution, kiểm thử, migrations/script, README/REPORT và tài liệu |

Mọi thay đổi route/DTO phải cập nhật CONTRACT.md và thông báo hai client. Không đưa chấm điểm, kiểm quyền hoặc truy vấn SQL vào Form/JavaScript. Chọn một vai trò hoạt động mỗi phiên; tài khoản có thể được gán nhiều vai trò.

## Kiểm tra và Git

Build solution với -m:1; chạy scripts/Test-Local.ps1 trên SQL Server thật. Dừng host đang chạy khi cần rebuild DLL bị khóa. Phân biệt build, DB, API, render giao diện và thao tác OS; không gọi kiểm tra chưa chạy là đạt.

Tạo nhánh codex/<ten-ngan-gon> hoặc nhánh nhóm đã thống nhất; commit theo Conventional Commits. Không tự hoàn nguyên thay đổi của thành viên khác. PR nghiệp vụ cần bằng chứng Desktop/API/SQL/Web hoặc chỉ rõ phần chưa kiểm chứng.

Báo cáo mỗi thành viên phải ghi phần thực sự thực hiện và giải thích được; bảng phân công kỹ thuật này không thay thế tên/đóng góp thực tế của 4 sinh viên.
