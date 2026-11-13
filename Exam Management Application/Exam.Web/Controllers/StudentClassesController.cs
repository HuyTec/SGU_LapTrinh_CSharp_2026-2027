using System.Security.Claims;
using Exam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(Roles = "Student")]
public sealed class StudentClassesController(StudentClassService classes, IWebHostEnvironment environment) : Controller
{
    private int StudentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["DemoEnrollment"] = environment.IsDevelopment();
        return View(await classes.ListAsync(StudentId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinDemo(string? code)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        if (!string.Equals(code?.Trim(), StudentClassService.DemoCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Mã lớp thử không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (await classes.JoinDemoAsync(StudentId))
            TempData["Success"] = "Bạn đã tham gia lớp thử DEMO-NV10.";
        else
            TempData["Error"] = "Lớp thử chưa được tạo hoặc đã ngừng hoạt động.";
        return RedirectToAction(nameof(Index));
    }
}
