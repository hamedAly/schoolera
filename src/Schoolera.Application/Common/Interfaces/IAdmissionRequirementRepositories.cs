using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolAdmissionRequirementRepository
{
    Task<SchoolAdmissionRequirement?> GetByIdAsync(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken = default);

    Task<SchoolAdmissionRequirement?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolAdmissionRequirement>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        AdmissionRequirementKind? kind,
        AdmissionRequirementPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolAdmissionRequirement>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string requirementCode,
        string scopeKey,
        Guid? excludeRequirementId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SchoolAdmissionRequirement requirement, CancellationToken cancellationToken = default);

    Task AddAuditAsync(SchoolAdmissionRequirementAudit audit, CancellationToken cancellationToken = default);

    Task<bool> HasApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmissionApplicationRequirementSnapshot>> ListApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);
}

public interface IAdmissionRequirementSnapshotService
{
    /// <summary>
    /// Creates immutable snapshots when none exist (new draft or one-time backfill). Idempotent.
    /// </summary>
    Task EnsureSnapshotsAsync(
        AdmissionApplication application,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true when the application already has persisted requirement snapshots.</summary>
    Task<bool> HasSnapshotsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}

public interface IAdmissionRequirementCompletenessService
{
    Task<AdmissionRequirementsEvaluation> EvaluateAsync(
        AdmissionApplication application,
        string culture,
        CancellationToken cancellationToken = default);
}

public sealed record AdmissionRequirementsEvaluation(
    bool IsComplete,
    IReadOnlyList<MissingAdmissionRequirementItem> Missing);

public sealed record MissingAdmissionRequirementItem(
    Guid RequirementSnapshotId,
    string RequirementCode,
    AdmissionRequirementKind Kind,
    string DisplayName,
    string ReasonCode,
    string WizardSection,
    AdmissionRequiredDocumentCode? DocumentCode);
