using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Common;

internal static class InterviewAssessmentSlotSupport
{
    public static Result<T> Fail<T>(IStringLocalizer<SchoolPortalMessages> localizer, string key, string code) =>
        Result<T>.Failure([localizer[key].Value], [code]);

    public static bool RowVersionMismatch(byte[]? supplied, byte[] current) =>
        supplied is { Length: > 0 } && !supplied.SequenceEqual(current);

    public static Result<SchoolPortalAccessContext>? Authorize<T>(
        Result<SchoolPortalAccessContext> resolved, Guid branchId,
        IStringLocalizer<SchoolPortalMessages> localizer, out SchoolPortalAccessContext? access)
    {
        access = resolved.Data;
        if (!resolved.Succeeded || access is null) return resolved;
        var permission = SchoolPortalAccess.RequireEditablePermission<T>(
            access, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permission.Succeeded)
            return Result<SchoolPortalAccessContext>.Failure(permission.Errors, permission.ErrorCodes);
        var branch = SchoolPortalAccess.RequireBranch<T>(access, branchId, localizer);
        return branch.Succeeded ? null :
            Result<SchoolPortalAccessContext>.Failure(branch.Errors, branch.ErrorCodes);
    }

    public static bool TryLocalToUtc(DateOnly date, TimeOnly time, string zoneId,
        out DateTimeOffset utc, out string? errorCode)
    {
        utc = default;
        errorCode = null;
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
        catch { errorCode = SchoolPortalErrorCodes.SlotInvalidTimezone; return false; }
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local)) { errorCode = SchoolPortalErrorCodes.SlotInvalidTime; return false; }
        if (zone.IsAmbiguousTime(local)) { errorCode = SchoolPortalErrorCodes.SlotAmbiguousTime; return false; }
        utc = TimeZoneInfo.ConvertTimeToUtc(local, zone);
        return true;
    }

    public static async Task<string?> ValidateOpenAsync(
        InterviewAssessmentSlot slot,
        IInterviewAssessmentSlotRepository repository,
        ISchoolInterviewAssessmentPolicyRepository policyRepository,
        CancellationToken cancellationToken)
    {
        try { TimeZoneInfo.FindSystemTimeZoneById(slot.TimeZoneId); }
        catch { return SchoolPortalErrorCodes.SlotInvalidTimezone; }
        if (slot.StartAtUtc <= DateTimeOffset.UtcNow || slot.Capacity is < 1 or > 100 ||
            string.IsNullOrWhiteSpace(slot.InstructionsAr) || string.IsNullOrWhiteSpace(slot.InstructionsEn))
            return SchoolPortalErrorCodes.SlotPolicyMismatch;
        if (!await repository.BranchScopeIsValidAsync(slot.SchoolId, slot.SchoolBranchId,
                slot.EducationalStageId, slot.GradeId, slot.AcademicYearId, cancellationToken))
            return SchoolPortalErrorCodes.SlotInvalidScope;
        if (slot.ResourceReferenceId is { } resource &&
            !await repository.ResourceIsValidAsync(slot.SchoolId, slot.SchoolBranchId, resource, cancellationToken))
            return SchoolPortalErrorCodes.SlotResourceInvalid;

        var policies = await policyRepository.ListPublishedActiveAsync(slot.SchoolId, cancellationToken);
        var policy = InterviewAssessmentPolicyCatalog.ResolveApplicable(policies, slot.SchoolBranchId,
            slot.EducationalStageId, slot.GradeId ?? Guid.Empty, slot.AcademicYearId);
        if (policy is null) return SchoolPortalErrorCodes.SlotPolicyMismatch;
        var supportsKind = slot.Kind == SlotKind.Interview
            ? policy.RequirementMode is InterviewAssessmentRequirementMode.InterviewOnly or InterviewAssessmentRequirementMode.InterviewAndAssessment
            : policy.RequirementMode is InterviewAssessmentRequirementMode.AssessmentOnly or InterviewAssessmentRequirementMode.InterviewAndAssessment;
        var supportsMode = slot.DeliveryMode == SlotDeliveryMode.Online
            ? policy.DeliveryMode is InterviewAssessmentDeliveryMode.Online or InterviewAssessmentDeliveryMode.Hybrid
            : policy.DeliveryMode is InterviewAssessmentDeliveryMode.OnSite or InterviewAssessmentDeliveryMode.Hybrid;
        if (!supportsKind || !supportsMode ||
            (int)(slot.EndAtUtc - slot.StartAtUtc).TotalMinutes != policy.ExpectedDurationMinutes)
            return SchoolPortalErrorCodes.SlotPolicyMismatch;
        if (slot.StartAtUtc < DateTimeOffset.UtcNow.AddHours(policy.MinimumSchedulingLeadTimeHours ?? 0))
            return SchoolPortalErrorCodes.SlotPolicyMismatch;
        var year = await repository.GetAcademicYearAsync(slot.AcademicYearId, cancellationToken);
        if (year is null) return SchoolPortalErrorCodes.SlotInvalidScope;
        var yearStart = new DateTimeOffset(year.StartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        if (policy.BookingWindowOpensDaysBefore is { } opens && slot.StartAtUtc < yearStart.AddDays(-opens))
            return SchoolPortalErrorCodes.SlotPolicyMismatch;
        if (policy.BookingWindowClosesDaysBefore is { } closes && slot.StartAtUtc > yearStart.AddDays(-closes))
            return SchoolPortalErrorCodes.SlotPolicyMismatch;
        if (slot.DeliveryMode == SlotDeliveryMode.Online)
        {
            if (!string.Equals(slot.MeetingProviderCode, policy.MeetingProviderCode, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(slot.MeetingProviderCode) ||
                !await repository.MeetingProviderIsActiveAsync(slot.MeetingProviderCode, cancellationToken))
                return SchoolPortalErrorCodes.SlotMeetingUnavailable;
        }
        else
        {
            var branch = await repository.GetBranchAsync(slot.SchoolId, slot.SchoolBranchId, cancellationToken);
            if (branch is null || !branch.IsActive ||
                (string.IsNullOrWhiteSpace(branch.AddressLineAr) && string.IsNullOrWhiteSpace(branch.AddressReference)))
                return SchoolPortalErrorCodes.SlotInvalidScope;
        }
        return null;
    }

    public static async Task<InterviewAssessmentSlotDto> MapAsync(
        InterviewAssessmentSlot slot,
        IInterviewAssessmentSlotRepository repository,
        CancellationToken cancellationToken)
    {
        var active = await repository.CountActiveAppointmentsAsync(slot.Id, cancellationToken);
        var affected = await repository.CountAffectedApplicationsAsync(slot.Id, cancellationToken);
        return new(slot.Id, slot.SchoolId, slot.SchoolBranchId, slot.EducationalStageId, slot.GradeId,
            slot.AcademicYearId, slot.Kind, slot.DeliveryMode, slot.StartAtUtc, slot.EndAtUtc,
            slot.TimeZoneId, slot.Capacity, active, affected, slot.ResourceKind, slot.ResourceReferenceId,
            slot.InstructionsAr, slot.InstructionsEn, slot.Status, slot.CancellationReasonAr,
            slot.CancellationReasonEn, slot.MeetingProviderCode, slot.GenerationBatchReference,
            slot.RowVersion, new(
                slot.Status is SlotStatus.Draft or SlotStatus.Closed && active == 0,
                slot.Status is SlotStatus.Draft or SlotStatus.Closed,
                slot.Status == SlotStatus.Open, slot.Status == SlotStatus.Closed,
                slot.Status != SlotStatus.Cancelled));
    }

    public static (IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)>? Occurrences, string? Error)
        Expand(SlotRecurrenceRequest body)
    {
        if (body.LocalEndDate < body.LocalStartDate ||
            body.LocalEndDate.DayNumber - body.LocalStartDate.DayNumber > 180 ||
            body.DurationMinutes is < 1 or > 1440 || string.IsNullOrWhiteSpace(body.RequestKey) ||
            body.RequestKey.Trim().Length > 128 || !Enum.IsDefined(body.Frequency))
            return (null, SchoolPortalErrorCodes.SlotRecurrenceLimit);
        var selected = (body.SelectedWeekdays ?? []).Distinct().ToHashSet();
        if (body.Frequency == SlotRecurrenceFrequency.Weekly && selected.Count == 0)
            return (null, SchoolPortalErrorCodes.SlotRecurrenceLimit);
        var rows = new List<(DateTimeOffset, DateTimeOffset)>();
        for (var date = body.LocalStartDate; date <= body.LocalEndDate; date = date.AddDays(1))
        {
            if (body.Frequency == SlotRecurrenceFrequency.Weekly && !selected.Contains(date.DayOfWeek)) continue;
            if (!TryLocalToUtc(date, body.LocalStartTime, body.TimeZoneId, out var start, out var error))
                return (null, error);
            var localEnd = date.ToDateTime(body.LocalStartTime).AddMinutes(body.DurationMinutes);
            if (!TryLocalToUtc(DateOnly.FromDateTime(localEnd), TimeOnly.FromDateTime(localEnd),
                    body.TimeZoneId, out var end, out error))
                return (null, error);
            if (end <= start) return (null, SchoolPortalErrorCodes.SlotInvalidTime);
            rows.Add((start, end));
            if (rows.Count > 100) return (null, SchoolPortalErrorCodes.SlotRecurrenceLimit);
        }
        return (rows, null);
    }

    public static string Fingerprint(SlotRecurrenceRequest body)
    {
        var canonical = JsonSerializer.Serialize(body with { RequestKey = string.Empty });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
