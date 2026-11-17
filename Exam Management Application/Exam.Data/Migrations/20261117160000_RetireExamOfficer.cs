using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Exam.Data.Migrations;

[DbContext(typeof(ExamDbContext))]
[Migration("20261117160000_RetireExamOfficer")]
public sealed class RetireExamOfficer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @old int = (SELECT RoleID FROM Roles WHERE NormalizedName = N'EXAMOFFICER');
            IF @old IS NOT NULL OR EXISTS (SELECT 1 FROM Users WHERE RequestedRole = N'ExamOfficer')
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM Roles WHERE NormalizedName = N'LECTURER')
                    INSERT Roles (Name, NormalizedName, ConcurrencyStamp) VALUES (N'Lecturer', N'LECTURER', CONVERT(nvarchar(36), NEWID()));
                DECLARE @lecturer int = (SELECT RoleID FROM Roles WHERE NormalizedName = N'LECTURER');
                UPDATE AuthSessions SET Revoked = 1
                WHERE UserId IN (SELECT UserID FROM UserRoles WHERE RoleID = @old);
                INSERT UserRoles (UserID, RoleID)
                SELECT old.UserID, @lecturer FROM UserRoles old
                WHERE old.RoleID = @old AND NOT EXISTS
                    (SELECT 1 FROM UserRoles currentRole WHERE currentRole.UserID = old.UserID AND currentRole.RoleID = @lecturer);
                INSERT LecturerSubjects (LecturerId, SubjectId)
                SELECT DISTINCT s.ManagedBy, e.SubjectId FROM ExamSessions s JOIN Exams e ON e.Id = s.ExamId
                WHERE EXISTS (SELECT 1 FROM UserRoles ur WHERE ur.UserID = s.ManagedBy AND ur.RoleID = @old)
                  AND NOT EXISTS (SELECT 1 FROM LecturerSubjects ls WHERE ls.LecturerId = s.ManagedBy AND ls.SubjectId = e.SubjectId);
                UPDATE Users SET RequestedRole = N'Lecturer' WHERE RequestedRole = N'ExamOfficer';
                DELETE FROM UserRoles WHERE RoleID = @old;
                DELETE FROM AspNetRoleClaims WHERE RoleId = @old;
                DELETE FROM Roles WHERE RoleID = @old;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data-only consolidation has no schema to undo. Keep Lecturer memberships
        // when downgrading; only a pre-migration backup can restore the original roles.
    }
}
