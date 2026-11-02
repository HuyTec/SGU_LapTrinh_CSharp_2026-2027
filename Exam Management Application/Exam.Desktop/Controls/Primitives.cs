using System.Drawing.Drawing2D;
namespace Exam.Desktop;

public enum UiButtonKind { Primary, Secondary, Danger, Ghost }
public class UiButton : Button
{
    private UiButtonKind kind;
    private bool hovered,pressed;
    [System.ComponentModel.DefaultValue(UiButtonKind.Primary)]
    public UiButtonKind Kind {get=>kind;set{kind=value;Invalidate();}}
    public UiButton()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;
        Font=Theme.BodyFont;ForeColor=Theme.Primary;BackColor=Theme.Surface;Cursor=Cursors.Hand;Height=40;Padding=new Padding(16,8,16,8);
        UseVisualStyleBackColor=false;Margin=new Padding(0,0,8,0);
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        var text=TextRenderer.MeasureText(Text,Font);
        return new Size(Math.Max(88,text.Width+Padding.Horizontal+(Image?.Width??0)+(Image!=null?8:0)),Math.Max(40,text.Height+Padding.Vertical));
    }
    protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hovered=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){pressed=e.Button==MouseButtons.Left;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background=new SolidBrush(Parent?.BackColor??Theme.Background);
        e.Graphics.FillRectangle(background,e.ClipRectangle);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        // Native Button background/default borders must never leave a square outside the rounded fill.
        using var background=new SolidBrush(Parent?.BackColor??Theme.Background);
        e.Graphics.FillRectangle(background,e.ClipRectangle);
        var scale=DeviceDpi/96F;
        var fill=Kind switch{UiButtonKind.Primary=>hovered?Theme.PrimaryHover:Theme.Primary,UiButtonKind.Danger=>hovered?Color.FromArgb(151,37,37):Theme.Danger,UiButtonKind.Ghost=>hovered?Theme.SoftPrimary:Parent?.BackColor??Theme.Surface, _=>hovered?Theme.SoftPrimary:Theme.Surface};
        var ink=Kind is UiButtonKind.Primary or UiButtonKind.Danger?Color.White:Kind==UiButtonKind.Ghost&&!hovered?ForeColor:Theme.Primary;
        if(!Enabled){fill=Color.FromArgb(233,236,241);ink=Theme.Muted;}
        if(pressed&&Enabled)fill=ControlPaint.Dark(fill,.08F);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=Theme.Rounded(new RectangleF(.5F,.5F,Width-1,Height-1),7*scale);
        using var brush=new SolidBrush(fill);e.Graphics.FillPath(brush,path);
        if(Kind==UiButtonKind.Secondary){using var pen=new Pen(Theme.Border);e.Graphics.DrawPath(pen,path);}
        var bounds=ClientRectangle;bounds.Inflate(-(int)(8*scale),-(int)(2*scale));
        if(Image!=null){var x=(int)(12*scale);e.Graphics.DrawImage(Image,x,(Height-Image.Height)/2,Image.Width,Image.Height);bounds.X=x+Image.Width+8;bounds.Width=Width-bounds.X-8;}
        TextRenderer.DrawText(e.Graphics,Text,Font,bounds,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if(Focused&&ShowFocusCues)
        {
            using var focus=Theme.Rounded(new RectangleF(4.5F*scale,4.5F*scale,Width-9*scale,Height-9*scale),4*scale);
            using var focusPen=new Pen(ink,Math.Max(1F,scale)){DashStyle=DashStyle.Dot};
            e.Graphics.DrawPath(focusPen,focus);
        }
    }
}
public class UiCard:Panel
{
    public UiCard(){DoubleBuffered=true;BackColor=Theme.Surface;Padding=new Padding(20);SetStyle(ControlStyles.ResizeRedraw,true);}
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background=new SolidBrush(Parent?.BackColor??Theme.Background);
        e.Graphics.FillRectangle(background,e.ClipRectangle);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var fillPath=Theme.Rounded(new RectangleF(0,0,Width-1,Height-1),8*DeviceDpi/96F);
        using var fill=new SolidBrush(BackColor);e.Graphics.FillPath(fill,fillPath);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=Theme.Rounded(new RectangleF(.5F,.5F,Width-1,Height-1),8*DeviceDpi/96F);
        using var pen=new Pen(Theme.Border);e.Graphics.DrawPath(pen,path);
    }
}
public class UiDataGridView:DataGridView
{
    public UiDataGridView(){DoubleBuffered=true;Theme.StyleGrid(this);}
}






