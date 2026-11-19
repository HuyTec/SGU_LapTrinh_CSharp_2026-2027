using Exam.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task DeadlineMigrationExtendsOnlyUnfinishedAttemptsClippedByAdmissionClose()
    {
        await Db.GetService<IMigrator>().MigrateAsync("20261117160000_RetireExamOfficer");
        var now = new DateTime(2026, 11, 18, 7, 0, 0, DateTimeKind.Utc);
        var teacher = new AppUser { UserName = "deadline-teacher", FullName = "Teacher" };
        var first = new AppUser { UserName = "deadline-first", FullName = "First" };
        var second = new AppUser { UserName = "deadline-second", FullName = "Second" };
        var paper = new ExamPaper { Creator = teacher, Subject = new Subject { Code = "DEADLINE", Name = "Deadline" }, Title = "Paper", Status = "Ready" };
        var session = new ExamSession
        {
            Exam = paper, Manager = teacher, Title = "Admission", Status = "Active",
            OpensAt = now.AddMinutes(-5), ClosesAt = now.AddMinutes(5), DurationMinutes = 20
        };
        Db.AddRange(session, first, second);
        await Db.SaveChangesAsync();
        var running = new Attempt { SessionId = session.Id, StudentId = first.Id, StartedAt = now, Deadline = session.ClosesAt };
        var ended = new Attempt { SessionId = session.Id, StudentId = second.Id, StartedAt = now, Deadline = session.ClosesAt,
            Status = "Submitted", SubmittedAt = now.AddMinutes(1), EndReason = "Manual",
            Result = new() { Score = 0, CorrectCount = 0, GradedAt = now.AddMinutes(1) } };
        Db.AddRange(running, ended);
        await Db.SaveChangesAsync();
        await Db.Database.MigrateAsync();
        Db.ChangeTracker.Clear();
        Assert.Equal(now.AddMinutes(20), (await Db.Attempts.SingleAsync(a => a.Id == running.Id)).Deadline);
        Assert.Equal(now.AddMinutes(5), (await Db.Attempts.SingleAsync(a => a.Id == ended.Id)).Deadline);
        Assert.Equal("Submitted", (await Db.Attempts.SingleAsync(a => a.Id == ended.Id)).Status);
        Assert.Single(await Db.ExamResults.ToListAsync());
    }
}
