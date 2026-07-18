using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdmissionRequirement;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolAdmissionRequirement;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolAdmissionRequirement;
using Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdmissionRequirements;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolAdmissionRequirement;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdmissionRequirement;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolAdmissionRequirement;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolAdmissionRequirements;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/admission-requirements")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalAdmissionRequirementsController(
    ISender mediator,
    ILogger<SchoolPortalAdmissionRequirementsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>>> List(
        Guid schoolId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] int? kind,
        [FromQuery] int? publicationStatus,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(ListSchoolAdmissionRequirementsQuery.FromFilters(schoolId, branchId, educationalStageId, gradeId, academicYearId, kind, publicationStatus, isActive), cancellationToken));
    }

    [HttpGet("{requirementId:guid}")]
    public async Task<ActionResult<Result<SchoolAdmissionRequirementDetailDto>>> Get(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetSchoolAdmissionRequirementQuery(schoolId, requirementId),
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionRequirementDetailDto>>> Create(
        Guid schoolId,
        [FromBody] CreateSchoolAdmissionRequirementRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolAdmissionRequirementCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("{requirementId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionRequirementDetailDto>>> Update(
        Guid schoolId,
        Guid requirementId,
        [FromBody] UpdateSchoolAdmissionRequirementRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolAdmissionRequirementCommand(schoolId, requirementId, body),
            cancellationToken));
    }

    [HttpPost("reorder")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>>> Reorder(
        Guid schoolId,
        [FromBody] ReorderSchoolAdmissionRequirementsRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ReorderSchoolAdmissionRequirementsCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPost("{requirementId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionRequirementDetailDto>>> Publish(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishSchoolAdmissionRequirementCommand(schoolId, requirementId),
            cancellationToken));
    }

    [HttpPost("{requirementId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionRequirementDetailDto>>> Unpublish(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishSchoolAdmissionRequirementCommand(schoolId, requirementId),
            cancellationToken));
    }

    [HttpPost("{requirementId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionRequirementDetailDto>>> Deactivate(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolAdmissionRequirementCommand(schoolId, requirementId),
            cancellationToken));
    }
}
