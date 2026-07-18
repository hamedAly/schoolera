using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class SchoolOnboardingRepository(SchooleraDbContext dbContext) : ISchoolOnboardingRepository
{
    public async Task<SchoolOnboardingApplication?> GetByOwnerAsync(
        Guid ownerUserId,
        bool includeChildren,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery(includeChildren);
        return await query
            .Where(application => application.OwnerUserId == ownerUserId)
            .OrderByDescending(application => application.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SchoolOnboardingApplication?> GetByIdAsync(
        Guid id,
        bool includeChildren,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery(includeChildren);
        return await query.FirstOrDefaultAsync(application => application.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        SchoolOnboardingApplication application,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SchoolOnboardingApplications.AddAsync(application, cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolOnboardingDocumentType>> ListActiveDocumentTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolOnboardingDocumentTypes
            .AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.NameEn)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SchoolOnboardingDocumentType?> GetDocumentTypeByIdAsync(
        Guid documentTypeId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolOnboardingDocumentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Id == documentTypeId, cancellationToken);
    }

    public async Task<SchoolOnboardingDocument?> GetDocumentByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolOnboardingDocuments
            .AsNoTracking()
            .Include(document => document.Application)
            .FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);
    }

    public async Task<bool> RegistrationNumberExistsAsync(
        string normalizedRegistrationNumber,
        string countryCode,
        Guid excludeApplicationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolOnboardingApplications
            .AsNoTracking()
            .AnyAsync(
                application =>
                    application.Id != excludeApplicationId &&
                    application.Status != SchoolOnboardingStatus.Rejected &&
                    application.CountryCode == countryCode &&
                    application.NormalizedRegistrationNumber == normalizedRegistrationNumber,
                cancellationToken);
    }

    public async Task<bool> DistrictBelongsToCityAsync(
        Guid districtId,
        Guid cityId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Districts
            .AsNoTracking()
            .AnyAsync(district => district.Id == districtId && district.CityId == cityId, cancellationToken);
    }

    public async Task<string?> GetSchoolNameAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Schools
            .AsNoTracking()
            .Where(school => school.Id == schoolId)
            .Select(school => school.NameAr)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<SchoolOnboardingApplication> Items, int TotalCount)> SearchAsync(
        SchoolOnboardingStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SchoolOnboardingApplications.AsNoTracking();
        if (status is { } value)
        {
            query = query.Where(application => application.Status == value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(application => application.SubmittedAtUtc ?? application.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<SchoolOnboardingApplication> BaseQuery(bool includeChildren)
    {
        var query = dbContext.SchoolOnboardingApplications.AsQueryable();
        if (includeChildren)
        {
            query = query
                .Include(application => application.Documents)
                .Include(application => application.StatusHistory);
        }

        return query;
    }
}
