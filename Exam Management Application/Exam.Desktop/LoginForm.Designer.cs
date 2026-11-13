#nullable enable

using System.ComponentModel;

namespace Exam.Desktop;

public sealed partial class LoginForm
{
    private IContainer? components;
    private TableLayoutPanel columns = null!;
    private Panel brand = null!;
    private TableLayoutPanel brandLayout = null!;
    private Label brandHeading = null!;
    private AcademicIllustration illustration = null!;
    private TableLayoutPanel process = null!;
    private Label processOne = null!;
    private Label processTwo = null!;
    private Label processThree = null!;
    private Label brandFootnote = null!;
    private Panel right = null!;
    private TableLayoutPanel form = null!;
    private Label eyebrow = null!;
    private Label heading = null!;
    private Label introduction = null!;
    private Label userNameLabel = null!;
    private TextBox userName = null!;
    private Label passwordLabel = null!;
    private TableLayoutPanel passwordRow = null!;
    private TextBox password = null!;
    private UiButton reveal = null!;
    private Label roleLabel = null!;
    private ComboBox role = null!;
    private Label roleNote = null!;
    private Label passwordNote = null!;
    private UiButton login = null!;
    private Label status = null!;
    private UiButton serverToggle = null!;
    private FlowLayoutPanel links = null!;
    private UiButton help = null!;
    private UiButton register = null!;
    private Label footnote = null!;
    private ErrorProvider errors = null!;

    private void InitializeComponent()
    {
        components = new Container();
        columns = new TableLayoutPanel();
        brand = new Panel();
        brandLayout = new TableLayoutPanel();
        brandHeading = new Label();
        process = new TableLayoutPanel();
        processOne = new Label();
        processTwo = new Label();
        processThree = new Label();
        brandFootnote = new Label();
        right = new Panel();
        form = new TableLayoutPanel();
        eyebrow = new Label();
        introduction = new Label();
        userNameLabel = new Label();
        userName = new TextBox();
        passwordLabel = new Label();
        passwordRow = new TableLayoutPanel();
        password = new TextBox();
        roleLabel = new Label();
        role = new ComboBox();
        roleNote = new Label();
        passwordNote = new Label();
        status = new Label();
        links = new FlowLayoutPanel();
        footnote = new Label();
        heading = new Label();
        errors = new ErrorProvider(components);
        columns.SuspendLayout();
        brand.SuspendLayout();
        brandLayout.SuspendLayout();
        process.SuspendLayout();
        right.SuspendLayout();
        form.SuspendLayout();
        passwordRow.SuspendLayout();
        ((ISupportInitialize)errors).BeginInit();
        SuspendLayout();
        // 
        // columns
        // 
        columns.ColumnCount = 2;
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        columns.Controls.Add(brand, 0, 0);
        columns.Controls.Add(right, 1, 0);
        columns.Dock = DockStyle.Fill;
        columns.Location = new Point(0, 0);
        columns.Margin = new Padding(0);
        columns.Name = "columns";
        columns.RowCount = 1;
        columns.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        columns.Size = new Size(1040, 680);
        columns.TabIndex = 0;
        // 
        // brand
        // 
        brand.BackColor = Color.FromArgb(30, 78, 140);
        brand.Controls.Add(brandLayout);
        brand.Dock = DockStyle.Fill;
        brand.Location = new Point(0, 0);
        brand.Margin = new Padding(0);
        brand.Name = "brand";
        brand.Size = new Size(436, 680);
        brand.TabIndex = 0;
        // 
        // brandLayout
        // 
        brandLayout.BackColor = Color.FromArgb(30, 78, 140);
        brandLayout.ColumnCount = 1;
        brandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        brandLayout.Controls.Add(brandHeading, 0, 0);
        brandLayout.Controls.Add(process, 0, 2);
        brandLayout.Controls.Add(brandFootnote, 0, 3);
        brandLayout.Dock = DockStyle.Fill;
        brandLayout.Location = new Point(0, 0);
        brandLayout.Name = "brandLayout";
        brandLayout.Padding = new Padding(32);
        brandLayout.RowCount = 4;
        brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
        brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        brandLayout.Size = new Size(436, 680);
        brandLayout.TabIndex = 0;
        // 
        // brandHeading
        // 
        brandHeading.AutoSize = true;
        brandHeading.Dock = DockStyle.Fill;
        brandHeading.Font = new Font("Arial", 13F, FontStyle.Bold);
        brandHeading.ForeColor = Color.White;
        brandHeading.Location = new Point(32, 32);
        brandHeading.Margin = new Padding(0);
        brandHeading.Name = "brandHeading";
        brandHeading.Size = new Size(372, 80);
        brandHeading.TabIndex = 0;
        brandHeading.Text = "EXAM\nKhông gian khảo thí";
        // 
        // process
        // 
        process.ColumnCount = 1;
        process.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        process.Controls.Add(processOne, 0, 0);
        process.Controls.Add(processTwo, 0, 1);
        process.Controls.Add(processThree, 0, 2);
        process.Dock = DockStyle.Fill;
        process.Location = new Point(32, 494);
        process.Margin = new Padding(0);
        process.Name = "process";
        process.RowCount = 3;
        process.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
        process.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
        process.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
        process.Size = new Size(372, 120);
        process.TabIndex = 2;
        // 
        // processOne
        // 
        processOne.AutoSize = true;
        processOne.Dock = DockStyle.Fill;
        processOne.ForeColor = Color.White;
        processOne.Location = new Point(0, 0);
        processOne.Margin = new Padding(0);
        processOne.Name = "processOne";
        processOne.Size = new Size(372, 39);
        processOne.TabIndex = 0;
        processOne.Text = "01   Chuẩn bị ngân hàng và đề thi";
        processOne.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // processTwo
        // 
        processTwo.AutoSize = true;
        processTwo.Dock = DockStyle.Fill;
        processTwo.ForeColor = Color.White;
        processTwo.Location = new Point(0, 39);
        processTwo.Margin = new Padding(0);
        processTwo.Name = "processTwo";
        processTwo.Size = new Size(372, 39);
        processTwo.TabIndex = 1;
        processTwo.Text = "02   Tổ chức kỳ thi rõ ràng";
        processTwo.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // processThree
        // 
        processThree.AutoSize = true;
        processThree.Dock = DockStyle.Fill;
        processThree.ForeColor = Color.White;
        processThree.Location = new Point(0, 78);
        processThree.Margin = new Padding(0);
        processThree.Name = "processThree";
        processThree.Size = new Size(372, 42);
        processThree.TabIndex = 2;
        processThree.Text = "03   Đánh giá bằng kết quả tin cậy";
        processThree.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // brandFootnote
        // 
        brandFootnote.AutoSize = true;
        brandFootnote.Font = new Font("Arial", 9F);
        brandFootnote.ForeColor = Color.FromArgb(221, 234, 250);
        brandFootnote.Location = new Point(32, 614);
        brandFootnote.Margin = new Padding(0);
        brandFootnote.Name = "brandFootnote";
        brandFootnote.Size = new Size(231, 17);
        brandFootnote.TabIndex = 3;
        brandFootnote.Text = "Một hệ thống · Dữ liệu dùng chung";
        // 
        // right
        // 
        right.AutoScroll = true;
        right.BackColor = Color.White;
        right.Controls.Add(form);
        right.Dock = DockStyle.Fill;
        right.Location = new Point(436, 0);
        right.Margin = new Padding(0);
        right.Name = "right";
        right.Padding = new Padding(42, 24, 42, 20);
        right.Size = new Size(604, 680);
        right.TabIndex = 1;
        // 
        // form
        // 
        form.AutoSize = true;
        form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        form.ColumnCount = 1;
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        form.Controls.Add(eyebrow, 0, 0);
        form.Controls.Add(introduction, 0, 2);
        form.Controls.Add(userNameLabel, 0, 3);
        form.Controls.Add(userName, 0, 4);
        form.Controls.Add(passwordLabel, 0, 5);
        form.Controls.Add(passwordRow, 0, 6);
        form.Controls.Add(roleLabel, 0, 7);
        form.Controls.Add(role, 0, 8);
        form.Controls.Add(roleNote, 0, 9);
        form.Controls.Add(passwordNote, 0, 10);
        form.Controls.Add(status, 0, 12);
        form.Controls.Add(links, 0, 15);
        form.Controls.Add(footnote, 0, 16);
        form.Controls.Add(heading, 0, 1);
        form.Dock = DockStyle.Top;
        form.Location = new Point(42, 24);
        form.Margin = new Padding(0);
        form.Name = "form";
        form.RowCount = 17;
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.RowStyles.Add(new RowStyle());
        form.Size = new Size(520, 468);
        form.TabIndex = 0;
        // 
        // eyebrow
        // 
        eyebrow.AutoSize = true;
        eyebrow.Font = new Font("Arial", 9F, FontStyle.Bold);
        eyebrow.ForeColor = Color.FromArgb(30, 78, 140);
        eyebrow.Location = new Point(0, 8);
        eyebrow.Margin = new Padding(0, 8, 0, 12);
        eyebrow.Name = "eyebrow";
        eyebrow.Size = new Size(181, 18);
        eyebrow.TabIndex = 0;
        eyebrow.Text = "KHÔNG GIAN KHẢO THÍ";
        // 
        // introduction
        // 
        introduction.AutoSize = true;
        introduction.ForeColor = Color.FromArgb(98, 109, 124);
        introduction.Location = new Point(0, 86);
        introduction.Margin = new Padding(0, 0, 0, 24);
        introduction.MaximumSize = new Size(520, 0);
        introduction.Name = "introduction";
        introduction.Size = new Size(386, 19);
        introduction.TabIndex = 2;
        introduction.Text = "Đăng nhập để chuẩn bị, tổ chức và đánh giá kỳ thi.";
        // 
        // userNameLabel
        // 
        userNameLabel.AutoSize = true;
        userNameLabel.ForeColor = Color.FromArgb(31, 41, 55);
        userNameLabel.Location = new Point(0, 129);
        userNameLabel.Margin = new Padding(0, 0, 0, 6);
        userNameLabel.Name = "userNameLabel";
        userNameLabel.Size = new Size(119, 19);
        userNameLabel.TabIndex = 3;
        userNameLabel.Text = "Tên đăng nhập";
        // 
        // userName
        // 
        userName.AccessibleName = "Tên đăng nhập";
        userName.BackColor = Color.White;
        userName.BorderStyle = BorderStyle.FixedSingle;
        userName.Dock = DockStyle.Fill;
        userName.Location = new Point(0, 154);
        userName.Margin = new Padding(0, 0, 0, 16);
        userName.Name = "userName";
        userName.PlaceholderText = "Tên tài khoản của bạn";
        userName.Size = new Size(520, 27);
        userName.TabIndex = 4;
        // 
        // passwordLabel
        // 
        passwordLabel.AutoSize = true;
        passwordLabel.ForeColor = Color.FromArgb(31, 41, 55);
        passwordLabel.Location = new Point(0, 197);
        passwordLabel.Margin = new Padding(0, 0, 0, 6);
        passwordLabel.Name = "passwordLabel";
        passwordLabel.Size = new Size(75, 19);
        passwordLabel.TabIndex = 5;
        passwordLabel.Text = "Mật khẩu";
        // 
        // passwordRow
        // 
        passwordRow.AutoSize = true;
        passwordRow.ColumnCount = 2;
        passwordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        passwordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
        passwordRow.Controls.Add(password, 0, 0);
        passwordRow.Dock = DockStyle.Fill;
        passwordRow.Location = new Point(0, 222);
        passwordRow.Margin = new Padding(0, 0, 0, 16);
        passwordRow.Name = "passwordRow";
        passwordRow.RowCount = 1;
        passwordRow.RowStyles.Add(new RowStyle());
        passwordRow.Size = new Size(520, 27);
        passwordRow.TabIndex = 6;
        // 
        // password
        // 
        password.AccessibleName = "Mật khẩu";
        password.BackColor = Color.White;
        password.BorderStyle = BorderStyle.FixedSingle;
        password.Dock = DockStyle.Fill;
        password.Location = new Point(0, 0);
        password.Margin = new Padding(0);
        password.Name = "password";
        password.PlaceholderText = "Nhập mật khẩu";
        password.Size = new Size(424, 27);
        password.TabIndex = 0;
        password.UseSystemPasswordChar = true;
        // 
        // roleLabel
        // 
        roleLabel.AutoSize = true;
        roleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        roleLabel.Location = new Point(0, 265);
        roleLabel.Margin = new Padding(0, 0, 0, 6);
        roleLabel.Name = "roleLabel";
        roleLabel.Size = new Size(134, 19);
        roleLabel.TabIndex = 7;
        roleLabel.Text = "Vai trò hoạt động";
        // 
        // role
        // 
        role.AccessibleName = "Vai trò hoạt động";
        role.BackColor = Color.White;
        role.DisplayMember = "Label";
        role.Dock = DockStyle.Fill;
        role.DropDownStyle = ComboBoxStyle.DropDownList;
        role.Location = new Point(0, 290);
        role.Margin = new Padding(0, 0, 0, 16);
        role.Name = "role";
        role.Size = new Size(520, 27);
        role.TabIndex = 8;
        // 
        // roleNote
        // 
        roleNote.AutoSize = true;
        roleNote.Font = new Font("Arial", 9F);
        roleNote.ForeColor = Color.FromArgb(98, 109, 124);
        roleNote.Location = new Point(0, 334);
        roleNote.Margin = new Padding(0, 0, 0, 16);
        roleNote.Name = "roleNote";
        roleNote.Size = new Size(311, 17);
        roleNote.TabIndex = 9;
        roleNote.Text = "Chỉ chọn vai trò bạn đã được quản trị viên cấp.";
        // 
        // passwordNote
        // 
        passwordNote.AutoSize = true;
        passwordNote.Font = new Font("Arial", 9F);
        passwordNote.ForeColor = Color.FromArgb(98, 109, 124);
        passwordNote.Location = new Point(0, 367);
        passwordNote.Margin = new Padding(0, 0, 0, 12);
        passwordNote.Name = "passwordNote";
        passwordNote.Size = new Size(253, 17);
        passwordNote.TabIndex = 10;
        passwordNote.Text = "Quên mật khẩu? Liên hệ quản trị viên.";
        // 
        // status
        // 
        status.AccessibleRole = AccessibleRole.StatusBar;
        status.AutoSize = true;
        status.ForeColor = Color.FromArgb(98, 109, 124);
        status.Location = new Point(0, 404);
        status.Margin = new Padding(0, 8, 0, 8);
        status.MaximumSize = new Size(520, 0);
        status.Name = "status";
        status.Size = new Size(0, 19);
        status.TabIndex = 12;
        // 
        // links
        // 
        links.AutoSize = true;
        links.Dock = DockStyle.Fill;
        links.Location = new Point(0, 441);
        links.Margin = new Padding(0, 10, 0, 0);
        links.Name = "links";
        links.Size = new Size(520, 1);
        links.TabIndex = 15;
        // 
        // footnote
        // 
        footnote.AutoSize = true;
        footnote.Font = new Font("Arial", 9F);
        footnote.ForeColor = Color.FromArgb(98, 109, 124);
        footnote.Location = new Point(0, 451);
        footnote.Margin = new Padding(0, 10, 0, 0);
        footnote.MaximumSize = new Size(520, 0);
        footnote.Name = "footnote";
        footnote.Size = new Size(444, 17);
        footnote.TabIndex = 16;
        footnote.Text = "Thí sinh làm bài trên Web. Tài khoản nhân sự mới cần được duyệt.";
        // 
        // heading
        // 
        heading.AutoSize = true;
        heading.Font = new Font("Arial", 20F, FontStyle.Bold);
        heading.ForeColor = Color.FromArgb(31, 41, 55);
        heading.Location = new Point(0, 38);
        heading.Margin = new Padding(0, 0, 0, 8);
        heading.Name = "heading";
        heading.Size = new Size(312, 40);
        heading.TabIndex = 1;
        heading.Text = "Chào mừng trở lại";
        // 
        // errors
        // 
        errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;
        errors.ContainerControl = this;
        // 
        // LoginForm
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 250);
        ClientSize = new Size(1040, 680);
        Controls.Add(columns);
        Font = new Font("Arial", 10F);
        ForeColor = Color.FromArgb(31, 41, 55);
        MinimumSize = new Size(760, 590);
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Exam — Không gian khảo thí";
        columns.ResumeLayout(false);
        brand.ResumeLayout(false);
        brandLayout.ResumeLayout(false);
        brandLayout.PerformLayout();
        process.ResumeLayout(false);
        process.PerformLayout();
        right.ResumeLayout(false);
        right.PerformLayout();
        form.ResumeLayout(false);
        form.PerformLayout();
        passwordRow.ResumeLayout(false);
        passwordRow.PerformLayout();
        ((ISupportInitialize)errors).EndInit();
        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }
}
