using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exam.Data.Migrations
{
    /// <inheritdoc />
    public partial class EarlyAdmissionClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdmissionsClosedAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdmissionsClosedAt",
                table: "ExamSessions");
        }
    }
}
