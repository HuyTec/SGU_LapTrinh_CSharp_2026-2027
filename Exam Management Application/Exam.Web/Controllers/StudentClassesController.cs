using System.Security.Claims;
using Exam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(Roles = "Student")]
public sealed class StudentClassesController(ExamService exams) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id) => View(await classes.DetailAsync(StudentId, id));

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(await exams.StudentClassesAsync(studentId));
    }
}
