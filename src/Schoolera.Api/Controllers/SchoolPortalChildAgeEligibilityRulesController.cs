using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.CloneSchoolChildAgeEligibilityRule;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolChildAgeEligibilityRule;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolChildAgeEligibilityRule;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolChildAgeEligibilityRule;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolChildAgeEligibilityRule;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolChildAgeEligibilityRule;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolChildAgeEligibilityRule;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolChildAgeEligibilityRules;
using Schoolera.Application.SchoolPortal.Queries.PreviewSchoolChildAgeEligibility;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/age-eligibility-rules")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalChildAgeEligibilityRulesController(
    ISender mediator,
    ILogger<SchoolPortalChildAgeEligibilityRulesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolChildAgeEligibilityRuleListItemDto>>>> List(
        Guid schoolId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] int? publicationStatus,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            ListSchoolChildAgeEligibilityRulesQuery.FromFilters(
                schoolId, branchId, educationalStageId, gradeId, academicYearId, publicationStatus, isActive),
            cancellationToken));
    }

    [HttpGet("{ruleId:guid}")]
    public async Task<ActionResult<Result<SchoolChildAgeEligibilityRuleDetailDto>>> Get(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetSchoolChildAgeEligibilityRuleQuery(schoolId, ruleId),
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolChildAgeEligibilityRuleDetailDto>>> Create(
        Guid schoolId,
        [FromBody] CreateSchoolChildAgeEligibilityRuleRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolChildAgeEligibilityRuleCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("{ruleId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolChildAgeEligibilityRuleDetailDto>>> Update(
        Guid schoolId,
        Guid ruleId,
        [FromBody] UpdateSchoolChildAgeEligibilityRuleRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolChildAgeEligibilityRuleCommand(schoolId, ruleId, body),
            cancellationToken));
    }

    [HttpPost("{ruleId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolChildAgeEligibilityRuleDetailDto>>> Publish(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishSchoolChildAgeEligibilityRuleCommand(schoolId, ruleId),
            cancellationToken));
    }

    [HttpPost("{ruleId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolChildAgeEligibilityRuleDetailDto>>> Unpublish(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishSchoolChildAgeEligibilityRuleCommand(schoolId, ruleId),
            cancellationToken));
    }

    [HttpPost("{ruleId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolChildAgeEligibilityRuleDetailDto>>> Deactivate(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolChildAgeEligibilityRuleCommand(schoolId, ruleId),
            cancellationToken));
    }

    [HttpPost("{ruleId:guid}/clone")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolChildAgeEligibilityRuleDetailDto>>> Clone(
        Guid schoolId,
        Guid ruleId,
        [FromBody] CloneSchoolChildAgeEligibilityRuleRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CloneSchoolChildAgeEligibilityRuleCommand(schoolId, ruleId, body),
            cancellationToken));
    }

    [HttpPost("preview")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<PreviewSchoolChildAgeEligibilityDto>>> Preview(
        Guid schoolId,
        [FromBody] PreviewSchoolChildAgeEligibilityRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PreviewSchoolChildAgeEligibilityQuery(schoolId, body),
            cancellationToken));
    }
}
