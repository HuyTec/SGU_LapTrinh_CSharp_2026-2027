using System.Data.Common;
using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task ResourceListsReadOnlyTheirOwnDataAndDenyUnauthorizedResourcesWithoutQueries()
    {
        var lecturer = new AppUser { UserName = "query-lecturer", FullName = "Lecturer" };
        var subject = new Subject { Code = "QUERY", Name = "Query subject" };
        Db.AddRange(lecturer, subject);
        await Db.SaveChangesAsync();
        Db.LecturerSubjects.Add(new() { LecturerId = lecturer.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        var counter = new AdminCommandCounter();
        await using var observed = CreateObservedAdminProvider(counter);
        var admin = new AdminService(observed.GetRequiredService<ExamDbContext>(),
            observed.GetRequiredService<UserManager<AppUser>>(), TimeProvider.System);

        Assert.Single((await admin.ListAsync(lecturer.Id, "Administrator", "subjects", null, 1, 25)).Items);
        Assert.Single(counter.Commands);
        Assert.Contains("[Subjects]", counter.Commands[0]);
        counter.Commands.Clear();
        await admin.ListAsync(lecturer.Id, "Administrator", "classes", null, 1, 25);
        Assert.Single(counter.Commands);
        Assert.DoesNotContain(counter.Commands, sql => sql.Contains("[Users]") || sql.Contains("[Roles]") || sql.Contains("[Topics]"));
        counter.Commands.Clear();
        await admin.ListAsync(lecturer.Id, "Lecturer", "questions", null, 1, 25);
        Assert.DoesNotContain(counter.Commands, sql => sql.Contains("[Users]") || sql.Contains("[UserRoles]"));

        counter.Commands.Clear();
        foreach (var (role, resource) in new[] { ("Student", "subjects"), ("Lecturer", "users"),
            ("Lecturer", "classes"), ("Administrator", "questions"), ("Administrator", "monitor"), ("Administrator", "unknown") })
            Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
                admin.ListAsync(lecturer.Id, role, resource, null, 1, 25))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.SaveAsync(lecturer.Id, "Lecturer", "monitor", null, new(new())))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.DeleteAsync(lecturer.Id, "Lecturer", "monitor", 1))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() =>
            admin.CatalogAsync(lecturer.Id, "Student"))).Status);
        Assert.Empty(counter.Commands);
    }

    [SqlFact]
    public async Task CatalogAndAccountListQueryCountsDoNotGrowWithTheNumberOfAccounts()
    {
        var teacher = new AppUser { UserName = "catalog-teacher", FullName = "Teacher" };
        var student = new AppUser { UserName = "catalog-student", FullName = "Student", StudentCode = "CAT001" };
        var inactive = new AppUser { UserName = "catalog-inactive", FullName = "Inactive", IsActive = false };
        Db.Users.AddRange(teacher, student, inactive);
        await Db.SaveChangesAsync();
        var lecturerRole = await Db.Roles.SingleAsync(r => r.NormalizedName == "LECTURER");
        var studentRole = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.AddRange(new() { UserId = teacher.Id, RoleId = lecturerRole.Id },
            new() { UserId = inactive.Id, RoleId = lecturerRole.Id }, new() { UserId = student.Id, RoleId = studentRole.Id });
        await Db.SaveChangesAsync();
        var counter = new AdminCommandCounter();
        await using var observed = CreateObservedAdminProvider(counter);
        var admin = new AdminService(observed.GetRequiredService<ExamDbContext>(),
            observed.GetRequiredService<UserManager<AppUser>>(), TimeProvider.System);

        var initial = await admin.CatalogAsync(teacher.Id, "Administrator");
        Assert.Equal(teacher.Id.ToString(), Assert.Single(initial.Resources.Single(r => r.Key == "assignments")
            .Fields.Single(f => f.Name == "lecturerId").Choices!).Value);
        var adminQueries = counter.Commands.Count;
        Assert.DoesNotContain(counter.Commands, sql => sql.Contains("[Topics]") || sql.Contains("[Questions]") || sql.Contains("[Exams]"));
        counter.Commands.Clear();
        await admin.CatalogAsync(teacher.Id, "Lecturer");
        var lecturerQueries = counter.Commands.Count;
        counter.Commands.Clear();
        await admin.ListAsync(teacher.Id, "Administrator", "users", null, 1, 200);
        var accountQueries = counter.Commands.Count;

        var extra = Enumerable.Range(1, 80).Select(i => new AppUser
            { UserName = $"catalog-extra-{i}", FullName = $"Extra {i}", StudentCode = $"EXTRA{i}" }).ToArray();
        Db.Users.AddRange(extra);
        await Db.SaveChangesAsync();
        Db.UserRoles.AddRange(extra.Select((u, i) => new IdentityUserRole<int>
            { UserId = u.Id, RoleId = i % 2 == 0 ? lecturerRole.Id : studentRole.Id }));
        Db.UserRoles.Add(new() { UserId = teacher.Id, RoleId = studentRole.Id });
        await Db.SaveChangesAsync();

        counter.Commands.Clear();
        var expanded = await admin.CatalogAsync(teacher.Id, "Administrator");
        Assert.Equal(adminQueries, counter.Commands.Count);
        Assert.Equal(41, expanded.Resources.Single(r => r.Key == "assignments").Fields.Single(f => f.Name == "lecturerId").Choices!.Count);
        counter.Commands.Clear();
        var lecturerCatalog = await admin.CatalogAsync(teacher.Id, "Lecturer");
        Assert.Equal(lecturerQueries, counter.Commands.Count);
        Assert.Equal(42, lecturerCatalog.Resources.Single(r => r.Key == "candidates").Fields.Single(f => f.Name == "studentId").Choices!.Count);
        counter.Commands.Clear();
        var accounts = await admin.ListAsync(teacher.Id, "Administrator", "users", null, 1, 200);
        Assert.Equal(accountQueries, counter.Commands.Count);
        Assert.Equal(2, accountQueries);
        var teacherRow = Assert.Single(accounts.Items, row => Equals(row["id"], teacher.Id));
        Assert.Equal(new[] { "Lecturer", "Student" }, Assert.IsType<string[]>(teacherRow["roles"]));
        Assert.Equal("Lecturer, Student", teacherRow["role"]);
    }

    private ServiceProvider CreateObservedAdminProvider(AdminCommandCounter counter)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ExamDbContext>(o => o.UseSqlServer(Db.Database.GetConnectionString()).AddInterceptors(counter));
        services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<ExamDbContext>();
        return services.BuildServiceProvider();
    }

    private sealed class AdminCommandCounter : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
