namespace Exam.Core.Contracts.Exams;

/// <summary>Question reference and scoring weight in an exam paper.</summary>
public sealed record ExamQuestionDto(int QuestionId, int Order, decimal Points);
