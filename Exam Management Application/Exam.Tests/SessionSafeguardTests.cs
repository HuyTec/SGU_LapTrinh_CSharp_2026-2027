using System.Text;
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
    public Task AssignmentRemovalProtectsLiveSessionsAndPreservesReadOnlyHistoryAfterSubmission()
        => VerifyAssignmentRemovalAsync(timeout: false);

    [SqlFact]
    public Task AssignmentRemovalAllowsHistoryAccessAtTheLastDeadlineBeforeWorkerGrading()
        => VerifyAssignmentRemovalAsync(timeout: true);

    private async Task VerifyAssignmentRemovalAsync(bool timeout)
    {
        var (teacher, student, paper, examClass, assignment) = await SeedSessionSafeguardsAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 11, 21, 7, 0, 0, TimeSpan.Zero));
        var sessions = new LecturerSessionService(Db, clock);
        var admin = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), clock);
        var draft = new SessionDraft("Protected session", paper.Id, clock.GetUtcNow().UtcDateTime.AddMinutes(1),
            clock.GetUtcNow().UtcDateTime.AddMinutes(5), 20, false, false, "ScoreOnly", [examClass.Id]);
        var id = await sessions.SaveAsync(teacher.Id, null, draft);
        await DenyRemovalAsync();
        await sessions.ChangeStateAsync(teacher.Id, id, "activate");
        await DenyRemovalAsync();
        clock.Set(clock.GetUtcNow().AddMinutes(1));
        await DenyRemovalAsync();
        var exams = new ExamService(Db, clock);
        var attempt = await exams.StartAsync(student.Id, id);
        var delivered = await exams.TakeAsync(student.Id, attempt);
        await sessions.ChangeStateAsync(teacher.Id, id, "closeAdmissions");
        await DenyRemovalAsync();

        if (timeout)
        {
            clock.Set(new DateTimeOffset(delivered.Deadline.AddTicks(-1), TimeSpan.Zero));
            await DenyRemovalAsync();
            clock.Set(new DateTimeOffset(delivered.Deadline, TimeSpan.Zero));
        }
        else
            await exams.SubmitAsync(student.Id, attempt);
        var cancelled = await sessions.SaveAsync(teacher.Id, null, draft with { Title = "Cancelled draft" });
        await sessions.ChangeStateAsync(teacher.Id, cancelled, "cancelDraft");
        var unpublished = await Assert.ThrowsAsync<BusinessException>(() =>
            admin.DeleteAsync(teacher.Id, "Administrator", "assignments", assignment.Id));
        Assert.Equal(409, unpublished.Status);
        Assert.Contains("chưa công bố", unpublished.Message);
        await admin.ActionAsync(teacher.Id, "Lecturer", "sessions", id, "publish");
        await admin.DeleteAsync(teacher.Id, "Administrator", "assignments", assignment.Id);
        Assert.False(await Db.LecturerSubjects.AnyAsync(a => a.Id == assignment.Id));
        Assert.Equal(id, Assert.Single(await sessions.ListAsync(teacher.Id)).Id);
        var listed = (await admin.ListAsync(teacher.Id, "Lecturer", "sessions", null, 1, 200)).Items;
        Assert.False((bool)listed.Single(row => Equals(row["id"], id))["canPublish"]!);
        Assert.Equal(student.Id, Assert.Single(await admin.SessionMonitorAsync(teacher.Id, "Lecturer", id))["id"]);
        Assert.Contains("SAFE001", Encoding.UTF8.GetString(await new ExportService(admin).ExportAsync(teacher.Id, "Lecturer", id, false)));
        Assert.NotEmpty((await admin.ListAsync(teacher.Id, "Lecturer", "monitor", null, 1, 200)).Items);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.ActionAsync(teacher.Id, "Lecturer", "sessions", id, "publish"))).Status);
        await Assert.ThrowsAsync<BusinessException>(() => sessions.SaveAsync(teacher.Id, null, draft));
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.SaveAsync(teacher.Id, "Lecturer", "topics", null, new(new()
            {
                ["subjectId"] = System.Text.Json.JsonSerializer.SerializeToElement(paper.SubjectId),
                ["name"] = System.Text.Json.JsonSerializer.SerializeToElement("Unauthorized topic")
            })))).Status);

        var replacement = new AppUser { UserName = "replacement-teacher", FullName = "Replacement" };
        Db.Users.Add(replacement);
        await Db.SaveChangesAsync();
        Db.LecturerSubjects.Add(new() { LecturerId = replacement.Id, SubjectId = paper.SubjectId });
        await Db.SaveChangesAsync();
        Assert.Empty(await sessions.ListAsync(replacement.Id));
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.SessionMonitorAsync(replacement.Id, "Lecturer", id))).Status);

        async Task DenyRemovalAsync()
        {
            var error = await Assert.ThrowsAsync<BusinessException>(() =>
                admin.DeleteAsync(teacher.Id, "Administrator", "assignments", assignment.Id));
            Assert.Equal(409, error.Status);
            Assert.Contains("chưa kết thúc", error.Message);
            Assert.True(await Db.LecturerSubjects.AnyAsync(a => a.Id == assignment.Id));
        }
    }

    [SqlFact]
    public async Task ActivationRejectsAnEmptyEligibleRosterWithoutWritingSnapshotsAndCanBeRetried()
    {
        var (teacher, student, paper, examClass, _) = await SeedSessionSafeguardsAsync();
        await Db.Users.Where(u => u.Id == student.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsActive, false));
        Db.Set<ClassStudent>().Add(new() { ClassId = examClass.Id, StudentId = teacher.Id });
        await Db.SaveChangesAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 11, 21, 7, 0, 0, TimeSpan.Zero));
        var sessions = new LecturerSessionService(Db, clock);
        var id = await sessions.SaveAsync(teacher.Id, null, new SessionDraft("Empty roster", paper.Id,
            clock.GetUtcNow().UtcDateTime.AddMinutes(1), clock.GetUtcNow().UtcDateTime.AddMinutes(5),
            20, false, false, "ScoreOnly", [examClass.Id]));
        var admin = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), clock);
        foreach (var activate in new Func<Task>[] {
            () => sessions.ChangeStateAsync(teacher.Id, id, "activate"),
            () => admin.ActionAsync(teacher.Id, "Lecturer", "sessions", id, "activate") })
        {
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(activate)).Status);
            var unchanged = await Db.ExamSessions.AsNoTracking().SingleAsync(s => s.Id == id);
            Assert.Equal("Draft", unchanged.Status);
            Assert.Null(unchanged.ActivatedAt);
            Assert.Null(unchanged.AccessCode);
            Assert.False(await Db.SessionCandidates.AnyAsync(c => c.SessionId == id));
            Assert.False(await Db.SessionQuestions.AnyAsync(q => q.SessionId == id));
        }
        await Db.Users.Where(u => u.Id == student.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsActive, true));
        await sessions.ChangeStateAsync(teacher.Id, id, "activate");
        Assert.Equal(student.Id, (await Db.SessionCandidates.SingleAsync(c => c.SessionId == id)).StudentId);
        Assert.Single(await Db.SessionQuestions.Where(q => q.SessionId == id).ToListAsync());
    }

    private async Task<(AppUser Teacher, AppUser Student, ExamPaper Paper, ExamClass Class, LecturerSubject Assignment)> SeedSessionSafeguardsAsync()
    {
        var teacher = new AppUser { UserName = "safeguard-teacher", FullName = "Teacher" };
        var student = new AppUser { UserName = "safeguard-student", FullName = "Student", StudentCode = "SAFE001" };
        var subject = new Subject { Code = "SAFE", Name = "Safeguards" };
        var question = new Question { Creator = teacher, Topic = new Topic { Subject = subject, Name = "Topic" },
            Text = "Question", Options = [new() { Text = "Right", Order = 1, IsCorrect = true }, new() { Text = "Wrong", Order = 2 }] };
        var paper = new ExamPaper { Creator = teacher, Subject = subject, Title = "Paper", Status = "Ready",
            Questions = [new() { Question = question, Order = 1, Points = 10m }] };
        Db.AddRange(paper, student);
        await Db.SaveChangesAsync();
        var role = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.Add(new() { UserId = student.Id, RoleId = role.Id });
        var examClass = new ExamClass { SubjectId = subject.Id, Code = "SAFE-CLASS", Students = [new() { StudentId = student.Id }] };
        var assignment = new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id };
        Db.AddRange(examClass, assignment);
        await Db.SaveChangesAsync();
        return (teacher, student, paper, examClass, assignment);
    }
}
