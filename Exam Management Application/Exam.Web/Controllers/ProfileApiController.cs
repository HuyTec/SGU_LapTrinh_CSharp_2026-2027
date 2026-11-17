using System.Security.Claims;
using Exam.Core;
using Exam.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.Web.Controllers;

[ApiController, Route("api/profile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")]
public sealed class ProfileApiController(ProfileService profiles) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public Task<ProfileDto> Get() => profiles.GetAsync(UserId);
    [HttpPut]
    public Task<ProfileDto> Update(UpdateProfileRequest request) => profiles.UpdateAsync(UserId, request);
}
