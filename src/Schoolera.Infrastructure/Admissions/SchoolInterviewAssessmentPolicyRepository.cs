using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admissions;

public sealed class SchoolInterviewAssessmentPolicyRepository(SchooleraDbContext dbContext)
    : ISchoolInterviewAssessmentPolicyRepository
{
    public Task<SchoolInterviewAssessmentPolicy?> GetByIdAsync(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolInterviewAssessmentPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                policy => policy.SchoolId == schoolId && policy.Id == policyId,
                cancellationToken);

    public Task<SchoolInterviewAssessmentPolicy?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid policyId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolInterviewAssessmentPolicies
            .FirstOrDefaultAsync(
                policy => policy.SchoolId == schoolId && policy.Id == policyId,
                cancellationToken);

    public async Task<IReadOnlyList<SchoolInterviewAssessmentPolicy>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        InterviewAssessmentPolicyPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SchoolInterviewAssessmentPolicies
            .AsNoTracking()
            .Where(policy => policy.SchoolId == schoolId);

        if (branchId is { } branch)
        {
            query = query.Where(policy => policy.SchoolBranchId == null || policy.SchoolBranchId == branch);
        }

        if (stageId is { } stage)
        {
            query = query.Where(policy =>
                policy.EducationalStageId == null || policy.EducationalStageId == stage);
        }

        if (gradeId is { } grade)
        {
            query = query.Where(policy => policy.GradeId == null || policy.GradeId == grade);
        }

        if (academicYearId is { } year)
        {
            query = query.Where(policy =>
                policy.AcademicYearId == null || policy.AcademicYearId == year);
        }

        if (publicationStatus is { } status)
        {
            query = query.Where(policy => policy.PublicationStatus == status);
        }

        if (isActive is { } active)
        {
            query = query.Where(policy => policy.IsActive == active);
        }

        return await query
            .OrderByDescending(policy => policy.UpdatedAtUtc)
            .ThenBy(policy => policy.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolInterviewAssessmentPolicy>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SchoolInterviewAssessmentPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.SchoolId == schoolId &&
                policy.IsActive &&
                policy.PublicationStatus == InterviewAssessmentPolicyPublicationStatus.Published)
            .OrderByDescending(policy => policy.UpdatedAtUtc)
            .ThenBy(policy => policy.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string scopeKey,
        Guid? excludePolicyId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolInterviewAssessmentPolicies.AnyAsync(
            policy =>
                policy.SchoolId == schoolId &&
                policy.ScopeKey == scopeKey &&
                policy.IsActive &&
                policy.PublicationStatus == InterviewAssessmentPolicyPublicationStatus.Published &&
                (excludePolicyId == null || policy.Id != excludePolicyId),
            cancellationToken);

    public Task<bool> HasApplicationSnapshotsReferencingAsync(
        Guid policyId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationInterviewAssessmentPolicySnapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.SourcePolicyId == policyId, cancellationToken);

    public Task<bool> HasApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationInterviewAssessmentPolicySnapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.AdmissionApplicationId == admissionApplicationId, cancellationToken);

    public Task<AdmissionApplicationInterviewAssessmentPolicySnapshot?> GetApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationInterviewAssessmentPolicySnapshots
            .FirstOrDefaultAsync(
                snapshot => snapshot.AdmissionApplicationId == admissionApplicationId,
                cancellationToken);

    public Task AddAsync(SchoolInterviewAssessmentPolicy policy, CancellationToken cancellationToken = default) =>
        dbContext.SchoolInterviewAssessmentPolicies.AddAsync(policy, cancellationToken).AsTask();

    public Task AddAuditAsync(SchoolInterviewAssessmentPolicyAudit audit, CancellationToken cancellationToken = default) =>
        dbContext.SchoolInterviewAssessmentPolicyAudits.AddAsync(audit, cancellationToken).AsTask();

    public Task RemoveApplicationSnapshotAsync(
        AdmissionApplicationInterviewAssessmentPolicySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        dbContext.AdmissionApplicationInterviewAssessmentPolicySnapshots.Remove(snapshot);
        return Task.CompletedTask;
    }
}
