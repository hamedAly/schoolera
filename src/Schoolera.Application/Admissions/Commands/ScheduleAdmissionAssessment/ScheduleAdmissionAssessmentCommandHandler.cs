using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.ScheduleAdmissionAssessment;

public sealed record ScheduleAdmissionAssessmentCommand(
    Guid SchoolId,
    Guid ApplicationId,
    ScheduleAdmissionAppointmentRequest Body,
    bool IsReschedule)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class ScheduleAdmissionAssessmentCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IParentAccountService parentAccountService,
    ISchoolPortalRepository schoolPortalRepository,
    IInterviewAssessmentSlotRepository slotRepository,
    ILogger<ScheduleAdmissionAssessmentCommandHandler> logger)
    : IRequestHandler<ScheduleAdmissionAssessmentCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        ScheduleAdmissionAssessmentCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolAdmissionApplicationDetailDto>.Failure(
                accessResult.Errors,
                accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        if (!access.IsEditable ||
            !access.HasPermission(SchoolPortalPermission.ManageApplicationReview))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "School review access denied.", AdmissionErrorCodes.ReviewSchoolAccessDenied);
        }
        var application = await admissionRepository.GetForSchoolForUpdateAsync(
            request.SchoolId, request.ApplicationId, cancellationToken);
        if (application is null)
        {
            return AdmissionResults.ReviewNotFound<SchoolAdmissionApplicationDetailDto>();
        }

        if (!access.CanAccessBranch(application.SchoolBranchId))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "School branch access denied.", AdmissionErrorCodes.ReviewSchoolAccessDenied);
        }

        InterviewAssessmentSlot? selectedSlot = null;
        if (request.Body.SlotId is { } slotId)
        {
            if (string.IsNullOrWhiteSpace(request.Body.IdempotencyKey) ||
                request.Body.IdempotencyKey.Length > 128)
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "A valid idempotency key is required for slot proposals.",
                    AdmissionErrorCodes.AppointmentIdempotencyConflict);
            selectedSlot = await slotRepository.GetAsync(request.SchoolId, slotId, false, cancellationToken);
            if (selectedSlot is null || selectedSlot.Status != SlotStatus.Open ||
                selectedSlot.Kind != SlotKind.Assessment ||
                selectedSlot.SchoolBranchId != application.SchoolBranchId ||
                selectedSlot.EducationalStageId != application.EducationalStageId ||
                selectedSlot.GradeId != application.GradeId ||
                selectedSlot.AcademicYearId != application.AcademicYearId)
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Selected slot does not match the application.", AdmissionErrorCodes.SlotMismatch);
            request = request with { Body = request.Body with {
                ScheduledAtUtc = selectedSlot.StartAtUtc, TimeZoneId = selectedSlot.TimeZoneId,
                Mode = selectedSlot.DeliveryMode == SlotDeliveryMode.Online
                    ? AdmissionAppointmentMode.Online : AdmissionAppointmentMode.InPerson,
                Location = selectedSlot.DeliveryMode == SlotDeliveryMode.OnSite
                    ? selectedSlot.InstructionsAr : null,
                OnlineInstructions = selectedSlot.DeliveryMode == SlotDeliveryMode.Online
                    ? selectedSlot.InstructionsAr : null,
                PreparationInstructions = selectedSlot.InstructionsAr } };
        }
        var requestedProposalAction = request.IsReschedule
            ? AdmissionAppointmentAction.ProposalReplaced
            : AdmissionAppointmentAction.Proposed;
        if (selectedSlot is not null)
        {
            var idempotency = await slotRepository.CheckSchoolProposalIdempotencyAsync(
                application.Id, SlotKind.Assessment, requestedProposalAction, selectedSlot.Id,
                request.Body.IdempotencyKey!.Trim(), cancellationToken);
            if (idempotency == SlotAssignmentResult.IdempotencyConflict)
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "The idempotency key was already used for another appointment action.",
                    AdmissionErrorCodes.AppointmentIdempotencyConflict);
            if (idempotency == SlotAssignmentResult.Existing)
            {
                var retryUsers = await userDirectory.GetUsersAsync(
                    [application.ParentUserId], cancellationToken);
                retryUsers.TryGetValue(application.ParentUserId, out var retryParent);
                return Result<SchoolAdmissionApplicationDetailDto>.Success(
                    SchoolAdmissionMapping.ToSchoolDetail(application,
                        SchoolAdmissionMapping.ToParentContact(retryParent), identityProtector));
            }
        }

        if (AdmissionResults.HasRowVersionMismatch(request.Body.RowVersion, application.RowVersion))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ReviewConcurrentUpdate);
        }

        if (!AdmissionLifecycleMapping.TryValidateTimeZone(request.Body.TimeZoneId, out var timeZone) ||
            request.Body.ScheduledAtUtc <= DateTimeOffset.UtcNow.AddMinutes(-5) ||
            !Enum.IsDefined(request.Body.Mode))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Assessment schedule is invalid.",
                AdmissionErrorCodes.AppointmentInvalid);
        }

        if (request.Body.Mode == AdmissionAppointmentMode.InPerson &&
            string.IsNullOrWhiteSpace(request.Body.Location))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Location is required for in-person assessments.",
                AdmissionErrorCodes.AppointmentInvalid);
        }

        if (request.Body.Mode == AdmissionAppointmentMode.Online &&
            string.IsNullOrWhiteSpace(request.Body.OnlineInstructions))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Online instructions are required for online assessments.",
                AdmissionErrorCodes.AppointmentInvalid);
        }

        var fromStatus = application.Status;
        var actorRole = access.HistoryActorRole;
        var internalNote = string.IsNullOrWhiteSpace(request.Body.InternalReviewNote)
            ? null
            : request.Body.InternalReviewNote.Trim();
        string historyAction;
        AdmissionAssessmentAppointment? existingAppointment = null;
        var isNoShowReplacement = false;

        if (request.IsReschedule)
        {
            if (!AdmissionTransitionPolicy.CanSchoolRescheduleAssessment(application.Status))
            {
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Invalid status transition.",
                    AdmissionErrorCodes.ReviewInvalidTransition);
            }

            existingAppointment = application.AssessmentAppointments?
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault(item => item.Lifecycle is AdmissionAppointmentLifecycle.Proposed or
                    AdmissionAppointmentLifecycle.RescheduleRequested);
            if (existingAppointment is null)
            {
                var noShow = application.AssessmentAppointments?
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .FirstOrDefault(item => item.Lifecycle == AdmissionAppointmentLifecycle.NoShow);
                var policy = application.PolicySnapshot;
                isNoShowReplacement = noShow is not null &&
                    policy?.ParentReschedulingAllowed == true &&
                    noShow.ParentRescheduleAttemptCount < policy.MaxParentRescheduleAttempts;
                if (!isNoShowReplacement)
                    return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                        "No reschedulable assessment found.",
                        AdmissionErrorCodes.AppointmentNotFound);
            }

            historyAction = AdmissionHistoryActions.AssessmentRescheduled;
        }
        else
        {
            if (!AdmissionTransitionPolicy.TryValidateSchoolTransition(
                    application.Status,
                    AdmissionApplicationStatus.AssessmentRequired,
                    out var transitionCode))
            {
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Invalid status transition.",
                    transitionCode);
            }

            var active = application.AssessmentAppointments?.FirstOrDefault(item => item.IsActiveReservation);
            if (active is not null)
            {
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "An assessment is already scheduled.",
                    AdmissionErrorCodes.AppointmentInvalid);
            }

            historyAction = AdmissionHistoryActions.AssessmentScheduled;
        }

        async Task MutateAsync(CancellationToken token)
        {
            AdmissionAssessmentAppointment changedAppointment;
            var previousLifecycle = existingAppointment?.Lifecycle ??
                AdmissionAppointmentLifecycle.Proposed;
            var oldSlotId = existingAppointment?.InterviewAssessmentSlotId;
            if (existingAppointment is not null)
            {
                existingAppointment.Propose(
                    request.Body.ScheduledAtUtc, timeZone, request.Body.Mode,
                    request.Body.Location, request.Body.OnlineInstructions,
                    request.Body.ParentVisibleNotes, request.Body.PreparationInstructions,
                    selectedSlot?.Id, access.UserId);
                changedAppointment = existingAppointment;
            }
            else
            {
                if (application.Status != AdmissionApplicationStatus.AssessmentRequired)
                    application.MoveToAssessmentRequired();
                var appointment = new AdmissionAssessmentAppointment(
                    application.Id, request.Body.ScheduledAtUtc, timeZone, request.Body.Mode,
                    request.Body.Location, request.Body.OnlineInstructions,
                    request.Body.ParentVisibleNotes, request.Body.PreparationInstructions,
                    access.UserId);
                if (selectedSlot is not null) appointment.LinkToSlot(selectedSlot.Id);
                application.AddAssessmentAppointment(appointment);
                changedAppointment = appointment;
            }
            var appointmentAction = new AdmissionAppointmentActionHistory(
                application.Id, changedAppointment.Id, SlotKind.Assessment,
                existingAppointment is null ? AdmissionAppointmentAction.Proposed
                    : AdmissionAppointmentAction.ProposalReplaced,
                previousLifecycle, changedAppointment.Lifecycle, oldSlotId,
                changedAppointment.InterviewAssessmentSlotId,
                AdmissionAppointmentActorType.School, access.UserId,
                request.Body.ParentVisibleNotes, request.Body.IdempotencyKey);
            await slotRepository.AddAppointmentActionAsync(appointmentAction, token);
            admissionRepository.AddHistory(new AdmissionApplicationHistory(
                application.Id, fromStatus, application.Status, historyAction, access.UserId,
                actorRole, parentVisible: true,
                parentVisibleNote: request.Body.ParentVisibleNotes?.Trim(), internalNote: internalNote));
            await AdmissionParentNotificationSupport.EnqueueAsync(
                notificationOutboxPublisher, parentAccountService, schoolPortalRepository,
                application,
                request.IsReschedule ? NotificationEventType.AssessmentRescheduled
                    : NotificationEventType.AssessmentScheduled,
                $"{(request.IsReschedule ? "assessment-rescheduled" : "assessment-scheduled")}:{appointmentAction.Id:N}",
                token);
        }

        if (selectedSlot is not null)
        {
            var assignment = await slotRepository.ExecuteAtomicSchoolProposalAsync(
                request.SchoolId, selectedSlot.Id, selectedSlot.Capacity,
                existingAppointment?.Id, application.Id, SlotKind.Assessment, requestedProposalAction,
                request.Body.IdempotencyKey!.Trim(), MutateAsync, cancellationToken);
            if (assignment == SlotAssignmentResult.IdempotencyConflict)
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "The idempotency key was already used for another appointment action.",
                    AdmissionErrorCodes.AppointmentIdempotencyConflict);
            if (assignment == SlotAssignmentResult.Full)
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Selected slot is full.", AdmissionErrorCodes.SlotFull);
            if (assignment == SlotAssignmentResult.Unavailable)
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Selected slot is unavailable.", AdmissionErrorCodes.SlotMismatch);
            if (assignment is not (SlotAssignmentResult.Succeeded or SlotAssignmentResult.Existing))
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "The application was modified by another operation.",
                    AdmissionErrorCodes.ReviewConcurrentUpdate);
        }
        else
        {
            await MutateAsync(cancellationToken);
            var conflict = await AdmissionResults.TrySaveAsync<SchoolAdmissionApplicationDetailDto>(
                unitOfWork, cancellationToken);
            if (conflict is not null)
                return AdmissionResults.RemapSchoolSaveConflict(conflict);
        }

        var loaded = await admissionRepository.GetForSchoolAsync(
            request.SchoolId, application.Id, cancellationToken) ?? application;
        var users = await userDirectory.GetUsersAsync([loaded.ParentUserId], cancellationToken);
        users.TryGetValue(loaded.ParentUserId, out var parentUser);

        logger.LogInformation("{Action} assessment for application {ApplicationId}.", historyAction, loaded.Id);

        return Result<SchoolAdmissionApplicationDetailDto>.Success(
            SchoolAdmissionMapping.ToSchoolDetail(
                loaded,
                SchoolAdmissionMapping.ToParentContact(parentUser),
                identityProtector));
    }
}
