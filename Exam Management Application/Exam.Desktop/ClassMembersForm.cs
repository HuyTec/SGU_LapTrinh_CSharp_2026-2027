using Exam.Core;

namespace Exam.Desktop;

internal sealed class ClassMembersForm : Form
{
    private readonly ExamApiClient api;
    private readonly int classId;
    private readonly ComboBox students = new() { Width = 320, DropDownStyle = ComboBoxStyle.DropDownList,
        AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems,
        DisplayMember = nameof(ChoiceDto.Label) };
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
        toolbar.Controls.AddRange([students, add, remove, refresh]);
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
        students.SelectedIndexChanged += (_, _) => UpdateButtons();
        FormClosing += (_, e) => e.Cancel = busy;
        add.Click += async (_, _) =>
        {
            if (students.SelectedItem is ChoiceDto choice && int.TryParse(choice.Value, out var id))
                await ChangeAsync(() => api.AddClassMemberAsync(classId, id));
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
        var eligible = await api.EligibleClassStudentsAsync(classId);
        grid.DataSource = members;
        students.DataSource = eligible.Select(x => new ChoiceDto(x.StudentId.ToString(),
            x.StudentCode is { Length: > 0 } ? $"{x.StudentCode} · {x.FullName}" : x.FullName)).ToList();
        students.SelectedIndex = -1;
        loaded = true;
        status.Text = eligible.Count == 0
            ? $"{members.Count} thành viên. Không có thí sinh đủ điều kiện để thêm."
            : $"{members.Count} thành viên. Chọn thí sinh để thêm vào lớp.";
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
        students.Enabled = loaded && !busy;
        add.Enabled = students.Enabled && students.SelectedItem is ChoiceDto;
        remove.Enabled = loaded && !busy && grid.CurrentRow?.DataBoundItem is ClassMemberDto;
    }
}
