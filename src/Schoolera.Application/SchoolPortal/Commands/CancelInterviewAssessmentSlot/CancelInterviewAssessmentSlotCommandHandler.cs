using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.Meetings;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.CancelInterviewAssessmentSlot;

public sealed record CancelInterviewAssessmentSlotCommand(
    Guid SchoolId, Guid SlotId, CancelInterviewAssessmentSlotRequest Body)
    : IRequest<Result<InterviewAssessmentSlotDto>>;

public sealed class CancelInterviewAssessmentSlotCommandHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IAdmissionApplicationRepository admissions,
    INotificationOutboxPublisher notifications,
    IParentAccountService accounts,
    IMeetingSessionService meetingSessions,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CancelInterviewAssessmentSlotCommandHandler> logger)
    : IRequestHandler<CancelInterviewAssessmentSlotCommand, Result<InterviewAssessmentSlotDto>>
{
    public async Task<Result<InterviewAssessmentSlotDto>> Handle(
        CancelInterviewAssessmentSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetAsync(request.SchoolId, request.SlotId, true, cancellationToken);
        if (slot is null)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotNotFound", SchoolPortalErrorCodes.SlotNotFound);
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<InterviewAssessmentSlotDto>(
            resolved, slot.SchoolBranchId, localizer, out var access);
        if (denied is not null || access is null)
            return Result<InterviewAssessmentSlotDto>.Failure(denied!.Errors, denied.ErrorCodes);
        if (InterviewAssessmentSlotSupport.RowVersionMismatch(request.Body.RowVersion, slot.RowVersion))
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "ConcurrentUpdate", SchoolPortalErrorCodes.ConcurrentUpdate);
        if (slot.Status == SlotStatus.Cancelled)
            return Result<InterviewAssessmentSlotDto>.Success(
                await InterviewAssessmentSlotSupport.MapAsync(slot, repository, cancellationToken));
        if (string.IsNullOrWhiteSpace(request.Body.CancellationReasonAr) ||
            string.IsNullOrWhiteSpace(request.Body.CancellationReasonEn))
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "RequiredField", SchoolPortalErrorCodes.SlotHasReservations);
        var applications = await repository.GetApplicationsForCancellationAsync(
            slot.Id, slot.Kind, cancellationToken);
        var affectedAppointmentCount = 0;
        slot.Cancel(request.Body.CancellationReasonAr, request.Body.CancellationReasonEn, access.UserId);
        foreach (var application in applications)
        {
            if (slot.Kind == SlotKind.Interview)
                foreach (var appointment in application.InterviewAppointments.Where(
                             x => x.InterviewAssessmentSlotId == slot.Id && x.IsActiveReservation))
                {
                    affectedAppointmentCount++;
                    var previous = appointment.Lifecycle;
                    appointment.RequestReschedule(
                        request.Body.CancellationReasonAr, AppointmentRescheduleInitiator.School);
                    await meetingSessions.HandleAppointmentUnavailableAsync(
                        appointment.Id, SlotKind.Interview, access.UserId,
                        $"slot:{slot.Id:N}:appointment:{appointment.Id:N}:cancelled",
                        cancellationToken);
                    var appointmentAction = new AdmissionAppointmentActionHistory(
                        application.Id, appointment.Id, SlotKind.Interview,
                        AdmissionAppointmentAction.RescheduleRequiredBySchool,
                        previous, appointment.Lifecycle, slot.Id, slot.Id,
                        AdmissionAppointmentActorType.School, access.UserId,
                        request.Body.CancellationReasonAr, null);
                    await repository.AddAppointmentActionAsync(appointmentAction, cancellationToken);
                    admissions.AddHistory(new(application.Id, application.Status, application.Status,
                        AdmissionHistoryActions.InterviewRescheduleRequired, access.UserId,
                        access.HistoryActorRole, true, request.Body.CancellationReasonAr, null));
                    await Enqueue(application, appointment.Id, appointmentAction.Id,
                        NotificationEventType.InterviewRescheduleRequired, cancellationToken);
                }
            else
                foreach (var appointment in application.AssessmentAppointments.Where(
                             x => x.InterviewAssessmentSlotId == slot.Id && x.IsActiveReservation))
                {
                    affectedAppointmentCount++;
                    var previous = appointment.Lifecycle;
                    appointment.RequestReschedule(
                        request.Body.CancellationReasonAr, AppointmentRescheduleInitiator.School);
                    await meetingSessions.HandleAppointmentUnavailableAsync(
                        appointment.Id, SlotKind.Assessment, access.UserId,
                        $"slot:{slot.Id:N}:appointment:{appointment.Id:N}:cancelled",
                        cancellationToken);
                    var appointmentAction = new AdmissionAppointmentActionHistory(
                        application.Id, appointment.Id, SlotKind.Assessment,
                        AdmissionAppointmentAction.RescheduleRequiredBySchool,
                        previous, appointment.Lifecycle, slot.Id, slot.Id,
                        AdmissionAppointmentActorType.School, access.UserId,
                        request.Body.CancellationReasonAr, null);
                    await repository.AddAppointmentActionAsync(appointmentAction, cancellationToken);
                    admissions.AddHistory(new(application.Id, application.Status, application.Status,
                        AdmissionHistoryActions.AssessmentRescheduleRequired, access.UserId,
                        access.HistoryActorRole, true, request.Body.CancellationReasonAr, null));
                    await Enqueue(application, appointment.Id, appointmentAction.Id,
                        NotificationEventType.AssessmentRescheduleRequired, cancellationToken);
                }
        }
        await repository.AddAuditAsync(new(request.SchoolId, slot.Id, "Cancelled", access.UserId,
            $"affectedAppointments={affectedAppointmentCount}"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cancelled interview assessment slot {SlotId}.", slot.Id);
        return Result<InterviewAssessmentSlotDto>.Success(
            await InterviewAssessmentSlotSupport.MapAsync(slot, repository, cancellationToken));

        async Task Enqueue(
            AdmissionApplication application, Guid appointmentId, Guid actionHistoryId,
            NotificationEventType type, CancellationToken token)
        {
            var account = await accounts.GetAsync(application.ParentUserId, token);
            await notifications.EnqueueAsync(new(application.ParentUserId, type,
                account?.PreferredLanguage?.StartsWith("en") == true ? "en" : "ar",
                $"slot:{slot.Id}:appointment:{appointmentId}:action:{actionHistoryId}:reschedule-required",
                new Dictionary<string, string>
                {
                    ["applicationNumber"] = application.ApplicationNumber,
                    ["status"] = application.Status.ToString()
                },
                $"/parent/applications/{application.Id}", application.SchoolId, application.Id), token);
        }
    }
}
