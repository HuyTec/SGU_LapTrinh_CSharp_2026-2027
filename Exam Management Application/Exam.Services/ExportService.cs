using System.IO.Compression;
using System.Security;
using System.Text;
using Exam.Core;
namespace Exam.Services;

public class ExportService(AdminService admin)
{
    public async Task<byte[]> ExportAsync(int uid, string role, int sid, bool xlsx)
    {
        var rows = await admin.SessionMonitorAsync(uid, role, sid);
        string[] cols = ["session", "studentCode", "student", "status", "score", "submittedAt"];
        string[] labels = ["Đợt thi", "Mã thí sinh", "Họ tên", "Trạng thái", "Điểm", "Nộp lúc (UTC)"];
        string Safe(object? value)
        {
            var s = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            var trimmed = s.TrimStart();
            return trimmed.Length > 0 && "=+-@".Contains(trimmed[0]) ? "'" + s : s;
        }
        var cells = new List<string[]> { labels };
        cells.AddRange(rows.Select(r => cols.Select(k => Safe(r.GetValueOrDefault(k))).ToArray()));
        if (!xlsx)
        {
            string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\r\n", cells.Select(r => string.Join(",", r.Select(Q)))))).ToArray();
        }
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            void Write(string path, string text)
            {
                using var w = new StreamWriter(zip.CreateEntry(path).Open());
                w.Write(text);
            }
            Write("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Write("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Write("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Kết quả\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Write("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            Write("xl/worksheets/sheet1.xml", "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" + string.Concat(cells.Select((r, i) => "<row r=\"" + (i + 1) + "\">" + string.Concat(r.Select(c => "<c t=\"inlineStr\"><is><t xml:space=\"preserve\">" + SecurityElement.Escape(c) + "</t></is></c>")) + "</row>")) + "</sheetData></worksheet>");
        }
        return ms.ToArray();
    }
}

