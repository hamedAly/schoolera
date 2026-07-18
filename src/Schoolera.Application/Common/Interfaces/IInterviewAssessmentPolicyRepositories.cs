using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolInterviewAssessmentPolicyRepository
{
    Task<SchoolInterviewAssessmentPolicy?> GetByIdAsync(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken = default);

    Task<SchoolInterviewAssessmentPolicy?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolInterviewAssessmentPolicy>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        InterviewAssessmentPolicyPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolInterviewAssessmentPolicy>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string scopeKey,
        Guid? excludePolicyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicationSnapshotsReferencingAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationInterviewAssessmentPolicySnapshot?> GetApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SchoolInterviewAssessmentPolicy policy, CancellationToken cancellationToken = default);

    Task AddAuditAsync(SchoolInterviewAssessmentPolicyAudit audit, CancellationToken cancellationToken = default);

    Task RemoveApplicationSnapshotAsync(
        AdmissionApplicationInterviewAssessmentPolicySnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public interface IAdmissionInterviewAssessmentPolicySnapshotService
{
    /// <summary>
    /// Ensures a policy snapshot exists when a published applicable policy is found.
    /// Idempotent when <paramref name="forceReplace"/> is false and a snapshot already exists.
    /// No snapshot is created when no published policy matches.
    /// </summary>
    Task EnsureSnapshotAsync(
        AdmissionApplication application,
        Guid actorUserId,
        bool forceReplace = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasSnapshotAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOperationalRecordsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
