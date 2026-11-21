using Exam.Services;
using Xunit;

namespace Exam.Tests;

public class ResultStatisticsTests
{
    [Fact]
    public void SummarySeparatesParticipationAndCountsFinishedBeforeAndAfterGrading()
    {
        var rows = new[]
        {
            Row("SV01", "Nguyễn An", "NotStarted"),
            Row("SV02", "Trần Bình", "InProgress"),
            Row("SV03", "Phạm Châu", "Submitted"),
            Row("SV04", "Lê Dũng", "Submitted", 0m)
        };

        var summary = ResultStatistics.Summarize(rows);

        Assert.Equal(new ResultStatistics(1, 1, 2), summary);
        Assert.Equal(4, summary.Total);
        Assert.Null(rows[0]["score"]);
        Assert.Equal(0m, rows[3]["score"]);
    }

    [Fact]
    public void FilterMatchesNameOrCodeAndCombinesSearchWithStatus()
    {
        var rows = new[]
        {
            Row("SV01", "Nguyễn An", "NotStarted"),
            Row("SV02", "Nguyễn Bình", "Submitted", 0m),
            Row("SV03", "Trần Châu", "Submitted", 9m)
        };

        Assert.Same(rows[1], Assert.Single(ResultStatistics.Filter(rows, " sv02 ", null)));
        Assert.Same(rows[1], Assert.Single(ResultStatistics.Filter(rows, "NGUYỄN", "submitted")));
        Assert.Same(rows[0], Assert.Single(ResultStatistics.Filter(rows, null, "NotStarted")));
        Assert.Empty(ResultStatistics.Filter(rows, "Nguyễn", "InProgress"));
        Assert.Equal(rows, ResultStatistics.Filter(rows, " ", null));
    }

    [Fact]
    public void EmptySessionHasZeroParticipants()
    {
        Assert.Equal(new ResultStatistics(0, 0, 0), ResultStatistics.Summarize([]));
        Assert.Empty(ResultStatistics.Filter([], "SV01", "Submitted"));
    }

    private static Dictionary<string, object?> Row(string code, string student, string status, decimal? score = null) => new()
    {
        ["studentCode"] = code, ["student"] = student, ["status"] = status, ["score"] = score
    };
}
