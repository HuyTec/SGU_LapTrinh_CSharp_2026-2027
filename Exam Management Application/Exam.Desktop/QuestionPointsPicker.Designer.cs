namespace Exam.Desktop;

partial class QuestionPointsPicker
{
    private UiDataGridView grid = null!;
    private DataGridViewCheckBoxColumn selectedColumn = null!;
    private DataGridViewTextBoxColumn questionColumn = null!;
    private DataGridViewTextBoxColumn pointsColumn = null!;
    private Label total = null!;
    private Label validation = null!;
    private TableLayoutPanel searchRow = null!;
    private Label searchLabel = null!;
    private TextBox search = null!;
    private TableLayoutPanel previewPanel = null!;
    private Label previewLabel = null!;
    private TextBox preview = null!;

    private void InitializeComponent()
    {
        grid = new UiDataGridView();
        selectedColumn = new DataGridViewCheckBoxColumn();
        questionColumn = new DataGridViewTextBoxColumn();
        pointsColumn = new DataGridViewTextBoxColumn();
        var pointsStyle = new DataGridViewCellStyle();
        total = new Label();
        validation = new Label();
        searchRow = new TableLayoutPanel();
        searchLabel = new Label();
        search = new TextBox();
        previewPanel = new TableLayoutPanel();
        previewLabel = new Label();
        preview = new TextBox();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();
        searchRow.SuspendLayout();
        previewPanel.SuspendLayout();

        grid.Name = "grid";
        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.AddRange(selectedColumn, questionColumn, pointsColumn);

        selectedColumn.Name = "selected";
        selectedColumn.DataPropertyName = "selected";
        selectedColumn.HeaderText = "Chọn";
        selectedColumn.FillWeight = 12F;
        selectedColumn.MinimumWidth = 65;

        questionColumn.Name = "question";
        questionColumn.DataPropertyName = "question";
        questionColumn.HeaderText = "Chủ đề và nội dung câu hỏi";
        questionColumn.ReadOnly = true;
        questionColumn.FillWeight = 70F;
        questionColumn.MinimumWidth = 140;

        pointsColumn.Name = "points";
        pointsColumn.DataPropertyName = "points";
        pointsColumn.HeaderText = "Điểm";
        pointsColumn.FillWeight = 18F;
        pointsColumn.MinimumWidth = 80;
        pointsStyle.Format = "0.##";
        pointsColumn.DefaultCellStyle = pointsStyle;

        total.Name = "total";
        total.Dock = DockStyle.Bottom;
        total.Height = 34;
        total.TextAlign = ContentAlignment.MiddleLeft;
        total.Font = Theme.BodyFont;
        total.ForeColor = Theme.Primary;

        validation.Name = "validation";
        validation.Dock = DockStyle.Bottom;
        validation.Height = 24;
        validation.ForeColor = Theme.Danger;
        validation.Font = Theme.SmallFont;

        searchRow.Name = "searchRow";
        searchRow.Dock = DockStyle.Top;
        searchRow.Height = 48;
        searchRow.ColumnCount = 2;
        searchRow.Margin = Padding.Empty;
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchRow.Controls.Add(searchLabel, 0, 0);
        searchRow.Controls.Add(search, 1, 0);

        searchLabel.Name = "searchLabel";
        searchLabel.Text = "Tìm câu hỏi";
        searchLabel.AutoSize = true;
        searchLabel.Margin = new Padding(0, 8, 12, 0);

        search.Name = "search";
        search.Dock = DockStyle.Fill;
        search.PlaceholderText = "Tìm theo nội dung hoặc chủ đề…";
        search.Font = Theme.BodyFont;
        search.ForeColor = Theme.Text;
        search.BackColor = Theme.Surface;
        search.BorderStyle = BorderStyle.FixedSingle;
        search.Margin = new Padding(0, 4, 0, 12);

        previewPanel.Name = "previewPanel";
        previewPanel.Dock = DockStyle.Bottom;
        previewPanel.Height = 140;
        previewPanel.ColumnCount = 1;
        previewPanel.RowCount = 2;
        previewPanel.Padding = new Padding(0, 8, 0, 0);
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        previewPanel.Controls.Add(previewLabel, 0, 0);
        previewPanel.Controls.Add(preview, 0, 1);

        previewLabel.Name = "previewLabel";
        previewLabel.Text = "Xem toàn bộ câu hỏi đang chọn";
        previewLabel.AutoSize = true;
        previewLabel.ForeColor = Theme.Muted;
        previewLabel.Font = Theme.SmallFont;

        preview.Name = "preview";
        preview.Dock = DockStyle.Fill;
        preview.Multiline = true;
        preview.ReadOnly = true;
        preview.ScrollBars = ScrollBars.Vertical;
        preview.TabStop = true;
        preview.Font = Theme.BodyFont;
        preview.ForeColor = Theme.Text;
        preview.BackColor = Theme.Surface;
        preview.BorderStyle = BorderStyle.FixedSingle;
        preview.Margin = new Padding(0, 4, 0, 12);

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;
        MinimumSize = new Size(280, 400);
        Name = "QuestionPointsPicker";
        Size = new Size(420, 500);
        Controls.Add(grid);
        Controls.Add(previewPanel);
        Controls.Add(validation);
        Controls.Add(total);
        Controls.Add(searchRow);
        previewPanel.ResumeLayout(false);
        previewPanel.PerformLayout();
        searchRow.ResumeLayout(false);
        searchRow.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
