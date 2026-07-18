using Schoolera.Application.Common.Models;

namespace Schoolera.Tests;

public sealed class PagingModelTests
{
    [Fact]
    public void PagedRequest_NormalizesInvalidValues()
    {
        var request = new PagedRequest(PageNumber: 0, PageSize: 500);

        Assert.Equal(1, request.NormalizedPageNumber);
        Assert.Equal(PagedRequest.MaxPageSize, request.NormalizedPageSize);
        Assert.Equal(0, request.Skip);
    }

    [Fact]
    public void PagedResult_ComputesNavigationFlags()
    {
        var request = new PagedRequest(2, 10);
        var result = PagedResult<string>.Create(["a", "b"], totalCount: 25, request);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }
}
