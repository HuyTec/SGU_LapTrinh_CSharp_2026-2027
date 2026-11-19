using System.ComponentModel.DataAnnotations;

namespace Exam.Core;

public record LoginRequest(string UserName, string Password, string? Role = null);
public record RegisterRequest(string UserName,string FullName,string? StudentCode,string Email,string Password,string AccountType="Student",string? PhoneNumber=null);
public record RegisterStudentRequest(string UserName, string FullName, string StudentCode, string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt, string DisplayName, string Role);
public record ProfileDto(string UserName, string FullName, string? Email, string? PhoneNumber,
    string? StudentCode, IReadOnlyList<string> Roles, string Version, IReadOnlyList<string> Subjects, int ClassCount);
public sealed class UpdateProfileRequest
{
    public UpdateProfileRequest() { }
    public UpdateProfileRequest(string fullName, string? email, string? phoneNumber, string version)
    {
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Version = version;
    }
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(120, ErrorMessage = "Họ tên tối đa 120 ký tự.")]
    public string FullName { get; set; } = "";
    [EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
    public string? Email { get; set; }
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string? PhoneNumber { get; set; }
    [Required(ErrorMessage = "Hãy tải lại hồ sơ trước khi lưu.")]
    public string Version { get; set; } = "";
}
public record ChoiceDto(string Value, string Label);
public record ClassMemberDto(int StudentId, string? StudentCode, string FullName, bool IsActive);
public record FieldDto(string Name, string Label, string Type = "text", bool Required = false, IReadOnlyList<ChoiceDto>? Choices = null);
public record ResourceDto(string Key, string Label, IReadOnlyList<FieldDto> Fields, bool CanCreate = true, bool CanDelete = true);
public record AdminCatalogDto(string DisplayName, string Role, IReadOnlyList<ResourceDto> Resources);
public record AdminPageDto(IReadOnlyList<Dictionary<string, object?>> Items, int Total, int Page, int PageSize);
public record AdminSaveRequest(Dictionary<string, System.Text.Json.JsonElement> Values);
public record ActionRequest(string Action);
public record SessionDto(int Id, string Title, string Subject, DateTime OpensAt, DateTime ClosesAt, int DurationMinutes, string Status, long? AttemptId, bool Published, string ReviewPolicy);
public record SessionListVm(IReadOnlyList<SessionDto> Sessions, string? Search = null, string? Status = null);
public record SessionDetailVm(SessionDto Session, string Instructions);
public record OptionDto(long Id, string Text, int Order);
public record QuestionDto(long Id, string Text, int Order, decimal Points, IReadOnlyList<OptionDto> Options, long? SelectedOptionId, bool IsFlagged);
public record AttemptVm(long Id, int SessionId, string Title, DateTime ServerNow, DateTime Deadline, string Status, IReadOnlyList<QuestionDto> Questions);
public record SaveAnswerRequest(long QuestionId, long? OptionId, bool IsFlagged);
public record AttemptStatusDto(long Id, DateTime ServerNow, DateTime Deadline, string Status);
public record ReviewQuestionDto(string Text, string? SelectedAnswer, string? CorrectAnswer, bool? IsCorrect, decimal Points);
public record ResultVm(long AttemptId, string Title, string Status, bool Published, decimal? Score, int? CorrectCount, int QuestionCount, DateTime? SubmittedAt, string ReviewPolicy, IReadOnlyList<ReviewQuestionDto> Questions);
public record HistoryVm(IReadOnlyList<ResultVm> Results);
public class BusinessException(string message, int status = 400, string? field = null) : Exception(message)
{
    public int Status { get; } = status;
    public string? Field { get; } = field;
}
public record ImportStudentsRequest(string Csv, bool Preview = false);



