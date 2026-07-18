using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public static class ParentAppointmentCommandSupport
{
    public static async Task<Result<AdmissionAppointmentDto>> ExecuteAsync(
        ICurrentUser currentUser,
        IInterviewAssessmentSlotRepository repository,
        IStringLocalizer<AdmissionMessages> localizer,
        Guid applicationId,
        SlotKind kind,
        ParentAppointmentActionKind action,
        Guid? slotId,
        byte[] rowVersion,
        string? idempotencyKey,
        string? reason,
        CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Failure(localizer, ParentAppointmentActionResult.NotFound);
        var outcome = await repository.ExecuteParentAppointmentActionAsync(
            new(userId, applicationId, kind, action, slotId, rowVersion, idempotencyKey, reason), ct);
        if (outcome.Result is not (ParentAppointmentActionResult.Succeeded or
            ParentAppointmentActionResult.Existing) || outcome.Application is null)
            return Failure(localizer, outcome.Result);
        var appointment = kind == SlotKind.Interview
            ? AdmissionLifecycleMapping.ToActiveInterview(outcome.Application)
            : AdmissionLifecycleMapping.ToActiveAssessment(outcome.Application);
        return appointment is null
            ? Failure(localizer, ParentAppointmentActionResult.NotFound)
            : Result<AdmissionAppointmentDto>.Success(appointment);
    }

    private static Result<AdmissionAppointmentDto> Failure(
        IStringLocalizer<AdmissionMessages> localizer,
        ParentAppointmentActionResult result)
    {
        var (resource, code) = result switch
        {
            ParentAppointmentActionResult.NotFound => ("AppointmentNotFound", AdmissionErrorCodes.AppointmentJourneyNotFound),
            ParentAppointmentActionResult.InvalidTransition => ("AppointmentInvalidTransition", AdmissionErrorCodes.AppointmentInvalidTransition),
            ParentAppointmentActionResult.PolicyMismatch => ("AppointmentPolicyMismatch", AdmissionErrorCodes.AppointmentPolicyMismatch),
            ParentAppointmentActionResult.RescheduleNotAllowed => ("AppointmentRescheduleNotAllowed", AdmissionErrorCodes.AppointmentRescheduleNotAllowed),
            ParentAppointmentActionResult.RescheduleLimitReached => ("AppointmentRescheduleLimitReached", AdmissionErrorCodes.AppointmentRescheduleLimitReached),
            ParentAppointmentActionResult.DeadlinePassed => ("AppointmentDeadlinePassed", AdmissionErrorCodes.AppointmentDeadlinePassed),
            ParentAppointmentActionResult.CancellationNotAllowed => ("AppointmentCancellationNotAllowed", AdmissionErrorCodes.AppointmentCancellationNotAllowed),
            ParentAppointmentActionResult.SlotUnavailable => ("AppointmentSlotUnavailable", AdmissionErrorCodes.AppointmentSlotUnavailable),
            ParentAppointmentActionResult.SlotFull => ("AppointmentSlotFull", AdmissionErrorCodes.AppointmentSlotFull),
            ParentAppointmentActionResult.ConcurrencyConflict => ("AppointmentConcurrencyConflict", AdmissionErrorCodes.AppointmentConcurrencyConflict),
            ParentAppointmentActionResult.IdempotencyConflict => ("AppointmentIdempotencyConflict", AdmissionErrorCodes.AppointmentIdempotencyConflict),
            ParentAppointmentActionResult.JoinTooEarly => ("AppointmentJoinTooEarly", AdmissionErrorCodes.AppointmentJoinTooEarly),
            ParentAppointmentActionResult.JoinExpired => ("AppointmentJoinExpired", AdmissionErrorCodes.AppointmentJoinExpired),
            _ => ("AppointmentJoinProviderUnavailable", AdmissionErrorCodes.AppointmentJoinProviderUnavailable),
        };
        return Result<AdmissionAppointmentDto>.Failure([localizer[resource]], [code]);
    }
}
