using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.Schools.Commands.SubmitSchoolContactLead;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Queries.GetPublicAdmissionRequirements;
using Schoolera.Application.Schools.Queries.GetPublicInterviewAssessmentPolicySummary;
using Schoolera.Application.Schools.Queries.GetPublicAgeEligibility;
using Schoolera.Application.Schools.Queries.GetPublicInterviewFaqs;
using Schoolera.Application.Schools.Queries.GetPublicMapConfiguration;
using Schoolera.Application.Schools.Queries.GetPublicSchoolBySlug;
using Schoolera.Application.Schools.Queries.GetPublicSchoolMapPins;
using Schoolera.Application.Schools.Queries.GetPublicSchools;
using Schoolera.Application.Schools.Queries.GetRelatedPublicSchools;

namespace Schoolera.Api.Controllers;

[Route("api/schools")]
public sealed class SchoolsController(
    ISender mediator,
    ILogger<SchoolsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<Result<PagedResult<PublicSchoolListItemDto>>> GetSchools(
        [FromQuery] PublicSchoolsQueryParams query,
        CancellationToken cancellationToken = default)
    {
        return Success(await Mediator.Send(query.ToQuery(), cancellationToken));
    }

    /// <summary>Public-safe Map client configuration from the active Map platform integration.</summary>
    [HttpGet("map-configuration")]
    public async Task<ActionResult<Result<PublicMapConfigurationDto>>> GetMapConfiguration(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetPublicMapConfigurationQuery(), cancellationToken));
    }

    /// <summary>Compact Branch map pins for the current viewport or radius (same business filters as List).</summary>
    [HttpGet("map-pins")]
    [EnableRateLimiting("schools-map-pins")]
    public async Task<ActionResult<Result<PublicSchoolMapPinsResultDto>>> GetMapPins(
        [FromQuery] PublicSchoolMapPinsQueryParams query,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(query.ToQuery(), cancellationToken));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Result<PublicSchoolProfileDto>>> GetSchoolBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetPublicSchoolBySlugQuery(slug), cancellationToken));
    }

    [HttpGet("{slug}/related")]
    public async Task<ActionResult<Result<IReadOnlyList<PublicSchoolListItemDto>>>> GetRelatedSchools(
        string slug,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetRelatedPublicSchoolsQuery(slug, limit), cancellationToken));
    }

    [HttpGet("{slug}/admission-requirements")]
    public async Task<ActionResult<Result<IReadOnlyList<PublicAdmissionRequirementSummaryDto>>>> GetAdmissionRequirements(
        string slug,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetPublicAdmissionRequirementsQuery(
                slug, branchId, educationalStageId, gradeId, academicYearId),
            cancellationToken));
    }

    [HttpGet("{slug}/interview-assessment-policy")]
    public async Task<ActionResult<Result<SafeInterviewAssessmentPolicySummaryDto?>>> GetInterviewAssessmentPolicy(
        string slug,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetPublicInterviewAssessmentPolicySummaryQuery(
                slug, branchId, educationalStageId, gradeId, academicYearId),
            cancellationToken));
    }

    [HttpGet("{slug}/age-eligibility")]
    public async Task<ActionResult<Result<AgeEligibilityResultDto?>>> GetAgeEligibility(
        string slug,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid academicYearId,
        [FromQuery] Guid? childProfileId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetPublicAgeEligibilityQuery(
                slug, branchId, educationalStageId, gradeId, academicYearId, childProfileId),
            cancellationToken));
    }

    [HttpGet("{slug}/interview-faqs")]
    public async Task<ActionResult<Result<IReadOnlyList<PublicInterviewFaqItemDto>>>> GetInterviewFaqs(
        string slug,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] int? category,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            GetPublicInterviewFaqsQuery.FromFilters(
                slug, branchId, educationalStageId, gradeId, academicYearId, category),
            cancellationToken));
    }

    [HttpPost("{slug}/contact-leads")]
    [EnableRateLimiting("schools-contact-lead")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolContactLeadResultDto>>> SubmitContactLead(
        string slug,
        [FromBody] SchoolContactLeadRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new SubmitSchoolContactLeadCommand(slug, body), cancellationToken));
    }
}
