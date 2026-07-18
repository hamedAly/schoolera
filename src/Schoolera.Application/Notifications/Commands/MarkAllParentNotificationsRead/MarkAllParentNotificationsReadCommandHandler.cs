using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Commands.MarkAllParentNotificationsRead;

public sealed record MarkAllParentNotificationsReadCommand : IRequest<Result<int>>;

public sealed class MarkAllParentNotificationsReadCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<MarkAllParentNotificationsReadCommandHandler> logger)
    : IRequestHandler<MarkAllParentNotificationsReadCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        MarkAllParentNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<int>.Failure(["Forbidden."], [ParentErrorCodes.Forbidden]);
        }

        var unreadBefore = await notificationRepository.CountParentUnreadAsync(
            userId,
            cancellationToken);
        await notificationRepository.MarkAllParentInAppReadAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked all parent notifications as read for {ParentUserId} ({Count} previously unread).",
            userId,
            unreadBefore);

        return Result<int>.Success(unreadBefore);
    }
}
