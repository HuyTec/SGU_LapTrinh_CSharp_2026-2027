using System.Security.Claims;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Lecturer")]
public class LecturerController(AdminService admin) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var catalog = await admin.CatalogAsync(userId, "Lecturer");
        var subjects = catalog.Resources.Single(resource => resource.Key == "topics")
            .Fields.Single(field => field.Name == "subjectId").Choices ?? [];
        return View(subjects);
    }
}
