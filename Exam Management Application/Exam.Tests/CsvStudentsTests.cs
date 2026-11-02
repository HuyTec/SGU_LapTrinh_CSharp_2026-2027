using Exam.Core;
using Exam.Services;
using Xunit;

namespace Exam.Tests;

public class CsvStudentsTests
{
    [Fact]
    public void QuotedNamesAndEmbeddedLineBreaksRemainInTheCorrectColumns()
    {
        var rows = CsvStudents.Parse("UserName,FullName,StudentCode\r\nsv1,\"Nguyễn, Văn A\",SV01\r\nsv2,\"Tên\r\nhai dòng\",SV02\r\n");
        Assert.Equal(3, rows.Count);
        Assert.Equal("Nguyễn, Văn A", rows[1][1]);
        Assert.Equal("Tên\r\nhai dòng", rows[2][1]);
        Assert.Equal("SV02", rows[2][2]);
    }

    [Fact]
    public void EscapedQuotesAndAnEmptyFinalFieldArePreserved()
    {
        var row = Assert.Single(CsvStudents.Parse("\"Tên \"\"A\"\"\",SV01,"));
        Assert.Equal(["Tên \"A\"", "SV01", ""], row);
    }

    [Theory]
    [InlineData("a,\"chưa đóng")]
    [InlineData("a,\"đã đóng\"lẫn dữ liệu")]
    [InlineData("a,nội\"dung")]
    public void MalformedCsvIsRejectedBeforeAnyImport(string input)
    {
        Assert.Throws<BusinessException>(() => CsvStudents.Parse(input));
    }
}
