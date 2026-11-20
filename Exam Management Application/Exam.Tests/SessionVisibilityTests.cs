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
    public Task ClosedSessionsStayVisibleUntilAndAfterTheLastPersonalDeadline()
        => VerifyClosedSessionVisibilityAsync(earlyClose: false);

    [SqlFact]
    public Task EarlyClosureStaysVisibleAndCompletesWhenTheLastCandidateSubmits()
        => VerifyClosedSessionVisibilityAsync(earlyClose: true);

    private async Task VerifyClosedSessionVisibilityAsync(bool earlyClose)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 11, 20, 7, 0, 0, TimeSpan.Zero));
        var teacher = new AppUser { UserName = "visibility-teacher", FullName = "Teacher" };
        var students = Enumerable.Range(1, 3).Select(i => new AppUser
            { UserName = $"visibility-student-{i}", FullName = $"Student {i}" }).ToArray();
        var subject = new Subject { Code = "VISIBILITY", Name = "Visibility" };
        var question = new Question { Creator = teacher, Topic = new Topic { Subject = subject, Name = "Topic" },
            Text = "Question", Options = [new() { Text = "Right", Order = 1, IsCorrect = true }, new() { Text = "Wrong", Order = 2 }] };
        var paper = new ExamPaper { Creator = teacher, Subject = subject, Title = "Paper", Status = "Ready",
            Questions = [new() { Question = question, Order = 1, Points = 10m }] };
        Db.Add(paper);
        Db.Users.AddRange(students);
        await Db.SaveChangesAsync();
        var role = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.AddRange(students.Select(u => new IdentityUserRole<int> { UserId = u.Id, RoleId = role.Id }));
        var examClass = new ExamClass { SubjectId = subject.Id, Code = "VISIBILITY-CLASS",
            Students = students.Select(u => new ClassStudent { StudentId = u.Id }).ToList() };
        Db.AddRange(examClass, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        var sessions = new LecturerSessionService(Db, clock);
        var sessionId = await sessions.SaveAsync(teacher.Id, null, new SessionDraft("Visible session", paper.Id,
            clock.GetUtcNow().UtcDateTime.AddMinutes(-1), clock.GetUtcNow().UtcDateTime.AddMinutes(5),
            20, false, false, "ScoreOnly", [examClass.Id]));
        await sessions.ChangeStateAsync(teacher.Id, sessionId, "activate");
        var exams = new ExamService(Db, clock);
        var admin = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), clock);
        var first = await exams.StartAsync(students[0].Id, sessionId);
        clock.Set(clock.GetUtcNow().AddMinutes(1));
        var last = await exams.StartAsync(students[1].Id, sessionId);
        var lastDeadline = (await exams.TakeAsync(students[1].Id, last)).Deadline;

        if (earlyClose)
            await sessions.ChangeStateAsync(teacher.Id, sessionId, "closeAdmissions");
        else
            clock.Set(clock.GetUtcNow().AddMinutes(4));
        await AssertVisibleAsync("Closed");
        Assert.Equal(first, (await exams.DetailAsync(students[0].Id, sessionId)).Session.AttemptId);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => exams.StartAsync(students[2].Id, sessionId))).Status);
        Assert.Equal(first, await exams.StartAsync(students[0].Id, sessionId));
        var delivered = await exams.TakeAsync(students[0].Id, first);
        await exams.SaveAsync(students[0].Id, first,
            new(delivered.Questions[0].Id, delivered.Questions[0].Options[0].Id, false));
        await exams.SubmitAsync(students[0].Id, first);
        await AssertVisibleAsync("Closed");

        if (earlyClose)
            await exams.SubmitAsync(students[1].Id, last);
        else
        {
            clock.Set(new DateTimeOffset(lastDeadline.AddTicks(-1), TimeSpan.Zero));
            await AssertVisibleAsync("Closed");
            clock.Set(new DateTimeOffset(lastDeadline, TimeSpan.Zero));
        }
        await AssertVisibleAsync("Completed");
        Assert.Empty((await exams.SessionsAsync(students[0].Id, status: "Closed")).Sessions);
        Assert.Equal(sessionId, Assert.Single((await exams.SessionsAsync(students[0].Id, status: "Completed")).Sessions).Id);
        if (!earlyClose)
            Assert.Equal("InProgress", (await Db.Attempts.AsNoTracking().SingleAsync(a => a.Id == last)).Status);

        async Task AssertVisibleAsync(string expected)
        {
            foreach (var student in students)
            {
                var listed = Assert.Single((await exams.SessionsAsync(student.Id)).Sessions);
                Assert.Equal(sessionId, listed.Id);
                Assert.Equal(expected, listed.Status);
                Assert.Equal(expected, (await exams.DetailAsync(student.Id, sessionId)).Session.Status);
                Assert.Equal(sessionId, Assert.Single(Assert.Single(await exams.StudentClassesAsync(student.Id)).Sessions).Id);
            }
            var lecturerRow = Assert.Single(await sessions.ListAsync(teacher.Id));
            Assert.Equal(sessionId, lecturerRow.Id);
            Assert.Equal(expected, lecturerRow.State);
            Assert.Null(lecturerRow.Code);
            Assert.Equal(expected, Assert.Single((await admin.ListAsync(teacher.Id, "Lecturer", "sessions", null, 1, 200)).Items)["status"]);
        }
    }
}
