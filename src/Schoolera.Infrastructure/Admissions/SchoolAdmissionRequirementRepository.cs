using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Infrastructure.Admissions;

public sealed class SchoolAdmissionRequirementRepository(SchooleraDbContext dbContext)
    : ISchoolAdmissionRequirementRepository
{
    public Task<SchoolAdmissionRequirement?> GetByIdAsync(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionRequirements
            .AsNoTracking()
            .FirstOrDefaultAsync(
                requirement => requirement.SchoolId == schoolId && requirement.Id == requirementId,
                cancellationToken);

    public Task<SchoolAdmissionRequirement?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid requirementId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionRequirements
            .FirstOrDefaultAsync(
                requirement => requirement.SchoolId == schoolId && requirement.Id == requirementId,
                cancellationToken);

    public async Task<IReadOnlyList<SchoolAdmissionRequirement>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        AdmissionRequirementKind? kind,
        AdmissionRequirementPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SchoolAdmissionRequirements
            .AsNoTracking()
            .Where(requirement => requirement.SchoolId == schoolId);

        if (branchId is { } branch)
        {
            query = query.Where(requirement => requirement.SchoolBranchId == null || requirement.SchoolBranchId == branch);
        }

        if (stageId is { } stage)
        {
            query = query.Where(requirement =>
                requirement.EducationalStageId == null || requirement.EducationalStageId == stage);
        }

        if (gradeId is { } grade)
        {
            query = query.Where(requirement => requirement.GradeId == null || requirement.GradeId == grade);
        }

        if (academicYearId is { } year)
        {
            query = query.Where(requirement =>
                requirement.AcademicYearId == null || requirement.AcademicYearId == year);
        }

        if (kind is { } requirementKind)
        {
            query = query.Where(requirement => requirement.Kind == requirementKind);
        }

        if (publicationStatus is { } status)
        {
            query = query.Where(requirement => requirement.PublicationStatus == status);
        }

        if (isActive is { } active)
        {
            query = query.Where(requirement => requirement.IsActive == active);
        }

        return await query
            .OrderBy(requirement => requirement.SortOrder)
            .ThenBy(requirement => requirement.RequirementCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolAdmissionRequirement>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SchoolAdmissionRequirements
            .AsNoTracking()
            .Where(requirement =>
                requirement.SchoolId == schoolId &&
                requirement.IsActive &&
                requirement.PublicationStatus == AdmissionRequirementPublicationStatus.Published)
            .OrderBy(requirement => requirement.SortOrder)
            .ThenBy(requirement => requirement.RequirementCode)
            .ToListAsync(cancellationToken);

    public Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string requirementCode,
        string scopeKey,
        Guid? excludeRequirementId,
        CancellationToken cancellationToken = default)
    {
        var code = requirementCode.Trim().ToLowerInvariant();
        return dbContext.SchoolAdmissionRequirements.AnyAsync(
            requirement =>
                requirement.SchoolId == schoolId &&
                requirement.RequirementCode == code &&
                requirement.ScopeKey == scopeKey &&
                requirement.IsActive &&
                requirement.PublicationStatus == AdmissionRequirementPublicationStatus.Published &&
                (excludeRequirementId == null || requirement.Id != excludeRequirementId),
            cancellationToken);
    }

    public Task AddAsync(SchoolAdmissionRequirement requirement, CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionRequirements.AddAsync(requirement, cancellationToken).AsTask();

    public Task AddAuditAsync(SchoolAdmissionRequirementAudit audit, CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionRequirementAudits.AddAsync(audit, cancellationToken).AsTask();

    public Task<bool> HasApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationRequirementSnapshots.AnyAsync(
            snapshot => snapshot.AdmissionApplicationId == admissionApplicationId,
            cancellationToken);

    public async Task<IReadOnlyList<AdmissionApplicationRequirementSnapshot>> ListApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.AdmissionApplicationRequirementSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.AdmissionApplicationId == admissionApplicationId)
            .OrderBy(snapshot => snapshot.SortOrder)
            .ThenBy(snapshot => snapshot.RequirementCode)
            .ToListAsync(cancellationToken);
}
