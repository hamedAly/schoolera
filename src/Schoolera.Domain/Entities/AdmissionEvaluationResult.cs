using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class AdmissionEvaluationResult
{
    private readonly List<AdmissionEvaluationResultVersion> versions = [];

    private AdmissionEvaluationResult() { }

    public AdmissionEvaluationResult(
        Guid applicationId,
        Guid appointmentId,
        Guid schoolId,
        Guid branchId,
        SlotKind kind,
        Guid templateId,
        int templateVersion,
        string templateSnapshotJson,
        Guid policySnapshotId,
        int policyVersion,
        string appointmentSnapshotJson,
        Guid evaluatorUserId,
        DateTimeOffset startedAtUtc)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = applicationId;
        AppointmentId = appointmentId;
        SchoolId = schoolId;
        SchoolBranchId = branchId;
        Kind = kind;
        TemplateId = templateId;
        TemplateVersion = templateVersion;
        TemplateSnapshotJson = Required(templateSnapshotJson, 100_000);
        PolicySnapshotId = policySnapshotId;
        PolicyVersion = policyVersion;
        AppointmentSnapshotJson = Required(appointmentSnapshotJson, 10_000);
        StartedAtUtc = startedAtUtc;
        EvaluatorUserId = evaluatorUserId;
        State = EvaluationResultState.Draft;
        ChildAttendance = EvaluationAttendance.Unknown;
        ParentAttendance = EvaluationAttendance.Unknown;
        Recommendation = EvaluationRecommendation.NoRecommendation;
        DraftAnswersJson = "[]";
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionApplicationId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid SchoolBranchId { get; private set; }
    public SlotKind Kind { get; private set; }
    public Guid TemplateId { get; private set; }
    public int TemplateVersion { get; private set; }
    public string TemplateSnapshotJson { get; private set; } = string.Empty;
    public Guid PolicySnapshotId { get; private set; }
    public int PolicyVersion { get; private set; }
    public string AppointmentSnapshotJson { get; private set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; private set; }
    public Guid EvaluatorUserId { get; private set; }
    public EvaluationResultState State { get; private set; }
    public int CurrentFinalizedVersion { get; private set; }
    public string DraftAnswersJson { get; private set; } = "[]";
    public EvaluationAttendance ChildAttendance { get; private set; }
    public EvaluationAttendance ParentAttendance { get; private set; }
    public EvaluationRecommendation Recommendation { get; private set; }
    public string? SuggestedParentReasonAr { get; private set; }
    public string? SuggestedParentReasonEn { get; private set; }
    public string? InternalNotes { get; private set; }
    public string? PendingCorrectionReason { get; private set; }
    public int? CorrectsVersionNumber { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<AdmissionEvaluationResultVersion> Versions => versions;

    public void SaveDraft(
        string answersJson,
        EvaluationAttendance childAttendance,
        EvaluationAttendance parentAttendance,
        EvaluationRecommendation recommendation,
        string? suggestedReasonAr,
        string? suggestedReasonEn,
        string? internalNotes)
    {
        if (State != EvaluationResultState.Draft)
            throw new InvalidOperationException("Only a draft result may be updated.");
        DraftAnswersJson = Required(answersJson, 100_000);
        ChildAttendance = Defined(childAttendance);
        ParentAttendance = Defined(parentAttendance);
        Recommendation = Defined(recommendation);
        SuggestedParentReasonAr = Optional(suggestedReasonAr, 2000);
        SuggestedParentReasonEn = Optional(suggestedReasonEn, 2000);
        InternalNotes = Optional(internalNotes, 4000);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public AdmissionEvaluationResultVersion Finalize(Guid actorUserId, DateTimeOffset endedAtUtc)
    {
        if (State != EvaluationResultState.Draft)
            throw new InvalidOperationException("The result is already finalized.");
        var nextVersion = CurrentFinalizedVersion + 1;
        var version = new AdmissionEvaluationResultVersion(
            Id, nextVersion, TemplateId, TemplateVersion, TemplateSnapshotJson,
            PolicySnapshotId, PolicyVersion, AppointmentSnapshotJson, StartedAtUtc, endedAtUtc,
            ChildAttendance, ParentAttendance, EvaluatorUserId, DraftAnswersJson, Recommendation,
            SuggestedParentReasonAr, SuggestedParentReasonEn, InternalNotes, actorUserId,
            PendingCorrectionReason, CorrectsVersionNumber);
        versions.Add(version);
        CurrentFinalizedVersion = nextVersion;
        State = EvaluationResultState.Finalized;
        PendingCorrectionReason = null;
        CorrectsVersionNumber = null;
        UpdatedAtUtc = endedAtUtc;
        return version;
    }

    public void BeginCorrection(AdmissionEvaluationResultVersion previous, string correctionReason)
    {
        if (State != EvaluationResultState.Finalized || previous.VersionNumber != CurrentFinalizedVersion)
            throw new InvalidOperationException("Only the current finalized version may be corrected.");
        State = EvaluationResultState.Draft;
        DraftAnswersJson = previous.AnswersJson;
        ChildAttendance = previous.ChildAttendance;
        ParentAttendance = previous.ParentAttendance;
        Recommendation = previous.Recommendation;
        SuggestedParentReasonAr = previous.SuggestedParentReasonAr;
        SuggestedParentReasonEn = previous.SuggestedParentReasonEn;
        InternalNotes = previous.InternalNotes;
        PendingCorrectionReason = Required(correctionReason, 500);
        CorrectsVersionNumber = previous.VersionNumber;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static T Defined<T>(T value) where T : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));

    private static string Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.");
        var normalized = value.Trim();
        return normalized[..Math.Min(max, normalized.Length)];
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal).Trim();
        return normalized[..Math.Min(max, normalized.Length)];
    }
}

public sealed class AdmissionEvaluationResultVersion
{
    private AdmissionEvaluationResultVersion() { }

    public AdmissionEvaluationResultVersion(
        Guid resultId,
        int versionNumber,
        Guid templateId,
        int templateVersion,
        string templateSnapshotJson,
        Guid policySnapshotId,
        int policyVersion,
        string appointmentSnapshotJson,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        EvaluationAttendance childAttendance,
        EvaluationAttendance parentAttendance,
        Guid evaluatorUserId,
        string answersJson,
        EvaluationRecommendation recommendation,
        string? suggestedReasonAr,
        string? suggestedReasonEn,
        string? internalNotes,
        Guid finalizedByUserId,
        string? correctionReason,
        int? previousVersionNumber)
    {
        Id = Guid.NewGuid();
        AdmissionEvaluationResultId = resultId;
        VersionNumber = versionNumber;
        TemplateId = templateId;
        TemplateVersion = templateVersion;
        TemplateSnapshotJson = templateSnapshotJson;
        PolicySnapshotId = policySnapshotId;
        PolicyVersion = policyVersion;
        AppointmentSnapshotJson = appointmentSnapshotJson;
        StartedAtUtc = startedAtUtc;
        EndedAtUtc = endedAtUtc;
        ChildAttendance = childAttendance;
        ParentAttendance = parentAttendance;
        EvaluatorUserId = evaluatorUserId;
        AnswersJson = answersJson;
        Recommendation = recommendation;
        SuggestedParentReasonAr = suggestedReasonAr;
        SuggestedParentReasonEn = suggestedReasonEn;
        InternalNotes = internalNotes;
        FinalizedAtUtc = endedAtUtc;
        FinalizedByUserId = finalizedByUserId;
        CorrectionReason = correctionReason;
        PreviousVersionNumber = previousVersionNumber;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionEvaluationResultId { get; private set; }
    public int VersionNumber { get; private set; }
    public Guid TemplateId { get; private set; }
    public int TemplateVersion { get; private set; }
    public string TemplateSnapshotJson { get; private set; } = string.Empty;
    public Guid PolicySnapshotId { get; private set; }
    public int PolicyVersion { get; private set; }
    public string AppointmentSnapshotJson { get; private set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset EndedAtUtc { get; private set; }
    public EvaluationAttendance ChildAttendance { get; private set; }
    public EvaluationAttendance ParentAttendance { get; private set; }
    public Guid EvaluatorUserId { get; private set; }
    public string AnswersJson { get; private set; } = "[]";
    public EvaluationRecommendation Recommendation { get; private set; }
    public string? SuggestedParentReasonAr { get; private set; }
    public string? SuggestedParentReasonEn { get; private set; }
    public string? InternalNotes { get; private set; }
    public DateTimeOffset FinalizedAtUtc { get; private set; }
    public Guid FinalizedByUserId { get; private set; }
    public string? CorrectionReason { get; private set; }
    public int? PreviousVersionNumber { get; private set; }
}

public sealed class AdmissionEvaluationHistory
{
    private AdmissionEvaluationHistory() { }

    public AdmissionEvaluationHistory(
        Guid resultId,
        Guid applicationId,
        Guid appointmentId,
        SlotKind kind,
        AdmissionEvaluationHistoryAction action,
        int? versionNumber,
        Guid actorUserId,
        string? idempotencyKey,
        string requestFingerprint)
    {
        Id = Guid.NewGuid();
        AdmissionEvaluationResultId = resultId;
        AdmissionApplicationId = applicationId;
        AppointmentId = appointmentId;
        Kind = kind;
        Action = action;
        VersionNumber = versionNumber;
        ActorUserId = actorUserId;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim()[..Math.Min(128, idempotencyKey.Trim().Length)];
        RequestFingerprint = string.IsNullOrWhiteSpace(requestFingerprint)
            ? throw new ArgumentException("Request fingerprint is required.")
            : requestFingerprint.Trim()[..Math.Min(64, requestFingerprint.Trim().Length)];
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionEvaluationResultId { get; private set; }
    public Guid AdmissionApplicationId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public SlotKind Kind { get; private set; }
    public AdmissionEvaluationHistoryAction Action { get; private set; }
    public int? VersionNumber { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string RequestFingerprint { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
