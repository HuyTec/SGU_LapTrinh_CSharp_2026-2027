namespace Exam.Desktop;

public sealed partial class DashboardView
{
    private Panel scroll = null!;
    private Label feedback = null!;
    private TableLayoutPanel layout = null!;
    private UiCard welcome = null!;

    private void InitializeComponent()
    {
        scroll = new Panel();
        feedback = new Label();
        layout = new TableLayoutPanel();
        welcome = new UiCard();
        SuspendLayout();
        scroll.Name = "scroll";
        scroll.Dock = DockStyle.Fill;
        scroll.AutoScroll = true;
        feedback.Name = "feedback";
        feedback.Dock = DockStyle.Top;
        feedback.Height = 40;
        feedback.TextAlign = ContentAlignment.MiddleLeft;
        feedback.ForeColor = Theme.Muted;
        layout.Name = "layout";
        layout.Dock = DockStyle.Top;
        layout.AutoSize = true;
        layout.ColumnCount = 1;
        layout.RowCount = 3;
        layout.Padding = new Padding(0, 0, 0, 16);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 278));
        welcome.Name = "welcome";
        welcome.Dock = DockStyle.Fill;
        welcome.Height = 142;
        welcome.Margin = new Padding(0, 0, 0, 16);
        welcome.BackColor = Theme.SoftPrimary;
        layout.Controls.Add(welcome, 0, 0);
        scroll.Controls.Add(layout);
        Controls.Add(scroll);
        Controls.Add(feedback);
        BackColor = Theme.Background;
        Font = Theme.BodyFont;
        Name = "DashboardView";
        Size = new Size(980, 680);
        ResumeLayout(false);
    }
}
