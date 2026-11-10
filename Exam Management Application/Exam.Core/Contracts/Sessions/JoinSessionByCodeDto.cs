namespace Exam.Core.Contracts.Sessions;

/// <summary>Request to join an open exam session using its access code.</summary>
public sealed record JoinSessionByCodeDto(string AccessCode);
