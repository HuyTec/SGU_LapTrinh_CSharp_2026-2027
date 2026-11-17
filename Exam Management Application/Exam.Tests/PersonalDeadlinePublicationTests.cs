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
    public async Task PublicationImmediatelyShowsFinishedResultsAndAutomaticallyIncludesLaterSubmissions()
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
        var session = new ExamSession { Exam = paper, Manager = teacher, Title = "Session", Status = "Active",
            OpensAt = clock.GetUtcNow().UtcDateTime.AddMinutes(-1), ClosesAt = clock.GetUtcNow().UtcDateTime.AddMinutes(5),
            DurationMinutes = 20 };
        Db.AddRange(paper, student, later);
        await Db.SaveChangesAsync();
        var examClass = new ExamClass { Code = "PUBLISH", SubjectId = subject.Id, Students = [new() { StudentId = student.Id }, new() { StudentId = later.Id }] };
        session.Classes.Add(new() { Class = examClass });
        Db.AddRange(session, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        var exams = new ExamService(Db, clock);
        var attemptId = await exams.StartAsync(student.Id, session.Id);
        var laterId = await exams.StartAsync(later.Id, session.Id);
        var admin = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), clock);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.ActionAsync(student.Id, "Lecturer", "sessions", session.Id, "publish", "Answers"))).Status);
        await Assert.ThrowsAsync<BusinessException>(() =>
            admin.ActionAsync(teacher.Id, "Lecturer", "sessions", session.Id, "publish", "Invalid"));
        await admin.ActionAsync(teacher.Id, "Lecturer", "sessions", session.Id, "publish", "Answers");
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
        Assert.Equal("A", Assert.Single(immediate.Questions).SelectedAnswer);
        Assert.True(Assert.Single(immediate.Questions).IsCorrect);
        Assert.Null(Assert.Single(immediate.Questions).CorrectAnswer);
        var stillWorking = await exams.ResultAsync(later.Id, laterId);
        Assert.False(stillWorking.Published);
        Assert.Equal("InProgress", stillWorking.Status);
        Assert.Null(stillWorking.Score);
        Assert.Equal(0, await exams.ExpireAsync());
        clock.Set(clock.GetUtcNow().AddMinutes(1));
        var laterQuestion = (await exams.TakeAsync(later.Id, laterId)).Questions.Single();
        await exams.SaveAsync(later.Id, laterId, new(laterQuestion.Id, laterQuestion.Options.Last().Id, false));
        await exams.SubmitAsync(later.Id, laterId);
        var laterResult = await exams.ResultAsync(later.Id, laterId);
        Assert.True(laterResult.Published);
        Assert.False(Assert.Single(laterResult.Questions).IsCorrect);
        Assert.Equal("B", Assert.Single(laterResult.Questions).SelectedAnswer);
        Assert.Null(Assert.Single(laterResult.Questions).CorrectAnswer);
        await admin.ActionAsync(teacher.Id, "Lecturer", "sessions", session.Id, "publish");
        Assert.Equal(publishedAt, (await Db.ExamSessions.AsNoTracking().SingleAsync(s => s.Id == session.Id)).PublishedAt);
    }
}
