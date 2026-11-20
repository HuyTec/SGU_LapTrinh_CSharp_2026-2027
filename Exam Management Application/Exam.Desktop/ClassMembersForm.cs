using Exam.Core;

namespace Exam.Desktop;

internal sealed class ClassMembersForm : Form
{
    private readonly ExamApiClient api;
    private readonly int classId;
    private readonly Button add = new() { Text = "Thêm thí sinh", AutoSize = true };
    private readonly Button remove = new() { Text = "Gỡ khỏi lớp", AutoSize = true };
    private readonly Button refresh = new() { Text = "Làm mới", AutoSize = true };
    private readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
        RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private bool busy;
    private bool loaded;
    public bool MembershipChanged { get; private set; }

    public ClassMembersForm(ExamApiClient api, int classId, string code)
    {
        this.api = api;
        this.classId = classId;
        Text = $"Thành viên lớp {code}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(790, 430);
        MinimumSize = new Size(575, 330);
        AutoScaleMode = AutoScaleMode.Font;

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã thí sinh", DataPropertyName = nameof(ClassMemberDto.StudentCode) });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ và tên", DataPropertyName = nameof(ClassMemberDto.FullName), FillWeight = 180 });
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Tài khoản hoạt động", DataPropertyName = nameof(ClassMemberDto.IsActive) });
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        toolbar.Controls.AddRange([add, remove, refresh]);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(grid, 0, 1);
        layout.Controls.Add(status, 0, 2);
        Controls.Add(layout);

        Shown += async (_, _) => await RunAsync(LoadAsync);
        refresh.Click += async (_, _) => await RunAsync(LoadAsync);
        grid.SelectionChanged += (_, _) => UpdateButtons();
        FormClosing += (_, e) => e.Cancel = busy;
        add.Click += async (_, _) =>
        {
            if (ShowAddStudentDialog() == DialogResult.OK)
                await RunAsync(LoadAsync);
        };
        remove.Click += async (_, _) =>
        {
            if (grid.CurrentRow?.DataBoundItem is ClassMemberDto member &&
                MessageBox.Show(this, $"Gỡ {member.FullName} khỏi lớp?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                await ChangeAsync(() => api.RemoveClassMemberAsync(classId, member.StudentId));
        };
        UpdateButtons();
    }

    private DialogResult ShowAddStudentDialog()
    {
        using var dialog = new Form
        {
            Text = "Thêm thí sinh vào lớp", StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(460, 190), FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, AutoScaleMode = AutoScaleMode.Font
        };
        var code = new TextBox { Dock = DockStyle.Fill, AccessibleName = "Mã sinh viên" };
        var error = new Label { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Color.Firebrick };
        var submit = new Button { Text = "Thêm", AutoSize = true };
        var cancel = new Button { Text = "Hủy", AutoSize = true, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.AddRange([submit, cancel]);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4
        };
        layout.Controls.Add(new Label { Text = "Mã sinh viên", AutoSize = true }, 0, 0);
        layout.Controls.Add(code, 0, 1);
        layout.Controls.Add(error, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        dialog.Controls.Add(layout);
        dialog.AcceptButton = submit;
        dialog.CancelButton = cancel;
        var saving = false;
        dialog.FormClosing += (_, e) => e.Cancel = saving;
        dialog.Shown += (_, _) => code.Focus();
        submit.Click += async (_, _) =>
        {
            if (saving) return;
            var studentCode = code.Text.Trim();
            if (studentCode.Length == 0)
            {
                error.Text = "Vui lòng nhập mã sinh viên.";
                code.Focus();
                return;
            }
            saving = true;
            submit.Enabled = cancel.Enabled = code.Enabled = false;
            submit.Text = "Đang thêm…";
            error.Text = "";
            try
            {
                var members = await api.ClassMembersAsync(classId);
                if (members.Any(member => string.Equals(member.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Thí sinh đã thuộc lớp này.");
                var eligible = await api.EligibleClassStudentsAsync(classId);
                var student = eligible.SingleOrDefault(member =>
                    string.Equals(member.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase));
                if (student is null)
                    throw new InvalidOperationException("Không tìm thấy thí sinh đang hoạt động có mã này.");
                await api.AddClassMemberAsync(classId, student.StudentId);
                MembershipChanged = true;
                saving = false;
                dialog.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                error.Text = ex is HttpRequestException or TaskCanceledException
                    ? "Không kết nối được máy chủ. Hãy thử lại." : ex.Message;
            }
            finally
            {
                saving = false;
                submit.Enabled = cancel.Enabled = code.Enabled = true;
                submit.Text = "Thêm";
                code.Focus();
            }
        };
        return dialog.ShowDialog(this);
    }

    private Task ChangeAsync(Func<Task> change) => RunAsync(async () =>
    {
        await change();
        MembershipChanged = true;
        await LoadAsync();
    });

    private async Task LoadAsync()
    {
        loaded = false;
        var members = await api.ClassMembersAsync(classId);
        grid.DataSource = members;
        loaded = true;
        status.Text = $"{members.Count} thành viên. Bấm Thêm thí sinh để nhập mã sinh viên.";
    }

    private async Task RunAsync(Func<Task> operation)
    {
        if (busy) return;
        busy = true;
        status.Text = "Đang xử lý…";
        UpdateButtons();
        try { await operation(); }
        catch (Exception error)
        {
            status.Text = error is HttpRequestException or TaskCanceledException
                ? "Không kết nối được máy chủ. Bấm Làm mới để thử lại."
                : error.Message;
        }
        finally
        {
            busy = false;
            UpdateButtons();
        }
    }

    private void UpdateButtons()
    {
        refresh.Enabled = !busy;
        add.Enabled = loaded && !busy;
        remove.Enabled = loaded && !busy && grid.CurrentRow?.DataBoundItem is ClassMemberDto;
    }
}
