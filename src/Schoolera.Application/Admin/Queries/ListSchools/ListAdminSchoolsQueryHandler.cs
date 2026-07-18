using MediatR;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admin.Queries.ListSchools;

public sealed record ListAdminSchoolsQuery(
    string? Search,
    string? Status,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<AdminSchoolListItemDto>>>;

public sealed class ListAdminSchoolsQueryHandler(IAdminPlatformService adminPlatform)
    : IRequestHandler<ListAdminSchoolsQuery, Result<PagedResult<AdminSchoolListItemDto>>>
{
    public async Task<Result<PagedResult<AdminSchoolListItemDto>>> Handle(
        ListAdminSchoolsQuery request,
        CancellationToken cancellationToken)
    {
        SchoolStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<SchoolStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var result = await adminPlatform.ListSchoolsAsync(request.Search, status, paging, cancellationToken);
        return Result<PagedResult<AdminSchoolListItemDto>>.Success(result);
    }
}
