using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    /// <summary>Exercise real MVC routes, cookies and anti-forgery with an isolated SQL database.</summary>
    [SqlFact]
    public async Task WebLecturerWorkflowAndStudentClassNavigation()
    {
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var teacher = new AppUser { UserName = "http-teacher", FullName = "HTTP teacher", IsActive = true };
        var student = new AppUser { UserName = "http-student", FullName = "HTTP student", StudentCode = "HTTP001", IsActive = true };
        const string password = "Example1!";
        foreach (var (user, role) in new[] { (teacher, "Lecturer"), (student, "Student") })
        {
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        var roles = provider.GetRequiredService<RoleManager<AppRole>>();
        Assert.True((await roles.CreateAsync(new() { Name = "Administrator" })).Succeeded);
        var administrator = new AppUser { UserName = "http-admin", FullName = "HTTP admin" };
        Assert.True((await users.CreateAsync(administrator, password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(administrator, "Administrator")).Succeeded);
        var subject = new Subject { Code = "HTTP", Name = "HTTP Subject" };
        Db.Add(subject);
        await Db.SaveChangesAsync();
        Db.Add(new LecturerSubject { SubjectId = subject.Id, LecturerId = teacher.Id });
        await Db.SaveChangesAsync();

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Exam Management Application.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var webRoot = Path.Combine(root.FullName, "Exam.Web");
        var dll = typeof(global::Program).Assembly.Location;
        Assert.True(File.Exists(dll));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = webRoot, UseShellExecute = false, CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(dll);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        start.Environment["ConnectionStrings__ExamDb"] = Db.Database.GetConnectionString()!;
        start.Environment["Jwt__Key"] = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        using var process = Process.Start(start)!;
        var logs = new System.Collections.Concurrent.ConcurrentQueue<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) logs.Enqueue(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) logs.Enqueue(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var staff = Client(port);
        using var pupil = Client(port);
        try
        {
            var started = false;
            for (var attempt = 0; attempt < 60 && !process.HasExited; attempt++)
            {
                try
                {
                    if ((await staff.GetAsync("/Account/Login")).IsSuccessStatusCode) { started = true; break; }
                }
                catch (HttpRequestException) { }
                await Task.Delay(500);
            }
            Assert.True(started, "Isolated Web host did not start.");
            using var desktop = Client(port);
            using var loginResponse = await desktop.PostAsJsonAsync("/api/auth/login",
                new Exam.Core.LoginRequest(administrator.UserName!, password, "Administrator"));
            Assert.True(loginResponse.IsSuccessStatusCode);
            var login = await loginResponse.Content.ReadFromJsonAsync<Exam.Core.LoginResponse>();
            desktop.DefaultRequestHeaders.Authorization = new("Bearer", login!.Token);
            var classValues = new { values = new { code = "HTTP-CLASS", name = "HTTP class", subjectId = subject.Id, isActive = true } };
            using var createdClass = await desktop.PostAsJsonAsync("/api/admin/classes", classValues);
            Assert.True(createdClass.IsSuccessStatusCode, await createdClass.Content.ReadAsStringAsync());
            var examClass = await Db.Set<ExamClass>().AsNoTracking().SingleAsync();
            Assert.Equal("HTTP class", examClass.Name);
            Assert.Equal(HttpStatusCode.Conflict, (await desktop.PostAsJsonAsync("/api/admin/classes", classValues)).StatusCode);
            var memberRoute = $"/api/admin/classes/{examClass.Id}/members";
            Assert.Equal(HttpStatusCode.BadRequest, (await desktop.PostAsJsonAsync(memberRoute, new { studentId = teacher.Id })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await desktop.PostAsJsonAsync(memberRoute, new { studentId = student.Id })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await desktop.PostAsJsonAsync(memberRoute, new { studentId = student.Id })).StatusCode);
            Assert.Equal(student.Id, Assert.Single((await desktop.GetFromJsonAsync<Exam.Core.ClassMemberDto[]>(memberRoute))!).StudentId);
            var adminProfile = await desktop.GetFromJsonAsync<Exam.Core.ProfileDto>("/api/profile");
            Assert.Equal("http-admin", adminProfile!.UserName);
            using var savedAdmin = await desktop.PutAsJsonAsync("/api/profile",
                new Exam.Core.UpdateProfileRequest("HTTP admin updated", "admin@example.com", "0909876543", adminProfile.Version));
            Assert.True(savedAdmin.IsSuccessStatusCode, await savedAdmin.Content.ReadAsStringAsync());
            Assert.Equal("HTTP admin updated", (await savedAdmin.Content.ReadFromJsonAsync<Exam.Core.ProfileDto>())!.FullName);
            await Login(staff, teacher.UserName!, password);
            Assert.Equal(HttpStatusCode.Unauthorized, (await staff.PostAsJsonAsync("/api/admin/classes", classValues)).StatusCode);
            using var home = await staff.GetAsync("/");
            Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
            Assert.Equal("/Lecturer", home.Headers.Location?.ToString());
            var profileHtml = WebUtility.HtmlDecode(await staff.GetStringAsync("/Profile"));
            Assert.Contains("Hồ sơ giảng viên", profileHtml);
            var version = WebUtility.HtmlDecode(Regex.Match(profileHtml, "name=\"Version\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            await PostForm(staff, "/Profile", "/Profile", new()
            {
                ["FullName"] = "HTTP teacher updated", ["Email"] = "teacher@example.com",
                ["PhoneNumber"] = "0901234567", ["Version"] = version,
                ["UserId"] = student.Id.ToString(), ["StudentCode"] = "TAMPERED", ["Roles"] = "Administrator"
            });
            Db.ChangeTracker.Clear();
            Assert.Equal("HTTP teacher updated", (await Db.Users.SingleAsync(u => u.Id == teacher.Id)).FullName);
            Assert.Equal("HTTP001", (await Db.Users.SingleAsync(u => u.Id == student.Id)).StudentCode);
            Assert.False(await users.IsInRoleAsync((await users.FindByIdAsync(teacher.Id.ToString()))!, "Administrator"));
            foreach (var route in new[] { "/Lecturer",
                "/Lecturer/Materials?resource=topics", "/Lecturer/Materials?resource=questions",
                "/LecturerPapers", "/LecturerSessions", "/LecturerResults" })
            {
                using var response = await staff.GetAsync(route);
                Assert.True(response.StatusCode == HttpStatusCode.OK,
                    $"{route} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}\n{string.Join("\n", logs.TakeLast(55))}");
            }

            await PostForm(staff, "/Lecturer/SaveMaterial?resource=topics", "/Lecturer/EditMaterial?resource=topics",
                new() { ["subjectId"] = subject.Id.ToString(), ["name"] = "HTTP Topic", ["isActive"] = "true" });
            var topic = await Db.Topics.SingleAsync(t => t.SubjectId == subject.Id);
            await PostForm(staff, "/Lecturer/SaveMaterial?resource=questions", "/Lecturer/EditMaterial?resource=questions",
                new() { ["topicId"] = topic.Id.ToString(), ["text"] = @"**HTTP Question** $\frac{1}{2}$", ["difficulty"] = "Medium",
                    ["option1"] = "A", ["option2"] = "B", ["option3"] = "C", ["option4"] = "D", ["correctIndex"] = "1", ["isActive"] = "true" });
            var question = await Db.Questions.SingleAsync(q => q.TopicId == topic.Id);
            Assert.Equal(@"**HTTP Question** $\frac{1}{2}$", question.Text);
            var questionEditor = await staff.GetStringAsync($"/Lecturer/EditMaterial?resource=questions&id={question.Id}");
            Assert.Contains("data-preview-for=\"text\"", questionEditor);
            Assert.Contains("question-content.js", questionEditor);
            await PostForm(staff, "/LecturerPapers/Save", $"/LecturerPapers/Create?subjectId={subject.Id}",
                new() { ["Title"] = "HTTP Paper", ["SubjectId"] = subject.Id.ToString(),
                    ["Questions[0].Id"] = question.Id.ToString(), ["Questions[0].Selected"] = "true", ["Questions[0].Points"] = "2.5" });
            var paper = await Db.Exams.SingleAsync(e => e.SubjectId == subject.Id);
            await PostForm(staff, "/LecturerPapers/ChangeStatus", $"/LecturerPapers/Edit/{paper.Id}",
                new() { ["id"] = paper.Id.ToString(), ["ready"] = "true" });
            Db.ChangeTracker.Clear();
            Assert.Equal("Ready", (await Db.Exams.SingleAsync(e => e.Id == paper.Id)).Status);
            var now = DateTime.UtcNow;
            var sessionForm = await staff.GetStringAsync("/LecturerSessions/Edit");
            var sessionToken = Regex.Match(sessionForm, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(sessionToken.Success);
            using (var invalidSession = await staff.PostAsync("/LecturerSessions/Edit", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Title"] = "HTTP invalid schedule", ["ExamId"] = paper.Id.ToString(),
                ["OpensAt"] = now.AddHours(7).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                ["ClosesAt"] = now.AddHours(7).AddMinutes(-40).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                ["DurationMinutes"] = "20", ["ReviewPolicy"] = "ScoreOnly", ["ClassIds"] = examClass.Id.ToString(),
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(sessionToken.Groups[1].Value)
            })))
            {
                Assert.Equal(HttpStatusCode.OK, invalidSession.StatusCode);
                var html = WebUtility.HtmlDecode(await invalidSession.Content.ReadAsStringAsync());
                Assert.Matches("data-valmsg-for=\"ClosesAt\"[^>]*>Giờ đóng phải sau giờ mở", html);
                Assert.Contains("value=\"HTTP invalid schedule\"", html);
                Assert.DoesNotContain("Nhập tên đợt tối đa 200 ký tự, giờ mở trước giờ đóng", html);
                Assert.Empty(await Db.ExamSessions.ToListAsync());
            }
            await PostForm(staff, "/LecturerSessions/Edit", "/LecturerSessions/Edit",
                new() { ["Title"] = "HTTP Session", ["ExamId"] = paper.Id.ToString(),
                    ["OpensAt"] = now.AddHours(7).AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                    ["ClosesAt"] = now.AddHours(7).AddMinutes(4).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                    ["DurationMinutes"] = "20", ["ReviewPolicy"] = "ScoreOnly", ["ClassIds"] = examClass.Id.ToString() });
            var session = await Db.ExamSessions.SingleAsync(e => e.ExamId == paper.Id);
            await PostForm(staff, $"/LecturerSessions/ChangeState/{session.Id}", "/LecturerSessions",
                new() { ["command"] = "activate" });
            Db.ChangeTracker.Clear();
            Assert.Equal("Active", (await Db.ExamSessions.SingleAsync(e => e.Id == session.Id)).Status);
            var publicationPage = WebUtility.HtmlDecode(await staff.GetStringAsync($"/LecturerResults?id={session.Id}"));
            Assert.Contains("Công bố ngay", publicationPage);
            await PostForm(staff, $"/LecturerResults/Publish/{session.Id}", $"/LecturerResults?id={session.Id}",
                new() { ["reviewPolicy"] = "Answers" });
            Db.ChangeTracker.Clear();
            var publishedSession = await Db.ExamSessions.SingleAsync(e => e.Id == session.Id);
            Assert.True(publishedSession.Published);
            Assert.Equal("Answers", publishedSession.ReviewPolicy);

            await Login(pupil, student.UserName!, password);
            var studentProfile = WebUtility.HtmlDecode(await pupil.GetStringAsync("/Profile"));
            Assert.Contains("Hồ sơ thí sinh", studentProfile);
            Assert.Contains("HTTP001", studentProfile);
            var classes = await pupil.GetStringAsync("/StudentClasses");
            Assert.Contains($"/StudentClasses/Details/{examClass.Id}", classes);
            var detail = await pupil.GetStringAsync($"/StudentClasses/Details/{examClass.Id}");
            Assert.Contains("HTTP-CLASS", detail);
            Assert.Contains("HTTP teacher", detail);
            Assert.Contains("HTTP Session", detail);
            await PostForm(pupil, $"/Sessions/Start/{session.Id}", $"/Sessions/Details/{session.Id}", new());
            var ownAttempt = await Db.Attempts.SingleAsync(a => a.SessionId == session.Id && a.StudentId == student.Id);
            var takePage = await pupil.GetStringAsync($"/Attempts/Take/{ownAttempt.Id}");
            var answerToken = Regex.Match(takePage, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(answerToken.Success);
            var deliveredQuestion = await Db.AttemptQuestions.Include(q => q.Options).SingleAsync(q => q.AttemptId == ownAttempt.Id);
            using (var answerRequest = new HttpRequestMessage(HttpMethod.Post, $"/Attempts/Save/{ownAttempt.Id}"))
            {
                answerRequest.Headers.Add("RequestVerificationToken", WebUtility.HtmlDecode(answerToken.Groups[1].Value));
                answerRequest.Content = JsonContent.Create(new Exam.Core.SaveAnswerRequest(deliveredQuestion.Id,
                    deliveredQuestion.Options.Single(o => o.IsCorrect).Id, false));
                using var answerResponse = await pupil.SendAsync(answerRequest);
                Assert.True(answerResponse.IsSuccessStatusCode);
            }
            using (var submitRequest = new HttpRequestMessage(HttpMethod.Post, $"/Attempts/Submit/{ownAttempt.Id}"))
            {
                submitRequest.Headers.Add("RequestVerificationToken", WebUtility.HtmlDecode(answerToken.Groups[1].Value));
                using var submitted = await pupil.SendAsync(submitRequest);
                Assert.True(submitted.IsSuccessStatusCode);
            }
            var resultPage = WebUtility.HtmlDecode(await pupil.GetStringAsync($"/Attempts/Result/{ownAttempt.Id}"));
            Assert.Contains("Điểm bài thi", resultPage);
            Assert.DoesNotContain("Đáp án đã chọn:", resultPage);
            Assert.DoesNotContain("review-correct", resultPage);
            Assert.DoesNotContain("Đáp án đúng:", resultPage);
            Assert.Contains("Phần xem lại bài sẽ mở", resultPage);
            await PostForm(staff, $"/LecturerSessions/ChangeState/{session.Id}", "/LecturerSessions",
                new() { ["command"] = "closeAdmissions" });
            resultPage = WebUtility.HtmlDecode(await pupil.GetStringAsync($"/Attempts/Result/{ownAttempt.Id}"));
            Assert.Contains("Đáp án đã chọn:", resultPage);
            Assert.Contains("review-correct", resultPage);
            Assert.DoesNotContain("Đáp án đúng:", resultPage);
            Assert.Equal(HttpStatusCode.NotFound, (await pupil.GetAsync("/StudentClasses/Details/2147483647")).StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, (await pupil.GetAsync("/LecturerClasses")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/LecturerResults?id={session.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await desktop.DeleteAsync($"/api/admin/classes/{examClass.Id}")).StatusCode);
            using var stoppedClass = await desktop.PutAsJsonAsync($"/api/admin/classes/{examClass.Id}",
                new { values = new { code = examClass.Code, name = "Updated class", subjectId = subject.Id, isActive = false } });
            Assert.Equal(HttpStatusCode.OK, stoppedClass.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await desktop.DeleteAsync($"{memberRoute}/{student.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await desktop.PostAsJsonAsync(memberRoute, new { studentId = student.Id })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await pupil.GetAsync($"/Attempts/Result/{ownAttempt.Id}")).StatusCode);
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
        }
    }

    private static HttpClient Client(int port) => new(new HttpClientHandler
    {
        AllowAutoRedirect = false, CookieContainer = new CookieContainer()
    }) { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(15) };

    private static async Task Login(HttpClient client, string name, string password) =>
        await PostForm(client, "/Account/Login", "/Account/Login", new() { ["UserName"] = name, ["Password"] = password });

    private static async Task PostForm(HttpClient client, string route, string formRoute, Dictionary<string, string> fields)
    {
        var page = await client.GetStringAsync(formRoute);
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, $"Missing anti-forgery field on {formRoute}");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        using var response = await client.PostAsync(route, new FormUrlEncodedContent(fields));
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"{route} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
}
