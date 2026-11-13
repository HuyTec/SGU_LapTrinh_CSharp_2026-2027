using System.Security.Claims;
using Exam.Core;
using Exam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Lecturer")]
public sealed class LecturerClassesController(LecturerClassService classes) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(int? id) => View(await classes.ListAsync(UserId, id));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Add(int id, string? code) =>
        ChangeAsync(id, () => classes.AddAsync(UserId, id, code), "Đã thêm thí sinh vào lớp.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Remove(int id, int studentId) =>
        ChangeAsync(id, () => classes.RemoveAsync(UserId, id, studentId), "Đã cập nhật danh sách lớp.");

    private async Task<IActionResult> ChangeAsync(int id, Func<Task> change, string message)
    {
        try
        {
            await change();
            TempData["Success"] = message;
        }
        catch (BusinessException ex) when (ex.Status is 400 or 409)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { id });
    }
}
