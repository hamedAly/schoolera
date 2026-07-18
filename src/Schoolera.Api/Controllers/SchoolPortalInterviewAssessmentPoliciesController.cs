using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.CloneSchoolInterviewAssessmentPolicy;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolInterviewAssessmentPolicy;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolInterviewAssessmentPolicy;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolInterviewAssessmentPolicy;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolInterviewAssessmentPolicy;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolInterviewAssessmentPolicy;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolInterviewAssessmentPolicy;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolInterviewAssessmentPolicies;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolMeetingProviderOptions;
using Schoolera.Application.SchoolPortal.Queries.PreviewSchoolInterviewAssessmentPolicyApplicability;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/interview-assessment-policies")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalInterviewAssessmentPoliciesController(
    ISender mediator,
    ILogger<SchoolPortalInterviewAssessmentPoliciesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolInterviewAssessmentPolicyListItemDto>>>> List(
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
            ListSchoolInterviewAssessmentPoliciesQuery.FromFilters(
                schoolId, branchId, educationalStageId, gradeId, academicYearId, publicationStatus, isActive),
            cancellationToken));
    }

    [HttpGet("meeting-providers")]
    public async Task<ActionResult<Result<IReadOnlyList<SafeMeetingProviderOptionDto>>>> ListMeetingProviders(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ListSchoolMeetingProviderOptionsQuery(schoolId),
            cancellationToken));
    }

    [HttpGet("{policyId:guid}")]
    public async Task<ActionResult<Result<SchoolInterviewAssessmentPolicyDetailDto>>> Get(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetSchoolInterviewAssessmentPolicyQuery(schoolId, policyId),
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewAssessmentPolicyDetailDto>>> Create(
        Guid schoolId,
        [FromBody] CreateSchoolInterviewAssessmentPolicyRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolInterviewAssessmentPolicyCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("{policyId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewAssessmentPolicyDetailDto>>> Update(
        Guid schoolId,
        Guid policyId,
        [FromBody] UpdateSchoolInterviewAssessmentPolicyRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolInterviewAssessmentPolicyCommand(schoolId, policyId, body),
            cancellationToken));
    }

    [HttpPost("{policyId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewAssessmentPolicyDetailDto>>> Publish(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishSchoolInterviewAssessmentPolicyCommand(schoolId, policyId),
            cancellationToken));
    }

    [HttpPost("{policyId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewAssessmentPolicyDetailDto>>> Unpublish(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishSchoolInterviewAssessmentPolicyCommand(schoolId, policyId),
            cancellationToken));
    }

    [HttpPost("{policyId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewAssessmentPolicyDetailDto>>> Deactivate(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolInterviewAssessmentPolicyCommand(schoolId, policyId),
            cancellationToken));
    }

    [HttpPost("{policyId:guid}/clone")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewAssessmentPolicyDetailDto>>> Clone(
        Guid schoolId,
        Guid policyId,
        [FromBody] CloneSchoolInterviewAssessmentPolicyRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CloneSchoolInterviewAssessmentPolicyCommand(schoolId, policyId, body),
            cancellationToken));
    }

    [HttpPost("preview-applicability")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto>>> PreviewApplicability(
        Guid schoolId,
        [FromBody] PreviewSchoolInterviewAssessmentPolicyApplicabilityRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PreviewSchoolInterviewAssessmentPolicyApplicabilityQuery(schoolId, body),
            cancellationToken));
    }
}
