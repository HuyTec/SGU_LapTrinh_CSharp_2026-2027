using System.Data;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Exam.Desktop;

public sealed partial class ImportStudentsForm : Form
{
    private const string Header = "UserName,FullName,StudentCode,Email,Password";
    private readonly ExamApiClient api = null!;
    private string? csv;
    private int count;
    private bool importing;
    private readonly List<Control> fileActions = [];

    public ImportStudentsForm() => InitializeComponent();

    public ImportStudentsForm(ExamApiClient api) : this()
    {
        this.api = api; Text = "Nhập danh sách thí sinh từ CSV"; Theme.Apply(this);
        var area = Screen.FromControl(this).WorkingArea;
        StartPosition = FormStartPosition.CenterParent; Size = new Size(Math.Min(1040, area.Width - 48), Math.Min(720, area.Height - 48)); MinimumSize = new Size(Math.Min(700, area.Width - 48), Math.Min(450, area.Height - 48));
        Theme.StyleGrid(preview);
        fileActions.Add(choose); fileActions.Add(template);
        footer.Resize += (_, _) => status.MaximumSize = new Size(Math.Max(180, footer.ClientSize.Width - actions.Width - 64), 0);
        FormClosing += (_, e) => { if (importing) e.Cancel = true; };
        choose.Click += (_, _) => ChooseFile();
        template.Click += async (_, _) =>
        {
            using var dialog = new SaveFileDialog { Filter = "CSV UTF-8|*.csv", FileName = "MauThiSinh.csv" };
            try { if (dialog.ShowDialog(this) == DialogResult.OK) await File.WriteAllTextAsync(dialog.FileName, Header + Environment.NewLine, new UTF8Encoding(true)); }
            catch (Exception ex) { status.ForeColor = Theme.Danger; status.Text = "Không lưu được file mẫu: " + ex.Message; }
        };
        import.Click += async (_, _) => await ImportAsync();
    }

    private void ChooseFile()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV UTF-8|*.csv", Multiselect = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        import.Enabled = false; csv = null; preview.DataSource = null;
        try
        {
            if (new FileInfo(dialog.FileName).Length > 1024 * 1024) throw new InvalidOperationException("File CSV vượt quá 1 MB.");
            var text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            using var parser = new TextFieldParser(new StringReader(text)) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
            parser.SetDelimiters(",");
            var header = parser.ReadFields();
            if (header is null || string.Join(',', header).TrimStart('\uFEFF') != Header) throw new InvalidOperationException("Dòng đầu phải đúng: " + Header);
            var table = new DataTable();
            foreach (var label in new[] { "Tài khoản", "Họ tên", "Mã thí sinh", "Email", "Mật khẩu (ẩn)" }) table.Columns.Add(label);
            while (!parser.EndOfData)
            {
                var row = parser.ReadFields();
                if (row is null) continue;
                if (row.Length != 5) throw new InvalidOperationException($"Dòng {parser.LineNumber - 1} cần đủ 5 cột.");
                table.Rows.Add(row[0], row[1], row[2], row[3], string.IsNullOrEmpty(row[4]) ? "Chưa nhập" : "••••••••");
            }
            count = table.Rows.Count;
            if (count == 0) throw new InvalidOperationException("File chưa có dòng thí sinh.");
            if (count > 500) throw new InvalidOperationException("Danh sách vượt quá 500 thí sinh. Hãy chia thành các tệp nhỏ hơn.");
            preview.DataSource = new BindingSource { DataSource = table }; csv = text;
            status.ForeColor = Theme.Muted; status.Text = $"Đã đọc {count} thí sinh. Kiểm tra bản xem trước rồi bấm Kiểm tra và nhập. Chưa ghi dữ liệu lên máy chủ.";
            import.Enabled = true;
        }
        catch (Exception ex) { status.ForeColor = Theme.Danger; status.Text = "Không đọc được file: " + ex.Message; }
    }

    private async Task ImportAsync()
    {
        if (csv is null) return;
        var payload = csv;
        importing = true; import.Enabled = false; foreach (var action in fileActions) action.Enabled = false;
        try
        {
            status.Text = "Đang kiểm tra toàn bộ danh sách trên máy chủ…";
            var validated = await api.ImportStudentsAsync(payload, preview: true);
            if (!UiConfirmDialog.Confirm(this, "Nhập danh sách thí sinh", $"Máy chủ đã kiểm tra {validated} tài khoản. Xác nhận tạo danh sách thí sinh?", "Xác nhận nhập"))
            { status.Text = "Đã kiểm tra hợp lệ. Chưa nhập dữ liệu."; import.Enabled = true; return; }
            var imported = await api.ImportStudentsAsync(payload);
            status.ForeColor = Theme.Success; status.Text = $"Đã nhập thành công {imported} thí sinh.";
            importing = false; DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { status.ForeColor = Theme.Danger; status.Text = ex is HttpRequestException ? "Mất kết nối. Hãy kiểm tra danh sách trên máy chủ trước khi thử lại." : ex.Message; import.Enabled = true; }
        finally { importing = false; foreach (var action in fileActions) action.Enabled = true; }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Enter)) { if (import.Enabled && !importing) _ = ImportAsync(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
