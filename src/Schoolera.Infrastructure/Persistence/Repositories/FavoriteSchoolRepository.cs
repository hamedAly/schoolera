using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class FavoriteSchoolRepository(SchooleraDbContext dbContext) : IFavoriteSchoolRepository
{
    public Task<bool> SchoolExistsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        dbContext.Schools.AsNoTracking().AnyAsync(school => school.Id == schoolId, cancellationToken);

    public Task<bool> ExistsAsync(
        Guid parentUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        dbContext.FavoriteSchools.AsNoTracking()
            .AnyAsync(
                favorite => favorite.ParentUserId == parentUserId && favorite.SchoolId == schoolId,
                cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetFavoriteSchoolIdsAsync(
        Guid parentUserId,
        IEnumerable<Guid> schoolIds,
        CancellationToken cancellationToken = default)
    {
        var idSet = schoolIds.Distinct().ToArray();
        if (idSet.Length == 0)
        {
            return new HashSet<Guid>();
        }

        var matches = await dbContext.FavoriteSchools.AsNoTracking()
            .Where(favorite => favorite.ParentUserId == parentUserId && idSet.Contains(favorite.SchoolId))
            .Select(favorite => favorite.SchoolId)
            .ToListAsync(cancellationToken);

        return matches.ToHashSet();
    }

    public Task<FavoriteSchool?> GetAsync(
        Guid parentUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        dbContext.FavoriteSchools
            .FirstOrDefaultAsync(
                favorite => favorite.ParentUserId == parentUserId && favorite.SchoolId == schoolId,
                cancellationToken);

    public async Task AddAsync(FavoriteSchool favorite, CancellationToken cancellationToken = default) =>
        await dbContext.FavoriteSchools.AddAsync(favorite, cancellationToken);

    public Task RemoveAsync(FavoriteSchool favorite, CancellationToken cancellationToken = default)
    {
        dbContext.FavoriteSchools.Remove(favorite);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<FavoriteSchoolListRow> Items, int TotalCount)> ListAsync(
        Guid parentUserId,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FavoriteSchools.AsNoTracking()
            .Where(favorite => favorite.ParentUserId == parentUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(favorite => favorite.CreatedAtUtc)
            .ThenBy(favorite => favorite.Id)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Join(
                dbContext.Schools.AsNoTracking(),
                favorite => favorite.SchoolId,
                school => school.Id,
                (favorite, school) => new FavoriteSchoolListRow(
                    favorite.Id,
                    school.Id,
                    favorite.CreatedAtUtc,
                    school.Status,
                    school.Slug,
                    school.NameAr,
                    school.NameEn))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
