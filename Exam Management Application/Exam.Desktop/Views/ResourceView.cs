using System.Data;
using System.Drawing.Drawing2D;
using System.Text.Json;
using Exam.Core;

namespace Exam.Desktop;

/// <summary>Reusable staff workspace with resource-specific columns, actions and question preview.</summary>
public partial class ResourceView : UserControl, IWorkspaceView, IResourceView
{
    private readonly string resourceKey;
    private readonly ResourcePresenter presenter = null!;
    private readonly ExamApiClient api = null!;
    private readonly BindingSource source = new();
    private readonly ComboBox subjectStatusFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(ChoiceDto.Label), Width = 180 };
    private readonly Dictionary<int, Dictionary<string, object?>> rowsById = new();
    private readonly List<UiButton> selectedActions = [];
    private ResourceDto? resource;
    private string accountRole = "Lecturer";
    private bool busy;
    private bool binding;
    private bool loaded;
    private bool arrangingToolbar;
    private Dictionary<string, JsonElement>? lastPreviewDetail;
    private string? lastPreviewMessage;
    private int renderedPreviewWidth;
    private string title;

    public string Title => title;
    public event EventHandler<string>? NavigateRequested;
    public bool IsResourceVisible => Visible && IsHandleCreated && !IsDisposed;
    public IWin32Window DialogOwner => (IWin32Window?)FindForm() ?? this;
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public ResourceQuery Query => new(search.Text.Trim(), resourceKey == "questions" ? Value(subjectFilter) : null,
        resourceKey == "questions" ? Value(topicFilter) : null, resourceKey == "questions" ? Value(difficultyFilter) : null,
        resourceKey is "subjects" or "classes" && subjectStatusFilter.SelectedIndex > 0 ? subjectStatusFilter.SelectedIndex == 1 : null, resourceKey == "users" ? accountRole : null);
    public int? SelectedRecordId => grid.CurrentRow is { } row && int.TryParse(row.Cells["_id"].Value?.ToString(), out var id) ? id : null;
    public int? SelectedSessionId => SelectedRecordId is int id && rowsById.TryGetValue(id, out var row) ? Integer(row.GetValueOrDefault("sessionId")) : null;

    public ResourceView() : this("") { }

    protected ResourceView(string resourceKey)
    {
        this.resourceKey = resourceKey;
        title = Titles.GetValueOrDefault(resourceKey, "Danh sách dữ liệu");
        InitializeComponent();
        countLabel.Text = title;
        filters.Visible = previewCard.Visible = sideGap.Visible = resourceKey is "" or "questions";
    }

    public ResourceView(ExamApiClient api, string resourceKey) : this(resourceKey)
    {
        this.api = api;
        presenter = new ResourcePresenter(api, this, resourceKey);
        Theme.StyleInput(search); Theme.StyleInput(subjectFilter); Theme.StyleInput(topicFilter); Theme.StyleInput(difficultyFilter);
        if (resourceKey is "subjects" or "classes")
        {
            search.PlaceholderText = resourceKey == "classes" ? "Tìm theo mã hoặc tên lớp…" : "Tìm theo mã hoặc tên môn…";
            add.Text = resourceKey == "classes" ? "Thêm lớp" : "Thêm môn học";
            Theme.StyleInput(subjectStatusFilter);
            subjectStatusFilter.Items.AddRange([
                new ChoiceDto("", "Tất cả"),
                new ChoiceDto("true", "Đang sử dụng"),
                new ChoiceDto("false", "Ngừng sử dụng")
            ]);
            subjectStatusFilter.SelectedIndex = 0;
            var statusGroup = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            statusGroup.Controls.Add(new Label { Text = "Trạng thái", AutoSize = true });
            statusGroup.Controls.Add(subjectStatusFilter);
            filters.Controls.Add(statusGroup);
            subjectStatusFilter.SelectedIndexChanged += async (_, _) => await presenter.SearchAsync();
        }
        if (resourceKey == "users")
        {
            foreach (var (role, label) in new[] { ("Lecturer", "Giảng viên"), ("Student", "Sinh viên") })
            {
                var category = Button(label, role == accountRole ? UiButtonKind.Primary : UiButtonKind.Secondary);
                category.Tag = role;
                category.Click += async (_, _) =>
                {
                    accountRole = role;
                    foreach (var button in filters.Controls.OfType<UiButton>())
                        button.Kind = button.Tag?.ToString() == role ? UiButtonKind.Primary : UiButtonKind.Secondary;
                    grid.Columns["lecturerCode"]!.Visible = role == "Lecturer";
                    grid.Columns["studentCode"]!.Visible = role == "Student";
                    foreach (Control action in resourceActions.Controls)
                        if (action.Tag?.ToString() == "studentImport") action.Visible = role == "Student";
                    LayoutToolbar();
                    await presenter.SearchAsync();
                };
                filters.Controls.Add(category);
            }
        }
        toolbar.SizeChanged += (_, _) => LayoutToolbar();
        empty.SizeChanged += (_, _) => emptyLayout.MaximumSize = new Size(Math.Max(200, empty.Width - 32), 0);
        grid.DataSource = source; ConfigureColumns();
        find.Click += async (_, _) => await presenter.SearchAsync();
        refresh.Click += async (_, _) => await RefreshAsync();
        add.Click += async (_, _) => await AddNewAsync(); emptyAction.Click += async (_, _) => await AddNewAsync();
        edit.Click += async (_, _) => await presenter.EditSelectedAsync();
        delete.Click += async (_, _) => await presenter.DeleteSelectedAsync();
        previous.Click += async (_, _) => await presenter.PreviousAsync(); next.Click += async (_, _) => await presenter.NextAsync();
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await presenter.SearchAsync(); } };
        grid.SelectionChanged += async (_, _) =>
        {
            if (binding) return;
            UpdateSelectionActions();
            await presenter.SelectQuestionAsync(SelectedRecordId);
        };
        grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name != "_members" && edit.Visible && edit.Enabled) await presenter.EditSelectedAsync(); };
        grid.CellContentClick += async (_, e) =>
        {
            if (busy || resourceKey != "classes" || e.RowIndex < 0 || e.ColumnIndex < 0 ||
                grid.Columns[e.ColumnIndex].Name != "_members") return;
            if (!int.TryParse(grid.Rows[e.RowIndex].Cells["_id"].Value?.ToString(), out var id) ||
                !rowsById.TryGetValue(id, out var row)) return;
            using var dialog = new ClassMembersForm(api, id, row.GetValueOrDefault("code")?.ToString() ?? "");
            dialog.ShowDialog(DialogOwner);
            if (dialog.MembershipChanged) await presenter.SearchAsync();
        };
        grid.CellPainting += PaintBadge;
        grid.DataError += (_, e) => e.ThrowException = false;
        SizeChanged += (_, _) => { previewCard.Width = Math.Clamp(Width / 3, 280, 360); LayoutToolbar(); };
        previewCard.SizeChanged += (_, _) =>
        {
            var width = Math.Max(200, previewCard.ClientSize.Width - previewCard.Padding.Horizontal - 24);
            if (resourceKey == "questions" && width != renderedPreviewWidth) RenderQuestionDetail(lastPreviewDetail, lastPreviewMessage);
        };
        VisibleChanged += (_, _) => LayoutToolbar();
        subjectGroup.Visible = topicGroup.Visible = difficultyGroup.Visible = resourceKey == "questions";
        filters.Visible = resourceKey is "subjects" or "questions" or "classes" or "users";
        previewCard.Visible = sideGap.Visible = resourceKey == "questions";
        RenderQuestionDetail(null); countLabel.Text = title;
        LayoutToolbar();
    }

    public Task LoadAsync() => presenter.LoadAsync();
    public Task RefreshAsync() => presenter.RefreshAsync();
    public Task AddNewAsync() => presenter.AddNewAsync();
    public void FocusSearch() { search.Focus(); search.SelectAll(); }

    public void Configure(AdminCatalogDto catalog, ResourceDto definition, bool preserveFilters)
    {
        resource = definition; title = definition.Label;
        void Fill(ComboBox combo, string all, IEnumerable<ChoiceDto>? choices)
        {
            var selected = preserveFilters ? Value(combo) : null;
            combo.BeginUpdate(); combo.Items.Clear(); combo.Items.Add(new ChoiceDto("", all));
            foreach (var choice in choices ?? []) combo.Items.Add(choice with { Label = UiLabels.Of(choice.Label) });
            combo.SelectedIndex = Enumerable.Range(0, combo.Items.Count).FirstOrDefault(i => (combo.Items[i] as ChoiceDto)?.Value == selected);
            combo.EndUpdate();
        }
        Fill(subjectFilter, "Tất cả môn", catalog.Resources.FirstOrDefault(r => r.Key == "topics")?.Fields.FirstOrDefault(f => f.Name == "subjectId")?.Choices);
        Fill(topicFilter, "Tất cả chủ đề", definition.Fields.FirstOrDefault(f => f.Name == "topicId")?.Choices);
        Fill(difficultyFilter, "Tất cả độ khó", definition.Fields.FirstOrDefault(f => f.Name == "difficulty")?.Choices);
        add.Visible = definition.CanCreate;
        edit.Visible = resourceKey is not ("monitor" or "assignments" or "candidates");
        delete.Visible = definition.CanDelete;
        emptyAction.Visible = definition.CanCreate;
        foreach (Control old in resourceActions.Controls.Cast<Control>().ToArray()) old.Dispose();
        selectedActions.Clear();
        void Action(string label, string action, bool danger = false)
        {
            var button = Button(label, danger ? UiButtonKind.Danger : UiButtonKind.Secondary);
            button.Tag = action;
            button.Click += async (_, _) => await presenter.ActionAsync(action, label, danger);
            resourceActions.Controls.Add(button); selectedActions.Add(button);
        }
        if (resourceKey == "users")
        {
            Action("Duyệt đăng ký", "approve"); Action("Khóa tài khoản", "lock"); Action("Mở khóa", "unlock");
            var password = Button("Mật khẩu / vai trò"); password.Click += async (_, _) => await presenter.EditSelectedAsync();
            resourceActions.Controls.Add(password); selectedActions.Add(password);
            var import = Button("Nhập CSV sinh viên"); import.Tag = "studentImport";
            import.Visible = accountRole == "Student";
            import.Click += async (_, _) => await presenter.ImportAsync(); resourceActions.Controls.Add(import);
        }
        if (resourceKey == "exams") { Action("Xác nhận sẵn sàng", "ready"); Action("Chuyển về nháp", "draft"); }
        if (resourceKey == "exams")
        {
            var related = Button("Ngân hàng câu hỏi", UiButtonKind.Ghost);
            related.Click += (_, _) => NavigateRequested?.Invoke(this, "questions"); resourceActions.Controls.Add(related);
        }
        if (resourceKey == "sessions")
        {
            Action("Kích hoạt", "activate"); Action("Hoãn về nháp", "postpone"); Action("Hủy đợt", "cancel", true); Action("Công bố kết quả", "publish");
        }
        if (resourceKey is "sessions" or "monitor")
        {
            foreach (var format in new[] { "csv", "xlsx" })
            {
                var button = Button(format == "csv" ? "Xuất CSV" : "Xuất Excel");
                button.Click += async (_, _) => await presenter.ExportAsync(format);
                resourceActions.Controls.Add(button); selectedActions.Add(button);
            }
        }
        resourceActions.Visible = resourceActions.Controls.Count > 0;
        LayoutToolbar();
        UpdateSelectionActions();
    }

    /// <summary>Measures wrapped rows directly so a docked AutoSize card cannot grow into the data grid.</summary>
    private void LayoutToolbar()
    {
        if (arrangingToolbar || IsDisposed || toolbar.ClientSize.Width <= toolbar.Padding.Horizontal) return;
        arrangingToolbar = true;
        toolbar.SuspendLayout();
        try
        {
            var width = Math.Max(240, toolbar.ClientSize.Width - toolbar.Padding.Horizontal);
            var y = toolbar.Padding.Top;
            foreach (var row in new[] { primaryActions, filters, resourceActions })
            {
                var include = row == primaryActions || row.Controls.Count > 0;
                row.Visible = include;
                row.Dock = DockStyle.None; row.AutoSize = false; row.MaximumSize = Size.Empty;
                if (!include) { row.Height = 0; continue; }
                row.SetBounds(toolbar.Padding.Left, y, width, 1);
                row.PerformLayout();
                var height = row.Controls.Cast<Control>().Where(c => c.Visible).Select(c => c.Bottom + c.Margin.Bottom).DefaultIfEmpty(0).Max();
                row.Height = height;
                y += height;
            }
            toolbar.Height = Math.Max(80, y + toolbar.Padding.Bottom);
        }
        finally { toolbar.ResumeLayout(false); arrangingToolbar = false; }
    }

    public void RenderPage(AdminPageDto page)
    {
        var oldId = SelectedRecordId;
        binding = true;
        _ = presenter.SelectQuestionAsync(null);
        try
        {
            rowsById.Clear();
            var table = new DataTable(); table.Columns.Add("_id", typeof(int));
            foreach (var column in ColumnsFor(resourceKey)) table.Columns.Add(column.Key, typeof(string));
            foreach (var item in page.Items)
            {
                var id = Integer(item.GetValueOrDefault("id")); if (!id.HasValue) continue;
                rowsById[id.Value] = item;
                var row = table.NewRow(); row["_id"] = id.Value;
                foreach (var column in ColumnsFor(resourceKey)) row[column.Key] = Format(item, column.Key);
                table.Rows.Add(row);
            }
            source.DataSource = table;
            if (oldId.HasValue)
            {
                foreach (DataGridViewRow row in grid.Rows)
                    if (row.Cells["_id"].Value?.ToString() == oldId.ToString()) { grid.CurrentCell = row.Cells.Cast<DataGridViewCell>().First(c => c.OwningColumn?.Visible == true); break; }
            }
            pageLabel.Text = $"Trang {page.Page} / {Math.Max(1, (int)Math.Ceiling(page.Total / (double)page.PageSize))}";
            previous.Tag = page.Page > 1; next.Tag = page.Page * page.PageSize < page.Total;
            countLabel.Text = $"{(resourceKey == "users" ? accountRole == "Lecturer" ? "Giảng viên" : "Sinh viên" : title)} · {page.Total:N0}";
            loaded = true;
            var hasRows = table.Rows.Count > 0;
            empty.Visible = !hasRows; grid.Visible = hasRows;
            if (!hasRows)
            {
                var filtering = Query.IsActive.HasValue || !string.IsNullOrWhiteSpace(Query.Search) || new[] { Query.SubjectId, Query.TopicId, Query.Difficulty }.Any(v => !string.IsNullOrEmpty(v));
                emptyTitle.Text = filtering ? "Không tìm thấy dữ liệu phù hợp" : resourceKey == "monitor" ? "Chưa có thí sinh được phân công" : "Danh sách đang trống";
                emptyMessage.Text = filtering ? "Thử đổi từ khóa hoặc bộ lọc rồi tìm lại." : EmptyDescriptions.GetValueOrDefault(resourceKey, "Thêm dữ liệu đầu tiên để bắt đầu quản lý.");
                emptyAction.Visible = !filtering && resource?.CanCreate == true;
                empty.BringToFront();
            }
            status.Text = resourceKey == "monitor"
                ? "Dữ liệu thực từ máy chủ · tự cập nhật mỗi 15 giây · cập nhật " + VietNamNow()
                : "Cập nhật " + VietNamNow() + " · Chọn một dòng để xem hoặc thực hiện thao tác.";
        }
        finally { binding = false; }
        UpdateSelectionActions();
        _ = presenter.SelectQuestionAsync(SelectedRecordId);
    }

    public void SetBusy(bool value, string message)
    {
        busy = value; primaryActions.Enabled = filters.Enabled = resourceActions.Enabled = !value;
        grid.Enabled = !value; progress.Visible = value;
        emptyAction.Enabled = !value;
        if (value) progress.BringToFront();
        if (value) status.Text = message;
        else if (!loaded) status.Text = "Chưa tải được dữ liệu. Bấm Làm mới để thử lại.";
        previous.Enabled = !value && previous.Tag is true; next.Enabled = !value && next.Tag is true;
        UpdateSelectionActions();
    }

    public void Notify(string message, bool isError = false)
    {
        toast.ShowMessage(message, isError);
        if (isError) status.Text = "Không thực hiện được yêu cầu · có thể bấm Làm mới để thử lại.";
    }

    public void RenderQuestionDetail(Dictionary<string, JsonElement>? detail, string? message = null)
    {
        if (resourceKey != "questions" || IsDisposed) return;
        lastPreviewDetail = detail; lastPreviewMessage = message;
        previewContent.SuspendLayout();
        foreach (Control old in previewContent.Controls.Cast<Control>().ToArray()) old.Dispose();
        var width = Math.Max(200, previewCard.ClientSize.Width - previewCard.Padding.Horizontal - 24);
        renderedPreviewWidth = width;
        Label TextBlock(string text, Font? font = null, Color? color = null) => new()
        {
            Text = text, AutoSize = true, MaximumSize = new Size(width, 0), Font = font ?? Theme.BodyFont,
            ForeColor = color ?? Theme.Text, Margin = new Padding(0, 0, 0, 14), UseMnemonic = false
        };
        previewContent.Controls.Add(TextBlock("Chi tiết câu hỏi", Theme.SectionFont));
        var topicsLink = new LinkLabel { Text = "Quản lý chủ đề", AutoSize = true, LinkColor = Theme.Primary, Margin = new Padding(0, 0, 0, 14) };
        topicsLink.LinkClicked += (_, _) => NavigateRequested?.Invoke(this, "topics"); previewContent.Controls.Add(topicsLink);
        if (detail is null)
        {
            previewContent.Controls.Add(TextBlock(message ?? "Chọn một câu hỏi để xem nội dung và đáp án.", color: Theme.Muted));
            previewContent.ResumeLayout(); return;
        }
        string Read(string key) => detail.TryGetValue(key, out var value) ? value.ToString() : "";
        previewContent.Controls.Add(TextBlock(Read("topic"), Theme.SmallFont, Theme.Muted));
        previewContent.Controls.Add(TextBlock(Read("text")));
        int.TryParse(Read("correctIndex"), out var correct);
        for (var n = 1; n <= 4; n++)
        {
            var text = Read("option" + n); if (string.IsNullOrEmpty(text)) continue;
            var optionText = $"{(char)('A' + n - 1)}{(n == correct ? " · Đáp án đúng" : "")}\n{text}";
            var innerWidth = width - 24;
            var textHeight = Math.Max(44, TextRenderer.MeasureText(optionText, Theme.BodyFont, new Size(innerWidth, 10000),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 4);
            var option = new Label
            {
                Text = optionText, AutoSize = false, Size = new Size(width, textHeight + 24),
                MinimumSize = new Size(width, textHeight + 24), MaximumSize = new Size(width, textHeight + 24),
                Padding = new Padding(12), Margin = new Padding(0, 0, 0, 10),
                BackColor = n == correct ? Theme.SoftAccent : Theme.Background,
                ForeColor = n == correct ? Color.FromArgb(23, 103, 71) : Theme.Text,
                Font = Theme.BodyFont, UseMnemonic = false, TextAlign = ContentAlignment.TopLeft
            };
            previewContent.Controls.Add(option);
        }
        previewContent.Controls.Add(TextBlock("Độ khó: " + UiLabels.Of(Read("difficulty")), Theme.SmallFont, Theme.Muted));
        previewContent.ResumeLayout();
    }

    private void ConfigureColumns()
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "_id", DataPropertyName = "_id", Visible = false });
        foreach (var column in ColumnsFor(resourceKey))
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = column.Key, DataPropertyName = column.Key, HeaderText = column.Label,
                FillWeight = column.Weight, MinimumWidth = column.MinimumWidth, SortMode = DataGridViewColumnSortMode.Automatic
            });
        if (resourceKey == "users") grid.Columns["studentCode"]!.Visible = false;
        if (resourceKey == "classes")
            grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "_members", HeaderText = "Thành viên lớp", Text = "Chi tiết xem thành viên",
                UseColumnTextForLinkValue = true, FillWeight = 130, MinimumWidth = 180,
                LinkColor = Theme.Primary, ActiveLinkColor = Theme.Primary, VisitedLinkColor = Theme.Primary,
                TrackVisitedState = false, SortMode = DataGridViewColumnSortMode.NotSortable
            });
    }

    private void UpdateSelectionActions()
    {
        var selected = SelectedRecordId.HasValue;
        edit.Enabled = delete.Enabled = selected && !busy;
        foreach (var button in selectedActions) button.Enabled = selected && !busy;
        if (!selected || !rowsById.TryGetValue(SelectedRecordId!.Value, out var row)) return;
        var state = row.GetValueOrDefault("status")?.ToString();
        if (resourceKey is "exams" or "sessions") edit.Enabled = !busy && state == "Draft";
        foreach (var button in selectedActions)
        {
            var action = button.Tag?.ToString();
            if (action == "approve") button.Enabled = !busy && !string.IsNullOrEmpty(row.GetValueOrDefault("requestedRole")?.ToString());
            if (action is "ready" or "activate") button.Enabled = !busy && state == "Draft";
            if (action == "draft") button.Enabled = !busy && state == "Ready";
            if (action is "postpone" or "cancel") button.Enabled = !busy && state is "Draft" or "Scheduled";
            if (action == "publish") button.Enabled = !busy && state == "Closed";
        }
    }

    private void PaintBadge(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name is not ("status" or "isActive" or "published" or "difficulty" or "requestedRole")) return;
        var text = e.FormattedValue?.ToString(); if (string.IsNullOrWhiteSpace(text) || text == "—") return;
        e.PaintBackground(e.ClipBounds, true);
        var isNegative = text is "Không" or "Đã hủy" or "Khó";
        var isWarning = text is "Nháp" or "Đang làm" or "Trung bình";
        var ink = isNegative ? Theme.Danger : isWarning ? Color.FromArgb(132, 86, 15) : Theme.Primary;
        var fill = isNegative ? Color.FromArgb(253, 236, 236) : isWarning ? Color.FromArgb(255, 247, 225) : Theme.SoftPrimary;
        var measured = TextRenderer.MeasureText(text, Theme.SmallFont);
        var bounds = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y + Math.Max(5, (e.CellBounds.Height - 26) / 2), Math.Min(e.CellBounds.Width - 20, measured.Width + 16), 26);
        if (bounds.Width <= 0) return;
        using var path = Rounded(bounds, 6); using var brush = new SolidBrush(fill);
        e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(e.Graphics, text, Theme.SmallFont, bounds, ink, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
        e.Handled = true;
    }

    private static GraphicsPath Rounded(Rectangle rect, int radius)
    {
        var path = new GraphicsPath(); var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90); path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90); path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90); path.CloseFigure(); return path;
    }

    private static string Format(Dictionary<string, object?> row, string key)
    {
        if (key == "questionCount")
        {
            var ids = row.GetValueOrDefault("questionIds");
            if (ids is JsonElement e && e.ValueKind == JsonValueKind.Array) return e.GetArrayLength().ToString();
            if (ids is Array array) return array.Length.ToString();
            return "—";
        }
        var value = row.GetValueOrDefault(key); var text = value?.ToString() ?? "";
        if (text.Length == 0) return "—";
        if (key is "opensAt" or "closesAt" or "submittedAt" or "publishedAt" && DateTimeOffset.TryParse(text, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var time))
            return TimeZoneInfo.ConvertTime(time, VietNamTime.Zone).ToString("dd/MM/yyyy HH:mm");
        if (key is "role" or "requestedRole") return string.Join(", ", text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(UiLabels.Of));
        if (key is "status") return text == "Scheduled" ? "Sắp mở" : UiLabels.Of(text);
        if (key is "difficulty" or "isActive" or "published") return UiLabels.Of(text);
        if (key == "score" && decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var number)) return number.ToString("0.##");
        return text;
    }

    private static int? Integer(object? value) => int.TryParse(value?.ToString(), out var id) ? id : null;
    private static string? Value(ComboBox combo) => (combo.SelectedItem as ChoiceDto)?.Value;
    private static string VietNamNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietNamTime.Zone).ToString("HH:mm:ss");
    private static UiButton Button(string text, UiButtonKind kind = UiButtonKind.Secondary) => new() { Text = text, Kind = kind, AutoSize = true, Margin = new Padding(0, 0, 8, 8) };

    private sealed record Column(string Key, string Label, float Weight = 100, int MinimumWidth = 100);
    private static IReadOnlyList<Column> ColumnsFor(string key) => key switch
    {
        "users" => [new("userName", "Tài khoản", 110, 130), new("fullName", "Họ và tên", 150, 170), new("requestedRole", "Vai trò đăng ký", 100, 140), new("isActive", "Hoạt động", 65, 100), new("role", "Vai trò hiện tại", 120, 150), new("lecturerCode", "Mã giảng viên", 85, 120), new("studentCode", "Mã sinh viên", 80, 115)],
        "subjects" => [new("code", "Mã môn", 70, 110), new("name", "Tên môn học", 200, 220), new("isActive", "Hoạt động", 70, 110)],
        "classes" => [new("code", "Mã lớp"), new("name", "Tên lớp", 180, 170), new("subject", "Môn học", 130, 140),
            new("memberCount", "Thành viên", 65, 85), new("isActive", "Hoạt động", 75, 90)],
        "topics" => [new("subject", "Môn học", 140, 180), new("name", "Tên chủ đề", 200, 220), new("isActive", "Hoạt động", 70, 110)],
        "assignments" => [new("lecturer", "Giảng viên", 150, 200), new("subject", "Môn học được phân công", 200, 250)],
        "candidates" => [new("session", "Đợt thi", 180, 230), new("student", "Thí sinh", 150, 200)],
        "questions" => [new("text", "Nội dung câu hỏi", 220, 230), new("topic", "Chủ đề", 110, 130), new("difficulty", "Độ khó", 70, 105), new("isActive", "Sử dụng", 60, 90)],
        "exams" => [new("title", "Tên đề thi", 180, 220), new("subject", "Môn học", 120, 170), new("questionCount", "Số câu", 50, 85), new("status", "Trạng thái", 80, 120)],
        "sessions" => [new("title", "Tên đợt thi", 180, 200), new("exam", "Đề thi", 120, 160), new("opensAt", "Mở lúc · giờ VN", 110, 155), new("closesAt", "Đóng lúc · giờ VN", 110, 155), new("durationMinutes", "Phút", 45, 70), new("status", "Trạng thái", 80, 115), new("published", "Công bố", 65, 100)],
        "monitor" => [new("session", "Đợt thi", 170, 200), new("student", "Thí sinh", 130, 180), new("studentCode", "Mã thí sinh", 80, 115), new("status", "Bài thi", 85, 120), new("score", "Điểm", 45, 75), new("submittedAt", "Nộp lúc · giờ VN", 110, 155), new("published", "Công bố", 65, 100)],
        _ => [new("name", "Tên", 200, 250)]
    };

    private static readonly Dictionary<string, string> Titles = new()
    {
        ["users"] = "Tài khoản & đăng ký", ["subjects"] = "Môn học", ["classes"] = "Quản lý lớp", ["topics"] = "Chủ đề", ["questions"] = "Ngân hàng câu hỏi", ["exams"] = "Đề thi", ["sessions"] = "Đợt thi", ["assignments"] = "Phân công giảng viên", ["candidates"] = "Phân công thí sinh", ["monitor"] = "Theo dõi & kết quả"
    };
    private static readonly Dictionary<string, string> EmptyDescriptions = new()
    {
        ["topics"] = "Tạo chủ đề trong môn được phân công để sắp xếp ngân hàng câu hỏi.",
        ["questions"] = "Thêm câu hỏi vào chủ đề đã có. Mỗi câu có đúng một đáp án đúng.",
        ["exams"] = "Tạo đề từ câu hỏi đang sử dụng, sau đó xác nhận sẵn sàng.",
        ["sessions"] = "Tạo đợt từ đề sẵn sàng để lên lịch và phân công thí sinh.",
        ["monitor"] = "Thông tin tham gia sẽ xuất hiện sau khi đợt có thí sinh được phân công."
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing) { presenter?.Dispose(); source.Dispose(); }
        base.Dispose(disposing);
    }
}
