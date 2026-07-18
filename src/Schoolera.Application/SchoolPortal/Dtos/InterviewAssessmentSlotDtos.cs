using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Dtos;

public sealed record UpsertInterviewAssessmentSlotRequest(
    Guid SchoolBranchId, Guid EducationalStageId, Guid? GradeId, Guid AcademicYearId,
    SlotKind Kind, SlotDeliveryMode DeliveryMode, DateOnly LocalDate, TimeOnly LocalStartTime,
    TimeOnly LocalEndTime, string TimeZoneId, int Capacity,
    SlotResourceKind? ResourceKind, Guid? ResourceReferenceId,
    string? InstructionsAr, string? InstructionsEn, string? MeetingProviderCode, byte[]? RowVersion);

public sealed record CancelInterviewAssessmentSlotRequest(
    string CancellationReasonAr, string CancellationReasonEn, byte[]? RowVersion);

public sealed record InterviewAssessmentSlotCapabilities(
    bool CanEdit, bool CanOpen, bool CanClose, bool CanReopen, bool CanCancel);

public sealed record InterviewAssessmentSlotDto(
    Guid Id, Guid SchoolId, Guid SchoolBranchId, Guid EducationalStageId, Guid? GradeId,
    Guid AcademicYearId, SlotKind Kind, SlotDeliveryMode DeliveryMode,
    DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc, string TimeZoneId, int Capacity,
    int ActiveAppointmentCount, int AffectedApplicationCount, SlotResourceKind? ResourceKind,
    Guid? ResourceReferenceId, string? InstructionsAr, string? InstructionsEn, SlotStatus Status,
    string? CancellationReasonAr, string? CancellationReasonEn, string? MeetingProviderCode,
    string? GenerationBatchReference, byte[] RowVersion, InterviewAssessmentSlotCapabilities Capabilities);

public sealed record InterviewAssessmentSlotAuditDto(
    Guid Id, string Action, Guid ActorUserId, string? Metadata, DateTimeOffset CreatedAtUtc);

public sealed record SlotRecurrenceRequest(
    Guid SchoolBranchId, Guid EducationalStageId, Guid? GradeId, Guid AcademicYearId,
    SlotKind Kind, SlotDeliveryMode DeliveryMode, DateOnly LocalStartDate, DateOnly LocalEndDate,
    TimeOnly LocalStartTime, int DurationMinutes, string TimeZoneId, int Capacity,
    SlotResourceKind? ResourceKind, Guid? ResourceReferenceId, string? InstructionsAr,
    string? InstructionsEn, string? MeetingProviderCode, string RequestKey,
    SlotRecurrenceFrequency Frequency, IReadOnlyList<DayOfWeek>? SelectedWeekdays);

public sealed record SlotOccurrenceDto(
    DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc, bool HasConflict);

public sealed record SlotRecurrencePreviewDto(
    int OccurrenceCount, IReadOnlyList<SlotOccurrenceDto> Occurrences);

public sealed record SlotGenerationResultDto(
    string BatchReference, bool WasIdempotent, IReadOnlyList<InterviewAssessmentSlotDto> Slots);

public sealed record ParentAvailableSlotDto(
    Guid SlotId, SlotKind Kind, SlotDeliveryMode DeliveryMode, DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc, string TimeZoneId, int RemainingSeats, string? Instructions,
    Guid SchoolBranchId, string BranchName, string? BranchAddress);
