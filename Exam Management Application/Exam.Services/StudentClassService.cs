using System.Data;
using Exam.Data;
using Microsoft.EntityFrameworkCore;

namespace Exam.Services;

public sealed class StudentClassService(ExamDbContext db)
{
    public const string DemoCode = "DEMO-NV10";
    public sealed record ClassRow(string Code, string Subject, bool IsActive);

    public async Task<IReadOnlyList<ClassRow>> ListAsync(int studentId)
    {
        return await db.Set<ExamClass>().AsNoTracking()
            .Where(c => c.Students.Any(s => s.StudentId == studentId))
            .Join(db.Subjects, c => c.SubjectId, s => s.Id, (c, s) => new { c.Code, Subject = s.Name, c.IsActive })
            .OrderBy(c => c.Subject).ThenBy(c => c.Code)
            .Select(c => new ClassRow(c.Code, c.Subject, c.IsActive)).ToListAsync();
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
