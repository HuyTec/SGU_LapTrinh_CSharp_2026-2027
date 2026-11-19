using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Exam.Core;
using Exam.Core.Contracts.Classes;

namespace Exam.Desktop;

/// <summary>Desktop transport only: all authorization, validation and business rules stay on the server.</summary>
public sealed class ExamApiClient : IDisposable
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private Uri? server;
    public string? Role { get; private set; }
    public string? DisplayName { get; private set; }

    public async Task LoginAsync(string address, string userName, string password, string? role = null)
    {
        if (!Uri.TryCreate(address.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
            throw new InvalidOperationException("Máy chủ phải dùng HTTPS; HTTP chỉ được dùng trên localhost khi phát triển.");
        server = uri;
        http.DefaultRequestHeaders.Authorization = null;
        var response = await SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", new LoginRequest(userName, password, role));
        if (response.Role == "Student") throw new InvalidOperationException("Tài khoản thí sinh vui lòng đăng nhập trên Web.");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Token);
        Role = response.Role;
        DisplayName = response.DisplayName;
    }

    public async Task LogoutAsync()
    {
        try { await SendAsync<JsonElement>(HttpMethod.Post, "api/auth/logout"); }
        finally { http.DefaultRequestHeaders.Authorization = null; Role = DisplayName = null; }
    }

    public Task<AdminCatalogDto> CatalogAsync() => SendAsync<AdminCatalogDto>(HttpMethod.Get, "api/admin/catalog");
    public Task<ProfileDto> ProfileAsync() => SendAsync<ProfileDto>(HttpMethod.Get, "api/profile");
    public async Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var profile = await SendAsync<ProfileDto>(HttpMethod.Put, "api/profile", request);
        DisplayName = profile.FullName;
        return profile;
    }
    public Task<AdminPageDto> ListAsync(string key, string search, int page, string? subjectId = null, string? topicId = null, string? difficulty = null, bool? isActive = null, string? accountRole = null) => SendAsync<AdminPageDto>(HttpMethod.Get,
        $"api/admin/{key}?search={Uri.EscapeDataString(search)}&page={page}&pageSize=25&subjectId={Uri.EscapeDataString(subjectId ?? "")}&topicId={Uri.EscapeDataString(topicId ?? "")}&difficulty={Uri.EscapeDataString(difficulty ?? "")}&isActive={isActive}&accountRole={Uri.EscapeDataString(accountRole ?? "")}");
    /// <summary>Read all authorized pages for presentation summaries; errors never become synthetic zero totals.</summary>
    public async Task<IReadOnlyList<Dictionary<string, object?>>> ListAllAsync(string key)
    {
        var rows = new List<Dictionary<string, object?>>();
        var page = 1;
        AdminPageDto result;
        do
        {
            result = await SendAsync<AdminPageDto>(HttpMethod.Get, $"api/admin/{key}?page={page++}&pageSize=200");
            rows.AddRange(result.Items);
        } while (rows.Count < result.Total && result.Items.Count > 0);
        return rows;
    }
    public Task<Dictionary<string, JsonElement>> DetailAsync(string key, int id) => SendAsync<Dictionary<string, JsonElement>>(HttpMethod.Get, $"api/admin/{key}/{id}");
    public Task SaveAsync(string key, int? id, Dictionary<string, JsonElement> values) => SendAsync<JsonElement>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
        $"api/admin/{key}" + (id.HasValue ? $"/{id}" : ""), new AdminSaveRequest(values));
    public Task DeleteAsync(string key, int id) => SendAsync<JsonElement>(HttpMethod.Delete, $"api/admin/{key}/{id}");
    public Task<List<ClassMemberDto>> ClassMembersAsync(int classId) =>
        SendAsync<List<ClassMemberDto>>(HttpMethod.Get, $"api/admin/classes/{classId}/members");
    public Task<List<ClassMemberDto>> EligibleClassStudentsAsync(int classId) =>
        SendAsync<List<ClassMemberDto>>(HttpMethod.Get, $"api/admin/classes/{classId}/eligible-students");
    public Task AddClassMemberAsync(int classId, int studentId) =>
        SendAsync<JsonElement>(HttpMethod.Post, $"api/admin/classes/{classId}/members", new AddStudentToClassDto(studentId));
    public Task RemoveClassMemberAsync(int classId, int studentId) =>
        SendAsync<JsonElement>(HttpMethod.Delete, $"api/admin/classes/{classId}/members/{studentId}");
    public Task ActionAsync(string key, int id, string action) => SendAsync<JsonElement>(HttpMethod.Post, $"api/admin/{key}/{id}/action", new ActionRequest(action));
    public async Task<int> ImportStudentsAsync(string csv, bool preview = false)
    {
        var result = await SendAsync<JsonElement>(HttpMethod.Post, "api/admin/users/import", new { csv, preview });
        return result.GetProperty("imported").GetInt32();
    }

    public async Task ExportAsync(int sessionId, string format, string path)
    {
        using var response = await http.GetAsync(new Uri(server!, $"api/admin/sessions/{sessionId}/export?format={format}"));
        await EnsureSuccessAsync(response);
        await using var file = File.Create(path);
        await response.Content.CopyToAsync(file);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string route, object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(server!, route));
        if (body is not null) request.Content = JsonContent.Create(body, options: json);
        using var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var text = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(text) ? default! : JsonSerializer.Deserialize<T>(text, json)!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng xuất và đăng nhập lại.",
            HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
            _ => "Không thực hiện được yêu cầu. Vui lòng thử lại."
        };
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString())) message = detail.GetString()!;
            else if (doc.RootElement.TryGetProperty("title", out var title)) message = title.GetString() ?? message;
        }
        catch (JsonException) { }
        throw new InvalidOperationException(message);
    }

    public void Dispose() => http.Dispose();
}
