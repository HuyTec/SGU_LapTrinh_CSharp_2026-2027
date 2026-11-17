namespace Exam.Services;

/// <summary>Pure business rules shared by use cases and verified with deterministic unit tests.</summary>
public static class ExamRules
{
    public static DateTime Deadline(DateTime started, int minutes)
        => started.AddMinutes(minutes);

    public static decimal Score(decimal earned, decimal total)
        => total <= 0 ? 0 : Math.Round(earned / total * 10, 2, MidpointRounding.AwayFromZero);

    public static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart < bEnd && bStart < aEnd;
}
