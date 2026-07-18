using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Admissions.Queries.GetAdminAdmissionApplication;
using Schoolera.Application.Admissions.Queries.ListAdminAdmissionApplications;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/admission-applications")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminAdmissionApplicationsController(
    ISender mediator,
    ILogger<AdminAdmissionApplicationsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AdminAdmissionApplicationListItemDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? schoolId,
        [FromQuery] Guid? cityId,
        [FromQuery] int? status,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] string? sort,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return FromResult(await Mediator.Send(
            ListAdminAdmissionApplicationsQuery.FromFilters(
                search, schoolId, cityId, status, branchId, gradeId, academicYearId,
                dateFrom, dateTo, sort, pageNumber, pageSize),
            cancellationToken));
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<Result<AdminAdmissionApplicationDetailDto>>> Get(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetAdminAdmissionApplicationQuery(applicationId),
            cancellationToken));
    }
}
