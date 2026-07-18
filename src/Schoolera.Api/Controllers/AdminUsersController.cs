using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admin.Commands.UpdateUserStatus;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Admin.Queries.ListUsers;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/users")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminUsersController(
    ISender mediator,
    ILogger<AdminUsersController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AdminUserListItemDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? accountStatus,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return FromResult(await Mediator.Send(
            new ListAdminUsersQuery(search, role, accountStatus, pageNumber, pageSize),
            cancellationToken));
    }

    [HttpPost("{userId:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdminUserListItemDto>>> UpdateStatus(
        Guid userId,
        [FromBody] UpdateAdminUserStatusRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateAdminUserStatusCommand(userId, body.AccountStatus),
            cancellationToken));
    }
}
