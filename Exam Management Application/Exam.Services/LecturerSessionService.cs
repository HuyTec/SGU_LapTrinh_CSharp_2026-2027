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
            s.Status == "Active" && !s.HasOpened && s.OpensAt > now)).ToList();
    }

    private async Task<ExamSession> LoadAsync(int lecturer, int id) => await Owned(lecturer)
        .Include(s => s.Exam).ThenInclude(p => p.Subject)
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
        if (action is "postpone" or "cancel")
        {
            if (session.Status != "Active" || session.HasOpened || Now >= session.OpensAt ||
                await db.Attempts.AnyAsync(a => a.SessionId == id))
                throw new BusinessException("Chỉ hoãn hoặc hủy đợt đã kích hoạt trước giờ mở lần đầu.", 409);
            session.Status = action == "postpone" ? "Draft" : "Cancelled";
            return true;
        }
        if (action != "activate")
            throw new BusinessException("Thao tác không hợp lệ.");
        EnsureDraft(session);
        if (session.Exam.Status != "Ready")
            throw new BusinessException("Đề thi chưa sẵn sàng. Xác nhận đề sẵn sàng trước khi kích hoạt đợt.");
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
        var overlap = await db.ExamSessions.AnyAsync(other => other.Id != id && other.Status == "Active" &&
            other.OpensAt < session.ClosesAt.AddMinutes(session.DurationMinutes) &&
            session.OpensAt < other.ClosesAt.AddMinutes(other.DurationMinutes) &&
            (other.Classes.Any(c => c.Class.IsActive && (classIds.Contains(c.ClassId) ||
                c.Class.Students.Any(m => members.Contains(m.StudentId)))) ||
             db.Attempts.Any(a => a.SessionId == other.Id && a.Status == "InProgress" && members.Contains(a.StudentId))));
        if (overlap)
            throw new BusinessException("Lớp hoặc thí sinh có thời gian làm bài trùng với đợt khác. Lịch được kiểm tra đến giờ đóng nhận lượt cộng thời lượng làm bài.", 409);

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
