using System.Drawing.Drawing2D;
using Exam.Core;

namespace Exam.Desktop;

/// <summary>Application shell: role navigation and lifecycle only. Workspaces and presenters own screen interaction.</summary>
public sealed class MainForm : Form
{
    private readonly ExamApiClient api;
    private readonly TableLayoutPanel frame = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    private readonly Panel sidebar = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel navigation = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(12, 4, 12, 8) };
    private readonly Panel workspace = new() { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 8) };
    private readonly Label heading = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
    private readonly Label breadcrumb = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
    private readonly Label brandName = new() { Text = "Exam", AutoSize = true, ForeColor = Color.White };
    private readonly Label brandSubtitle = new() { Text = "Không gian khảo thí", AutoSize = true, ForeColor = Color.White };
    private readonly UiToast toast = new() { Dock = DockStyle.Bottom };
    private readonly Dictionary<string, AcademicNavigationButton> menu = new();
    private readonly ToolTip hints = new();
    private readonly NavigationService navigator;
    private AdminCatalogDto? catalog;
    private bool compact;
    private bool navigating;
    private bool signingOut;
    private bool canClose;
    public string? LogoutNotice { get; private set; }

    public MainForm(ExamApiClient api)
    {
        this.api = api;
        navigator = new NavigationService(workspace, CreateView);
        Theme.Apply(this);
        Text = "Exam • Không gian khảo thí";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1366, 820);
        MinimumSize = new Size(1024, 680);
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        frame.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 244));
        frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.BackColor = Theme.Primary;
        var brand = new TableLayoutPanel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(24, 24, 12, 20), ColumnCount = 1, RowCount = 2 };
        brandName.Font = new Font("Segoe UI", 26, FontStyle.Bold);
        brandSubtitle.Font = Theme.BodyFont;
        brand.Controls.Add(brandName); brand.Controls.Add(brandSubtitle);
        var sidebarFooter = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 116, Padding = new Padding(12), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var help = new AcademicNavigationButton("Hướng dẫn", "\uE897");
        help.Click += (_, _) => { using var dialog = new HelpForm(api.Role); dialog.ShowDialog(this); };
        var logout = new AcademicNavigationButton("Đăng xuất", "\uE8AC");
        logout.Click += (_, _) => Close();
        sidebarFooter.Controls.Add(help); sidebarFooter.Controls.Add(logout);
        sidebar.Controls.Add(navigation); sidebar.Controls.Add(sidebarFooter); sidebar.Controls.Add(brand);
        var right = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 104, ColumnCount = 3, Padding = new Padding(24, 12, 24, 12), BackColor = Theme.Surface };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var toggle = new UiButton { Text = "\uE700", Font = new Font("Segoe MDL2 Assets", 16), Kind = UiButtonKind.Ghost, Width = 40, Height = 40, AccessibleName = "Thu gọn hoặc mở thanh điều hướng", Margin = new Padding(0, 16, 12, 0) };
        toggle.Click += (_, _) => ToggleSidebar();
        var title = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        title.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); title.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Font = Theme.TitleFont; heading.ForeColor = Theme.Text;
        breadcrumb.Text = "Không gian làm việc"; breadcrumb.Font = Theme.SmallFont; breadcrumb.ForeColor = Theme.Muted;
        title.Controls.Add(breadcrumb); title.Controls.Add(heading);
        var identity = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 12, 0, 0) };
        var displayName = string.IsNullOrWhiteSpace(api.DisplayName) ? UiLabels.Of(api.Role ?? "") : api.DisplayName;
        var initials = string.Concat(displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeLast(2).Select(x => x[0]));
        identity.Controls.Add(new AcademicAvatar(initials) { Width = 44, Height = 44, Margin = new Padding(0, 4, 10, 0) });
        identity.Controls.Add(new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = displayName, Font = Theme.SmallFont, ForeColor = Theme.Text, MaximumSize = new Size(200, 0) });
        header.Controls.Add(toggle, 0, 0); header.Controls.Add(title, 1, 0); header.Controls.Add(identity, 2, 0);
        right.Controls.Add(workspace); right.Controls.Add(toast); right.Controls.Add(header);
        frame.Controls.Add(sidebar, 0, 0); frame.Controls.Add(right, 1, 0); Controls.Add(frame);
        navigation.SizeChanged += (_, _) => ResizeNavigation();
        sidebarFooter.SizeChanged += (_, _) => { help.Width = logout.Width = Math.Max(48, sidebarFooter.ClientSize.Width - 24); help.Compact = logout.Compact = compact; };
        Shown += async (_, _) => await InitializeAsync();
        FormClosing += OnClosingAsync;
        KeyDown += async (_, e) =>
        {
            if (navigator.Current is not IWorkspaceView view || navigating) return;
            if (e.KeyCode == Keys.F5) { e.SuppressKeyPress = true; await RunAsync(view.RefreshAsync); }
            else if (e.Control && e.KeyCode == Keys.F) { e.SuppressKeyPress = true; await FocusCurrentSearchAsync(); }
            else if (e.Control && e.KeyCode == Keys.N) { e.SuppressKeyPress = true; await RunAsync(view.AddNewAsync); }
        };
    }

    public async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            catalog = await api.CatalogAsync();
            navigation.Controls.Clear(); menu.Clear();
            AddMenu("dashboard", "Tổng quan", "\uE80F");
            foreach (var resource in catalog.Resources)
                AddMenu(resource.Key, ScreenTitle(resource.Key, resource.Label), Theme.IconFor(resource.Key));
            ResizeNavigation();
        });
        if (catalog != null) await OpenWorkspaceAsync("dashboard");
    }

    private void AddMenu(string key, string text, string glyph)
    {
        var button = new AcademicNavigationButton(text, glyph) { Tag = key, Compact = compact };
        button.Click += async (_, _) => await OpenWorkspaceAsync(key);
        hints.SetToolTip(button, text); menu[key] = button; navigation.Controls.Add(button);
    }

    public async Task OpenWorkspaceAsync(string key)
    {
        if (navigating) return;
        navigating = true; navigation.Enabled = false;
        try
        {
            if (key != "dashboard" && catalog?.Resources.All(r => r.Key != key) != false)
                throw new InvalidOperationException("Chức năng không thuộc vai trò hiện tại.");
            heading.Text = key == "dashboard" ? "Tổng quan" : ScreenTitle(key, catalog!.Resources.First(r => r.Key == key).Label);
            breadcrumb.Text = $"Exam / {UiLabels.Of(api.Role ?? "")} / Không gian làm việc";
            foreach (var pair in menu) pair.Value.Selected = pair.Key == key;
            await navigator.NavigateAsync(key);
        }
        catch (Exception ex) { toast.ShowMessage(ex.Message, true); }
        finally { navigating = false; navigation.Enabled = true; }
    }

    private UserControl CreateView(string key)
    {
        if (key == "dashboard")
        {
            var dashboard = new DashboardView(api);
            dashboard.NavigateRequested += async (_, target) => await OpenWorkspaceAsync(target);
            return dashboard;
        }
        ResourceView view = key switch
        {
            "questions" => new QuestionBankView(api), "topics" => new TopicsView(api),
            "exams" => new ExamPapersView(api), "sessions" => new ExamSessionsView(api),
            "users" => new UsersView(api), "subjects" => new SubjectsView(api),
            "assignments" => new AssignmentsView(api), "candidates" => new CandidatesView(api),
            "monitor" => new MonitorView(api), _ => throw new InvalidOperationException("Không tìm thấy màn hình.")
        };
        view.NavigateRequested += async (_, target) => await OpenWorkspaceAsync(target);
        return view;
    }

    private void ToggleSidebar()
    {
        compact = !compact;
        frame.ColumnStyles[0].Width = compact ? 76 : 244;
        brandName.Visible = brandSubtitle.Visible = !compact;
        foreach (var button in sidebar.Controls.OfType<FlowLayoutPanel>().SelectMany(f => f.Controls.OfType<AcademicNavigationButton>())) button.Compact = compact;
        ResizeNavigation();
    }
    private async Task FocusCurrentSearchAsync()
    {
        if (navigator.CurrentKey == "dashboard")
        {
            var key = api.Role switch { "Administrator" => "users", "Lecturer" => "questions", _ => "sessions" };
            await OpenWorkspaceAsync(key);
        }
        (navigator.Current as IWorkspaceView)?.FocusSearch();
    }
    private void ResizeNavigation()
    {
        foreach (var button in menu.Values) { button.Width = Math.Max(44, navigation.ClientSize.Width - 28); button.Compact = compact; }
    }
    private async Task RunAsync(Func<Task> action)
    {
        try { await action(); } catch (Exception ex) { toast.ShowMessage(ex.Message, true); }
    }
    private async void OnClosingAsync(object? sender, FormClosingEventArgs e)
    {
        if (canClose) return;
        e.Cancel = true;
        if (signingOut) return;
        signingOut = true; Enabled = false;
        try { await api.LogoutAsync(); }
        catch (Exception) { LogoutNotice = "Đã xóa token trên máy này; máy chủ chưa xác nhận thu hồi phiên."; }
        finally { canClose = true; Close(); }
    }
    private static string ScreenTitle(string key, string fallback) => key switch
    {
        "questions" => "Ngân hàng câu hỏi", "topics" => "Chủ đề học tập", "exams" => "Đề thi",
        "sessions" => "Tổ chức đợt thi", "users" => "Người dùng & phân quyền", "subjects" => "Danh mục môn học",
        "assignments" => "Phân công giảng viên", "candidates" => "Danh sách dự thi", "monitor" => "Theo dõi & kết quả", _ => fallback
    };
    protected override void Dispose(bool disposing)
    {
        if (disposing) { navigator?.Dispose(); hints.Dispose(); }
        base.Dispose(disposing);
    }
}

internal sealed class AcademicNavigationButton : Button
{
    private bool selected;
    private bool compact;
    private bool hover;
    private readonly string glyph;
    [System.ComponentModel.DefaultValue(false)]
    public bool Selected { get => selected; set { selected = value; Invalidate(); } }
    [System.ComponentModel.DefaultValue(false)]
    public bool Compact { get => compact; set { compact = value; Invalidate(); } }
    public AcademicNavigationButton(string text, string glyph)
    {
        Text = text; this.glyph = glyph; AccessibleName = text;
        Width = 216; Height = text.Length > 22 ? 62 : 48; Margin = new Padding(0, 0, 0, 6);
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand;
        Font = Theme.BodyFont; SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Theme.Primary);
        var foreground = selected ? Theme.Primary : Color.White;
        using var path = Theme.Rounded(new RectangleF(0, 0, Width - 1, Height - 1), 7);
        using var background = new SolidBrush(selected ? Color.White : hover ? Theme.PrimaryHover : Theme.Primary);
        e.Graphics.FillPath(background, path);
        using var iconFont = new Font("Segoe MDL2 Assets", 17);
        TextRenderer.DrawText(e.Graphics, glyph, iconFont, new Rectangle(10, 0, 30, Height), foreground, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        if (!compact)
        {
            var flags = TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
            var textSize = TextRenderer.MeasureText(Text, Theme.BodyFont, new Size(Math.Max(1, Width - 53), Height), flags);
            TextRenderer.DrawText(e.Graphics, Text, Theme.BodyFont,
                new Rectangle(47, Math.Max(4, (Height - textSize.Height) / 2), Math.Max(1, Width - 53), Height - 8), foreground, flags);
        }
        if (Focused && ShowFocusCues)
        {
            using var focusPath = Theme.Rounded(new RectangleF(4, 4, Width - 9, Height - 9), 5 * DeviceDpi / 96F);
            using var focusPen = new Pen(foreground) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawPath(focusPen, focusPath);
        }
    }
}

internal sealed class AcademicAvatar(string initials) : Control
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var background = new SolidBrush(Theme.SoftPrimary); e.Graphics.FillEllipse(background, 1, 1, Width - 2, Height - 2);
        TextRenderer.DrawText(e.Graphics, initials, Theme.BodyFont, ClientRectangle, Theme.Primary, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
    }
}
