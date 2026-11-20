using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task SessionValidationIdentifiesOnlyTheFieldThatNeedsCorrection()
    {
        var service = new LecturerSessionService(Db, TimeProvider.System);
        var now = DateTime.UtcNow;
        var draft = new SessionDraft("Giải tích", 1, now, now.AddMinutes(-40), 20,
            false, false, "ScoreOnly", []);
        var dateError = await Assert.ThrowsAsync<BusinessException>(() => service.SaveAsync(1, null, draft));
        Assert.Equal(nameof(SessionDraft.ClosesAt), dateError.Field);
        Assert.Contains("Giờ đóng phải sau giờ mở", dateError.Message);
        Assert.DoesNotContain("tên đợt", dateError.Message);
        var durationError = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveAsync(1, null, draft with { ClosesAt = now.AddMinutes(10), DurationMinutes = 0 }));
        Assert.Equal(nameof(SessionDraft.DurationMinutes), durationError.Field);
        Assert.Contains("ít nhất 1 phút", durationError.Message);
        var titleError = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveAsync(1, null, draft with { Title = new string('x', 201) }));
        Assert.Equal(nameof(SessionDraft.Title), titleError.Field);
        Assert.Empty(await Db.ExamSessions.ToListAsync());
    }

    [SqlFact]
    public async Task LecturerReplacesDraftClassAndActivationLocksConfiguration()
    {
        var teacher = new AppUser { UserName = "session-teacher", FullName = "Session Teacher" };
        var outsider = new AppUser { UserName = "outsider", FullName = "Outsider" };
        var eligible = new AppUser { UserName = "eligible", FullName = "Eligible Student" };
        var inactive = new AppUser { UserName = "inactive", FullName = "Inactive Student", IsActive = false };
        var firstOnly = new AppUser { UserName = "first-only", FullName = "First Class Student" };
        var subject = new Subject { Code = "SESSION", Name = "Session Subject" };
        var topic = new Topic { Subject = subject, Name = "Topic" };
        var question = new Question
        {
            Creator = teacher, Topic = topic, Text = "Question", Difficulty = "Medium",
            Options = [new() { Text = "Right", IsCorrect = true, Order = 1 }, new() { Text = "Wrong", Order = 2 }]
        };
        var paper = new ExamPaper
        {
            Subject = subject, Creator = teacher, Title = "Ready paper", Status = "Ready",
            Questions = [new() { Question = question, Points = 2.5m, Order = 1 }]
        };
        Db.AddRange(paper, outsider, eligible, inactive, firstOnly);
        await Db.SaveChangesAsync();
        var role = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.AddRange(new[] { eligible, inactive, firstOnly }
            .Select(u => new IdentityUserRole<int> { RoleId = role.Id, UserId = u.Id }));
        var first = new ExamClass { Code = "FIRST", SubjectId = subject.Id, Students = [new() { StudentId = firstOnly.Id }] };
        var second = new ExamClass
        {
            Code = "SECOND", SubjectId = subject.Id,
            Students = [new() { StudentId = eligible.Id }, new() { StudentId = inactive.Id }, new() { StudentId = outsider.Id }]
        };
        Db.AddRange(first, second, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        var clock = new SessionTestClock(new DateTimeOffset(2026, 11, 18, 7, 0, 0, TimeSpan.Zero));
        var service = new LecturerSessionService(Db, clock);
        var draft = new SessionDraft("Session", paper.Id, clock.GetUtcNow().UtcDateTime.AddMinutes(1),
            clock.GetUtcNow().UtcDateTime.AddMinutes(6), 20, false, false, "ScoreOnly", [first.Id]);
        var id = await service.SaveAsync(teacher.Id, null, draft);
        await Assert.ThrowsAsync<BusinessException>(() => service.EditorAsync(outsider.Id, id));
        await service.SaveAsync(teacher.Id, id, draft with { ClassIds = [second.Id] });
        Assert.Equal(second.Id, (await Db.Set<SessionClass>().SingleAsync(c => c.SessionId == id)).ClassId);
        await service.ChangeStateAsync(teacher.Id, id, "activate");
        var active = (await service.ListAsync(teacher.Id)).Single();
        Assert.False(string.IsNullOrWhiteSpace(active.Code));
        Assert.False(active.CanEdit);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, (await Db.ExamSessions.SingleAsync(s => s.Id == id)).ActivatedAt);
        Assert.Equal(eligible.Id, (await Db.SessionCandidates.SingleAsync(c => c.SessionId == id)).StudentId);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(
            () => service.SaveAsync(teacher.Id, id, draft))).Status);
        var following = await service.SaveAsync(teacher.Id, null, draft with
        {
            Title = "Following", OpensAt = clock.GetUtcNow().UtcDateTime.AddMinutes(10),
            ClosesAt = clock.GetUtcNow().UtcDateTime.AddMinutes(15), ClassIds = [second.Id]
        });
        await service.ChangeStateAsync(teacher.Id, following, "activate");
        Assert.Equal(2, await Db.ExamSessions.CountAsync(s => s.Status == "Active"));
        Assert.Equal(2, await Db.SessionCandidates.CountAsync(c => c.StudentId == eligible.Id));

        await Db.Set<ClassStudent>().Where(m => m.ClassId == second.Id && m.StudentId == eligible.Id).ExecuteDeleteAsync();
        Db.Set<ClassStudent>().Add(new() { ClassId = second.Id, StudentId = firstOnly.Id });
        await Db.SaveChangesAsync();
        var monitor = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), clock);
        var row = Assert.Single(await monitor.SessionMonitorAsync(teacher.Id, "Lecturer", id));
        Assert.Equal(eligible.Id, row["id"]);
        Assert.Equal("NotStarted", row["status"]);
        Assert.Null(row["score"]);
    }

    [SqlFact]
    public async Task PostponementClearsTheRosterAndCancellationKeepsIt()
    {
        var teacher = new AppUser { UserName = "withdraw-teacher", FullName = "Teacher" };
        var student = new AppUser { UserName = "withdraw-student", FullName = "Student" };
        var subject = new Subject { Code = "WITHDRAW", Name = "Subject" };
        var paper = new ExamPaper { Creator = teacher, Subject = subject, Title = "Paper", Status = "Ready" };
        Db.AddRange(paper, student);
        await Db.SaveChangesAsync();
        var role = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.Add(new() { RoleId = role.Id, UserId = student.Id });
        var chosen = new ExamClass { Code = "CHOSEN", SubjectId = subject.Id, Students = [new() { StudentId = student.Id }] };
        Db.AddRange(chosen, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();

        var clock = new SessionTestClock(new DateTimeOffset(2026, 11, 20, 7, 0, 0, TimeSpan.Zero));
        var now = clock.GetUtcNow().UtcDateTime;
        var service = new LecturerSessionService(Db, clock);
        var id = await service.SaveAsync(teacher.Id, null,
            new SessionDraft("Session", paper.Id, now.AddMinutes(10), now.AddMinutes(30), 20, false, false, "ScoreOnly", [chosen.Id]));

        await service.ChangeStateAsync(teacher.Id, id, "activate");
        Assert.True(await Db.SessionCandidates.AnyAsync(c => c.SessionId == id));
        await service.ChangeStateAsync(teacher.Id, id, "postpone");
        var draft = await Db.ExamSessions.SingleAsync(s => s.Id == id);
        Assert.Equal("Draft", draft.Status);
        Assert.Null(draft.ActivatedAt);
        Assert.False(await Db.SessionCandidates.AnyAsync(c => c.SessionId == id));

        await service.ChangeStateAsync(teacher.Id, id, "activate");
        await service.ChangeStateAsync(teacher.Id, id, "cancel");
        var cancelled = await Db.ExamSessions.SingleAsync(s => s.Id == id);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(now, cancelled.ActivatedAt);
        Assert.Equal(student.Id, (await Db.SessionCandidates.SingleAsync(c => c.SessionId == id)).StudentId);
    }

    private sealed class SessionTestClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
