using System.Data;
using System.Text.Json;
using Exam.Core;
using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Exam.Services;

public class AdminService(ExamDbContext db, UserManager<AppUser> users, TimeProvider clock)
{
    DateTime Now => clock.GetUtcNow().UtcDateTime;
    static BusinessException Denied() => new("Bạn không có quyền với dữ liệu này.", 403);
    async Task<List<int>> SubjectIds(int uid) => await db.LecturerSubjects.Where(x => x.LecturerId == uid).Select(x => x.SubjectId).ToListAsync();
    async Task RequireSubject(int uid, string role, int subject)
    {
        if (role != "Lecturer" || !await db.LecturerSubjects.AnyAsync(x => x.LecturerId == uid && x.SubjectId == subject))
            throw Denied();
    }
    async Task<ExamSession> Session(int uid, string role, int id)
    {
        var s = await db.ExamSessions.Include(x => x.Exam).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không tìm thấy đợt thi.", 404);
        if (role == "Lecturer" && s.ManagedBy == uid && await db.LecturerSubjects.AnyAsync(x => x.LecturerId == uid && x.SubjectId == s.Exam.SubjectId))
            return s;
        throw Denied();
    }
    void Role(string actual, string expected)
    {
        if (actual != expected)
            throw Denied();
    }
    /// <summary>Build an editor catalog whose resources and lookup choices follow the active role and subject scope.</summary>
    public async Task<AdminCatalogDto> CatalogAsync(int uid, string role)
    {
        var rs = new List<ResourceDto>();
        List<ChoiceDto> C(IEnumerable<(int id, string label)> pairs) => pairs.Select(x => new ChoiceDto(x.id.ToString(), x.label)).ToList();
        var subids = await SubjectIds(uid);
        var subjects = await db.Subjects.Where(s => role != "Lecturer" || subids.Contains(s.Id)).ToListAsync();
        var subs = C(subjects.Select(s => (s.Id, s.Code + " — " + s.Name)));
        var topics = await db.Topics.Include(x => x.Subject).Where(x => role != "Lecturer" || subids.Contains(x.SubjectId)).ToListAsync();
        var tops = C(topics.Select(x => (x.Id, x.Subject.Name + " — " + x.Name)));
        var people = await db.Users.Where(x => x.IsActive).ToListAsync();
        var lecturers = new List<ChoiceDto>();
        var students = new List<ChoiceDto>();
        foreach (var u in people)
        {
            if (await users.IsInRoleAsync(u, "Lecturer"))
                lecturers.Add(new(u.Id.ToString(), u.FullName + " (" + u.UserName + ")"));
            if (await users.IsInRoleAsync(u, "Student"))
                students.Add(new(u.Id.ToString(), u.StudentCode + " — " + u.FullName));
        }
        var questions = await db.Questions.Include(x => x.Topic).Where(x => subids.Contains(x.Topic.SubjectId) && x.IsActive).ToListAsync();
        var papers = await db.Exams.Include(x => x.Subject).Where(x => x.Status == "Ready" && x.Subject.IsActive && subids.Contains(x.SubjectId)).ToListAsync();
        var sessions = await db.ExamSessions.Where(x => x.ManagedBy == uid && subids.Contains(x.Exam.SubjectId)).ToListAsync();
        List<ChoiceDto> Choices(params string[] v) => v.Select(x => new ChoiceDto(x, x)).ToList();
        if (role == "Administrator")
        {
            rs.Add(new("users", "Tài khoản / thí sinh", [new("userName", "Tên đăng nhập", Required: true), new("fullName", "Họ tên", Required: true), new("studentCode", "Mã thí sinh"), new("email", "Email"), new("password", "Mật khẩu mới", "password"), new("roles", "Vai trò", "multiselect", true, Choices("Student", "Lecturer", "Administrator")), new("isActive", "Hoạt động", "checkbox")]));
            rs.Add(new("assignments", "Phân công giảng viên", [new("lecturerId", "Giảng viên", "select", true, lecturers), new("subjectId", "Môn học", "select", true, subs)]));
            rs.Add(new("classes", "Danh mục lớp", [new("subjectId", "Môn học", "select", true, subs), new("code", "Mã lớp", Required: true), new("isActive", "Hoạt động", "checkbox")]));
            rs.Add(new("subjects", "Danh mục môn học", [new("code", "Mã môn", Required: true), new("name", "Tên môn", Required: true), new("isActive", "Đang sử dụng", "checkbox")]));
        }
        if (role == "Lecturer")
        {
            rs.Add(new("topics", "Chủ đề", [new("subjectId", "Môn học", "select", true, subs), new("name", "Tên chủ đề", Required: true), new("isActive", "Hoạt động", "checkbox")]));
            rs.Add(new("questions", "Ngân hàng câu hỏi", [new("version", "Phiên bản", "hidden"), new("topicId", "Chủ đề", "select", true, tops), new("text", "Nội dung", Required: true), new("difficulty", "Độ khó", "select", true, Choices("Easy", "Medium", "Hard")), new("option1", "Đáp án A", Required: true), new("option2", "Đáp án B", Required: true), new("option3", "Đáp án C", Required: true), new("option4", "Đáp án D", Required: true), new("correctIndex", "Đáp án đúng", "select", true, [new("1", "A"), new("2", "B"), new("3", "C"), new("4", "D")]), new("isActive", "Hoạt động", "checkbox")]));
            rs.Add(new("exams", "Xây dựng đề thi", [new("subjectId", "Môn học", "select", true, subs), new("title", "Tên đề", Required: true), new("questionPoints", "Chọn câu hỏi và điểm", "questionpoints", true, C(questions.Select(x => (x.Id, x.Topic.Name + " — " + x.Text))))]));
        }
        if (role == "Lecturer")
        {
            rs.Add(new("sessions", "Tổ chức đợt thi", [new("examId", "Đề sẵn sàng", "select", true, C(papers.Select(x => (x.Id, x.Subject.Name + " — " + x.Title)))), new("title", "Tên đợt", Required: true), new("opensAt", "Mở lúc (giờ địa phương)", "datetime", true), new("closesAt", "Đóng lúc (giờ địa phương)", "datetime", true), new("durationMinutes", "Thời lượng phút", "number", true), new("shuffleQuestions", "Trộn câu hỏi", "checkbox"), new("shuffleOptions", "Trộn đáp án", "checkbox"), new("reviewPolicy", "Chính sách công bố", "select", true, Choices("ScoreOnly", "Answers", "CorrectAnswers"))]));
            rs.Add(new("candidates", "Phân công thí sinh", [new("sessionId", "Đợt thi", "select", true, C(sessions.Select(x => (x.Id, x.Title)))), new("studentId", "Thí sinh", "select", true, students)]));
        }
        if (role == "Lecturer")
            rs.Add(new("monitor", "Theo dõi / kết quả", [], false, false));
        return new((await users.FindByIdAsync(uid.ToString()))?.FullName ?? "", role, rs);
    }
    public async Task EnsureSessionReadAsync(int uid, string role, int id)
    {
        await Session(uid, role, id);
    }
    static Dictionary<string, object?> D(params (string key, object? value)[] v) => v.ToDictionary(x => x.key, x => x.value);
    public async Task<AdminPageDto> ListAsync(int uid, string role, string resource, string? search, int page, int size, int? exactId = null, int? subjectId = null, int? topicId = null, string? difficulty = null, bool? isActive = null)
    {
        if (!(await CatalogAsync(uid, role)).Resources.Any(x => x.Key == resource))
            throw Denied();
        var ids = await SubjectIds(uid);
        List<Dictionary<string, object?>> rows = [];
        switch (resource)
        {
            case "users":
                foreach (var x in await db.Users.ToListAsync())
                    rows.Add(D(("id", x.Id), ("userName", x.UserName), ("fullName", x.FullName), ("studentCode", x.StudentCode), ("requestedRole",x.RequestedRole), ("email", x.Email), ("role",string.Join(", ",await users.GetRolesAsync(x))),("roles",(await users.GetRolesAsync(x)).ToArray()), ("isActive", x.IsActive)));
                break;
            case "subjects":
                var matchingSubjects = db.Subjects.Where(x => isActive == null || x.IsActive == isActive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    matchingSubjects = matchingSubjects.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
                }
                rows = (await matchingSubjects.OrderBy(x => x.Code).ToListAsync())
                    .Select(x => D(("id", x.Id), ("code", x.Code), ("name", x.Name), ("isActive", x.IsActive))).ToList();
                break;
            case "classes":
                var classes = from c in db.Set<ExamClass>().AsNoTracking()
                              join subject in db.Subjects on c.SubjectId equals subject.Id
                              orderby subject.Name, c.Code
                              select new { c.Id, c.Code, c.SubjectId, Subject = subject.Name, c.IsActive };
                rows = (await classes.ToListAsync()).Select(c =>
                    D(("id", c.Id), ("code", c.Code), ("subjectId", c.SubjectId), ("subject", c.Subject), ("isActive", c.IsActive))).ToList();
                break;
            case "assignments":
                rows = (await db.LecturerSubjects.Include(x => x.Lecturer).Include(x => x.Subject).ToListAsync()).Select(x => D(("id", x.Id), ("lecturerId", x.LecturerId), ("lecturer", x.Lecturer.FullName), ("subjectId", x.SubjectId), ("subject", x.Subject.Name))).ToList();
                break;
            case "topics":
                rows = (await db.Topics.Include(x => x.Subject).Where(x => ids.Contains(x.SubjectId)).ToListAsync()).Select(x => D(("id", x.Id), ("subjectId", x.SubjectId), ("subject", x.Subject.Name), ("name", x.Name), ("isActive", x.IsActive))).ToList();
                break;
            case "questions":
                var questionQuery = db.Questions.Include(x => x.Topic).ThenInclude(x => x.Subject).Include(x => x.Options).Where(x => ids.Contains(x.Topic.SubjectId));
                if (subjectId != null)
                    questionQuery = questionQuery.Where(x => x.Topic.SubjectId == subjectId);
                if (topicId != null)
                    questionQuery = questionQuery.Where(x => x.TopicId == topicId);
                if (!string.IsNullOrWhiteSpace(difficulty))
                    questionQuery = questionQuery.Where(x => x.Difficulty == difficulty);
                if (exactId != null)
                    questionQuery = questionQuery.Where(x => x.Id == exactId);
                if (!string.IsNullOrWhiteSpace(search))
                    questionQuery = questionQuery.Where(x => x.Text.Contains(search) || x.Topic.Name.Contains(search) || x.Topic.Subject.Name.Contains(search) || x.Difficulty.Contains(search));
                var questionCount = await questionQuery.CountAsync();
                rows = (await questionQuery.OrderBy(x => x.Id).Skip((Math.Max(1, page) - 1) * Math.Clamp(size, 1, 200)).Take(Math.Clamp(size, 1, 200)).ToListAsync()).Select(x => D(("id", x.Id), ("version", Convert.ToBase64String(x.RowVersion)), ("topicId", x.TopicId), ("topic", x.Topic.Name), ("subject", x.Topic.Subject.Name), ("text", x.Text), ("difficulty", x.Difficulty), ("option1", x.Options.SingleOrDefault(o => o.Order == 1)?.Text), ("option2", x.Options.SingleOrDefault(o => o.Order == 2)?.Text), ("option3", x.Options.SingleOrDefault(o => o.Order == 3)?.Text), ("option4", x.Options.SingleOrDefault(o => o.Order == 4)?.Text), ("correctIndex", x.Options.SingleOrDefault(o => o.IsCorrect)?.Order), ("isActive", x.IsActive))).ToList();
                return new(rows, questionCount, Math.Max(1, page), Math.Clamp(size, 1, 200));
            case "exams":
                rows = (await db.Exams.Include(x => x.Questions).Include(x => x.Subject).Where(x => ids.Contains(x.SubjectId)).ToListAsync()).Select(x => D(("id", x.Id), ("title", x.Title), ("subjectId", x.SubjectId), ("subject", x.Subject.Name), ("status", x.Status), ("questionIds", x.Questions.Select(q => q.QuestionId).ToArray()), ("pointsPerQuestion", x.Questions.FirstOrDefault()?.Points ?? 1), ("questionPoints", x.Questions.ToDictionary(q => q.QuestionId.ToString(), q => q.Points)))).ToList();
                break;
            case "sessions":
                rows = (await db.ExamSessions.Include(x => x.Exam).Where(x => x.ManagedBy == uid && ids.Contains(x.Exam.SubjectId)).ToListAsync()).Select(x => D(("id", x.Id), ("examId", x.ExamId), ("exam", x.Exam.Title), ("title", x.Title), ("opensAt", x.OpensAt), ("closesAt", x.ClosesAt), ("durationMinutes", x.DurationMinutes), ("status", ExamService.SessionState(x, Now)), ("shuffleQuestions", x.ShuffleQuestions), ("shuffleOptions", x.ShuffleOptions), ("reviewPolicy", x.ReviewPolicy), ("published", x.Published),("publishedAt",x.PublishedAt))).ToList();
                break;
            case "candidates":
                rows = (await db.SessionCandidates.Include(x => x.Student).Include(x => x.Session).Where(x => x.Session.ManagedBy == uid && ids.Contains(x.Session.Exam.SubjectId)).ToListAsync()).Select(x => D(("id", x.Id), ("sessionId", x.SessionId), ("session", x.Session.Title), ("studentId", x.StudentId), ("student", x.Student.FullName))).ToList();
                break;
            case "monitor":
                var ss = await db.ExamSessions.Where(x => x.ManagedBy == uid && ids.Contains(x.Exam.SubjectId)).Select(x => x.Id).ToListAsync();
                var candidates = await db.SessionCandidates.Include(x => x.Student).Include(x => x.Session).Where(x => ss.Contains(x.SessionId)).ToListAsync();
                var attempts = await db.Attempts.Include(x => x.Result).Where(x => ss.Contains(x.SessionId)).ToListAsync();
                foreach (var c in candidates)
                {
                    var a = attempts.SingleOrDefault(x => x.SessionId == c.SessionId && x.StudentId == c.StudentId);
                    rows.Add(D(("id", c.Id), ("sessionId", c.SessionId), ("session", c.Session.Title), ("student", c.Student.FullName), ("studentCode", c.Student.StudentCode), ("status", a?.Status ?? "NotStarted"), ("score", a?.Result?.Score), ("submittedAt", a?.SubmittedAt), ("published", c.Session.Published)));
                }
                break;
            default:
                throw new BusinessException("Danh mục không tồn tại.", 404);
        }
        if (exactId != null)
            rows = rows.Where(r => Convert.ToInt32(r["id"]) == exactId).ToList();
        if (resource != "subjects" && !string.IsNullOrWhiteSpace(search))
            rows = rows.Where(r => r.Values.Any(v => v?.ToString()?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)).ToList();
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 200);
        return new(rows.Skip((page - 1) * size).Take(size).ToList(), rows.Count, page, size);
    }
    string S(Dictionary<string, JsonElement> v, string key, string fallback = "") => v.TryGetValue(key, out var e) ? e.ToString().Trim() : fallback;
    int I(Dictionary<string, JsonElement> v, string key, int fallback = 0) => int.TryParse(S(v, key), out var n) ? n : fallback;
    bool B(Dictionary<string, JsonElement> v, string key, bool fallback = true) => bool.TryParse(S(v, key), out var x) ? x : fallback;
    DateTime T(Dictionary<string, JsonElement> v, string key) => DateTime.TryParse(S(v, key), null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d.ToUniversalTime() : throw new BusinessException("Thời gian không hợp lệ.");
    string Required(Dictionary<string, JsonElement> v, string key)
    {
        var s = S(v, key);
        if (string.IsNullOrWhiteSpace(s))
            throw new BusinessException("Thiếu trường " + key);
        return s;
    }
    void Identity(IdentityResult r)
    {
        if (!r.Succeeded)
            throw new BusinessException(string.Join("; ", r.Errors.Select(x => x.Description)));
    }
    /// <summary>Validate and persist administrator changes atomically; forms never implement business rules.</summary>
    public async Task<int> SaveAsync(int uid, string role, string resource, int? id, AdminSaveRequest request)
    {
        if (resource == "candidates")
            return await SaveInternalAsync(uid, role, resource, id, request);
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var saved = await SaveInternalAsync(uid, role, resource, id, request); await tx.CommitAsync(); return saved; });
    }
    private async Task<int> SaveInternalAsync(int uid, string role, string resource, int? id, AdminSaveRequest request)
    {
        var v = request.Values;
        if (!(await CatalogAsync(uid, role)).Resources.Any(x => x.Key == resource && x.CanCreate))
            throw Denied();
        switch (resource)
        {
            case "classes":
                Role(role, "Administrator");
                var examClass = id == null ? new ExamClass() : await db.Set<ExamClass>().FindAsync(id)
                    ?? throw new BusinessException("Không tìm thấy lớp.", 404);
                var classCode = Required(v, "code");
                if (classCode.Length > 50)
                    throw new BusinessException("Mã lớp tối đa 50 ký tự.");
                var classSubjectId = I(v, "subjectId");
                var classSubject = await db.Subjects.FindAsync(classSubjectId)
                    ?? throw new BusinessException("Môn học không hợp lệ.");
                var classActive = B(v, "isActive", examClass.IsActive);
                if (!classSubject.IsActive && (classActive || classSubjectId != examClass.SubjectId))
                    throw new BusinessException("Không thể chọn hoặc kích hoạt lớp thuộc môn đã ngừng hoạt động.");
                if (id != null && classSubjectId != examClass.SubjectId && await ClassIsReferencedAsync(examClass.Id))
                    throw new BusinessException("Lớp đã được sử dụng nên không thể đổi môn.", 409);
                if (await db.Set<ExamClass>().AnyAsync(c => c.SubjectId == classSubjectId && c.Code == classCode && c.Id != examClass.Id))
                    throw new BusinessException("Mã lớp đã tồn tại trong môn này.", 409);
                examClass.SubjectId = classSubjectId;
                examClass.Code = classCode;
                examClass.IsActive = classActive;
                if (id == null)
                    db.Add(examClass);
                await db.SaveChangesAsync();
                return examClass.Id;
            case "subjects":
                Role(role, "Administrator");
                var sub = id == null ? new Subject() : await db.Subjects.FindAsync(id) ?? throw new BusinessException("Không thấy môn.", 404);
                sub.Code = Required(v, "code");
                sub.Name = Required(v, "name");
                if (sub.Code.Length > 30 || sub.Name.Length > 150)
                    throw new BusinessException("Mã môn tối đa 30 ký tự, tên môn tối đa 150 ký tự.");
                sub.IsActive = B(v, "isActive", sub.IsActive);
                if (await db.Subjects.AnyAsync(x => x.Code == sub.Code && x.Id != sub.Id))
                    throw new BusinessException("Mã môn trùng.");
                if (id == null)
                    db.Subjects.Add(sub);
                await db.SaveChangesAsync();
                return sub.Id;
            case "users":
                Role(role, "Administrator");
                var u = id == null ? new AppUser() : await users.FindByIdAsync(id.ToString()!) ?? throw new BusinessException("Không thấy tài khoản.", 404);
                var selectedRoles=v.TryGetValue("roles",out var roleArray)&&roleArray.ValueKind==JsonValueKind.Array?roleArray.EnumerateArray().Select(e=>e.GetString()??"").Distinct().ToList():[Required(v,"role")];
                if(selectedRoles.Count==0||selectedRoles.Any(r=>!new[]{"Student","Lecturer","Administrator"}.Contains(r)))
                    throw new BusinessException("Vai trò không hợp lệ.");
                u.UserName = Required(v, "userName");
                u.FullName = Required(v, "fullName");
                u.Email = S(v, "email") is { Length: > 0 } email ? email : null;if(u.Email!=null&&!System.Net.Mail.MailAddress.TryCreate(u.Email,out _))throw new BusinessException("Email không hợp lệ.");
                u.StudentCode = S(v, "studentCode") is { Length: > 0 } code ? code : null;
                u.IsActive = B(v, "isActive");if(u.IsActive&&selectedRoles.Count>0)u.RequestedRole=null;if(id==uid&&(!u.IsActive||!selectedRoles.Contains("Administrator")))throw new BusinessException("Không thể tự khóa hoặc tự gỡ quyền quản trị.");
                if(selectedRoles.Contains("Student")&&u.StudentCode==null)
                    throw new BusinessException("Thí sinh cần mã thí sinh.");
                if (u.StudentCode != null && await db.Users.AnyAsync(x => x.StudentCode == u.StudentCode && x.Id != u.Id))
                    throw new BusinessException("Mã thí sinh trùng.");
                if (id == null)
                    Identity(await users.CreateAsync(u, Required(v, "password")));
                else
                {
                    Identity(await users.UpdateAsync(u));
                    if (S(v, "password").Length > 0)
                    {
                        Identity(await users.ResetPasswordAsync(u, await users.GeneratePasswordResetTokenAsync(u), S(v, "password")));
                    }
                }
                var old = await users.GetRolesAsync(u);
                if (old.Count > 0)
                    Identity(await users.RemoveFromRolesAsync(u, old));
                Identity(await users.AddToRolesAsync(u,selectedRoles));
                await Revoke(u.Id);
                return u.Id;
            case "assignments":
                Role(role, "Administrator");
                if (id != null)
                    throw new BusinessException("Xóa phân công cũ rồi tạo lại.");
                var lecturer = await users.FindByIdAsync(I(v, "lecturerId").ToString());
                if (lecturer == null || !lecturer.IsActive || !await users.IsInRoleAsync(lecturer, "Lecturer"))
                    throw new BusinessException("Giảng viên không hợp lệ.");
                var subject = await db.Subjects.FindAsync(I(v, "subjectId"));
                if (subject?.IsActive != true)
                    throw new BusinessException("Môn học không hoạt động.");
                if (await db.LecturerSubjects.AnyAsync(x => x.LecturerId == lecturer.Id && x.SubjectId == subject.Id))
                    throw new BusinessException("Đã có phân công.");
                var assignment = new LecturerSubject { LecturerId = lecturer.Id, SubjectId = subject.Id };
                db.LecturerSubjects.Add(assignment);
                await db.SaveChangesAsync();
                return assignment.Id;
            case "topics":
                var topic = id == null ? new Topic() : await db.Topics.FindAsync(id) ?? throw new BusinessException("Không thấy chủ đề.", 404);
                if (id != null)
                    await RequireSubject(uid, role, topic.SubjectId);
                await RequireSubject(uid, role, I(v, "subjectId"));
                if (id != null && topic.SubjectId != I(v, "subjectId") && await db.Questions.AnyAsync(x => x.TopicId == topic.Id))
                    throw new BusinessException("Chủ đề đã có câu hỏi, không thể chuyển sang môn khác.", 409);
                topic.SubjectId = I(v, "subjectId");
                topic.Name = Required(v, "name");
                topic.IsActive = B(v, "isActive");
                if (!await db.Subjects.AnyAsync(x => x.Id == topic.SubjectId && x.IsActive))
                    throw new BusinessException("Môn học đã ngừng dùng.");
                if (id == null)
                    db.Topics.Add(topic);
                await db.SaveChangesAsync();
                return topic.Id;
            case "questions":
                var q = id == null ? new Question { CreatedBy = uid } : await db.Questions.Include(x => x.Topic).Include(x => x.Options).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không thấy câu hỏi.", 404);
                if (id != null)
                {
                    await RequireSubject(uid, role, q.Topic.SubjectId);
                    if (string.IsNullOrWhiteSpace(S(v, "version")) || !string.Equals(S(v, "version"), Convert.ToBase64String(q.RowVersion), StringComparison.Ordinal))
                        throw new BusinessException("Câu hỏi đã được cập nhật hoặc thiếu phiên bản. Vui lòng tải lại trước khi sửa.", 409);
                    if (await db.ExamQuestions.AnyAsync(x => x.QuestionId == q.Id && x.Exam.Status == "Ready"))
                        throw new BusinessException("Câu hỏi đã thuộc đề sẵn sàng, không thể sửa; tạo câu hỏi mới.");
                }
                var qt = await db.Topics.FindAsync(I(v, "topicId")) ?? throw new BusinessException("Chủ đề không hợp lệ.");
                await RequireSubject(uid, role, qt.SubjectId);
                if (!qt.IsActive || !await db.Subjects.AnyAsync(x => x.Id == qt.SubjectId && x.IsActive))
                    throw new BusinessException("Chủ đề hoặc môn đã ngừng dùng.");
                q.TopicId = qt.Id;
                q.Text = Required(v, "text");
                q.Difficulty = S(v, "difficulty", "Medium");
                q.IsActive = B(v, "isActive");
                var correct = I(v, "correctIndex");
                if (correct < 1 || correct > 4)
                    throw new BusinessException("Chọn đúng một đáp án đúng.");
                foreach (var existingOption in q.Options)
                    existingOption.IsCorrect = false;
                if (id != null)
                {
                    // Even option-only edits must advance the question concurrency token.
                    db.Entry(q).Property(x => x.Text).IsModified = true;
                    await db.SaveChangesAsync();
                }
                for (var n = 1; n <= 4; n++)
                {
                    var o = q.Options.SingleOrDefault(x => x.Order == n);
                    if (o == null)
                    {
                        o = new AnswerOption { Order = n };
                        q.Options.Add(o);
                    }
                    o.Text = Required(v, "option" + n);
                    o.IsCorrect = n == correct;
                }
                if (q.Options.Select(x => x.Text).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
                    throw new BusinessException("Các đáp án không được trùng.");
                if (id == null)
                    db.Questions.Add(q);
                await db.SaveChangesAsync();
                return q.Id;
            case "exams":
                var ex = id == null ? new ExamPaper { CreatedBy = uid } : await db.Exams.Include(x => x.Questions).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không thấy đề.", 404);
                if (id != null)
                {
                    await RequireSubject(uid, role, ex.SubjectId);
                    if (ex.Status != "Draft")
                        throw new BusinessException("Chỉ sửa đề nháp.");
                }
                await RequireSubject(uid, role, I(v, "subjectId"));
                ex.SubjectId = I(v, "subjectId");
                if (!await db.Subjects.AnyAsync(x => x.Id == ex.SubjectId && x.IsActive))
                    throw new BusinessException("Môn học đã ngừng dùng.");
                ex.Title = Required(v, "title");
                var qids = v.TryGetValue("questionIds", out var qe) && qe.ValueKind == JsonValueKind.Array ? qe.EnumerateArray().Select(e => int.Parse(e.ToString())).Distinct().ToList() : [];
                if (qids.Count == 0)
                    throw new BusinessException("Chọn ít nhất một câu hỏi.");
                var valid = await db.Questions.Include(x => x.Topic).Where(x => qids.Contains(x.Id) && x.IsActive && x.Topic.SubjectId == ex.SubjectId).CountAsync();
                if (valid != qids.Count)
                    throw new BusinessException("Câu hỏi phải hoạt động và thuộc môn của đề.");
                decimal.TryParse(S(v, "pointsPerQuestion"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var pts);
                if (pts <= 0)
                    pts = 1;
                var pointMap = new Dictionary<int, decimal>();
                if (v.TryGetValue("questionPoints", out var qp) && qp.ValueKind == JsonValueKind.Object)
                foreach (var prop in qp.EnumerateObject())
                {
                    if (!int.TryParse(prop.Name, out var qid) || !decimal.TryParse(prop.Value.ToString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var point) || point <= 0 || decimal.Round(point, 2) != point)
                        throw new BusinessException("Điểm câu hỏi phải dương và tối đa hai chữ số thập phân.");
                    pointMap[qid] = point;
                }
                db.ExamQuestions.RemoveRange(ex.Questions);
                ex.Questions = qids.Select((qid, n) => new ExamQuestion { QuestionId = qid, Points = pointMap.GetValueOrDefault(qid, pts), Order = n + 1 }).ToList();
                if (id == null)
                    db.Exams.Add(ex);
                await db.SaveChangesAsync();
                return ex.Id;
            case "sessions":
                Role(role, "Lecturer");
                var s = id == null ? new ExamSession { ManagedBy = uid } : await Session(uid, role, id.Value);
                if (s.Status != "Draft" || s.HasOpened || Now >= s.OpensAt && id != null && s.Status == "Active")
                    throw new BusinessException("Chỉ sửa đợt nháp chưa từng mở.");
                var paper = await db.Exams.FindAsync(I(v, "examId"));
                if (paper?.Status != "Ready")
                    throw new BusinessException("Chọn đề sẵn sàng.");
                await RequireSubject(uid, role, paper.SubjectId);
                if (!await db.Subjects.AnyAsync(x => x.Id == paper.SubjectId && x.IsActive))
                    throw new BusinessException("Môn học đã ngừng dùng.");
                s.ExamId = paper.Id;
                s.Title = Required(v, "title");
                s.OpensAt = T(v, "opensAt");
                s.ClosesAt = T(v, "closesAt");
                s.DurationMinutes = I(v, "durationMinutes");
                if (s.OpensAt >= s.ClosesAt || s.DurationMinutes <= 0)
                    throw new BusinessException("Lịch và thời lượng không hợp lệ.");
                s.ShuffleOptions = B(v, "shuffleOptions", false);
                s.ShuffleQuestions = B(v, "shuffleQuestions", false);
                s.ReviewPolicy = S(v, "reviewPolicy", "ScoreOnly");
                if (!new[] { "ScoreOnly", "Answers", "CorrectAnswers" }.Contains(s.ReviewPolicy))
                    throw new BusinessException("Chính sách không hợp lệ.");
                if (id == null)
                    db.ExamSessions.Add(s);
                await db.SaveChangesAsync();
                return s.Id;
            case "candidates":
                Role(role, "Lecturer");
                if (id != null)
                    throw new BusinessException("Xóa phân công cũ rồi tạo lại.");
                return await Candidate(uid, I(v, "sessionId"), I(v, "studentId"));
            default:
                throw Denied();
        }
    }
    public async Task Revoke(int uid)
    {
        await db.AuthSessions.Where(x => x.UserId == uid && !x.Revoked).ExecuteUpdateAsync(s => s.SetProperty(x => x.Revoked, true));
    }
    async Task ValidateCandidate(ExamSession s, int student)
    {
        var u = await users.FindByIdAsync(student.ToString());
        if (u == null || !u.IsActive || !await users.IsInRoleAsync(u, "Student"))
            throw new BusinessException("Thí sinh không hợp lệ.");
        if (await db.SessionCandidates.AnyAsync(c => c.StudentId == student && c.SessionId != s.Id && c.Session.Status != "Cancelled" && c.Session.OpensAt < s.ClosesAt && s.OpensAt < c.Session.ClosesAt))
            throw new BusinessException("Thí sinh bị trùng lịch.");
    }
    async Task<int> Candidate(int uid, int sid, int student)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var s = await Session(uid, "Lecturer", sid); if (s.Status != "Draft" || s.HasOpened) throw new BusinessException("Danh sách đã khóa."); await ValidateCandidate(s, student); if (await db.SessionCandidates.AnyAsync(x => x.SessionId == sid && x.StudentId == student)) throw new BusinessException("Thí sinh đã được phân công."); var c = new SessionCandidate { SessionId = sid, StudentId = student }; db.Add(c); await db.SaveChangesAsync(); await tx.CommitAsync(); return c.Id; });
    }
    /// <summary>Apply guarded state transitions; active session configuration and published scores cannot be edited.</summary>
    public async Task ActionAsync(int uid, string role, string resource, int id, string action)
    {
        if (resource == "users")
        {
            Role(role,"Administrator");
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
            {
                db.ChangeTracker.Clear();
                await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var u=await users.FindByIdAsync(id.ToString())??throw new BusinessException("Không thấy tài khoản.",404);
                if(action=="approve")
                {
                    if(u.IsActive||u.RequestedRole==null||!new[]{"Lecturer","Administrator"}.Contains(u.RequestedRole))
                        throw new BusinessException("Tài khoản không có yêu cầu đăng ký nhân sự đang chờ duyệt.",409);
                    if(!await users.IsInRoleAsync(u,u.RequestedRole))Identity(await users.AddToRoleAsync(u,u.RequestedRole));
                    u.IsActive=true;
                    u.RequestedRole=null;
                }
                else
                {
                    if(action is not("lock" or "unlock"))throw new BusinessException("Thao tác không hợp lệ.");
                    if(action=="unlock"&&u.RequestedRole!=null)throw new BusinessException("Tài khoản đang chờ duyệt. Hãy chọn Duyệt đăng ký thay vì Mở khóa.",409);
                    if(id==uid&&action=="lock")throw new BusinessException("Không thể tự khóa tài khoản.");
                    u.IsActive=action=="unlock";
                }
                Identity(await users.UpdateAsync(u));
                await Revoke(id);
                await tx.CommitAsync();
            });
            return;
        }
        if (resource == "exams")
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var e = await db.Exams.Include(x => x.Questions).ThenInclude(x => x.Question).ThenInclude(x => x.Options)
                    .Include(x => x.Questions).ThenInclude(x => x.Question).ThenInclude(x => x.Topic)
                    .SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không thấy đề.", 404);
                await RequireSubject(uid, role, e.SubjectId);
                if (action == "ready")
                {
                    if (!await db.Subjects.AnyAsync(x => x.Id == e.SubjectId && x.IsActive))
                        throw new BusinessException("Môn học đã ngừng dùng.");
                    if (e.Questions.Count == 0 || e.Questions.Any(q => !q.Question.IsActive || q.Question.Topic.SubjectId != e.SubjectId || !q.Question.Topic.IsActive || q.Question.Options.Count < 2 || q.Question.Options.Count(o => o.IsCorrect) != 1) || Math.Abs(e.Questions.Sum(x => x.Points) - 10) > 0.001m)
                        throw new BusinessException("Đề phải có câu hỏi hợp lệ, mỗi câu một đáp án đúng, tổng điểm bằng 10.");
                    e.Status = "Ready";
                }
                else if (action == "draft")
                {
                    if (await db.ExamSessions.AnyAsync(s => s.ExamId == id && (s.Status != "Draft" || s.HasOpened)))
                        throw new BusinessException("Chỉ hủy xác nhận khi mọi đợt liên quan còn nháp và chưa từng mở.", 409);
                    e.Status = "Draft";
                }
                else
                    throw new BusinessException("Thao tác không hợp lệ.");
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            });
            return;
        }
        if (resource != "sessions")
            throw Denied();
        Role(role, "Lecturer");
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var s = await Session(uid, role, id); switch (action) { case "activate": if (!await db.Subjects.AnyAsync(x => x.Id == s.Exam.SubjectId && x.IsActive)) throw new BusinessException("Môn học đã ngừng dùng."); if (s.Status != "Draft" || s.HasOpened || s.ClosesAt <= Now || s.OpensAt >= s.ClosesAt || s.DurationMinutes <= 0 || s.Exam.Status != "Ready") throw new BusinessException("Đợt không đủ điều kiện kích hoạt."); var candidates = await db.SessionCandidates.Where(x => x.SessionId == id).ToListAsync(); if (candidates.Count == 0) throw new BusinessException("Chưa phân công thí sinh."); foreach (var c in candidates) await ValidateCandidate(s, c.StudentId); s.Status = "Active"; break; case "postpone": case "cancel": if (s.HasOpened || Now >= s.OpensAt || await db.Attempts.AnyAsync(a => a.SessionId == id)) throw new BusinessException("Chỉ hoãn/hủy trước lần mở đầu."); s.Status = action == "cancel" ? "Cancelled" : "Draft"; break; case "publish": if (s.Status != "Active" || Now < s.ClosesAt || await db.Attempts.AnyAsync(a => a.SessionId == id && (a.Status == "InProgress" || a.Result == null))) throw new BusinessException("Chỉ công bố sau khi đợt đóng và đã chấm hết bài."); s.Published = true;s.PublishedAt??=Now; break; default: throw new BusinessException("Thao tác không hợp lệ."); } await db.SaveChangesAsync(); await tx.CommitAsync(); });
    }
    private async Task<bool> ClassIsReferencedAsync(int id) =>
        await db.Set<ClassStudent>().AnyAsync(x => x.ClassId == id) ||
        await db.Set<SessionClass>().AnyAsync(x => x.ClassId == id);

    public async Task DeleteAsync(int uid,string role,string resource,int id){await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>{db.ChangeTracker.Clear();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);await DeleteInternalAsync(uid,role,resource,id);await tx.CommitAsync();});} private async Task DeleteInternalAsync(int uid, string role, string resource, int id)
    {
        if (!(await CatalogAsync(uid, role)).Resources.Any(x => x.Key == resource && x.CanDelete))
            throw Denied();
        switch (resource)
        {
            case "users":
                Role(role, "Administrator");
                if (uid == id)
                    throw new BusinessException("Không tự xóa tài khoản.");
                var u = await users.FindByIdAsync(id.ToString()) ?? throw new BusinessException("Không thấy tài khoản.", 404);
                if (await db.AuthSessions.AnyAsync(x => x.UserId == id) || await db.Attempts.AnyAsync(x => x.StudentId == id) || await db.SessionCandidates.AnyAsync(x => x.StudentId == id) || await db.Questions.AnyAsync(x => x.CreatedBy == id) || await db.ExamSessions.AnyAsync(x => x.ManagedBy == id) || await db.LecturerSubjects.AnyAsync(x => x.LecturerId == id))
                    throw new BusinessException("Tài khoản đã được tham chiếu; dùng Khóa.");
                var userRoles=await users.GetRolesAsync(u);if(userRoles.Count>0)Identity(await users.RemoveFromRolesAsync(u,userRoles));Identity(await users.DeleteAsync(u));
                return;
            case "classes":
                Role(role, "Administrator");
                var examClass = await db.Set<ExamClass>().FindAsync(id)
                    ?? throw new BusinessException("Không tìm thấy lớp.", 404);
                if (await ClassIsReferencedAsync(id))
                    throw new BusinessException("Lớp đã được sử dụng; hãy chọn ngừng hoạt động.", 409);
                db.Remove(examClass);
                break;
            case "subjects":
                Role(role, "Administrator");
                if (await db.Topics.AnyAsync(x => x.SubjectId == id) || await db.Exams.AnyAsync(x => x.SubjectId == id) || await db.LecturerSubjects.AnyAsync(x => x.SubjectId == id)
                    || await db.Set<ExamClass>().AnyAsync(x => x.SubjectId == id))
                    throw new BusinessException("Môn đã có dữ liệu; chọn ngừng hoạt động.");
                db.Remove(await db.Subjects.FindAsync(id) ?? throw new BusinessException("Không thấy môn.", 404));
                break;
            case "assignments":
                Role(role, "Administrator");
                db.Remove(await db.LecturerSubjects.FindAsync(id) ?? throw new BusinessException("Không thấy phân công.", 404));
                break;
            case "topics":
                var t = await db.Topics.FindAsync(id) ?? throw new BusinessException("Không thấy chủ đề.", 404);
                await RequireSubject(uid, role, t.SubjectId);
                if (await db.Questions.AnyAsync(x => x.TopicId == id))
                    throw new BusinessException("Chủ đề đã có câu hỏi; chọn ngừng hoạt động.");
                db.Remove(t);
                break;
            case "questions":
                var q = await db.Questions.Include(x => x.Topic).Include(x => x.Options).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không thấy câu hỏi.", 404);
                await RequireSubject(uid, role, q.Topic.SubjectId);
                if (await db.ExamQuestions.AnyAsync(x => x.QuestionId == id))
                {
                    q.IsActive = false;
                }
                else
                {
                    db.AnswerOptions.RemoveRange(q.Options);
                    db.Remove(q);
                }
                break;
            case "exams":
                var e = await db.Exams.Include(x => x.Questions).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không thấy đề.", 404);
                await RequireSubject(uid, role, e.SubjectId);
                if (e.Status != "Draft" || await db.ExamSessions.AnyAsync(x => x.ExamId == id))
                    throw new BusinessException("Chỉ xóa đề nháp chưa sử dụng.");
                db.ExamQuestions.RemoveRange(e.Questions);
                db.Remove(e);
                break;
            case "sessions":
                Role(role, "Lecturer");
                var s = await Session(uid, role, id);
                if (s.Status != "Draft" || s.HasOpened || await db.SessionCandidates.AnyAsync(x => x.SessionId == id))
                    throw new BusinessException("Chỉ xóa đợt nháp chưa có phân công.");
                db.Remove(s);
                break;
            case "candidates":
                Role(role, "Lecturer");
                var c = await db.SessionCandidates.FindAsync(id) ?? throw new BusinessException("Không thấy thí sinh.", 404);
                var cs = await Session(uid, role, c.SessionId);
                if (cs.Status != "Draft" || cs.HasOpened)
                    throw new BusinessException("Danh sách đã khóa.");
                db.Remove(c);
                break;
            default:
                throw Denied();
        }
        await db.SaveChangesAsync();
    }
    /// <summary>Validate every CSV row before confirmation and import atomically without overwriting accounts.</summary>
    public async Task<int> ImportStudentsAsync(int uid,string role,string csv,bool preview=false)
    {
        Role(role,"Administrator");
        var rows=CsvStudents.Parse(csv.TrimStart('\uFEFF'));
        string[] header=["UserName","FullName","StudentCode","Email","Password"];
        if(rows.Count<2||!rows[0].SequenceEqual(header,StringComparer.OrdinalIgnoreCase))throw new BusinessException("CSV cần tiêu đề UserName,FullName,StudentCode,Email,Password và ít nhất một dòng dữ liệu.");
        if(rows.Count>501)throw new BusinessException("Mỗi lần nhập tối đa 500 thí sinh.");
        var candidates=new List<(AppUser User,string Password,int Line)>();
        var seenNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var seenCodes=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var errors=new List<string>();
        for(var i=1;i<rows.Count;i++)
        {
            var r=rows[i];if(r.Length!=5){errors.Add($"Dòng {i+1}: cần đúng 5 cột.");continue;}
            var u=new AppUser{UserName=r[0].Trim(),FullName=r[1].Trim(),StudentCode=r[2].Trim(),Email=string.IsNullOrWhiteSpace(r[3])?null:r[3].Trim(),IsActive=true};
            if(string.IsNullOrWhiteSpace(u.UserName)||string.IsNullOrWhiteSpace(u.FullName)||string.IsNullOrWhiteSpace(u.StudentCode)){errors.Add($"Dòng {i+1}: thiếu tên đăng nhập, họ tên hoặc mã thí sinh.");continue;}
            if(u.Email!=null&&!System.Net.Mail.MailAddress.TryCreate(u.Email,out _)){errors.Add($"Dòng {i+1}: email không hợp lệ.");continue;}if(u.StudentCode.Length>30||u.FullName.Length>150){errors.Add($"Dòng {i+1}: mã thí sinh tối đa 30 ký tự, họ tên tối đa 150.");continue;}
            if(!seenNames.Add(u.UserName)||!seenCodes.Add(u.StudentCode)){errors.Add($"Dòng {i+1}: trùng tên đăng nhập hoặc mã trong tệp.");continue;}
            if(await users.FindByNameAsync(u.UserName)!=null||await db.Users.AnyAsync(x=>x.StudentCode==u.StudentCode)){errors.Add($"Dòng {i+1}: tài khoản hoặc mã đã tồn tại; không ghi đè.");continue;}
            foreach(var validator in users.PasswordValidators){var validation=await validator.ValidateAsync(users,u,r[4]);if(!validation.Succeeded)errors.Add($"Dòng {i+1}: "+string.Join("; ",validation.Errors.Select(e=>e.Description)));}
            foreach(var validator in users.UserValidators){var validation=await validator.ValidateAsync(users,u);if(!validation.Succeeded)errors.Add($"Dòng {i+1}: "+string.Join("; ",validation.Errors.Select(e=>e.Description)));}
            candidates.Add((u,r[4],i+1));
        }
        if(errors.Count>0)throw new BusinessException(string.Join("\n",errors));
        if(preview)return candidates.Count;
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
        {
            db.ChangeTracker.Clear();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            foreach(var candidate in candidates)
            {
                candidate.User.Id=0;
                var result=await users.CreateAsync(candidate.User,candidate.Password);
                if(!result.Succeeded)throw new BusinessException($"Dòng {candidate.Line}: "+string.Join("; ",result.Errors.Select(e=>e.Description)));
                Identity(await users.AddToRoleAsync(candidate.User,"Student"));
            }
            await tx.CommitAsync();return candidates.Count;
        });
    }}

















