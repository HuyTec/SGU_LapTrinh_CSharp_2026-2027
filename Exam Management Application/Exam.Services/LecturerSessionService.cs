using System.Data;
using System.Security.Cryptography;
using Exam.Core;
using Exam.Data;
using Microsoft.EntityFrameworkCore;

namespace Exam.Services;

public sealed record SessionDraft(string Title, int ExamId, DateTime OpensAt, DateTime ClosesAt, int DurationMinutes,
    bool ShuffleQuestions, bool ShuffleOptions, string ReviewPolicy, int[] ClassIds);
public sealed record LecturerSessionRow(int Id, string Title, string Paper, string State, string? Code, bool CanEdit, bool CanWithdraw);
public sealed record SessionPaperChoice(int Id, int SubjectId, string Label);
public sealed record SessionClassChoice(int Id, int SubjectId, string Label);
public sealed record SessionEditorData(SessionDraft? Draft, IReadOnlyList<SessionPaperChoice> Papers, IReadOnlyList<SessionClassChoice> Classes);

public sealed class LecturerSessionService(ExamDbContext db, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private IQueryable<ExamSession> Owned(int lecturer) => db.ExamSessions
        .Where(s => s.ManagedBy == lecturer && db.LecturerSubjects.Any(a =>
            a.LecturerId == lecturer && a.SubjectId == s.Exam.SubjectId));

    public async Task<IReadOnlyList<LecturerSessionRow>> ListAsync(int lecturer)
    {
        var now = Now;
        var rows = await Owned(lecturer).AsNoTracking().Include(s => s.Exam)
            .OrderByDescending(s => s.OpensAt).ThenByDescending(s => s.Id).ToListAsync();
        return rows.Select(s => new LecturerSessionRow(s.Id, s.Title, s.Exam.Title, ExamService.SessionState(s, now),
                s.Status == "Active" ? s.AccessCode : null, s.Status == "Draft" && !s.HasOpened,
                s.Status == "Active" && !s.HasOpened && s.OpensAt > now))
            .Where(s => s.State is not ("Cancelled" or "Closed"))
            .ToList();
    }

    private async Task<ExamSession> LoadAsync(int lecturer, int id) => await Owned(lecturer)
        .Include(s => s.Exam).ThenInclude(p => p.Subject)
        .Include(s => s.Exam).ThenInclude(p => p.Questions).ThenInclude(q => q.Question).ThenInclude(q => q.Options)
        .Include(s => s.Classes).ThenInclude(c => c.Class)
        .SingleOrDefaultAsync(s => s.Id == id) ?? throw new BusinessException("Không tìm thấy đợt thi trong phạm vi phụ trách.", 404);

    public async Task<SessionEditorData> EditorAsync(int lecturer, int? id)
    {
        var session = id is int sessionId ? await LoadAsync(lecturer, sessionId) : null;
        if (session is not null) EnsureDraft(session);
        var subjects = db.LecturerSubjects.Where(a => a.LecturerId == lecturer).Select(a => a.SubjectId);
        var papers = await db.Exams.AsNoTracking().Where(p => p.Status == "Ready" && p.Subject.IsActive && subjects.Contains(p.SubjectId))
            .OrderBy(p => p.Title).Select(p => new SessionPaperChoice(p.Id, p.SubjectId, p.Subject.Code + " — " + p.Title)).ToListAsync();
        var classes = await (from c in db.Set<ExamClass>().AsNoTracking()
                             join subject in db.Subjects on c.SubjectId equals subject.Id
                             where c.IsActive && subject.IsActive && subjects.Contains(c.SubjectId)
                             orderby subject.Code, c.Code
                             select new SessionClassChoice(c.Id, c.SubjectId, subject.Code + " — " + c.Code)).ToListAsync();
        var draft = session is null ? null : new SessionDraft(session.Title, session.ExamId, session.OpensAt, session.ClosesAt,
            session.DurationMinutes, session.ShuffleQuestions, session.ShuffleOptions, session.ReviewPolicy,
            session.Classes.Select(c => c.ClassId).ToArray());
        return new(draft, papers, classes);
    }

    private static void EnsureDraft(ExamSession session)
    {
        if (session.Status != "Draft" || session.HasOpened)
            throw new BusinessException("Chỉ được sửa đợt nháp chưa từng mở.", 409);
    }

    private Task<T> WriteAsync<T>(Func<Task<T>> operation) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var result = await operation();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    });

    public Task<int> SaveAsync(int lecturer, int? id, SessionDraft draft) => WriteAsync(async () =>
    {
        var session = id is int sessionId ? await LoadAsync(lecturer, sessionId) : new ExamSession { ManagedBy = lecturer };
        EnsureDraft(session);
        var title = draft.Title?.Trim() ?? "";
        if (title.Length == 0)
            throw new BusinessException("Vui lòng nhập tên đợt thi.", field: nameof(SessionDraft.Title));
        if (title.Length > 200)
            throw new BusinessException("Tên đợt thi tối đa 200 ký tự.", field: nameof(SessionDraft.Title));
        ValidateSchedule(draft.OpensAt, draft.ClosesAt, draft.DurationMinutes);
        if (draft.ReviewPolicy is not ("ScoreOnly" or "Answers" or "CorrectAnswers"))
            throw new BusinessException("Chọn lại chính sách xem kết quả.", field: nameof(SessionDraft.ReviewPolicy));
        var paper = await db.Exams.Include(p => p.Subject).SingleOrDefaultAsync(p =>
            p.Id == draft.ExamId && p.Status == "Ready" && p.Subject.IsActive && db.LecturerSubjects.Any(a =>
                a.LecturerId == lecturer && a.SubjectId == p.SubjectId))
            ?? throw new BusinessException("Đề đã chọn chưa sẵn sàng hoặc không thuộc môn đang hoạt động được phân công. Hãy chọn lại đề.", field: nameof(SessionDraft.ExamId));
        var classIds = draft.ClassIds.Distinct().ToArray();
        if (await db.Set<ExamClass>().CountAsync(c => classIds.Contains(c.Id) && c.IsActive && c.SubjectId == paper.SubjectId) != classIds.Length)
            throw new BusinessException("Có lớp đã ngừng hoạt động hoặc khác môn với đề. Hãy kiểm tra lại các lớp đã chọn.", field: nameof(SessionDraft.ClassIds));
        session.Title = title;
        session.ExamId = paper.Id;
        session.OpensAt = draft.OpensAt;
        session.ClosesAt = draft.ClosesAt;
        session.DurationMinutes = draft.DurationMinutes;
        session.ShuffleQuestions = draft.ShuffleQuestions;
        session.ShuffleOptions = draft.ShuffleOptions;
        session.ReviewPolicy = draft.ReviewPolicy;
        foreach (var link in session.Classes.Where(c => !classIds.Contains(c.ClassId)).ToList())
            db.Set<SessionClass>().Remove(link);
        foreach (var classId in classIds.Where(c => !session.Classes.Any(link => link.ClassId == c)))
            session.Classes.Add(new SessionClass { ClassId = classId });
        if (!id.HasValue)
            db.ExamSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    });

    public Task ChangeStateAsync(int lecturer, int id, string action) => WriteAsync(async () =>
    {
        var session = await LoadAsync(lecturer, id);
        if (action == "cancelDraft")
        {
            if (session.Status != "Draft" || session.HasOpened ||
                await db.Attempts.AnyAsync(a => a.SessionId == id))
                throw new BusinessException("Chỉ hủy được đợt nháp chưa có lượt thi.", 409);
            session.Status = "Cancelled";
            return true;
        }
        if (action == "closeAdmissions")
        {
            if (session.Status != "Active" || session.AdmissionsClosedAt != null || session.ClosesAt <= Now)
                throw new BusinessException("Đợt thi không còn nhận thí sinh mới hoặc đã đóng nhận thí sinh.", 409);
            session.AdmissionsClosedAt = Now;
            return true;
        }
        if (action == "cancel")
        {
            if (session.Status != "Active" || session.HasOpened || Now >= session.OpensAt ||
                await db.Attempts.AnyAsync(a => a.SessionId == id))
                throw new BusinessException("Chỉ hủy đợt trước giờ mở và khi chưa có thí sinh bắt đầu làm bài.", 409);
            session.Status = "Cancelled";
            return true;
        }
        if (action == "postpone")
        {
            if (session.Status != "Active" || session.HasOpened || Now >= session.OpensAt ||
                await db.Attempts.AnyAsync(a => a.SessionId == id))
                throw new BusinessException("Chỉ hoãn đợt đã kích hoạt trước giờ mở lần đầu.", 409);
            await db.SessionCandidates.Where(c => c.SessionId == id).ExecuteDeleteAsync();
            await db.SessionOptions.Where(o => o.Question.SessionId == id).ExecuteDeleteAsync();
            await db.SessionQuestions.Where(q => q.SessionId == id).ExecuteDeleteAsync();
            session.ActivatedAt = null;
            session.Status = "Draft";
            return true;
        }
        if (action != "activate")
            throw new BusinessException("Thao tác không hợp lệ.");
        EnsureDraft(session);
        if (session.Exam.Status != "Ready")
            throw new BusinessException("Đề thi chưa sẵn sàng. Xác nhận đề sẵn sàng trước khi kích hoạt đợt.");
        if (session.Exam.Questions.Sum(q => q.Points) > 10m)
            throw new BusinessException("Đề thi vượt quá 10 điểm. Chỉnh lại điểm của đề trước khi kích hoạt đợt thi.");
        if (!session.Exam.Subject.IsActive)
            throw new BusinessException("Môn học đã ngừng hoạt động. Liên hệ quản trị viên để kiểm tra môn.");
        if (session.ClosesAt <= Now)
            throw new BusinessException("Giờ đóng đã qua. Sửa lịch thi trước khi kích hoạt.");
        ValidateSchedule(session.OpensAt, session.ClosesAt, session.DurationMinutes);
        if (session.Classes.Count == 0)
            throw new BusinessException("Chưa chọn lớp dự thi. Sửa đợt nháp và chọn ít nhất một lớp trước khi kích hoạt.");
        if (session.Classes.Any(c => !c.Class.IsActive || c.Class.SubjectId != session.Exam.SubjectId))
            throw new BusinessException("Có lớp đã ngừng hoạt động hoặc khác môn với đề. Sửa lại các lớp đã chọn trước khi kích hoạt.");
        var classIds = session.Classes.Select(c => c.ClassId).ToArray();
        var members = db.Set<ClassStudent>().Where(m => classIds.Contains(m.ClassId)).Select(m => m.StudentId);
        var studentIds = await (from studentId in members
                                join user in db.Users on studentId equals user.Id
                                join userRole in db.UserRoles on user.Id equals userRole.UserId
                                join role in db.Roles on userRole.RoleId equals role.Id
                                where user.IsActive && role.NormalizedName == "STUDENT"
                                select studentId).Distinct().ToListAsync();
        if (session.Exam.Questions.Count == 0 || session.Exam.Questions.Any(q => q.Question.Options.Count < 2 || q.Question.Options.Count(o => o.IsCorrect) != 1))
            throw new BusinessException("Đề không còn đủ câu hỏi hoặc phương án hợp lệ. Hãy kiểm tra lại đề trước khi kích hoạt.", 409);
        if (await db.SessionQuestions.AnyAsync(q => q.SessionId == id))
            throw new BusinessException("Đợt nháp còn bản chụp đề cũ. Không thể kích hoạt để tránh thay đổi nội dung đã chốt.", 409);

        await db.SessionCandidates.Where(c => c.SessionId == id).ExecuteDeleteAsync();
        db.SessionCandidates.AddRange(studentIds.Select(studentId => new SessionCandidate { SessionId = id, StudentId = studentId }));
        foreach (var examQuestion in session.Exam.Questions.OrderBy(q => q.Order))
        {
            var snapshotQuestion = new SessionQuestion
            {
                SourceQuestionId = examQuestion.QuestionId,
                Text = examQuestion.Question.Text,
                Order = examQuestion.Order,
                Points = examQuestion.Points
            };
            snapshotQuestion.Options.AddRange(examQuestion.Question.Options.OrderBy(o => o.Order).Select(o => new SessionOption
            {
                Text = o.Text,
                Order = o.Order,
                IsCorrect = o.IsCorrect
            }));
            session.Questions.Add(snapshotQuestion);
        }
        session.ActivatedAt = Now;
        session.AccessCode ??= Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        session.Status = "Active";
        return true;
    });

    private static void ValidateSchedule(DateTime opensAt, DateTime closesAt, int durationMinutes)
    {
        if (closesAt <= opensAt)
            throw new BusinessException(
                $"Giờ đóng phải sau giờ mở ({opensAt.AddHours(7):dd/MM/yyyy HH:mm}, giờ Việt Nam). Kiểm tra cả ngày và giờ đóng.",
                field: nameof(SessionDraft.ClosesAt));
        if (durationMinutes <= 0)
            throw new BusinessException("Thời lượng làm bài phải ít nhất 1 phút.", field: nameof(SessionDraft.DurationMinutes));
        if (durationMinutes > (DateTime.MaxValue - closesAt).TotalMinutes)
            throw new BusinessException(
                "Thời lượng vượt giới hạn thời gian được hỗ trợ. Chọn thời lượng hoặc ngày thi nhỏ hơn.",
                field: nameof(SessionDraft.DurationMinutes));
    }
}
