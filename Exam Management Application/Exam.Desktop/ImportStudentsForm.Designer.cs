namespace Exam.Desktop;

public sealed partial class ImportStudentsForm
{
    private UiDataGridView preview = null!;
    private Label status = null!;
    private UiButton import = null!;
    private UiButton choose = null!;
    private UiButton template = null!;
    private TableLayoutPanel footer = null!;
    private FlowLayoutPanel actions = null!;
    private TableLayoutPanel header = null!;
    private Label heading = null!;
    private Label description = null!;
    private FlowLayoutPanel toolbar = null!;
    private Label instructions = null!;
    private UiCard card = null!;
    private Panel content = null!;
    private UiButton close = null!;

    private void InitializeComponent()
    {
        SuspendLayout();
        import = new UiButton() { Text = "Kiểm tra và nhập", Kind = UiButtonKind.Primary, AutoSize = true, Enabled = false };
        status = new Label() { Dock = DockStyle.Fill, AutoSize = true, ForeColor = Theme.Muted, Padding = new Padding(0, 8, 16, 0), Text = "Chọn tệp CSV để xem trước. Chưa có dữ liệu được nhập." };
        preview = new UiDataGridView() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        header = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(24, 20, 24, 12), BackColor = Theme.Surface };
        heading = new Label { Text = "Nhập danh sách thí sinh", Font = Theme.SectionFont, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        header.Controls.Add(heading);
        description = new Label { AutoSize = true, ForeColor = Theme.Muted, Text = "Chọn tệp → xem trước → máy chủ kiểm tra → xác nhận nhập", Margin = new Padding(0, 0, 0, 4) };
        header.Controls.Add(description);
        toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(24, 16, 24, 8) };
        choose = new UiButton { Text = "Chọn tệp CSV UTF-8", Kind = UiButtonKind.Secondary, AutoSize = true };
        template = new UiButton { Text = "Tải mẫu CSV", Kind = UiButtonKind.Ghost, AutoSize = true };
        toolbar.Controls.Add(choose);
        toolbar.Controls.Add(template);

        instructions = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(24, 0, 24, 12), ForeColor = Theme.Muted, Font = Theme.SmallFont, Text = "Cột bắt buộc: UserName,FullName,StudentCode,Email,Password\nCSV UTF-8 · tối đa 500 thí sinh / 1 MB · mật khẩu luôn được ẩn trong bản xem trước." };
        card = new UiCard { Dock = DockStyle.Fill, Padding = new Padding(16), Margin = new Padding(24) };
        card.Controls.Add(preview);
        content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 16), AutoScroll = true };
        content.Controls.Add(card);
        footer = new TableLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 2, Padding = new Padding(24, 12, 24, 12), BackColor = Theme.Surface };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        close = new UiButton { Text = "Đóng", Kind = UiButtonKind.Secondary, AutoSize = true, DialogResult = DialogResult.Cancel, CausesValidation = false };
        actions.Controls.Add(close);
        actions.Controls.Add(import);
        footer.Controls.Add(status, 0, 0);
        footer.Controls.Add(actions, 1, 0);

        Controls.Add(content);
        Controls.Add(instructions);
        Controls.Add(toolbar);
        Controls.Add(header);
        Controls.Add(footer);
        CancelButton = close;

        Text = "Nhập danh sách thí sinh từ CSV";
        ClientSize = new Size(1024, 681);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = new Font("Arial", 10F);
        ResumeLayout(false);
        PerformLayout();
    }
}
