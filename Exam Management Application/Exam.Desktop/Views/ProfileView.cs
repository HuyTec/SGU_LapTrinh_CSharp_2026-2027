using Exam.Core;
using System.ComponentModel.DataAnnotations;

namespace Exam.Desktop;

public sealed class ProfileView : UserControl, IWorkspaceView
{
    private readonly ExamApiClient api;
    private readonly TextBox fullName = new() { MaxLength = 120, Dock = DockStyle.Fill, AccessibleName = "Họ và tên" };
    private readonly TextBox email = new() { MaxLength = 256, Dock = DockStyle.Fill, AccessibleName = "Email" };
    private readonly TextBox phone = new() { MaxLength = 20, Dock = DockStyle.Fill, AccessibleName = "Số điện thoại" };
    private readonly Label account = new() { AutoSize = true };
    private readonly Label role = new() { AutoSize = true };
    private readonly Label feedback = new() { AutoSize = true, ForeColor = Theme.Muted, MaximumSize = new Size(640, 0) };
    private readonly Button save = new() { Text = "Lưu hồ sơ", AutoSize = true, MinimumSize = new Size(120, 44) };
    private readonly Button reload = new() { Text = "Tải lại", AutoSize = true, MinimumSize = new Size(100, 44) };
    private string version = "";
    private bool busy;
    public string Title => "Hồ sơ quản trị viên";
    public event EventHandler<ProfileDto>? ProfileSaved;

    public ProfileView(ExamApiClient api)
    {
        this.api = api;
        BackColor = Theme.Background;
        AutoScroll = true;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2,
            Padding = new Padding(24), BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(layout, "Tên đăng nhập", account);
        AddRow(layout, "Vai trò", role);
        AddRow(layout, "Họ và tên", fullName);
        AddRow(layout, "Email", email);
        AddRow(layout, "Số điện thoại", phone);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        actions.Controls.AddRange([save, reload]);
        AddRow(layout, "", actions);
        AddRow(layout, "", feedback);
        Controls.Add(layout);
        save.Click += async (_, _) => await SaveAsync();
        reload.Click += async (_, _) => await RefreshAsync();
    }

    private static void AddRow(TableLayoutPanel layout, string text, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = text, AutoSize = true, Margin = new Padding(0, 12, 8, 12) }, 0, row);
        control.Margin = new Padding(0, 8, 0, 8);
        layout.Controls.Add(control, 1, row);
    }

    public Task LoadAsync() => RefreshAsync();
    public void FocusSearch() => fullName.Focus();
    public Task AddNewAsync() { fullName.Focus(); return Task.CompletedTask; }

    public async Task RefreshAsync()
    {
        if (busy) return;
        SetBusy(true);
        try
        {
            var profile = await api.ProfileAsync();
            if (IsDisposed) return;
            Render(profile);
            feedback.ForeColor = Theme.Muted;
            feedback.Text = "Cập nhật họ tên, email và số điện thoại của bạn.";
        }
        catch (Exception ex)
        {
            if (!IsDisposed) { feedback.ForeColor = Theme.Danger; feedback.Text = ex.Message; }
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task SaveAsync()
    {
        if (busy || string.IsNullOrEmpty(version)) return;
        var request = new UpdateProfileRequest(fullName.Text.Trim(),
            string.IsNullOrWhiteSpace(email.Text) ? null : email.Text.Trim(),
            string.IsNullOrWhiteSpace(phone.Text) ? null : phone.Text.Trim(), version);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
        {
            feedback.ForeColor = Theme.Danger;
            feedback.Text = string.Join(" ", errors.Select(error => error.ErrorMessage));
            return;
        }
        SetBusy(true);
        try
        {
            var profile = await api.UpdateProfileAsync(request);
            if (IsDisposed) return;
            Render(profile);
            feedback.ForeColor = Theme.Success;
            feedback.Text = "Đã cập nhật hồ sơ.";
            ProfileSaved?.Invoke(this, profile);
        }
        catch (Exception ex)
        {
            if (!IsDisposed) { feedback.ForeColor = Theme.Danger; feedback.Text = ex.Message + " Nếu mất kết nối, tải lại để kiểm tra trước khi lưu tiếp."; }
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private void Render(ProfileDto profile)
    {
        account.Text = profile.UserName;
        role.Text = string.Join(", ", profile.Roles.Select(UiLabels.Of));
        fullName.Text = profile.FullName;
        email.Text = profile.Email ?? "";
        phone.Text = profile.PhoneNumber ?? "";
        version = profile.Version;
    }

    private void SetBusy(bool value)
    {
        busy = value;
        save.Enabled = !value && !string.IsNullOrEmpty(version);
        reload.Enabled = !value;
        fullName.Enabled = email.Enabled = phone.Enabled = !value;
    }
}
