using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Queries.ListContactRequests;

public sealed record ListContactRequestsQuery(
    string? Search,
    string? Status,
    string? Category,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<ContactRequestListItemDto>>>;

public sealed class ListContactRequestsQueryHandler(
    ICmsRepository cmsRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListContactRequestsQuery, Result<PagedResult<ContactRequestListItemDto>>>
{
    public async Task<Result<PagedResult<ContactRequestListItemDto>>> Handle(
        ListContactRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<PagedResult<ContactRequestListItemDto>>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        ContactRequestStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ContactRequestStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var contacts = await cmsRepository.ListContactRequestsAsync(
            request.Search,
            status,
            request.Category,
            paging,
            cancellationToken);

        var items = contacts.Items
            .Select(CmsDtoMapping.ToListItem)
            .ToArray();

        return Result<PagedResult<ContactRequestListItemDto>>.Success(
            PagedResult<ContactRequestListItemDto>.Create(items, contacts.TotalCount, paging));
    }
}
