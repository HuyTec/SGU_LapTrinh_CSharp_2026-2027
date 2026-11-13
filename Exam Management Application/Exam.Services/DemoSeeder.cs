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
        foreach (var role in new[] { "Student", "Lecturer", "ExamOfficer", "Administrator" })
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new AppRole { Name = role });
        var names = new[] { ("admin", "Administrator", "Quản trị viên"), ("lecturer", "Lecturer", "Giảng viên 1"), ("lecturer2", "Lecturer", "Giảng viên 2"), ("officer", "ExamOfficer", "Cán bộ 1"), ("officer2", "ExamOfficer", "Cán bộ 2"), ("student", "Student", "Thí sinh 1"), ("student2", "Student", "Thí sinh 2") };
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
            return;
        var lecturer = (await users.FindByNameAsync("lecturer"))!;
        var lecturer2 = (await users.FindByNameAsync("lecturer2"))!;
        var officer = (await users.FindByNameAsync("officer"))!;
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
        var session = new ExamSession { ExamId = paper.Id, ManagedBy = officer.Id, Title = "Thi thử C#", OpensAt = DateTime.UtcNow.AddMinutes(-10), ClosesAt = DateTime.UtcNow.AddDays(7), DurationMinutes = 30, Status = "Active", ShuffleOptions = true, ShuffleQuestions = true, ReviewPolicy = "CorrectAnswers" };
        var demoClass = new ExamClass { SubjectId = subject.Id, Code = "DEMO-TS01" };
        foreach (var name in new[] { "student", "student2" })
            demoClass.Students.Add(new() { StudentId = (await users.FindByNameAsync(name))!.Id });
        session.AccessCode = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8));
        session.Classes.Add(new() { Class = demoClass });
        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();
        Console.WriteLine($"Mã vào thi demo: {session.AccessCode}");
    }
}

