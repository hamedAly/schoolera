using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Queries.ListParentAvailableSlots;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Api.Controllers;

[Route("api/parent/admission-applications/{applicationId:guid}/available-slots")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentAvailableAdmissionSlotsController(
    ISender mediator, ILogger<ParentAvailableAdmissionSlotsController> logger)
    : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<ParentAvailableSlotDto>>>> List(
        Guid applicationId, [FromQuery] int kind, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            ListParentAvailableSlotsQuery.FromFilter(applicationId, kind), ct));
}
