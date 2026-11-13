using Exam.Core;
using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace Exam.Desktop;

/// <summary>Desktop sign-in and connection settings. Authentication and role decisions remain on the API.</summary>
public sealed class LoginForm:Form
{
    private readonly ExamApiClient api;
    private readonly TextBox address=new(){Text=Environment.GetEnvironmentVariable("EXAM_API_URL")??"http://localhost:5180",Dock=DockStyle.Fill};
    private readonly TextBox userName=new(){Dock=DockStyle.Fill,PlaceholderText="Tên tài khoản của bạn",AccessibleName="Tên đăng nhập"};
    private readonly TextBox password=new(){UseSystemPasswordChar=true,Dock=DockStyle.Fill,PlaceholderText="Nhập mật khẩu",AccessibleName="Mật khẩu"};
    private readonly ComboBox role=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Label",AccessibleName="Vai trò hoạt động"};
    private readonly Label status=new(){AutoSize=true,ForeColor=Theme.Muted,MaximumSize=new Size(520,0),Margin=new Padding(0,8,0,8),AccessibleRole=AccessibleRole.StatusBar};
    private readonly UiButton login=new(){Text="Đăng nhập",Kind=UiButtonKind.Primary,Dock=DockStyle.Fill,Height=44,AccessibleName="Đăng nhập nhân sự"};
    private readonly ErrorProvider errors=new(){BlinkStyle=ErrorBlinkStyle.NeverBlink};
    private readonly TableLayoutPanel columns=new(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,Padding=Padding.Empty};
    private readonly Panel brand=new(){Dock=DockStyle.Fill,BackColor=Theme.Primary,Margin=Padding.Empty};
    private readonly TableLayoutPanel settings=new(){AutoSize=true,Dock=DockStyle.Top,ColumnCount=1,Visible=false,Margin=new Padding(0,0,0,8)};
    private readonly UiButton serverToggle=new(){Text="Cài đặt máy chủ",Kind=UiButtonKind.Ghost,AutoSize=true};
    private bool busy;

    public LoginForm(ExamApiClient api)
    {
        this.api=api;Theme.Apply(this);errors.ContainerControl=this;
        Text="Exam — Không gian khảo thí";StartPosition=FormStartPosition.CenterScreen;
        ClientSize=new Size(1040,680);MinimumSize=new Size(760,590);
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));
        Controls.Add(columns);columns.Controls.Add(brand,0,0);
        BuildBrand();
        var right=new Panel{Dock=DockStyle.Fill,BackColor=Theme.Surface,Padding=new Padding(42,24,42,20),AutoScroll=true,Margin=Padding.Empty};
        columns.Controls.Add(right,1,0);
        var form=new TableLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,ColumnCount=1,Padding=Padding.Empty,Margin=Padding.Empty};
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));right.Controls.Add(form);
        form.SizeChanged+=(_,_)=>{foreach(var label in form.Controls.OfType<Label>())label.MaximumSize=new Size(Math.Max(220,form.ClientSize.Width),0);};
        form.Controls.Add(new Label{Text="KHÔNG GIAN KHẢO THÍ",Font=new Font("Arial",9F,FontStyle.Bold),ForeColor=Theme.Primary,AutoSize=true,Margin=new Padding(0,8,0,12)});
        form.Controls.Add(new Label{Text="Chào mừng trở lại",Font=Theme.TitleFont,ForeColor=Theme.Text,AutoSize=true,Margin=new Padding(0,0,0,8)});
        form.Controls.Add(new Label{Text="Đăng nhập để chuẩn bị, tổ chức và đánh giá kỳ thi.",ForeColor=Theme.Muted,AutoSize=true,MaximumSize=new Size(520,0),Margin=new Padding(0,0,0,24)});
        AddField(form,"Tên đăng nhập",userName);
        var passwordRow=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};
        passwordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));passwordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,96));
        var reveal=new UiButton{Text="Hiện",Kind=UiButtonKind.Ghost,Dock=DockStyle.Fill,Height=32,Padding=new Padding(4),Margin=Padding.Empty,AccessibleName="Hiện mật khẩu"};
        Theme.StyleInput(password);password.Margin=Padding.Empty;
        passwordRow.Controls.Add(password,0,0);passwordRow.Controls.Add(reveal,1,0);
        reveal.Click+=(_,_)=>{password.UseSystemPasswordChar=!password.UseSystemPasswordChar;reveal.Text=password.UseSystemPasswordChar?"Hiện":"Ẩn";reveal.AccessibleName=password.UseSystemPasswordChar?"Hiện mật khẩu":"Ẩn mật khẩu";};
        AddField(form,"Mật khẩu",passwordRow);
        role.Items.Add(new ChoiceDto("","Tự động chọn vai trò"));
        foreach(var name in new[]{"Administrator","Lecturer","ExamOfficer"})role.Items.Add(new ChoiceDto(name,UiLabels.Of(name)));
        role.SelectedIndex=0;AddField(form,"Vai trò hoạt động",role);
        form.Controls.Add(new Label{Text="Chỉ chọn vai trò bạn đã được quản trị viên cấp.",ForeColor=Theme.Muted,Font=Theme.SmallFont,AutoSize=true,Margin=new Padding(0,0,0,16)});
        form.Controls.Add(new Label{Text="Quên mật khẩu? Liên hệ quản trị viên.",AutoSize=true,Font=Theme.SmallFont,ForeColor=Theme.Muted,Margin=new Padding(0,0,0,12)});
        form.Controls.Add(login);form.Controls.Add(status);
        form.Controls.Add(serverToggle);
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        settings.Controls.Add(new Label{Text="Địa chỉ máy chủ",AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(0,8,0,4)});
        Theme.StyleInput(address);settings.Controls.Add(address);
        settings.Controls.Add(new Label{Text="Dùng địa chỉ do nhóm quản trị cung cấp.",ForeColor=Theme.Muted,Font=Theme.SmallFont,AutoSize=true,Margin=new Padding(0,0,0,4)});
        form.Controls.Add(settings);
        serverToggle.Click+=(_,_)=>{settings.Visible=!settings.Visible;serverToggle.Text=settings.Visible?"Ẩn cài đặt máy chủ":"Cài đặt máy chủ";if(settings.Visible)address.Focus();};
        var links=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true,Margin=new Padding(0,10,0,0)};
        var help=new UiButton{Text="Hướng dẫn",Kind=UiButtonKind.Ghost,AutoSize=true};
        var register=new UiButton{Text="Đăng ký tài khoản",Kind=UiButtonKind.Ghost,AutoSize=true};
        help.Click+=(_,_)=>{using var helpForm=new HelpForm();helpForm.ShowDialog(this);};
        register.Click+=(_,_)=>OpenRegistration();links.Controls.Add(help);links.Controls.Add(register);form.Controls.Add(links);
        form.Controls.Add(new Label{Text="Thí sinh làm bài trên Web. Tài khoản nhân sự mới cần được duyệt.",AutoSize=true,MaximumSize=new Size(520,0),Font=Theme.SmallFont,ForeColor=Theme.Muted,Margin=new Padding(0,10,0,0)});
        AcceptButton=login;login.Click+=async(_,_)=>await LoginAsync();
        userName.TextChanged+=(_,_)=>errors.SetError(userName,"");password.TextChanged+=(_,_)=>errors.SetError(password,"");
        Shown+=(_,_)=>{var area=Screen.FromControl(this).WorkingArea;MinimumSize=new Size(Math.Min(MinimumSize.Width,area.Width-24),Math.Min(MinimumSize.Height,area.Height-24));if(Width>area.Width-24)Width=area.Width-24;if(Height>area.Height-24)Height=area.Height-24;CenterToScreen();userName.Focus();};
        Resize+=(_,_)=>UpdateResponsiveColumns();UpdateResponsiveColumns();
        FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};
    }
    private static void AddField(TableLayoutPanel form,string title,Control input)
    {
        form.Controls.Add(new Label{Text=title,AutoSize=true,ForeColor=Theme.Text,Margin=new Padding(0,0,0,6)});
        if(input is not TableLayoutPanel)Theme.StyleInput(input);
        input.Margin=new Padding(0,0,0,16);form.Controls.Add(input);
    }
    private void BuildBrand()
    {
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(32),BackColor=Theme.Primary};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,80));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,120));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
        layout.Controls.Add(new Label{Text="EXAM\nKhông gian khảo thí",AutoSize=true,Font=Theme.SectionFont,ForeColor=Color.White,Dock=DockStyle.Fill,Margin=Padding.Empty});
        layout.Controls.Add(new AcademicIllustration{Dock=DockStyle.Fill,BackColor=Theme.Primary,Margin=new Padding(0,8,0,8)});
        var process=new TableLayoutPanel{ColumnCount=1,RowCount=3,Dock=DockStyle.Fill,Margin=Padding.Empty};
        for(var i=0;i<3;i++)process.RowStyles.Add(new RowStyle(SizeType.Percent,100F/3));
        foreach(var text in new[]{"01   Chuẩn bị ngân hàng và đề thi","02   Tổ chức kỳ thi rõ ràng","03   Đánh giá bằng kết quả tin cậy"})
            process.Controls.Add(new Label{Text=text,AutoSize=true,ForeColor=Color.White,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty});
        layout.Controls.Add(process);
        layout.Controls.Add(new Label{Text="Một hệ thống · Dữ liệu dùng chung",Font=Theme.SmallFont,ForeColor=Color.FromArgb(221,234,250),AutoSize=true,Margin=Padding.Empty});
        brand.Controls.Add(layout);
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
    private async Task LoginAsync()
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
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}

/// <summary>Lightweight scalable book and test-sheet illustration, painted entirely in GDI+.</summary>
internal sealed class AcademicIllustration:Control
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




