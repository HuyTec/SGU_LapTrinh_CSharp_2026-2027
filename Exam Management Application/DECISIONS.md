# Quyết định triển khai đã duyệt

Ngày xác nhận: 24/10/2026 (Asia/Saigon). Người dùng duyệt `1a, 2a 3a 4a 5a` sau cổng rà soát.

| Mục | Quyết định | Lý do |
|---|---|---|
| Nguồn yêu cầu | `docs/YEU_CAU_DU_AN_DE_TAI_18.md` là tiêu chí nghiệm thu chính; đặc tả 3.2 chi tiết nghiệp vụ, ưu tiên hơn 3.3 | Theo chỉ định trực tiếp của người dùng |
| Desktop | Tạo Exam.Desktop WinForms; giữ nguyên App1 WinUI cũ | Đúng lựa chọn nhóm, dễ dựng và giải thích |
| Web/backend | Một host Exam.Web chứa MVC/Razor/Bootstrap và Web API; Exam.Core, Exam.Data, Exam.Services dùng chung | Không nhân đôi nghiệp vụ; Desktop chỉ gọi API |
| Nền tảng | .NET 10, EF Core 10; SQL Server Express đang có tại `.\SQLEXPRESS` | Môi trường đã kiểm tra |
| Identity | Ánh xạ vào Users, Roles, UserRoles với khóa int và tên khóa UserID/RoleID | Dùng chính người dùng của mô hình 3.3, không tạo tài khoản nghiệp vụ song song |
| Xác thực | Cookie cho Web; JWT có hạn cho Desktop; kiểm tra tài khoản và phiên ở máy chủ, có thu hồi phiên | Đăng xuất, khóa tài khoản phải có hiệu lực |
| UC-17 | Cán bộ quản lý thi quản lý môn học | Theo bảng quyền 3.1.3 và UC chi tiết |
| CSDL | Cho phép tạo schema/migration, các trường bổ sung trong mục 10 và bảng kỹ thuật Identity/phiên; dùng DB demo/test mới | Đã duyệt tại câu 2 và câu 5 |
| Phạm vi | Bắt buộc + xUnit + xuất Excel/CSV cơ bản; một đáp án đúng, một lượt mỗi đợt, thang 10 | Ưu tiên chạy được trong quỹ 2–4 tuần |
| Hoãn | Ma trận đề, Excel câu hỏi, giám sát mất kết nối, hồ sơ/đổi mật khẩu cá nhân nâng cao | Không làm chậm luồng chính |
| Giao diện | Đơn giản, sáng, rõ nhãn/lỗi/trạng thái; Web từ 360 px; WinForms control chuẩn | Yêu cầu trực tiếp của người dùng |
| Triển khai | Cho phép 3 tác nhân sửa mã, thêm project/package và cập nhật tài liệu theo các quyết định này | Duyệt câu 5; kết thúc giai đoạn chỉ đọc |

App1 được giữ làm mã tham khảo, không phải sản phẩm WinForms mới. Không xóa hay ghi đè CSDL có sẵn. Dữ liệu mẫu phải được cấp qua cấu hình cục bộ, không commit mật khẩu/khóa ký/connection string thật. Chỉ ghi nhận build, DB, API, giao diện, hiệu năng là đạt khi có kiểm tra thực tế tương ứng.

## Bổ sung do người dùng yêu cầu sau nghiệm thu

Thêm đăng ký tài khoản với các thông tin đã có: tên đăng nhập, họ tên, mã thí sinh, email, mật khẩu và xác nhận. Đăng ký công khai tạo vai trò Thí sinh; cấp quyền nhân sự vẫn qua quản trị viên. Giữ cùng Users/Roles/UserRoles, không đổi schema. Bổ sung hướng dẫn truy cập ngay trên Web và Desktop, có quy trình theo vai trò. Đây là thay đổi phạm vi được yêu cầu trực tiếp, không xem các trường mật khẩu xác nhận là cột dữ liệu cần lưu.

### Mở rộng đăng ký nhân sự theo yêu cầu tiếp theo

Người dùng yêu cầu bổ sung đăng ký Giảng viên và Quản trị viên. Trang đăng ký nay hỗ trợ cả bốn loại tài khoản, gồm Cán bộ quản lý thi theo hệ thống vai trò hiện có. Nhân sự gửi yêu cầu, quản trị viên hiện tại duyệt trên Desktop trước khi quyền có hiệu lực. Bổ sung RequestedRole nullable trong Users và action approve; không dùng trường công khai để gán quyền ngay. Luồng này thay thế giới hạn đăng ký công khai chỉ dành cho Thí sinh ở ghi chú trước.
