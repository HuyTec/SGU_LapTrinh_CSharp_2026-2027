using System.Globalization;
using System.Text.Json;

namespace Exam.Desktop;

public record DashboardMetric(string Label, string Value, string Description);
public record DashboardItem(string Title, string Subtitle, string Status);
public record DashboardSnapshot(string Heading, string Description, string NextStep, string NextKey,
    IReadOnlyList<DashboardMetric> Metrics, IReadOnlyList<DashboardItem> Recent,
    IReadOnlyList<int> ScoreBins, int GradedCount, decimal? AverageScore, string RecentTitle);

/// <summary>Read-only presentation summaries over the user's authorized API data; never grades or changes exam state.</summary>
public sealed class DashboardPresenter(ExamApiClient api)
{
    public async Task<DashboardSnapshot> LoadAsync()
    {
        var catalog = await api.CatalogAsync();
        string[] keys = catalog.Role switch
        {
            "Administrator" => ["users", "assignments"],
            "Lecturer" => ["questions", "topics", "exams", "monitor"],
            _ => ["sessions", "monitor"]
        };
        var loaded = await Task.WhenAll(keys.Select(async key => (Key: key, Rows: await api.ListAllAsync(key))));
        var data = loaded.ToDictionary(x => x.Key, x => x.Rows);
        IReadOnlyList<Dictionary<string, object?>> Rows(string key) => data.GetValueOrDefault(key) ?? [];
        DashboardMetric Metric(string name, int value, string hint) => new(name, value.ToString("N0"), hint);
        var monitor = Rows("monitor");
        var scores = monitor.Select(r => Number(r, "score")).Where(s => s.HasValue).Select(s => s!.Value).ToList();
        var bins = new int[5];
        foreach (var score in scores) bins[Math.Clamp((int)(score / 2), 0, 4)]++;
        var average = scores.Count == 0 ? (decimal?)null : scores.Average();

        if (catalog.Role == "Administrator")
        {
            var people = Rows("users");
            DashboardMetric[] metrics =
            [
                Metric("Tài khoản", people.Count, "Trong hệ thống"),
                Metric("Chờ phê duyệt", people.Count(r => Value(r, "requestedRole").Length > 0), "Đăng ký nhân sự mới"),
                Metric("Giảng viên", people.Count(r => HasRole(r, "Lecturer")), "Đã được cấp vai trò"),
                Metric("Thí sinh", people.Count(r => HasRole(r, "Student")), "Hồ sơ tham gia thi")
            ];
            var recent = people.OrderByDescending(r => Number(r, "id")).Take(6)
                .Select(r => new DashboardItem(Value(r, "fullName"), Value(r, "userName"), Value(r, "requestedRole").Length > 0
                    ? "Chờ duyệt" : Value(r, "isActive").Equals("True", StringComparison.OrdinalIgnoreCase) ? "Hoạt động" : "Đã khóa")).ToList();
            return new("Mỗi kỳ thi bắt đầu từ một đội ngũ sẵn sàng.", "Quản lý đúng người, đúng vai trò và đúng phạm vi giảng dạy.",
                "Quản lý tài khoản", "users", metrics, recent, bins, 0, null, "Thành viên gần đây");
        }
        if (catalog.Role == "Lecturer")
        {
            var papers = Rows("exams");
            DashboardMetric[] metrics =
            [
                Metric("Câu hỏi", Rows("questions").Count, $"Trong {Rows("topics").Count} chủ đề được giao"),
                Metric("Đề sẵn sàng", papers.Count(r => Value(r, "status") == "Ready"), "Đã xác nhận cấu trúc"),
                Metric("Đang làm bài", monitor.Count(r => Value(r, "status") == "InProgress"), "Trong môn được phân công"),
                Metric("Bài đã chấm", scores.Count, "Có kết quả từ máy chủ")
            ];
            var recent = papers.OrderByDescending(r => Number(r, "id")).Take(6)
                .Select(r => new DashboardItem(Value(r, "title"), Value(r, "subject"), UiLabels.Of(Value(r, "status")))).ToList();
            return new("Chuẩn bị chỉn chu. Đánh giá công bằng.", "Tổ chức ngân hàng câu hỏi và xây dựng đề thi cho môn học được phân công.",
                "Mở ngân hàng câu hỏi", "questions", metrics, recent, bins, scores.Count, average, "Đề thi gần đây");
        }
        var sessions = Rows("sessions");
        DashboardMetric[] officerMetrics =
        [
            Metric("Sắp diễn ra", sessions.Count(r => Value(r, "status") == "Scheduled"), "Đã kích hoạt, chưa mở"),
            Metric("Đang mở", sessions.Count(r => Value(r, "status") == "Open"), "Theo giờ máy chủ"),
            Metric("Bài đã chấm", scores.Count, "Trong các đợt phụ trách"),
            Metric("Thí sinh", monitor.Select(r => Value(r, "studentCode")).Where(s => s.Length > 0).Distinct().Count(), "Đã phân công trong phạm vi")
        ];
        var recentSessions = sessions.OrderByDescending(r => Value(r, "opensAt")).Take(6)
            .Select(r => new DashboardItem(Value(r, "title"), DisplayDate(Value(r, "opensAt")), UiLabels.Of(Value(r, "status")))).ToList();
        return new("Một không gian cho kỳ thi được tổ chức tốt.", "Theo dõi lịch, danh sách dự thi và kết quả của các đợt bạn phụ trách.",
            "Quản lý đợt thi", "sessions", officerMetrics, recentSessions, bins, scores.Count, average, "Lịch thi gần đây");
    }

    public static string Value(Dictionary<string, object?> row, string key) => row.GetValueOrDefault(key)?.ToString() ?? "";
    public static decimal? Number(Dictionary<string, object?> row, string key) => decimal.TryParse(Value(row, key), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
    private static bool HasRole(Dictionary<string, object?> row, string role) => row.GetValueOrDefault("roles") is JsonElement { ValueKind: JsonValueKind.Array } roles && roles.EnumerateArray().Any(r => r.GetString() == role);
    private static string DisplayDate(string value) => DateTimeOffset.TryParse(value, out var date) ? TimeZoneInfo.ConvertTime(date, VietNamTime.Zone).ToString("dd/MM/yyyy  HH:mm") : "Lịch chưa được cập nhật";
}
