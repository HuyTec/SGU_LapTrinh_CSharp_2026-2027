using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Exam.Data.Migrations;

[DbContext(typeof(ExamDbContext))]
[Migration("20261118120000_PersonalAttemptDeadlines")]
public sealed class PersonalAttemptDeadlines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE a SET Deadline = DATEADD(minute, s.DurationMinutes, a.StartedAt)
            FROM Attempts a JOIN ExamSessions s ON s.Id = a.SessionId
            WHERE a.Status = N'InProgress' AND a.Deadline = s.ClosesAt
              AND a.StartedAt < s.ClosesAt
              AND a.Deadline < DATEADD(minute, s.DurationMinutes, a.StartedAt)
              AND NOT EXISTS (SELECT 1 FROM ExamResults r WHERE r.AttemptId = a.Id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep already-issued personal deadlines when reverting this data-only migration.
    }
}
