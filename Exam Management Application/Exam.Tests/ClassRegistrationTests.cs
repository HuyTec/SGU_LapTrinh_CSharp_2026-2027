using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests : IAsyncLifetime
{
    private ServiceProvider provider = null!;
    private string database = "";
    private ExamDbContext Db => provider.GetRequiredService<ExamDbContext>();

    public async Task InitializeAsync()
    {
        database = "ExamAppTests_" + Guid.NewGuid().ToString("N");
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EXAM_TEST_CONNECTION")) { InitialCatalog = database };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddDbContext<ExamDbContext>(o => o.UseSqlServer(connection.ConnectionString));
        services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<ExamDbContext>();
        provider = services.BuildServiceProvider();
        await Db.GetService<IMigrator>().MigrateAsync("20261113090207_StudentClassAdmission");
        foreach (var role in new[] { "Lecturer", "Student" })
            Assert.True((await provider.GetRequiredService<RoleManager<AppRole>>().CreateAsync(new() { Name = role })).Succeeded);
    }

    public async Task DisposeAsync()
    {
        if (provider is null) return;
        Assert.Equal(database, Db.Database.GetDbConnection().Database);
        Assert.StartsWith("ExamAppTests_", database);
        await Db.Database.EnsureDeletedAsync();
        await provider.DisposeAsync();
    }

    [SqlFact]
    public async Task LecturerCreatesOnlyWithinAssignedSubjectAndStudentSeesOwnMembership()
    {
        var teacher = new AppUser { UserName = "teacher", FullName = "Teacher" };
        var student = new AppUser { UserName = "student", FullName = "Student" };
        var subject = new Subject { Code = "TEST", Name = "Test" };
        Db.AddRange(teacher, student, subject);
        await Db.SaveChangesAsync();
        var service = new LecturerClassService(Db);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(teacher.Id, subject.Id, "A"))).Status);
        Db.Add(new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        await service.CreateAsync(teacher.Id, subject.Id, "A");
        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(teacher.Id, subject.Id, "A"));
        var created = await Db.Set<ExamClass>().SingleAsync();
        Db.Add(new ClassStudent { ClassId = created.Id, StudentId = student.Id });
        await Db.SaveChangesAsync();
        Assert.Single(await new StudentClassService(Db).ListAsync(student.Id));
        Assert.Empty(await new StudentClassService(Db).ListAsync(teacher.Id));
    }

    [SqlFact]
    public async Task RegistrationPersistsPhoneAndKeepsLecturerPendingApproval()
    {
        var auth = new AuthService(Db, provider.GetRequiredService<UserManager<AppUser>>(), TimeProvider.System);
        await auth.RegisterAsync(new("lecturer", "Lecturer", null, "lecturer@example.com", "Example1!", "Lecturer", "0901234567"));
        var user = await Db.Users.SingleAsync();
        Assert.Equal("0901234567", user.PhoneNumber);
        Assert.Equal("Lecturer", user.RequestedRole);
        Assert.False(user.IsActive);
        Assert.Empty(await Db.UserRoles.ToListAsync());
        await Assert.ThrowsAsync<BusinessException>(() => auth.RegisterAsync(new("bad", "Bad", "X", "bad@example.com", "Example1!", "Student", "invalid")));
    }

    [SqlFact]
    public async Task LegacyRoleMigrationPreservesUserAndReassignsMembership()
    {
        var migrator = Db.GetService<IMigrator>();

        var role = new AppRole { Name = "ExamOfficer", NormalizedName = "EXAMOFFICER" };
        var user = new AppUser { UserName = "legacy", FullName = "Legacy" };
        Db.AddRange(role, user);
        await Db.SaveChangesAsync();
        Db.UserRoles.Add(new() { UserId = user.Id, RoleId = role.Id });
        Db.AuthSessions.Add(new() { Id = Guid.NewGuid(), UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddHours(1) });
        await Db.SaveChangesAsync();
        await migrator.MigrateAsync();
        Db.ChangeTracker.Clear();
        Assert.False(await Db.Roles.AnyAsync(r => r.Name == "ExamOfficer"));
        var lecturer = await Db.Roles.SingleAsync(r => r.Name == "Lecturer");
        Assert.True(await Db.UserRoles.AnyAsync(r => r.UserId == user.Id && r.RoleId == lecturer.Id));
        Assert.True((await Db.AuthSessions.SingleAsync()).Revoked);
        Assert.True(await Db.Users.AnyAsync(u => u.Id == user.Id));
    }
}
