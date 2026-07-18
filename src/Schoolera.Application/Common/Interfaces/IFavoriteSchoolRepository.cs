using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface IFavoriteSchoolRepository
{
    Task<bool> SchoolExistsAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid parentUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetFavoriteSchoolIdsAsync(
        Guid parentUserId,
        IEnumerable<Guid> schoolIds,
        CancellationToken cancellationToken = default);

    Task<FavoriteSchool?> GetAsync(
        Guid parentUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task AddAsync(FavoriteSchool favorite, CancellationToken cancellationToken = default);

    Task RemoveAsync(FavoriteSchool favorite, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FavoriteSchoolListRow> Items, int TotalCount)> ListAsync(
        Guid parentUserId,
        PagedRequest paging,
        CancellationToken cancellationToken = default);
}

public sealed record FavoriteSchoolListRow(
    Guid FavoriteId,
    Guid SchoolId,
    DateTimeOffset CreatedAtUtc,
    SchoolStatus SchoolStatus,
    string SchoolSlug,
    string NameAr,
    string? NameEn);
