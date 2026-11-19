using Exam.Data;
using Exam.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task AccountCategoriesExcludeAdministratorsBeforeCountingAndPaging()
    {
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var roles = provider.GetRequiredService<RoleManager<AppRole>>();
        Assert.True((await roles.CreateAsync(new() { Name = "Administrator" })).Succeeded);
        var admin = new AppUser { UserName = "admin", FullName = "Admin" };
        var teacher = new AppUser { UserName = "teacher", FullName = "Teacher" };
        var student = new AppUser { UserName = "student", FullName = "Student", StudentCode = "SV001" };
        foreach (var user in new[] { admin, teacher, student })
            Assert.True((await users.CreateAsync(user)).Succeeded);
        Assert.True((await users.AddToRolesAsync(admin, ["Administrator", "Lecturer"])).Succeeded);
        Assert.True((await users.AddToRoleAsync(teacher, "Lecturer")).Succeeded);
        Assert.True((await users.AddToRoleAsync(student, "Student")).Succeeded);
        var service = new AdminService(Db, users, TimeProvider.System);

        foreach (var (role, expectedId) in new[] { ("Lecturer", teacher.Id), ("Student", student.Id) })
        {
            var page = await service.ListAsync(admin.Id, "Administrator", "users", null, 1, 1, accountRole: role);
            Assert.Equal(1, page.Total);
            Assert.Equal(expectedId, Assert.Single(page.Items)["id"]);
        }
        var all = await service.ListAsync(admin.Id, "Administrator", "users", null, 1, 25);
        Assert.Equal(2, all.Total);
        Assert.DoesNotContain(all.Items, row => Equals(row["id"], admin.Id));
    }
}
