using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public enum SlotAssignmentResult
{
    Succeeded,
    Existing,
    Full,
    Unavailable,
    IdempotencyConflict,
    ConcurrencyConflict,
}

public enum AtomicSlotOpenResult
{
    Succeeded,
    ResourceConflict,
    ConcurrencyConflict,
}

public enum AtomicSlotGenerationResult
{
    Created,
    Existing,
    IdempotencyConflict,
    ResourceConflict,
    ConcurrencyConflict,
}

public sealed record AtomicSlotGenerationOutcome(
    AtomicSlotGenerationResult Result,
    string? BatchReference = null);

public enum ParentAppointmentActionKind
{
    Confirm,
    SelectSlot,
    RequestReschedule,
    Cancel,
    Join,
}

public enum ParentAppointmentActionResult
{
    Succeeded,
    Existing,
    NotFound,
    InvalidTransition,
    PolicyMismatch,
    RescheduleNotAllowed,
    RescheduleLimitReached,
    DeadlinePassed,
    CancellationNotAllowed,
    SlotUnavailable,
    SlotFull,
    ConcurrencyConflict,
    IdempotencyConflict,
    JoinTooEarly,
    JoinExpired,
    JoinProviderUnavailable,
}

public sealed record ParentAppointmentActionRequest(
    Guid ParentUserId,
    Guid ApplicationId,
    SlotKind Kind,
    ParentAppointmentActionKind Action,
    Guid? SlotId,
    byte[]? RowVersion,
    string? IdempotencyKey,
    string? Reason);

public sealed record ParentAppointmentActionOutcome(
    ParentAppointmentActionResult Result,
    AdmissionApplication? Application = null);

public interface IInterviewAssessmentSlotRepository
{
    Task<InterviewAssessmentSlot?> GetAsync(Guid schoolId, Guid slotId, bool tracking,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterviewAssessmentSlot>> ListAsync(Guid schoolId, Guid? branchId,
        Guid? stageId, Guid? gradeId, Guid? academicYearId, SlotKind? kind,
        SlotDeliveryMode? mode, SlotStatus? status, Guid? resourceId,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
    Task AddAsync(InterviewAssessmentSlot slot, CancellationToken cancellationToken = default);
    Task AddAuditAsync(InterviewAssessmentSlotAudit audit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterviewAssessmentSlotAudit>> ListAuditAsync(Guid schoolId, Guid slotId,
        CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid schoolId, Guid branchId, SlotResourceKind? resourceKind, Guid? resourceId,
        DateTimeOffset start, DateTimeOffset end, Guid? excludeSlotId,
        CancellationToken cancellationToken = default);
    Task<SlotAssignmentResult> ExecuteAtomicAssignmentAsync(
        Guid schoolId, Guid slotId, int expectedCapacity, Guid? excludeAppointmentId,
        Func<CancellationToken, Task> mutation, CancellationToken cancellationToken = default);
    Task<SlotAssignmentResult> ExecuteAtomicSchoolProposalAsync(
        Guid schoolId, Guid slotId, int expectedCapacity, Guid? excludeAppointmentId,
        Guid applicationId, SlotKind kind, AdmissionAppointmentAction action,
        string idempotencyKey, Func<CancellationToken, Task> mutation,
        CancellationToken cancellationToken = default);
    Task<SlotAssignmentResult> CheckSchoolProposalIdempotencyAsync(
        Guid applicationId, SlotKind kind, AdmissionAppointmentAction action,
        Guid slotId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<AtomicSlotOpenResult> ExecuteAtomicOpenAsync(
        Guid schoolId, Guid slotId, SlotResourceKind? resourceKind, Guid? resourceId,
        DateTimeOffset start, DateTimeOffset end, Func<CancellationToken, Task> mutation,
        CancellationToken cancellationToken = default);
    Task<AtomicSlotGenerationOutcome> ExecuteAtomicGenerationAsync(
        Guid schoolId, string requestKey, string fingerprint, SlotResourceKind? resourceKind,
        Guid? resourceId,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> occurrences,
        Func<CancellationToken, Task<InterviewAssessmentSlotGenerationBatch>> mutation,
        CancellationToken cancellationToken = default);
    Task<int> CountActiveAppointmentsAsync(Guid slotId, CancellationToken cancellationToken = default);
    Task<int> CountAffectedApplicationsAsync(Guid slotId, CancellationToken cancellationToken = default);
    Task<bool> BranchScopeIsValidAsync(Guid schoolId, Guid branchId, Guid stageId, Guid? gradeId,
        Guid academicYearId, CancellationToken cancellationToken = default);
    Task<bool> ResourceIsValidAsync(Guid schoolId, Guid branchId, Guid resourceId,
        CancellationToken cancellationToken = default);
    Task<bool> MeetingProviderIsActiveAsync(string providerCode, CancellationToken cancellationToken = default);
    Task<AcademicYear?> GetAcademicYearAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SchoolBranch?> GetBranchAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default);
    Task<InterviewAssessmentSlotGenerationBatch?> GetBatchAsync(Guid schoolId, string requestKey,
        CancellationToken cancellationToken = default);
    Task AddBatchAsync(InterviewAssessmentSlotGenerationBatch batch,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterviewAssessmentSlot>> ListBatchAsync(Guid schoolId, string batchReference,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdmissionApplication>> GetApplicationsForCancellationAsync(Guid slotId,
        SlotKind kind, CancellationToken cancellationToken = default);
    Task<AdmissionApplication?> GetOwnedApplicationAsync(Guid parentUserId, Guid applicationId,
        CancellationToken cancellationToken = default);
    Task AddAppointmentActionAsync(AdmissionAppointmentActionHistory action,
        CancellationToken cancellationToken = default);
    Task<ParentAppointmentActionOutcome> ExecuteParentAppointmentActionAsync(
        ParentAppointmentActionRequest request, CancellationToken cancellationToken = default);
}
