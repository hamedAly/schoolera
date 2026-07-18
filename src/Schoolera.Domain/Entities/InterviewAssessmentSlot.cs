using System.Text.RegularExpressions;
using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class InterviewAssessmentSlot
{
    private InterviewAssessmentSlot() { }

    public InterviewAssessmentSlot(
        Guid schoolId, Guid schoolBranchId, Guid educationalStageId, Guid? gradeId,
        Guid academicYearId, SlotKind kind, SlotDeliveryMode deliveryMode,
        DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, string timeZoneId, int capacity,
        SlotResourceKind? resourceKind, Guid? resourceReferenceId,
        string? instructionsAr, string? instructionsEn, string? meetingProviderCode,
        string? generationBatchReference, Guid actorUserId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        Status = SlotStatus.Draft;
        CreatedByUserId = UpdatedByUserId = actorUserId;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        Apply(kind, deliveryMode, startAtUtc, endAtUtc, timeZoneId, capacity, resourceKind,
            resourceReferenceId, instructionsAr, instructionsEn, meetingProviderCode,
            generationBatchReference);
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid SchoolBranchId { get; private set; }
    public Guid EducationalStageId { get; private set; }
    public Guid? GradeId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public SlotKind Kind { get; private set; }
    public SlotDeliveryMode DeliveryMode { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset EndAtUtc { get; private set; }
    public string TimeZoneId { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public SlotResourceKind? ResourceKind { get; private set; }
    public Guid? ResourceReferenceId { get; private set; }
    public string? InstructionsAr { get; private set; }
    public string? InstructionsEn { get; private set; }
    public SlotStatus Status { get; private set; }
    public string? CancellationReasonAr { get; private set; }
    public string? CancellationReasonEn { get; private set; }
    public string? MeetingProviderCode { get; private set; }
    public string? GenerationBatchReference { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? OpenedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void UpdateDraftOrUnbooked(
        Guid schoolBranchId, Guid educationalStageId, Guid? gradeId, Guid academicYearId,
        SlotKind kind, SlotDeliveryMode mode, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc,
        string timeZoneId, int capacity, SlotResourceKind? resourceKind, Guid? resourceReferenceId,
        string? instructionsAr, string? instructionsEn, string? meetingProviderCode, Guid actorUserId)
    {
        if (Status == SlotStatus.Cancelled)
            throw new InvalidOperationException("Cancelled slots are terminal.");
        if (Status == SlotStatus.Open)
            throw new InvalidOperationException("Open slots must be closed before editing.");

        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        Apply(kind, mode, startAtUtc, endAtUtc, timeZoneId, capacity, resourceKind,
            resourceReferenceId, instructionsAr, instructionsEn, meetingProviderCode,
            GenerationBatchReference);
        Touch(actorUserId);
    }

    public void Open(Guid actorUserId)
    {
        EnsureTransition(SlotStatus.Open);
        Status = SlotStatus.Open;
        OpenedAtUtc = DateTimeOffset.UtcNow;
        Touch(actorUserId);
    }

    public void Close(Guid actorUserId)
    {
        EnsureTransition(SlotStatus.Closed);
        Status = SlotStatus.Closed;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        Touch(actorUserId);
    }

    public void Reopen(Guid actorUserId) => Open(actorUserId);

    public bool Cancel(string reasonAr, string reasonEn, Guid actorUserId)
    {
        if (Status == SlotStatus.Cancelled) return false;
        CancellationReasonAr = Required(reasonAr, 2000);
        CancellationReasonEn = Required(reasonEn, 2000);
        Status = SlotStatus.Cancelled;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        Touch(actorUserId);
        return true;
    }

    public static bool Overlaps(
        DateTimeOffset existingStart, DateTimeOffset existingEnd,
        DateTimeOffset newStart, DateTimeOffset newEnd) =>
        existingStart < newEnd && existingEnd > newStart;

    private void EnsureTransition(SlotStatus target)
    {
        var allowed = target switch
        {
            SlotStatus.Open => Status is SlotStatus.Draft or SlotStatus.Closed,
            SlotStatus.Closed => Status == SlotStatus.Open,
            _ => false,
        };
        if (!allowed) throw new InvalidOperationException("Invalid slot transition.");
    }

    private void Apply(
        SlotKind kind, SlotDeliveryMode mode, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc,
        string timeZoneId, int capacity, SlotResourceKind? resourceKind, Guid? resourceReferenceId,
        string? instructionsAr, string? instructionsEn, string? meetingProviderCode,
        string? generationBatchReference)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException();
        if (endAtUtc <= startAtUtc) throw new ArgumentException("End must follow start.");
        if (capacity is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (resourceKind.HasValue != resourceReferenceId.HasValue ||
            resourceKind is not (null or SlotResourceKind.StaffMember))
            throw new ArgumentException("Invalid resource.");

        Kind = kind;
        DeliveryMode = mode;
        StartAtUtc = startAtUtc.ToUniversalTime();
        EndAtUtc = endAtUtc.ToUniversalTime();
        TimeZoneId = Required(timeZoneId, 128);
        Capacity = capacity;
        ResourceKind = resourceKind;
        ResourceReferenceId = resourceReferenceId;
        InstructionsAr = Optional(instructionsAr, 2000);
        InstructionsEn = Optional(instructionsEn, 2000);
        MeetingProviderCode = SafeCode(meetingProviderCode, FieldLengthLimits.MeetingProviderCode);
        GenerationBatchReference = SafeCode(generationBatchReference, 128);
    }

    private void Touch(Guid actor) { UpdatedByUserId = actor; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    private static string Required(string value, int max) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Required.") :
        value.Trim()[..Math.Min(max, value.Trim().Length)];
    private static string? Optional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(max, value.Trim().Length)];
    private static string? SafeCode(string? value, int max)
    {
        var normalized = Optional(value, max);
        if (normalized is not null && !Regex.IsMatch(normalized, "^[A-Za-z0-9._:-]+$"))
            throw new ArgumentException("Unsafe code.");
        return normalized;
    }
}

public sealed class InterviewAssessmentSlotAudit
{
    private InterviewAssessmentSlotAudit() { }
    public InterviewAssessmentSlotAudit(Guid schoolId, Guid slotId, string action, Guid actorUserId, string? metadata)
    {
        Id = Guid.NewGuid(); SchoolId = schoolId; InterviewAssessmentSlotId = slotId;
        Action = action.Trim()[..Math.Min(64, action.Trim().Length)]; ActorUserId = actorUserId;
        Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim()[..Math.Min(500, metadata.Trim().Length)];
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid InterviewAssessmentSlotId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public string? Metadata { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class InterviewAssessmentSlotGenerationBatch
{
    private InterviewAssessmentSlotGenerationBatch() { }
    public InterviewAssessmentSlotGenerationBatch(Guid schoolId, string requestKey, string fingerprint, Guid createdBy)
    {
        Id = Guid.NewGuid(); SchoolId = schoolId;
        RequestKey = requestKey.Trim()[..Math.Min(128, requestKey.Trim().Length)];
        RequestFingerprint = fingerprint; BatchReference = Guid.NewGuid().ToString("N");
        CreatedByUserId = createdBy; CreatedAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public string RequestKey { get; private set; } = string.Empty;
    public string RequestFingerprint { get; private set; } = string.Empty;
    public string BatchReference { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
