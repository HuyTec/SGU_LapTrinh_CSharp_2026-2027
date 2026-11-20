using Microsoft.AspNetCore.Identity;
namespace Exam.Data;

public class AppUser : IdentityUser<int>
{
    public string LecturerCode => $"GV{Id:D6}";
    public string? RequestedRole { get; set; }
    public string FullName { get; set; } = ""; public string? StudentCode
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;
}
public class AppRole : IdentityRole<int>
{
}
public class AuthSession
{
    public Guid Id
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public AppUser User { get; set; } = null!; public DateTime ExpiresAt
    {
        get; set;
    }
    public bool Revoked
    {
        get; set;
    }
}
public class Subject
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsActive { get; set; } = true;
}
public class LecturerSubject
{
    public int Id
    {
        get; set;
    }
    public int LecturerId
    {
        get; set;
    }
    public AppUser Lecturer { get; set; } = null!; public int SubjectId
    {
        get; set;
    }
    public Subject Subject { get; set; } = null!;
}
public class Topic
{
    public int Id
    {
        get; set;
    }
    public int SubjectId
    {
        get; set;
    }
    public Subject Subject { get; set; } = null!; public string Name { get; set; } = ""; public bool IsActive { get; set; } = true;
}
public class Question
{
    public byte[] RowVersion { get; set; } = [];
    public int Id
    {
        get; set;
    }
    public int TopicId
    {
        get; set;
    }
    public Topic Topic { get; set; } = null!; public int CreatedBy
    {
        get; set;
    }
    public AppUser Creator { get; set; } = null!; public string Text { get; set; } = ""; public string Difficulty { get; set; } = "Medium"; public bool IsActive { get; set; } = true; public List<AnswerOption> Options { get; set; } = [];
}
public class AnswerOption
{
    public int Id
    {
        get; set;
    }
    public int QuestionId
    {
        get; set;
    }
    public Question Question { get; set; } = null!; public string Text { get; set; } = ""; public bool IsCorrect
    {
        get; set;
    }
    public int Order
    {
        get; set;
    }
}
public class ExamPaper
{
    public int Id
    {
        get; set;
    }
    public int SubjectId
    {
        get; set;
    }
    public Subject Subject { get; set; } = null!; public int CreatedBy
    {
        get; set;
    }
    public AppUser Creator { get; set; } = null!; public string Title { get; set; } = ""; public string Status { get; set; } = "Draft"; public List<ExamQuestion> Questions { get; set; } = [];
}
public class ExamQuestion
{
    public int Id
    {
        get; set;
    }
    public int ExamId
    {
        get; set;
    }
    public ExamPaper Exam { get; set; } = null!; public int QuestionId
    {
        get; set;
    }
    public Question Question { get; set; } = null!; public int Order
    {
        get; set;
    }
    public decimal Points { get; set; } = 1;
}
public class ExamClass
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<ClassStudent> Students { get; set; } = new List<ClassStudent>();
}

public class ClassStudent
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
}

public class SessionClass
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public ExamClass Class { get; set; } = null!;
}

public class ExamSession
{
    public string? AccessCode { get; set; }
    public ICollection<SessionClass> Classes { get; set; } = new List<SessionClass>();
    public int Id
    {
        get; set;
    }
    public int ExamId
    {
        get; set;
    }
    public ExamPaper Exam { get; set; } = null!; public int ManagedBy
    {
        get; set;
    }
    public AppUser Manager { get; set; } = null!; public string Title { get; set; } = ""; public DateTime OpensAt
    {
        get; set;
    }
    public DateTime ClosesAt
    {
        get; set;
    }
    public DateTime? AdmissionsClosedAt { get; set; }
    public DateTime? ActivatedAt
    {
        get; set;
    }
    public int DurationMinutes { get; set; } = 30; public string Status { get; set; } = "Draft"; public bool ShuffleQuestions
    {
        get; set;
    }
    public bool ShuffleOptions
    {
        get; set;
    }
    public string ReviewPolicy { get; set; } = "ScoreOnly"; public DateTime? PublishedAt
    {
        get; set;
    }
    public bool Published
    {
        get; set;
    }
    public bool HasOpened
    {
        get; set;
    }
    public List<SessionCandidate> Candidates { get; set; } = [];
}
public class SessionCandidate
{
    public int Id
    {
        get; set;
    }
    public int SessionId
    {
        get; set;
    }
    public ExamSession Session { get; set; } = null!; public int StudentId
    {
        get; set;
    }
    public AppUser Student { get; set; } = null!;
}
public class Attempt
{
    public long Id
    {
        get; set;
    }
    public int SessionId
    {
        get; set;
    }
    public ExamSession Session { get; set; } = null!; public int StudentId
    {
        get; set;
    }
    public AppUser Student { get; set; } = null!; public DateTime StartedAt
    {
        get; set;
    }
    public DateTime Deadline
    {
        get; set;
    }
    public DateTime? SubmittedAt
    {
        get; set;
    }
    public string Status { get; set; } = "InProgress"; public string? EndReason
    {
        get; set;
    }
    public List<AttemptQuestion> Questions { get; set; } = []; public ExamResult? Result
    {
        get; set;
    }
}
public class AttemptQuestion
{
    public long Id
    {
        get; set;
    }
    public long AttemptId
    {
        get; set;
    }
    public Attempt Attempt { get; set; } = null!; public int SourceQuestionId
    {
        get; set;
    }
    public string Text { get; set; } = ""; public int Order
    {
        get; set;
    }
    public decimal Points
    {
        get; set;
    }
    public List<AttemptOption> Options { get; set; } = []; public Response? Response
    {
        get; set;
    }
}
public class AttemptOption
{
    public long Id
    {
        get; set;
    }
    public long AttemptQuestionId
    {
        get; set;
    }
    public AttemptQuestion Question { get; set; } = null!; public string Text { get; set; } = ""; public int Order
    {
        get; set;
    }
    public bool IsCorrect
    {
        get; set;
    }
}
public class Response
{
    public long Id
    {
        get; set;
    }
    public long AttemptQuestionId
    {
        get; set;
    }
    public AttemptQuestion Question { get; set; } = null!; public long? SelectedOptionId
    {
        get; set;
    }
    public AttemptOption? SelectedOption
    {
        get; set;
    }
    public bool IsFlagged
    {
        get; set;
    }
    public DateTime SavedAt
    {
        get; set;
    }
}
public class ExamResult
{
    public long Id
    {
        get; set;
    }
    public long AttemptId
    {
        get; set;
    }
    public Attempt Attempt { get; set; } = null!; public decimal Score
    {
        get; set;
    }
    public int CorrectCount
    {
        get; set;
    }
    public DateTime GradedAt
    {
        get; set;
    }
}




