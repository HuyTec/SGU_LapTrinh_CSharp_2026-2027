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
    public Task PublicationShowsScoresButWithholdsAnswersUntilAdmissionsClose()
        => VerifyPublicationAsync("Answers", "finishBeforeClose");

    [SqlFact]
    public Task PublicationWithholdsCorrectAnswersWhileAnotherCandidateIsWorking()
        => VerifyPublicationAsync("CorrectAnswers", "finishAfterClose");

    [SqlFact]
    public Task PublicationOpensReviewAtTheLastDeadlineWithoutWaitingForTheWorker()
        => VerifyPublicationAsync("CorrectAnswers", "timeout");

    [SqlFact]
    public Task PublicationKeepsScoreOnlyPrivateAfterEarlyAdmissionClosure()
        => VerifyPublicationAsync("ScoreOnly", "earlyClose");

    [SqlFact]
    public Task PublicationOpensCorrectAnswersAfterEarlyClosureAndTheLastSubmission()
        => VerifyPublicationAsync("CorrectAnswers", "earlyClose");

    private async Task VerifyPublicationAsync(string policy, string completion)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 11, 18, 7, 0, 0, TimeSpan.Zero));
        var teacher = new AppUser { UserName = "publish-teacher", FullName = "Teacher" };
        var student = new AppUser { UserName = "publish-student", FullName = "Student" };
        var later = new AppUser { UserName = "publish-later", FullName = "Later" };
        var subject = new Subject { Code = "PUBLISH", Name = "Publish" };
        var question = new Question { Creator = teacher, Topic = new Topic { Subject = subject, Name = "Topic" },
            Text = "Question", Difficulty = "Easy", Options = [new() { Text = "A", IsCorrect = true, Order = 1 }, new() { Text = "B", Order = 2 }] };
        var paper = new ExamPaper { Subject = subject, Creator = teacher, Title = "Paper", Status = "Ready",
            Questions = [new() { Question = question, Points = 1, Order = 1 }] };
        var session = new ExamSession { Exam = paper, Manager = teacher, Title = "Session",
            OpensAt = clock.GetUtcNow().UtcDateTime.AddMinutes(-1), ClosesAt = clock.GetUtcNow().UtcDateTime.AddMinutes(5),
            DurationMinutes = 20 };
        Db.AddRange(paper, student, later);
        await Db.SaveChangesAsync();
        var examClass = new ExamClass { Code = "PUBLISH", SubjectId = subject.Id, Students = [new() { StudentId = student.Id }, new() { StudentId = later.Id }] };
        session.Classes.Add(new() { Class = examClass });
        Db.AddRange(session, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        var studentRole = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.AddRange(new[] { student, later }.Select(u =>
            new IdentityUserRole<int> { UserId = u.Id, RoleId = studentRole.Id }));
        await Db.SaveChangesAsync();
        var sessions = new LecturerSessionService(Db, clock);
        await sessions.ChangeStateAsync(teacher.Id, session.Id, "activate");
        var exams = new ExamService(Db, clock);
        var attemptId = await exams.StartAsync(student.Id, session.Id);
        var laterId = await exams.StartAsync(later.Id, session.Id);
        var admin = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), clock);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.ActionAsync(student.Id, "Lecturer", "sessions", session.Id, "publish", "Answers"))).Status);
        await Assert.ThrowsAsync<BusinessException>(() =>
            admin.ActionAsync(teacher.Id, "Lecturer", "sessions", session.Id, "publish", "Invalid"));
        await admin.ActionAsync(teacher.Id, "Lecturer", "sessions", session.Id, "publish", policy);
        var publishedAt = (await Db.ExamSessions.AsNoTracking().SingleAsync(s => s.Id == session.Id)).PublishedAt;
        Assert.NotNull(publishedAt);
        var unfinished = await exams.ResultAsync(student.Id, attemptId);
        Assert.False(unfinished.Published);
        Assert.Null(unfinished.Score);
        Assert.Empty(unfinished.Questions);
        var delivered = await exams.TakeAsync(student.Id, attemptId);
        var item = delivered.Questions.Single();
        await exams.SaveAsync(student.Id, attemptId, new(item.Id, item.Options.First().Id, false));
        await exams.SubmitAsync(student.Id, attemptId);
        var immediate = await exams.ResultAsync(student.Id, attemptId);
        Assert.True(immediate.Published);
        Assert.Equal(10m, immediate.Score);
        Assert.Empty(immediate.Questions);
        Assert.Empty(Assert.Single((await exams.HistoryAsync(student.Id)).Results).Questions);
        var stillWorking = await exams.ResultAsync(later.Id, laterId);
        Assert.False(stillWorking.Published);
        Assert.Equal("InProgress", stillWorking.Status);
        Assert.Null(stillWorking.Score);
        Assert.Equal(0, await exams.ExpireAsync());
        if (completion == "timeout")
        {
            clock.Set(new DateTimeOffset(delivered.Deadline.AddTicks(-1), TimeSpan.Zero));
            Assert.Empty((await exams.ResultAsync(student.Id, attemptId)).Questions);
            clock.Set(new DateTimeOffset(delivered.Deadline, TimeSpan.Zero));
            Assert.Equal("A", Assert.Single((await exams.ResultAsync(student.Id, attemptId)).Questions).CorrectAnswer);
            // The other attempt still awaits background grading, but can no longer save answers.
            Assert.Equal("InProgress", (await Db.Attempts.AsNoTracking().SingleAsync(a => a.Id == laterId)).Status);
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() =>
                exams.SaveAsync(later.Id, laterId, new(item.Id, null, false)))).Status);
            return;
        }
        if (completion == "finishAfterClose")
        {
            clock.Set(clock.GetUtcNow().AddMinutes(5));
            Assert.Empty((await exams.ResultAsync(student.Id, attemptId)).Questions);
        }
        else if (completion == "earlyClose")
        {
            await sessions.ChangeStateAsync(teacher.Id, session.Id, "closeAdmissions");
            Assert.Empty((await exams.ResultAsync(student.Id, attemptId)).Questions);
        }
        var laterQuestion = (await exams.TakeAsync(later.Id, laterId)).Questions.Single();
        await exams.SaveAsync(later.Id, laterId, new(laterQuestion.Id, laterQuestion.Options.Last().Id, false));
        await exams.SubmitAsync(later.Id, laterId);
        var laterResult = await exams.ResultAsync(later.Id, laterId);
        Assert.True(laterResult.Published);
        if (completion == "finishBeforeClose")
        {
            Assert.Empty(laterResult.Questions);
            clock.Set(clock.GetUtcNow().AddMinutes(5).AddTicks(-1));
            Assert.Empty((await exams.ResultAsync(student.Id, attemptId)).Questions);
            clock.Set(clock.GetUtcNow().AddTicks(1));
        }
        var reviewed = await exams.ResultAsync(student.Id, attemptId);
        if (policy == "ScoreOnly")
            Assert.Empty(reviewed.Questions);
        else
        {
            Assert.True(Assert.Single(reviewed.Questions).IsCorrect);
            Assert.Equal("A", Assert.Single(reviewed.Questions).SelectedAnswer);
            Assert.Equal(policy == "CorrectAnswers" ? "A" : null, Assert.Single(reviewed.Questions).CorrectAnswer);
            var laterReview = Assert.Single((await exams.ResultAsync(later.Id, laterId)).Questions);
            Assert.False(laterReview.IsCorrect);
            Assert.Equal("B", laterReview.SelectedAnswer);
        }
        await admin.ActionAsync(teacher.Id, "Lecturer", "sessions", session.Id, "publish");
        Assert.Equal(publishedAt, (await Db.ExamSessions.AsNoTracking().SingleAsync(s => s.Id == session.Id)).PublishedAt);
    }
}
