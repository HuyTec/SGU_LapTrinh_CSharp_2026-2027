namespace Exam.Desktop;

public sealed partial class HelpForm
{
    private Label title = null!;
    private RichTextBox body = null!;
    private Panel content = null!;
    private UiCard card = null!;
    private FlowLayoutPanel footer = null!;
    private UiButton close = null!;

    private void InitializeComponent()
    {
        title = new Label();
        body = new RichTextBox();
        content = new Panel();
        card = new UiCard();
        footer = new FlowLayoutPanel();
        close = new UiButton();
        SuspendLayout();
        content.SuspendLayout();
        card.SuspendLayout();
        footer.SuspendLayout();

        title.Name = "title";
        title.Dock = DockStyle.Top;
        title.Height = 76;
        title.Padding = new Padding(24, 20, 24, 12);
        title.Font = new Font("Arial", 13F, FontStyle.Bold);
        title.BackColor = Color.White;
        title.Text = "Bắt đầu sử dụng";

        body.Name = "body";
        body.Dock = DockStyle.Fill;
        body.ReadOnly = true;
        body.BorderStyle = BorderStyle.None;
        body.BackColor = Color.White;
        body.ForeColor = Color.FromArgb(31, 41, 55);
        body.Font = new Font("Arial", 10F);
        body.DetectUrls = false;
        body.ScrollBars = RichTextBoxScrollBars.Vertical;

        content.Name = "content";
        content.Dock = DockStyle.Fill;
        content.Padding = new Padding(24, 20, 24, 20);
        content.Controls.Add(card);

        card.Name = "card";
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(20);
        card.Controls.Add(body);

        footer.Name = "footer";
        footer.Dock = DockStyle.Bottom;
        footer.AutoSize = true;
        footer.Padding = new Padding(24, 12, 24, 12);
        footer.FlowDirection = FlowDirection.RightToLeft;
        footer.BackColor = Color.White;
        footer.Controls.Add(close);

        close.Name = "close";
        close.Text = "Đóng hướng dẫn";
        close.Kind = UiButtonKind.Secondary;
        close.AutoSize = true;
        close.DialogResult = DialogResult.Cancel;

        Name = "HelpForm";
        Text = "Hướng dẫn sử dụng Exam";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Arial", 10F);
        BackColor = Color.FromArgb(245, 247, 250);
        ForeColor = Color.FromArgb(31, 41, 55);
        Size = new Size(900, 720);
        MinimumSize = new Size(650, 450);
        CancelButton = close;
        Controls.Add(content);
        Controls.Add(title);
        Controls.Add(footer);
        footer.ResumeLayout(false);
        footer.PerformLayout();
        card.ResumeLayout(false);
        content.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
