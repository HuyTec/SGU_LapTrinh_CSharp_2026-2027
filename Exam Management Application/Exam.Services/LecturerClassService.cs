using System.Data;
using Exam.Core;
using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Exam.Services;

public sealed class LecturerClassService(ExamDbContext db, UserManager<AppUser> users)
{
    public sealed record ClassRow(int Id, string Code, string Subject, bool Active);
    public sealed record Member(int Id, string? Code, string Name, bool Active);
    public sealed record Page(IReadOnlyList<ClassRow> Classes, ClassRow? Selected, IReadOnlyList<Member> Members);

    private IQueryable<ClassRow> Assigned(int lecturer) =>
        from c in db.Set<ExamClass>()
        join s in db.Subjects on c.SubjectId equals s.Id
        where db.LecturerSubjects.Any(a => a.LecturerId == lecturer && a.SubjectId == s.Id)
        select new ClassRow(c.Id, c.Code, s.Name, c.IsActive && s.IsActive);

    public async Task<Page> ListAsync(int lecturer, int? id)
    {
        var classes = await Assigned(lecturer).ToListAsync();
        var selected = id.HasValue ? classes.SingleOrDefault(c => c.Id == id)
            ?? throw new BusinessException("Bạn không có quyền quản lý lớp này.", 403) : null;
        var members = selected is null ? [] : await (
            from m in db.Set<ClassStudent>()
            join u in db.Users on m.StudentId equals u.Id
            where m.ClassId == id
            orderby u.FullName, u.Id
            select new Member(u.Id, u.StudentCode, u.FullName, u.IsActive)).ToListAsync();
        return new(classes.OrderBy(c => c.Subject).ThenBy(c => c.Code).ToList(), selected, members);
    }

    public Task AddAsync(int lecturer, int id, string? code) => WriteAsync(lecturer, id, async c =>
    {
        if (!c.Active)
            throw new BusinessException("Lớp hoặc môn học đã ngừng hoạt động.", 409);
        code = code?.Trim();
        if (string.IsNullOrEmpty(code) || code.Length > 30)
            throw new BusinessException("Nhập mã thí sinh từ 1 đến 30 ký tự.");
        var student = await db.Users.SingleOrDefaultAsync(u => u.StudentCode == code);
        if (student is null || !student.IsActive || !await users.IsInRoleAsync(student, "Student"))
            throw new BusinessException("Không tìm thấy thí sinh đang hoạt động theo mã này.");
        if (await db.Set<ClassStudent>().AnyAsync(m => m.ClassId == id && m.StudentId == student.Id))
            throw new BusinessException("Thí sinh đã thuộc lớp này.", 409);

        db.Add(new ClassStudent { ClassId = id, StudentId = student.Id });
        await db.SaveChangesAsync();
    });

    public Task RemoveAsync(int lecturer, int id, int studentId) => WriteAsync(lecturer, id, async _ =>
    {
        await db.Set<ClassStudent>().Where(m => m.ClassId == id && m.StudentId == studentId).ExecuteDeleteAsync();
    });

    private Task WriteAsync(int lecturer, int id, Func<ClassRow, Task> action) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var c = await Assigned(lecturer).SingleOrDefaultAsync(c => c.Id == id)
                ?? throw new BusinessException("Bạn không có quyền quản lý lớp này.", 403);
            await action(c);
            await transaction.CommitAsync();
        });
}
