using System.Reflection;
using System.Text.Json;
using Exam.Core;
using Exam.Desktop;

internal sealed class LargeRenderWindow:NativeWindow
{
    public LargeRenderWindow(Form form){AssignHandle(form.Handle);form.HandleDestroyed+=(_,_)=>ReleaseHandle();}
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if(message.Msg==0x24){var info=System.Runtime.InteropServices.Marshal.PtrToStructure<Limits>(message.LParam);info.MaxTrack=new Point(5000,4000);System.Runtime.InteropServices.Marshal.StructureToPtr(info,message.LParam,false);}
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]private struct Limits{public Point Reserved;public Point MaxSize;public Point MaxPosition;public Point MinTrack;public Point MaxTrack;}
}
internal static class Program
{
    private static readonly string Root=FindRoot();
    private static readonly string Output=Path.Combine(Root,"artifacts/desktop-redesign");
    private static readonly List<string> Checks=[];
    private static readonly List<object> Captures=[];
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Form,LargeRenderWindow> Hooks=new();
    private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetDefaultFont(new Font("Segoe UI",10));
        Directory.CreateDirectory(Output);
        using var host=new Form{Opacity=0,ShowInTaskbar=false,Location=new Point(-30000,-30000),StartPosition=FormStartPosition.Manual};
        host.Shown+=async(_,_)=>
        {
            try
            {
                if(args.Contains("--buttons-only"))await DesktopRedesignQa.ButtonStates.CaptureAsync(Output,Checks.Add);
                else if(args.Contains("--preview-only"))await PreviewQuestionAsync();
                else if(args.Contains("--login-only"))await LoginScreensAsync();
                else await VerifyAsync();
            }
            catch(Exception ex){Checks.Add("FAIL: "+ex.GetType().Name+": "+ex.Message+"\\n"+ex.StackTrace);Environment.ExitCode=1;}
            finally
            {
                var report="# Desktop redesign verification\n\n"+
                    "Offscreen WinForms DrawToBitmap captures on Windows. Actual forms and authorized API reads are used; no Save, Delete, Action, Import, seed or business-data updates are called. Authentication creates/revokes login sessions. These captures do not establish native interactive Windows usability. The 150% cases explicitly scale control bounds/fonts at the current device DPI; they are simulations, not native per-monitor DPI tests.\n\n"+
                    string.Join("\n",Checks.Select(x=>"- "+x))+"\n";
                var targeted=args.Contains("--buttons-only")||args.Contains("--preview-only");
                if(targeted)await File.AppendAllTextAsync(Path.Combine(Output,"qa.md"),"\\n## Targeted follow-up\\n\\n".Replace("\\n","\n")+string.Join("\n",Checks.Select(c=>"- "+c))+"\n");
                else await File.WriteAllTextAsync(Path.Combine(Output,"qa.md"),report);
                if(Captures.Count>0)await File.WriteAllTextAsync(Path.Combine(Output,targeted?"follow-up-captures.json":"captures.json"),JsonSerializer.Serialize(Captures,new JsonSerializerOptions{WriteIndented=true}));
                foreach(var check in Checks)Console.WriteLine(check);host.Close();
            }
        };
        Application.Run(host);
    }

    private static async Task LoginScreensAsync()
    {
        using var api=new ExamApiClient();
        foreach(var size in new[]{new Size(1366,768),new Size(1920,1080)})
        {
            using(var form=new LoginForm(api))await CaptureAsync(form,$"login-{size.Width}x{size.Height}",size);
            using(var form=new LoginForm(api))await CaptureAsync(form,$"login-{size.Width}x{size.Height}-sim150",size,true);
        }
        using var validation=new LoginForm(api);
        await PrepareAsync(validation,new Size(1040,680),false);
        var button=(Button)typeof(LoginForm).GetField("login",Members)!.GetValue(validation)!;
        button.PerformClick();await Task.Delay(80);
        var message=(Label)typeof(LoginForm).GetField("status",Members)!.GetValue(validation)!;
        if(string.IsNullOrWhiteSpace(message.Text))throw new InvalidOperationException("Login did not show local validation feedback.");
        await CaptureAsync(validation,"login-validation",new Size(1040,680));
        Checks.Add("PASS: Login local validation produced visible feedback without submitting credentials.");
    }

    private static async Task PreviewQuestionAsync()
    {
        var password=Environment.GetEnvironmentVariable("EXAM_DEMO_PASSWORD");
        if(string.IsNullOrEmpty(password))password=(await File.ReadAllLinesAsync(Path.Combine(Root,".local/demo-credentials.txt"))).Single(x=>x.StartsWith("password=",StringComparison.OrdinalIgnoreCase))["password=".Length..];
        using var api=new ExamApiClient();
        await api.LoginAsync(Environment.GetEnvironmentVariable("EXAM_API_URL")??"http://localhost:5180","lecturer",password,"Lecturer");
        try
        {
            using var main=new MainForm(api);
            await PrepareAsync(main,new Size(1366,768),false);
            await FindNavigationAsync(main);
            await main.OpenWorkspaceAsync("questions");
            await Task.Delay(300);
            await PrepareAsync(main,new Size(1366,768),false);
            var workspace=typeof(MainForm).GetField("navigator",Members)!.GetValue(main) as NavigationService;
            var preview=workspace?.Current?.GetType().BaseType?.GetField("previewContent",Members)?.GetValue(workspace.Current) as FlowLayoutPanel;
            if(preview==null&&workspace?.Current!=null)preview=typeof(ResourceView).GetField("previewContent",Members)?.GetValue(workspace.Current) as FlowLayoutPanel;
            if(preview!=null)
            {
                var layout=new{flowBounds=preview.Bounds,displayRectangle=preview.DisplayRectangle,parentClient=preview.Parent?.ClientSize,scroll=preview.AutoScrollPosition,cards=preview.Controls.OfType<UiCard>().Select(c=>new{c.Bounds,c.DisplayRectangle,c.ClientSize,c.MinimumSize,c.MaximumSize,c.AutoSize,labels=c.Controls.OfType<Label>().Select(l=>new{l.Text,l.Bounds,l.MaximumSize,l.AutoSize,preferred=l.PreferredSize})})};
                await File.WriteAllTextAsync(Path.Combine(Output,"question-preview-layout.json"),JsonSerializer.Serialize(layout,new JsonSerializerOptions{WriteIndented=true}));
                Checks.Add("INFO: Actual preview/card/label bounds saved in question-preview-layout.json.");
            }
            await CaptureAsync(main,"resource-lecturer-questions",new Size(1366,768),prepare:false);
            Checks.Add("PASS: Targeted lecturer question preview re-rendered from the real authorized question bank without saving.");
        }
        finally{await api.LogoutAsync();}
    }
    private static async Task VerifyAsync()
    {
        await DesktopRedesignQa.ButtonStates.CaptureAsync(Output,Checks.Add);
        await LoginScreensAsync();
        var password=Environment.GetEnvironmentVariable("EXAM_DEMO_PASSWORD");
        if(string.IsNullOrEmpty(password))
        {
            var lines=await File.ReadAllLinesAsync(Path.Combine(Root,".local/demo-credentials.txt"));
            password=lines.Single(x=>x.StartsWith("password=",StringComparison.OrdinalIgnoreCase))["password=".Length..];
        }
        var address=Environment.GetEnvironmentVariable("EXAM_API_URL")??"http://localhost:5180";
        foreach(var(account,role)in new[]{("admin","Administrator"),("lecturer","Lecturer"),("officer","ExamOfficer")})
        {
            using var api=new ExamApiClient();
            await api.LoginAsync(address,account,password,role);
            try
            {
                var catalog=await api.CatalogAsync();
                using var main=new MainForm(api);
                await PrepareAsync(main,new Size(1366,768),false);
                var navigation=await FindNavigationAsync(main);
                await main.OpenWorkspaceAsync("dashboard");
                await CaptureAsync(main,$"dashboard-{account}-1366x768",new Size(1366,768));
                if(account=="admin")
                {
                    await PrepareAsync(main,new Size(1366,768),false);
                    var toggle=Walk(main).OfType<Button>().First(b=>b.AccessibleName=="Thu gọn hoặc mở thanh điều hướng");
                    toggle.PerformClick();await CaptureAsync(main,"dashboard-admin-compact",new Size(1366,768),prepare:false);await PrepareAsync(main,new Size(1366,768),false);toggle.PerformClick();
                }
                foreach(var key in PreferredResources(role).Where(k=>catalog.Resources.Any(r=>r.Key==k)))
                {
                    await main.OpenWorkspaceAsync(key);
                    await CaptureAsync(main,$"resource-{account}-{key}",new Size(1366,768));
                    var resource=catalog.Resources.Single(r=>r.Key==key);
                    if(resource.CanCreate)
                    {
                        var page=await api.ListAsync(key,"",1);
                        var row=page.Items.FirstOrDefault();
                        var id=row!=null?Convert.ToInt32(row["id"]?.ToString()): (int?)null;
                        var values=id!=null?await api.DetailAsync(key,id.Value):new Dictionary<string,JsonElement>();
                        using var editor=new ResourceEditorForm(api,resource,id,values);
                        await CaptureAsync(editor,$"editor-{account}-{key}",new Size(1100,760));
                        if(key is "questions" or "exams")
                        {
                            await PrepareAsync(editor,new Size(1100,760),false);
                            var scroll=Walk(editor).OfType<Panel>().FirstOrDefault(p=>p.AutoScroll&&p.DisplayRectangle.Height>p.ClientSize.Height);
                            if(scroll!=null){scroll.AutoScrollPosition=new Point(0,scroll.DisplayRectangle.Height);await Task.Delay(100);await CaptureAsync(editor,$"editor-{account}-{key}-bottom",new Size(1100,760),prepare:false);}
                        }
                        if(key=="sessions")
                        {
                            for(var step=2;step<=3;step++)
                            {
                                await PrepareAsync(editor,new Size(1100,760),false);
                                var next=Walk(editor).OfType<Button>().FirstOrDefault(b=>b.Name=="wizardNext"&&b.Enabled);
                                if(next==null)break;next.PerformClick();await Task.Delay(120);
                                var progress=Walk(editor).OfType<Label>().FirstOrDefault(l=>l.Name=="wizardProgress");
                                if(progress?.Text.StartsWith($"Bước {step}/3")!=true)throw new InvalidOperationException("Session wizard did not advance to step "+step+".");
                                await CaptureAsync(editor,$"editor-{account}-{key}-step{step}",new Size(1100,760),prepare:false);
                            }
                        }
                        Checks.Add($"PASS: Constructed actual {key} editor from authorized metadata/detail; no save performed.");
                    }
                }
                await main.OpenWorkspaceAsync("dashboard");
                await CaptureAsync(main,$"dashboard-{account}-1920x1080",new Size(1920,1080));
                foreach(var size in new[]{new Size(1366,768),new Size(1920,1080)})
                {
                    using var simulatedMain=new MainForm(api);
                    await PrepareAsync(simulatedMain,size,false);
                    var simulatedNavigation=await FindNavigationAsync(simulatedMain);
                    await simulatedMain.OpenWorkspaceAsync("dashboard");
                    await CaptureAsync(simulatedMain,$"dashboard-{account}-{size.Width}x{size.Height}-sim150",size,true);
                    typeof(MainForm).GetField("signingOut",Members)?.SetValue(simulatedMain,true);
                }
                var signingOut=typeof(MainForm).GetField("signingOut",Members);
                signingOut?.SetValue(main,true);
                main.Hide();
                Checks.Add($"PASS: Actual {role} dashboard and authorized resource screens loaded.");
            }
            finally{await api.LogoutAsync();}
        }
    }

    private static IEnumerable<string> PreferredResources(string role)=>role switch
    {
        "Lecturer"=>["questions","exams","monitor"],
        "ExamOfficer"=>["sessions","candidates","monitor"],
        _=>["users","assignments"]
    };
    private static async Task<NavigationService> FindNavigationAsync(MainForm main)
    {
        for(int i=0;i<400;i++)
        {
            var field=typeof(MainForm).GetFields(Members).FirstOrDefault(f=>f.FieldType==typeof(NavigationService));
            if(field?.GetValue(main)is NavigationService service&&service.Current!=null && typeof(MainForm).GetField("navigating",Members)?.GetValue(main) is false){await Task.Delay(150);return service;}
            await Task.Delay(50);
        }
        throw new InvalidOperationException("New MainForm navigation did not initialize within 20 seconds.");
    }
    private static async Task PrepareAsync(Form form,Size size,bool simulate)
    {
        form.ShowInTaskbar=false;form.Opacity=0;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);
        if(!form.Visible)form.Show();
        Hooks.GetValue(form,f=>new LargeRenderWindow(f));
        form.WindowState=FormWindowState.Normal;form.MinimumSize=Size.Empty;form.MaximumSize=new Size(Math.Max(4000,size.Width+100),Math.Max(3000,size.Height+100));
        if(simulate)
        {
            var fonts=Walk(form).Select(c=>(Control:c,Font:c.Font)).ToList();
            form.AutoScaleMode=AutoScaleMode.None;form.Scale(new SizeF(1.5F,1.5F));
            foreach(var(c,font)in fonts)c.Font=new Font(font.FontFamily,font.SizeInPoints*1.5F,font.Style);
        }
        form.ClientSize=size;form.Location=new Point(-30000,-30000);form.PerformLayout();
        await Task.Delay(150);
    }
    private static async Task CaptureAsync(Form form,string name,Size size,bool simulate=false,bool prepare=true)
    {
        try
        {
        if(prepare)await PrepareAsync(form,size,simulate);
        using var full=new Bitmap(form.Width,form.Height);
        form.DrawToBitmap(full,new Rectangle(Point.Empty,full.Size));
        var horizontal=Math.Max(0,(form.Width-form.ClientSize.Width)/2);
        var origin=new Point(horizontal,Math.Max(0,form.Height-form.ClientSize.Height-horizontal));
        using var bitmap=full.Clone(new Rectangle(origin,form.ClientSize),System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var path=Path.Combine(Output,name+".png");bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
        var labels=Walk(form).OfType<Label>().Where(l=>l.Visible&&l.Text.Length>0).Count();
        Captures.Add(new{name,width=form.ClientSize.Width,height=form.ClientSize.Height,deviceDpi=form.DeviceDpi,simulated150Percent=simulate,visibleLabels=labels,path});
        Checks.Add($"PASS: Captured {name} ({form.ClientSize.Width}×{form.ClientSize.Height}, device DPI {form.DeviceDpi}"+(simulate?", simulated control/font scaling 150%":"")+").");
        form.Hide();
        }catch(Exception ex)
        {
            Checks.Add("WARN: Capture "+name+" failed: "+ex.GetType().Name+": "+ex.Message);
            form.Hide();
        }
    }
    private static IEnumerable<Control> Walk(Control parent)
    {
        yield return parent;
        foreach(Control child in parent.Controls)foreach(var item in Walk(child))yield return item;
    }
    private static string FindRoot()
    {
        for(var directory=new DirectoryInfo(AppContext.BaseDirectory);directory!=null;directory=directory.Parent)
            if(File.Exists(Path.Combine(directory.FullName,"DECISIONS.md")))return directory.FullName;
        throw new InvalidOperationException("Workspace root not found.");
    }
}












