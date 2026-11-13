#nullable disable
namespace Exam.Desktop;

partial class MainForm
{
    private TableLayoutPanel frame;
    private Panel sidebar;
    private FlowLayoutPanel navigation;
    private Panel workspace;
    private Label heading;
    private Label breadcrumb;
    private Label brandName;
    private Label brandSubtitle;
    private UiToast toast;
    private TableLayoutPanel brand;
    private FlowLayoutPanel sidebarFooter;
    private AcademicNavigationButton help;
    private AcademicNavigationButton logout;
    private Panel right;
    private TableLayoutPanel header;
    private UiButton toggle;
    private TableLayoutPanel title;
    private UiButton quickSearch;
    private TableLayoutPanel identity;
    private Label footer;
    private AcademicAvatar avatar;
    private Label accountLabel;

    private void InitializeComponent()
    {
        frame = new TableLayoutPanel();
        sidebar = new Panel();
        navigation = new FlowLayoutPanel();
        sidebarFooter = new FlowLayoutPanel();
        help = new AcademicNavigationButton();
        logout = new AcademicNavigationButton();
        avatar = new AcademicAvatar();
        brand = new TableLayoutPanel();
        brandName = new Label();
        brandSubtitle = new Label();
        right = new Panel();
        workspace = new Panel();
        toast = new UiToast();
        footer = new Label();
        header = new TableLayoutPanel();
        toggle = new UiButton();
        title = new TableLayoutPanel();
        breadcrumb = new Label();
        heading = new Label();
        quickSearch = new UiButton();
        identity = new TableLayoutPanel();
        accountLabel = new Label();
        frame.SuspendLayout();
        sidebar.SuspendLayout();
        brand.SuspendLayout();
        right.SuspendLayout();
        header.SuspendLayout();
        title.SuspendLayout();
        identity.SuspendLayout();
        SuspendLayout();
        // 
        // frame
        // 
        frame.ColumnCount = 2;
        frame.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 244F));
        frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        frame.Controls.Add(sidebar, 0, 0);
        frame.Controls.Add(right, 1, 0);
        frame.Dock = DockStyle.Fill;
        frame.Location = new Point(0, 0);
        frame.Margin = new Padding(0);
        frame.Name = "frame";
        frame.RowCount = 1;
        frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        frame.Size = new Size(1366, 820);
        frame.TabIndex = 0;
        // 
        // sidebar
        // 
        sidebar.BackColor = Color.FromArgb(30, 78, 140);
        sidebar.Controls.Add(navigation);
        sidebar.Controls.Add(sidebarFooter);
        sidebar.Controls.Add(brand);
        sidebar.Dock = DockStyle.Fill;
        sidebar.Location = new Point(3, 3);
        sidebar.Name = "sidebar";
        sidebar.Size = new Size(238, 814);
        sidebar.TabIndex = 0;
        // 
        // navigation
        // 
        navigation.AutoScroll = true;
        navigation.Dock = DockStyle.Fill;
        navigation.FlowDirection = FlowDirection.TopDown;
        navigation.Location = new Point(0, 120);
        navigation.Name = "navigation";
        navigation.Padding = new Padding(12, 4, 12, 8);
        navigation.Size = new Size(238, 578);
        navigation.TabIndex = 0;
        navigation.WrapContents = false;
        // 
        // sidebarFooter
        // 
        help.Text = "Hướng dẫn";
        help.Glyph = "?";
        logout.Text = "Đăng xuất";
        logout.Glyph = "↪";
        sidebarFooter.Controls.Add(help);
        sidebarFooter.Controls.Add(logout);
        sidebarFooter.Dock = DockStyle.Bottom;
        sidebarFooter.FlowDirection = FlowDirection.TopDown;
        sidebarFooter.Location = new Point(0, 698);
        sidebarFooter.Name = "sidebarFooter";
        sidebarFooter.Padding = new Padding(12);
        sidebarFooter.Size = new Size(238, 116);
        sidebarFooter.TabIndex = 1;
        sidebarFooter.WrapContents = false;
        // 
        // brand
        // 
        brand.ColumnCount = 1;
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        brand.Controls.Add(brandName, 0, 0);
        brand.Controls.Add(brandSubtitle, 0, 1);
        brand.Dock = DockStyle.Top;
        brand.Location = new Point(0, 0);
        brand.Name = "brand";
        brand.Padding = new Padding(24, 24, 12, 20);
        brand.RowCount = 2;
        brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        brand.Size = new Size(238, 120);
        brand.TabIndex = 2;
        // 
        // brandName
        // 
        brandName.AutoSize = true;
        brandName.Font = new Font("Arial", 26F, FontStyle.Bold);
        brandName.ForeColor = Color.White;
        brandName.Location = new Point(27, 24);
        brandName.Name = "brandName";
        brandName.Size = new Size(139, 20);
        brandName.TabIndex = 0;
        brandName.Text = "Exam";
        brandName.Click += brandName_Click;
        // 
        // brandSubtitle
        // 
        brandSubtitle.AutoSize = true;
        brandSubtitle.Font = new Font("Arial", 10F);
        brandSubtitle.ForeColor = Color.White;
        brandSubtitle.Location = new Point(27, 44);
        brandSubtitle.Name = "brandSubtitle";
        brandSubtitle.Size = new Size(155, 19);
        brandSubtitle.TabIndex = 1;
        brandSubtitle.Text = "Không gian khảo thí";
        // 
        // right
        // 
        right.BackColor = Color.FromArgb(245, 247, 250);
        right.Controls.Add(workspace);
        right.Controls.Add(toast);
        right.Controls.Add(footer);
        right.Controls.Add(header);
        right.Dock = DockStyle.Fill;
        right.Location = new Point(247, 3);
        right.Name = "right";
        right.Size = new Size(1116, 814);
        right.TabIndex = 1;
        // 
        // workspace
        // 
        workspace.Dock = DockStyle.Fill;
        workspace.Location = new Point(0, 104);
        workspace.Name = "workspace";
        workspace.Padding = new Padding(24, 0, 24, 8);
        workspace.Size = new Size(1116, 624);
        workspace.TabIndex = 0;
        // 
        // toast
        // 
        toast.AccessibleRole = AccessibleRole.Alert;
        toast.Dock = DockStyle.Bottom;
        toast.Font = new Font("Segoe UI", 10F);
        toast.Location = new Point(0, 728);
        toast.Name = "toast";
        toast.Size = new Size(1116, 56);
        toast.TabIndex = 1;
        toast.Visible = false;
        // 
        // footer
        // 
        footer.Dock = DockStyle.Bottom;
        footer.Font = new Font("Arial", 9F);
        footer.ForeColor = Color.FromArgb(98, 109, 124);
        footer.Location = new Point(0, 784);
        footer.Name = "footer";
        footer.Padding = new Padding(24, 0, 24, 0);
        footer.Size = new Size(1116, 30);
        footer.TabIndex = 2;
        footer.Text = "Ctrl+N  Thêm mới     Ctrl+F  Tìm kiếm     F5  Làm mới                         Dữ liệu dùng chung qua API";
        footer.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // header
        // 
        header.BackColor = Color.White;
        header.ColumnCount = 4;
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle());
        header.ColumnStyles.Add(new ColumnStyle());
        header.Controls.Add(toggle, 0, 0);
        header.Controls.Add(title, 1, 0);
        header.Controls.Add(quickSearch, 2, 0);
        header.Controls.Add(identity, 3, 0);
        header.Dock = DockStyle.Top;
        header.Location = new Point(0, 0);
        header.Name = "header";
        header.Padding = new Padding(24, 12, 24, 12);
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        header.Size = new Size(1116, 104);
        header.TabIndex = 3;
        // 
        // toggle
        // 
        toggle.AccessibleName = "Thu gọn hoặc mở thanh điều hướng";
        toggle.BackColor = Color.White;
        toggle.FlatStyle = FlatStyle.Flat;
        toggle.Font = new Font("Segoe MDL2 Assets", 16F);
        toggle.ForeColor = Color.FromArgb(30, 78, 140);
        toggle.Kind = UiButtonKind.Ghost;
        toggle.Location = new Point(24, 28);
        toggle.Margin = new Padding(0, 16, 12, 0);
        toggle.Name = "toggle";
        toggle.Padding = new Padding(16, 8, 16, 8);
        toggle.Size = new Size(40, 40);
        toggle.TabIndex = 0;
        toggle.Text = "";
        toggle.UseVisualStyleBackColor = false;
        // 
        // title
        // 
        title.ColumnCount = 1;
        title.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        title.Controls.Add(breadcrumb, 0, 0);
        title.Controls.Add(heading, 0, 1);
        title.Dock = DockStyle.Fill;
        title.Location = new Point(79, 15);
        title.Name = "title";
        title.RowCount = 2;
        title.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        title.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        title.Size = new Size(770, 74);
        title.TabIndex = 1;
        // 
        // breadcrumb
        // 
        breadcrumb.Dock = DockStyle.Fill;
        breadcrumb.Font = new Font("Arial", 9F);
        breadcrumb.ForeColor = Color.FromArgb(98, 109, 124);
        breadcrumb.Location = new Point(3, 0);
        breadcrumb.Name = "breadcrumb";
        breadcrumb.Size = new Size(764, 26);
        breadcrumb.TabIndex = 0;
        breadcrumb.Text = "Không gian làm việc";
        breadcrumb.TextAlign = ContentAlignment.BottomLeft;
        // 
        // heading
        // 
        heading.Dock = DockStyle.Fill;
        heading.Font = new Font("Arial", 20F, FontStyle.Bold);
        heading.ForeColor = Color.FromArgb(31, 41, 55);
        heading.Location = new Point(3, 26);
        heading.Name = "heading";
        heading.Size = new Size(764, 48);
        heading.TabIndex = 1;
        heading.Text = "Tổng quan";
        heading.TextAlign = ContentAlignment.MiddleLeft;
        heading.UseMnemonic = false;
        // 
        // quickSearch
        // 
        quickSearch.AutoSize = true;
        quickSearch.BackColor = Color.White;
        quickSearch.FlatStyle = FlatStyle.Flat;
        quickSearch.Font = new Font("Segoe UI", 10F);
        quickSearch.ForeColor = Color.FromArgb(30, 78, 140);
        quickSearch.Kind = UiButtonKind.Secondary;
        quickSearch.Location = new Point(868, 31);
        quickSearch.Margin = new Padding(16, 19, 16, 0);
        quickSearch.Name = "quickSearch";
        quickSearch.Padding = new Padding(16, 8, 16, 8);
        quickSearch.Size = new Size(168, 40);
        quickSearch.TabIndex = 2;
        quickSearch.Text = "Tìm kiếm  Ctrl+F";
        quickSearch.UseVisualStyleBackColor = false;
        // 
        // identity
        // 
        identity.AutoSize = true;
        identity.ColumnCount = 2;
        identity.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
        identity.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        avatar.Size = new Size(36, 36);
        identity.Controls.Add(avatar, 0, 0);
        identity.Controls.Add(accountLabel, 1, 0);
        identity.Location = new Point(1052, 24);
        identity.Margin = new Padding(0, 12, 0, 0);
        identity.Name = "identity";
        identity.RowCount = 1;
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        identity.Size = new Size(40, 20);
        identity.TabIndex = 3;
        // 
        // accountLabel
        // 
        accountLabel.AutoSize = true;
        accountLabel.Font = new Font("Arial", 9F);
        accountLabel.ForeColor = Color.FromArgb(31, 41, 55);
        accountLabel.Location = new Point(23, 0);
        accountLabel.MaximumSize = new Size(200, 0);
        accountLabel.Name = "accountLabel";
        accountLabel.Padding = new Padding(0, 4, 0, 0);
        accountLabel.Size = new Size(14, 20);
        accountLabel.TabIndex = 0;
        accountLabel.Text = "Người dùng\nVai trò";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 250);
        ClientSize = new Size(1366, 820);
        Controls.Add(frame);
        Font = new Font("Arial", 10F);
        ForeColor = Color.FromArgb(31, 41, 55);
        KeyPreview = true;
        MinimumSize = new Size(1024, 680);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Exam • Không gian khảo thí";
        WindowState = FormWindowState.Maximized;
        frame.ResumeLayout(false);
        sidebar.ResumeLayout(false);
        brand.ResumeLayout(false);
        brand.PerformLayout();
        right.ResumeLayout(false);
        header.ResumeLayout(false);
        header.PerformLayout();
        title.ResumeLayout(false);
        identity.ResumeLayout(false);
        identity.PerformLayout();
        ResumeLayout(false);
    }
}
