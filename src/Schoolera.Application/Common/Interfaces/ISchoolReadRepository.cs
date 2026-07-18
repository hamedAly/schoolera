using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolReadRepository
{
    Task<School?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicSchoolSearchProjection>?> GetRelatedPublishedAsync(
        string slug,
        int limit,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PublicSchoolSearchProjection> Items, int TotalCount)> SearchPublishedAsync(
        PagedRequest paging,
        PublicSchoolListFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds public list-card projections for the given school IDs that are currently Published.
    /// Order follows <paramref name="orderedIds"/>; missing/non-published IDs are omitted.
    /// </summary>
    Task<IReadOnlyList<PublicSchoolSearchProjection>> GetPublishedListCardsByIdsAsync(
        IReadOnlyList<Guid> orderedIds,
        CancellationToken cancellationToken = default);

    Task<School?> GetBySlugAsync(
        string slug,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<PagedResult<School>> ListAsync(
        SchoolCatalogFilters filters,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeSchoolId = null,
        CancellationToken cancellationToken = default);
}
