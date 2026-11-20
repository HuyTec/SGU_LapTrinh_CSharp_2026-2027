using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exam.Data.Migrations
{
    /// <inheritdoc />
    public partial class SessionExamSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    SourceQuestionId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionQuestions", x => x.Id);
                    table.CheckConstraint("CK_SessionQuestion_Points", "[Points] > 0");
                    table.ForeignKey("FK_SessionQuestions_ExamSessions_SessionId", x => x.SessionId, "ExamSessions", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SessionQuestions_Questions_SourceQuestionId", x => x.SourceQuestionId, "Questions", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionQuestionId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionOptions", x => x.Id);
                    table.ForeignKey("FK_SessionOptions_SessionQuestions_SessionQuestionId", x => x.SessionQuestionId, "SessionQuestions", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionOptions_SessionQuestionId_Order",
                table: "SessionOptions",
                columns: new[] { "SessionQuestionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionOptions_SessionQuestionId",
                table: "SessionOptions",
                column: "SessionQuestionId",
                unique: true,
                filter: "[IsCorrect] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestions_SessionId_Order",
                table: "SessionQuestions",
                columns: new[] { "SessionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestions_SourceQuestionId",
                table: "SessionQuestions",
                column: "SourceQuestionId");

            // Preserve active sessions during rollout. This is a best-effort
            // baseline from the current ready paper; older edits cannot be
            // reconstructed from the normalized question bank.
            migrationBuilder.Sql("""
                INSERT INTO [SessionQuestions] ([SessionId], [SourceQuestionId], [Text], [Order], [Points])
                SELECT s.[Id], eq.[QuestionId], q.[Text], eq.[Order], eq.[Points]
                FROM [ExamSessions] s
                JOIN [ExamQuestions] eq ON eq.[ExamId] = s.[ExamId]
                JOIN [Questions] q ON q.[Id] = eq.[QuestionId]
                WHERE s.[Status] = N'Active'
                  AND s.[ActivatedAt] IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM [SessionQuestions] sq
                      WHERE sq.[SessionId] = s.[Id] AND sq.[SourceQuestionId] = eq.[QuestionId]);

                INSERT INTO [SessionOptions] ([SessionQuestionId], [Text], [Order], [IsCorrect])
                SELECT sq.[Id], ao.[Text], ao.[Order], ao.[IsCorrect]
                FROM [SessionQuestions] sq
                JOIN [ExamSessions] s ON s.[Id] = sq.[SessionId]
                JOIN [AnswerOptions] ao ON ao.[QuestionId] = sq.[SourceQuestionId]
                WHERE s.[Status] = N'Active'
                  AND NOT EXISTS (
                      SELECT 1 FROM [SessionOptions] so
                      WHERE so.[SessionQuestionId] = sq.[Id] AND so.[Order] = ao.[Order]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SessionOptions");
            migrationBuilder.DropTable(name: "SessionQuestions");
        }
    }
}
