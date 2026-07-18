using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Dtos;

public sealed record EvaluationCriterionOptionRequest(
    string Value,
    string LabelAr,
    string LabelEn);

public sealed record EvaluationCriterionRequest(
    EvaluationCriterionType Type,
    string LabelAr,
    string LabelEn,
    string? HelpTextAr,
    string? HelpTextEn,
    bool IsRequired,
    int SortOrder,
    int? ShortTextMaxLength,
    IReadOnlyList<EvaluationCriterionOptionRequest> Options);

public sealed record UpsertEvaluationTemplateRequest(
    string NameAr,
    string NameEn,
    EvaluationTemplateKind Kind,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    IReadOnlyList<EvaluationCriterionRequest> Criteria,
    string IdempotencyKey,
    byte[]? RowVersion);

public sealed record EvaluationTemplateActionRequest(byte[] RowVersion, string IdempotencyKey);

public sealed record EvaluationCriterionOptionDto(
    Guid Id, string Value, string LabelAr, string LabelEn, int SortOrder);

public sealed record EvaluationCriterionDto(
    Guid Id,
    EvaluationCriterionType Type,
    string LabelAr,
    string LabelEn,
    string? HelpTextAr,
    string? HelpTextEn,
    bool IsRequired,
    int SortOrder,
    int? ShortTextMaxLength,
    IReadOnlyList<EvaluationCriterionOptionDto> Options);

public sealed record EvaluationTemplateDto(
    Guid Id,
    Guid SchoolId,
    string NameAr,
    string NameEn,
    EvaluationTemplateKind Kind,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    EvaluationTemplatePublicationStatus PublicationStatus,
    bool IsActive,
    int Version,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyList<EvaluationCriterionDto> Criteria,
    byte[] RowVersion);

public sealed record EvaluationTemplateAuditDto(
    Guid Id,
    string Action,
    int Version,
    DateTimeOffset CreatedAtUtc);

public sealed record EvaluationTemplateListQuery(
    Guid? BranchId,
    Guid? StageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    EvaluationTemplateKind? Kind,
    EvaluationTemplatePublicationStatus? PublicationStatus,
    bool? IsActive);

public sealed record EvaluationAnswerRequest(
    Guid CriterionId,
    bool? YesNoValue,
    int? RatingValue,
    string? SelectedOptionValue,
    string? ShortTextValue);

public sealed record SaveEvaluationDraftRequest(
    EvaluationAttendance ChildAttendance,
    EvaluationAttendance ParentAttendance,
    IReadOnlyList<EvaluationAnswerRequest> Answers,
    EvaluationRecommendation Recommendation,
    string? SuggestedParentReasonAr,
    string? SuggestedParentReasonEn,
    string? InternalNotes,
    byte[] RowVersion,
    string IdempotencyKey);

public sealed record StartEvaluationSessionRequest(
    byte[] AppointmentRowVersion,
    string IdempotencyKey);

public sealed record FinalizeEvaluationResultRequest(
    byte[] ResultRowVersion,
    string IdempotencyKey);

public sealed record RecordEvaluationNoShowRequest(
    byte[] AppointmentRowVersion,
    string IdempotencyKey);

public sealed record BeginEvaluationCorrectionRequest(
    string CorrectionReason,
    byte[] ResultRowVersion,
    string IdempotencyKey);

public sealed record EvaluationAnswerDto(
    Guid CriterionId,
    bool? YesNoValue,
    int? RatingValue,
    string? SelectedOptionValue,
    string? ShortTextValue);

public sealed record EvaluationResultVersionDto(
    int VersionNumber,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    EvaluationAttendance ChildAttendance,
    EvaluationAttendance ParentAttendance,
    IReadOnlyList<EvaluationAnswerDto> Answers,
    EvaluationRecommendation Recommendation,
    string? SuggestedParentReasonAr,
    string? SuggestedParentReasonEn,
    string? InternalNotes,
    DateTimeOffset FinalizedAtUtc,
    string? CorrectionReason,
    int? PreviousVersionNumber);

public sealed record EvaluationExecutionCapabilitiesDto(
    bool CanStartSession,
    bool CanSaveDraft,
    bool CanFinalize,
    bool CanRecordNoShow,
    bool CanBeginCorrection,
    bool CanUseAdmissionDecisionActions,
    DateTimeOffset? StartWindowOpensAtUtc,
    DateTimeOffset? NoShowAllowedAtUtc);

public sealed record AdmissionEvaluationResultDto(
    Guid Id,
    Guid AdmissionApplicationId,
    Guid AppointmentId,
    SlotKind Kind,
    EvaluationResultState State,
    DateTimeOffset StartedAtUtc,
    int CurrentFinalizedVersion,
    EvaluationTemplateDto? TemplateSnapshot,
    EvaluationAttendance ChildAttendance,
    EvaluationAttendance ParentAttendance,
    IReadOnlyList<EvaluationAnswerDto> DraftAnswers,
    EvaluationRecommendation Recommendation,
    string? SuggestedParentReasonAr,
    string? SuggestedParentReasonEn,
    string? InternalNotes,
    IReadOnlyList<EvaluationResultVersionDto> Versions,
    EvaluationExecutionCapabilitiesDto Capabilities,
    byte[] RowVersion);

public sealed record AdmissionEvaluationSessionContextDto(
    Guid AppointmentId,
    SlotKind Kind,
    AdmissionAppointmentLifecycle AppointmentLifecycle,
    EvaluationExecutionCapabilitiesDto Capabilities,
    EvaluationTemplateDto? ResolvedTemplate,
    AdmissionEvaluationResultDto? Result);
