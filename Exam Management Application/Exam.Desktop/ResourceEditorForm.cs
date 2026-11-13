using System.Text.Json;
using Exam.Core;

namespace Exam.Desktop;

/// <summary>Builds a labeled editor from server-owned field definitions and lookup choices.</summary>
public sealed partial class ResourceEditorForm : Form
{
    private readonly ExamApiClient api = null!;
    private readonly ResourceDto resource = new("", "", []);
    private readonly int? id;
    private readonly Dictionary<string, JsonElement> original = new();
    private readonly Dictionary<string, Control> inputs = new();
    private bool saving;
    private readonly Dictionary<string, TableLayoutPanel> sections = new();
    private readonly Dictionary<string, Control> sectionCards = new();
    private TableLayoutPanel? answerGrid;
    private Control? reviewCard;
    private TextBox? reviewText;
    private int wizardStep;
    private bool IsSessionWizard => resource.Key == "sessions";

    public ResourceEditorForm() => InitializeComponent();

    public ResourceEditorForm(ExamApiClient api, ResourceDto resource, int? id, Dictionary<string, JsonElement> values) : this()
    {
        this.api = api; this.resource = resource; this.id = id; original = values;
        Text = $"{(id.HasValue ? "Xem / Sửa" : "Thêm")} — {resource.Label}";
        var area = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(resource.Key == "exams" ? 1100 : 940, area.Width - 48), Math.Min(760, area.Height - 48));
        MinimumSize = new Size(Math.Min(640, area.Width - 48), Math.Min(450, area.Height - 48));
        heading.Text = (id.HasValue ? "Chỉnh sửa " : "Thêm ") + resource.Label.ToLowerInvariant();
        if (IsSessionWizard)
            guidance.Text = "Hoàn thành 3 bước để lưu đợt nháp. Ctrl + Enter để tiếp tục / lưu · Esc để đóng.";
        wizardProgress.Visible = IsSessionWizard;
        FormClosing += (_, e) => { if (saving) e.Cancel = true; };
        foreach (var field in resource.Fields) AddField(field);
        status.Text = resource.Key switch { "exams" => "Nhập điểm từng câu. Tổng điểm 10 để xác nhận sẵn sàng sau khi lưu.", "sessions" => "Lưu đợt nháp trước; phân công thí sinh và kích hoạt ở màn đợt thi.", _ => "Thay đổi chỉ được ghi khi bấm Lưu." };
        footer.Resize += (_, _) => status.MaximumSize = new Size(Math.Max(160, footer.ClientSize.Width - actions.Width - 72), 0);
        save.Click += async (_, _) => await SaveAsync();
        if (IsSessionWizard)
        {
            BuildSessionReview();
            previous.Click += (_, _) => ShowWizardStep(wizardStep - 1);
            next.Click += (_, _) => AdvanceWizard();
            ShowWizardStep(0);
        }
        Shown += (_, _) => { var working = Screen.FromControl(this).WorkingArea; if (Height > working.Height - 24) Height = working.Height - 24; if (Width > working.Width - 24) Width = working.Width - 24; };
    }

    private void AddField(FieldDto field)
    {
        if (field.Type.Equals("hidden", StringComparison.OrdinalIgnoreCase)) return;
        original.TryGetValue(field.Name, out var value);
        Control input;
        switch (field.Type.ToLowerInvariant())
        {
            case "questionpoints":
                input = new QuestionPointsPicker(field.Choices, value); break;
            case "checkbox":
                input = new CheckBox { Checked = value.ValueKind == JsonValueKind.True || (!id.HasValue && field.Name == "isActive"), AutoSize = true, Text = "Có / Bật" }; break;
            case "number":
                var number = new NumericUpDown { Minimum = -1000000, Maximum = 1000000, DecimalPlaces = field.Name.Contains("point", StringComparison.OrdinalIgnoreCase) ? 2 : 0, Width = 200 };
                if (decimal.TryParse(value.ToString(), out var n)) number.Value = Math.Clamp(n, number.Minimum, number.Maximum);
                else if (field.Name == "pointsPerQuestion") number.Value = 1;
                else if (field.Name == "durationMinutes") number.Value = 60;
                input = number; break;
            case "datetime":
                var picker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Width = 240, ShowUpDown = false };
                if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var date))
                    picker.Value = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(date, DateTimeKind.Utc), VietNamTime.Zone);
                else picker.Value = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddMinutes(field.Name == "closesAt" ? 65 : 5), VietNamTime.Zone);
                input = picker; break;
            case "select":
                if (field.Name.Equals("correctIndex", StringComparison.OrdinalIgnoreCase))
                {
                    var group = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
                    foreach (var choice in field.Choices ?? []) group.Controls.Add(new RadioButton { Text = "Đáp án " + choice.Label, Tag = choice.Value, Checked = value.ToString() == choice.Value, AutoSize = true, Margin = new Padding(0, 8, 24, 8) });
                    if (!group.Controls.OfType<RadioButton>().Any(r => r.Checked) && group.Controls.Count > 0) ((RadioButton)group.Controls[0]).Checked = true;
                    input = group; break;
                }
                var select = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 400, DisplayMember = nameof(ChoiceDto.Label), ValueMember = nameof(ChoiceDto.Value) };
                foreach (var choice in field.Choices ?? []) select.Items.Add(choice with { Label = UiLabels.Of(choice.Label) });
                var selected = value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? "" : value.ToString();
                select.SelectedItem = select.Items.Cast<ChoiceDto>().FirstOrDefault(c => c.Value == selected);
                if (select.SelectedIndex < 0 && select.Items.Count > 0) select.SelectedIndex = 0;
                input = select; break;
            case "multiselect":
                var list = new CheckedListBox { CheckOnClick = true, Height = 170, Width = 460, DisplayMember = nameof(ChoiceDto.Label), HorizontalScrollbar = true };
                var checkedValues = value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Select(v => v.ToString()).ToHashSet() : [];
                foreach (var choice in field.Choices ?? []) list.Items.Add(choice with { Label = UiLabels.Of(choice.Label) }, checkedValues.Contains(choice.Value));
                input = list; break;
            default:
                var text = new TextBox { Width = field.Name.StartsWith("option", StringComparison.OrdinalIgnoreCase) ? 200 : 460, UseSystemPasswordChar = field.Type == "password" };
                if (field.Type != "password" && value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null) text.Text = value.ToString();
                if (field.Name.Contains("text", StringComparison.OrdinalIgnoreCase) || field.Name.StartsWith("option", StringComparison.OrdinalIgnoreCase))
                { text.Multiline = true; text.Height = field.Name == "text" ? 110 : 72; text.ScrollBars = ScrollBars.Vertical; }
                input = text; break;
        }
        Theme.StyleInput(input); input.Name = field.Name; input.AccessibleName = field.Label; input.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        inputs[field.Name] = input;
        input.Validating += (_, e) => { e.Cancel = !ValidateField(field, input); };
        var label = field.Label.Replace(" (giờ địa phương)", "") + (field.Required ? " *" : "");
        if (field.Type == "datetime") label += "\n(giờ Việt Nam)";
        if (field.Type == "password" && id.HasValue) label += "\n(để trống để giữ nguyên)";
        AddRow(field, new Label { Text = label, Font = Theme.BodyFont, AutoSize = true, Margin = new Padding(0, 4, 0, 2) }, input);
    }

    private void AddRow(FieldDto field, Control label, Control input)
    {
        var name = resource.Key switch
        {
            "questions" when field.Name.StartsWith("option") || field.Name == "correctIndex" => "Phương án trả lời",
            "questions" => "Nội dung và phân loại",
            "exams" when field.Name == "questionPoints" => "Câu hỏi và điểm",
            "sessions" when field.Name is "opensAt" or "closesAt" or "durationMinutes" => "Lịch và thời lượng",
            "sessions" when field.Name is "shuffleQuestions" or "shuffleOptions" or "reviewPolicy" => "Thiết lập bài thi và kết quả",
            _ => "Thông tin chung"
        };
        if (!sections.TryGetValue(name, out var table))
        {
            var card = new UiCard { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(20), Margin = new Padding(0, 0, 0, 16) };
            table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Theme.Surface, Margin = Padding.Empty };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.Controls.Add(new Label { Text = name, Font = Theme.SectionFont, AutoSize = true, Margin = new Padding(0, 0, 0, 14) }, 0, table.RowCount++);
            card.Controls.Add(table); sections[name] = table; sectionCards[name] = card; layout.Controls.Add(card, 0, layout.RowCount++);
        }
        var fieldPanel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Margin = Padding.Empty };
        fieldPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); fieldPanel.Controls.Add(label, 0, 0); fieldPanel.Controls.Add(input, 0, 1);
        if (resource.Key == "questions" && field.Name.StartsWith("option", StringComparison.OrdinalIgnoreCase))
        {
            if (answerGrid is null)
            {
                answerGrid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Margin = Padding.Empty };
                answerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); answerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                table.Controls.Add(answerGrid, 0, table.RowCount++);
            }
            var index = answerGrid.Controls.Count; fieldPanel.Margin = new Padding(index % 2 == 0 ? 0 : 8, 0, index % 2 == 0 ? 8 : 0, 0);
            answerGrid.Controls.Add(fieldPanel, index % 2, index / 2);
        }
        else table.Controls.Add(fieldPanel, 0, table.RowCount++);
    }

    private bool ValidateField(FieldDto field, Control input)
    {
        var required = field.Required || (field.Type == "password" && !id.HasValue);
        var invalid = required && input switch
        {
            TextBox text => string.IsNullOrWhiteSpace(text.Text) && !(id.HasValue && field.Type == "password"),
            ComboBox combo => combo.SelectedItem is null,
            CheckedListBox list => list.CheckedItems.Count == 0,
            FlowLayoutPanel group => !group.Controls.OfType<RadioButton>().Any(r => r.Checked),
            QuestionPointsPicker picker => !picker.CanCommit() || picker.SelectedPoints().Count == 0,
            _ => false
        };
        errors.SetError(input, invalid ? "Vui lòng nhập hoặc chọn " + field.Label.ToLowerInvariant() + "." : "");
        return !invalid;
    }

    private async Task SaveAsync()
    {
        if (saving) return;
        if (IsSessionWizard && wizardStep < 2) { AdvanceWizard(); return; }
        var valid = true;
        FieldDto? firstInvalid = null;
        foreach (var field in resource.Fields.Where(f => !f.Type.Equals("hidden", StringComparison.OrdinalIgnoreCase)))
        {
            var fieldValid = ValidateField(field, inputs[field.Name]); valid &= fieldValid;
            if (!fieldValid) firstInvalid ??= field;
        }
        if (IsSessionWizard && !ValidateSessionSchedule()) { valid = false; firstInvalid ??= resource.Fields.FirstOrDefault(f => inputs.ContainsKey(f.Name) && errors.GetError(inputs[f.Name]).Length > 0); }
        if (!valid)
        {
            if (firstInvalid is not null) { if (IsSessionWizard) ShowWizardStep(StepFor(firstInvalid.Name)); inputs[firstInvalid.Name].Focus(); }
            status.ForeColor = Theme.Danger; status.Text = "Kiểm tra các trường được đánh dấu lỗi.";
            return;
        }
        var values = new Dictionary<string, JsonElement>();
        foreach (var field in resource.Fields)
        {
            if (field.Type.Equals("hidden", StringComparison.OrdinalIgnoreCase))
            {
                if (original.TryGetValue(field.Name, out var hiddenValue)) values[field.Name] = hiddenValue.Clone();
                continue;
            }
            var control = inputs[field.Name];
            if (field.Type == "password" && id.HasValue && string.IsNullOrEmpty(control.Text)) continue;
            object? value = control switch
            {
                CheckBox check => check.Checked,
                NumericUpDown number => number.Value,
                DateTimePicker date => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.Value, DateTimeKind.Unspecified), VietNamTime.Zone),
                ComboBox combo => ConvertChoice((combo.SelectedItem as ChoiceDto)?.Value),
                CheckedListBox list => list.CheckedItems.Cast<ChoiceDto>().Select(c => ConvertChoice(c.Value)).ToArray(),
                FlowLayoutPanel group => ConvertChoice(group.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked)?.Tag?.ToString()),
                QuestionPointsPicker picker => picker.SelectedPoints(),
                _ => resource.Key == "classes" && field.Name == "code" ? control.Text.Trim().ToUpperInvariant() :
                    field.Type == "password" ? control.Text : control.Text.Trim()
            };
            values[field.Name] = JsonSerializer.SerializeToElement(value);
            if (control is QuestionPointsPicker questionPicker)
                values["questionIds"] = JsonSerializer.SerializeToElement(questionPicker.SelectedPoints().Keys.Select(int.Parse).ToArray());
        }
        saving = true; save.Enabled = false; previous.Enabled = false; next.Enabled = false; layout.Enabled = false; status.ForeColor = Theme.Muted; status.Text = "Đang lưu…";
        try { await api.SaveAsync(resource.Key, id, values); saving = false; DialogResult = DialogResult.OK; Close(); }
        catch (Exception ex)
        {
            if (IsSessionWizard && ex is not HttpRequestException)
            {
                var fieldName = ex.Message switch { "Chọn đề sẵn sàng." => "examId", "Lịch và thời lượng không hợp lệ." => "closesAt", "Chính sách không hợp lệ." => "reviewPolicy", _ => null };
                if (fieldName is not null && inputs.TryGetValue(fieldName, out var target)) { layout.Enabled = true; ShowWizardStep(StepFor(fieldName)); errors.SetError(target, ex.Message); target.Focus(); }
            }
            status.ForeColor = Theme.Danger; status.Text = ex is HttpRequestException ? "Không kết nối được máy chủ. Dữ liệu chưa được lưu; hãy thử lại." : ex.Message;
        }
        finally { saving = false; save.Enabled = true; previous.Enabled = true; next.Enabled = true; layout.Enabled = true; }
    }

    private static int StepFor(string fieldName) => fieldName is "examId" or "title" ? 0 : 1;

    private void BuildSessionReview()
    {
        var card = new UiCard { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(20), Margin = new Padding(0, 0, 0, 16) };
        var content = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = Theme.Surface };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.Controls.Add(new Label { Text = "Xem lại đợt thi", Font = Theme.SectionFont, AutoSize = true, Margin = new Padding(0, 0, 0, 16) }, 0, 0);
        reviewText = new TextBox { Multiline = true, ReadOnly = true, Height = 290, Width = 460, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.BodyFont, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Margin = Padding.Empty };
        content.Controls.Add(reviewText, 0, 1);
        var note = new Label { Text = "Bước tiếp theo sau khi lưu: mở Phân công thí sinh để gán danh sách, rồi quay lại đợt thi để Kích hoạt. Lưu ở đây chỉ tạo hoặc cập nhật đợt nháp; chưa cho phép thí sinh bắt đầu thi.", AutoSize = true, ForeColor = Theme.Primary, Margin = new Padding(0, 16, 0, 0) };
        content.Controls.Add(note, 0, 2); content.Resize += (_, _) => note.MaximumSize = new Size(Math.Max(240, content.ClientSize.Width), 0);
        card.Controls.Add(content); reviewCard = card; layout.Controls.Add(card, 0, layout.RowCount++);
    }

    private void ShowWizardStep(int step)
    {
        wizardStep = Math.Clamp(step, 0, 2);
        layout.SuspendLayout();
        foreach (var section in sectionCards) section.Value.Visible = wizardStep == (section.Key == "Thông tin chung" ? 0 : 1);
        if (reviewCard is not null) reviewCard.Visible = wizardStep == 2;
        if (wizardStep == 2 && reviewText is not null)
            reviewText.Text = string.Join(Environment.NewLine + Environment.NewLine, resource.Fields.Where(f => inputs.ContainsKey(f.Name)).Select(f => f.Label.Replace(" (giờ địa phương)", "") + ": " + DisplayValue(inputs[f.Name])));
        previous.Visible = wizardStep > 0; next.Visible = wizardStep < 2; save.Visible = wizardStep == 2; save.Text = "Lưu đợt nháp";
        wizardProgress.Text = $"Bước {wizardStep + 1}/3 · " + new[] { "Thông tin và đề thi", "Lịch và cấu hình", "Xem lại và lưu nháp" }[wizardStep];
        status.ForeColor = Theme.Muted;
        status.Text = wizardStep switch { 0 => "Chọn đề sẵn sàng và đặt tên đợt thi.", 1 => "Nhập lịch theo giờ Việt Nam và thiết lập bài thi.", _ => "Kiểm tra thông tin. Danh sách thí sinh được phân công sau khi lưu nháp." };
        layout.ResumeLayout(true);
        if (layout.Parent is ScrollableControl scroll) scroll.AutoScrollPosition = Point.Empty;
    }

    private static string DisplayValue(Control input) => input switch
    {
        ComboBox combo => (combo.SelectedItem as ChoiceDto)?.Label ?? "Chưa chọn",
        CheckBox check => check.Checked ? "Có" : "Không",
        DateTimePicker date => date.Value.ToString("dd/MM/yyyy HH:mm") + " (giờ Việt Nam)",
        NumericUpDown number => number.Value.ToString("0.##"),
        _ => input.Text.Trim()
    };

    private bool ValidateSessionSchedule()
    {
        var valid = true;
        if (inputs.GetValueOrDefault("opensAt") is DateTimePicker opens && inputs.GetValueOrDefault("closesAt") is DateTimePicker closes)
        {
            var error = closes.Value <= opens.Value ? "Giờ đóng phải sau giờ mở." : "";
            errors.SetError(closes, error); valid &= error.Length == 0;
        }
        if (inputs.GetValueOrDefault("durationMinutes") is NumericUpDown duration)
        {
            var error = duration.Value <= 0 ? "Thời lượng phải lớn hơn 0 phút." : "";
            errors.SetError(duration, error); valid &= error.Length == 0;
        }
        return valid;
    }

    private void AdvanceWizard()
    {
        if (saving || wizardStep >= 2) return;
        Control? firstInvalid = null;
        foreach (var field in resource.Fields.Where(f => inputs.ContainsKey(f.Name) && StepFor(f.Name) == wizardStep))
            if (!ValidateField(field, inputs[field.Name])) firstInvalid ??= inputs[field.Name];
        if (wizardStep == 1 && !ValidateSessionSchedule()) firstInvalid ??= inputs.Values.FirstOrDefault(control => errors.GetError(control).Length > 0);
        if (firstInvalid is not null) { status.ForeColor = Theme.Danger; status.Text = "Kiểm tra các trường được đánh dấu lỗi trước khi tiếp tục."; firstInvalid.Focus(); return; }
        ShowWizardStep(wizardStep + 1);
    }

    private static object? ConvertChoice(string? value) => value is null ? null : int.TryParse(value, out var id) ? id : value;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Enter)) { if (!saving) _ = SaveAsync(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) errors.Dispose();
        base.Dispose(disposing);
    }
}
