namespace Exam.Core.Contracts.Attempts;

/// <summary>Idempotent submit command for an attempt owned by the caller.</summary>
public sealed record SubmitAttemptDto(long AttemptId);
