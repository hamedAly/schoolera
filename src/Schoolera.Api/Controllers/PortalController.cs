using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/portal")]
public sealed class PortalController : ControllerBase
{
    [HttpGet("parent")]
    [Authorize(Policy = SchooleraPolicies.ParentOnly)]
    public Result<string> Parent() => Result<string>.Success("parent");

    [HttpGet("school")]
    [Authorize(Policy = SchooleraPolicies.SchoolPortal)]
    public Result<string> School() => Result<string>.Success("school");

    [HttpGet("admin")]
    [Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
    public Result<string> Admin() => Result<string>.Success("admin");

    [HttpGet("support")]
    [Authorize(Policy = SchooleraPolicies.SupportOrAdmin)]
    public Result<string> Support() => Result<string>.Success("support");
}
