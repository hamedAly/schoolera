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

namespace Schoolera.Application.SchoolPortal.Commands.UpdateInterviewAssessmentSlot;

public sealed record UpdateInterviewAssessmentSlotCommand(
    Guid SchoolId, Guid SlotId, UpsertInterviewAssessmentSlotRequest Body)
    : IRequest<Result<InterviewAssessmentSlotDto>>;

public sealed class UpdateInterviewAssessmentSlotCommandHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateInterviewAssessmentSlotCommandHandler> logger)
    : IRequestHandler<UpdateInterviewAssessmentSlotCommand, Result<InterviewAssessmentSlotDto>>
{
    public async Task<Result<InterviewAssessmentSlotDto>> Handle(
        UpdateInterviewAssessmentSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetAsync(request.SchoolId, request.SlotId, true, cancellationToken);
        if (slot is null)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotNotFound", SchoolPortalErrorCodes.SlotNotFound);
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<InterviewAssessmentSlotDto>(
            resolved, request.Body.SchoolBranchId, localizer, out var access);
        if (denied is not null || access is null)
            return Result<InterviewAssessmentSlotDto>.Failure(denied!.Errors, denied.ErrorCodes);
        if (InterviewAssessmentSlotSupport.RowVersionMismatch(request.Body.RowVersion, slot.RowVersion))
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "ConcurrentUpdate", SchoolPortalErrorCodes.ConcurrentUpdate);
        if (await repository.CountActiveAppointmentsAsync(slot.Id, cancellationToken) > 0)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotHasReservations", SchoolPortalErrorCodes.SlotHasReservations);
        if (request.Body.LocalEndTime <= request.Body.LocalStartTime)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidTime", SchoolPortalErrorCodes.SlotInvalidTime);
        if (!InterviewAssessmentSlotSupport.TryLocalToUtc(
                request.Body.LocalDate, request.Body.LocalStartTime, request.Body.TimeZoneId,
                out var startAtUtc, out var conversionError) ||
            !InterviewAssessmentSlotSupport.TryLocalToUtc(
                request.Body.LocalDate, request.Body.LocalEndTime, request.Body.TimeZoneId,
                out var endAtUtc, out conversionError))
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidTime", conversionError!);
        try
        {
            slot.UpdateDraftOrUnbooked(request.Body.SchoolBranchId, request.Body.EducationalStageId,
                request.Body.GradeId, request.Body.AcademicYearId, request.Body.Kind,
                request.Body.DeliveryMode, startAtUtc, endAtUtc, request.Body.TimeZoneId,
                request.Body.Capacity, request.Body.ResourceKind, request.Body.ResourceReferenceId,
                request.Body.InstructionsAr, request.Body.InstructionsEn,
                request.Body.MeetingProviderCode, access.UserId);
        }
        catch
        {
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidTransition", SchoolPortalErrorCodes.SlotInvalidTransition);
        }
        await repository.AddAuditAsync(
            new(request.SchoolId, slot.Id, "Updated", access.UserId, null), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated interview assessment slot {SlotId}.", slot.Id);
        return Result<InterviewAssessmentSlotDto>.Success(
            await InterviewAssessmentSlotSupport.MapAsync(slot, repository, cancellationToken));
    }
}
