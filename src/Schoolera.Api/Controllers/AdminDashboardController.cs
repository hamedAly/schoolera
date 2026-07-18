using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Admin.Queries.GetDashboard;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/dashboard")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminDashboardController(
    ISender mediator,
    ILogger<AdminDashboardController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<AdminDashboardDto>>> Get(CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetAdminDashboardQuery(), cancellationToken));
    }
}
