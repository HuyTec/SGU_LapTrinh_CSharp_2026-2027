using System.Security.Claims;
using Exam.Data;
using Exam.Services;
using Exam.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exam.Tests;

public partial class ClassRegistrationTests
{
    [SqlFact]
    public async Task TopicFiltersApplyBeforeCountingAndPagingWithinAssignedSubjects()
    {
        var (lecturer, subject, forbidden, topics) = await SeedMatrixTopicsAsync();
        var admin = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), TimeProvider.System);
        var first = await admin.ListAsync(lecturer, "Lecturer", "topics", null, 1, 200,
            subjectId: subject, isActive: true);
        var second = await admin.ListAsync(lecturer, "Lecturer", "topics", null, 2, 200,
            subjectId: subject, isActive: true);
        Assert.Equal(201, first.Total);
        Assert.Equal(201, second.Total);
        Assert.Equal(200, first.Items.Count);
        Assert.Single(second.Items);
        Assert.Equal(topics, first.Items.Concat(second.Items).Select(row => (int)row["id"]!).ToArray());
        Assert.All(first.Items.Concat(second.Items), row => Assert.Equal(subject, row["subjectId"]));
        var inactive = await admin.ListAsync(lecturer, "Lecturer", "topics", null, 1, 200,
            subjectId: subject, isActive: false);
        Assert.Equal(1, inactive.Total);
        Assert.False((bool)Assert.Single(inactive.Items)["isActive"]!);
        var denied = await admin.ListAsync(lecturer, "Lecturer", "topics", null, 1, 200, subjectId: forbidden);
        Assert.Equal(0, denied.Total);
        Assert.Empty(denied.Items);
    }

    [SqlFact]
    public async Task MatrixLoadsEveryTopicAndBuildsPaperFromATopicBeyondTheFirstPage()
    {
        var (lecturer, subject, forbidden, topics) = await SeedMatrixTopicsAsync();
        var admin = new AdminService(Db, provider.GetRequiredService<UserManager<AppUser>>(), TimeProvider.System);
        var controller = new LecturerPapersController(admin)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, lecturer.ToString()),
                        new Claim(ClaimTypes.Role, "Lecturer")], "Test"))
                }
            }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new MatrixTestTempDataProvider());
        var view = Assert.IsType<ViewResult>(await controller.Matrix(subject));
        var model = Assert.IsType<LecturerPapersController.MatrixEditor>(view.Model);
        Assert.Equal(topics, model.Topics.Select(topic => topic.Id).ToArray());
        Assert.Equal(topics, model.Quotas.Select(quota => quota.TopicId).ToArray());
        var hiddenView = Assert.IsType<ViewResult>(await controller.Matrix(forbidden));
        Assert.Empty(Assert.IsType<LecturerPapersController.MatrixEditor>(hiddenView.Model).Topics);

        var created = Assert.IsType<RedirectToActionResult>(await controller.CreateFromMatrix(new()
        {
            SubjectId = subject, Title = "Last-page topic paper",
            Quotas = [new() { TopicId = topics[^1], EasyCount = 1 }]
        }));
        Assert.Equal("Edit", created.ActionName);
        var paper = await Db.Exams.Include(e => e.Questions).ThenInclude(q => q.Question).SingleAsync();
        Assert.Equal(subject, paper.SubjectId);
        Assert.Equal(topics[^1], Assert.Single(paper.Questions).Question.TopicId);
        Assert.Equal(10m, Assert.Single(paper.Questions).Points);
    }

    private async Task<(int Lecturer, int Subject, int Forbidden, int[] Topics)> SeedMatrixTopicsAsync()
    {
        var lecturer = new AppUser { UserName = "matrix-lecturer", FullName = "Matrix lecturer" };
        var earlier = new Subject { Code = "EARLIER", Name = "Other assigned subject" };
        var selected = new Subject { Code = "SELECTED", Name = "Selected subject" };
        var forbidden = new Subject { Code = "FORBIDDEN", Name = "Unassigned subject" };
        Db.AddRange(lecturer, earlier, selected, forbidden);
        await Db.SaveChangesAsync();
        Db.LecturerSubjects.AddRange(
            new() { LecturerId = lecturer.Id, SubjectId = earlier.Id },
            new() { LecturerId = lecturer.Id, SubjectId = selected.Id });
        Db.Topics.AddRange(Enumerable.Range(1, 205).Select(i =>
            new Topic { SubjectId = earlier.Id, Name = $"Earlier {i:D3}" }));
        await Db.SaveChangesAsync();
        var topics = Enumerable.Range(1, 201).Select(i =>
            new Topic { SubjectId = selected.Id, Name = $"Selected {i:D3}" }).ToArray();
        Db.Topics.AddRange(topics);
        Db.Topics.AddRange(new Topic { SubjectId = selected.Id, Name = "Inactive", IsActive = false },
            new Topic { SubjectId = forbidden.Id, Name = "Private" });
        await Db.SaveChangesAsync();
        var ordered = topics.OrderBy(topic => topic.Id).ToArray();
        Db.Questions.Add(new Question
        {
            CreatedBy = lecturer.Id, TopicId = ordered[^1].Id, Text = "Last-page question", Difficulty = "Easy",
            Options = Enumerable.Range(1, 4).Select(i =>
                new AnswerOption { Text = $"Option {i}", Order = i, IsCorrect = i == 1 }).ToList()
        });
        await Db.SaveChangesAsync();
        return (lecturer.Id, selected.Id, forbidden.Id, ordered.Select(topic => topic.Id).ToArray());
    }

    private sealed class MatrixTestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
