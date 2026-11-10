namespace Exam.Core.Contracts.Classes;

/// <summary>Request to create a class under a subject.</summary>
public sealed record CreateClassDto(string Code, string Name, int SubjectId, string? Term);
