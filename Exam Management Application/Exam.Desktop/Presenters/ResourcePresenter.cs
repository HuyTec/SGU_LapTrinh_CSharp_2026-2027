using System.Text.Json;
using Exam.Core;

namespace Exam.Desktop;

public sealed record ResourceQuery(string Search, string? SubjectId, string? TopicId, string? Difficulty);

public interface IResourceView
{
    bool IsDisposed { get; }
    bool IsResourceVisible { get; }
    IWin32Window DialogOwner { get; }
    ResourceQuery Query { get; }
    int? SelectedRecordId { get; }
    int? SelectedSessionId { get; }
    void Configure(AdminCatalogDto catalog, ResourceDto resource, bool preserveFilters);
    void RenderPage(AdminPageDto page);
    void SetBusy(bool busy, string message);
    void Notify(string message, bool isError = false);
    void RenderQuestionDetail(Dictionary<string, JsonElement>? detail, string? message = null);
}

/// <summary>Orchestrates staff screens and HTTP calls; authorization and exam rules remain in Exam.Services.</summary>
public sealed class ResourcePresenter : IDisposable
{
    private readonly ExamApiClient api;
    private readonly IResourceView view;
    private readonly string resourceKey;
    private readonly System.Windows.Forms.Timer? polling;
    private ResourceDto? resource;
    private int page = 1;
    private int detailGeneration;
    private bool busy;
    private bool disposed;

    public ResourcePresenter(ExamApiClient api, IResourceView view, string resourceKey)
    {
        this.api = api; this.view = view; this.resourceKey = resourceKey;
        if (resourceKey == "monitor")
        {
            polling = new System.Windows.Forms.Timer { Interval = 15000 };
            polling.Tick += async (_, _) =>
            {
                if (!busy && !disposed && view.IsResourceVisible)
                    await RunAsync(() => LoadPageAsync(false, true), "Đang cập nhật theo dõi…");
            };
        }
    }

    public Task LoadAsync() => RunAsync(async () => { page = 1; await LoadPageAsync(true, false); polling?.Start(); }, "Đang tải dữ liệu…");
    public Task RefreshAsync() => RunAsync(() => LoadPageAsync(true, true), "Đang làm mới dữ liệu…");
    public Task SearchAsync() => RunAsync(async () => { page = 1; await LoadPageAsync(false, true); }, "Đang tìm kiếm…");
    public Task PreviousAsync() => RunAsync(async () => { page = Math.Max(1, page - 1); await LoadPageAsync(false, true); }, "Đang tải trang…");
    public Task NextAsync() => RunAsync(async () => { page++; await LoadPageAsync(false, true); }, "Đang tải trang…");
    public Task AddNewAsync() => EditAsync(null);
    public Task EditSelectedAsync()
    {
        if (view.SelectedRecordId is int id) return EditAsync(id);
        if (!Unavailable) view.Notify("Vui lòng chọn một dòng dữ liệu.", true);
        return Task.CompletedTask;
    }

    private async Task LoadPageAsync(bool freshCatalog, bool preserveFilters)
    {
        if (freshCatalog || resource is null)
        {
            var catalog = await api.CatalogAsync();
            if (Unavailable) return;
            resource = catalog.Resources.FirstOrDefault(r => r.Key == resourceKey)
                ?? throw new InvalidOperationException("Phiên hiện tại không có quyền truy cập chức năng này.");
            view.Configure(catalog, resource, preserveFilters);
        }
        var query = view.Query;
        var result = await api.ListAsync(resourceKey, query.Search, page, query.SubjectId, query.TopicId, query.Difficulty);
        if (Unavailable) return;
        var lastPage = Math.Max(1, (int)Math.Ceiling(result.Total / 25d));
        if (page > lastPage)
        {
            page = lastPage;
            result = await api.ListAsync(resourceKey, query.Search, page, query.SubjectId, query.TopicId, query.Difficulty);
        }
        if (Unavailable) return;
        view.RenderPage(result);
    }

    private Task EditAsync(int? id) => RunAsync(async () =>
    {
        var catalog = await api.CatalogAsync();
        if (Unavailable) return;
        resource = catalog.Resources.FirstOrDefault(r => r.Key == resourceKey)
            ?? throw new InvalidOperationException("Chức năng không còn được cấp quyền trong phiên này.");
        if (!id.HasValue && !resource.CanCreate) { view.Notify("Chức năng này chỉ dùng để xem dữ liệu.", true); return; }
        var values = id.HasValue ? await api.DetailAsync(resourceKey, id.Value) : new Dictionary<string, JsonElement>();
        if (Unavailable) return;
        // The editor carries original hidden row versions back to the API on save.
        using var editor = new ResourceEditorForm(api, resource, id, values);
        if (editor.ShowDialog(view.DialogOwner) == DialogResult.OK)
        {
            view.Notify(id.HasValue ? "Đã lưu thay đổi." : "Đã thêm dữ liệu mới.");
            await LoadPageAsync(true, true);
        }
    }, "Đang mở biểu mẫu…");

    public Task DeleteSelectedAsync() => RunAsync(async () =>
    {
        var id = RequireSelection();
        if (!UiConfirmDialog.Confirm(view.DialogOwner, "Xóa dữ liệu", "Bạn muốn xóa dòng đang chọn? Dữ liệu đã được sử dụng sẽ được máy chủ bảo vệ hoặc chuyển sang ngừng sử dụng theo quy tắc nghiệp vụ.", "Xóa dữ liệu", true)) return;
        await api.DeleteAsync(resourceKey, id);
        if (Unavailable) return;
        view.Notify("Đã xử lý yêu cầu xóa.");
        await LoadPageAsync(true, true);
    }, "Đang xóa dữ liệu…");

    public Task ActionAsync(string action, string label, bool danger = false) => RunAsync(async () =>
    {
        var id = RequireSelection();
        if (!UiConfirmDialog.Confirm(view.DialogOwner, label, $"{label} dòng đang chọn? Máy chủ sẽ kiểm tra quyền và trạng thái trước khi thực hiện.", label, danger)) return;
        await api.ActionAsync(resourceKey, id, action);
        if (Unavailable) return;
        view.Notify("Đã thực hiện: " + label.ToLowerInvariant() + ".");
        await LoadPageAsync(true, true);
    }, "Đang thực hiện thao tác…");

    public Task ExportAsync(string format) => RunAsync(async () =>
    {
        var sessionId = resourceKey == "monitor" ? view.SelectedSessionId : view.SelectedRecordId;
        if (!sessionId.HasValue) { view.Notify("Chọn một đợt thi hoặc một dòng theo dõi để xuất kết quả.", true); return; }
        using var dialog = new SaveFileDialog
        {
            Filter = format == "xlsx" ? "Excel Workbook|*.xlsx" : "CSV UTF-8|*.csv",
            FileName = $"KetQuaDotThi-{sessionId}.{format}", AddExtension = true
        };
        if (dialog.ShowDialog(view.DialogOwner) != DialogResult.OK) return;
        await api.ExportAsync(sessionId.Value, format, dialog.FileName);
        if (!Unavailable) view.Notify("Đã xuất kết quả đến " + dialog.FileName);
    }, "Đang xuất kết quả…");

    public Task ImportAsync() => RunAsync(async () =>
    {
        using var form = new ImportStudentsForm(api);
        if (form.ShowDialog(view.DialogOwner) == DialogResult.OK)
        {
            view.Notify("Đã nhập danh sách thí sinh.");
            await LoadPageAsync(true, true);
        }
    }, "Đang mở danh sách nhập…");

    public async Task SelectQuestionAsync(int? id)
    {
        if (resourceKey != "questions" || Unavailable) return;
        var generation = ++detailGeneration;
        view.RenderQuestionDetail(null, id.HasValue ? "Đang tải nội dung câu hỏi…" : "Chọn một câu hỏi để xem nội dung và đáp án.");
        if (!id.HasValue) return;
        try
        {
            var detail = await api.DetailAsync(resourceKey, id.Value);
            if (Unavailable || generation != detailGeneration || view.SelectedRecordId != id) return;
            view.RenderQuestionDetail(detail);
        }
        catch (Exception ex)
        {
            if (!Unavailable && generation == detailGeneration)
                view.RenderQuestionDetail(null, Friendly(ex));
        }
    }

    private int RequireSelection() => view.SelectedRecordId ?? throw new InvalidOperationException("Vui lòng chọn một dòng dữ liệu.");
    private bool Unavailable => disposed || view.IsDisposed;
    private async Task RunAsync(Func<Task> action, string message)
    {
        if (busy || Unavailable) return;
        busy = true; view.SetBusy(true, message);
        try { await action(); }
        catch (Exception ex) { if (!Unavailable) view.Notify(Friendly(ex), true); }
        finally
        {
            busy = false;
            if (!Unavailable) view.SetBusy(false, "");
        }
    }

    private static string Friendly(Exception error) => error is HttpRequestException or TaskCanceledException
        ? "Không kết nối được máy chủ. Kiểm tra kết nối và bấm Làm mới để thử lại."
        : error.Message;

    public void Dispose()
    {
        disposed = true; detailGeneration++;
        polling?.Stop(); polling?.Dispose();
    }
}
