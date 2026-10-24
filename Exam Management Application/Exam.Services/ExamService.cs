using System.Data;
using Exam.Core;
using Exam.Data;
using Microsoft.EntityFrameworkCore;
namespace Exam.Services;

public class ExamService(ExamDbContext db, TimeProvider clock)
{
    DateTime Now => clock.GetUtcNow().UtcDateTime;
    public static string SessionState(ExamSession s, DateTime now) => s.Status == "Active" ? (now < s.OpensAt ? "Scheduled" : now >= s.ClosesAt ? "Closed" : "Open") : s.Status;
    SessionDto Dto(ExamSession s, long? attempt) => new(s.Id, s.Title, s.Exam.Subject.Name, s.OpensAt, s.ClosesAt, s.DurationMinutes, SessionState(s, Now), attempt, s.Published, s.ReviewPolicy);
    public async Task<SessionListVm> SessionsAsync(int student, string? search = null, string? status = null)
    {
        var list = await db.ExamSessions.Include(x => x.Exam).ThenInclude(x => x.Subject).Where(x => x.Candidates.Any(c => c.StudentId == student) && x.Status != "Draft").OrderByDescending(x => x.OpensAt).ToListAsync();
        var attempts = await db.Attempts.Where(x => x.StudentId == student).ToDictionaryAsync(x => x.SessionId, x => x.Id);
        var rows = list.Select(s => Dto(s, attempts.TryGetValue(s.Id, out var a) ? a : null)).Where(s => (string.IsNullOrWhiteSpace(search) || (s.Title + " " + s.Subject).Contains(search, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(status) || s.Status == status)).ToList();
        return new(rows, search, status);
    }
    public async Task<SessionDetailVm> DetailAsync(int student, int id)
    {
        var s = await db.ExamSessions.Include(x => x.Exam).ThenInclude(x => x.Subject).SingleOrDefaultAsync(x => x.Id == id && x.Candidates.Any(c => c.StudentId == student)) ?? throw new BusinessException("Không tìm thấy đợt thi.", 404);
        var a = await db.Attempts.Where(x => x.SessionId == id && x.StudentId == student).Select(x => (long?)x.Id).SingleOrDefaultAsync();
        return new(Dto(s, a), "Một lượt thi mỗi đợt. Đáp án chỉ được chấm khi đã được máy chủ xác nhận lưu. Hết giờ hệ thống tự kết thúc; tải lại trang không làm tăng thời gian.");
    }
    /// <summary>Retry a whole serializable transaction, clearing tracked state before every retry.</summary>
    async Task<T> Serial<T>(Func<Task<T>> work)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var result = await work(); await db.SaveChangesAsync(); await tx.CommitAsync(); return result; });
    }
    /// <summary>Create one immutable, shuffled snapshot per candidate and session, or return the existing attempt.</summary>
    public Task<long> StartAsync(int student, int sessionId) => Serial(async () => { var old = await db.Attempts.SingleOrDefaultAsync(x => x.StudentId == student && x.SessionId == sessionId); if (old != null) return old.Id; var s = await db.ExamSessions.Include(x => x.Candidates).Include(x => x.Exam).ThenInclude(x => x.Questions).ThenInclude(x => x.Question).ThenInclude(x => x.Options).SingleOrDefaultAsync(x => x.Id == sessionId) ?? throw new BusinessException("Không tìm thấy đợt thi.", 404); if (!s.Candidates.Any(x => x.StudentId == student)) throw new BusinessException("Bạn không được phân công đợt này.", 403); var now = Now; if (s.Status != "Active" || now < s.OpensAt || now >= s.ClosesAt) throw new BusinessException("Đợt thi chưa mở hoặc đã đóng.", 409); if (s.Exam.Status != "Ready") throw new BusinessException("Đề chưa sẵn sàng.", 409); s.HasOpened = true; var a = new Attempt { SessionId = s.Id, StudentId = student, StartedAt = now, Deadline = ExamRules.Deadline(now, s.DurationMinutes, s.ClosesAt) }; var questions = s.Exam.Questions.OrderBy(q => q.Order).ToList(); if (s.ShuffleQuestions) Random.Shared.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(questions)); foreach (var q in questions) { var aq = new AttemptQuestion { SourceQuestionId = q.QuestionId, Text = q.Question.Text, Points = q.Points, Order = a.Questions.Count + 1 }; var opts = q.Question.Options.OrderBy(x => x.Order).ToList(); if (s.ShuffleOptions) Random.Shared.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(opts)); foreach (var o in opts) aq.Options.Add(new() { Text = o.Text, IsCorrect = o.IsCorrect, Order = aq.Options.Count + 1 }); aq.Response = new Response { SavedAt = now }; a.Questions.Add(aq); } db.Attempts.Add(a); await db.SaveChangesAsync(); return a.Id; });
    IQueryable<Attempt> Full => db.Attempts.Include(a => a.Session).ThenInclude(s => s.Exam).Include(a => a.Result).Include(a => a.Questions).ThenInclude(q => q.Options).Include(a => a.Questions).ThenInclude(q => q.Response);
    async Task<Attempt> Own(int student, long id)
    {
        var a = await Full.SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không tìm thấy bài thi.", 404);
        if (a.StudentId != student)
            throw new BusinessException("Bạn không có quyền xem bài thi này.", 403);
        return a;
    }
    public async Task<AttemptVm> TakeAsync(int student, long id)
    {
        await ExpireAsync(id);
        var a = await Own(student, id);
        return new(a.Id, a.SessionId, a.Session.Title, Now, a.Deadline, a.Status, a.Questions.OrderBy(x => x.Order).Select(q => new QuestionDto(q.Id, q.Text, q.Order, q.Points, q.Options.OrderBy(x => x.Order).Select(o => new OptionDto(o.Id, o.Text, o.Order)).ToList(), q.Response?.SelectedOptionId, q.Response?.IsFlagged ?? false)).ToList());
    }
    public async Task<AttemptStatusDto> StatusAsync(int student, long id)
    {
        await ExpireAsync(id);
        var a = await Own(student, id);
        return new(a.Id, Now, a.Deadline, a.Status);
    }
    /// <summary>Accept only options from this question while server time remains strictly before the deadline.</summary>
    public Task<AttemptStatusDto> SaveAsync(int student, long id, SaveAnswerRequest request) => Serial(async () => { var a = await Own(student, id); var now = Now; if (a.Status != "InProgress" || now >= a.Deadline) throw new BusinessException("Đã hết thời gian làm bài.", 409); var q = a.Questions.SingleOrDefault(x => x.Id == request.QuestionId) ?? throw new BusinessException("Câu hỏi không thuộc bài thi."); if (request.OptionId != null && !q.Options.Any(x => x.Id == request.OptionId)) throw new BusinessException("Đáp án không thuộc câu hỏi."); q.Response ??= new Response(); q.Response.SelectedOptionId = request.OptionId; q.Response.IsFlagged = request.IsFlagged; q.Response.SavedAt = now; return new AttemptStatusDto(a.Id, now, a.Deadline, a.Status); });
    public Task<long> SubmitAsync(int student, long id) => Serial(async () => { var a = await Own(student, id); Grade(a, Now >= a.Deadline ? "Timeout" : "Manual"); return a.Id; });
    /// <summary>Grade only stored snapshot responses and create at most one result, even for repeated submits.</summary>
    void Grade(Attempt a, string reason)
    {
        if (a.Result != null)
            return;
        a.Status = "Submitted";
        a.EndReason ??= reason;
        a.SubmittedAt ??= a.EndReason == "Timeout" ? a.Deadline : Now;
        var correct = a.Questions.Where(q => q.Options.Any(o => o.Id == q.Response?.SelectedOptionId && o.IsCorrect)).ToList();
        a.Result = new ExamResult { Score = ExamRules.Score(correct.Sum(q => q.Points), a.Questions.Sum(q => q.Points)), CorrectCount = correct.Count, GradedAt = Now };
    }
    /// <summary>Recover expired or incompletely graded attempts at startup and during periodic sweeps.</summary>
    public Task<int> ExpireAsync(long? id = null) => Serial(async () => { var now = Now; var query = Full.Where(a => (a.Status == "InProgress" && a.Deadline <= now) || (a.Status == "Submitted" && a.Result == null)); if (id != null) query = query.Where(a => a.Id == id); var rows = await query.ToListAsync(); foreach (var a in rows) Grade(a, "Timeout"); return rows.Count; });
    public async Task<ResultVm> ResultAsync(int student, long id)
    {
        await ExpireAsync(id);
        var a = await Own(student, id);
        var pub = a.Session.Published && a.Result != null;
        var review = pub && a.Session.ReviewPolicy != "ScoreOnly";
        return new(a.Id, a.Session.Title, a.Status == "Submitted" && a.Result == null ? "PendingGrading" : a.Status, pub, pub ? a.Result!.Score : null, pub ? a.Result!.CorrectCount : null, a.Questions.Count, a.SubmittedAt, a.Session.ReviewPolicy, review ? a.Questions.OrderBy(q => q.Order).Select(q => new ReviewQuestionDto(q.Text, q.Options.SingleOrDefault(o => o.Id == q.Response?.SelectedOptionId)?.Text, a.Session.ReviewPolicy == "CorrectAnswers" ? q.Options.Single(o => o.IsCorrect).Text : null, a.Session.ReviewPolicy == "CorrectAnswers" ? q.Options.Any(o => o.Id == q.Response?.SelectedOptionId && o.IsCorrect) : null, q.Points)).ToList() : []);
    }
    public async Task<HistoryVm> HistoryAsync(int student)
    {
        var ids = await db.Attempts.Where(a => a.StudentId == student).OrderByDescending(a => a.StartedAt).Select(a => a.Id).ToListAsync();
        var rows = new List<ResultVm>();
        foreach (var id in ids)
            rows.Add(await ResultAsync(student, id));
        return new(rows);
    }
}





