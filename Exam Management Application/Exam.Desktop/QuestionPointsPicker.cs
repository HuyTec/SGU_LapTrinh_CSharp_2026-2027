using System.Data;
using System.Text.Json;
using Exam.Core;

namespace Exam.Desktop;

/// <summary>Edits selection and points for individual questions; the API validates the finished exam.</summary>
public sealed partial class QuestionPointsPicker : UserControl
{
    private readonly DataTable rows = new();
    private readonly BindingSource source = new();
    private bool disposingPicker;

    public QuestionPointsPicker() => InitializeComponent();

    public QuestionPointsPicker(IReadOnlyList<ChoiceDto>? choices, JsonElement values) : this()
    {
        rows.Columns.Add("selected", typeof(bool)); rows.Columns.Add("question", typeof(string)); rows.Columns.Add("points", typeof(decimal)); rows.Columns.Add("id", typeof(int));
        var points = values.ValueKind == JsonValueKind.Object ? values.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetDecimal()) : [];
        foreach (var choice in choices ?? [])
        {
            if (!int.TryParse(choice.Value, out var id)) continue;
            rows.Rows.Add(points.ContainsKey(choice.Value), choice.Label, points.GetValueOrDefault(choice.Value, 1m), id);
        }
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
        search.TextChanged += (_, _) =>
        {
            if (disposingPicker || !grid.EndEdit()) return;
            var escaped = string.Concat(search.Text.Trim().Select(c => c switch { '\'' => "''", '[' => "[[]", ']' => "[]]", '%' => "[%]", '*' => "[*]", _ => c.ToString() }));
            source.Filter = string.IsNullOrEmpty(escaped) ? null : "question LIKE '%" + escaped + "%'";
        };
        UpdateTotal();
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
        total.Text = $"Đã chọn {selected.Count} câu — tổng trọng số: {selected.Sum(r => (decimal)r["points"]):0.##}";
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
