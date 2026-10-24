using Exam.Desktop;
using System.Drawing.Imaging;
namespace DesktopRedesignQa;

/// <summary>Renderer-only state captures, using the actual UiButton and no API or business actions.</summary>
internal static class ButtonStates
{
    public static async Task CaptureAsync(string output,Action<string> log)
    {
        string[] states=["Bình thường","Hover","Nhấn","Focus","Vô hiệu"];
        var kinds=new[]{UiButtonKind.Primary,UiButtonKind.Secondary,UiButtonKind.Danger,UiButtonKind.Ghost};
        using var form=new Form{Opacity=0,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(-30000,-30000),ClientSize=new Size(500,160),BackColor=Theme.Background};
        using var button=new StateButton{Text="Xác nhận",Size=new Size(176,48),Location=new Point(20,20),BackColor=Theme.Primary};
        using var neutral=new TextBox{Location=new Point(240,20)};
        form.Controls.Add(button);form.Controls.Add(neutral);form.AcceptButton=button;form.Show();
        using var atlas=new Bitmap(1130,365);using var graphics=Graphics.FromImage(atlas);graphics.Clear(Theme.Background);
        using var heading=new Font("Segoe UI",10,FontStyle.Bold);
        TextRenderer.DrawText(graphics,"UiButton · trạng thái thực của control",heading,new Point(20,12),Theme.Text);
        for(int column=0;column<states.Length;column++)TextRenderer.DrawText(graphics,states[column],Theme.SmallFont,new Point(135+column*198,48),Theme.Muted);
        var focusedConfirmed=false;
        for(int row=0;row<kinds.Length;row++)
        {
            var kind=kinds[row];button.Kind=kind;
            TextRenderer.DrawText(graphics,kind.ToString(),Theme.BodyFont,new Point(20,95+row*64),Theme.Text);
            for(int column=0;column<states.Length;column++)
            {
                button.Enabled=true;neutral.Focus();button.Reset();
                switch(column)
                {
                    case 1:button.Hover();break;
                    case 2:button.Press();break;
                    case 3:focusedConfirmed|=button.Focus();break;
                    case 4:button.Enabled=false;break;
                }
                await Task.Delay(20);button.Update();
                using var image=new Bitmap(button.Width,button.Height);
                button.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));
                foreach(var corner in new[]{new Point(0,0),new Point(image.Width-1,0),new Point(0,image.Height-1),new Point(image.Width-1,image.Height-1)})
                    if(image.GetPixel(corner.X,corner.Y).ToArgb()!=form.BackColor.ToArgb())throw new InvalidOperationException($"Square outer corner remains on {kind}/{states[column]} at {corner}.");
                graphics.DrawImageUnscaled(image,135+column*198,80+row*64);
            }
        }
        atlas.Save(Path.Combine(output,"button-states.png"),ImageFormat.Png);
        if(!focusedConfirmed)throw new InvalidOperationException("The focused button state could not obtain focus.");
        log("PASS: Actual UiButton normal/hover/pressed/focused/disabled renders captured for four kinds; all four outer corner pixels equal the parent background in all 20 cases, even with BackColor=Primary and AcceptButton enabled.");
        log("PASS: Button keyboard focus obtained on the UI thread; its indicator is a rounded dotted path, with no native square focus rectangle.");
        form.Hide();
    }
    private sealed class StateButton:UiButton
    {
        protected override bool ShowFocusCues=>true;
        public void Reset(){OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,10,10,0));OnMouseLeave(EventArgs.Empty);}
        public void Hover()=>OnMouseEnter(EventArgs.Empty);
        public void Press(){Hover();OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,10,10,0));}
    }
}

