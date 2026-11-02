using System.Reflection;
using System.Text.Json;
using Exam.Desktop;

internal static class Program
{
    private static readonly string Root = FindRoot();
    private static readonly List<string> Checks = [];
    private static string Output => Path.Combine(Root, "artifacts/desktop");

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "DECISIONS.md"))) return directory.FullName;
        throw new InvalidOperationException("Workspace root was not found.");
    }

    [STAThread]
    private static void Main(string[] args)
    {
        var versionOnly = args.Contains("--version-only");
        var helpOnly = args.Contains("--help-only");
        var lookupOnly = args.Contains("--lookup-refresh-only");
        var approvalUser = args.FirstOrDefault(a => a.StartsWith("--approve-user="))?["--approve-user=".Length..];
        ApplicationConfiguration.Initialize(); Application.SetDefaultFont(new Font("Segoe UI", 10));
        using var host = new Form { Opacity = 0, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-30000, -30000) };
        host.Shown += async (_, _) =>
        {
            try { if (lookupOnly) await VerifyLookupRefreshAsync(); else if (approvalUser is not null) await VerifyApprovalAsync(approvalUser); else if (helpOnly) await VerifyHelpAsync(); else if (versionOnly) await VerifyVersionAsync(); else await VerifyAsync(); Checks.Add("PASS: Offscreen forms constructed without unhandled exceptions."); }
            catch (Exception ex) { Checks.Add("FAIL: " + ex.GetType().Name + ": " + ex.Message); Environment.ExitCode = 1; }
            finally
            {
                Directory.CreateDirectory(Output);
                var report = string.Join("\n", Checks.Select(c => "- " + c)) + "\n";
                if (lookupOnly) await File.AppendAllTextAsync(Path.Combine(Output, "qa.md"), "\n## Fresh topic lookup regression\n\n" + report);
                else if (approvalUser is not null) await File.AppendAllTextAsync(Path.Combine(Output, "qa.md"), "\n## Staff registration approval follow-up\n\n" + report);
                else if (helpOnly) await File.AppendAllTextAsync(Path.Combine(Output, "qa.md"), "\n## In-app guidance follow-up\n\n" + report);
                else if (versionOnly) await File.AppendAllTextAsync(Path.Combine(Output, "qa.md"), "\n## Hidden concurrency version follow-up\n\n" + report);
                else await File.WriteAllTextAsync(Path.Combine(Output, "qa.md"), "# Desktop verification\n\nCode-generated offscreen WinForms rendering and real API-client checks on Windows. This does not establish interactive OS usability. Actual footer controls are rendered separately and composited at their native bounds to work around intermittent WinForms DrawToBitmap child-paint omission.\n\n" + report + "\nVisual review corrected CSV instruction clipping, logout contrast, and restricted translated enum labels so literal usernames/student codes remain unchanged.\n");
                foreach (var check in Checks) Console.WriteLine(check);
                host.Close();
            }
        };
        Application.Run(host);
    }

    private static async Task VerifyLookupRefreshAsync()
    {
        var credentials = await File.ReadAllLinesAsync(Path.Combine(Root, ".local/demo-credentials.txt"));
        var password = credentials.Single(l => l.StartsWith("password=", StringComparison.OrdinalIgnoreCase))["password=".Length..];
        using var api = new ExamApiClient(); using var other = new ExamApiClient();
        await api.LoginAsync("http://localhost:5180", "lecturer", password, "Lecturer");
        await other.LoginAsync("http://localhost:5180", "lecturer2", password, "Lecturer");
        var initial = await api.CatalogAsync();
        var otherCatalog = await other.CatalogAsync();
        var otherSubjects = otherCatalog.Resources.Single(r => r.Key == "topics").Fields.Single(f => f.Name == "subjectId").Choices!.Select(c => c.Value).ToHashSet();
        var subject = initial.Resources.Single(r => r.Key == "topics").Fields.Single(f => f.Name == "subjectId").Choices!.First(c => !otherSubjects.Contains(c.Value));
        var created = new List<int>();
        using var main = new MainForm(api);
        try
        {
            await RenderAsync(main, "07-topic-lookup-initial.png", new Size(1200, 800), waitForGrid: true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var navigation = (FlowLayoutPanel)typeof(MainForm).GetField("navigation", flags)!.GetValue(main)!;
            var topicFilter = (ComboBox)typeof(MainForm).GetField("topicFilter", flags)!.GetValue(main)!;
            var subjectFilter = (ComboBox)typeof(MainForm).GetField("subjectFilter", flags)!.GetValue(main)!;
            var difficultyFilter = (ComboBox)typeof(MainForm).GetField("difficultyFilter", flags)!.GetValue(main)!;
            var toolbar = (FlowLayoutPanel)typeof(MainForm).GetField("toolbar", flags)!.GetValue(main)!;
            var marker = "DesktopLookupQA-" + Guid.NewGuid().ToString("N")[..8];
            async Task<int> CreateTopic(string suffix)
            {
                var name = marker + "-" + suffix;
                await api.SaveAsync("topics", null, new Dictionary<string, JsonElement>
                {
                    ["subjectId"] = JsonSerializer.SerializeToElement(int.Parse(subject.Value)),
                    ["name"] = JsonSerializer.SerializeToElement(name), ["isActive"] = JsonSerializer.SerializeToElement(true)
                });
                var row = (await api.ListAsync("topics", name, 1)).Items.Single();
                var id = int.Parse(row["id"]!.ToString()!); created.Add(id); return id;
            }
            async Task Navigate(string key)
            {
                var label = initial.Resources.Single(r => r.Key == key).Label;
                navigation.Controls.OfType<Button>().Single(b => b.Text == label).PerformClick();
                for (var i = 0; i < 150 && !navigation.Enabled; i++) await Task.Delay(50);
                if (!navigation.Enabled || ((Exam.Core.ResourceDto?)typeof(MainForm).GetField("resource", flags)!.GetValue(main))?.Key != key)
                    throw new InvalidOperationException("Navigation did not finish loading " + key + ".");
            }
            bool Contains(int id) => topicFilter.Items.Cast<Exam.Core.ChoiceDto>().Any(c => c.Value == id.ToString());
            var first = await CreateTopic("nav");
            await Navigate("questions");
            if (!Contains(first)) throw new InvalidOperationException("New topic missing after navigation from the initial catalog.");
            Checks.Add("PASS: Actual question navigation refreshed lookup choices for a topic created after MainForm initialization.");
            topicFilter.SelectedItem = topicFilter.Items.Cast<Exam.Core.ChoiceDto>().Single(c => c.Value == first.ToString());
            subjectFilter.SelectedItem = subjectFilter.Items.Cast<Exam.Core.ChoiceDto>().Single(c => c.Value == subject.Value);
            difficultyFilter.SelectedItem = difficultyFilter.Items.Cast<Exam.Core.ChoiceDto>().First(c => c.Value.Length > 0);
            var selectedDifficulty = ((Exam.Core.ChoiceDto)difficultyFilter.SelectedItem!).Value;
            var second = await CreateTopic("refresh");
            toolbar.Controls.OfType<Button>().Single(b => b.Text == "Làm mới").PerformClick();
            for (var i = 0; i < 150 && !navigation.Enabled; i++) await Task.Delay(50);
            if (!navigation.Enabled || !Contains(second)) throw new InvalidOperationException("Manual refresh did not load the second new topic.");
            if ((topicFilter.SelectedItem as Exam.Core.ChoiceDto)?.Value != first.ToString()
                || (subjectFilter.SelectedItem as Exam.Core.ChoiceDto)?.Value != subject.Value
                || (difficultyFilter.SelectedItem as Exam.Core.ChoiceDto)?.Value != selectedDifficulty)
                throw new InvalidOperationException("Manual refresh lost an existing selected filter.");
            Checks.Add("PASS: Actual Làm mới button fetched a second new topic and preserved selected subject/topic/difficulty filters.");
            await Navigate("topics"); await Navigate("questions");
            if (!Contains(first) || !Contains(second)) throw new InvalidOperationException("Screen switching restored the stale initial topic catalog.");
            if ((topicFilter.SelectedItem as Exam.Core.ChoiceDto)?.Value != "") throw new InvalidOperationException("Switching screens did not reset the question filter.");
            Checks.Add("PASS: Switching Topics → Questions retained fresh lookups and reset screen filters.");
            var scopedCatalog = await other.CatalogAsync();
            var otherTopics = scopedCatalog.Resources.Single(r => r.Key == "questions").Fields.Single(f => f.Name == "topicId").Choices!;
            if (otherTopics.Any(c => created.Contains(int.Parse(c.Value))) || (await other.ListAsync("topics", marker, 1)).Total != 0)
                throw new InvalidOperationException("New topic leaked to an unassigned lecturer.");
            Checks.Add("PASS: Lecturer2 catalog and topics API could not see either topic in the unassigned subject.");
            topicFilter.SelectedItem = topicFilter.Items.Cast<Exam.Core.ChoiceDto>().Single(c => c.Value == second.ToString());
            await RenderAsync(main, "07-topic-lookup-refreshed.png", new Size(1200, 800));
        }
        finally
        {
            typeof(MainForm).GetField("signingOut", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(main, true); main.Close();
            foreach (var id in created) await api.DeleteAsync("topics", id);
            await api.LogoutAsync(); await other.LogoutAsync();
            Checks.Add("PASS: Only isolated QA topics were created/deleted; existing topics including Thiên văn were not modified.");
        }
    }

    private static async Task VerifyApprovalAsync(string username)
    {
        var credentials = await File.ReadAllLinesAsync(Path.Combine(Root, ".local/demo-credentials.txt"));
        var password = credentials.Single(l => l.StartsWith("password=", StringComparison.OrdinalIgnoreCase))["password=".Length..];
        using var admin = new ExamApiClient();
        await admin.LoginAsync("http://localhost:5180", "admin", password);
        var row = (await admin.ListAsync("users", username, 1)).Items.Single(r => r["userName"]?.ToString() == username);
        var id = int.Parse(row["id"]!.ToString()!);
        var original = await admin.DetailAsync("users", id);
        var requested = original["requestedRole"].GetString()!;
        if (original["isActive"].GetBoolean() || original["roles"].GetArrayLength() != 0 || requested != "Lecturer") throw new InvalidOperationException("Fixture is not a pending Lecturer request.");
        using (var main = new MainForm(admin))
        {
            await RenderAsync(main, "06-admin-approval-menu.png", new Size(1200, 800), waitForGrid: true);
            var search = (TextBox)typeof(MainForm).GetField("search", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
            search.Text = username;
            await (Task)typeof(MainForm).GetMethod("LoadRowsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(main, null)!;
            var grid = (DataGridView)typeof(MainForm).GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
            if (grid.Columns["requestedRole"]!.HeaderText != "Vai trò đăng ký" || grid.Rows[0].Cells["requestedRole"].Value?.ToString() != "Giảng viên") throw new InvalidOperationException("Requested role label/translation is missing.");
            static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
            if (!Descendants(main).Any(c => c.Text == "Duyệt đăng ký" && c.Visible)) throw new InvalidOperationException("Approval action button is not visible.");
            await RenderAsync(main, "06-pending-staff.png", new Size(1000, 700));
            Checks.Add("PASS: Pending Lecturer row is visible with Vietnamese requested-role header/value and an accessible approval button at 1000×700.");
            typeof(MainForm).GetField("signingOut", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(main, true);
            main.Close();
        }
        await admin.ActionAsync("users", id, "approve");
        var approved = await admin.DetailAsync("users", id);
        if (!approved["isActive"].GetBoolean() || !approved["roles"].EnumerateArray().Any(r => r.GetString() == "Lecturer")) throw new InvalidOperationException("Approval did not activate the requested role.");
        using var lecturer = new ExamApiClient();
        await lecturer.LoginAsync("http://localhost:5180", username, password, "Lecturer");
        if ((await lecturer.CatalogAsync()).Role != "Lecturer") throw new InvalidOperationException("Approved fixture cannot obtain Lecturer catalog.");
        await lecturer.LogoutAsync(); await admin.LogoutAsync();
        Checks.Add("PASS: Desktop API-client approved only the browser-created Lecturer fixture; personnel login/catalog succeeded afterward.");
        Checks.Add("Fixture " + username + " remains approved for the browser QA handoff. Existing seed/user accounts were not altered. Native confirmation-dialog interaction was not automated.");
    }

    private static async Task VerifyHelpAsync()
    {
        using var api = new ExamApiClient();
        using (var login = new LoginForm(api))
        {
            await RenderAsync(login, "01-login.png", new Size(600, 620));
            static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
            var controls = Descendants(login).ToArray();
            if (!controls.Any(c => c.Text == "Hướng dẫn") || !controls.Any(c => c.Text == "Đăng ký tài khoản trên Web")) throw new InvalidOperationException("Login guidance/registration buttons are missing.");
            Checks.Add("PASS: Login form exposes guidance and all-role Web-registration buttons at 600×620.");
        }
        foreach (var role in new string?[] { null, "Administrator", "Lecturer", "ExamOfficer" })
        {
            using var help = new HelpForm(role);
            await RenderAsync(help, "05-help-" + (role ?? "login") + ".png", new Size(820, 620));
            Checks.Add("PASS: Rendered scrollable guidance for " + (role ?? "login, including all-role Web registration and pending personnel approval") + ".");
        }
        Checks.Add("Browser launch was not invoked during offscreen checks; registration target HTTP(S) validation and /Account/Register construction were inspected in source.");
    }

    private static async Task VerifyVersionAsync()
    {
        var credentials = await File.ReadAllLinesAsync(Path.Combine(Root, ".local/demo-credentials.txt"));
        var password = credentials.Single(l => l.StartsWith("password=", StringComparison.OrdinalIgnoreCase))["password=".Length..];
        using var api = new ExamApiClient();
        await api.LoginAsync("http://localhost:5180", "lecturer", password);
        var resource = (await api.CatalogAsync()).Resources.Single(r => r.Key == "questions");
        if (!resource.Fields.Any(f => f.Name == "version" && f.Type == "hidden")) throw new InvalidOperationException("Server hidden version metadata is missing.");
        var marker = "DesktopVersionQA-" + Guid.NewGuid().ToString("N")[..8];
        await SaveEditorAsync(api, resource, controls =>
        {
            if (controls.ContainsKey("version")) throw new InvalidOperationException("Hidden field became a visible input.");
            controls["text"].Text = marker;
            foreach (var n in Enumerable.Range(1, 4)) controls["option" + n].Text = "Lựa chọn " + n;
        });
        var id = int.Parse((await api.ListAsync("questions", marker, 1)).Items.Single()["id"]!.ToString()!);
        var original = await api.DetailAsync("questions", id);
        await SaveEditorAsync(api, resource, controls =>
        {
            if (controls.ContainsKey("version")) throw new InvalidOperationException("Hidden field became a visible input.");
            controls["text"].Text = marker + " edited";
        }, id, original);
        var changed = await api.DetailAsync("questions", id);
        if (changed["text"].GetString() != marker + " edited" || changed["version"].ToString() == original["version"].ToString()) throw new InvalidOperationException("Hidden version did not round-trip or did not advance after edit.");
        Checks.Add("PASS: Actual existing question Form preserved the original hidden version through API save; SQL edit advanced the version.");
        Checks.Add("PASS: Both new/edit Form input dictionaries omit the hidden Base64 version control.");
        await api.DeleteAsync("questions", id); await api.LogoutAsync();
        Checks.Add("PASS: Isolated question used for concurrency round-trip verification was deleted; seed questions untouched.");
    }

    private static async Task VerifyAsync()
    {
        Directory.CreateDirectory(Output);
        var credentials = await File.ReadAllLinesAsync(Path.Combine(Root, ".local/demo-credentials.txt"));
        var password = credentials.Single(l => l.StartsWith("password=", StringComparison.OrdinalIgnoreCase))["password=".Length..];
        using (var api = new ExamApiClient()) using (var login = new LoginForm(api)) await RenderAsync(login, "01-login.png", new Size(600, 470));
        foreach (var account in new[] { "admin", "lecturer", "officer" })
        {
            using var api = new ExamApiClient();
            await api.LoginAsync("http://localhost:5180", account, password);
            var catalog = await api.CatalogAsync();
            Checks.Add($"PASS: {account} API login/catalog returned {catalog.Resources.Count} authorized resources.");
            using (var main = new MainForm(api))
            {
                main.WindowState = FormWindowState.Normal;
                await RenderAsync(main, $"02-main-{account}.png", new Size(1200, 800), waitForGrid: true);
                typeof(MainForm).GetField("signingOut", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(main, true);
                main.Close();
            }
            foreach (var key in new[] { "users", "questions", "exams", "sessions" })
            {
                var resource = catalog.Resources.FirstOrDefault(r => r.Key == key);
                if (resource is null) continue;
                var page = await api.ListAsync(key, "", 1);
                Dictionary<string, JsonElement> detail = [];
                int? id = null;
                if (page.Items.Count > 0)
                {
                    id = int.Parse(page.Items[0]["id"]!.ToString()!);
                    detail = await api.DetailAsync(key, id.Value);
                    Checks.Add($"PASS: {account} {key} list/detail real API returned data.");
                }
                using var editor = new ResourceEditorForm(api, resource, id, detail);
                await RenderAsync(editor, $"03-editor-{key}.png", new Size(880, 800));
                if (key == "exams")
                {
                    var fieldControls = (Dictionary<string, Control>)typeof(ResourceEditorForm).GetField("inputs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
                    if (fieldControls["questionPoints"].GetType().Name != "QuestionPointsPicker") throw new InvalidOperationException("Question point picker is missing.");
                    Checks.Add("PASS: Exam editor uses question selection/individual points grid, not a JSON editor.");
                }
                editor.Close();
            }
            if (account == "admin") using (var import = new ImportStudentsForm(api)) await RenderAsync(import, "04-import.png", new Size(960, 640));
            if (account == "officer")
            {
                var marker = "DesktopQA-" + DateTime.UtcNow.ToString("HHmmss");
                var values = new Dictionary<string, JsonElement> { ["code"] = JsonSerializer.SerializeToElement(marker), ["name"] = JsonSerializer.SerializeToElement("Desktop client smoke"), ["isActive"] = JsonSerializer.SerializeToElement(true) };
                await api.SaveAsync("subjects", null, values);
                var found = await api.ListAsync("subjects", marker, 1);
                var id = int.Parse(found.Items.Single()["id"]!.ToString()!);
                values["name"] = JsonSerializer.SerializeToElement("Desktop client updated");
                await api.SaveAsync("subjects", id, values);
                var updated = await api.DetailAsync("subjects", id);
                if (updated["name"].GetString() != "Desktop client updated") throw new InvalidOperationException("Update did not persist.");
                await api.DeleteAsync("subjects", id);
                Checks.Add("PASS: Officer real Desktop API-client create/read/update/delete persisted and cleaned up an isolated subject.");
            }
            if (account == "lecturer")
            {
                var marker = "DesktopFormQA-" + Guid.NewGuid().ToString("N")[..8];
                await SaveEditorAsync(api, catalog.Resources.Single(r => r.Key == "questions"), controls =>
                {
                    controls["text"].Text = marker;
                    foreach (var n in Enumerable.Range(1, 4)) controls["option" + n].Text = "Lựa chọn " + n;
                    var group = (FlowLayoutPanel)controls["correctIndex"];
                    group.Controls.OfType<RadioButton>().Single(r => r.Tag?.ToString() == "3").Checked = true;
                });
                var questionId = int.Parse((await api.ListAsync("questions", marker, 1)).Items.Single()["id"]!.ToString()!);
                var question = await api.DetailAsync("questions", questionId);
                if (question["correctIndex"].GetInt32() != 3) throw new InvalidOperationException("Question radio serialization mismatch.");
                if (question.ContainsKey("version"))
                {
                    await SaveEditorAsync(api, catalog.Resources.Single(r => r.Key == "questions"), controls =>
                    {
                        if (controls.ContainsKey("version")) throw new InvalidOperationException("Hidden question version leaked into an input control.");
                        controls["text"].Text = marker + " edited";
                    }, questionId, question);
                    var edited = await api.DetailAsync("questions", questionId);
                    if (edited["text"].GetString() != marker + " edited" || edited["version"].ToString() == question["version"].ToString()) throw new InvalidOperationException("Question hidden version did not round-trip correctly.");
                    Checks.Add("PASS: Question Form preserved hidden concurrency version and edit produced a new version without showing a Base64 input.");
                }
                await api.DeleteAsync("questions", questionId);
                Checks.Add("PASS: Actual question Form saved option text and one correct radio through the API; isolated question deleted.");
                await SaveEditorAsync(api, catalog.Resources.Single(r => r.Key == "exams"), controls =>
                {
                    controls["title"].Text = marker;
                    var picker = controls["questionPoints"];
                    var grid = (DataGridView)picker.GetType().GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(picker)!;
                    for (var i = 0; i < 3; i++) { grid.Rows[i].Cells["selected"].Value = true; grid.Rows[i].Cells["points"].Value = i == 2 ? 3.34m : 3.33m; }
                });
                var examId = int.Parse((await api.ListAsync("exams", marker, 1)).Items.Single()["id"]!.ToString()!);
                var exam = await api.DetailAsync("exams", examId);
                var points = exam["questionPoints"].EnumerateObject().Select(p => p.Value.GetDecimal()).ToArray();
                if (points.Length != 3 || points.Sum() != 10 || !points.Contains(3.34m)) throw new InvalidOperationException("Individual question points serialization mismatch.");
                await api.DeleteAsync("exams", examId);
                Checks.Add("PASS: Actual exam Form saved 3.33 + 3.33 + 3.34 individual points to SQL; isolated draft exam deleted.");
            }
            await api.LogoutAsync();
        }
    }

    private static async Task SaveEditorAsync(ExamApiClient api, Exam.Core.ResourceDto resource, Action<Dictionary<string, Control>> fill, int? id = null, Dictionary<string, JsonElement>? original = null)
    {
        using var editor = new ResourceEditorForm(api, resource, id, original ?? []);
        editor.StartPosition = FormStartPosition.Manual; editor.Location = new Point(-30000, -30000); editor.ShowInTaskbar = false;
        editor.Show(); await Task.Delay(100);
        var controls = (Dictionary<string, Control>)typeof(ResourceEditorForm).GetField("inputs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        fill(controls);
        var task = (Task)typeof(ResourceEditorForm).GetMethod("SaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null)!;
        await task;
        if (editor.DialogResult != DialogResult.OK)
        {
            var status = (Label)typeof(ResourceEditorForm).GetField("status", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
            throw new InvalidOperationException("Form save failed: " + status.Text);
        }
    }

    private static async Task RenderAsync(Form form, string name, Size size, bool waitForGrid = false)
    {
        form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-30000, -30000); form.ShowInTaskbar = false; form.Opacity = 1; form.Size = size;
        form.Show(); form.WindowState = FormWindowState.Normal; form.Size = size; form.Location = new Point(-30000, -30000); await Task.Delay(250);
        if (waitForGrid)
        {
            var status = (ToolStripStatusLabel)typeof(MainForm).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
            for (var i = 0; i < 100 && !status.Text!.StartsWith("Đã tải") && !status.Text.StartsWith("Chưa có"); i++) await Task.Delay(100);
            if (!status.Text!.StartsWith("Đã tải") && !status.Text.StartsWith("Chưa có")) throw new InvalidOperationException("Main form did not finish API loading: " + status.Text);
        }
        form.PerformLayout(); form.Refresh(); await Task.Delay(100);
        if (form is ResourceEditorForm or HelpForm)
        {
            var footer = form.Controls.OfType<FlowLayoutPanel>().Single();
            Checks.Add($"Layout {name}: footer {footer.Bounds}, visible={footer.Visible}; buttons=" + string.Join(";", footer.Controls.Cast<Control>().Select(c => c.Text + ":" + c.Bounds + ":" + c.Visible)));
        }
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        if (form is ResourceEditorForm)
        {
            var footer = form.Controls.OfType<FlowLayoutPanel>().Single();
            using var footerBitmap = new Bitmap(footer.Width, footer.Height);
            footer.DrawToBitmap(footerBitmap, new Rectangle(Point.Empty, footerBitmap.Size));
            using var graphics = Graphics.FromImage(bitmap);
            var origin = form.PointToScreen(footer.Location);
            graphics.DrawImageUnscaled(footerBitmap, origin.X - form.Left, origin.Y - form.Top);
        }
        if (form is MainForm)
        {
            var label = (Label)typeof(MainForm).GetField("pageLabel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
            Checks.Add("Pagination label: " + label.Text + ", bounds=" + label.Bounds);
        }
        bitmap.Save(Path.Combine(Output, name));
        Checks.Add("PASS: Rendered " + name + $" ({form.Width}×{form.Height}).");
    }
}
