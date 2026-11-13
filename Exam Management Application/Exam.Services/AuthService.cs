using System.Security.Claims;
using Exam.Core;
using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Exam.Services;

public class AuthService(ExamDbContext db, UserManager<AppUser> users, TimeProvider clock)
{
    /// <summary>Students activate immediately; staff registrations remain inactive without roles until administrator approval.</summary>
    public Task RegisterStudentAsync(RegisterStudentRequest request) => RegisterAsync(new(request.UserName,request.FullName,request.StudentCode,request.Email,request.Password,"Student"));
    public async Task RegisterAsync(RegisterRequest request)
    {
        var userName = request.UserName.Trim();
        var fullName = request.FullName.Trim();
        if(!new[]{"Student","Lecturer"}.Contains(request.AccountType))throw new BusinessException("Loại tài khoản không hợp lệ.");
        var isStudent=request.AccountType=="Student";
        var studentCode = isStudent ? request.StudentCode?.Trim() : null;
        var email = request.Email.Trim();
        if (userName.Length is 0 or > 60 || fullName.Length is 0 or > 120 || (isStudent && (string.IsNullOrWhiteSpace(studentCode) || studentCode.Length > 30)))
            throw new BusinessException("Tên đăng nhập, họ tên và mã thí sinh là bắt buộc, trong giới hạn độ dài cho phép.");
        if (email.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new BusinessException("Email không đúng định dạng.");
        if (request.Password.Length is < 8 or > 100)
            throw new BusinessException("Mật khẩu cần từ 8 đến 100 ký tự.");

        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (await users.FindByNameAsync(userName) != null)
                throw new BusinessException("Tên đăng nhập đã được sử dụng.", 409);
            if (isStudent && await db.Users.AnyAsync(u => u.StudentCode == studentCode))
                throw new BusinessException("Mã thí sinh đã được sử dụng. Nếu đã được cấp tài khoản, hãy liên hệ cán bộ để lấy lại thông tin đăng nhập.", 409);

            var user = new AppUser { UserName = userName, FullName = fullName, StudentCode = studentCode, Email = email, IsActive = isStudent, RequestedRole = isStudent ? null : request.AccountType };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded)
            {
                var errors = created.Errors.Select(e => e.Code switch
                {
                    "DuplicateUserName" => "Tên đăng nhập đã được sử dụng.",
                    "InvalidUserName" => "Tên đăng nhập chỉ dùng chữ cái không dấu, số và ký tự - . _ @ +.",
                    "PasswordRequiresDigit" => "Mật khẩu cần có chữ số.",
                    "PasswordRequiresLower" => "Mật khẩu cần có chữ thường.",
                    "PasswordRequiresUpper" => "Mật khẩu cần có chữ hoa.",
                    "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần có ký tự đặc biệt.",
                    _ => "Không thể tạo tài khoản. Kiểm tra thông tin và yêu cầu mật khẩu."
                });
                throw new BusinessException(string.Join(" ", errors.Distinct()));
            }
            if(isStudent)
            {
                var assigned = await users.AddToRoleAsync(user,"Student");
                if(!assigned.Succeeded)
                    throw new BusinessException("Chưa thể cấp tài khoản thí sinh. Vui lòng liên hệ quản trị viên.");
            }
            await transaction.CommitAsync();
        });
    }

    public async Task<(AppUser User, string Role, AuthSession Session)> LoginAsync(LoginRequest request, bool desktop)
    {
        var u = await users.FindByNameAsync(request.UserName);
        if (u == null || await users.IsLockedOutAsync(u) || !await users.CheckPasswordAsync(u, request.Password))
        {
            if (u != null)
                await users.AccessFailedAsync(u);
            throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng.", 401);
        }
        await users.ResetAccessFailedCountAsync(u);
        if(!u.IsActive)
            throw new BusinessException(u.RequestedRole!=null?"Tài khoản đang chờ quản trị viên duyệt. Vui lòng liên hệ quản trị viên trước khi đăng nhập.":"Tên đăng nhập hoặc mật khẩu không đúng.",u.RequestedRole!=null?403:401);
        var roles = await users.GetRolesAsync(u);
        var role = desktop ? "Administrator" : roles.Contains("Lecturer") ? "Lecturer" : "Student";
        if (!roles.Contains(role) || (desktop && !string.IsNullOrWhiteSpace(request.Role) && request.Role != role))
            throw new BusinessException(desktop
                ? "Chỉ quản trị viên được đăng nhập ứng dụng Desktop."
                : "Chỉ giảng viên và thí sinh được đăng nhập trên Web.", 403);
        var session = new AuthSession { Id = Guid.NewGuid(), UserId = u.Id, ExpiresAt = clock.GetUtcNow().UtcDateTime.AddHours(4) };
        db.AuthSessions.Add(session);
        await db.SaveChangesAsync();
        return (u, role, session);
    }
    public static List<Claim> Claims(AppUser u, string role, AuthSession session) => [new(ClaimTypes.NameIdentifier, u.Id.ToString()), new(ClaimTypes.Name, u.FullName), new(ClaimTypes.Role, role), new("sid", session.Id.ToString())];
    public async Task<bool> ValidAsync(ClaimsPrincipal principal, bool desktop)
    {
        var role = principal.FindFirstValue(ClaimTypes.Role);
        if ((desktop ? role != "Administrator" : role is not ("Lecturer" or "Student"))
            || !Guid.TryParse(principal.FindFirstValue("sid"), out var sid)
            || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return false;

        var now = clock.GetUtcNow().UtcDateTime;
        var normalizedRole = users.NormalizeName(role);
        return await db.AuthSessions.AnyAsync(s => s.Id == sid && s.UserId == userId
            && !s.Revoked && s.ExpiresAt > now && s.User.IsActive
            && db.UserRoles.Any(link => link.UserId == userId
                && db.Roles.Any(r => r.Id == link.RoleId && r.NormalizedName == normalizedRole)));
    }
    public async Task LogoutAsync(ClaimsPrincipal principal)
    {
        if (Guid.TryParse(principal.FindFirstValue("sid"), out var sid))
            await db.AuthSessions.Where(s => s.Id == sid).ExecuteUpdateAsync(s => s.SetProperty(x => x.Revoked, true));
    }
}



