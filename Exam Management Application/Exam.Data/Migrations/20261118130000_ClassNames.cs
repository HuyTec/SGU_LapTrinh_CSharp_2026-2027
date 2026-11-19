using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Exam.Data.Migrations;

[DbContext(typeof(ExamDbContext))]
[Migration("20261118130000_ClassNames")]
public sealed class ClassNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Name", table: "Classes", type: "nvarchar(150)", maxLength: 150,
            nullable: false, defaultValue: "");
        migrationBuilder.Sql("EXEC(N'UPDATE [Classes] SET [Name] = [Code] WHERE LEN([Name]) = 0');");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "Name", table: "Classes");
}
