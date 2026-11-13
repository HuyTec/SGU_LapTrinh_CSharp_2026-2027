using System.ComponentModel.DataAnnotations;
using Exam.Web.ViewModels;
using Xunit;

namespace Exam.Tests;

public sealed class RegistrationValidationTests
{
    [Theory]
    [InlineData("Administrator", null, nameof(RegisterVm.AccountType))]
    [InlineData("ExamOfficer", null, nameof(RegisterVm.AccountType))]
    [InlineData("Student", null, nameof(RegisterVm.StudentCode))]
    [InlineData("Student", "SV001", null)]
    [InlineData("Lecturer", null, null)]
    public void ValidatesPublicRegistration(string accountType, string? studentCode, string? invalidField)
    {
        var request = new RegisterVm
        {
            AccountType = accountType,
            StudentCode = studentCode,
            UserName = "test-user",
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Example1!",
            ConfirmPassword = "Example1!"
        };
        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        Assert.Equal(invalidField is null, valid);
        if (invalidField is not null)
            Assert.Contains(errors, error => error.MemberNames.Contains(invalidField));
    }
}
