namespace Exam.Core.Contracts.Exams;

/// <summary>Request to create an exam paper for a subject.</summary>
public sealed record CreateExamPaperDto(int SubjectId, string Title);
