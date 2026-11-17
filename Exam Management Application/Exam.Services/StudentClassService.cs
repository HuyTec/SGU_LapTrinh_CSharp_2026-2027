using System.Data;
using Exam.Core;
using Exam.Data;
using Microsoft.EntityFrameworkCore;

namespace Exam.Services;

public sealed class StudentClassService(ExamDbContext db)
{
    public const string DemoCode = "DEMO-NV10";
    public sealed record ClassRow(int Id, string Code, string Subject, bool IsActive);
    public sealed record ClassSession(int Id, string Title, DateTime OpensAt, DateTime ClosesAt);
    public sealed record ClassDetail(ClassRow Class, IReadOnlyList<string> Lecturers, IReadOnlyList<ClassSession> Sessions);

    public async Task<IReadOnlyList<ClassRow>> ListAsync(int studentId)
    {
        return await db.Set<ExamClass>().AsNoTracking()
            .Where(c => c.Students.Any(s => s.StudentId == studentId))
            .Join(db.Subjects, c => c.SubjectId, s => s.Id, (c, s) => new { c.Id, c.Code, Subject = s.Name, IsActive = c.IsActive && s.IsActive })
            .OrderBy(c => c.Subject).ThenBy(c => c.Code)
            .Select(c => new ClassRow(c.Id, c.Code, c.Subject, c.IsActive)).ToListAsync();
    }

    public async Task<ClassDetail> DetailAsync(int studentId, int id)
    {
        var item = await db.Set<ExamClass>().AsNoTracking()
            .Where(c => c.Id == id && c.Students.Any(m => m.StudentId == studentId))
            .Join(db.Subjects, c => c.SubjectId, s => s.Id,
                (c, s) => new { c.Id, c.Code, c.SubjectId, Subject = s.Name, Active = c.IsActive && s.IsActive })
            .SingleOrDefaultAsync() ?? throw new BusinessException("Không tìm thấy lớp của bạn.", 404);
        var lecturers = await db.LecturerSubjects.Where(a => a.SubjectId == item.SubjectId && a.Lecturer.IsActive)
            .OrderBy(a => a.Lecturer.FullName).Select(a => a.Lecturer.FullName).ToListAsync();
        var sessions = await db.ExamSessions.AsNoTracking()
            .Where(s => s.Status != "Draft" && s.Exam.SubjectId == item.SubjectId && s.Classes.Any(c => c.ClassId == id) &&
                (item.Active || db.Attempts.Any(a => a.SessionId == s.Id && a.StudentId == studentId)))
            .OrderByDescending(s => s.OpensAt)
            .Select(s => new ClassSession(s.Id, s.Title, s.OpensAt, s.ClosesAt)).ToListAsync();
        return new(new(item.Id, item.Code, item.Subject, item.Active), lecturers, sessions);
    }

    public async Task<bool> JoinDemoAsync(int studentId)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var classId = await db.Set<ExamClass>()
                .Where(c => c.IsActive && c.Code == DemoCode &&
                    db.Subjects.Any(s => s.Id == c.SubjectId && s.Code == "DEMO-NV" && s.IsActive))
                .Select(c => (int?)c.Id).SingleOrDefaultAsync();
            if (classId is null)
                return false;

            if (!await db.Set<ClassStudent>().AnyAsync(s => s.ClassId == classId.Value && s.StudentId == studentId))
            {
                db.Add(new ClassStudent { ClassId = classId.Value, StudentId = studentId });
                await db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return true;
        });
    }
}
