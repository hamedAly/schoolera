using System.Globalization;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Application.Meetings;

namespace Schoolera.Application.Admissions.Common;

public static class AdmissionLifecycleMapping
{
    public static AdmissionMissingItemsRequestDto? ToActiveMissingRequest(AdmissionApplication application)
    {
        var request = application.MissingItemsRequests?
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.RequestedAtUtc)
            .FirstOrDefault();
        return request is null ? null : ToMissingRequest(request);
    }

    public static AdmissionMissingItemsRequestDto ToMissingRequest(AdmissionMissingItemsRequest request)
    {
        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        return new AdmissionMissingItemsRequestDto(
            request.Id,
            request.ParentVisibleReason,
            request.Instructions,
            request.ResponseDeadlineUtc,
            request.RequestedAtUtc,
            request.ClearedAtUtc,
            (request.Items ?? [])
                .OrderBy(item => item.LabelEn)
                .Select(item => new AdmissionMissingItemDto(
                    item.Id,
                    item.Kind,
                    item.IsMandatory,
                    item.IsCompleted,
                    isArabic ? item.LabelAr : item.LabelEn,
                    item.RequirementSnapshotId,
                    item.QuestionSnapshotId,
                    item.ParentSnapshotField,
                    item.ChildSnapshotField))
                .ToArray());
    }

    public static AdmissionAppointmentDto? ToActiveInterview(
        AdmissionApplication application, MeetingSessionSummaryDto? meeting = null) =>
        ToAppointment(
            application.InterviewAppointments?
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault(item => item.IsActiveReservation)
            ?? application.InterviewAppointments?
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault(),
            application,
            meeting);

    public static AdmissionAppointmentDto? ToActiveAssessment(
        AdmissionApplication application, MeetingSessionSummaryDto? meeting = null) =>
        ToAppointment(
            application.AssessmentAppointments?
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault(item => item.IsActiveReservation)
            ?? application.AssessmentAppointments?
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault(),
            application,
            meeting);

    public static AdmissionAppointmentDto? ToAppointment(
        AdmissionInterviewAppointment? appointment, AdmissionApplication application,
        MeetingSessionSummaryDto? meeting = null) =>
        appointment is null
            ? null
            : new AdmissionAppointmentDto(
                appointment.Id,
                appointment.InterviewAssessmentSlotId,
                appointment.ScheduledAtUtc,
                appointment.TimeZoneId,
                appointment.Mode,
                appointment.Location,
                appointment.OnlineInstructions,
                appointment.ParentVisibleNotes,
                appointment.PreparationInstructions,
                appointment.Lifecycle,
                appointment.CreatedAtUtc,
                appointment.ProposedAtUtc,
                appointment.ConfirmedAtUtc,
                appointment.RescheduleRequestedAtUtc,
                appointment.NoShowAtUtc,
                appointment.CompletedAtUtc,
                appointment.CancelledAtUtc,
                appointment.OutcomeNotes,
                appointment.ParentRescheduleAttemptCount,
                appointment.LastParentVisibleRescheduleReason,
                appointment.CancellationReason,
                appointment.RescheduleInitiator,
                appointment.RowVersion,
                Capabilities(application, SlotKind.Interview, appointment.Lifecycle, appointment.Mode,
                    appointment.ScheduledAtUtc, appointment.ParentRescheduleAttemptCount,
                    appointment.RescheduleInitiator, meeting),
                OnSite(application, appointment.Mode, appointment.ScheduledAtUtc,
                    appointment.TimeZoneId, appointment.ParentVisibleNotes,
                    appointment.PreparationInstructions),
                appointment.Mode == AdmissionAppointmentMode.Online ? meeting : null);

    public static AdmissionAppointmentDto? ToAppointment(
        AdmissionAssessmentAppointment? appointment, AdmissionApplication application,
        MeetingSessionSummaryDto? meeting = null) =>
        appointment is null
            ? null
            : new AdmissionAppointmentDto(
                appointment.Id,
                appointment.InterviewAssessmentSlotId,
                appointment.ScheduledAtUtc,
                appointment.TimeZoneId,
                appointment.Mode,
                appointment.Location,
                appointment.OnlineInstructions,
                appointment.ParentVisibleNotes,
                appointment.PreparationInstructions,
                appointment.Lifecycle,
                appointment.CreatedAtUtc,
                appointment.ProposedAtUtc,
                appointment.ConfirmedAtUtc,
                appointment.RescheduleRequestedAtUtc,
                appointment.NoShowAtUtc,
                appointment.CompletedAtUtc,
                appointment.CancelledAtUtc,
                appointment.OutcomeNotes,
                appointment.ParentRescheduleAttemptCount,
                appointment.LastParentVisibleRescheduleReason,
                appointment.CancellationReason,
                appointment.RescheduleInitiator,
                appointment.RowVersion,
                Capabilities(application, SlotKind.Assessment, appointment.Lifecycle, appointment.Mode,
                    appointment.ScheduledAtUtc, appointment.ParentRescheduleAttemptCount,
                    appointment.RescheduleInitiator, meeting),
                OnSite(application, appointment.Mode, appointment.ScheduledAtUtc,
                    appointment.TimeZoneId, appointment.ParentVisibleNotes,
                    appointment.PreparationInstructions),
                appointment.Mode == AdmissionAppointmentMode.Online ? meeting : null);

    private static AppointmentCapabilitiesDto Capabilities(
        AdmissionApplication application,
        SlotKind kind,
        AdmissionAppointmentLifecycle lifecycle,
        AdmissionAppointmentMode mode,
        DateTimeOffset scheduledAtUtc,
        int attempts,
        AppointmentRescheduleInitiator? initiator,
        MeetingSessionSummaryDto? meeting)
    {
        var policy = application.PolicySnapshot;
        var required = kind == SlotKind.Interview
            ? application.Status == AdmissionApplicationStatus.InterviewRequired
            : application.Status == AdmissionApplicationStatus.AssessmentRequired;
        var remaining = Math.Max(0, (policy?.MaxParentRescheduleAttempts ?? 0) - attempts);
        DateTimeOffset? deadline = policy is null
            ? null
            : scheduledAtUtc.AddHours(-(policy.MinimumSchedulingLeadTimeHours ?? 0));
        var now = DateTimeOffset.UtcNow;
        var beforeStart = now < scheduledAtUtc;
        var beforeDeadline = beforeStart && (!deadline.HasValue || now <= deadline);
        var recovery = lifecycle == AdmissionAppointmentLifecycle.RescheduleRequested &&
            initiator == AppointmentRescheduleInitiator.School;
        var parentCanReschedule = policy?.ParentReschedulingAllowed == true && remaining > 0;
        var actionable = lifecycle is AdmissionAppointmentLifecycle.Proposed or
            AdmissionAppointmentLifecycle.Confirmed or
            AdmissionAppointmentLifecycle.RescheduleRequested;
        var canSelect = required && actionable && (recovery ||
            parentCanReschedule && lifecycle is AdmissionAppointmentLifecycle.Proposed or
                AdmissionAppointmentLifecycle.Confirmed or
                AdmissionAppointmentLifecycle.RescheduleRequested) &&
            beforeStart && (recovery || beforeDeadline);
        var canCancel = required && policy?.ParentCancellationAllowed == true &&
            actionable &&
            (lifecycle is AdmissionAppointmentLifecycle.Proposed or
                AdmissionAppointmentLifecycle.Confirmed or
                AdmissionAppointmentLifecycle.RescheduleRequested) &&
            beforeDeadline;
        DateTimeOffset? joinStart = mode == AdmissionAppointmentMode.Online
            ? scheduledAtUtc.AddMinutes(-15) : null;
        DateTimeOffset? joinEnd = mode == AdmissionAppointmentMode.Online
            ? scheduledAtUtc.AddMinutes(policy?.ExpectedDurationMinutes ?? 0) : null;

        return new AppointmentCapabilitiesDto(
            required && beforeStart && lifecycle == AdmissionAppointmentLifecycle.Proposed,
            canSelect,
            required && parentCanReschedule && beforeDeadline &&
                lifecycle is AdmissionAppointmentLifecycle.Proposed or AdmissionAppointmentLifecycle.Confirmed,
            canCancel,
            meeting?.CanParentJoin == true,
            canSelect,
            remaining,
            deadline,
            deadline,
            joinStart,
            joinEnd,
            meeting?.JoinUnavailableReasonCode ??
                (mode == AdmissionAppointmentMode.Online ? "meeting.notProvisioned" : null),
            $"/parent/support-tickets/new?admissionApplicationId={application.Id}");
    }

    private static OnSiteAttendanceDetailsDto? OnSite(
        AdmissionApplication application,
        AdmissionAppointmentMode mode,
        DateTimeOffset scheduledAtUtc,
        string timeZoneId,
        string? attendanceInstructions,
        string? preparationInstructions)
    {
        if (mode != AdmissionAppointmentMode.InPerson) return null;
        var branch = application.SchoolBranch;
        return new(
            branch.NameAr,
            branch.NameEn,
            branch.AddressLineAr ?? branch.AddressReference,
            branch.AddressLineEn ?? branch.AddressReference,
            branch.Landmark,
            attendanceInstructions,
            preparationInstructions,
            application.PolicySnapshot?.RequiredParticipants,
            scheduledAtUtc,
            timeZoneId,
            null,
            branch.IsActive,
            !branch.IsActive);
    }

    public static AdmissionWaitingListDto? ToWaitingList(AdmissionApplication application)
    {
        if (application.Status != AdmissionApplicationStatus.WaitingList &&
            application.WaitingListEnteredAtUtc is null)
        {
            return null;
        }

        return new AdmissionWaitingListDto(
            application.WaitingListReason,
            application.WaitingListPosition,
            application.WaitingListReviewDate,
            application.WaitingListEnteredAtUtc);
    }

    public static bool TryValidateTimeZone(string? timeZoneId, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            normalized = timeZoneId.Trim();
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
