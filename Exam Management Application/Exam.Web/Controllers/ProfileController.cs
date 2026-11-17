using System.Security.Claims;
using Exam.Core;
using Exam.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Student,Lecturer")]
public sealed class ProfileController(ProfileService profiles) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await profiles.GetAsync(UserId);
        ViewData["Profile"] = profile;
        return View(new UpdateProfileRequest(profile.FullName, profile.Email, profile.PhoneNumber, profile.Version));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UpdateProfileRequest model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var profile = await profiles.UpdateAsync(UserId, model);
                var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var identity = new ClaimsIdentity(User.Identity as ClaimsIdentity);
                foreach (var name in identity.FindAll(ClaimTypes.Name).ToList()) identity.RemoveClaim(name);
                identity.AddClaim(new Claim(ClaimTypes.Name, profile.FullName));
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity), authentication.Properties);
                TempData["Success"] = "Đã cập nhật hồ sơ.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }
        ViewData["Profile"] = await profiles.GetAsync(UserId);
        return View(model);
    }
}
