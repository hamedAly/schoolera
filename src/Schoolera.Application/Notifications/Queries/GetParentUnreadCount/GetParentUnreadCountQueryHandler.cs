using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Queries.GetParentUnreadCount;

public sealed record GetParentUnreadCountQuery : IRequest<Result<int>>;

public sealed class GetParentUnreadCountQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetParentUnreadCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetParentUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<int>.Failure(["Forbidden."], [ParentErrorCodes.Forbidden]);
        }

        var count = await notificationRepository.CountParentUnreadAsync(userId, cancellationToken);
        return Result<int>.Success(count);
    }
}
