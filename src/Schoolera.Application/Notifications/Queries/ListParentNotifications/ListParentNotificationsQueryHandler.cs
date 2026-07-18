using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Common;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Queries.ListParentNotifications;

public sealed record ListParentNotificationsQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ParentInAppNotificationDto>>>;

public sealed class ListParentNotificationsQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<ListParentNotificationsQuery, Result<PagedResult<ParentInAppNotificationDto>>>
{
    public async Task<Result<PagedResult<ParentInAppNotificationDto>>> Handle(
        ListParentNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<PagedResult<ParentInAppNotificationDto>>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var items = await notificationRepository.ListParentInAppAsync(
            userId,
            paging.NormalizedPageNumber,
            paging.NormalizedPageSize,
            cancellationToken);
        var total = await notificationRepository.CountParentInAppAsync(userId, cancellationToken);

        return Result<PagedResult<ParentInAppNotificationDto>>.Success(
            PagedResult<ParentInAppNotificationDto>.Create(
                items.Select(ParentNotificationMapping.ToInAppDto).ToArray(),
                total,
                paging));
    }
}
