using System.Security.Claims;
using Exam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(Roles = "Student")]
public sealed class StudentClassesController(ExamService exams, StudentClassService classes) : Controller
{
    private int StudentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Details(int id) => View(await classes.DetailAsync(StudentId, id));

    [HttpGet]
    public async Task<IActionResult> Index() => View(await exams.StudentClassesAsync(StudentId));
}
