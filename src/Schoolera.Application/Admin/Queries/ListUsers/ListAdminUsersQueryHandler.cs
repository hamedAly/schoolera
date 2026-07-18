using MediatR;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admin.Queries.ListUsers;

public sealed record ListAdminUsersQuery(
    string? Search,
    string? Role,
    string? AccountStatus,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<AdminUserListItemDto>>>;

public sealed class ListAdminUsersQueryHandler(IAdminPlatformService adminPlatform)
    : IRequestHandler<ListAdminUsersQuery, Result<PagedResult<AdminUserListItemDto>>>
{
    public async Task<Result<PagedResult<AdminUserListItemDto>>> Handle(
        ListAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        AccountStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.AccountStatus) &&
            Enum.TryParse<AccountStatus>(request.AccountStatus, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var result = await adminPlatform.ListUsersAsync(
            request.Search,
            request.Role,
            status,
            paging,
            cancellationToken);
        return Result<PagedResult<AdminUserListItemDto>>.Success(result);
    }
}
