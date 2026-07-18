using Microsoft.Extensions.Localization;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.Meetings;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.CancelAdmissionAssessment;

public sealed record CancelAdmissionAssessmentCommand(
    Guid SchoolId, Guid ApplicationId, CancelAdmissionAppointmentRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class CancelAdmissionAssessmentCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IParentAccountService parentAccountService,
    ISchoolPortalRepository schoolPortalRepository,
    IInterviewAssessmentSlotRepository slotRepository,
    IMeetingSessionService meetingSessions,
    ILogger<CancelAdmissionAssessmentCommandHandler> logger,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<CancelAdmissionAssessmentCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        CancelAdmissionAssessmentCommand request,
        CancellationToken cancellationToken)
    {
        var begin = await SchoolLifecycleCommandSupport.BeginAsync(
            portalAccess,
            admissionRepository,
            localizer,
            request.SchoolId,
            request.ApplicationId,
            request.Body.RowVersion, cancellationToken);
        if (begin.EarlyResult is not null)
        {
            return begin.EarlyResult;
        }

        var access = begin.Access!;
        var application = begin.Application!;

        if (!AdmissionTransitionPolicy.CanSchoolCancelAssessmentAppointment(application.Status))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid status transition.",
                AdmissionErrorCodes.ReviewInvalidTransition);
        }

        var appointment = application.AssessmentAppointments?
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault(item => item.Lifecycle is AdmissionAppointmentLifecycle.Proposed or
                AdmissionAppointmentLifecycle.Confirmed or AdmissionAppointmentLifecycle.RescheduleRequested);
        if (appointment is null)
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "No scheduled assessment found.",
                AdmissionErrorCodes.AppointmentNotFound);
        }

        var from = application.Status;
        var previousLifecycle = appointment.Lifecycle;
        appointment.CancelBySchool(request.Body.InternalReviewNote);
        await meetingSessions.HandleAppointmentUnavailableAsync(
            appointment.Id, SlotKind.Assessment, access.UserId,
            $"school-cancel:{appointment.Id:N}", cancellationToken);
        var appointmentAction = new AdmissionAppointmentActionHistory(
            application.Id, appointment.Id, SlotKind.Assessment,
            AdmissionAppointmentAction.Cancelled, previousLifecycle, appointment.Lifecycle,
            appointment.InterviewAssessmentSlotId, appointment.InterviewAssessmentSlotId,
            AdmissionAppointmentActorType.School, access.UserId, null, null);
        await slotRepository.AddAppointmentActionAsync(appointmentAction, cancellationToken);
        application.ReturnToUnderReview();
        var role = access.HistoryActorRole;
        admissionRepository.AddHistory(new AdmissionApplicationHistory(
            application.Id, from, application.Status, AdmissionHistoryActions.AssessmentCancelled,
            access.UserId, role, parentVisible: true, parentVisibleNote: null,
            internalNote: request.Body.InternalReviewNote?.Trim()));

        await AdmissionParentNotificationSupport.EnqueueAsync(
            notificationOutboxPublisher,
            parentAccountService,
            schoolPortalRepository,
            application,
            NotificationEventType.AssessmentCancelled,
            $"assessment-cancelled:{appointmentAction.Id:N}",
            cancellationToken);

        logger.LogInformation("Cancelled assessment appointment for application {ApplicationId}.", application.Id);
        return await SchoolLifecycleCommandSupport.FinishAsync(
            admissionRepository, userDirectory, unitOfWork, identityProtector,
            request.SchoolId, application, cancellationToken);
    }
}
