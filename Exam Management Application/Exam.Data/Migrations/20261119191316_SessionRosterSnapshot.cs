using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exam.Data.Migrations
{
    /// <inheritdoc />
    public partial class SessionRosterSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                EXEC(N'
                INSERT INTO [SessionCandidates] ([SessionId], [StudentId])
                SELECT DISTINCT s.[Id], cs.[StudentId]
                FROM [ExamSessions] s
                JOIN [Exams] e ON e.[Id] = s.[ExamId]
                JOIN [SessionClasses] sc ON sc.[SessionId] = s.[Id]
                JOIN [Classes] c ON c.[Id] = sc.[ClassId] AND c.[SubjectId] = e.[SubjectId]
                JOIN [ClassStudents] cs ON cs.[ClassId] = c.[Id]
                JOIN [Users] u ON u.[UserID] = cs.[StudentId]
                JOIN [UserRoles] ur ON ur.[UserID] = u.[UserID]
                JOIN [Roles] r ON r.[RoleID] = ur.[RoleID]
                WHERE s.[Status] = N''Active''
                  AND s.[ActivatedAt] IS NULL
                  AND c.[IsActive] = 1
                  AND u.[IsActive] = 1
                  AND r.[NormalizedName] = N''STUDENT''
                  AND NOT EXISTS (
                      SELECT 1 FROM [SessionCandidates] existing
                      WHERE existing.[SessionId] = s.[Id] AND existing.[StudentId] = cs.[StudentId]);

                UPDATE s SET [ActivatedAt] = SYSUTCDATETIME()
                FROM [ExamSessions] s
                WHERE s.[Status] = N''Active''
                  AND s.[ActivatedAt] IS NULL
                  AND EXISTS (SELECT 1 FROM [SessionClasses] sc WHERE sc.[SessionId] = s.[Id]);
                ');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "ExamSessions");
        }
    }
}
