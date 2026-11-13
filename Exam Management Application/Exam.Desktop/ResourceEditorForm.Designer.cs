namespace Exam.Desktop;

partial class ResourceEditorForm
{
    private ErrorProvider errors = null!;
    private Panel scroll = null!;
    private TableLayoutPanel layout = null!;
    private TableLayoutPanel header = null!;
    private Label heading = null!;
    private Label guidance = null!;
    private Label wizardProgress = null!;
    private TableLayoutPanel footer = null!;
    private Label status = null!;
    private FlowLayoutPanel actions = null!;
    private UiButton cancel = null!;
    private UiButton previous = null!;
    private UiButton next = null!;
    private UiButton save = null!;

    private void InitializeComponent()
    {
        errors = new ErrorProvider();
        scroll = new Panel();
        layout = new TableLayoutPanel();
        header = new TableLayoutPanel();
        heading = new Label();
        guidance = new Label();
        wizardProgress = new Label();
        footer = new TableLayoutPanel();
        status = new Label();
        actions = new FlowLayoutPanel();
        cancel = new UiButton();
        previous = new UiButton();
        next = new UiButton();
        save = new UiButton();
        ((System.ComponentModel.ISupportInitialize)errors).BeginInit();
        SuspendLayout();
        scroll.SuspendLayout();
        header.SuspendLayout();
        footer.SuspendLayout();
        actions.SuspendLayout();

        errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;
        errors.ContainerControl = this;

        scroll.Name = "scroll";
        scroll.Dock = DockStyle.Fill;
        scroll.AutoScroll = true;
        scroll.Controls.Add(layout);

        layout.Name = "layout";
        layout.Dock = DockStyle.Top;
        layout.AutoSize = true;
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Padding = new Padding(20);
        layout.BackColor = Theme.Background;

        header.Name = "header";
        header.Dock = DockStyle.Top;
        header.AutoSize = true;
        header.ColumnCount = 1;
        header.Padding = new Padding(24, 20, 24, 16);
        header.BackColor = Theme.Surface;
        header.Controls.Add(heading);
        header.Controls.Add(guidance);
        header.Controls.Add(wizardProgress);

        heading.Name = "heading";
        heading.Text = "Thêm thông tin";
        heading.Font = Theme.SectionFont;
        heading.AutoSize = true;
        heading.Margin = new Padding(0, 0, 0, 8);

        guidance.Name = "guidance";
        guidance.Text = "Các trường có dấu * là bắt buộc. Ctrl + Enter để lưu · Esc để đóng.";
        guidance.Font = Theme.SmallFont;
        guidance.ForeColor = Theme.Muted;
        guidance.AutoSize = true;

        wizardProgress.Name = "wizardProgress";
        wizardProgress.AutoSize = true;
        wizardProgress.Font = Theme.BodyFont;
        wizardProgress.ForeColor = Theme.Primary;
        wizardProgress.Margin = new Padding(0, 12, 0, 0);
        wizardProgress.Visible = false;

        footer.Name = "footer";
        footer.Dock = DockStyle.Bottom;
        footer.AutoSize = true;
        footer.ColumnCount = 2;
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Padding = new Padding(24, 12, 24, 12);
        footer.BackColor = Theme.Surface;
        footer.Controls.Add(status, 0, 0);
        footer.Controls.Add(actions, 1, 0);

        status.Name = "status";
        status.AutoSize = true;
        status.Dock = DockStyle.Fill;
        status.ForeColor = Theme.Muted;
        status.Padding = new Padding(0, 4, 12, 4);

        actions.Name = "actions";
        actions.AutoSize = true;
        actions.WrapContents = false;
        actions.Margin = Padding.Empty;
        actions.FlowDirection = FlowDirection.LeftToRight;
        actions.Controls.Add(cancel);
        actions.Controls.Add(previous);
        actions.Controls.Add(next);
        actions.Controls.Add(save);

        cancel.Name = "cancel";
        cancel.Text = "Đóng";
        cancel.Kind = UiButtonKind.Secondary;
        cancel.AutoSize = true;
        cancel.DialogResult = DialogResult.Cancel;
        cancel.CausesValidation = false;

        previous.Name = "wizardPrevious";
        previous.Text = "Quay lại";
        previous.Kind = UiButtonKind.Secondary;
        previous.AutoSize = true;
        previous.CausesValidation = false;
        previous.Visible = false;

        next.Name = "wizardNext";
        next.Text = "Tiếp tục";
        next.Kind = UiButtonKind.Primary;
        next.AutoSize = true;
        next.Visible = false;

        save.Name = "saveResource";
        save.Text = "Lưu thay đổi";
        save.Kind = UiButtonKind.Primary;
        save.AutoSize = true;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        CancelButton = cancel;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;
        Name = "ResourceEditorForm";
        Size = new Size(940, 760);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Chỉnh sửa thông tin";
        Controls.Add(scroll);
        Controls.Add(header);
        Controls.Add(footer);
        actions.ResumeLayout(false);
        actions.PerformLayout();
        footer.ResumeLayout(false);
        footer.PerformLayout();
        header.ResumeLayout(false);
        header.PerformLayout();
        scroll.ResumeLayout(false);
        scroll.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)errors).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
