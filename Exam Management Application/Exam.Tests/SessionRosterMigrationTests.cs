using System.Text.RegularExpressions;
using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task RosterMigrationBackfillsEligibleStudentsWithoutRepeatingOrReplacingRows()
    {
        const string previous = "20261118130000_ClassNames";
        var teacher = new AppUser { UserName = "migration-teacher", FullName = "Teacher" };
        var existing = new AppUser { UserName = "migration-existing", FullName = "Existing Student" };
        var eligible = new AppUser { UserName = "migration-eligible", FullName = "Eligible Student" };
        var inactive = new AppUser { UserName = "migration-inactive", FullName = "Inactive Student", IsActive = false };
        var excluded = new AppUser { UserName = "migration-excluded", FullName = "Excluded Student" };
        var subject = new Subject { Code = "ROSTER", Name = "Roster subject" };
        var otherSubject = new Subject { Code = "OTHER", Name = "Other subject" };
        var paper = new ExamPaper { Creator = teacher, Subject = subject, Title = "Paper", Status = "Ready" };
        Db.AddRange(paper, otherSubject, existing, eligible, inactive, excluded);
        await Db.SaveChangesAsync();
        var studentRole = await Db.Roles.SingleAsync(role => role.NormalizedName == "STUDENT");
        Db.UserRoles.AddRange(new[] { existing, eligible, inactive, excluded }
            .Select(student => new IdentityUserRole<int> { UserId = student.Id, RoleId = studentRole.Id }));

        ExamClass Class(string code, Subject owner, bool active, params AppUser[] people) => new()
        {
            Code = code, SubjectId = owner.Id, IsActive = active,
            Students = people.Select(u => new ClassStudent { StudentId = u.Id }).ToList()
        };
        var first = Class("FIRST", subject, true, existing, eligible, inactive, teacher);
        var second = Class("SECOND", subject, true, eligible);
        var stopped = Class("STOPPED", subject, false, excluded);
        var wrongSubject = Class("WRONG", otherSubject, true, excluded);
        var now = new DateTime(2026, 11, 20, 7, 0, 0, DateTimeKind.Utc);
        ExamSession Session(string title, string status, params ExamClass[] classes) => new()
        {
            Exam = paper, Manager = teacher, Title = title, Status = status,
            OpensAt = now, ClosesAt = now.AddMinutes(30),
            Classes = classes.Select(c => new SessionClass { Class = c }).ToList()
        };
        var active = Session("Active", "Active", first, second, stopped, wrongSubject);
        var legacy = Session("Roster only", "Active");
        var draft = Session("Draft", "Draft", first);
        var cancelled = Session("Cancelled", "Cancelled", first);
        var kept = new SessionCandidate { Session = active, StudentId = existing.Id };
        Db.AddRange(kept, new SessionCandidate { Session = legacy, StudentId = excluded.Id }, draft, cancelled);
        await Db.SaveChangesAsync();

        var migrator = Db.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        Db.ChangeTracker.Clear();
        var script = migrator.GenerateScript(previous, options: MigrationsSqlGenerationOptions.Idempotent);
        var batches = Regex.Split(script, @"(?im)^[ \t]*GO[ \t]*\r?$").Where(batch => !string.IsNullOrWhiteSpace(batch)).ToArray();
        await Db.Database.OpenConnectionAsync();
        try
        {
            for (var run = 0; run < 2; run++)
                foreach (var batch in batches)
                    await Db.Database.ExecuteSqlRawAsync(batch);
        }
        finally
        {
            await Db.Database.CloseConnectionAsync();
        }

        var roster = await Db.SessionCandidates.AsNoTracking().Where(c => c.SessionId == active.Id).ToListAsync();
        Assert.Equal(new[] { existing.Id, eligible.Id }.Order(), roster.Select(c => c.StudentId).Order());
        Assert.Equal(kept.Id, roster.Single(c => c.StudentId == existing.Id).Id);
        Assert.Equal(excluded.Id, (await Db.SessionCandidates.SingleAsync(c => c.SessionId == legacy.Id)).StudentId);
        Assert.False(await Db.SessionCandidates.AnyAsync(c => c.SessionId == draft.Id || c.SessionId == cancelled.Id));
        var stamps = await Db.ExamSessions.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.ActivatedAt);
        Assert.NotNull(stamps[active.Id]);
        Assert.Null(stamps[legacy.Id]);
        Assert.Null(stamps[draft.Id]);
        Assert.Null(stamps[cancelled.Id]);
    }
}
