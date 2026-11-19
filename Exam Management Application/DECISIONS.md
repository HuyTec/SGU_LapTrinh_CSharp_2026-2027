# Quyết định triển khai đã duyệt

Ngày xác nhận: 24/10/2026 (Asia/Saigon). Người dùng duyệt `1a, 2a 3a 4a 5a` sau cổng rà soát.

### Thay đổi phạm vi ngày 10/11/2026

Ngày 18/11/2026, chủ dự án yêu cầu ứng dụng phụ thuộc Docker: SQL Server chạy qua `team/docker/compose.sql.yml`, endpoint `tcp:127.0.0.1,14333`, database mặc định `ExamAppDocker`. Web/Desktop vẫn chạy bằng .NET trên Windows. Web từ chối khởi động khi cấu hình ngoài endpoint này hoặc không kết nối được database; không fallback sang SQL Express. Script khởi tạo/chạy/kiểm thử dùng Docker. Giữ nguyên SQL Express và volume cũ; chưa di chuyển dữ liệu giữa hai môi trường. Quyết định này thay mặc định SQL Express trong bảng nền tảng bên dưới.

Ngày 18/11/2026, giờ mở/đóng đợt được xác định là khung giờ nhận lượt thi mới [OpensAt, ClosesAt). Mỗi lượt có Deadline = StartedAt + DurationMinutes và được tiếp tục sau khi hết giờ nhận lượt. Thời lượng có thể dài hơn khung giờ vào thi. Worker chấm theo Deadline cá nhân. Theo điều chỉnh tiếp theo cùng ngày, giảng viên có thể bật công bố bất kỳ lúc nào sau khi kích hoạt; từng thí sinh được xem ngay khi bài của mình đã kết thúc và chấm xong. Bài nộp sau tự hiển thị theo chính sách, mặc định điểm và lựa chọn đã trả lời trên form công bố. Kiểm tra trùng lịch dùng khoảng từ giờ mở đến giờ đóng nhận lượt cộng thời lượng. Migration PersonalAttemptDeadlines điều chỉnh lượt InProgress chưa có kết quả bị cắt hạn bởi giờ đóng cũ.

Ngày 18/11/2026, chủ dự án bổ sung hồ sơ cá nhân cho cả ba tác nhân (UC-03): Thí sinh và Giảng viên dùng Web, Quản trị viên dùng WinForms. Người dùng cập nhật họ tên, email và số điện thoại của chính mình. Trang thí sinh hiển thị mã và số lớp; trang giảng viên hiển thị môn được phân công. Tên đăng nhập, mã thí sinh và vai trò chỉ hiển thị. Các client gọi chung ProfileService, có kiểm tra phiên bản khi lưu.

Ngày 17/11/2026, chủ dự án cho phép giảng viên tự tạo lớp học trên Web, giới hạn trong môn đang hoạt động được phân công. Thí sinh xem các lớp mình là thành viên qua mục “Lớp của tôi”. Đăng ký bổ sung số điện thoại tùy chọn; không tự điền số cho hồ sơ cũ. Migration RetireExamOfficer chuyển quyền cũ sang Lecturer, giữ ID/chủ sở hữu và bổ sung phân công môn cho các đợt do tài khoản cũ phụ trách.

Theo chỉ đạo mới của người dùng, bỏ tác nhân/vai trò Cán bộ quản lý thi (`ExamOfficer`). Giảng viên nhận các nghiệp vụ UC-12/13/17–21 trong phạm vi môn được phân công; bổ sung UC-24 quản lý lớp học phần và đưa học viên từ lớp vào danh sách đợt thi. Giữ Thí sinh và Quản trị viên. Đây là thay đổi so với quyết định 24/10/2026.

PR QT01/QT02 chuyển đăng nhập về ba vai trò, khóa API Desktop cho quản trị viên và cung cấp trang Web xem môn được phân công. Các màn học liệu thuộc HL01/HL02. Tạo/sửa đợt nháp thuộc DT01 (UC-18), gán lớp và kích hoạt thuộc DT03 (UC-18/19), theo dõi và công bố kết quả thuộc DT04 (UC-12/13/21). Thao tác hoãn/hủy (UC-20) cần được nối trên Web giảng viên cùng luồng quản lý đợt. Các luồng này cần dùng xác thực Web và kiểm tra phạm vi tại máy chủ. Dữ liệu đợt cũ còn đứng tên cán bộ cần được bàn giao cho giảng viên được nhóm xác nhận.

| Mục | Quyết định | Lý do |
|---|---|---|
| Nguồn yêu cầu | `docs/YEU_CAU_DU_AN_DE_TAI_18.md` là tiêu chí nghiệm thu chính; đặc tả 3.2 chi tiết nghiệp vụ, ưu tiên hơn 3.3 | Theo chỉ định trực tiếp của người dùng |
| Desktop | Tạo Exam.Desktop WinForms; giữ nguyên App1 WinUI cũ | Đúng lựa chọn nhóm, dễ dựng và giải thích |
| Web/backend | Một host Exam.Web chứa MVC/Razor/Bootstrap và Web API; Exam.Core, Exam.Data, Exam.Services dùng chung | Không nhân đôi nghiệp vụ; Desktop chỉ gọi API |
| Nền tảng | .NET 10, EF Core 10; SQL Server 2022 trong Docker tại `tcp:127.0.0.1,14333` | Theo điều chỉnh ngày 18/11/2026 |
| Identity | Ánh xạ vào Users, Roles, UserRoles với khóa int và tên khóa UserID/RoleID | Dùng chính người dùng của mô hình 3.3, không tạo tài khoản nghiệp vụ song song |
| Xác thực | Cookie cho Web; JWT có hạn cho Desktop; kiểm tra tài khoản và phiên ở máy chủ, có thu hồi phiên | Đăng xuất, khóa tài khoản phải có hiệu lực |
| UC-17 | Quản trị viên quản lý môn học trên Desktop | Theo phạm vi hiện hành và sơ đồ UC-17 |
| CSDL | Cho phép tạo schema/migration, các trường bổ sung trong mục 10 và bảng kỹ thuật Identity/phiên; dùng DB demo/test mới | Đã duyệt tại câu 2 và câu 5 |
| Phạm vi | Bắt buộc + xUnit + xuất Excel/CSV cơ bản; một đáp án đúng, một lượt mỗi đợt, thang 10 | Ưu tiên chạy được trong quỹ 2–4 tuần |
| Hoãn | Ma trận đề, Excel câu hỏi, giám sát mất kết nối, hồ sơ/đổi mật khẩu cá nhân nâng cao | Không làm chậm luồng chính |
| Giao diện | Đơn giản, sáng, rõ nhãn/lỗi/trạng thái; Web từ 360 px; WinForms control chuẩn | Yêu cầu trực tiếp của người dùng |
| Triển khai | Cho phép 3 tác nhân sửa mã, thêm project/package và cập nhật tài liệu theo các quyết định này | Duyệt câu 5; kết thúc giai đoạn chỉ đọc |

App1 được giữ làm mã tham khảo, không phải sản phẩm WinForms mới. Không xóa hay ghi đè CSDL có sẵn. Dữ liệu mẫu phải được cấp qua cấu hình cục bộ, không commit mật khẩu/khóa ký/connection string thật. Chỉ ghi nhận build, DB, API, giao diện, hiệu năng là đạt khi có kiểm tra thực tế tương ứng.

## Bổ sung do người dùng yêu cầu sau nghiệm thu

Thêm đăng ký tài khoản với các thông tin đã có: tên đăng nhập, họ tên, mã thí sinh, email, mật khẩu và xác nhận. Đăng ký công khai tạo vai trò Thí sinh; cấp quyền nhân sự vẫn qua quản trị viên. Giữ cùng Users/Roles/UserRoles, không đổi schema. Bổ sung hướng dẫn truy cập ngay trên Web và Desktop, có quy trình theo vai trò. Đây là thay đổi phạm vi được yêu cầu trực tiếp, không xem các trường mật khẩu xác nhận là cột dữ liệu cần lưu.

### Đăng ký giảng viên

Web cho thí sinh và giảng viên đăng ký. Tài khoản giảng viên chờ quản trị viên duyệt trên Desktop trước khi đăng nhập Web. Tài khoản quản trị viên do quản trị viên hiện có cấp trên Desktop.
