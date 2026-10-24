using System.ComponentModel.DataAnnotations;
namespace Exam.Web.ViewModels;
public class RegisterVm : IValidatableObject
{
    [Required(ErrorMessage="Vui lòng nhập tên đăng nhập."),StringLength(60,ErrorMessage="Tên đăng nhập tối đa 60 ký tự.")]
    public string UserName {get;set;}="";
    [Required(ErrorMessage="Vui lòng nhập họ tên."),StringLength(120,ErrorMessage="Họ tên tối đa 120 ký tự.")]
    public string FullName {get;set;}="";
    [StringLength(30,ErrorMessage="Mã thí sinh tối đa 30 ký tự.")]
    public string? StudentCode {get;set;}="";
    [Required,RegularExpression("^(Student|Lecturer|ExamOfficer|Administrator)$",ErrorMessage="Loại tài khoản không hợp lệ.")]
    public string AccountType {get;set;}="Student";
    [Required(ErrorMessage="Vui lòng nhập email."),EmailAddress(ErrorMessage="Email không đúng định dạng."),StringLength(256)]
    public string Email {get;set;}="";
    [Required(ErrorMessage="Vui lòng nhập mật khẩu."),StringLength(100,MinimumLength=8,ErrorMessage="Mật khẩu cần từ 8 đến 100 ký tự."),DataType(DataType.Password)]
    public string Password {get;set;}="";
    [Required(ErrorMessage="Vui lòng xác nhận mật khẩu."),Compare(nameof(Password),ErrorMessage="Mật khẩu xác nhận không khớp."),DataType(DataType.Password)]
    public string ConfirmPassword {get;set;}="";
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(AccountType=="Student"&&string.IsNullOrWhiteSpace(StudentCode))
            yield return new ValidationResult("Vui lòng nhập mã thí sinh.",[nameof(StudentCode)]);
    }
}
