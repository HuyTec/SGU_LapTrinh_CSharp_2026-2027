using Exam.Core;

namespace Exam.Desktop;

public sealed class ClassMembersForm : Form
{
    private readonly ExamApiClient api;
    private readonly int classId;
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly TextBox code = new() { Width = 160, MaxLength = 30, PlaceholderText = "Mã thí sinh", AccessibleName = "Mã thí sinh" };
    private readonly FlowLayoutPanel tools = new() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(8) };
    private bool busy;

    public ClassMembersForm(ExamApiClient api, int classId)
    {
        this.api = api;
        this.classId = classId;
        Text = "Thành viên lớp";
        ClientSize = new Size(760, 450);
        StartPosition = FormStartPosition.CenterParent;
        Theme.Apply(this);
        Theme.StyleGrid(grid);
        Theme.StyleInput(code);
        grid.Columns.Add("code", "Mã thí sinh");
        grid.Columns.Add("name", "Họ tên");
        grid.Columns.Add("status", "Trạng thái tài khoản");
        tools.Controls.Add(code);
        AddButton("Thêm", async () => { await api.AddClassMemberAsync(classId, code.Text); code.Clear(); });
        AddButton("Gỡ khỏi lớp", async () =>
        {
            if (grid.CurrentRow?.Tag is not ClassMemberDto member)
                return;
            if (MessageBox.Show(this, $"Gỡ {member.FullName} khỏi lớp? Lượt thi đã tạo vẫn được giữ.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                await api.RemoveClassMemberAsync(classId, member.StudentId);
        });
        AddButton("Nhập CSV vào lớp", () =>
        {
            using var dialog = new ImportStudentsForm(api, classId);
            dialog.ShowDialog(this);
            return Task.CompletedTask;
        });
        AddButton("Làm mới", () => Task.CompletedTask);
        Controls.Add(grid);
        Controls.Add(tools);
        Controls.Add(status);
        Shown += async (_, _) => await RefreshAfterAsync(() => Task.CompletedTask);
        FormClosing += (_, e) => e.Cancel = busy;
    }

    private void AddButton(string text, Func<Task> action)
    {
        var button = new UiButton { Text = text, Kind = UiButtonKind.Secondary, AutoSize = true };
        button.Click += async (_, _) => await RefreshAfterAsync(action);
        tools.Controls.Add(button);
    }

    private async Task RefreshAfterAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        tools.Enabled = false;
        status.Text = "Đang xử lý…";
        try
        {
            await action();
            var members = await api.ClassMembersAsync(classId);
            grid.Rows.Clear();
            foreach (var member in members)
            {
                var row = grid.Rows.Add(member.StudentCode ?? "—", member.FullName, member.IsActive ? "Hoạt động" : "Đã khóa");
                grid.Rows[row].Tag = member;
            }
            status.Text = $"{members.Count} thành viên";
        }
        catch (Exception ex) { status.Text = ex.Message; }
        finally { tools.Enabled = true; busy = false; }
    }
}
