using System.Drawing.Drawing2D;

namespace Exam.Desktop;

public sealed class DashboardView : UserControl, IWorkspaceView
{
    private readonly DashboardPresenter presenter;
    private readonly string role;
    private readonly Panel scroll = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Label feedback = new() { Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleLeft };
    private bool loading;
    public string Title => "Tổng quan";
    public event EventHandler<string>? NavigateRequested;

    public DashboardView(ExamApiClient api)
    {
        presenter = new(api); role = api.Role ?? "";
        BackColor = Theme.Background; Font = Theme.BodyFont;
        Controls.Add(scroll); Controls.Add(feedback); feedback.ForeColor = Theme.Muted;
    }
    public Task LoadAsync() => RefreshAsync();
    public async Task RefreshAsync()
    {
        if (loading) return;
        loading = true; feedback.Text = "Đang tải không gian làm việc của bạn…"; feedback.ForeColor = Theme.Muted;
        try
        {
            var snapshot = await presenter.LoadAsync();
            if (IsDisposed) return;
            Render(snapshot);
            feedback.Text = $"Dữ liệu theo vai trò {UiLabels.Of(role)}  •  Cập nhật lúc {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietNamTime.Zone):HH:mm:ss}";
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            feedback.ForeColor = Theme.Danger; feedback.Text = "Không tải được tổng quan. Nhấn F5 để thử lại.";
            ClearContent();
            var card = new UiCard { Dock = DockStyle.Top, Height = 170 };
            var label = new Label { Text = ex.Message, Dock = DockStyle.Fill, ForeColor = Theme.Text, Font = Theme.BodyFont };
            var retry = new UiButton { Text = "Thử tải lại", Dock = DockStyle.Bottom, Kind = UiButtonKind.Secondary };
            retry.Click += async (_, _) => await RefreshAsync();
            card.Controls.Add(label); card.Controls.Add(retry); scroll.Controls.Add(card);
        }
        finally { loading = false; }
    }
    private void ClearContent()
    {
        foreach (Control old in scroll.Controls.Cast<Control>().ToArray()) old.Dispose();
        scroll.Controls.Clear();
    }
    private void Render(DashboardSnapshot snapshot)
    {
        ClearContent();
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 0, 0, 16) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 278));
        var welcome = new UiCard { Dock = DockStyle.Fill, Height = 142, Margin = new Padding(0, 0, 0, 16), BackColor = Theme.SoftPrimary };
        var welcomeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        welcomeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68)); welcomeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        var copy = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        copy.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); copy.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        copy.Controls.Add(new Label { Text = snapshot.Heading, Dock = DockStyle.Fill, Font = Theme.SectionFont, ForeColor = Theme.Primary, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        copy.Controls.Add(new Label { Text = snapshot.Description, Dock = DockStyle.Fill, Font = Theme.BodyFont, ForeColor = Theme.Text }, 0, 1);
        var next = new UiButton { Text = snapshot.NextStep, Kind = UiButtonKind.Primary, AutoSize = false, Dock = DockStyle.Fill, Margin = new Padding(16, 22, 0, 22) };
        next.Click += (_, _) => NavigateRequested?.Invoke(this, snapshot.NextKey);
        welcomeLayout.Controls.Add(copy, 0, 0); welcomeLayout.Controls.Add(next, 1, 0); welcome.Controls.Add(welcomeLayout); layout.Controls.Add(welcome, 0, 0);
        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 122, ColumnCount = 4, Margin = new Padding(0, 0, 0, 16) };
        for (var i = 0; i < snapshot.Metrics.Count; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            var metric = snapshot.Metrics[i];
            var card = new UiCard { Dock = DockStyle.Fill, Margin = new Padding(i == 0 ? 0 : 6, 0, i == 3 ? 0 : 6, 0), Padding = new Padding(16, 12, 12, 12) };
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            content.Controls.Add(new Label { Text = metric.Label, Font = Theme.SmallFont, ForeColor = Theme.Muted, Dock = DockStyle.Fill });
            content.Controls.Add(new Label { Text = metric.Value, Font = Theme.TitleFont, ForeColor = Theme.Primary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            content.Controls.Add(new Label { Text = metric.Description, Font = Theme.SmallFont, ForeColor = Theme.Muted, Dock = DockStyle.Fill, AutoEllipsis = true });
            card.Controls.Add(content); cards.Controls.Add(card, i, 0);
        }
        layout.Controls.Add(cards, 0, 1);
        var bottom = new TableLayoutPanel { Dock = DockStyle.Top, Height = 278, ColumnCount = 2, Margin = Padding.Empty };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        var recent = new UiCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        var heading = new Label { Text = snapshot.RecentTitle, Font = Theme.SectionFont, Dock = DockStyle.Top, Height = 38, ForeColor = Theme.Text };
        var list = new UiDataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
        list.Columns.Add("title", "Nội dung"); list.Columns.Add("subtitle", "Thông tin"); list.Columns.Add("status", "Trạng thái");
        list.Columns[0].FillWeight = 46; list.Columns[1].FillWeight = 30; list.Columns[2].FillWeight = 24;
        foreach (var item in snapshot.Recent) list.Rows.Add(item.Title, item.Subtitle, item.Status);
        recent.Controls.Add(list); recent.Controls.Add(heading);
        if (snapshot.Recent.Count == 0)
        {
            list.Visible = false;
            recent.Controls.Add(new Label { Text = "Chưa có dữ liệu.\nDùng thao tác gợi ý phía trên để bắt đầu.", Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = Theme.BodyFont, TextAlign = ContentAlignment.MiddleCenter });
            heading.BringToFront();
        }
        var insight = new UiCard { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0) };
        if (role == "Administrator")
            insight.Controls.Add(new Label { Text = "Quy trình chuẩn bị\n\n01  Tạo hoặc duyệt tài khoản\n\n02  Phân công giảng viên theo môn\n\n03  Cán bộ tổ chức đợt thi\n\nMỗi người có một không gian làm việc đúng với vai trò được giao.", Font = Theme.BodyFont, ForeColor = Theme.Text, Dock = DockStyle.Fill });
        else insight.Controls.Add(new ScoreDistributionView(snapshot.ScoreBins, snapshot.GradedCount, snapshot.AverageScore) { Dock = DockStyle.Fill });
        bottom.Controls.Add(recent, 0, 0); bottom.Controls.Add(insight, 1, 0); layout.Controls.Add(bottom, 0, 2); scroll.Controls.Add(layout);
    }
    public void FocusSearch() { }
    public Task AddNewAsync() => Task.CompletedTask;
}

internal sealed class ScoreDistributionView(IReadOnlyList<int> bins, int count, decimal? average) : Control
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        TextRenderer.DrawText(e.Graphics, "Phân bố điểm", Theme.SectionFont, new Rectangle(0, 0, Width, 32), Theme.Text, TextFormatFlags.Left);
        TextRenderer.DrawText(e.Graphics, count == 0 ? "Chưa có bài đã chấm" : $"{count} bài  •  Trung bình {average:0.##}/10", Theme.SmallFont, new Rectangle(0, 35, Width, 28), Theme.Muted, TextFormatFlags.Left);
        if (count == 0)
        {
            TextRenderer.DrawText(e.Graphics, "Biểu đồ sẽ xuất hiện khi\ncó kết quả từ máy chủ.", Theme.BodyFont, new Rectangle(0, 84, Width, 80), Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak); return;
        }
        var top = 83; var available = Math.Max(35, Height - top - 39); var width = Math.Max(14, (Width - 20) / 5); var maximum = Math.Max(1, bins.Max());
        string[] labels = ["0–<2", "2–<4", "4–<6", "6–<8", "8–10"];
        for (var i = 0; i < 5; i++)
        {
            var height = (int)((available - 24) * bins[i] / (double)maximum); var left = i * width + 5;
            using var brush = new SolidBrush(i == 4 ? Theme.Accent : Theme.Primary);
            e.Graphics.FillRectangle(brush, left, top + available - height, Math.Max(5, width - 12), height);
            TextRenderer.DrawText(e.Graphics, bins[i].ToString(), Theme.SmallFont, new Rectangle(left - 4, top + available - height - 22, width, 22), Theme.Text, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(e.Graphics, labels[i], Theme.SmallFont, new Rectangle(left - 6, top + available + 6, width, 22), Theme.Muted, TextFormatFlags.HorizontalCenter);
        }
    }
}
