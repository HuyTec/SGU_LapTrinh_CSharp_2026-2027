namespace Exam.Desktop;

/// <summary>Inline feedback. Errors remain visible until the user explicitly dismisses them.</summary>
public class UiToast:UserControl
{
    private readonly Label message=new(){Dock=DockStyle.Fill,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(12,8,8,8)};
    private readonly UiButton close=new(){Text="×",Kind=UiButtonKind.Ghost,Dock=DockStyle.Right,Width=40,AccessibleName="Đóng thông báo",Padding=Padding.Empty,TabStop=true};
    private readonly System.Windows.Forms.Timer dismiss=new(){Interval=6000};
    public UiToast()
    {
        Height=56;Dock=DockStyle.Top;Visible=false;AccessibleRole=AccessibleRole.Alert;
        Font=Theme.BodyFont;Controls.Add(message);Controls.Add(close);
        close.Click+=(_,_)=>{dismiss.Stop();Visible=false;};
        dismiss.Tick+=(_,_)=>{dismiss.Stop();Visible=false;};
    }
    public void ShowMessage(string text,bool isError=false)
    {
        dismiss.Stop();message.Text=text;AccessibleName=(isError?"Lỗi: ":"Thông báo: ")+text;
        BackColor=isError?Color.FromArgb(253,236,236):Theme.SoftAccent;
        message.ForeColor=isError?Color.FromArgb(136,25,25):Color.FromArgb(25,91,62);
        close.BackColor=BackColor;
        Visible=true;BringToFront();
        AccessibilityNotifyClients(AccessibleEvents.SystemAlert,-1);
        if(!isError)dismiss.Start();
    }
    protected override void Dispose(bool disposing){if(disposing)dismiss.Dispose();base.Dispose(disposing);}
}

public static class UiConfirmDialog
{
    public static bool Confirm(IWin32Window owner,string title,string message,string confirmText="Xác nhận",bool danger=false)
    {
        using var dialog=new Form{Text=title,FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(520,280),MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(460,240)};
        Theme.Apply(dialog);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=1,Padding=new Padding(28),MaximumSize=new Size(600,0)};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        layout.Controls.Add(new Label{Text=title,AutoSize=true,Font=Theme.SectionFont,ForeColor=danger?Theme.Danger:Theme.Text,Margin=new Padding(0,0,0,16)});
        layout.Controls.Add(new Label{Text=message,AutoSize=true,MaximumSize=new Size(460,0),Margin=new Padding(0,0,0,24)});
        var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};
        var confirm=new UiButton{Text=confirmText,Kind=danger?UiButtonKind.Danger:UiButtonKind.Primary,AutoSize=true,DialogResult=DialogResult.OK,AccessibleName=confirmText};
        var cancel=new UiButton{Text="Quay lại",Kind=UiButtonKind.Secondary,AutoSize=true,DialogResult=DialogResult.Cancel};
        actions.Controls.Add(confirm);actions.Controls.Add(cancel);layout.Controls.Add(actions);dialog.Controls.Add(layout);
        dialog.AcceptButton=confirm;dialog.CancelButton=cancel;
        dialog.Shown+=(_,_)=>{if(danger)cancel.Focus();else confirm.Focus();};
        return dialog.ShowDialog(owner)==DialogResult.OK;
    }
}
