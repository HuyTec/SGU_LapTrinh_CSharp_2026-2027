using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task ClassDetailRequiresMembershipAndHidesDraftCancelledAndWrongSubjectSessions()
    {
        var teacher = new AppUser { UserName = "detail-teacher", FullName = "Detail teacher" };
        var student = new AppUser { UserName = "detail-student", FullName = "Detail student" };
        var outsider = new AppUser { UserName = "detail-outsider", FullName = "Outsider" };
        var subject = new Subject { Code = "DETAIL", Name = "Subject of class" };
        var otherSubject = new Subject { Code = "OTHER", Name = "Other subject" };
        Db.AddRange(teacher, student, outsider, subject, otherSubject);
        await Db.SaveChangesAsync();
        var exam = new ExamPaper { SubjectId = subject.Id, Creator = teacher, Title = "Class exam", Status = "Ready" };
        var wrongExam = new ExamPaper { SubjectId = otherSubject.Id, Creator = teacher, Title = "Wrong subject", Status = "Ready" };
        var examClass = new ExamClass { SubjectId = subject.Id, Code = "DETAIL-A", Students = [new() { StudentId = student.Id }] };
        Db.AddRange(exam, wrongExam, examClass, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        var now = DateTime.UtcNow;
        ExamSession Session(string title, ExamPaper paper, string status) => new()
        {
            Exam = paper, Manager = teacher, Title = title, Status = status,
            OpensAt = now.AddMinutes(-1), ClosesAt = now.AddHours(1), DurationMinutes = 30,
            Classes = [new() { ClassId = examClass.Id }]
        };
        var visible = Session("Visible", exam, "Active");
        var cancelled = Session("Cancelled hidden", exam, "Cancelled");
        Db.AddRange(visible, cancelled, Session("Draft hidden", exam, "Draft"), Session("Wrong subject hidden", wrongExam, "Active"));
        await Db.SaveChangesAsync();
        var service = new StudentClassService(Db);

        var detail = await service.DetailAsync(student.Id, examClass.Id);
        Assert.Equal(examClass.Id, detail.Class.Id);
        Assert.Equal("Subject of class", detail.Class.Subject);
        Assert.Equal("Detail teacher", Assert.Single(detail.Lecturers));
        Assert.Equal(visible.Id, Assert.Single(detail.Sessions).Id);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() =>
            new ExamService(Db, TimeProvider.System).DetailAsync(student.Id, cancelled.Id))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => service.DetailAsync(outsider.Id, examClass.Id))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => service.DetailAsync(student.Id, int.MaxValue))).Status);

        examClass.IsActive = false;
        await Db.SaveChangesAsync();
        Assert.Empty((await service.DetailAsync(student.Id, examClass.Id)).Sessions);
        Db.Attempts.Add(new Attempt
        {
            SessionId = visible.Id, StudentId = student.Id, StartedAt = now, Deadline = now.AddMinutes(30)
        });
        await Db.SaveChangesAsync();
        Assert.Equal(visible.Id, Assert.Single((await service.DetailAsync(student.Id, examClass.Id)).Sessions).Id);
        Assert.False(Assert.Single(await service.ListAsync(student.Id)).IsActive);
    }

    [SqlFact]
    public async Task ClassDetailAndSessionDetailShareTheActivatedRosterInsteadOfCurrentMembership()
    {
        var teacher = new AppUser { UserName = "roster-detail-teacher", FullName = "Teacher" };
        var eligible = new AppUser { UserName = "roster-detail-eligible", FullName = "Eligible" };
        var late = new AppUser { UserName = "roster-detail-late", FullName = "Late member" };
        var subject = new Subject { Code = "ROSTER-DETAIL", Name = "Roster subject" };
        var question = new Question
        {
            Creator = teacher, Topic = new Topic { Subject = subject, Name = "Topic" }, Text = "Question",
            Options = [new() { Text = "Right", Order = 1, IsCorrect = true }, new() { Text = "Wrong", Order = 2 }]
        };
        var paper = new ExamPaper { Creator = teacher, Subject = subject, Title = "Paper", Status = "Ready",
            Questions = [new() { Question = question, Order = 1, Points = 10m }] };
        Db.AddRange(paper, eligible, late);
        await Db.SaveChangesAsync();
        var studentRole = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.AddRange(new[] { eligible, late }.Select(u =>
            new IdentityUserRole<int> { UserId = u.Id, RoleId = studentRole.Id }));
        var examClass = new ExamClass { SubjectId = subject.Id, Code = "ROSTER-CLASS",
            Students = [new() { StudentId = eligible.Id }] };
        Db.AddRange(examClass, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 11, 20, 7, 0, 0, TimeSpan.Zero));
        var sessions = new LecturerSessionService(Db, clock);
        var sessionId = await sessions.SaveAsync(teacher.Id, null, new SessionDraft("Roster session", paper.Id,
            clock.GetUtcNow().UtcDateTime.AddMinutes(1), clock.GetUtcNow().UtcDateTime.AddMinutes(30),
            20, false, false, "ScoreOnly", [examClass.Id]));
        await sessions.ChangeStateAsync(teacher.Id, sessionId, "activate");
        Db.Set<ClassStudent>().Add(new() { ClassId = examClass.Id, StudentId = late.Id });
        await Db.SaveChangesAsync();

        var classes = new StudentClassService(Db);
        var exams = new ExamService(Db, clock);
        Assert.Empty((await classes.DetailAsync(late.Id, examClass.Id)).Sessions);
        Assert.Empty((await exams.SessionsAsync(late.Id)).Sessions);
        Assert.Empty(Assert.Single(await exams.StudentClassesAsync(late.Id)).Sessions);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => exams.DetailAsync(late.Id, sessionId))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() => exams.StartAsync(late.Id, sessionId))).Status);

        var listed = Assert.Single((await classes.DetailAsync(eligible.Id, examClass.Id)).Sessions);
        Assert.Equal(sessionId, listed.Id);
        Assert.Equal(listed.Id, (await exams.DetailAsync(eligible.Id, listed.Id)).Session.Id);

        // Disabling a class does not change the roster already frozen for an active exam.
        await Db.Set<ExamClass>().Where(c => c.Id == examClass.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.IsActive, false));
        Assert.Equal(sessionId, Assert.Single((await classes.DetailAsync(eligible.Id, examClass.Id)).Sessions).Id);
        Assert.Equal(sessionId, (await exams.DetailAsync(eligible.Id, sessionId)).Session.Id);
        Assert.Empty((await classes.DetailAsync(late.Id, examClass.Id)).Sessions);

        clock.Set(clock.GetUtcNow().AddMinutes(1));
        var attemptId = await exams.StartAsync(eligible.Id, sessionId);
        Assert.True(attemptId > 0);
        Assert.Equal(attemptId, (await exams.DetailAsync(eligible.Id, sessionId)).Session.AttemptId);
        Assert.Equal(sessionId, Assert.Single((await classes.DetailAsync(eligible.Id, examClass.Id)).Sessions).Id);
    }
}
