using Exam.Core;
using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace Exam.Desktop;

/// <summary>Desktop sign-in and connection settings. Authentication and role decisions remain on the API.</summary>
public sealed partial class LoginForm : Form
{
    private bool busy;

    public LoginForm()
    {
        InitializeComponent();
    }

    public LoginForm(ExamApiClient api) : this()
    {
        address.Text = Environment.GetEnvironmentVariable("EXAM_API_URL") ?? address.Text;
        settings.Visible = false;
        form.SizeChanged += (_, _) => UpdateLabelWidths();
        UpdateLabelWidths();

        role.Items.Add(new ChoiceDto("", "Tự động chọn vai trò"));
        foreach (var name in new[] { "Administrator", "Lecturer", "ExamOfficer" })
            role.Items.Add(new ChoiceDto(name, UiLabels.Of(name)));
        role.SelectedIndex = 0;

        reveal.Click += (_, _) =>
        {
            password.UseSystemPasswordChar = !password.UseSystemPasswordChar;
            reveal.Text = password.UseSystemPasswordChar ? "Hiện" : "Ẩn";
            reveal.AccessibleName = password.UseSystemPasswordChar ? "Hiện mật khẩu" : "Ẩn mật khẩu";
        };
        serverToggle.Click += (_, _) =>
        {
            settings.Visible = !settings.Visible;
            serverToggle.Text = settings.Visible ? "Ẩn cài đặt máy chủ" : "Cài đặt máy chủ";
            if (settings.Visible)
                address.Focus();
        };
        help.Click += (_, _) =>
        {
            using var helpForm = new HelpForm();
            helpForm.ShowDialog(this);
        };
        register.Click += (_, _) => OpenRegistration();
        login.Click += async (_, _) => await LoginAsync(api);
        userName.TextChanged += (_, _) => errors.SetError(userName, "");
        password.TextChanged += (_, _) => errors.SetError(password, "");
        Shown += (_, _) => FitToScreen();
        Resize += (_, _) => UpdateResponsiveColumns();
        UpdateResponsiveColumns();
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
    }

    private void UpdateLabelWidths()
    {
        foreach (var label in form.Controls.OfType<Label>())
            label.MaximumSize = new Size(Math.Max(220, form.ClientSize.Width), 0);
    }

    private void FitToScreen()
    {
        var area = Screen.FromControl(this).WorkingArea;
        MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width - 24), Math.Min(MinimumSize.Height, area.Height - 24));
        Width = Math.Min(Width, area.Width - 24);
        Height = Math.Min(Height, area.Height - 24);
        CenterToScreen();
        userName.Focus();
    }

    private void UpdateResponsiveColumns()
    {
        bool narrow=ClientSize.Width<800*DeviceDpi/96;
        brand.Visible=!narrow;columns.ColumnStyles[0].Width=narrow?0:42;columns.ColumnStyles[1].Width=narrow?100:58;
    }
    private void OpenRegistration()
    {
        try
        {
            var server=ValidServer();
            Process.Start(new ProcessStartInfo(new Uri(server,"Account/Register").AbsoluteUri){UseShellExecute=true});
        }
        catch(Exception ex){ShowStatus("Không mở được trang đăng ký: "+ex.Message,true);}
    }
    private Uri ValidServer()
    {
        if(!Uri.TryCreate(address.Text.Trim().TrimEnd('/')+"/",UriKind.Absolute,out var server)||server.Scheme is not("http" or "https")||!string.IsNullOrEmpty(server.UserInfo))
            throw new InvalidOperationException("Vui lòng nhập địa chỉ máy chủ HTTP hoặc HTTPS hợp lệ.");
        return server;
    }
    private async Task LoginAsync(ExamApiClient api)
    {
        if(busy)return;errors.Clear();
        if(string.IsNullOrWhiteSpace(userName.Text)){errors.SetError(userName,"Vui lòng nhập tên đăng nhập.");userName.Focus();ShowStatus("Vui lòng nhập tài khoản và mật khẩu.",true);return;}
        if(string.IsNullOrEmpty(password.Text)){errors.SetError(password,"Vui lòng nhập mật khẩu.");password.Focus();ShowStatus("Vui lòng nhập tài khoản và mật khẩu.",true);return;}
        try{ValidServer();}catch(Exception ex){settings.Visible=true;ShowStatus(ex.Message,true);address.Focus();return;}
        busy=true;login.Enabled=false;role.Enabled=false;address.Enabled=false;ShowStatus("Đang kết nối máy chủ…");
        try
        {
            var selectedRole=(role.SelectedItem as ChoiceDto)?.Value;
            await api.LoginAsync(address.Text,userName.Text.Trim(),password.Text,string.IsNullOrEmpty(selectedRole)?null:selectedRole);
            password.Clear();Hide();
            string? logoutNotice;
            using(var main=new MainForm(api)){main.ShowDialog();logoutNotice=main.LogoutNotice;}
            Show();ShowStatus(logoutNotice??"Đã đăng xuất. Bạn có thể đăng nhập lại.",logoutNotice!=null);userName.Focus();
        }
        catch(Exception ex){ShowStatus(ex is HttpRequestException?"Không kết nối được máy chủ. Kiểm tra địa chỉ và chứng chỉ HTTPS.":ex.Message,true);}
        finally{busy=false;login.Enabled=true;role.Enabled=true;address.Enabled=true;}
    }
    private void ShowStatus(string text,bool isError=false){status.ForeColor=isError?Theme.Danger:Theme.Muted;status.Text=text;status.AccessibleName=text;}
}

/// <summary>Lightweight scalable book and test-sheet illustration, painted entirely in GDI+.</summary>
public sealed class AcademicIllustration:Control
{
    public AcademicIllustration(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);AccessibleName="Minh họa trang sách và đề thi";AccessibleRole=AccessibleRole.Graphic;}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);if(Width<20||Height<20)return;
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        float scale=Math.Min(Width/340F,Height/260F);e.Graphics.TranslateTransform((Width-340*scale)/2,(Height-260*scale)/2);e.Graphics.ScaleTransform(scale,scale);
        using var glow=new SolidBrush(Color.FromArgb(38,255,255,255));e.Graphics.FillEllipse(glow,20,20,300,230);
        using var shadow=new SolidBrush(Color.FromArgb(35,4,22,49));e.Graphics.FillEllipse(shadow,38,218,270,20);
        using var page=new SolidBrush(Color.White);using var pale=new SolidBrush(Color.FromArgb(232,240,250));using var line=new Pen(Color.FromArgb(198,212,233),3);using var green=new Pen(Theme.Accent,4){StartCap=LineCap.Round,EndCap=LineCap.Round};
        PointF[] left=[new(26,82),new(148,99),new(148,221),new(26,204)];PointF[] right=[new(148,99),new(271,81),new(271,205),new(148,221)];
        e.Graphics.FillPolygon(page,left);e.Graphics.FillPolygon(pale,right);e.Graphics.DrawLine(line,148,101,148,220);
        for(int i=0;i<4;i++){e.Graphics.DrawLine(line,45,113+i*23,127,125+i*23);e.Graphics.DrawLine(line,168,125+i*23,248,113+i*23);}
        using var sheet=Theme.Rounded(new RectangleF(178,30,118,158),9);e.Graphics.FillPath(page,sheet);
        using var title=new SolidBrush(Theme.Primary);e.Graphics.FillRectangle(title,193,47,65,8);
        for(int i=0;i<3;i++){float y=80+i*33;e.Graphics.DrawEllipse(green,193,y,15,15);e.Graphics.DrawLine(green,196,y+7,200,y+11);e.Graphics.DrawLine(green,200,y+11,207,y+3);e.Graphics.DrawLine(line,218,y+7,278,y+7);}
        using var accent=new SolidBrush(Theme.Accent);e.Graphics.FillEllipse(accent,54,35,11,11);e.Graphics.FillEllipse(pale,312,117,7,7);
        using var pencil=new Pen(Color.FromArgb(247,202,111),12){StartCap=LineCap.Round,EndCap=LineCap.Triangle};e.Graphics.DrawLine(pencil,87,63,127,30);
    }
}
