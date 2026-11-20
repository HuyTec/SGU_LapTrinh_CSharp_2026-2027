using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    private async Task<(int Teacher, int Outsider, int Student, int Class)> PrepareManagedClassAsync()
    {
        var teacher = new AppUser { UserName = "class-teacher", FullName = "Teacher" };
        var outsider = new AppUser { UserName = "other-teacher", FullName = "Other" };
        var student = new AppUser { UserName = "class-student", FullName = "Student", StudentCode = "MEMBER01" };
        var subject = new Subject { Code = "CLASS", Name = "Class Subject" };
        var item = new ExamClass { Code = "CLASS01", SubjectId = 0 };
        Db.AddRange(teacher, outsider, student, subject);
        await Db.SaveChangesAsync();
        item.SubjectId = subject.Id;
        Db.Add(item);
        Db.LecturerSubjects.Add(new() { LecturerId = teacher.Id, SubjectId = subject.Id });
        var studentRole = await Db.Roles.SingleAsync(r => r.NormalizedName == "STUDENT");
        Db.UserRoles.Add(new() { UserId = student.Id, RoleId = studentRole.Id });
        await Db.SaveChangesAsync();
        return (teacher.Id, outsider.Id, student.Id, item.Id);
    }

    [SqlFact]
    public async Task LecturerClassPageAndMembershipRespectSubjectScope()
    {
        var data = await PrepareManagedClassAsync();
        var service = new LecturerClassService(Db);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() => service.PageAsync(data.Outsider, data.Class))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessException>(() => service.AddAsync(data.Outsider, data.Class, "MEMBER01"))).Status);
        await service.AddAsync(data.Teacher, data.Class, "MEMBER01");
        var page = await service.PageAsync(data.Teacher, data.Class);
        Assert.Single(page.Members);
        Assert.Equal(data.Student, page.Members[0].Id);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => service.AddAsync(data.Teacher, data.Class, "MEMBER01"))).Status);
        await service.RemoveAsync(data.Teacher, data.Class, data.Student);
        Assert.Empty((await service.PageAsync(data.Teacher, data.Class)).Members);
    }

    [SqlFact]
    public async Task LecturerEnrollmentAllowsOverlappingActivatedSessionsAndRemovalPreservesAttempts()
    {
        var data = await PrepareManagedClassAsync();
        var item = await Db.Set<ExamClass>().SingleAsync();
        var otherClass = new ExamClass
        {
            Code = "OTHER", SubjectId = item.SubjectId,
            Students = [new() { StudentId = data.Student }]
        };
        var paper = new ExamPaper { SubjectId = item.SubjectId, CreatedBy = data.Teacher, Title = "Ready", Status = "Ready" };
        var now = DateTime.UtcNow;
        var existing = new ExamSession
        {
            Exam = paper, ManagedBy = data.Teacher, Title = "Existing", Status = "Active",
            OpensAt = now.AddMinutes(1), ClosesAt = now.AddMinutes(31), DurationMinutes = 20,
            Classes = [new() { Class = otherClass }]
        };
        var target = new ExamSession
        {
            Exam = paper, ManagedBy = data.Teacher, Title = "Target", Status = "Active",
            OpensAt = now.AddMinutes(2), ClosesAt = now.AddMinutes(32), DurationMinutes = 20,
            Classes = [new() { ClassId = data.Class }]
        };
        Db.AddRange(existing, target);
        await Db.SaveChangesAsync();
        var service = new LecturerClassService(Db);
        await service.AddAsync(data.Teacher, data.Class, "MEMBER01");
        Assert.True(await Db.Set<ClassStudent>().AnyAsync(m => m.ClassId == data.Class && m.StudentId == data.Student));
        var attempt = new Attempt
        {
            SessionId = target.Id, StudentId = data.Student, StartedAt = now,
            Deadline = now.AddMinutes(20), Status = "InProgress"
        };
        Db.Add(attempt);
        await Db.SaveChangesAsync();
        await service.RemoveAsync(data.Teacher, data.Class, data.Student);
        Assert.True(await Db.Attempts.AnyAsync(a => a.Id == attempt.Id));
        Assert.False(await Db.Set<ClassStudent>().AnyAsync(m => m.ClassId == data.Class && m.StudentId == data.Student));
    }
}
