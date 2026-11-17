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
    public async Task ProfileUpdatesOnlyContactFieldsAndRejectsStaleVersions()
    {
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var self = new AppUser { UserName = "profile-self", FullName = "Original", StudentCode = "PROFILE01",
            Email = "old@example.com", EmailConfirmed = true, PhoneNumber = "0901234567", PhoneNumberConfirmed = true };
        var other = new AppUser { UserName = "profile-other", FullName = "Other" };
        Assert.True((await users.CreateAsync(self)).Succeeded);
        Assert.True((await users.CreateAsync(other)).Succeeded);
        Assert.True((await users.AddToRoleAsync(self, "Student")).Succeeded);
        var profiles = new ProfileService(Db, users);
        var before = await profiles.GetAsync(self.Id);
        var updated = await profiles.UpdateAsync(self.Id, new(" Updated ", "new@example.com", "0912345678", before.Version));
        Assert.Equal("Updated", updated.FullName);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("0912345678", updated.PhoneNumber);
        Assert.Equal("PROFILE01", updated.StudentCode);
        Assert.Equal("profile-self", updated.UserName);
        Assert.Equal(new[] { "Student" }, updated.Roles);
        Assert.Equal("Other", (await profiles.GetAsync(other.Id)).FullName);
        Assert.False(self.EmailConfirmed);
        Assert.False(self.PhoneNumberConfirmed);
        Db.ChangeTracker.Clear();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() =>
            profiles.UpdateAsync(self.Id, new("Stale", null, null, before.Version)))).Status);
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() =>
            profiles.UpdateAsync(self.Id, new("Invalid", "bad-email", "letters", updated.Version)))).Status);
        Assert.Equal("Updated", (await profiles.GetAsync(self.Id)).FullName);
    }
}
