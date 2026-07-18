using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.ActivateSchoolInterviewFaq;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolInterviewFaq;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolInterviewFaq;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolInterviewFaq;
using Schoolera.Application.SchoolPortal.Commands.ReorderSchoolInterviewFaqs;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolInterviewFaq;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolInterviewFaq;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolInterviewFaq;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolInterviewFaqs;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/interview-faqs")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalInterviewFaqsController(
    ISender mediator,
    ILogger<SchoolPortalInterviewFaqsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>>> List(
        Guid schoolId,
        [FromQuery] int? interviewCategory,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] bool? isPublished,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(ListSchoolInterviewFaqsQuery.FromFilters(
            schoolId, interviewCategory, branchId, educationalStageId, gradeId,
            academicYearId, isPublished, isActive), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<SchoolInterviewFaqDetailDto>>> Get(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetSchoolInterviewFaqQuery(schoolId, id),
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewFaqDetailDto>>> Create(
        Guid schoolId,
        [FromBody] CreateSchoolInterviewFaqRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolInterviewFaqCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewFaqDetailDto>>> Update(
        Guid schoolId,
        Guid id,
        [FromBody] UpdateSchoolInterviewFaqRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolInterviewFaqCommand(schoolId, id, body),
            cancellationToken));
    }

    [HttpPost("reorder")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>>> Reorder(
        Guid schoolId,
        [FromBody] ReorderSchoolInterviewFaqsRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ReorderSchoolInterviewFaqsCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewFaqDetailDto>>> Publish(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishSchoolInterviewFaqCommand(schoolId, id),
            cancellationToken));
    }

    [HttpPost("{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewFaqDetailDto>>> Unpublish(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishSchoolInterviewFaqCommand(schoolId, id),
            cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewFaqDetailDto>>> Activate(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateSchoolInterviewFaqCommand(schoolId, id),
            cancellationToken));
    }

    [HttpPost("{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolInterviewFaqDetailDto>>> Deactivate(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolInterviewFaqCommand(schoolId, id),
            cancellationToken));
    }
}
