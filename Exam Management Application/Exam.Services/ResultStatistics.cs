namespace Exam.Services;

public sealed record ResultStatistics(int NotStarted, int InProgress, int Finished)
{
    public int Total => NotStarted + InProgress + Finished;

    public static ResultStatistics Summarize(IReadOnlyList<Dictionary<string, object?>> rows) => new(
        rows.Count(row => row.GetValueOrDefault("status") is "NotStarted"),
        rows.Count(row => row.GetValueOrDefault("status") is "InProgress"),
        rows.Count(row => row.GetValueOrDefault("status") is "Submitted"));

    public static IReadOnlyList<Dictionary<string, object?>> Filter(
        IEnumerable<Dictionary<string, object?>> rows, string? search, string? status)
    {
        search = search?.Trim();
        status = status?.Trim();
        return rows.Where(row =>
            (string.IsNullOrEmpty(status) || string.Equals(row.GetValueOrDefault("status") as string, status, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(search) ||
             row.GetValueOrDefault("studentCode") is string code && code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             row.GetValueOrDefault("student") is string name && name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
