using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolAdmissionQuestionRepository
{
    Task<SchoolAdmissionQuestion?> GetByIdAsync(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task<SchoolAdmissionQuestion?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolAdmissionQuestion>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        AdmissionQuestionType? questionType,
        AdmissionQuestionPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolAdmissionQuestion>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string questionCode,
        string scopeKey,
        Guid? excludeQuestionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SchoolAdmissionQuestion question, CancellationToken cancellationToken = default);

    Task AddAuditAsync(SchoolAdmissionQuestionAudit audit, CancellationToken cancellationToken = default);

    Task<bool> HasApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicationAnswersAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmissionApplicationQuestionSnapshot>> ListApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);
}

public interface IAdmissionQuestionSnapshotService
{
    /// <summary>
    /// Creates immutable question snapshots when none exist, or replaces them when <paramref name="forceReplace"/> is true.
    /// </summary>
    Task EnsureSnapshotsAsync(
        AdmissionApplication application,
        Guid actorUserId,
        bool forceReplace = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasSnapshotsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnswersAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}

public interface IAdmissionQuestionCompletenessService
{
    Task<AdmissionQuestionsEvaluation> EvaluateAsync(
        AdmissionApplication application,
        string culture,
        CancellationToken cancellationToken = default);
}

public sealed record AdmissionQuestionsEvaluation(
    bool IsComplete,
    IReadOnlyList<MissingAdmissionQuestionItem> Missing);

public sealed record MissingAdmissionQuestionItem(
    Guid QuestionSnapshotId,
    string QuestionCode,
    AdmissionQuestionType QuestionType,
    string DisplayName,
    string ReasonCode,
    string WizardSection);
