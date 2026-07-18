using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Dtos;

using Schoolera.Application.Meetings;

public sealed record AdmissionMissingItemRefDto(
    AdmissionMissingItemKind Kind,
    Guid? RequirementSnapshotId,
    Guid? QuestionSnapshotId,
    AdmissionParentSnapshotFieldCode? ParentSnapshotField,
    AdmissionChildSnapshotFieldCode? ChildSnapshotField,
    bool IsMandatory);

public sealed record RequestMissingDocumentsRequest(
    string ParentVisibleReason,
    string? Instructions,
    DateTimeOffset? ResponseDeadlineUtc,
    IReadOnlyList<AdmissionMissingItemRefDto> Items,
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record ScheduleAdmissionAppointmentRequest(
    DateTimeOffset ScheduledAtUtc,
    string TimeZoneId,
    AdmissionAppointmentMode Mode,
    string? Location,
    string? OnlineInstructions,
    string? ParentVisibleNotes,
    string? PreparationInstructions,
    string? InternalReviewNote,
    byte[]? RowVersion,
    Guid? SlotId = null,
    string? IdempotencyKey = null);

public sealed record CompleteAdmissionAppointmentRequest(
    string? ParentVisibleOutcomeNotes,
    string? InternalReviewNote,
    /// <summary>Next status after completion: UnderReview, WaitingList, Accepted, or Rejected.</summary>
    AdmissionApplicationStatus NextStatus,
    string? WaitingListReason,
    int? WaitingListPosition,
    DateOnly? WaitingListReviewDate,
    string? ParentVisibleRejectionReason,
    byte[]? RowVersion);

public sealed record CancelAdmissionAppointmentRequest(
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record ParentAppointmentMutationRequest(
    byte[] RowVersion,
    string IdempotencyKey);

public sealed record ParentSelectAppointmentSlotRequest(
    Guid SlotId,
    byte[] RowVersion,
    string IdempotencyKey);

public sealed record ParentRequestAppointmentRescheduleRequest(
    string Reason,
    byte[] RowVersion,
    string IdempotencyKey);

public sealed record ParentCancelAppointmentRequest(
    string? Reason,
    byte[] RowVersion,
    string IdempotencyKey);

public sealed record ParentJoinAppointmentRequest(byte[] RowVersion);

public sealed record MoveToWaitingListRequest(
    string? ParentVisibleReason,
    int? Position,
    DateOnly? ReviewDate,
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record ReturnFromWaitingListRequest(
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record MarkRegisteredRequest(
    string? InternalReviewNote,
    byte[]? RowVersion);

public sealed record ResubmitMissingDocumentsRequest(byte[]? RowVersion);

public sealed record CorrectMissingSnapshotFieldRequest(
    Guid MissingItemId,
    string? TextValue,
    bool? BooleanValue,
    byte[]? RowVersion);

public sealed record AdmissionMissingItemDto(
    Guid Id,
    AdmissionMissingItemKind Kind,
    bool IsMandatory,
    bool IsCompleted,
    string Label,
    Guid? RequirementSnapshotId,
    Guid? QuestionSnapshotId,
    AdmissionParentSnapshotFieldCode? ParentSnapshotField,
    AdmissionChildSnapshotFieldCode? ChildSnapshotField);

public sealed record AdmissionMissingItemsRequestDto(
    Guid Id,
    string ParentVisibleReason,
    string? Instructions,
    DateTimeOffset? ResponseDeadlineUtc,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ClearedAtUtc,
    IReadOnlyList<AdmissionMissingItemDto> Items);

public sealed record AppointmentCapabilitiesDto(
    bool CanConfirmAppointment,
    bool CanSelectAlternateSlot,
    bool CanRequestReschedule,
    bool CanCancelAppointment,
    bool CanJoinOnlineAppointment,
    bool CanViewAvailableSlots,
    int RemainingRescheduleAttempts,
    DateTimeOffset? RescheduleDeadlineUtc,
    DateTimeOffset? CancellationDeadlineUtc,
    DateTimeOffset? JoinWindowStartsAtUtc,
    DateTimeOffset? JoinWindowEndsAtUtc,
    string? JoinUnavailableReasonCode,
    string SupportRoute);

public sealed record AdmissionAppointmentDto(
    Guid Id,
    Guid? SlotId,
    DateTimeOffset ScheduledAtUtc,
    string TimeZoneId,
    AdmissionAppointmentMode Mode,
    string? Location,
    string? OnlineInstructions,
    string? ParentVisibleNotes,
    string? PreparationInstructions,
    AdmissionAppointmentLifecycle Lifecycle,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ProposedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? RescheduleRequestedAtUtc,
    DateTimeOffset? NoShowAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? OutcomeNotes,
    int ParentRescheduleAttemptCount,
    string? LastParentVisibleRescheduleReason,
    string? CancellationReason,
    AppointmentRescheduleInitiator? RescheduleInitiator,
    byte[] RowVersion,
    AppointmentCapabilitiesDto Capabilities,
    OnSiteAttendanceDetailsDto? OnSiteAttendance,
    MeetingSessionSummaryDto? OnlineMeeting);

public sealed record AdmissionWaitingListDto(
    string? Reason,
    int? Position,
    DateOnly? ReviewDate,
    DateTimeOffset? EnteredAtUtc);
