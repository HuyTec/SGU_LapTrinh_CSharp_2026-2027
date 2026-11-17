using System.Data;
using Exam.Core;
using Exam.Data;
using Microsoft.EntityFrameworkCore;

namespace Exam.Services;

public sealed class LecturerClassService(ExamDbContext db)
{
    public sealed record ClassRow(int Id, string Code, string Subject, bool IsActive);
    public sealed record Member(int Id, string? Code, string Name, bool IsActive);
    public sealed record Page(IReadOnlyList<ChoiceDto> Subjects, IReadOnlyList<ClassRow> Classes,
        ClassRow? Selected, IReadOnlyList<Member> Members);

    public Task<List<ChoiceDto>> SubjectsAsync(int lecturer) =>
        db.LecturerSubjects.Where(a => a.LecturerId == lecturer && a.Subject.IsActive)
            .Select(a => new ChoiceDto(a.SubjectId.ToString(), a.Subject.Name)).ToListAsync();

    public Task<List<ClassRow>> ListAsync(int lecturer) =>
        db.Set<ExamClass>().Where(c => db.LecturerSubjects.Any(a => a.LecturerId == lecturer && a.SubjectId == c.SubjectId))
            .Join(db.Subjects, c => c.SubjectId, s => s.Id,
                (c, s) => new { c.Id, c.Code, Subject = s.Name, Active = c.IsActive && s.IsActive })
            .OrderBy(c => c.Subject).ThenBy(c => c.Code)
            .Select(c => new ClassRow(c.Id, c.Code, c.Subject, c.Active)).ToListAsync();

    public async Task<Page> PageAsync(int lecturer, int? id)
    {
        var classes = await ListAsync(lecturer);
        var selected = id.HasValue ? classes.SingleOrDefault(c => c.Id == id)
            ?? throw new BusinessException("Bạn không có quyền quản lý lớp này.", 403) : null;
        var members = selected is null ? [] : await (
            from membership in db.Set<ClassStudent>()
            join student in db.Users on membership.StudentId equals student.Id
            where membership.ClassId == id
            orderby student.FullName, student.Id
            select new Member(student.Id, student.StudentCode, student.FullName, student.IsActive)).ToListAsync();
        return new(await SubjectsAsync(lecturer), classes, selected, members);
    }

    public Task AddAsync(int lecturer, int id, string? code) => ChangeMembershipAsync(lecturer, id, async item =>
    {
        if (!item.IsActive)
            throw new BusinessException("Lớp hoặc môn học đã ngừng hoạt động.", 409);
        code = code?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 30)
            throw new BusinessException("Nhập mã thí sinh từ 1 đến 30 ký tự.");
        var student = await db.Users.SingleOrDefaultAsync(u => u.StudentCode == code && u.IsActive);
        if (student is null || !await db.UserRoles.AnyAsync(link => link.UserId == student.Id &&
            db.Roles.Any(role => role.Id == link.RoleId && role.NormalizedName == "STUDENT")))
            throw new BusinessException("Không tìm thấy thí sinh đang hoạt động theo mã này.");
        if (await db.Set<ClassStudent>().AnyAsync(m => m.ClassId == id && m.StudentId == student.Id))
            throw new BusinessException("Thí sinh đã thuộc lớp này.", 409);

        var now = DateTime.UtcNow;
        var activeSessions = db.ExamSessions.Where(s => s.Status == "Active" && s.ClosesAt.AddMinutes(s.DurationMinutes) > now);
        var targetSessions = activeSessions.Where(s => s.Classes.Any(c => c.ClassId == id && c.Class.SubjectId == s.Exam.SubjectId));
        if (await targetSessions.AnyAsync(target => activeSessions.Any(other =>
            other.Id != target.Id && other.OpensAt < target.ClosesAt.AddMinutes(target.DurationMinutes) &&
            target.OpensAt < other.ClosesAt.AddMinutes(other.DurationMinutes) &&
            (other.Classes.Any(c => c.Class.IsActive && c.Class.SubjectId == other.Exam.SubjectId &&
                c.Class.Students.Any(m => m.StudentId == student.Id)) ||
             db.Attempts.Any(a => a.SessionId == other.Id && a.StudentId == student.Id && a.Status == "InProgress")))))
            throw new BusinessException("Thí sinh bị trùng lịch với đợt đã kích hoạt. Hãy kiểm tra lịch trước khi thêm vào lớp.", 409);
        db.Add(new ClassStudent { ClassId = id, StudentId = student.Id });
        await db.SaveChangesAsync();
    });

    public Task RemoveAsync(int lecturer, int id, int studentId) => ChangeMembershipAsync(lecturer, id, async _ =>
    {
        await db.Set<ClassStudent>().Where(m => m.ClassId == id && m.StudentId == studentId).ExecuteDeleteAsync();
    });

    private Task ChangeMembershipAsync(int lecturer, int id, Func<ClassRow, Task> change) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var item = (await ListAsync(lecturer)).SingleOrDefault(c => c.Id == id)
                ?? throw new BusinessException("Bạn không có quyền quản lý lớp này.", 403);
            await change(item);
            await transaction.CommitAsync();
        });

    public async Task CreateAsync(int lecturer, int subjectId, string? code)
    {
        code = code?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 50)
            throw new BusinessException("Nhập mã lớp từ 1 đến 50 ký tự.");
        if (!await db.LecturerSubjects.AnyAsync(a => a.LecturerId == lecturer && a.SubjectId == subjectId && a.Subject.IsActive))
            throw new BusinessException("Chọn môn đang hoạt động và được phân công.", 403);
        if (await db.Set<ExamClass>().AnyAsync(c => c.SubjectId == subjectId && c.Code == code))
            throw new BusinessException("Mã lớp đã tồn tại trong môn học.", 409);
        db.Add(new ExamClass { SubjectId = subjectId, Code = code, IsActive = true });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { throw new BusinessException("Không thể tạo lớp. Mã lớp có thể vừa được sử dụng hoặc môn học đã thay đổi.", 409); }
    }
}
