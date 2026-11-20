using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Exam.Tests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EXAM_TEST_CONNECTION")))
            Skip = "Set EXAM_TEST_CONNECTION to run against an isolated SQL Server test database.";
    }
}

/// <summary>Each test owns a new database. No application or existing database is reset.</summary>
public partial class SqlExamTests : IAsyncLifetime
{
    private readonly string databaseName = "ExamAppTests_" + Guid.NewGuid().ToString("N");
    private DbContextOptions<ExamDbContext> options = null!;
    private readonly TestClock clock = new(new DateTimeOffset(2026, 10, 24, 7, 0, 0, TimeSpan.Zero));
    private int sessionId;
    private int studentId;
    private int otherStudentId;

    public async Task InitializeAsync()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EXAM_TEST_CONNECTION"));
        connection.InitialCatalog = databaseName;
        options = new DbContextOptionsBuilder<ExamDbContext>().UseSqlServer(connection.ConnectionString,
            sql => sql.EnableRetryOnFailure()).Options;
        await using var db = NewContext();
        await db.Database.MigrateAsync();
        var student = new AppUser { UserName = "student", FullName = "Thí sinh A", StudentCode = "TEST001" };
        var other = new AppUser { UserName = "other", FullName = "Thí sinh B", StudentCode = "TEST002" };
        var teacher = new AppUser { UserName = "teacher", FullName = "Giảng viên" };
        var officer = new AppUser { UserName = "officer", FullName = "Cán bộ" };
        db.Users.AddRange(student, other, teacher, officer);
        var subject = new Subject { Code = "TEST", Name = "Môn kiểm thử" };
        var topic = new Topic { Subject = subject, Name = "Chủ đề" };
        var questions = Enumerable.Range(1, 2).Select(i => new Question
        {
            Topic = topic, Creator = teacher, Text = $"Câu {i}", Difficulty = "Medium",
            Options = [new() { Text = "Đúng", IsCorrect = true, Order = 1 }, new() { Text = "Sai", Order = 2 }]
        }).ToList();
        var exam = new ExamPaper
        {
            Subject = subject, Creator = teacher, Title = "Đề kiểm thử", Status = "Ready",
            Questions = questions.Select((q, i) => new ExamQuestion { Question = q, Points = 5m, Order = i + 1 }).ToList()
        };
        var session = new ExamSession
        {
            Exam = exam, Manager = officer, Title = "Đợt kiểm thử", Status = "Active",
            OpensAt = clock.GetUtcNow().UtcDateTime.AddMinutes(-10),
            ClosesAt = clock.GetUtcNow().UtcDateTime.AddMinutes(30), DurationMinutes = 20,
            ShuffleQuestions = true, ShuffleOptions = true
        };
        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();
        session.Classes.Add(new()
        {
            Class = new ExamClass
            {
                SubjectId = subject.Id,
                Code = "TEST-CLASS",
                Students = [new() { StudentId = student.Id }]
            }
        });
        await db.SaveChangesAsync();
        sessionId = session.Id;
        studentId = student.Id;
        otherStudentId = other.Id;
    }

    [SqlFact]
    public async Task LecturerPublishesAndExportsOnlyOwnedSession()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ExamDbContext>(o => o.UseSqlServer(
            options.Extensions.OfType<RelationalOptionsExtension>().Single().ConnectionString!,
            sql => sql.EnableRetryOnFailure()));
        services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<ExamDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        var session = await db.ExamSessions.Include(x => x.Exam).SingleAsync();
        db.LecturerSubjects.Add(new LecturerSubject { SubjectId = session.Exam.SubjectId, LecturerId = session.ManagedBy });
        await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<AppRole>>();
        var studentRole = new AppRole { Name = "Student" };
        Assert.True((await roles.CreateAsync(studentRole)).Succeeded);
        db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<int> { UserId = studentId, RoleId = studentRole.Id });
        await db.SaveChangesAsync();
        var manager = session.ManagedBy;
        var admin = new AdminService(db, users, clock);
        var exporter = new ExportService(admin);

        Assert.Single(await admin.SessionMonitorAsync(manager, "Lecturer", sessionId));
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(
            () => exporter.ExportAsync(otherStudentId, "Lecturer", sessionId, false))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(
            () => admin.ActionAsync(otherStudentId, "Lecturer", "sessions", sessionId, "publish"))).Status);
        var attemptId = await Service(db).StartAsync(studentId, sessionId);
        await Service(db).SubmitAsync(studentId, attemptId);
        Assert.False((await Service(db).ResultAsync(studentId, attemptId)).Published);

        await admin.ActionAsync(manager, "Lecturer", "sessions", sessionId, "publish");
        var result = await Service(db).ResultAsync(studentId, attemptId);
        Assert.True(result.Published);
        Assert.Equal(0m, result.Score);
        await db.Set<ClassStudent>().Where(m => m.StudentId == studentId).ExecuteDeleteAsync();
        var csv = System.Text.Encoding.UTF8.GetString(await exporter.ExportAsync(manager, "Lecturer", sessionId, false));
        Assert.Contains("TEST001", csv);
        Assert.DoesNotContain("TEST002", csv);
        using var stream = new MemoryStream(await exporter.ExportAsync(manager, "Lecturer", sessionId, true));
        using var zip = new System.IO.Compression.ZipArchive(stream);
        using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        Assert.Contains("TEST001", await reader.ReadToEndAsync());
    }

    [SqlFact]
    public async Task LecturerActivatesDraftForAssignedClassAndStudentCanJoin()
    {
        await using var db = NewContext();
        var lecturer = await db.Users.Where(u => u.UserName == "teacher").Select(u => u.Id).SingleAsync();
        var paper = await db.Exams.SingleAsync();
        var classId = await db.Set<ExamClass>().Select(c => c.Id).SingleAsync();
        db.LecturerSubjects.Add(new LecturerSubject { LecturerId = lecturer, SubjectId = paper.SubjectId });
        var studentRole = new AppRole { Name = "Student", NormalizedName = "STUDENT" };
        db.Roles.Add(studentRole);
        await db.SaveChangesAsync();
        db.UserRoles.AddRange(new() { RoleId = studentRole.Id, UserId = studentId },
            new() { RoleId = studentRole.Id, UserId = otherStudentId });
        await db.SaveChangesAsync();

        var service = new LecturerSessionService(db, clock);
        var now = clock.GetUtcNow().UtcDateTime;
        var draft = new SessionDraft("Thi theo lớp", paper.Id, now.AddMinutes(1), now.AddMinutes(31), 20,
            true, false, "ScoreOnly", [classId]);
        var id = await service.SaveAsync(lecturer, null, draft);
        var hidden = await Assert.ThrowsAsync<BusinessException>(() => service.EditorAsync(otherStudentId, id));
        Assert.Equal(404, hidden.Status);

        await service.SaveAsync(lecturer, id, draft with { OpensAt = now.AddMinutes(2), ClosesAt = now.AddMinutes(32) });
        db.SessionCandidates.Add(new SessionCandidate { SessionId = id, StudentId = otherStudentId });
        await db.SaveChangesAsync();
        await service.ChangeStateAsync(lecturer, id, "activate");
        var activated = (await service.ListAsync(lecturer)).Single();
        Assert.False(string.IsNullOrEmpty(activated.Code));
        Assert.False(activated.CanEdit);
        Assert.True(activated.CanWithdraw);
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveAsync(lecturer, id, draft));
        Assert.Equal(studentId, (await db.SessionCandidates.SingleAsync(c => c.SessionId == id)).StudentId);
        await db.Set<ClassStudent>().Where(m => m.ClassId == classId && m.StudentId == studentId).ExecuteDeleteAsync();
        db.Set<ClassStudent>().Add(new() { ClassId = classId, StudentId = otherStudentId });
        await db.SaveChangesAsync();

        var student = new ExamService(db, clock);
        Assert.Equal(id, await student.FindSessionByCodeAsync(studentId, activated.Code));
        await Assert.ThrowsAsync<BusinessException>(() => student.FindSessionByCodeAsync(otherStudentId, activated.Code));
        clock.Set(clock.GetUtcNow().AddMinutes(2));
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() => student.StartAsync(otherStudentId, id))).Status);
        Assert.True(await student.StartAsync(studentId, id) > 0);
    }

    [SqlFact]
    public async Task PaperTotalCannotExceedTenWhenSavedOrMarkedReady()
    {
        await using var db = NewContext();
        var teacher = await db.Users.SingleAsync(u => u.UserName == "teacher");
        var paper = await db.Exams.Include(e => e.Questions).SingleAsync();
        db.LecturerSubjects.Add(new() { LecturerId = teacher.Id, SubjectId = paper.SubjectId });
        await db.SaveChangesAsync();
        var questionIds = paper.Questions.Select(q => q.QuestionId).ToArray();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<ExamDbContext>();
        await using var provider = services.BuildServiceProvider();
        var users = provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        var admin = new AdminService(db, users, clock);
        AdminSaveRequest Request(decimal points) => new(new()
        {
            ["subjectId"] = JsonSerializer.SerializeToElement(paper.SubjectId),
            ["title"] = JsonSerializer.SerializeToElement("Score limit"),
            ["questionIds"] = JsonSerializer.SerializeToElement(questionIds),
            ["questionPoints"] = JsonSerializer.SerializeToElement(questionIds.ToDictionary(id => id, _ => points))
        });
        var error = await Assert.ThrowsAsync<BusinessException>(() =>
            admin.SaveAsync(teacher.Id, "Lecturer", "exams", null, Request(5.01m)));
        Assert.Contains("10", error.Message);
        var id = await admin.SaveAsync(teacher.Id, "Lecturer", "exams", null, Request(5m));
        await admin.ActionAsync(teacher.Id, "Lecturer", "exams", id, "ready");
        Assert.Equal("Ready", (await db.Exams.SingleAsync(e => e.Id == id)).Status);
        await admin.ActionAsync(teacher.Id, "Lecturer", "exams", id, "draft");
        await db.ExamQuestions.Where(q => q.ExamId == id).ExecuteUpdateAsync(q => q.SetProperty(x => x.Points, 6m));
        var readyError = await Assert.ThrowsAsync<BusinessException>(() =>
            admin.ActionAsync(teacher.Id, "Lecturer", "exams", id, "ready"));
        Assert.Contains("10", readyError.Message);
    }

    public async Task DisposeAsync()
    {
        // Drop only the database created by this test, with the verified private prefix.
        if (options is null || !databaseName.StartsWith("ExamAppTests_", StringComparison.Ordinal)) return;
        await using var db = NewContext();
        Assert.Equal(databaseName, db.Database.GetDbConnection().Database);
        await db.Database.EnsureDeletedAsync();
    }

    private ExamDbContext NewContext() => new(options);
    private ExamService Service(ExamDbContext db) => new(db, clock);

    [SqlFact]
    public async Task DemoClassMembershipIsPrivateAndJoiningTwiceIsIdempotent()
    {
        await using var db = NewContext();
        var subject = new Subject { Code = "DEMO-NV", Name = "Môn demo" };
        db.Add(subject);
        await db.SaveChangesAsync();
        db.Add(new ExamClass { Code = StudentClassService.DemoCode, SubjectId = subject.Id });
        await db.SaveChangesAsync();
        var service = new StudentClassService(db);
        var demoStudent = new AppUser { UserName = "demo-only", FullName = "Demo", StudentCode = "DEMO-ONLY" };
        db.Users.Add(demoStudent);
        await db.SaveChangesAsync();
        var demoStudentId = demoStudent.Id;

        Assert.Empty(await service.ListAsync(demoStudentId));
        Assert.True(await service.JoinDemoAsync(demoStudentId));
        Assert.True(await service.JoinDemoAsync(demoStudentId));
        Assert.Single(await service.ListAsync(demoStudentId));
        Assert.Empty(await service.ListAsync(otherStudentId));
        Assert.Single(await db.Set<ClassStudent>().Where(s => s.StudentId == demoStudentId).ToListAsync());

        var demoClass = await db.Set<ExamClass>().SingleAsync(c => c.Code == StudentClassService.DemoCode);
        demoClass.IsActive = false;
        await db.SaveChangesAsync();
        Assert.False(await service.JoinDemoAsync(otherStudentId));

        demoClass = await db.Set<ExamClass>().SingleAsync(c => c.Code == StudentClassService.DemoCode);
        demoClass.IsActive = true;
        var demoSubject = await db.Subjects.SingleAsync(s => s.Id == demoClass.SubjectId);
        demoSubject.IsActive = false;
        await db.SaveChangesAsync();
        Assert.False(await service.JoinDemoAsync(otherStudentId));
        Assert.Empty(await service.ListAsync(otherStudentId));
    }

    [SqlFact]
    public async Task CodeLookupRequiresClassAndDoesNotCreateAttempt()
    {
        await using var db = NewContext();
        var session = await db.ExamSessions.Include(x => x.Exam).Include(x => x.Classes).SingleAsync();
        session.AccessCode = "CLASS-TEST";
        await db.SaveChangesAsync();

        var secondClass = new ExamClass
        {
            SubjectId = session.Exam.SubjectId,
            Code = "TS01-OTHER",
            Students = [new() { StudentId = studentId }]
        };
        db.Add(secondClass);
        await db.SaveChangesAsync();
        var classIds = await db.Set<ClassStudent>().Where(x => x.StudentId == studentId)
            .Select(x => x.ClassId).ToListAsync();
        Assert.Equal(2, classIds.Count);
        Assert.Contains(session.Classes.Single().ClassId, classIds);
        Assert.Contains(secondClass.Id, classIds);

        var service = Service(db);
        Assert.Equal(sessionId, await service.FindSessionByCodeAsync(studentId, " class-test "));
        await Assert.ThrowsAsync<BusinessException>(() => service.FindSessionByCodeAsync(otherStudentId, "CLASS-TEST"));
        Assert.Empty(await db.Attempts.ToListAsync());

        var attempt = await service.StartAsync(studentId, sessionId);
        var membership = await db.Set<ClassStudent>().SingleAsync(x => x.ClassId != secondClass.Id);
        db.Remove(membership);
        await db.SaveChangesAsync();
        Assert.Equal(attempt, await service.StartAsync(studentId, sessionId));
        Assert.Equal(1, await db.Attempts.CountAsync());
    }

    [SqlFact]
    public async Task UpgradingLegacySessionPreservesAttemptsWithoutGrantingNewAccessFromRoster()
    {
        const string previousMigration = "20261024110741_StaffRegistrationApproval";
        long inProgressId;
        long submittedId;
        int rosterOnlyId;
        var startedAt = clock.GetUtcNow().UtcDateTime.AddMinutes(-5);
        var deadline = startedAt.AddMinutes(20);
        var submittedAt = startedAt.AddMinutes(2);

        await using (var legacy = NewContext())
        {
            await legacy.GetService<IMigrator>().MigrateAsync(previousMigration);
            var rosterOnly = new AppUser { UserName = "roster-only", FullName = "Thí sinh chưa bắt đầu" };
            legacy.Users.Add(rosterOnly);
            await legacy.SaveChangesAsync();
            rosterOnlyId = rosterOnly.Id;
            legacy.SessionCandidates.AddRange(
                new SessionCandidate { SessionId = sessionId, StudentId = studentId },
                new SessionCandidate { SessionId = sessionId, StudentId = otherStudentId },
                new SessionCandidate { SessionId = sessionId, StudentId = rosterOnlyId });
            // The old database requires roster rows before inserting attempts.
            await legacy.SaveChangesAsync();

            var questionId = await legacy.Questions.OrderBy(q => q.Id).Select(q => q.Id).FirstAsync();
            var inProgress = new Attempt
            {
                SessionId = sessionId, StudentId = studentId, StartedAt = startedAt, Deadline = deadline,
                Questions = [new() { SourceQuestionId = questionId, Text = "Câu hỏi đã giao", Order = 1, Points = 10m,
                    Options = [new() { Text = "Đáp án đã lưu", Order = 1, IsCorrect = true }] }]
            };
            var submitted = new Attempt
            {
                SessionId = sessionId, StudentId = otherStudentId, StartedAt = startedAt, Deadline = deadline,
                Status = "Submitted", SubmittedAt = submittedAt, EndReason = "Manual",
                Result = new() { Score = 10m, CorrectCount = 1, GradedAt = submittedAt }
            };
            legacy.Attempts.AddRange(inProgress, submitted);
            await legacy.SaveChangesAsync();
            var snapshot = inProgress.Questions.Single();
            snapshot.Response = new() { SelectedOptionId = snapshot.Options.Single().Id, IsFlagged = true, SavedAt = startedAt };
            await legacy.SaveChangesAsync();
            inProgressId = inProgress.Id;
            submittedId = submitted.Id;
        }

        await using var upgraded = NewContext();
        await upgraded.Database.MigrateAsync();
        var service = Service(upgraded);
        var session = await upgraded.ExamSessions.Include(s => s.Classes).SingleAsync();
        Assert.Null(session.AccessCode);
        Assert.Null(session.ActivatedAt);
        Assert.Empty(session.Classes);
        Assert.Empty(await upgraded.Set<ClassStudent>().ToListAsync());
        Assert.Equal(3, await upgraded.SessionCandidates.CountAsync());
        Assert.Equal(2, await upgraded.Attempts.CountAsync());

        Assert.Equal(inProgressId, await service.StartAsync(studentId, sessionId));
        var resumed = await service.TakeAsync(studentId, inProgressId);
        Assert.Equal("InProgress", resumed.Status);
        Assert.Equal(deadline, resumed.Deadline);
        var resumedQuestion = Assert.Single(resumed.Questions);
        Assert.Equal("Câu hỏi đã giao", resumedQuestion.Text);
        Assert.True(resumedQuestion.IsFlagged);
        Assert.Equal(Assert.Single(resumedQuestion.Options).Id, resumedQuestion.SelectedOptionId);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(
            () => service.TakeAsync(otherStudentId, inProgressId))).Status);

        var storedResult = await upgraded.ExamResults.AsNoTracking().SingleAsync();
        Assert.Equal(submittedId, storedResult.AttemptId);
        Assert.Equal(10m, storedResult.Score);
        Assert.Equal(submittedAt, storedResult.GradedAt);
        var history = Assert.Single((await service.HistoryAsync(otherStudentId)).Results);
        Assert.Equal(submittedId, history.AttemptId);
        Assert.Equal("Submitted", history.Status);
        Assert.Equal(submittedAt, history.SubmittedAt);
        Assert.Null(history.Score);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(
            () => service.ResultAsync(studentId, submittedId))).Status);

        Assert.Empty((await service.SessionsAsync(rosterOnlyId)).Sessions);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(
            () => service.DetailAsync(rosterOnlyId, sessionId))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(
            () => service.StartAsync(rosterOnlyId, sessionId))).Status);
        Assert.Equal(2, await upgraded.Attempts.CountAsync());
    }

    [SqlFact]
    public async Task DatabaseRejectsDuplicateSessionClassAndAccessCode()
    {
        int classId;
        await using (var setup = NewContext())
        {
            classId = await setup.Set<SessionClass>().Select(x => x.ClassId).SingleAsync();
            var session = await setup.ExamSessions.SingleAsync();
            session.AccessCode = "UNIQUE-CODE";
            await setup.SaveChangesAsync();
        }

        await using (var duplicateClass = NewContext())
        {
            duplicateClass.Add(new SessionClass { SessionId = sessionId, ClassId = classId });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateClass.SaveChangesAsync());
        }

        await using var duplicateCode = NewContext();
        var original = await duplicateCode.ExamSessions.SingleAsync();
        duplicateCode.ExamSessions.Add(new()
        {
            ExamId = original.ExamId, ManagedBy = original.ManagedBy, Title = "Đợt thứ hai",
            AccessCode = original.AccessCode, OpensAt = original.OpensAt, ClosesAt = original.ClosesAt,
            DurationMinutes = original.DurationMinutes
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateCode.SaveChangesAsync());
    }

    private async Task<long> Start()
    {
        await using var db = NewContext();
        return await Service(db).StartAsync(studentId, sessionId);
    }

    [SqlFact]
    public async Task ConcurrentStartCreatesOneAttemptAndReturnsTheSameId()
    {
        async Task<long> StartSeparately()
        {
            await using var db = NewContext();
            return await Service(db).StartAsync(studentId, sessionId);
        }
        var ids = await Task.WhenAll(StartSeparately(), StartSeparately());
        Assert.Equal(ids[0], ids[1]);
        await using var verify = NewContext();
        Assert.Equal(1, await verify.Attempts.CountAsync());
    }

    [SqlFact]
    public async Task RepeatedStartKeepsTheOriginalAttemptAndDeadline()
    {
        var id = await Start();
        await using var before = NewContext();
        var original = await before.Attempts.AsNoTracking().SingleAsync();

        clock.Set(clock.GetUtcNow().AddMinutes(5));
        Assert.Equal(id, await Start());

        clock.Set(clock.GetUtcNow().AddMinutes(26));

        Assert.Equal(id, await Start());
        await using var after = NewContext();
        var resumed = await after.Attempts.AsNoTracking().SingleAsync();
        Assert.Equal(original.StartedAt, resumed.StartedAt);
        Assert.Equal(original.Deadline, resumed.Deadline);
    }

    [SqlFact]
    public async Task ConcurrentSubmitCreatesExactlyOneResult()
    {
        var id = await Start();
        async Task SubmitSeparately()
        {
            await using var db = NewContext();
            await Service(db).SubmitAsync(studentId, id);
        }
        await Task.WhenAll(SubmitSeparately(), SubmitSeparately());
        await using var verify = NewContext();
        Assert.Equal(1, await verify.ExamResults.CountAsync(x => x.AttemptId == id));
        Assert.Equal("Submitted", (await verify.Attempts.SingleAsync()).Status);
        Assert.Equal(0m, (await verify.ExamResults.SingleAsync()).Score);
    }

    [SqlFact]
    public async Task SavingAtTheExactDeadlineIsRejected()
    {
        var id = await Start();
        await using var read = NewContext();
        var attempt = await Service(read).TakeAsync(studentId, id);
        clock.Set(new DateTimeOffset(DateTime.SpecifyKind(attempt.Deadline, DateTimeKind.Utc)));
        await using var write = NewContext();
        var q = attempt.Questions[0];
        var error = await Assert.ThrowsAsync<BusinessException>(() => Service(write).SaveAsync(studentId, id,
            new(q.Id, q.Options[0].Id, false)));
        Assert.Equal(409, error.Status);
        await using var verify = NewContext();
        Assert.All(await verify.Responses.ToListAsync(), r => Assert.Null(r.SelectedOptionId));
    }

    [SqlFact]
    public async Task AnotherStudentCannotOpenOrChangeTheAttempt()
    {
        var id = await Start();
        await using var db = NewContext();
        var service = Service(db);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() => service.TakeAsync(otherStudentId, id))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() => service.SubmitAsync(otherStudentId, id))).Status);
    }

    [SqlFact]
    public async Task AnotherStudentCannotTriggerGradingOfAnExpiredAttempt()
    {
        var id = await Start();
        clock.Set(clock.GetUtcNow().AddMinutes(21));
        await using (var request = NewContext())
        {
            var error = await Assert.ThrowsAsync<BusinessException>(() => Service(request).StatusAsync(otherStudentId, id));
            Assert.Equal(403, error.Status);
        }

        await using var verify = NewContext();
        Assert.Equal("InProgress", (await verify.Attempts.SingleAsync(x => x.Id == id)).Status);
        Assert.Empty(await verify.ExamResults.ToListAsync());
    }

    [SqlFact]
    public async Task AttemptPayloadNeverContainsCorrectAnswerFields()
    {
        var id = await Start();
        await using var db = NewContext();
        var payload = JsonSerializer.Serialize(await Service(db).TakeAsync(studentId, id));
        Assert.DoesNotContain("IsCorrect", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CorrectAnswer", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", payload, StringComparison.OrdinalIgnoreCase);
    }

    [SqlFact]
    public async Task AnOptionFromAnotherQuestionIsRejected()
    {
        var id = await Start();
        await using var db = NewContext();
        var service = Service(db);
        var attempt = await service.TakeAsync(studentId, id);
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveAsync(studentId, id,
            new(attempt.Questions[0].Id, attempt.Questions[1].Options[0].Id, false)));
    }

    [SqlFact]
    public async Task SnapshotAndGradingSurviveSourceChangesAndUnpublishedResultsStayHidden()
    {
        var id = await Start();
        await using (var bank = NewContext())
        {
            var questions = await bank.Questions.Include(x => x.Options).ToListAsync();
            foreach (var question in questions) question.Text = "Nội dung ngân hàng đã thay đổi";
            await bank.SaveChangesAsync();
        }
        await using (var answer = NewContext())
        {
            var questions = await answer.AttemptQuestions.Include(x => x.Options).Where(x => x.AttemptId == id).ToListAsync();
            foreach (var q in questions)
                await Service(answer).SaveAsync(studentId, id, new(q.Id, q.Options.Single(o => o.IsCorrect).Id, true));
            var restored = await Service(answer).TakeAsync(studentId, id);
            Assert.All(restored.Questions, q => Assert.StartsWith("Câu ", q.Text));
            Assert.All(restored.Questions, q => Assert.True(q.IsFlagged));
            await Service(answer).SubmitAsync(studentId, id);
        }
        await using (var verify = NewContext())
        {
            var hidden = await Service(verify).ResultAsync(studentId, id);
            Assert.False(hidden.Published);
            Assert.Null(hidden.Score);
            Assert.Empty(hidden.Questions);
            Assert.Equal(10m, (await verify.ExamResults.SingleAsync()).Score);
        }
        await using (var publish = NewContext())
        {
            var s = await publish.ExamSessions.SingleAsync();
            s.Published = true;
            s.ReviewPolicy = "ScoreOnly";
            await publish.SaveChangesAsync();
        }
        await using (var verify = NewContext())
        {
            var visible = await Service(verify).ResultAsync(studentId, id);
            Assert.True(visible.Published);
            Assert.Equal(10m, visible.Score);
            Assert.Empty(visible.Questions);
        }
    }

    [SqlFact]
    public async Task ExpiryWorksWithoutAnOpenBrowserAndRepeatedSubmissionDoesNotCreateAnotherResult()
    {
        var id = await Start();
        clock.Set(clock.GetUtcNow().AddMinutes(21));
        await using (var worker = NewContext()) Assert.Equal(1, await Service(worker).ExpireAsync());
        await using (var retry = NewContext()) await Service(retry).SubmitAsync(studentId, id);
        await using var verify = NewContext();
        var attempt = await verify.Attempts.SingleAsync();
        Assert.Equal("Timeout", attempt.EndReason);
        Assert.Equal(attempt.Deadline, attempt.SubmittedAt);
        Assert.Equal(1, await verify.ExamResults.CountAsync());
    }

    [SqlFact]
    public async Task ExpiryCommitsHealthyAttemptsWhenOneResultCannotBeWritten()
    {
        var failingId = await Start();
        long healthyId;
        await using (var setup = NewContext())
        {
            var service = Service(setup);
            var classId = await setup.Set<SessionClass>().Where(x => x.SessionId == sessionId)
                .Select(x => x.ClassId).SingleAsync();
            setup.Set<ClassStudent>().Add(new ClassStudent { ClassId = classId, StudentId = otherStudentId });
            await setup.SaveChangesAsync();
            healthyId = await service.StartAsync(otherStudentId, sessionId);
            var answers = await setup.AttemptOptions.Where(x => x.Question.AttemptId == failingId && x.IsCorrect)
                .Select(x => new { QuestionId = x.AttemptQuestionId, OptionId = x.Id }).ToListAsync();
            Assert.NotEmpty(answers);
            foreach (var answer in answers)
                await service.SaveAsync(studentId, failingId, new(answer.QuestionId, answer.OptionId, false));
            // Fail the perfect score at the SQL boundary; the unanswered attempt still scores zero.
            await setup.Database.ExecuteSqlRawAsync(
                "ALTER TABLE dbo.ExamResults ADD CONSTRAINT CK_Test_ResultWrite CHECK (Score = 0)");
        }

        clock.Set(clock.GetUtcNow().AddMinutes(21));
        await using (var worker = NewContext())
        {
            await Assert.ThrowsAsync<AggregateException>(() => Service(worker).ExpireAsync());
            await worker.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.ExamResults DROP CONSTRAINT CK_Test_ResultWrite");
        }

        await using (var verify = NewContext())
        {
            Assert.Equal("InProgress", (await verify.Attempts.SingleAsync(x => x.Id == failingId)).Status);
            Assert.Equal("Submitted", (await verify.Attempts.SingleAsync(x => x.Id == healthyId)).Status);
            Assert.Equal(healthyId, (await verify.ExamResults.SingleAsync()).AttemptId);
        }

        await using (var retry = NewContext())
            Assert.Equal(1, await Service(retry).ExpireAsync());
        await using var completed = NewContext();
        Assert.Equal(2, await completed.ExamResults.CountAsync());
        Assert.Equal(10m, (await completed.ExamResults.SingleAsync(x => x.AttemptId == failingId)).Score);
        Assert.All(await completed.Attempts.ToListAsync(), attempt => Assert.Equal("Timeout", attempt.EndReason));
    }

    [SqlFact]
    public async Task StartingAfterTheSessionClosesDoesNotCreateAnAttempt()
    {
        clock.Set(clock.GetUtcNow().AddMinutes(31));
        await using var db = NewContext();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Service(db).StartAsync(studentId, sessionId))).Status);
        Assert.Empty(await db.Attempts.ToListAsync());
    }

    [SqlFact]
    public async Task DatabaseItselfRejectsDuplicateAttemptsEvenWithoutCallingTheService()
    {
        await Start();
        await using var db = NewContext();
        db.Attempts.Add(new Attempt { SessionId = sessionId, StudentId = studentId,
            StartedAt = clock.GetUtcNow().UtcDateTime, Deadline = clock.GetUtcNow().UtcDateTime.AddMinutes(1) });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SqlFact]
    public async Task DatabaseItselfRejectsAnOptionOwnedByAnotherQuestion()
    {
        var id = await Start();
        await using var db = NewContext();
        var questions = await db.AttemptQuestions.Include(q => q.Options).Include(q => q.Response)
            .Where(q => q.AttemptId == id).OrderBy(q => q.Id).ToListAsync();
        questions[0].Response!.SelectedOptionId = questions[1].Options[0].Id;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}

public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset current = now;
    public override DateTimeOffset GetUtcNow() => current;
    public void Set(DateTimeOffset value) => current = value;
}
