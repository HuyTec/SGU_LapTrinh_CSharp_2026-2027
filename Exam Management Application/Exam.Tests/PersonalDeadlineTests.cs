using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Exam.Tests;

public partial class SqlExamTests
{
    [SqlFact]
    public async Task AdmissionClosingDoesNotShortenIndividualAttempts()
    {
        var origin = clock.GetUtcNow();
        int thirdStudent;
        int followingSession;
        await using (var setup = NewContext())
        {
            var session = await setup.ExamSessions.SingleAsync();
            var classId = await setup.Set<SessionClass>().Select(c => c.ClassId).SingleAsync();
            var third = new AppUser { UserName = "late-new", FullName = "Late new" };
            setup.Users.Add(third);
            await setup.SaveChangesAsync();
            thirdStudent = third.Id;
            setup.AddRange(new ClassStudent { ClassId = classId, StudentId = otherStudentId },
                new ClassStudent { ClassId = classId, StudentId = third.Id });
            var role = await setup.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
            setup.UserRoles.Add(new() { UserId = third.Id, RoleId = role.Id });
            await setup.SaveChangesAsync();
            var sessions = new LecturerSessionService(setup, clock);
            var draft = new SessionDraft("Personal deadlines", session.ExamId, origin.UtcDateTime,
                origin.UtcDateTime.AddMinutes(5), 20, false, false, "ScoreOnly", [classId]);
            sessionId = await sessions.SaveAsync(session.ManagedBy, null, draft);
            await sessions.ChangeStateAsync(session.ManagedBy, sessionId, "activate");
            followingSession = await sessions.SaveAsync(session.ManagedBy, null,
                draft with { Title = "Following", ClosesAt = origin.UtcDateTime.AddMinutes(15) });
            await sessions.ChangeStateAsync(session.ManagedBy, followingSession, "activate");
        }

        clock.Set(origin.AddMinutes(4));
        var firstId = await Start();
        clock.Set(origin.AddMinutes(4).AddSeconds(30));
        long secondId;
        await using (var second = NewContext())
            secondId = await Service(second).StartAsync(otherStudentId, sessionId);

        clock.Set(origin.AddMinutes(5));
        await using (var closed = NewContext())
        {
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(
                () => Service(closed).StartAsync(thirdStudent, sessionId))).Status);
            Assert.Equal(firstId, await Service(closed).StartAsync(studentId, sessionId));
        }
        clock.Set(origin.AddMinutes(6));
        await using (var running = NewContext())
        {
            var service = Service(running);
            var attempt = await service.TakeAsync(studentId, firstId);
            Assert.Equal(origin.UtcDateTime.AddMinutes(24), attempt.Deadline);
            var question = attempt.Questions.First();
            await service.SaveAsync(studentId, firstId, new(question.Id, question.Options.First().Id, false));
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(
                () => service.StartAsync(studentId, followingSession))).Status);
            Assert.Equal(0, await service.ExpireAsync());
            Assert.Empty(await running.ExamResults.ToListAsync());
        }
        clock.Set(origin.AddMinutes(24));
        await using (var firstExpiry = NewContext())
        {
            Assert.Equal(1, await Service(firstExpiry).ExpireAsync());
            Assert.Equal("Submitted", (await firstExpiry.Attempts.SingleAsync(a => a.Id == firstId)).Status);
            Assert.Equal("InProgress", (await firstExpiry.Attempts.SingleAsync(a => a.Id == secondId)).Status);
        }
        clock.Set(origin.AddMinutes(24).AddSeconds(30));
        await using var final = NewContext();
        Assert.Equal(1, await Service(final).ExpireAsync());
        Assert.Equal(2, await final.ExamResults.CountAsync());
    }
}
