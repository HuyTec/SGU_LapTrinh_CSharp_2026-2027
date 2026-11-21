using Exam.Data;

namespace Exam.Services;

internal static class LecturerSessionAccess
{
    public static IQueryable<ExamSession> Unfinished(ExamDbContext db, DateTime now) =>
        db.ExamSessions.Where(s => s.Status == "Draft" || s.Status == "Active" &&
            (s.ClosesAt > now && (s.AdmissionsClosedAt == null || s.AdmissionsClosedAt > now)
             || db.Attempts.Any(a => a.SessionId == s.Id && a.Status == "InProgress" && a.Deadline > now)));

    public static IQueryable<ExamSession> Readable(ExamDbContext db, int lecturer, DateTime now) =>
        db.ExamSessions.Where(s => s.ManagedBy == lecturer &&
            (db.LecturerSubjects.Any(a => a.LecturerId == lecturer && a.SubjectId == s.Exam.SubjectId)
             || !Unfinished(db, now).Any(pending => pending.Id == s.Id)));
}
