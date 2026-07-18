namespace Schoolera.Application.Common.Models;

/// <summary>
/// Standard paging request for list endpoints. PageNumber is 1-based.
/// </summary>
public sealed record PagedRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public int NormalizedPageNumber => PageNumber < 1 ? 1 : PageNumber;

    public int NormalizedPageSize => PageSize < 1
        ? DefaultPageSize
        : Math.Min(PageSize, MaxPageSize);

    public int Skip => (NormalizedPageNumber - 1) * NormalizedPageSize;
}
