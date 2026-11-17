# Đối chiếu code với sơ đồ — nhánh web-thi-sinh

Mốc kiểm tra `1feb8469413e2c043762b25c100219cecbfa364a`. Sơ đồ đối chiếu là [Trắc nghiệm.drawio (1).xml](<C:/Users/NHUT HUY/Downloads/Trắc nghiệm.drawio (1).xml>). So sánh với `main` mốc `6f66e0d160263903197c6cad5bb666f053722457` cho thấy các controller, Razor view và service quản lý lớp, đề, đợt thi và kết quả giảng viên không có trên nhánh đang mở. Vì vậy kết luận về thao tác gỡ lớp qua MVC của lượt trước chỉ áp dụng cho phiên bản có màn đó.

| Mức | Use case | Độ lệch và bằng chứng |
| --- | --- | --- |
| P1 | UC-10/11/22 | Giảng viên chỉ xem môn được phân công. Chưa có luồng MVC truy cập, chỉnh sửa chủ đề, ngân hàng câu hỏi và đề. `Exam.Web/Controllers/LecturerController.cs:12`. |
| P1 | UC-18/19/20 | Thiếu MVC tạo đợt, gán lớp, phát mã, kích hoạt và hoãn/hủy. Nhánh kích hoạt cũ yêu cầu `SessionCandidates` trong khi thí sinh lấy quyền từ `SessionClasses` và `ClassStudents`. Seed demo có thể tạo sẵn đợt/mã nhưng không cung cấp chức năng cho giảng viên. `Exam.Services/AdminService.cs:461`, `Exam.Services/ExamService.cs:13`, `scripts/Seed-StudentClassDemo.sql:70`. |
| P1 | UC-12/13/21 | Thiếu đường Web giảng viên theo dõi, xuất và công bố. API quản trị đang giới hạn Administrator. Nhánh theo dõi cũ còn dùng roster theo cá nhân. `Exam.Web/Controllers/ApiControllers.cs:16`, `Exam.Services/AdminService.cs:133`. |
| P1 | UC-24 | Desktop/API chưa có quản lý lớp/thành viên. Thí sinh có màn xem lớp của mình và tự tham gia lớp demo khi ở Development. Model chưa có học kỳ. `Exam.Services/AdminService.cs:55`, `Exam.Web/Controllers/StudentClassesController.cs:15`, `Exam.Data/Entities.cs:143`. |
| P1 · Đã tái hiện | UC-07 | Ca kiểm chứng trên SQL Server buộc ghi điểm lỗi khi nộp trước hạn. Attempt vẫn InProgress, chưa có ExamResult và máy chủ vẫn nhận đáp án mới. Trạng thái khóa và kết quả hiện lưu trong cùng giao dịch. Cần lưu trạng thái khóa trước và thử chấm lại từ bài đã khóa. `Exam.Services/ExamService.cs:137`. |
| P2 | UC-05/06/08/09 | Luồng tra mã, một lượt mỗi đợt, snapshot, lưu theo hạn và chính sách xem kết quả đã có. Code ưu tiên lượt cũ trước khi kiểm quyền cấp lượt mới. Nên tách hai nhánh trong UC-06 và Hình 3.2. Trang lịch sử thiếu thời điểm. `Exam.Services/ExamService.cs:55`, `Exam.Web/Views/Attempts/History.cshtml:7`. |

Kiểm chứng xUnit với SQL Server cách ly gồm 39 ca, tính cả ca tạm tái hiện UC-07. Có 38 đạt và 1 lỗi, không bỏ qua. Ca kiểm chứng tạm đạt và đã được gỡ khỏi mã nguồn. Bộ có sẵn tương ứng 37/38 đạt. Ca lỗi `DemoClassMembershipIsPrivateAndJoiningTwiceIsIdempotent` dừng vì fixture đã gán sinh viên vào TEST-CLASS trước kiểm tra danh sách rỗng (`Exam.Tests/SqlExamTests.cs:104`). Chưa chạy nghiệm thu giao diện toàn tuyến.

Nghiệm thu bản tích hợp theo Hình 3.2: Admin quản lý lớp → giảng viên gán lớp và kích hoạt → thí sinh làm bài → giảng viên công bố. Dùng một mô hình gán lớp cho cả hai phía. Thêm ca lỗi lưu điểm trước hạn để kiểm chứng bài đã khóa.
