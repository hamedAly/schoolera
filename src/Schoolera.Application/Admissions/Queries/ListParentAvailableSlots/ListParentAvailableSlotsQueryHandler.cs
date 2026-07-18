using MediatR;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Queries.ListParentAvailableSlots;

public sealed record ListParentAvailableSlotsQuery(Guid ApplicationId, SlotKind Kind)
    : IRequest<Result<IReadOnlyList<ParentAvailableSlotDto>>>
{
    public static ListParentAvailableSlotsQuery FromFilter(Guid applicationId, int kind) =>
        new(applicationId, (SlotKind)kind);
}

public sealed class ListParentAvailableSlotsQueryHandler(
    ICurrentUser currentUser,
    IInterviewAssessmentSlotRepository repository)
    : IRequestHandler<ListParentAvailableSlotsQuery, Result<IReadOnlyList<ParentAvailableSlotDto>>>
{
    public async Task<Result<IReadOnlyList<ParentAvailableSlotDto>>> Handle(
        ListParentAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Result<IReadOnlyList<ParentAvailableSlotDto>>.Failure(
                ["Application not found."], [AdmissionErrorCodes.NotFound]);
        var application = await repository.GetOwnedApplicationAsync(
            userId, request.ApplicationId, cancellationToken);
        var snapshot = application?.PolicySnapshot;
        if (application is null || snapshot is null)
            return Result<IReadOnlyList<ParentAvailableSlotDto>>.Failure(
                ["Application not found."], [AdmissionErrorCodes.NotFound]);
        var required = request.Kind == SlotKind.Interview
            ? snapshot.RequirementMode is InterviewAssessmentRequirementMode.InterviewOnly or InterviewAssessmentRequirementMode.InterviewAndAssessment &&
              application.Status == AdmissionApplicationStatus.InterviewRequired
            : snapshot.RequirementMode is InterviewAssessmentRequirementMode.AssessmentOnly or InterviewAssessmentRequirementMode.InterviewAndAssessment &&
              application.Status == AdmissionApplicationStatus.AssessmentRequired;
        if (!required)
            return Result<IReadOnlyList<ParentAvailableSlotDto>>.Failure(
                ["Slots are unavailable for this application."], [AdmissionErrorCodes.SlotUnavailable]);

        var rows = await repository.ListAsync(application.SchoolId, application.SchoolBranchId,
            application.EducationalStageId, application.GradeId, application.AcademicYearId,
            request.Kind, null, SlotStatus.Open, null, DateTimeOffset.UtcNow, null, cancellationToken);
        var academicYear = await repository.GetAcademicYearAsync(
            application.AcademicYearId, cancellationToken);
        if (academicYear is null)
            return Result<IReadOnlyList<ParentAvailableSlotDto>>.Success([]);
        var yearStart = new DateTimeOffset(
            academicYear.StartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var result = new List<ParentAvailableSlotDto>();
        foreach (var slot in rows)
        {
            var active = await repository.CountActiveAppointmentsAsync(slot.Id, cancellationToken);
            if (active >= slot.Capacity ||
                slot.StartAtUtc < DateTimeOffset.UtcNow.AddHours(snapshot.MinimumSchedulingLeadTimeHours ?? 0) ||
                (int)(slot.EndAtUtc - slot.StartAtUtc).TotalMinutes != snapshot.ExpectedDurationMinutes ||
                (snapshot.BookingWindowOpensDaysBefore is { } opens &&
                    slot.StartAtUtc < yearStart.AddDays(-opens)) ||
                (snapshot.BookingWindowClosesDaysBefore is { } closes &&
                    slot.StartAtUtc > yearStart.AddDays(-closes)))
                continue;
            var modeAllowed = slot.DeliveryMode == SlotDeliveryMode.Online
                ? snapshot.DeliveryMode is InterviewAssessmentDeliveryMode.Online or InterviewAssessmentDeliveryMode.Hybrid
                : snapshot.DeliveryMode is InterviewAssessmentDeliveryMode.OnSite or InterviewAssessmentDeliveryMode.Hybrid;
            if (!modeAllowed) continue;
            var branch = await repository.GetBranchAsync(
                application.SchoolId, slot.SchoolBranchId, cancellationToken);
            if (branch is null || !branch.IsActive) continue;
            if (slot.DeliveryMode == SlotDeliveryMode.Online &&
                (string.IsNullOrWhiteSpace(slot.MeetingProviderCode) ||
                 !string.Equals(slot.MeetingProviderCode, snapshot.MeetingProviderCode,
                     StringComparison.OrdinalIgnoreCase) ||
                 !await repository.MeetingProviderIsActiveAsync(slot.MeetingProviderCode, cancellationToken)))
                continue;
            if (slot.DeliveryMode == SlotDeliveryMode.OnSite &&
                string.IsNullOrWhiteSpace(branch.AddressLineAr) &&
                string.IsNullOrWhiteSpace(branch.AddressReference))
                continue;
            var english = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
            result.Add(new(slot.Id, slot.Kind, slot.DeliveryMode, slot.StartAtUtc, slot.EndAtUtc,
                slot.TimeZoneId, Math.Min(100, slot.Capacity - active),
                english ? slot.InstructionsEn ?? slot.InstructionsAr : slot.InstructionsAr,
                branch.Id, english ? branch.NameEn ?? branch.NameAr : branch.NameAr,
                english ? branch.AddressLineEn ?? branch.AddressLineAr : branch.AddressLineAr));
        }
        return Result<IReadOnlyList<ParentAvailableSlotDto>>.Success(result);
    }
}
