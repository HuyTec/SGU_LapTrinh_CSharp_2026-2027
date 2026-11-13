using System.Security.Claims;
using System.Text.Json;
using Exam.Core;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Lecturer")]
public class LecturerController(AdminService admin) : Controller
{
    private const string Role = "Lecturer";
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var topics = await GetResourceAsync("topics");
        return View(topics.Fields.Single(field => field.Name == "subjectId").Choices ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Materials(string resource = "topics", string? search = null, int page = 1)
    {
        if (!IsMaterial(resource))
            return NotFound();

        var definition = await GetResourceAsync(resource);
        var results = await admin.ListAsync(UserId, Role, resource, search, page, 25);
        return View(new MaterialsPage(definition, results, search));
    }

    [HttpGet]
    public async Task<IActionResult> EditMaterial(string resource, int? id)
    {
        if (!IsMaterial(resource))
            return NotFound();

        var definition = await GetResourceAsync(resource);
        Dictionary<string, string> values = [];
        if (id.HasValue)
        {
            var row = (await admin.ListAsync(UserId, Role, resource, null, 1, 1, exactId: id)).Items.SingleOrDefault();
            if (row is null)
                return NotFound();

            values = row.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString() ?? "");
        }
        else
        {
            values["isActive"] = "true";
            values["difficulty"] = "Medium";
        }
        return View(new MaterialEditor(definition, id, values));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMaterial(string resource, int? id)
    {
        if (!IsMaterial(resource))
            return NotFound();

        var definition = await GetResourceAsync(resource);
        var fields = await Request.ReadFormAsync();
        var values = definition.Fields.ToDictionary(
            field => field.Name,
            field => field.Type == "checkbox" && !fields.ContainsKey(field.Name) ? "false" : fields[field.Name].ToString());
        var payload = values.ToDictionary(pair => pair.Key, pair => JsonSerializer.SerializeToElement(pair.Value));
        try
        {
            await admin.SaveAsync(UserId, Role, resource, id, new AdminSaveRequest(payload));
            TempData["Success"] = "Đã lưu thay đổi.";
            return RedirectToAction(nameof(Materials), new { resource });
        }
        catch (BusinessException ex) when (ex.Status != 403 && ex.Status != 404)
        {
            Response.StatusCode = ex.Status;
            ModelState.AddModelError("", ex.Message);
            return View(nameof(EditMaterial), new MaterialEditor(definition, id, values));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMaterial(string resource, int id)
    {
        if (!IsMaterial(resource))
            return NotFound();
        try
        {
            await admin.DeleteAsync(UserId, Role, resource, id);
            TempData["Success"] = "Đã xóa dữ liệu.";
        }
        catch (BusinessException ex) when (ex.Status != 403 && ex.Status != 404)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Materials), new { resource });
    }

    private static bool IsMaterial(string? resource) => resource is "topics" or "questions";

    private async Task<ResourceDto> GetResourceAsync(string key) =>
        (await admin.CatalogAsync(UserId, Role)).Resources.Single(resource => resource.Key == key);

    public sealed record MaterialsPage(ResourceDto Definition, AdminPageDto Page, string? Search);
    public sealed record MaterialEditor(ResourceDto Definition, int? Id, IReadOnlyDictionary<string, string> Values);
}
