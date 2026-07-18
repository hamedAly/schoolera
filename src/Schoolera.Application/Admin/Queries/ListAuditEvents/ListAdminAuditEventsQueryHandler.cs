using MediatR;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Queries.ListAuditEvents;

public sealed record ListAdminAuditEventsQuery(
    string? Action,
    string? EntityType,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<AdminAuditEventDto>>>;

public sealed class ListAdminAuditEventsQueryHandler(IAdminPlatformService adminPlatform)
    : IRequestHandler<ListAdminAuditEventsQuery, Result<PagedResult<AdminAuditEventDto>>>
{
    public async Task<Result<PagedResult<AdminAuditEventDto>>> Handle(
        ListAdminAuditEventsQuery request,
        CancellationToken cancellationToken)
    {
        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var result = await adminPlatform.ListAuditEventsAsync(
            request.Action,
            request.EntityType,
            paging,
            cancellationToken);
        return Result<PagedResult<AdminAuditEventDto>>.Success(result);
    }
}
