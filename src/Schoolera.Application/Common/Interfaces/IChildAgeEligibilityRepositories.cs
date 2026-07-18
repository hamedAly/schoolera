using Schoolera.Application.Admissions.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolChildAgeEligibilityRuleRepository
{
    Task<SchoolChildAgeEligibilityRule?> GetByIdAsync(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task<SchoolChildAgeEligibilityRule?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolChildAgeEligibilityRule>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        ChildAgeEligibilityPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolChildAgeEligibilityRule>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string scopeKey,
        Guid? excludeRuleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicationSnapshotsReferencingAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationChildAgeEligibilitySnapshot?> GetApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SchoolChildAgeEligibilityRule rule, CancellationToken cancellationToken = default);

    Task AddAuditAsync(SchoolChildAgeEligibilityRuleAudit audit, CancellationToken cancellationToken = default);

    Task RemoveApplicationSnapshotAsync(
        AdmissionApplicationChildAgeEligibilitySnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public interface IAdmissionChildAgeEligibilitySnapshotService
{
    /// <summary>
    /// Ensures an age eligibility snapshot exists when a published applicable rule is found.
    /// Idempotent when <paramref name="forceReplace"/> is false and a snapshot already exists.
    /// No snapshot is created when no published rule matches (RuleNotConfigured — does not block drafts).
    /// On forceReplace with an approved manual exception, clears the exception and records Invalidated history.
    /// </summary>
    Task EnsureSnapshotAsync(
        AdmissionApplication application,
        Guid actorUserId,
        bool forceReplace = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasSnapshotAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-evaluates live birth date, updates or creates snapshot, and returns the evaluation
    /// (honoring an existing valid manual exception when still NotEligible*).
    /// </summary>
    Task<ChildAgeEligibilityEvaluation> RecalculateForSubmitAsync(
        AdmissionApplication application,
        DateOnly? birthDate,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
