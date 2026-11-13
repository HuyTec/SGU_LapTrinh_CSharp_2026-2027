using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Exam.Core;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Lecturer")]
public sealed class LecturerSessionsController(LecturerSessionService sessions) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public sealed class Editor
    {
        public int? Id { get; set; }
        [Required, StringLength(200)]
        public string Title { get; set; } = "";
        [Range(1, int.MaxValue)]
        public int ExamId { get; set; }
        [Required]
        public DateTime? OpensAt { get; set; }
        [Required]
        public DateTime? ClosesAt { get; set; }
        [Range(1, int.MaxValue)]
        public int DurationMinutes { get; set; } = 30;
        public bool ShuffleQuestions { get; set; }
        public bool ShuffleOptions { get; set; }
        public string ReviewPolicy { get; set; } = "ScoreOnly";
        public int[] ClassIds { get; set; } = [];
        public IReadOnlyList<SessionPaperChoice> Papers { get; set; } = [];
        public IReadOnlyList<SessionClassChoice> Classes { get; set; } = [];
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await sessions.ListAsync(UserId));

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        var data = await sessions.EditorAsync(UserId, id);
        var model = data.Draft is { } d
            ? new Editor { Title = d.Title, ExamId = d.ExamId, OpensAt = d.OpensAt + VietnamOffset,
                ClosesAt = d.ClosesAt + VietnamOffset, DurationMinutes = d.DurationMinutes,
                ShuffleQuestions = d.ShuffleQuestions, ShuffleOptions = d.ShuffleOptions,
                ReviewPolicy = d.ReviewPolicy, ClassIds = d.ClassIds }
            : new Editor();
        model.Id = id;
        Fill(model, data);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, Editor model)
    {
        if (ModelState.IsValid && model.OpensAt is DateTime opens && model.ClosesAt is DateTime closes)
        {
            try
            {
                await sessions.SaveAsync(UserId, id, new SessionDraft(model.Title, model.ExamId,
                    new DateTimeOffset(DateTime.SpecifyKind(opens, DateTimeKind.Unspecified), VietnamOffset).UtcDateTime,
                    new DateTimeOffset(DateTime.SpecifyKind(closes, DateTimeKind.Unspecified), VietnamOffset).UtcDateTime,
                    model.DurationMinutes, model.ShuffleQuestions, model.ShuffleOptions, model.ReviewPolicy, model.ClassIds));
                TempData["Success"] = "Đã lưu đợt thi nháp.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex) when (ex.Status != 404)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }
        model.Id = id;
        Fill(model, await sessions.EditorAsync(UserId, id));
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeState(int id, string command)
    {
        try
        {
            await sessions.ChangeStateAsync(UserId, id, command);
            TempData["Success"] = command switch
            {
                "activate" => "Đã kích hoạt đợt thi. Mã và link đã sẵn sàng.",
                "postpone" => "Đã hoãn và chuyển đợt về nháp.",
                _ => "Đã hủy đợt thi."
            };
        }
        catch (BusinessException ex) when (ex.Status != 404)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private static void Fill(Editor model, SessionEditorData data)
    {
        model.Papers = data.Papers;
        model.Classes = data.Classes;
    }
}
