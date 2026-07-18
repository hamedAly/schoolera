using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Admin.Queries.ListAuditEvents;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/audit")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminAuditController(
    ISender mediator,
    ILogger<AdminAuditController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AdminAuditEventDto>>>> List(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return FromResult(await Mediator.Send(
            new ListAdminAuditEventsQuery(action, entityType, pageNumber, pageSize),
            cancellationToken));
    }
}
