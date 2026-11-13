-- Run on the local ExamAppDocker development database after EF migrations and demo account seeding.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

IF DB_NAME() <> N'ExamAppDocker'
    THROW 50001, N'Chỉ chạy trên database demo ExamAppDocker.', 1;

DECLARE @ClassCode nvarchar(50) = N'DEMO-NV10';
DECLARE @ExamCode nvarchar(32) = N'THI-NV10';
DECLARE @ExamId int, @ClassId int, @SessionId int, @SubjectId int, @TopicId int;

BEGIN TRANSACTION;

SELECT @SessionId = Id, @ExamId = ExamId
FROM dbo.ExamSessions WITH (UPDLOCK, HOLDLOCK) WHERE AccessCode = @ExamCode;

IF @SessionId IS NULL
BEGIN
    DECLARE @LecturerId int = (SELECT UserID FROM dbo.Users WHERE NormalizedUserName = N'LECTURER');
    IF @LecturerId IS NULL
        THROW 50002, N'Chạy seeder demo để tạo tài khoản lecturer trước.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Subjects WHERE Code = N'DEMO-NV')
        THROW 50003, N'Mã môn DEMO-NV đã tồn tại nhưng chưa có đợt THI-NV10; kiểm tra dữ liệu trước khi chạy lại.', 1;

    INSERT dbo.Subjects (Code, Name, IsActive)
        VALUES (N'DEMO-NV', N'Nghiệp vụ ứng dụng thi trắc nghiệm', 1);
    SET @SubjectId = CONVERT(int, SCOPE_IDENTITY());
    INSERT dbo.LecturerSubjects (SubjectId, LecturerId) VALUES (@SubjectId, @LecturerId);
    INSERT dbo.Classes (SubjectId, Code, IsActive) VALUES (@SubjectId, @ClassCode, 1);
    SET @ClassId = CONVERT(int, SCOPE_IDENTITY());
    INSERT dbo.Topics (SubjectId, Name, IsActive) VALUES (@SubjectId, N'Nghiệp vụ cơ bản', 1);
    SET @TopicId = CONVERT(int, SCOPE_IDENTITY());
    INSERT dbo.Exams (SubjectId, CreatedBy, Title, Status)
        VALUES (@SubjectId, @LecturerId, N'Đề thử nghiệp vụ ứng dụng – 10 câu', N'Ready');
    SET @ExamId = CONVERT(int, SCOPE_IDENTITY());

    DECLARE @Items TABLE (
        Seq int PRIMARY KEY, Question nvarchar(300), A nvarchar(200), B nvarchar(200),
        C nvarchar(200), D nvarchar(200), Correct int
    );
    INSERT @Items VALUES
    (1,N'Thí sinh cần quyền gì để bắt đầu một đợt thi theo lớp?',N'Là thành viên của lớp được gán',N'Có tài khoản bất kỳ',N'Biết tên môn học',N'Đã xem hướng dẫn',1),
    (2,N'Mã vào đợt thi có tác dụng gì?',N'Cấp quyền quản trị',N'Tra cứu đợt thi trước khi bắt đầu',N'Tạo tài khoản',N'Gia hạn bài thi',2),
    (3,N'Thao tác nào tạo lượt thi?',N'Mở trang đăng nhập',N'Xem tên lớp',N'Bấm Bắt đầu khi đủ điều kiện',N'Nhập email',3),
    (4,N'Tải lại trang khi đang thi có thay đổi hạn nộp không?',N'Tăng một phút',N'Đặt lại toàn bộ thời gian',N'Đổi theo máy thí sinh',N'Không, dùng hạn nộp của lượt cũ',4),
    (5,N'Khi nào đáp án được coi là đã lưu?',N'Khi máy chủ xác nhận lưu',N'Ngay khi đọc câu hỏi',N'Khi thoát trình duyệt',N'Khi đổi tab',1),
    (6,N'Một thí sinh có thể có bao nhiêu lượt trong cùng một đợt?',N'Hai lượt',N'Một lượt',N'Không giới hạn',N'Mỗi lần tải lại thêm một lượt',2),
    (7,N'Khi hết hạn nộp, bên nào quyết định trạng thái bài?',N'Đồng hồ máy thí sinh',N'Ứng dụng Desktop của giảng viên',N'Máy chủ',N'Trình duyệt',3),
    (8,N'Khi nào thí sinh được xem điểm theo chính sách đợt thi?',N'Ngay khi mở đề',N'Ngay khi chọn đáp án',N'Trước khi nộp',N'Sau khi kết quả được công bố',4),
    (9,N'Dữ liệu nào được dùng để chấm lượt đã bắt đầu?',N'Bản câu hỏi, đáp án và trọng số lưu theo lượt',N'Mọi câu trong ngân hàng',N'Phiên bản đề sửa sau đó',N'Các câu được đánh dấu',1),
    (10,N'Ai phải duyệt tài khoản giảng viên tự đăng ký?',N'Thí sinh',N'Quản trị viên',N'Trình duyệt tự duyệt',N'Không có ai',2);

    DECLARE @Seq int = 1, @QuestionId int;
    WHILE @Seq <= 10
    BEGIN
        INSERT dbo.Questions (TopicId, CreatedBy, Text, Difficulty, IsActive)
            SELECT @TopicId, @LecturerId, Question, N'Easy', 1 FROM @Items WHERE Seq = @Seq;
        SET @QuestionId = CONVERT(int, SCOPE_IDENTITY());
        INSERT dbo.AnswerOptions (QuestionId, Text, IsCorrect, [Order])
            SELECT @QuestionId, v.Text, IIF(v.Ordinal = i.Correct, 1, 0), v.Ordinal
            FROM @Items i CROSS APPLY (VALUES (1,i.A),(2,i.B),(3,i.C),(4,i.D)) v(Ordinal,Text)
            WHERE i.Seq = @Seq;
        INSERT dbo.ExamQuestions (ExamId, QuestionId, [Order], Points)
            VALUES (@ExamId, @QuestionId, @Seq, 1);
        SET @Seq += 1;
    END;

    DECLARE @Now datetime2 = SYSUTCDATETIME();
    INSERT dbo.ExamSessions (ExamId, ManagedBy, Title, OpensAt, ClosesAt, DurationMinutes,
        Status, ShuffleQuestions, ShuffleOptions, ReviewPolicy, Published, HasOpened, AccessCode)
        VALUES (@ExamId, @LecturerId, N'Thi thử nghiệp vụ ứng dụng', DATEADD(minute,-1,@Now),
            DATEADD(day,7,@Now), 20, N'Active', 0, 0, N'CorrectAnswers', 0, 0, @ExamCode);
    SET @SessionId = CONVERT(int, SCOPE_IDENTITY());
    INSERT dbo.SessionClasses (SessionId, ClassId) VALUES (@SessionId, @ClassId);
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.SessionClasses sc
    JOIN dbo.Classes c ON c.Id = sc.ClassId
    JOIN dbo.Subjects s ON s.Id = c.SubjectId
    WHERE sc.SessionId = @SessionId AND c.Code = @ClassCode AND s.Code = N'DEMO-NV'
) OR (SELECT COUNT(*) FROM dbo.ExamQuestions WHERE ExamId = @ExamId) <> 10
    THROW 50004, N'Mã thi trùng hoặc bộ dữ liệu demo không đầy đủ.', 1;

COMMIT TRANSACTION;

SELECT c.Id AS ClassId, c.Code AS ClassCode, es.ExamId, es.Id AS SessionId,
    es.AccessCode, es.OpensAt AS OpensUtc, es.ClosesAt AS ClosesUtc, es.DurationMinutes,
    (SELECT COUNT(*) FROM dbo.ExamQuestions eq WHERE eq.ExamId = es.ExamId) AS Questions,
    (SELECT COUNT(*) FROM dbo.ClassStudents cs WHERE cs.ClassId = c.Id) AS EnrolledStudents
FROM dbo.ExamSessions es
JOIN dbo.SessionClasses sc ON sc.SessionId = es.Id
JOIN dbo.Classes c ON c.Id = sc.ClassId
JOIN dbo.Subjects s ON s.Id = c.SubjectId
WHERE es.Id = @SessionId AND c.Code = @ClassCode AND s.Code = N'DEMO-NV';
