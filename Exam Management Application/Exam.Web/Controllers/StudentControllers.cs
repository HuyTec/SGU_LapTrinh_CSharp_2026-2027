using System.Security.Claims;
using Exam.Core;
using Exam.Services;
using Exam.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Exam.Web.Controllers;
public class AccountController(AuthService auth):Controller{
 [HttpGet,AllowAnonymous]
 public IActionResult Register(string? accountType) => View(new RegisterVm { AccountType = accountType == "Lecturer" ? "Lecturer" : "Student" });

 [HttpPost,AllowAnonymous,ValidateAntiForgeryToken]
 public async Task<IActionResult> Register(RegisterVm vm)
 {
     if (ModelState.IsValid)
     {
         try
         {
             await auth.RegisterAsync(new(vm.UserName, vm.FullName, vm.AccountType == "Student" ? vm.StudentCode : null, vm.Email, vm.Password, vm.AccountType, vm.PhoneNumber));
             TempData["Success"] = "Đăng ký thành công. Bạn có thể đăng nhập ngay.";
             return RedirectToAction(nameof(Login));
         }
         catch (BusinessException ex) { ModelState.AddModelError("", ex.Message); }
         catch (Microsoft.EntityFrameworkCore.DbUpdateException) { ModelState.AddModelError("", "Tên đăng nhập hoặc mã thí sinh đã được sử dụng. Vui lòng kiểm tra lại."); }
     }
     vm.Password = vm.ConfirmPassword = "";
     ModelState.SetModelValue(nameof(vm.Password), new Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult(""));
     ModelState.SetModelValue(nameof(vm.ConfirmPassword), new Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult(""));
     return View(vm);
 }
 [HttpGet]public IActionResult Login(string? returnUrl)=>View(new LoginVm{ReturnUrl=returnUrl});
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Login(LoginVm vm){if(!ModelState.IsValid)return View(vm);try{var (u,role,s)=await auth.LoginAsync(new(vm.UserName,vm.Password),false);await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(AuthService.Claims(u,role,s),CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{ExpiresUtc=s.ExpiresAt});if (role == "Lecturer") return RedirectToAction("Index", "Lecturer"); return LocalRedirect(Url.IsLocalUrl(vm.ReturnUrl)?vm.ReturnUrl!:"/Sessions");}catch(BusinessException ex){ModelState.AddModelError("",ex.Message);return View(vm);}}
 [HttpPost,ValidateAntiForgeryToken,Authorize]public async Task<IActionResult> Logout(){await auth.LogoutAsync(User);await HttpContext.SignOutAsync();return RedirectToAction("Login");}
 [HttpGet]public IActionResult AccessDenied()=>StatusCode(403,"Bạn không có quyền truy cập.");
}
[AllowAnonymous]
public class HelpController : Controller
{
 public IActionResult Index() => View();
}
[Authorize(Roles = "Student,Lecturer")]
public class HomeController(ExamService exams) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Lecturer"))
            return RedirectToAction("Index", "Lecturer");

        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(await exams.SessionsAsync(studentId));
    }
}
[Authorize(Roles="Student")]
public class SessionsController(ExamService exams):Controller{
 int Uid=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public async Task<IActionResult> Index(string? search,string? status)=>View(await exams.SessionsAsync(Uid,search,status));
 [HttpGet]
 public Task<IActionResult> Join(string? code) =>
     string.IsNullOrWhiteSpace(code) ? Task.FromResult<IActionResult>(View()) : ShowSessionByCode(code);


 [HttpPost, ActionName(nameof(Join)), ValidateAntiForgeryToken]
 public Task<IActionResult> ConfirmJoin(string? code) => ShowSessionByCode(code);

 private async Task<IActionResult> ShowSessionByCode(string? code)
 {
     try
     {
         var id = await exams.FindSessionByCodeAsync(Uid, code);
         return RedirectToAction(nameof(Details), new { id });
     }
     catch (BusinessException ex)
     {
         ModelState.AddModelError("", ex.Message);
         return View("Join", code);
     }
 }

 public async Task<IActionResult> Details(int id)=>View(await exams.DetailAsync(Uid,id));
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Start(int id){try{return RedirectToAction("Take","Attempts",new{id=await exams.StartAsync(Uid,id)});}catch(BusinessException ex){TempData["Error"]=ex.Message;return RedirectToAction("Details",new{id});}}
}
[Authorize(Roles="Student")]
public class AttemptsController(ExamService exams):Controller{
 int Uid=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public async Task<IActionResult> Take(long id){var vm=await exams.TakeAsync(Uid,id);return vm.Status=="InProgress"?View(vm):RedirectToAction("Result",new{id});}
 [HttpGet]public async Task<IActionResult> Status(long id)=>Json(await exams.StatusAsync(Uid,id));
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Save(long id,[FromBody]SaveAnswerRequest r)=>Json(await exams.SaveAsync(Uid,id,r));
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Submit(long id){await exams.SubmitAsync(Uid,id);return Json(new{redirectUrl=Url.Action("Result",new{id})});}
 public async Task<IActionResult> Result(long id)=>View(await exams.ResultAsync(Uid,id));
 public async Task<IActionResult> History()=>View(await exams.HistoryAsync(Uid));
}

