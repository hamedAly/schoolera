using Schoolera.Application.Admissions.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface IAdmissionEvaluationRepository
{
    Task<IReadOnlyList<SchoolAdmissionEvaluationTemplate>> ListTemplatesAsync(
        Guid schoolId, EvaluationTemplateListQuery query, CancellationToken cancellationToken = default);

    Task<SchoolAdmissionEvaluationTemplate?> GetTemplateAsync(
        Guid schoolId, Guid templateId, bool tracking, CancellationToken cancellationToken = default);

    Task AddTemplateAsync(
        SchoolAdmissionEvaluationTemplate template, CancellationToken cancellationToken = default);

    Task AddTemplateAuditAsync(
        SchoolAdmissionEvaluationTemplateAudit audit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolAdmissionEvaluationTemplateAudit>> ListTemplateAuditAsync(
        Guid schoolId, Guid templateId, CancellationToken cancellationToken = default);

    Task<SchoolAdmissionEvaluationTemplateAudit?> GetTemplateAuditByKeyAsync(
        Guid schoolId, string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<bool> ScopeIsValidAsync(
        Guid schoolId, Guid? branchId, Guid? stageId, Guid? gradeId, Guid? academicYearId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPublishedConflictAsync(
        Guid schoolId, string scopeKey, EvaluationTemplateKind kind, Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<EvaluationJourneyOutcome> ExecuteJourneyAsync(
        EvaluationJourneyRequest request, CancellationToken cancellationToken = default);

    Task<AdmissionEvaluationSessionContextDto?> GetContextAsync(
        Guid schoolId, Guid applicationId, SlotKind kind, Guid evaluatorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasFinalizedRequiredEvaluationAsync(
        Guid applicationId, AdmissionApplicationStatus status,
        CancellationToken cancellationToken = default);
}

public enum EvaluationJourneyAction
{
    Start = 1,
    SaveDraft = 2,
    Finalize = 3,
    NoShow = 4,
    BeginCorrection = 5,
}

public enum EvaluationJourneyResult
{
    Succeeded = 1,
    Existing = 2,
    NotFound = 3,
    InvalidTransition = 4,
    MissingTemplate = 5,
    InvalidAttendance = 6,
    InvalidAnswers = 7,
    InvalidRecommendation = 8,
    TooEarly = 9,
    ConcurrencyConflict = 10,
    IdempotencyConflict = 11,
    CorrectionBlocked = 12,
}

public sealed record EvaluationJourneyRequest(
    Guid SchoolId,
    Guid ApplicationId,
    SlotKind Kind,
    Guid EvaluatorUserId,
    EvaluationJourneyAction Action,
    byte[] RowVersion,
    string IdempotencyKey,
    SaveEvaluationDraftRequest? Draft,
    string? CorrectionReason);

public sealed record EvaluationJourneyOutcome(
    EvaluationJourneyResult Result,
    AdmissionEvaluationResultDto? Data = null);
