using System.IO.Compression;
using System.Xml.Linq;
using Exam.Services;
using Xunit;

namespace Exam.Tests;

public sealed class QuestionExcelImportTests
{
    [Fact]
    public void DownloadTemplateIsAnXlsxWithTheExpectedColumns()
    {
        using var input = new MemoryStream(QuestionExcelImportService.CreateTemplate());
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheet);
        using var sheetStream = sheet!.Open();
        var document = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var headers = document.Descendants(ns + "row").First().Elements(ns + "c")
            .Select(cell => cell.Element(ns + "is")?.Element(ns + "t")?.Value).ToArray();
        Assert.Equal(new[] { "Chủ đề", "Nội dung", "Độ khó", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng" }, headers);
    }

    [Fact]
    public void SubjectTemplateIncludesTopicChoicesAndDropdownValidation()
    {
        using var input = new MemoryStream(QuestionExcelImportService.CreateTemplate(["Lập trình C#", "Mảng"]));
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheet);
        using var sheetStream = sheet!.Open();
        var document = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Assert.Equal("DanhSachChuDe", document.Descendants(ns + "formula1").Single().Value);

        var topics = archive.GetEntry("xl/worksheets/sheet2.xml");
        Assert.NotNull(topics);
        using var topicsStream = topics!.Open();
        var topicDocument = XDocument.Load(topicsStream);
        Assert.Equal(new[] { "Lập trình C#", "Mảng" }, topicDocument.Descendants(ns + "t").Select(x => x.Value));
    }
}
