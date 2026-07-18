using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Commands.CancelAdmissionApplication;
using Schoolera.Application.Admissions.Commands.CreateAdmissionApplication;
using Schoolera.Application.Admissions.Commands.DeleteAdmissionAttachment;
using Schoolera.Application.Admissions.Commands.DeleteAdmissionAnswer;
using Schoolera.Application.Admissions.Commands.EnsureAdmissionQuestionSnapshots;
using Schoolera.Application.Admissions.Commands.CorrectMissingSnapshotField;
using Schoolera.Application.Admissions.Commands.ResubmitMissingDocuments;
using Schoolera.Application.Admissions.Commands.SubmitAdmissionApplication;
using Schoolera.Application.Admissions.Commands.UpdateAdmissionApplication;
using Schoolera.Application.Admissions.Commands.UploadAdmissionAttachment;
using Schoolera.Application.Admissions.Commands.UpsertAdmissionAnswer;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Admissions.Queries.GetAdmissionApplication;
using Schoolera.Application.Admissions.Queries.CheckParentAgeEligibility;
using Schoolera.Application.Admissions.Queries.GetApplicationQuestions;
using Schoolera.Application.Admissions.Queries.ListAdmissionApplications;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Commands.CopyChildDocumentToAdmissionDraft;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Couriers;
using Schoolera.Application.Couriers.Queries.GetParentCourierAvailability;

namespace Schoolera.Api.Controllers;

[Route("api/parent/admission-applications")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentAdmissionApplicationsController(
    ISender mediator,
    ILogger<ParentAdmissionApplicationsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AdmissionApplicationListItemDto>>>> List(
        [FromQuery] int? status,
        [FromQuery] Guid? childProfileId,
        [FromQuery] Guid? schoolId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return FromResult(await Mediator.Send(ListAdmissionApplicationsQuery.FromFilters(status, childProfileId, schoolId, academicYearId, search, sort, pageNumber, pageSize), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> Create(
        [FromBody] CreateAdmissionApplicationRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateAdmissionApplicationCommand(body),
            cancellationToken));
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> Get(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetAdmissionApplicationQuery(applicationId),
            cancellationToken));
    }

    [HttpGet("{applicationId:guid}/courier-availability")]
    public async Task<ActionResult<Result<ParentCourierAvailabilityDto>>> CourierAvailability(
        Guid applicationId,
        [FromQuery] Guid countryId,
        [FromQuery] Guid? governorateId,
        [FromQuery] Guid? cityId,
        [FromQuery] Guid? districtId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetParentCourierAvailabilityQuery(
                applicationId, countryId, governorateId, cityId, districtId),
            cancellationToken));
    }

    [HttpPut("{applicationId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> Update(
        Guid applicationId,
        [FromBody] UpdateAdmissionApplicationRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateAdmissionApplicationCommand(applicationId, body),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SubmitAdmissionOutcomeDto>>> Submit(
        Guid applicationId,
        [FromBody] SubmitAdmissionApplicationRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new SubmitAdmissionApplicationCommand(
                applicationId,
                body ?? new SubmitAdmissionApplicationRequest(false, false)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> Cancel(
        Guid applicationId,
        [FromBody] CancelAdmissionApplicationRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CancelAdmissionApplicationCommand(
                applicationId,
                body ?? new CancelAdmissionApplicationRequest(null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/question-snapshots/ensure")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> EnsureQuestionSnapshots(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new EnsureAdmissionQuestionSnapshotsCommand(applicationId), cancellationToken));
    }

    [HttpGet("{applicationId:guid}/questions")]
    public async Task<ActionResult<Result<AdmissionApplicationQuestionsDto>>> GetQuestions(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetApplicationQuestionsQuery(applicationId), cancellationToken));
    }

    [HttpPut("{applicationId:guid}/answers")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> UpsertAnswer(
        Guid applicationId,
        [FromBody] UpsertAdmissionAnswerRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new UpsertAdmissionAnswerCommand(applicationId, body), cancellationToken));
    }

    [HttpDelete("{applicationId:guid}/answers/{questionSnapshotId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> DeleteAnswer(
        Guid applicationId,
        Guid questionSnapshotId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new DeleteAdmissionAnswerCommand(applicationId, questionSnapshotId), cancellationToken));
    }

    [HttpPost("{applicationId:guid}/resubmit-missing-documents")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> ResubmitMissingDocuments(
        Guid applicationId,
        [FromBody] ResubmitMissingDocumentsRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ResubmitMissingDocumentsCommand(
                applicationId,
                body ?? new ResubmitMissingDocumentsRequest(null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/correct-missing-field")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> CorrectMissingField(
        Guid applicationId,
        [FromBody] CorrectMissingSnapshotFieldRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CorrectMissingSnapshotFieldCommand(applicationId, body),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/attachments")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> UploadAttachment(
        Guid applicationId,
        IFormFile? file,
        [FromForm] int attachmentType = 1,
        [FromForm] Guid? requirementSnapshotId = null,
        [FromForm] Guid? questionSnapshotId = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file?.OpenReadStream() ?? Stream.Null;
        return FromResult(await Mediator.Send(UploadAdmissionAttachmentCommand.FromRaw(
            applicationId, attachmentType, stream, file?.FileName ?? string.Empty,
            file?.ContentType ?? string.Empty, file?.Length ?? 0,
            requirementSnapshotId, questionSnapshotId), cancellationToken));
    }

    [HttpPost("{applicationId:guid}/attachments/from-vault")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> CopyAttachmentFromVault(
        Guid applicationId,
        [FromBody] CopyChildDocumentToAdmissionRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CopyChildDocumentToAdmissionDraftCommand(
                applicationId,
                body?.ChildDocumentId ?? Guid.Empty,
                body?.RequirementSnapshotId),
            cancellationToken));
    }

    [HttpDelete("{applicationId:guid}/attachments/{attachmentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionApplicationDetailDto>>> DeleteAttachment(
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeleteAdmissionAttachmentCommand(applicationId, attachmentId),
            cancellationToken));
    }
}
