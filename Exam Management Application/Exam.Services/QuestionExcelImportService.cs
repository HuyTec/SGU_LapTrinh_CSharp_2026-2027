using System.IO.Compression;
using System.Data;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Exam.Core;
using Exam.Data;
using Microsoft.EntityFrameworkCore;

namespace Exam.Services;

public sealed record QuestionImportRow(string Topic, string Text, string Difficulty,
    string OptionA, string OptionB, string OptionC, string OptionD, int CorrectOption);

public sealed class QuestionExcelImportService(ExamDbContext db)
{
    private const int MaxFileBytes = 4 * 1024 * 1024;
    private const int MaxQuestions = 500;
    private static readonly string[] Headers = ["Chủ đề", "Nội dung", "Độ khó", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng"];

    public async Task<IReadOnlyList<string>> GetActiveTopicNamesAsync(int lecturerId, int subjectId, CancellationToken cancellationToken = default)
    {
        var assigned = await db.LecturerSubjects.AnyAsync(x => x.LecturerId == lecturerId && x.SubjectId == subjectId
            && x.Subject.IsActive, cancellationToken);
        if (!assigned)
            throw new BusinessException("Môn học không nằm trong phạm vi được phân công.", 403);
        return await db.Topics.Where(x => x.SubjectId == subjectId && x.IsActive)
            .OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(cancellationToken);
    }

    public static byte[] CreateTemplate(IReadOnlyList<string>? topicNames = null)
    {
        topicNames ??= [];
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            AddXml(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            AddXml(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            AddXml(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="CauHoi" sheetId="1" r:id="rId1"/><sheet name="ChuDeMonHoc" sheetId="2" r:id="rId2"/></sheets>
                  <definedNames><definedName name="DanhSachChuDe">'ChuDeMonHoc'!$A$1:$A${Math.Max(1, topicNames.Count)}</definedName></definedNames>
                </workbook>
                """);
            AddXml(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
                </Relationships>
                """);
            var cells = string.Join("", Headers.Select((header, index) => $"<c r=\"{Column(index + 1)}1\" t=\"inlineStr\"><is><t>{XmlEscape(header)}</t></is></c>"));
            var note = "<row r=\"2\"><c r=\"J2\" t=\"inlineStr\"><is><t>Ô A2 trở xuống gợi ý chủ đề cũ; có thể gõ chủ đề mới, hệ thống sẽ tự thêm vào môn đã chọn.</t></is></c></row>";
            var topicValidation = topicNames.Count == 0 ? "" : "<dataValidations count=\"1\"><dataValidation type=\"list\" allowBlank=\"0\" showErrorMessage=\"0\" sqref=\"A2:A501\"><formula1>DanhSachChuDe</formula1></dataValidation></dataValidations>";
            AddXml(archive, "xl/worksheets/sheet1.xml", $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1">{cells}</row>{note}</sheetData>{topicValidation}</worksheet>
                """);
            var topicRows = topicNames.Count == 0
                ? "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>Chủ đề hiện có trong môn đã chọn</t></is></c></row>"
                : string.Concat(topicNames.Select((name, index) => $"<row r=\"{index + 1}\"><c r=\"A{index + 1}\" t=\"inlineStr\"><is><t>{XmlEscape(name)}</t></is></c></row>"));
            AddXml(archive, "xl/worksheets/sheet2.xml", $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>{topicRows}</sheetData></worksheet>
                """);
        }
        return memory.ToArray();
    }

    public async Task<int> ImportAsync(int lecturerId, int subjectId, Stream file, long length, CancellationToken cancellationToken = default)
    {
        if (length is <= 0 or > MaxFileBytes)
            throw new BusinessException("File Excel phải có dung lượng từ 1 byte đến 4 MB.");

        var rows = ReadWorkbook(file);
        if (rows.Count == 0)
            throw new BusinessException("File chưa có dòng câu hỏi nào dưới hàng tiêu đề.");
        if (rows.Count > MaxQuestions)
            throw new BusinessException($"Mỗi lần chỉ nhập tối đa {MaxQuestions} câu hỏi.");

        var assigned = await db.LecturerSubjects.AnyAsync(x => x.LecturerId == lecturerId && x.SubjectId == subjectId
            && x.Subject.IsActive, cancellationToken);
        if (!assigned)
            throw new BusinessException("Môn học không nằm trong phạm vi được phân công.", 403);

        var errors = new List<string>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Topic.Trim().Length is 0 or > 150)
                errors.Add($"Dòng {i + 2}: chủ đề phải có độ dài từ 1 đến 150 ký tự.");
            if (row.Text.Trim().Length is 0 or > 2000 || row.OptionA.Trim().Length is 0 or > 2000 || row.OptionB.Trim().Length is 0 or > 2000 ||
                row.OptionC.Trim().Length is 0 or > 2000 || row.OptionD.Trim().Length is 0 or > 2000)
                errors.Add($"Dòng {i + 2}: nội dung câu hỏi và 4 đáp án phải có độ dài từ 1 đến 2000 ký tự.");
            if (new[] { row.OptionA, row.OptionB, row.OptionC, row.OptionD }.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
                errors.Add($"Dòng {i + 2}: bốn đáp án không được trùng nhau.");
            if (row.CorrectOption is < 1 or > 4)
                errors.Add($"Dòng {i + 2}: đáp án đúng phải là A, B, C, D hoặc 1, 2, 3, 4.");
            if (row.Difficulty is not ("Easy" or "Medium" or "Hard"))
                errors.Add($"Dòng {i + 2}: độ khó phải là Dễ, Trung bình, Khó hoặc Easy, Medium, Hard.");
            var key = row.Topic.Trim() + "\0" + row.Text.Trim();
            if (!seen.Add(key))
                errors.Add($"Dòng {i + 2}: câu hỏi bị trùng với một dòng khác trong file.");
        }
        if (errors.Count > 0)
            throw new BusinessException("Không nhập dữ liệu nào. Vui lòng sửa các lỗi sau:\n" + string.Join("\n", errors.Take(30)));

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var topics = await db.Topics.Where(x => x.SubjectId == subjectId).ToListAsync(cancellationToken);
            var topicsByName = topics.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var importedTopicNames = rows.Select(x => x.Topic.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var topicName in importedTopicNames)
            {
                if (!topicsByName.TryGetValue(topicName, out var topic))
                {
                    topic = new Topic { SubjectId = subjectId, Name = topicName, IsActive = true };
                    db.Topics.Add(topic);
                    topicsByName.Add(topicName, topic);
                }
                else if (!topic.IsActive)
                {
                    topic.IsActive = true;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            foreach (var row in rows)
            {
                var question = new Question
                {
                    TopicId = topicsByName[row.Topic.Trim()].Id,
                    CreatedBy = lecturerId,
                    Text = row.Text.Trim(),
                    Difficulty = row.Difficulty,
                    IsActive = true,
                    Options = new[] { row.OptionA, row.OptionB, row.OptionC, row.OptionD }
                        .Select((text, index) => new AnswerOption
                        {
                            Text = text.Trim(), Order = index + 1, IsCorrect = index + 1 == row.CorrectOption
                        }).ToList()
                };
                db.Questions.Add(question);
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return rows.Count;
        });
    }

    private static List<QuestionImportRow> ReadWorkbook(Stream stream)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
            if (archive.Entries.Count > 200 || archive.Entries.Any(e => e.Length > MaxFileBytes))
                throw new BusinessException("Cấu trúc file Excel không hợp lệ hoặc vượt giới hạn cho phép.");
            var workbook = ReadXml(archive, "xl/workbook.xml");
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace packageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
            var sheet = workbook.Root?.Element(main + "sheets")?.Elements(main + "sheet").FirstOrDefault()
                ?? throw new BusinessException("File Excel không có trang tính.");
            var relationId = (string?)sheet.Attribute(rel + "id")
                ?? throw new BusinessException("Không đọc được trang tính đầu tiên trong file Excel.");
            var relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels");
            var target = (string?)relationships.Root?.Elements(packageRel + "Relationship")
                .FirstOrDefault(x => (string?)x.Attribute("Id") == relationId)?.Attribute("Target")
                ?? throw new BusinessException("Liên kết trang tính trong file Excel không hợp lệ.");
            var sheetPath = NormalizeSheetPath(target);
            var sheetXml = ReadXml(archive, sheetPath);
            string[] sharedStrings = archive.GetEntry("xl/sharedStrings.xml") is null
                ? Array.Empty<string>()
                : ReadXml(archive, "xl/sharedStrings.xml").Root?.Elements(main + "si")
                    .Select(si => string.Concat(si.Descendants(main + "t").Select(t => t.Value))).ToArray() ?? [];
            var worksheetRows = sheetXml.Root?.Element(main + "sheetData")?.Elements(main + "row").ToList() ?? [];
            if (worksheetRows.Count == 0)
                throw new BusinessException("Trang tính đang trống.");
            if (worksheetRows.Count > MaxQuestions + 1)
                throw new BusinessException($"Mỗi lần chỉ nhập tối đa {MaxQuestions} câu hỏi.");
            var headerCells = ReadRow(worksheetRows[0], sharedStrings, main);
            var headers = Headers.Select((_, i) => Get(headerCells, i + 1).Trim()).ToArray();
            if (!headers.SequenceEqual(Headers, StringComparer.OrdinalIgnoreCase))
                throw new BusinessException("Tiêu đề cột chưa đúng mẫu. Tải file mẫu và giữ nguyên 8 cột theo thứ tự.");

            var result = new List<QuestionImportRow>();
            foreach (var row in worksheetRows.Skip(1))
            {
                var cells = ReadRow(row, sharedStrings, main);
                if (cells.Values.All(string.IsNullOrWhiteSpace))
                    continue;
                if (result.Count >= MaxQuestions)
                    throw new BusinessException($"Mỗi lần chỉ nhập tối đa {MaxQuestions} câu hỏi.");
                var difficultyText = Get(cells, 3).Trim();
                var difficulty = difficultyText.ToLowerInvariant() switch
                {
                    "dễ" or "de" or "easy" => "Easy",
                    "trung bình" or "trung binh" or "medium" => "Medium",
                    "khó" or "kho" or "hard" => "Hard",
                    _ => difficultyText
                };
                var correctText = Get(cells, 8).Trim().ToUpperInvariant();
                var correct = correctText switch { "A" or "1" => 1, "B" or "2" => 2, "C" or "3" => 3, "D" or "4" => 4, _ => 0 };
                result.Add(new(Get(cells, 1).Trim(), Get(cells, 2).Trim(), difficulty,
                    Get(cells, 4).Trim(), Get(cells, 5).Trim(), Get(cells, 6).Trim(), Get(cells, 7).Trim(), correct));
            }
            return result;
        }
        catch (BusinessException) { throw; }
        catch (Exception ex) when (ex is XmlException or IOException or ArgumentException)
        {
            throw new BusinessException("Không đọc được file .xlsx. Hãy dùng mẫu Excel tải từ màn hình này.");
        }
    }

    private static Dictionary<int, string> ReadRow(XElement row, string[] shared, XNamespace main)
    {
        var result = new Dictionary<int, string>();
        foreach (var cell in row.Elements(main + "c"))
        {
            var address = (string?)cell.Attribute("r") ?? "";
            var column = 0;
            foreach (var ch in address.TakeWhile(char.IsLetter))
                column = column * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
            if (column is < 1 or > 8) continue;
            var value = cell.Element(main + "v")?.Value ??
                string.Concat(cell.Descendants(main + "t").Select(x => x.Value));
            if ((string?)cell.Attribute("t") == "s" && int.TryParse(value, out var index) && index >= 0 && index < shared.Length)
                value = shared[index];
            result[column] = value;
        }
        return result;
    }

    private static string Get(Dictionary<int, string> cells, int column) => cells.GetValueOrDefault(column, "");
    private static string NormalizeSheetPath(string target)
    {
        var path = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part is "" or ".") continue;
            if (part == "..") { if (parts.Count == 0) throw new InvalidDataException(); parts.RemoveAt(parts.Count - 1); }
            else parts.Add(part);
        }
        return string.Join('/', parts);
    }
    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidDataException();
        using var input = entry.Open();
        using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxFileBytes });
        return XDocument.Load(reader);
    }
    private static void AddXml(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
    private static string Column(int number)
    {
        var result = "";
        while (number > 0) { number--; result = (char)('A' + number % 26) + result; number /= 26; }
        return result;
    }
    private static string XmlEscape(string value) => System.Security.SecurityElement.Escape(value) ?? "";
}
