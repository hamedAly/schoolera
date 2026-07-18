using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdmissionQuestion;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolAdmissionQuestion;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolAdmissionQuestion;
using Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdmissionQuestions;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolAdmissionQuestion;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdmissionQuestion;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolAdmissionQuestion;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolAdmissionQuestions;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/admission-questions")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalAdmissionQuestionsController(
    ISender mediator,
    ILogger<SchoolPortalAdmissionQuestionsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>>> List(
        Guid schoolId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] int? questionType,
        [FromQuery] int? publicationStatus,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(ListSchoolAdmissionQuestionsQuery.FromFilters(schoolId, branchId, educationalStageId, gradeId, academicYearId, questionType, publicationStatus, isActive), cancellationToken));
    }

    [HttpGet("{questionId:guid}")]
    public async Task<ActionResult<Result<SchoolAdmissionQuestionDetailDto>>> Get(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetSchoolAdmissionQuestionQuery(schoolId, questionId), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionQuestionDetailDto>>> Create(
        Guid schoolId,
        [FromBody] CreateSchoolAdmissionQuestionRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new CreateSchoolAdmissionQuestionCommand(schoolId, body), cancellationToken));
    }

    [HttpPut("{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionQuestionDetailDto>>> Update(
        Guid schoolId,
        Guid questionId,
        [FromBody] UpdateSchoolAdmissionQuestionRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new UpdateSchoolAdmissionQuestionCommand(schoolId, questionId, body), cancellationToken));
    }

    [HttpPost("reorder")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>>> Reorder(
        Guid schoolId,
        [FromBody] ReorderSchoolAdmissionQuestionsRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new ReorderSchoolAdmissionQuestionsCommand(schoolId, body), cancellationToken));
    }

    [HttpPost("{questionId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionQuestionDetailDto>>> Publish(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new PublishSchoolAdmissionQuestionCommand(schoolId, questionId), cancellationToken));
    }

    [HttpPost("{questionId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionQuestionDetailDto>>> Unpublish(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new UnpublishSchoolAdmissionQuestionCommand(schoolId, questionId), cancellationToken));
    }

    [HttpPost("{questionId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionQuestionDetailDto>>> Deactivate(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new DeactivateSchoolAdmissionQuestionCommand(schoolId, questionId), cancellationToken));
    }
}
