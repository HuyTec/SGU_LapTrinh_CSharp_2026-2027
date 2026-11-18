# Exam Desktop (WinForms)

Ứng dụng nhân sự trên Windows, .NET 10. Giao diện dùng control WinForms chuẩn,
không truy cập SQL Server trực tiếp và không chứa quy tắc chấm thi.

```powershell
dotnet run --project Exam.Desktop
```

Khởi động `Exam.Web` và SQL Server trước. Địa chỉ mặc định là
`http://localhost:5180`; có thể sửa tại màn đăng nhập hoặc đặt biến môi trường:

```powershell
$env:EXAM_API_URL = 'http://localhost:5180'
dotnet run --project Exam.Desktop
```

HTTP chỉ được phép với địa chỉ loopback khi demo cục bộ. Khi triển khai,
dùng HTTPS với chứng chỉ được Windows tin cậy. Không bỏ qua kiểm tra chứng chỉ.
JWT chỉ giữ trong bộ nhớ; không lưu tài khoản, mật khẩu hoặc token xuống file.

Menu và danh sách chọn được tải qua `/api/admin/catalog`, theo quyền máy chủ.
Form gửi dữ liệu qua `ExamApiClient`; máy chủ kiểm tra lại quyền và trạng thái.
Danh sách hỗ trợ tìm kiếm và phân trang 25 dòng. Để đổi mật khẩu/role tài khoản,
chọn dòng rồi bấm **Đặt lại mật khẩu / vai trò**. Mật khẩu để trống khi sửa sẽ
giữ nguyên. Giờ hiển thị và nhập là giờ Việt Nam; gửi API dưới dạng UTC.

Tài khoản có thể có nhiều vai trò; khi đăng nhập chọn một vai trò cho phiên làm
việc, hoặc để hệ thống chọn tự động. Quản trị viên có thể nhập danh sách thí sinh
bằng CSV UTF-8 (tối đa 500 dòng / 1 MB), tải mẫu tại màn nhập. Danh sách được xem
trước với mật khẩu ẩn và được máy chủ kiểm tra trước khi xác nhận nhập.

Nút **Hướng dẫn** có ở màn đăng nhập và trong menu ứng dụng; nội dung trong
ứng dụng theo vai trò của phiên hiện tại. Nút **Đăng ký tài khoản trên Web** mở
`/Account/Register` tại địa chỉ máy chủ đang nhập. Mọi vai trò có thể đăng ký:
thí sinh dùng Web sau khi đăng ký; nhân sự chờ quản trị viên duyệt trước khi
đăng nhập Desktop. Quản trị viên mở **Tài khoản / thí sinh**, kiểm tra cột
**Vai trò đăng ký**, chọn đúng dòng và bấm **Duyệt đăng ký**. Quản trị viên
vẫn có thể trực tiếp tạo tài khoản/cấp vai trò bằng chức năng Thêm.

`MainForm` là shell; `NavigationService` chuyển UserControl và hủy màn cũ cùng timer.
`Views/ResourceView` cùng các view chuyên biệt hiển thị bảng và hành động;
`Presenters/ResourcePresenter` và `DashboardPresenter` điều phối API.
Trang Theo dõi tự tải lại mỗi 15 giây, không suy diễn trạng thái mất kết nối.
Ctrl+F tìm kiếm, Ctrl+N thêm mới, F5 tải lại; Ctrl+Enter lưu trong trình chỉnh sửa.
Tạo đợt thi có ba bước: thông tin, lịch/cấu hình, kiểm tra và lưu nháp.

`ResourceEditorForm` hiển thị metadata
thành ô nhập, lookup theo tên, danh sách chọn nhiều và radio đáp án đúng.
`ExamApiClient` chỉ xử lý giao tiếp HTTP và lỗi ProblemDetails.

Build thành công không thay thế kiểm tra trực quan. Cần chạy đủ luồng với dữ liệu
SQL Server và tài khoản từng vai trò để nghiệm thu giao diện/tích hợp.

Phạm vi hiện hành: [quyết định triển khai](../DECISIONS.md). Hướng dẫn chạy dùng SQL Docker: [README](../README.md).
