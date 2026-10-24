using System.Drawing.Drawing2D;
namespace Exam.Desktop;

/// <summary>Academic desktop palette and reusable WinForms styling; no business decisions live here.</summary>
public static class Theme
{
    public static readonly Color Background = ColorTranslator.FromHtml("#F5F7FA");
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = ColorTranslator.FromHtml("#E1E6EC");
    public static readonly Color Text = ColorTranslator.FromHtml("#1F2937");
    public static readonly Color Muted = ColorTranslator.FromHtml("#626D7C");
    public static readonly Color Primary = ColorTranslator.FromHtml("#1E4E8C");
    public static readonly Color PrimaryHover = ColorTranslator.FromHtml("#163B6B");
    public static readonly Color Accent = ColorTranslator.FromHtml("#2A9D8F");
    public static readonly Color Danger = ColorTranslator.FromHtml("#B83232");
    public static readonly Color Success = ColorTranslator.FromHtml("#2E9E5B");
    public static readonly Color Warning = ColorTranslator.FromHtml("#E9A23B");
    public static readonly Color Info = ColorTranslator.FromHtml("#3B82F6");
    public static readonly Color SoftPrimary = ColorTranslator.FromHtml("#EAF0F8");
    public static readonly Color SoftAccent = ColorTranslator.FromHtml("#E8F5F1");
    public static readonly Font BodyFont = new("Segoe UI", 10F);
    public static readonly Font SmallFont = new("Segoe UI", 9F);
    public static readonly Font TitleFont = new("Segoe UI", 20F, FontStyle.Bold);
    public static readonly Font SectionFont = new("Segoe UI", 13F, FontStyle.Bold);

    public static void Apply(Form form)
    {
        form.AutoScaleDimensions = new SizeF(96F,96F);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.Font = BodyFont;
        form.BackColor = Background;
        form.ForeColor = Text;
    }

    public static void StyleInput(Control control)
    {
        control.Font = BodyFont;
        control.ForeColor = Text;
        if(control is TextBoxBase or ComboBox or NumericUpDown or DateTimePicker)
            control.BackColor = Surface;
        if(control is TextBoxBase text) text.BorderStyle = BorderStyle.FixedSingle;
        if(control is ComboBox combo) combo.FlatStyle = FlatStyle.Standard;
        control.Margin = new Padding(0, 4, 0, 12);
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.Font = BodyFont;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor=Background, ForeColor=Muted, Font=new Font("Segoe UI",9F,FontStyle.Bold),
            Padding=new Padding(12,0,8,0), SelectionBackColor=Background, SelectionForeColor=Text
        };
        grid.ColumnHeadersHeight = 42;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor=Surface, ForeColor=Text, SelectionBackColor=SoftPrimary,
            SelectionForeColor=Text, Padding=new Padding(12,5,8,5), NullValue="—"
        };
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250,251,253);
        grid.RowTemplate.Height = 40;
        grid.RowHeadersVisible = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.AllowUserToResizeRows = false;
    }

    public static string IconFor(string resourceKey) => resourceKey switch
    {
        "home" or "dashboard" => "\uE80F", "questions" => "\uE8F2", "topics" => "\uE8B7",
        "subjects" => "\uE736", "exams" => "\uE8A5", "sessions" => "\uE787",
        "users" => "\uE716", "assignments" => "\uE8D4", "candidates" => "\uE77B",
        "monitor" => "\uE9D9", "help" => "\uE897", "logout" => "\uE8BB", _ => "\uE8A5"
    };

    internal static GraphicsPath Rounded(RectangleF rectangle,float radius)
    {
        var path=new GraphicsPath();
        var d=Math.Min(radius*2,Math.Min(rectangle.Width,rectangle.Height));
        if(d<=0){path.AddRectangle(rectangle);return path;}
        path.AddArc(rectangle.Left,rectangle.Top,d,d,180,90);
        path.AddArc(rectangle.Right-d,rectangle.Top,d,d,270,90);
        path.AddArc(rectangle.Right-d,rectangle.Bottom-d,d,d,0,90);
        path.AddArc(rectangle.Left,rectangle.Bottom-d,d,d,90,90);
        path.CloseFigure();return path;
    }
}

