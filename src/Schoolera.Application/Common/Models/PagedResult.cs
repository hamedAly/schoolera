namespace Schoolera.Application.Common.Models;

/// <summary>
/// Standard paged result for list endpoints.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        PagedRequest request)
    {
        return new PagedResult<T>(
            items,
            totalCount,
            request.NormalizedPageNumber,
            request.NormalizedPageSize);
    }
}
