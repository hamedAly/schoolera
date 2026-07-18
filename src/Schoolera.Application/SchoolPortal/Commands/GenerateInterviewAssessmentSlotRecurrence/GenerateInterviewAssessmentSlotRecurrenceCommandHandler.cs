using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.GenerateInterviewAssessmentSlotRecurrence;

public sealed record GenerateInterviewAssessmentSlotRecurrenceCommand(
    Guid SchoolId, SlotRecurrenceRequest Body) : IRequest<Result<SlotGenerationResultDto>>;

public sealed class GenerateInterviewAssessmentSlotRecurrenceCommandHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<GenerateInterviewAssessmentSlotRecurrenceCommandHandler> logger)
    : IRequestHandler<GenerateInterviewAssessmentSlotRecurrenceCommand, Result<SlotGenerationResultDto>>
{
    public async Task<Result<SlotGenerationResultDto>> Handle(
        GenerateInterviewAssessmentSlotRecurrenceCommand request, CancellationToken cancellationToken)
    {
        _ = unitOfWork;
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<SlotGenerationResultDto>(
            resolved, request.Body.SchoolBranchId, localizer, out var access);
        if (denied is not null || access is null)
            return Result<SlotGenerationResultDto>.Failure(denied!.Errors, denied.ErrorCodes);
        if (!await repository.BranchScopeIsValidAsync(request.SchoolId, request.Body.SchoolBranchId,
                request.Body.EducationalStageId, request.Body.GradeId,
                request.Body.AcademicYearId, cancellationToken))
            return InterviewAssessmentSlotSupport.Fail<SlotGenerationResultDto>(
                localizer, "SlotInvalidScope", SchoolPortalErrorCodes.SlotInvalidScope);
        if (request.Body.ResourceReferenceId is { } resourceId &&
            !await repository.ResourceIsValidAsync(
                request.SchoolId, request.Body.SchoolBranchId, resourceId, cancellationToken))
            return InterviewAssessmentSlotSupport.Fail<SlotGenerationResultDto>(
                localizer, "SlotResourceInvalid", SchoolPortalErrorCodes.SlotResourceInvalid);
        var fingerprint = InterviewAssessmentSlotSupport.Fingerprint(request.Body);
        var (occurrences, error) = InterviewAssessmentSlotSupport.Expand(request.Body);
        if (occurrences is null)
            return InterviewAssessmentSlotSupport.Fail<SlotGenerationResultDto>(
                localizer, "SlotRecurrenceLimit", error!);
        InterviewAssessmentSlot[] slots = [];
        var requestKey = request.Body.RequestKey.Trim();
        var outcome = await repository.ExecuteAtomicGenerationAsync(
            request.SchoolId, requestKey, fingerprint, request.Body.ResourceKind,
            request.Body.ResourceReferenceId, occurrences,
            async token =>
            {
                var batch = new InterviewAssessmentSlotGenerationBatch(
                    request.SchoolId, requestKey, fingerprint, access.UserId);
                await repository.AddBatchAsync(batch, token);
                slots = occurrences.Select(x => new InterviewAssessmentSlot(
                    request.SchoolId, request.Body.SchoolBranchId, request.Body.EducationalStageId,
                    request.Body.GradeId, request.Body.AcademicYearId, request.Body.Kind,
                    request.Body.DeliveryMode, x.Start, x.End, request.Body.TimeZoneId,
                    request.Body.Capacity, request.Body.ResourceKind,
                    request.Body.ResourceReferenceId, request.Body.InstructionsAr,
                    request.Body.InstructionsEn, request.Body.MeetingProviderCode,
                    batch.BatchReference, access.UserId)).ToArray();
                foreach (var slot in slots)
                {
                    await repository.AddAsync(slot, token);
                    await repository.AddAuditAsync(new(request.SchoolId, slot.Id, "Generated",
                        access.UserId, $"batch={batch.BatchReference}"), token);
                }
                return batch;
            }, cancellationToken);
        if (outcome.Result == AtomicSlotGenerationResult.IdempotencyConflict)
            return InterviewAssessmentSlotSupport.Fail<SlotGenerationResultDto>(
                localizer, "SlotIdempotencyConflict", SchoolPortalErrorCodes.SlotIdempotencyConflict);
        if (outcome.Result == AtomicSlotGenerationResult.ResourceConflict)
            return InterviewAssessmentSlotSupport.Fail<SlotGenerationResultDto>(
                localizer, "SlotRecurrenceConflict", SchoolPortalErrorCodes.SlotRecurrenceConflict);
        if (outcome.Result == AtomicSlotGenerationResult.ConcurrencyConflict ||
            outcome.BatchReference is null)
            return InterviewAssessmentSlotSupport.Fail<SlotGenerationResultDto>(
                localizer, "ConcurrentUpdate", SchoolPortalErrorCodes.ConcurrentUpdate);
        if (outcome.Result == AtomicSlotGenerationResult.Existing)
            slots = (await repository.ListBatchAsync(
                request.SchoolId, outcome.BatchReference, cancellationToken)).ToArray();
        var mapped = new List<InterviewAssessmentSlotDto>();
        foreach (var slot in slots)
            mapped.Add(await InterviewAssessmentSlotSupport.MapAsync(
                slot, repository, cancellationToken));
        logger.LogInformation(
            "Generated interview assessment slot recurrence batch {BatchReference}.",
            outcome.BatchReference);
        return Result<SlotGenerationResultDto>.Success(new(
            outcome.BatchReference, outcome.Result == AtomicSlotGenerationResult.Existing, mapped));
    }
}
