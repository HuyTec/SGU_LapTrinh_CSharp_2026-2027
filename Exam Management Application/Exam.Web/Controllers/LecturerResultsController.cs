using System.Security.Claims;
using Exam.Core;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Lecturer")]
public sealed class LecturerResultsController(AdminService admin, ExportService export) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private const string Role = "Lecturer";

    [HttpGet]
    public async Task<IActionResult> Index(int? id, string? search = null, string? status = null)
    {
        var rows = id is int sessionId ? await admin.SessionMonitorAsync(UserId, Role, sessionId) : [];
        var sessions = await ReadAllAsync("sessions");
        var filtered = ResultStatistics.Filter(rows, search, status);
        return View(new ResultsPage(id, sessions, filtered, ResultStatistics.Summarize(rows), search?.Trim(), status?.Trim()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, string reviewPolicy = "Answers", string? search = null, string? status = null)
    {
        try
        {
            await admin.ActionAsync(UserId, Role, "sessions", id, "publish", reviewPolicy);
            TempData["Success"] = "Đã bật công bố điểm cho các bài đã chấm. Phần xem lại bài chỉ mở sau khi đóng nhận thí sinh và không còn lượt đang làm trong thời hạn.";
        }
        catch (BusinessException ex) when (ex.Status == 400 || ex.Status == 409)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { id, search, status });
    }

    [HttpGet]
    public async Task<IActionResult> Export(int id, bool xlsx = false, string? search = null, string? status = null)
    {
        var bytes = await export.ExportAsync(UserId, Role, id, xlsx, search, status);
        return File(bytes, xlsx ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv; charset=utf-8",
            $"ket-qua-{id}." + (xlsx ? "xlsx" : "csv"));
    }

    private async Task<List<Dictionary<string, object?>>> ReadAllAsync(string resource)
    {
        var rows = new List<Dictionary<string, object?>>();
        for (var page = 1; ; page++)
        {
            var result = await admin.ListAsync(UserId, Role, resource, null, page, 200);
            rows.AddRange(result.Items);
            if (rows.Count >= result.Total || result.Items.Count == 0)
                return rows;
        }
    }

    public sealed record ResultsPage(int? SessionId, IReadOnlyList<Dictionary<string, object?>> Sessions,
        IReadOnlyList<Dictionary<string, object?>> Rows, ResultStatistics Statistics, string? Search, string? Status)
    {
        public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Status);
    }
}
