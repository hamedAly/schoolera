using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admissions;

public sealed class SchoolChildAgeEligibilityRuleRepository(SchooleraDbContext dbContext)
    : ISchoolChildAgeEligibilityRuleRepository
{
    public Task<SchoolChildAgeEligibilityRule?> GetByIdAsync(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolChildAgeEligibilityRules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                rule => rule.SchoolId == schoolId && rule.Id == ruleId,
                cancellationToken);

    public Task<SchoolChildAgeEligibilityRule?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolChildAgeEligibilityRules
            .FirstOrDefaultAsync(
                rule => rule.SchoolId == schoolId && rule.Id == ruleId,
                cancellationToken);

    public async Task<IReadOnlyList<SchoolChildAgeEligibilityRule>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        ChildAgeEligibilityPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SchoolChildAgeEligibilityRules
            .AsNoTracking()
            .Where(rule => rule.SchoolId == schoolId);

        if (branchId is { } branch)
        {
            query = query.Where(rule => rule.SchoolBranchId == null || rule.SchoolBranchId == branch);
        }

        if (stageId is { } stage)
        {
            query = query.Where(rule => rule.EducationalStageId == stage);
        }

        if (gradeId is { } grade)
        {
            query = query.Where(rule => rule.GradeId == null || rule.GradeId == grade);
        }

        if (academicYearId is { } year)
        {
            query = query.Where(rule => rule.AcademicYearId == year);
        }

        if (publicationStatus is { } status)
        {
            query = query.Where(rule => rule.PublicationStatus == status);
        }

        if (isActive is { } active)
        {
            query = query.Where(rule => rule.IsActive == active);
        }

        return await query
            .OrderByDescending(rule => rule.UpdatedAtUtc)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolChildAgeEligibilityRule>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SchoolChildAgeEligibilityRules
            .AsNoTracking()
            .Where(rule =>
                rule.SchoolId == schoolId &&
                rule.IsActive &&
                rule.PublicationStatus == ChildAgeEligibilityPublicationStatus.Published)
            .OrderByDescending(rule => rule.UpdatedAtUtc)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string scopeKey,
        Guid? excludeRuleId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolChildAgeEligibilityRules.AnyAsync(
            rule =>
                rule.SchoolId == schoolId &&
                rule.ScopeKey == scopeKey &&
                rule.IsActive &&
                rule.PublicationStatus == ChildAgeEligibilityPublicationStatus.Published &&
                (excludeRuleId == null || rule.Id != excludeRuleId),
            cancellationToken);

    public Task<bool> HasApplicationSnapshotsReferencingAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationChildAgeEligibilitySnapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.SourceRuleId == ruleId, cancellationToken);

    public Task<bool> HasApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationChildAgeEligibilitySnapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.AdmissionApplicationId == admissionApplicationId, cancellationToken);

    public Task<AdmissionApplicationChildAgeEligibilitySnapshot?> GetApplicationSnapshotAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationChildAgeEligibilitySnapshots
            .FirstOrDefaultAsync(
                snapshot => snapshot.AdmissionApplicationId == admissionApplicationId,
                cancellationToken);

    public Task AddAsync(SchoolChildAgeEligibilityRule rule, CancellationToken cancellationToken = default) =>
        dbContext.SchoolChildAgeEligibilityRules.AddAsync(rule, cancellationToken).AsTask();

    public Task AddAuditAsync(SchoolChildAgeEligibilityRuleAudit audit, CancellationToken cancellationToken = default) =>
        dbContext.SchoolChildAgeEligibilityRuleAudits.AddAsync(audit, cancellationToken).AsTask();

    public Task RemoveApplicationSnapshotAsync(
        AdmissionApplicationChildAgeEligibilitySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        dbContext.AdmissionApplicationChildAgeEligibilitySnapshots.Remove(snapshot);
        return Task.CompletedTask;
    }
}
