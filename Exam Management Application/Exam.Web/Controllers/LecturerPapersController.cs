using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Exam.Core;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Lecturer")]
[AutoValidateAntiforgeryToken]
public sealed class LecturerPapersController(AdminService admin) : Controller
{
    private const string Resource = "exams";
    private const string Role = "Lecturer";
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1) =>
        View(await admin.ListAsync(UserId, Role, Resource, null, page, 25));

    [HttpGet]
    public async Task<IActionResult> Create(int? subjectId)
    {
        var model = new PaperEditor { SubjectId = subjectId ?? 0 };
        await PopulateAsync(model);
        if (subjectId.HasValue && !IsAssigned(model))
            return NotFound();
        return View("Edit", model);
    }

    [HttpGet]
    public async Task<IActionResult> Matrix(int? subjectId)
    {
        var model = new MatrixEditor { SubjectId = subjectId ?? 0 };
        await PopulateMatrixAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromMatrix(MatrixEditor model)
    {
        var postedTopicIds = model.Quotas?.Select(x => x.TopicId).ToArray() ?? [];
        var hasDuplicateTopics = postedTopicIds.Distinct().Count() != postedTopicIds.Length;
        await PopulateMatrixAsync(model);
        if (hasDuplicateTopics)
            ModelState.AddModelError("", "Danh sách chủ đề bị trùng. Hãy tải lại trang và thử lại.");
        if (postedTopicIds.Any(id => !model.Topics.Any(topic => topic.Id == id)))
            ModelState.AddModelError("", "Ma trận chứa chủ đề không thuộc môn được chọn. Hãy tải lại trang.");
        if (string.IsNullOrWhiteSpace(model.Title) || model.Title.Trim().Length > 2000)
            ModelState.AddModelError(nameof(model.Title), "Tên đề không được để trống và tối đa 2000 ký tự.");
        var quotas = model.Quotas ?? [];
        if (quotas.Count == 0 || quotas.Select(x => x.TopicId).Distinct().Count() != quotas.Count)
            ModelState.AddModelError("", "Danh sách chủ đề không hợp lệ. Hãy tải lại trang.");
        if (quotas.Any(x => x.EasyCount is < 0 or > 100 || x.MediumCount is < 0 or > 100 || x.HardCount is < 0 or > 100))
            ModelState.AddModelError("", "Mỗi mức độ chỉ nhận số lượng từ 0 đến 100.");
        var total = quotas.Sum(x => (long)x.EasyCount + x.MediumCount + x.HardCount);
        if (total is < 1 or > 500)
            ModelState.AddModelError("", "Ma trận phải chọn từ 1 đến 500 câu hỏi.");
        if (!ModelState.IsValid)
            return View("Matrix", model);

        var topicIds = model.Topics.Select(x => x.Id).ToHashSet();
        if (quotas.Any(q => !topicIds.Contains(q.TopicId)))
        {
            ModelState.AddModelError("", "Ma trận chứa chủ đề không thuộc môn được chọn.");
            return View("Matrix", model);
        }

        var questions = new List<Dictionary<string, object?>>();
        for (var page = 1; ; page++)
        {
            var result = await admin.ListAsync(UserId, Role, "questions", null, page, 200, subjectId: model.SubjectId);
            questions.AddRange(result.Items);
            if (questions.Count >= result.Total || result.Items.Count == 0)
                break;
        }
        var selectedIds = new List<int>();
        foreach (var quota in quotas)
        {
            foreach (var (difficulty, count) in new[]
            {
                ("Easy", quota.EasyCount), ("Medium", quota.MediumCount), ("Hard", quota.HardCount)
            })
            {
                if (count == 0) continue;
                var candidates = questions.Where(row =>
                        (int)row["topicId"]! == quota.TopicId &&
                        string.Equals(row["difficulty"]?.ToString(), difficulty, StringComparison.OrdinalIgnoreCase) &&
                        row["isActive"] is true && row["correctIndex"] is not null &&
                        Enumerable.Range(1, 4).All(n => !string.IsNullOrWhiteSpace(row[$"option{n}"]?.ToString())))
                    .ToList();
                if (candidates.Count < count)
                {
                    var topic = model.Topics.Single(t => t.Id == quota.TopicId).Name;
                    ModelState.AddModelError("", $"Chủ đề ‘{topic}’ mức {DifficultyLabel(difficulty)} chỉ có {candidates.Count} câu hợp lệ, nhưng ma trận yêu cầu {count}.");
                    continue;
                }
                selectedIds.AddRange(candidates.OrderBy(_ => Random.Shared.Next()).Take(count).Select(row => (int)row["id"]!));
            }
        }
        if (!ModelState.IsValid)
            return View("Matrix", model);

        var basePoints = (1000 / selectedIds.Count) / 100m;
        var extraPointCount = 1000 % selectedIds.Count;
        var questionPoints = selectedIds.Select((id, index) => new
        {
            id,
            points = basePoints + (index < extraPointCount ? 0.01m : 0m)
        }).ToDictionary(item => item.id, item => item.points);

        var request = new AdminSaveRequest(new()
        {
            ["subjectId"] = JsonSerializer.SerializeToElement(model.SubjectId),
            ["title"] = JsonSerializer.SerializeToElement(model.Title.Trim()),
            ["questionIds"] = JsonSerializer.SerializeToElement(selectedIds),
            ["questionPoints"] = JsonSerializer.SerializeToElement(questionPoints)
        });
        try
        {
            var id = await admin.SaveAsync(UserId, Role, Resource, null, request);
            TempData["Success"] = $"Đã tạo đề nháp gồm {selectedIds.Count} câu theo ma trận. Kiểm tra đề rồi lưu để hoàn tất.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (BusinessException ex) when (ex.Status is not (403 or 404))
        {
            ModelState.AddModelError("", ex.Message);
            return View("Matrix", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var paper = (await admin.ListAsync(UserId, Role, Resource, null, 1, 1, id)).Items.SingleOrDefault();
        if (paper is null)
            return NotFound();

        var points = (Dictionary<string, decimal>)paper["questionPoints"]!;
        var model = new PaperEditor
        {
            Id = id,
            Title = (string)paper["title"]!,
            SubjectId = (int)paper["subjectId"]!,
            Status = (string)paper["status"]!,
            Questions = ((int[])paper["questionIds"]!).Select(qid => new PaperQuestion
            {
                Id = qid, Selected = true, Points = points[qid.ToString(CultureInfo.InvariantCulture)]
            }).ToList()
        };
        await PopulateAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Save(PaperEditor model)
    {
        var selected = model.Questions.Where(q => q.Selected).ToList();
        if (selected.Count == 0)
            ModelState.AddModelError("", "Chọn ít nhất một câu hỏi.");
        if (selected.Select(q => q.Id).Distinct().Count() != selected.Count)
            ModelState.AddModelError("", "Danh sách câu hỏi bị trùng.");
        if (selected.Any(q => q.Points is not decimal points || points is < 0.01m or > 999999.99m || decimal.Round(points, 2) != points))
            ModelState.AddModelError("", "Trọng số câu được chọn phải dương, tối đa 999999,99 và có không quá hai chữ số thập phân.");

        if (ModelState.IsValid)
        {
            var request = new AdminSaveRequest(new()
            {
                ["subjectId"] = JsonSerializer.SerializeToElement(model.SubjectId),
                ["title"] = JsonSerializer.SerializeToElement(model.Title),
                ["questionIds"] = JsonSerializer.SerializeToElement(selected.Select(q => q.Id)),
                ["questionPoints"] = JsonSerializer.SerializeToElement(selected.ToDictionary(q => q.Id, q => q.Points!.Value))
            });
            try
            {
                var id = await admin.SaveAsync(UserId, Role, Resource, model.Id, request);
                TempData["Success"] = "Đã lưu đề nháp.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (BusinessException ex) when (ex.Status is not (403 or 404))
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (BusinessException ex)
            {
                return StatusCode(ex.Status);
            }
        }

        await PopulateAsync(model);
        if (!IsAssigned(model))
            return NotFound();

        // The options are rebuilt from the current subject; posted collection indices may no longer match.
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Questions[", StringComparison.Ordinal)).ToArray())
        {
            if (ModelState[key]!.Errors.Count > 0)
                ModelState.AddModelError("", "Trọng số câu hỏi không đúng định dạng.");
            ModelState.Remove(key);
        }
        return View("Edit", model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, bool ready)
    {
        try
        {
            await admin.ActionAsync(UserId, Role, Resource, id, ready ? "ready" : "draft");
            TempData["Success"] = ready ? "Đề đã sẵn sàng." : "Đã chuyển đề về nháp.";
        }
        catch (BusinessException ex) when (ex.Status is not (403 or 404))
        {
            TempData["Error"] = ex.Message;
        }
        catch (BusinessException ex)
        {
            return StatusCode(ex.Status);
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task PopulateAsync(PaperEditor model)
    {
        var catalog = await admin.CatalogAsync(UserId, Role);
        var fields = catalog.Resources.Single(r => r.Key == Resource).Fields;
        model.Subjects = fields.Single(f => f.Name == "subjectId").Choices ?? [];
        var selected = model.Questions.Where(q => q.Selected).GroupBy(q => q.Id)
            .ToDictionary(group => group.Key, group => group.First().Points);
        var options = new List<PaperQuestion>();
        if (IsAssigned(model))
        {
            var page = 1;
            AdminPageDto result;
            do
            {
                result = await admin.ListAsync(UserId, Role, "questions", null, page++, 200, subjectId: model.SubjectId);
                foreach (var row in result.Items.Where(r => (bool)r["isActive"]! || selected.ContainsKey((int)r["id"]!)))
                {
                    var questionId = (int)row["id"]!;
                    options.Add(new PaperQuestion
                    {
                        Id = questionId,
                        Text = (string)row["text"]!,
                        Selected = selected.ContainsKey(questionId),
                        Points = selected.GetValueOrDefault(questionId, 1m)
                    });
                }
            } while ((page - 1L) * result.PageSize < result.Total);
        }

        var order = selected.Keys.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        model.Questions = options.OrderBy(q => order.GetValueOrDefault(q.Id, int.MaxValue)).ToList();
    }

    private async Task PopulateMatrixAsync(MatrixEditor model)
    {
        var catalog = await admin.CatalogAsync(UserId, Role);
        var fields = catalog.Resources.Single(r => r.Key == Resource).Fields;
        model.Subjects = fields.Single(f => f.Name == "subjectId").Choices ?? [];
        model.Topics = [];
        if (model.SubjectId <= 0 || !model.Subjects.Any(s => s.Value == model.SubjectId.ToString(CultureInfo.InvariantCulture)))
        {
            model.SubjectId = 0;
            model.Quotas = [];
            return;
        }
        var topics = await admin.ListAsync(UserId, Role, "topics", null, 1, 200, subjectId: model.SubjectId);
        model.Topics = topics.Items.Where(row => row["isActive"] is true && (int)row["subjectId"]! == model.SubjectId)
            .Select(row => new MatrixTopic((int)row["id"]!, row["name"]?.ToString() ?? ""))
            .ToList();
        var existing = (model.Quotas ?? []).GroupBy(x => x.TopicId).ToDictionary(group => group.Key, group => group.First());
        model.Quotas = model.Topics.Select(topic => existing.GetValueOrDefault(topic.Id) ?? new TopicQuota { TopicId = topic.Id }).ToList();
    }

    private static string DifficultyLabel(string value) => value switch
    {
        "Easy" => "Dễ", "Medium" => "Trung bình", _ => "Khó"
    };

    private static bool IsAssigned(PaperEditor model) =>
        model.Subjects.Any(s => s.Value == model.SubjectId.ToString(CultureInfo.InvariantCulture));

    public sealed class PaperEditor
    {
        public int? Id { get; set; }
        public int SubjectId { get; set; }
        [Required(ErrorMessage = "Nhập tên đề thi."), StringLength(2000)]
        public string Title { get; set; } = "";
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public string Status { get; set; } = "Draft";
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public IReadOnlyList<ChoiceDto> Subjects { get; set; } = [];
        public List<PaperQuestion> Questions { get; set; } = [];
    }

    public sealed class MatrixEditor
    {
        public int SubjectId { get; set; }
        [Required, StringLength(2000)]
        public string Title { get; set; } = "";
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public IReadOnlyList<ChoiceDto> Subjects { get; set; } = [];
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public IReadOnlyList<MatrixTopic> Topics { get; set; } = [];
        public List<TopicQuota> Quotas { get; set; } = [];
    }

    public sealed record MatrixTopic(int Id, string Name);
    public sealed class TopicQuota
    {
        public int TopicId { get; set; }
        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }
    }

    public sealed class PaperQuestion
    {
        public int Id { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public string Text { get; set; } = "";
        public bool Selected { get; set; }
        public decimal? Points { get; set; }
    }
}
