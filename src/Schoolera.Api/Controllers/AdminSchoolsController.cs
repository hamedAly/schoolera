using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admin.Commands.UpdateSchoolStatus;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Admin.Queries.GetSchoolDetail;
using Schoolera.Application.Admin.Queries.ListSchools;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/schools")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminSchoolsController(
    ISender mediator,
    ILogger<AdminSchoolsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AdminSchoolListItemDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return FromResult(await Mediator.Send(
            new ListAdminSchoolsQuery(search, status, pageNumber, pageSize),
            cancellationToken));
    }

    [HttpGet("{schoolId:guid}")]
    public async Task<ActionResult<Result<AdminSchoolDetailDto>>> Detail(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetAdminSchoolDetailQuery(schoolId),
            cancellationToken));
    }

    [HttpPost("{schoolId:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdminSchoolDetailDto>>> UpdateStatus(
        Guid schoolId,
        [FromBody] UpdateAdminSchoolStatusRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateAdminSchoolStatusCommand(schoolId, body.Status),
            cancellationToken));
    }
}
