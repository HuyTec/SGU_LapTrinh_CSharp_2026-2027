using Exam.Core;
using Exam.Services;
using Xunit;

namespace Exam.Tests;

public class ExamRulesTests
{
    private static readonly DateTime Start = new(2026, 10, 24, 7, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(30, 60)]
    [InlineData(60, 30)]
    [InlineData(30, 30)]
    [InlineData(1, 60)]
    public void DeadlineAlwaysGivesTheFullDuration(int duration, int minutesUntilClose)
    {
        var deadline = ExamRules.Deadline(Start, duration);
        Assert.Equal(Start.AddMinutes(duration), deadline);
        Assert.Equal(duration > minutesUntilClose, deadline > Start.AddMinutes(minutesUntilClose));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 10)]
    [InlineData(7.5, 7.5)]
    [InlineData(2.345, 2.35)]
    public void TenPointExamsPreserveEarnedPointsAndRoundOnlyTheFinalScore(decimal earned, decimal expected)
    {
        Assert.Equal(expected, ExamRules.Score(earned, 10m));
    }

    [Fact]
    public void AdjacentSessionsDoNotOverlap()
    {
        Assert.False(ExamRules.Overlaps(Start, Start.AddMinutes(30), Start.AddMinutes(30), Start.AddHours(1)));
    }

    [Fact]
    public void ASessionContainedInsideAnotherStillOverlaps()
    {
        Assert.True(ExamRules.Overlaps(Start, Start.AddHours(2), Start.AddMinutes(30), Start.AddHours(1)));
    }
}
