using Exam.Core;
using Exam.Data;
using Exam.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task ClassDetailRequiresMembershipAndHidesDraftAndWrongSubjectSessions()
    {
        var teacher = new AppUser { UserName = "detail-teacher", FullName = "Detail teacher" };
        var student = new AppUser { UserName = "detail-student", FullName = "Detail student" };
        var outsider = new AppUser { UserName = "detail-outsider", FullName = "Outsider" };
        var subject = new Subject { Code = "DETAIL", Name = "Subject of class" };
        var otherSubject = new Subject { Code = "OTHER", Name = "Other subject" };
        Db.AddRange(teacher, student, outsider, subject, otherSubject);
        await Db.SaveChangesAsync();
        var exam = new ExamPaper { SubjectId = subject.Id, Creator = teacher, Title = "Class exam", Status = "Ready" };
        var wrongExam = new ExamPaper { SubjectId = otherSubject.Id, Creator = teacher, Title = "Wrong subject", Status = "Ready" };
        var examClass = new ExamClass { SubjectId = subject.Id, Code = "DETAIL-A", Students = [new() { StudentId = student.Id }] };
        Db.AddRange(exam, wrongExam, examClass, new LecturerSubject { LecturerId = teacher.Id, SubjectId = subject.Id });
        await Db.SaveChangesAsync();
        var now = DateTime.UtcNow;
        ExamSession Session(string title, ExamPaper paper, string status) => new()
        {
            Exam = paper, Manager = teacher, Title = title, Status = status,
            OpensAt = now.AddMinutes(-1), ClosesAt = now.AddHours(1), DurationMinutes = 30,
            Classes = [new() { ClassId = examClass.Id }]
        };
        var visible = Session("Visible", exam, "Active");
        Db.AddRange(visible, Session("Draft hidden", exam, "Draft"), Session("Wrong subject hidden", wrongExam, "Active"));
        await Db.SaveChangesAsync();
        var service = new StudentClassService(Db);

        var detail = await service.DetailAsync(student.Id, examClass.Id);
        Assert.Equal(examClass.Id, detail.Class.Id);
        Assert.Equal("Subject of class", detail.Class.Subject);
        Assert.Equal("Detail teacher", Assert.Single(detail.Lecturers));
        Assert.Equal(visible.Id, Assert.Single(detail.Sessions).Id);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => service.DetailAsync(outsider.Id, examClass.Id))).Status);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => service.DetailAsync(student.Id, int.MaxValue))).Status);

        examClass.IsActive = false;
        await Db.SaveChangesAsync();
        Assert.Empty((await service.DetailAsync(student.Id, examClass.Id)).Sessions);
        Db.Attempts.Add(new Attempt
        {
            SessionId = visible.Id, StudentId = student.Id, StartedAt = now, Deadline = now.AddMinutes(30)
        });
        await Db.SaveChangesAsync();
        Assert.Equal(visible.Id, Assert.Single((await service.DetailAsync(student.Id, examClass.Id)).Sessions).Id);
        Assert.False(Assert.Single(await service.ListAsync(student.Id)).IsActive);
    }
}
