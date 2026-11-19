using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Exam.Core;
using Exam.Core.Contracts.Classes;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
namespace Exam.Web.Controllers;
[ApiController,Route("api/auth")]
public class AuthApiController(AuthService auth,IConfiguration config):ControllerBase{
 [HttpPost("login")]public async Task<LoginResponse> Login(LoginRequest request){var(u,role,s)=await auth.LoginAsync(request,true);var token=new JwtSecurityToken(config["Jwt:Issuer"]??"Exam.Web",config["Jwt:Audience"]??"Exam.Desktop",AuthService.Claims(u,role,s),DateTime.UtcNow,s.ExpiresAt,new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),SecurityAlgorithms.HmacSha256));return new(new JwtSecurityTokenHandler().WriteToken(token),s.ExpiresAt,u.FullName,role);}
 [HttpPost("logout"),Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme)]public async Task<IActionResult> Logout(){await auth.LogoutAsync(User);return NoContent();}
}
[ApiController,Route("api/admin"),Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme,Roles="Administrator")]
public class AdminController(AdminService admin,ExportService export):ControllerBase{
 int Uid=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);string Role=>User.FindFirstValue(ClaimTypes.Role)!;
 [HttpGet("catalog")]public Task<AdminCatalogDto> Catalog()=>admin.CatalogAsync(Uid,Role);
 [HttpGet("{resource}")]public Task<AdminPageDto> List(string resource,string? search,int page=1,int pageSize=100,int? subjectId=null,int? topicId=null,string? difficulty=null,bool? isActive=null,string? accountRole=null)=>admin.ListAsync(Uid,Role,resource,search,page,pageSize,null,subjectId,topicId,difficulty,isActive,accountRole);
 [HttpGet("{resource}/{id:int}")]public async Task<Dictionary<string,object?>> Get(string resource,int id){var row=(await admin.ListAsync(Uid,Role,resource,null,1,1,id)).Items.SingleOrDefault(x=>Convert.ToInt32(x["id"])==id);return row??throw new BusinessException("Không tìm thấy dữ liệu.",404);}
 [HttpPost("users/import")]public async Task<object> Import(ImportStudentsRequest request)=>new{imported=await admin.ImportStudentsAsync(Uid,Role,request.Csv,request.Preview)};
 [HttpGet("classes/{id:int}/members")]
 public Task<IReadOnlyList<ClassMemberDto>> ClassMembers(int id) => admin.ClassMembersAsync(Role, id);
 [HttpGet("classes/{id:int}/eligible-students")]
 public Task<IReadOnlyList<ClassMemberDto>> EligibleClassStudents(int id) => admin.EligibleClassStudentsAsync(Role, id);
 [HttpPost("classes/{id:int}/members")]
 public async Task<IActionResult> AddClassMember(int id, AddStudentToClassDto request)
 {
     await admin.ChangeClassMembershipAsync(Role, id, request.StudentId, add: true);
     return NoContent();
 }
 [HttpDelete("classes/{id:int}/members/{studentId:int}")]
 public async Task<IActionResult> RemoveClassMember(int id, int studentId)
 {
     await admin.ChangeClassMembershipAsync(Role, id, studentId, add: false);
     return NoContent();
 }
 [HttpPost("{resource}")]public async Task<object> Create(string resource,AdminSaveRequest request)=>new{id=await admin.SaveAsync(Uid,Role,resource,null,request)};
 [HttpPut("{resource}/{id:int}")]public async Task<object> Update(string resource,int id,AdminSaveRequest request)=>new{id=await admin.SaveAsync(Uid,Role,resource,id,request)};
 [HttpDelete("{resource}/{id:int}")]public async Task<IActionResult> Delete(string resource,int id){await admin.DeleteAsync(Uid,Role,resource,id);return NoContent();}
 [HttpPost("{resource}/{id:int}/action")]public async Task<IActionResult> Action(string resource,int id,ActionRequest request){await admin.ActionAsync(Uid,Role,resource,id,request.Action);return NoContent();}
 [HttpGet("sessions/{id:int}/export")]public async Task<IActionResult> Export(int id,string format="csv"){var xlsx=format.Equals("xlsx",StringComparison.OrdinalIgnoreCase);return File(await export.ExportAsync(Uid,Role,id,xlsx),xlsx?"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":"text/csv; charset=utf-8",$"ket-qua-{id}."+(xlsx?"xlsx":"csv"));}
}


