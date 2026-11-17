using System.ComponentModel.DataAnnotations;
using Exam.Core;
using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Exam.Services;

public sealed class ProfileService(ExamDbContext db, UserManager<AppUser> users)
{
    public async Task<ProfileDto> GetAsync(int userId)
    {
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new BusinessException("Không tìm thấy tài khoản.", 404);
        var roles = await users.GetRolesAsync(user);
        var subjects = roles.Contains("Lecturer")
            ? await db.LecturerSubjects.Where(a => a.LecturerId == userId)
                .OrderBy(a => a.Subject.Name).Select(a => a.Subject.Name).ToListAsync()
            : new List<string>();
        var count = roles.Contains("Student")
            ? await db.Set<ClassStudent>().CountAsync(m => m.StudentId == userId) : 0;
        return new(user.UserName ?? "", user.FullName, user.Email, user.PhoneNumber,
            user.StudentCode, roles.ToArray(), user.ConcurrencyStamp ?? "", subjects, count);
    }

    /// <summary>Update only contact fields of the authenticated user, using Identity concurrency protection.</summary>
    public async Task<ProfileDto> UpdateAsync(int userId, UpdateProfileRequest request)
    {
        var normalized = new UpdateProfileRequest(request.FullName?.Trim() ?? "",
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(), request.Version);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(normalized, new ValidationContext(normalized), errors, true))
            throw new BusinessException("Kiểm tra họ tên (tối đa 120 ký tự), email và số điện thoại (tối đa 20 ký tự).");
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new BusinessException("Không tìm thấy tài khoản.", 404);
        if (user.ConcurrencyStamp != normalized.Version)
            throw new BusinessException("Hồ sơ đã thay đổi. Tải lại trước khi lưu.", 409);
        user.FullName = normalized.FullName;
        if (!string.Equals(user.Email, normalized.Email, StringComparison.OrdinalIgnoreCase))
            user.EmailConfirmed = false;
        if (user.PhoneNumber != normalized.PhoneNumber)
            user.PhoneNumberConfirmed = false;
        user.Email = normalized.Email;
        user.PhoneNumber = normalized.PhoneNumber;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BusinessException(result.Errors.Any(e => e.Code == "ConcurrencyFailure")
                ? "Hồ sơ đã thay đổi. Tải lại trước khi lưu."
                : "Không thể lưu hồ sơ. Kiểm tra lại thông tin.",
                result.Errors.Any(e => e.Code == "ConcurrencyFailure") ? 409 : 400);
        return await GetAsync(userId);
    }
}
