# Lược đồ SQL Server triển khai

Published/PublishedAt ghi việc giảng viên bật công bố cho đợt đã kích hoạt. Việc xem từng lượt yêu cầu lượt đã nộp và có ExamResult; không chờ đóng nhận lượt hoặc các thí sinh khác. ReviewPolicy quy định điểm, lựa chọn và đáp án đúng được hiển thị.

Quy tắc ngày 18/11/2026: ExamSessions.OpensAt/ClosesAt là khung giờ được tạo lượt mới. Attempts.Deadline = StartedAt + DurationMinutes, độc lập giờ đóng nhận lượt. Lượt cũ còn làm và bị cắt hạn bởi giờ đóng được điều chỉnh bằng migration PersonalAttemptDeadlines; bài đã có kết quả giữ dữ liệu lịch sử.

Nguồn chính xác có thể dựng lại: `Exam.Data/ExamDbContext.cs`, `Exam.Data/Entities.cs`, `Exam.Data/Migrations` và `schema.sql`. Đây là mô hình triển khai của 17 thực thể nghiệp vụ trong mục 3.3, bổ sung Identity và phiên truy cập theo DECISIONS.md.

## Bảng và trách nhiệm

| Bảng | Khóa / liên kết quan trọng | Nội dung |
|---|---|---|
| Users | UserID int; username chuẩn hóa duy nhất; StudentCode duy nhất khi khác null | Identity user, họ tên, trạng thái, mã thí sinh |
| Roles | RoleID int; tên vai trò chuẩn hóa duy nhất | Student, Lecturer, Administrator |
| UserRoles | PK(UserID, RoleID) | Một người có nhiều vai trò; chọn một vai trò hoạt động mỗi phiên Desktop |
| Subjects | Id int; Code duy nhất | Môn học, trạng thái hoạt động |
| LecturerSubjects | Id; UQ(LecturerId, SubjectId), FK Users/Subjects | Phạm vi quản lý của giảng viên |
| Topics | Id; UQ(SubjectId, Name), FK Subjects | Chủ đề theo môn |
| Questions | Id; FK TopicId/CreatedBy | Nội dung, độ khó Easy/Medium/Hard, ngừng sử dụng |
| AnswerOptions | Id; FK QuestionId; UQ vị trí, unique filtered IsCorrect | Các phương án và tối đa một đáp án đúng; service kiểm tra đủ phương án |
| Exams | Id; FK SubjectId/CreatedBy | Đề Draft/Ready |
| ExamQuestions | Id; FK ExamId/QuestionId; UQ câu và vị trí trong đề | Điểm từng câu > 0, tổng 10 khi xác nhận Ready |
| ExamSessions | Id; FK ExamId/ManagedBy; UQ AccessCode khi khác NULL; CHECK lịch/thời lượng/trạng thái/chính sách | Nháp/kích hoạt/hủy; Open/Closed được tính từ giờ server; ActivatedAt lưu thời điểm chốt danh sách, PublishedAt lưu thời điểm công bố |
| Classes | Id; FK SubjectId; UQ(SubjectId, Code) | Lớp theo môn và trạng thái hoạt động |
| ClassStudents | PK(ClassId, StudentId); FK Classes và Users | Thành viên lớp |
| SessionClasses | PK(SessionId, ClassId); FK ExamSessions và Classes | Liên kết đợt thi với lớp được gán |
| SessionCandidates | Id; UQ(SessionId, StudentId); FK ExamSessions và Users | Danh sách thí sinh được chốt khi kích hoạt, cũng giữ các dòng lịch sử từ luồng cũ |
| Attempts | Id bigint; UQ(SessionId, StudentId); FK trực tiếp ExamSessions và Users | Bắt đầu, Deadline, trạng thái, SubmittedAt, EndReason |
| AttemptQuestions | Id bigint; FK AttemptId và SourceQuestionId; UQ thứ tự trong lượt | Snapshot nội dung, điểm và thứ tự |
| AttemptOptions | Id bigint; FK AttemptQuestionId; alternate key(AttemptQuestionId, Id) | Snapshot phương án, đáp án và thứ tự |
| Responses | Id bigint; unique FK AttemptQuestionId; FK ghép tới AttemptOptions | Đáp án chọn, IsFlagged và SavedAt; không được chọn phương án của câu khác |
| ExamResults | Id bigint; unique FK AttemptId; CHECK Score 0..10 | Score decimal(5,2), CorrectCount, GradedAt |
| AuthSessions | Id GUID; FK UserId | Hạn phiên và cờ thu hồi, kiểm tra ở máy chủ trên từng request |

Bốn bảng kỹ thuật Identity mặc định (`AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`) không chứa tài khoản nghiệp vụ song song. `__EFMigrationsHistory` chỉ ghi migration đã áp dụng. Không có bảng `AspNetUsers` hoặc `AspNetRoles` riêng.

## Tên triển khai so với tên luận lý

Ngoài các tên khóa Identity được ánh xạ UserID/RoleID, mã dùng tên ngắn `Id`, `Code`, `Name`, `Text`, `Order`; ví dụ Subjects.Id = SubjectID luận lý, Questions.Text = QuestionContent, ExamSessions.OpensAt/ClosesAt = StartsAt/EndsAt, Attempts.Deadline = DeadlineAt. Các DTO không phải tên cột SQL.

Các bảng liên kết LecturerSubjects/SessionCandidates và Responses có khóa thay thế Id, kèm UNIQUE/alternate key giữ đúng khóa nghiệp vụ. Đây là lựa chọn vật lý trong phạm vi schema đã được duyệt; không làm thay đổi lực lượng quan hệ. Snapshot giữ nội dung/phương án/đáp án/điểm/thứ tự để chấm lịch sử, tham chiếu nguồn câu hỏi; không cần đọc lại đáp án hiện tại trong ngân hàng khi chấm. Không mô tả các tên cột minh họa của tài liệu phân tích là tên SQL thực tế.

Thời gian lưu UTC trong datetime2 độ chính xác mặc định của EF Core, giữ độ chính xác dưới giây để kiểm tra chính xác biên `now >= deadline`. Snapshot và điểm kết quả được lưu riêng, có chủ đích. Các thao tác bắt đầu/lưu/nộp, sửa đề/đợt, phân công và nhập CSV nhiều dòng dùng giao dịch; kiểm thử đồng thời dùng SQL Server thật, không suy ra từ SQLite hoặc EF InMemory.

## 3NF và dữ liệu lịch sử

Migration `QuestionConcurrency` thêm `Questions.RowVersion` kiểu SQL Server rowversion. API trả token base64; Form sửa gửi lại token gốc và máy chủ trả 409 nếu dữ liệu đã thay đổi. Sửa riêng phương án cũng làm tăng phiên bản câu hỏi. Đây là thuộc tính kỹ thuật, không hiện thành trường nhập cho người dùng.

- 1NF: phương án, câu trong đề, vai trò và phân công là các dòng riêng, không lưu danh sách câu/đáp án trong một cột nghiệp vụ.
- 2NF: thuộc tính phân công thuộc cả cặp giảng viên–môn hoặc đợt–thí sinh; không sao chép tên người/môn vào bảng liên kết.
- 3NF: tên môn lấy từ Subjects, chủ đề lấy từ Topics, người dùng lấy từ Users. Các FK giữ nguồn dữ liệu chuẩn.
- Snapshot mô tả sự kiện “nội dung thực tế đã giao”, không phải bản sao thay thế danh mục; chỉ ghi lúc bắt đầu và giữ nguyên. ExamResults mô tả sự kiện chấm, mỗi lượt có tối đa một kết quả.

Các giới hạn nhiều dòng như đúng một đáp án, tổng 10, quyền môn, lịch giao nhau và điều kiện công bố được kiểm tra ở Exam.Services; các ràng buộc PK/FK/UQ/CHECK bảo vệ thêm tại SQL. Không khẳng định CHECK đơn lẻ có thể kiểm tra tổng điểm nhiều dòng.

## Đăng ký nhân sự chờ duyệt

Migration `StaffRegistrationApproval` thêm Users.RequestedRole (nvarchar(32), NULL). Tài khoản chờ duyệt có IsActive=false và chưa có dòng UserRoles. Quản trị viên duyệt trong giao dịch để cấp vai trò, kích hoạt và xóa RequestedRole. Migration `RetireExamOfficer` chuyển vai trò và yêu cầu duyệt ExamOfficer cũ sang Lecturer. RequestedRole ghi yêu cầu cấp quyền, chưa phải quyền đang có hiệu lực.

## Chốt danh sách khi kích hoạt (BR12, BR13)

Khi giảng viên kích hoạt, hệ thống ghi `ExamSessions.ActivatedAt` (UTC, `datetime2`, cho phép NULL) và chốt một dòng `SessionCandidates` cho mỗi tài khoản Student đang hoạt động trong các lớp đã chọn. Kích hoạt và ghi danh sách nằm trong cùng một giao dịch. Thêm hoặc gỡ thành viên lớp sau đó không đổi quyền dự thi. Hoãn về nháp xóa danh sách và đặt lại ActivatedAt = NULL. Hủy đợt giữ danh sách để đối chiếu.

Đợt đã chốt dùng danh sách này để xét lượt mới, theo dõi và xuất kết quả. Người chưa có lượt được ghi là chưa tham gia và để trống điểm. Theo dõi cũng giữ người đã có lượt dù không còn trong danh sách. Đợt chưa có ActivatedAt tiếp tục xét thành viên lớp hợp lệ theo quy tắc chuyển tiếp. Một danh sách cũ không kèm lớp đã gán không đủ để cấp lượt mới. Chủ sở hữu vẫn được tiếp tục lượt đã có theo quy tắc về thời hạn và công bố.

Migration `20261119191316_SessionRosterSnapshot` bổ sung ActivatedAt và điền các cặp còn thiếu cho đợt Active dựa trên lớp hợp lệ tại thời điểm nâng cấp. Migration không suy đoán thành viên tại ngày kích hoạt ban đầu, giữ các dòng phân công đã có và chỉ ghi mốc chốt cho đợt Active có liên kết lớp. Đợt nháp, đợt hủy và đợt cũ chưa gán lớp không được chốt bởi migration.

## Nâng cấp dữ liệu đợt thi (DT02)

Migration `20261113090207_StudentClassAdmission` thêm các bảng lớp và mã vào thi. Migration tháo FK ghép từ `Attempts` đến `SessionCandidates`, giữ hai FK trực tiếp, khóa duy nhất và dữ liệu từng lượt. Mã vào thi của đợt cũ bắt đầu là NULL. Giữ các dòng `SessionCandidates` để đối chiếu. Chỉ ghi ánh xạ đợt–lớp từ nguồn đã được nhóm xác nhận.

Đợt cũ thiếu ánh xạ lớp không cấp quyền tạo lượt mới, kể cả khi đang mở. Chủ sở hữu lượt đã có vẫn tiếp tục theo hạn ban đầu và xem kết quả theo chính sách công bố. Căn cứ nghiệp vụ là [UC19](https://docs.google.com/spreadsheets/d/10sSsgNXNt2c-S9SLn9rWQej7txwzoXuFAWfPI9gzFOc/edit#gid=899770140&range=A392%3AB401) và [FR-F03, FR-F04](https://docs.google.com/spreadsheets/d/10sSsgNXNt2c-S9SLn9rWQej7txwzoXuFAWfPI9gzFOc/edit#gid=482467226&range=A70%3AF71).

Trước khi áp dụng lên DB dùng chung, xác minh bản sao lưu có thể phục hồi vào DB riêng. Chạy migration trên bản phục hồi và đối chiếu theo ID lượt, đợt, chủ sở hữu, trạng thái, thời hạn, câu trả lời và điểm. Nếu đã có lượt tạo theo lớp mà thiếu dòng tương ứng trong `SessionCandidates`, migration xuống phiên bản cũ sẽ không thể khôi phục FK ghép. Dùng bản sao lưu đã xác minh để phục hồi khi cần.

Ngày 13/11/2026, thử trên SQL Server Express cục bộ với dữ liệu mẫu ở phiên bản `StaffRegistrationApproval`. Bản sao lưu `COPY_ONLY` qua kiểm tra checksum và phục hồi vào DB riêng. Chạy `docs/schema.sql` hai lần trên bản phục hồi. Kết quả đối chiếu khớp 2/2 lượt (một đang làm, một đã nộp), gồm ID, đợt, chủ sở hữu, trạng thái, mốc thời gian, bản chụp đề, câu trả lời và điểm. Cả 3 dòng phân công cũ còn nguyên và có 0 liên kết lớp phát sinh. Bộ `Exam.Tests` chạy trên SQL Server đạt 30/30, gồm kiểm tra từ chối tạo lượt chỉ từ danh sách cũ và chống trùng đợt–lớp, mã vào thi.
