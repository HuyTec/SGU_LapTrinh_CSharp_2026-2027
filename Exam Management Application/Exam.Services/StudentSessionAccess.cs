using Exam.Data;

namespace Exam.Services;

/// <summary>Shared student access rule for session lists, details and starting attempts.</summary>
internal static class StudentSessionAccess
{
    public static IQueryable<ExamSession> Assigned(ExamDbContext db, int studentId) =>
        db.ExamSessions.Where(session =>
            db.Attempts.Any(a => a.SessionId == session.Id && a.StudentId == studentId) ||
            (session.ActivatedAt != null && session.Classes.Any() &&
                session.Candidates.Any(candidate => candidate.StudentId == studentId)) ||
            (session.ActivatedAt == null && session.Classes.Any(link => link.Class.IsActive &&
                link.Class.SubjectId == session.Exam.SubjectId &&
                link.Class.Students.Any(member => member.StudentId == studentId))));
}
