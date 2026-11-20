using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Exam.Services;

public static class DemoSeeder
{
    public static async Task SeedAsync(IServiceProvider services, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Seed:Password is required.");
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        foreach (var role in new[] { "Student", "Lecturer", "Administrator" })
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new AppRole { Name = role });
        var names = new[] { ("admin", "Administrator", "Quản trị viên"), ("lecturer", "Lecturer", "Giảng viên 1"), ("lecturer2", "Lecturer", "Giảng viên 2"), ("student", "Student", "Thí sinh 1"), ("student2", "Student", "Thí sinh 2") };
        foreach (var (name, role, full) in names)
        {
            if (await users.FindByNameAsync(name) != null)
                continue;
            var u = new AppUser { UserName = name, FullName = full, StudentCode = role == "Student" ? name.ToUpperInvariant() : null };
            var r = await users.CreateAsync(u, password);
            if (!r.Succeeded)
                throw new InvalidOperationException(string.Join(";", r.Errors.Select(x => x.Description)));
            await users.AddToRoleAsync(u, role);
        }
        if (await db.Subjects.AnyAsync())
        {
            await EnsureSessionSnapshotsAsync(db);
            await EnsureActivationDraftAsync(db);
            return;
        }
        var lecturer = (await users.FindByNameAsync("lecturer"))!;
        var lecturer2 = (await users.FindByNameAsync("lecturer2"))!;
        var subject = new Subject { Code = "CSHARP", Name = "Lập trình C#" };
        var other = new Subject { Code = "DATABASE", Name = "Cơ sở dữ liệu" };
        db.Subjects.AddRange(subject, other);
        await db.SaveChangesAsync();
        db.LecturerSubjects.AddRange(new LecturerSubject { LecturerId = lecturer.Id, SubjectId = subject.Id }, new LecturerSubject { LecturerId = lecturer2.Id, SubjectId = other.Id });
        var topic = new Topic { SubjectId = subject.Id, Name = "C# cơ bản" };
        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        var qs = new List<Question>();
        (string Text, string[] Options, int Correct)[] samples =
        [
            ("Từ khóa nào dùng để khai báo một lớp trong C#?", ["class", "table", "function", "module"], 0),
            ("Kiểu dữ liệu nào lưu giá trị true hoặc false?", ["int", "string", "bool", "double"], 2),
            ("Phương thức nào thường được dùng để ghi một dòng ra cửa sổ console?", ["Console.ReadLine", "Console.WriteLine", "File.ReadAllText", "Math.Round"], 1),
            ("Từ khóa nào ngăn một lớp bị kế thừa?", ["virtual", "abstract", "static", "sealed"], 3),
            ("Cấu trúc nào dùng để giải phóng tài nguyên IDisposable khi kết thúc phạm vi?", ["using", "switch", "lock", "foreach"], 0)
        ];
        foreach (var sample in samples)
        {
            qs.Add(new Question
            {
                TopicId = topic.Id,
                CreatedBy = lecturer.Id,
                Text = sample.Text,
                Options = sample.Options.Select((text, index) => new AnswerOption
                {
                    Text = text,
                    IsCorrect = index == sample.Correct,
                    Order = index + 1
                }).ToList()
            });
        }
        db.Questions.AddRange(qs);
        await db.SaveChangesAsync();
        var paper = new ExamPaper { SubjectId = subject.Id, CreatedBy = lecturer.Id, Title = "C# — đề mẫu", Status = "Ready", Questions = qs.Select((q, i) => new ExamQuestion { QuestionId = q.Id, Points = 2, Order = i + 1 }).ToList() };
        db.Exams.Add(paper);
        await db.SaveChangesAsync();
        var session = new ExamSession { ExamId = paper.Id, ManagedBy = lecturer.Id, Title = "Thi thử C#", OpensAt = DateTime.UtcNow.AddMinutes(-10), ClosesAt = DateTime.UtcNow.AddDays(7), DurationMinutes = 30, Status = "Active", ShuffleOptions = true, ShuffleQuestions = true, ReviewPolicy = "CorrectAnswers" };
        var demoClass = new ExamClass { SubjectId = subject.Id, Code = "DEMO-TS01" };
        foreach (var name in new[] { "student", "student2" })
            demoClass.Students.Add(new() { StudentId = (await users.FindByNameAsync(name))!.Id });
        session.AccessCode = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8));
        session.Classes.Add(new() { Class = demoClass });
        session.ActivatedAt = DateTime.UtcNow;
        session.Candidates.AddRange(demoClass.Students.Select(x => new SessionCandidate { StudentId = x.StudentId }));
        session.Questions.AddRange(paper.Questions.OrderBy(x => x.Order).Select(x => new SessionQuestion
        {
            SourceQuestionId = x.QuestionId,
            Text = x.Question.Text,
            Order = x.Order,
            Points = x.Points,
            Options = x.Question.Options.OrderBy(o => o.Order).Select(o => new SessionOption
            {
                Text = o.Text,
                Order = o.Order,
                IsCorrect = o.IsCorrect
            }).ToList()
        }));
        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();
        await EnsureActivationDraftAsync(db, paper.Id, demoClass.Id);
        Console.WriteLine($"Mã vào thi demo: {session.AccessCode}");
    }

    private static async Task EnsureSessionSnapshotsAsync(ExamDbContext db)
    {
        var session = await db.ExamSessions
            .Include(s => s.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Question).ThenInclude(q => q.Options)
            .Include(s => s.Questions)
            .Include(s => s.Candidates)
            .Include(s => s.Classes).ThenInclude(c => c.Class).ThenInclude(c => c.Students)
            .SingleOrDefaultAsync(s => s.Title == "Thi thử C#" && s.Status == "Active");
        if (session is null)
            return;

        var questionIds = session.Questions.Select(q => q.SourceQuestionId).ToHashSet();
        foreach (var source in session.Exam.Questions.OrderBy(q => q.Order).Where(q => !questionIds.Contains(q.QuestionId)))
            session.Questions.Add(new SessionQuestion
            {
                SourceQuestionId = source.QuestionId,
                Text = source.Question.Text,
                Order = source.Order,
                Points = source.Points,
                Options = source.Question.Options.OrderBy(o => o.Order).Select(o => new SessionOption
                {
                    Text = o.Text,
                    Order = o.Order,
                    IsCorrect = o.IsCorrect
                }).ToList()
            });

        var candidateIds = session.Candidates.Select(c => c.StudentId).ToHashSet();
        foreach (var studentId in session.Classes.SelectMany(c => c.Class.Students).Select(c => c.StudentId).Distinct().Where(id => !candidateIds.Contains(id)))
            session.Candidates.Add(new SessionCandidate { StudentId = studentId });
        session.ActivatedAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task EnsureActivationDraftAsync(ExamDbContext db, int? paperId = null, int? classId = null)
    {
        if (await db.ExamSessions.AnyAsync(s => s.Title == "Kiểm thử kích hoạt chụp đề"))
            return;
        var paper = paperId is int pId
            ? await db.Exams.SingleOrDefaultAsync(p => p.Id == pId)
            : await db.Exams.Where(p => p.Status == "Ready" && p.Subject.Code == "CSHARP").OrderByDescending(p => p.Id).FirstOrDefaultAsync();
        var demoClass = classId is int cId
            ? await db.Set<ExamClass>().SingleOrDefaultAsync(c => c.Id == cId)
            : await db.Set<ExamClass>().SingleOrDefaultAsync(c => c.Code == "DEMO-TS01");
        if (paper is null || demoClass is null || !demoClass.IsActive)
            return;

        var studentIds = await db.Users.Where(u => u.UserName == "student" || u.UserName == "student2")
            .Select(u => u.Id).ToListAsync();
        var existingIds = await db.Set<ClassStudent>().Where(m => m.ClassId == demoClass.Id && studentIds.Contains(m.StudentId))
            .Select(m => m.StudentId).ToListAsync();
        db.Set<ClassStudent>().AddRange(studentIds.Except(existingIds).Select(id => new ClassStudent { ClassId = demoClass.Id, StudentId = id }));

        var latestClose = await db.ExamSessions.Where(s => s.Status == "Active" && s.Classes.Any(c => c.ClassId == demoClass.Id))
            .MaxAsync(s => (DateTime?)s.ClosesAt);
        var opensAt = DateTime.UtcNow.AddDays(8);
        if (latestClose is DateTime activeClose && activeClose.AddDays(1) > opensAt)
            opensAt = activeClose.AddDays(1);
        db.ExamSessions.Add(new ExamSession
        {
            ExamId = paper.Id,
            ManagedBy = paper.CreatedBy,
            Title = "Kiểm thử kích hoạt chụp đề",
            OpensAt = opensAt,
            ClosesAt = opensAt.AddDays(1),
            DurationMinutes = 30,
            Status = "Draft",
            ShuffleQuestions = true,
            ShuffleOptions = true,
            ReviewPolicy = "CorrectAnswers",
            Classes = [new SessionClass { ClassId = demoClass.Id }]
        });
        await db.SaveChangesAsync();
    }
}

