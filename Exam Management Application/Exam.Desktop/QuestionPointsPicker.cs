using System.Data;
using System.Text.Json;
using Exam.Core;

namespace Exam.Desktop;

/// <summary>Edits selection and points for individual questions; the API validates the finished exam.</summary>
internal sealed class QuestionPointsPicker : UserControl
{
    private readonly DataTable rows = new();
    private readonly UiDataGridView grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoGenerateColumns = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly Label total = new() { Dock = DockStyle.Bottom, Height = 34, TextAlign = ContentAlignment.MiddleLeft, Font = Theme.BodyFont, ForeColor = Theme.Primary };
    private readonly Label validation = new() { Dock = DockStyle.Bottom, Height = 24, ForeColor = Theme.Danger, Font = Theme.SmallFont };
    private readonly TextBox search = new() { Dock = DockStyle.Fill, PlaceholderText = "Tìm theo nội dung hoặc chủ đề…" };
    private readonly TextBox preview = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, TabStop = true };
    private readonly BindingSource source = new();
    private bool disposingPicker;

    public QuestionPointsPicker(IReadOnlyList<ChoiceDto>? choices, JsonElement values)
    {
        Height = 500; Width = 420; MinimumSize = new Size(280, 400); BackColor = Theme.Surface;
        Font = Theme.BodyFont;
        rows.Columns.Add("selected", typeof(bool)); rows.Columns.Add("question", typeof(string)); rows.Columns.Add("points", typeof(decimal)); rows.Columns.Add("id", typeof(int));
        var points = values.ValueKind == JsonValueKind.Object ? values.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetDecimal()) : [];
        foreach (var choice in choices ?? [])
        {
            if (!int.TryParse(choice.Value, out var id)) continue;
            rows.Rows.Add(points.ContainsKey(choice.Value), choice.Label, points.GetValueOrDefault(choice.Value, 1m), id);
        }
        Theme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "selected", DataPropertyName = "selected", HeaderText = "Chọn", FillWeight = 12, MinimumWidth = 65 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "question", DataPropertyName = "question", HeaderText = "Chủ đề và nội dung câu hỏi", ReadOnly = true, FillWeight = 70, MinimumWidth = 140 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "points", DataPropertyName = "points", HeaderText = "Điểm", FillWeight = 18, MinimumWidth = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.##" } });
        source.DataSource = rows; grid.DataSource = source;
        grid.CurrentCellDirtyStateChanged += (_, _) => { if (!disposingPicker && grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.CellValueChanged += (_, _) => UpdateTotal();
        grid.SelectionChanged += PreviewSelectionChanged;
        grid.DataError += (_, e) => { e.ThrowException = false; validation.Text = "Điểm cần là số hợp lệ theo định dạng máy."; };
        grid.CellValidating += (_, e) =>
        {
            if (disposingPicker || e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count || grid.Columns[e.ColumnIndex].Name != "points") return;
            if (!decimal.TryParse(e.FormattedValue?.ToString(), out var point) || point < 0.01m || decimal.Round(point, 2) != point)
            { e.Cancel = true; validation.Text = "Điểm ít nhất 0,01, tối đa hai chữ số thập phân."; }
            else validation.Text = "";
        };
        var searchRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 48, ColumnCount = 2, Margin = Padding.Empty };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.Controls.Add(new Label { Text = "Tìm câu hỏi", AutoSize = true, Margin = new Padding(0, 8, 12, 0) }, 0, 0); searchRow.Controls.Add(search, 1, 0);
        Theme.StyleInput(search); Theme.StyleInput(preview);
        var previewPanel = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 140, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        previewPanel.Controls.Add(new Label { Text = "Xem toàn bộ câu hỏi đang chọn", AutoSize = true, ForeColor = Theme.Muted, Font = Theme.SmallFont }, 0, 0); previewPanel.Controls.Add(preview, 0, 1);
        search.TextChanged += (_, _) =>
        {
            if (disposingPicker || !grid.EndEdit()) return;
            var escaped = string.Concat(search.Text.Trim().Select(c => c switch { '\'' => "''", '[' => "[[]", ']' => "[]]", '%' => "[%]", '*' => "[*]", _ => c.ToString() }));
            source.Filter = string.IsNullOrEmpty(escaped) ? null : "question LIKE '%" + escaped + "%'";
        };
        Controls.Add(grid); Controls.Add(previewPanel); Controls.Add(validation); Controls.Add(total); Controls.Add(searchRow); UpdateTotal();
    }

    public Dictionary<string, decimal> SelectedPoints()
    {
        grid.EndEdit();
        return rows.Rows.Cast<DataRow>().Where(r => (bool)r["selected"]).ToDictionary(r => r["id"].ToString()!, r => (decimal)r["points"]);
    }

    public bool CanCommit() => grid.EndEdit();

    private void UpdateTotal()
    {
        if (disposingPicker || IsDisposed) return;
        var selected = rows.Rows.Cast<DataRow>().Where(r => r["selected"] is true).ToList();
        total.Text = $"Đã chọn {selected.Count} câu — tổng điểm: {selected.Sum(r => (decimal)r["points"]):0.##} / 10";
    }

    private void PreviewSelectionChanged(object? sender, EventArgs e)
    {
        if (disposingPicker || IsDisposed || grid.IsDisposed || preview.IsDisposed) return;
        if (source.Position < 0 || source.Position >= source.Count || grid.CurrentRow is not { IsNewRow: false } row || row.Index < 0 || row.Index >= source.Count)
        { preview.Text = "Chọn một dòng để xem toàn bộ nội dung câu hỏi."; return; }
        // Read the valid binding position rather than DataBoundItem while CurrencyManager resets rows.
        preview.Text = source.Current is DataRowView current && current.Row.RowState != DataRowState.Detached
            ? current.Row["question"].ToString() : "Chọn một dòng để xem toàn bộ nội dung câu hỏi.";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposingPicker)
        {
            disposingPicker = true;
            grid.SelectionChanged -= PreviewSelectionChanged;
            grid.DataSource = null;
            source.Dispose(); rows.Dispose();
        }
        base.Dispose(disposing);
    }
}
