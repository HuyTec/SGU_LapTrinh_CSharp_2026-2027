using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.EntityFrameworkCore;
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
        Db.AddRange(paper, outsider);
        await Db.SaveChangesAsync();
        var first = new ExamClass { Code = "FIRST", SubjectId = subject.Id };
        var second = new ExamClass { Code = "SECOND", SubjectId = subject.Id };
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
        Assert.Empty(await Db.SessionCandidates.Where(c => c.SessionId == id).ToListAsync());
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(
            () => service.SaveAsync(teacher.Id, id, draft))).Status);
        var following = await service.SaveAsync(teacher.Id, null, draft with
        {
            Title = "Following", OpensAt = clock.GetUtcNow().UtcDateTime.AddMinutes(10),
            ClosesAt = clock.GetUtcNow().UtcDateTime.AddMinutes(15), ClassIds = [second.Id]
        });
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(
            () => service.ChangeStateAsync(teacher.Id, following, "activate"))).Status);
    }

    private sealed class SessionTestClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
