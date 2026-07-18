using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Common;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Commands.MarkParentNotificationRead;

public sealed record MarkParentNotificationReadCommand(Guid NotificationId)
    : IRequest<Result<ParentInAppNotificationDto>>;

public sealed class MarkParentNotificationReadCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<MarkParentNotificationReadCommandHandler> logger)
    : IRequestHandler<MarkParentNotificationReadCommand, Result<ParentInAppNotificationDto>>
{
    public async Task<Result<ParentInAppNotificationDto>> Handle(
        MarkParentNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentInAppNotificationDto>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var message = await notificationRepository.GetParentInAppAsync(
            userId,
            request.NotificationId,
            cancellationToken);
        if (message is null)
        {
            return Result<ParentInAppNotificationDto>.Failure(
                ["Notification not found."],
                [ParentErrorCodes.NotificationNotFound]);
        }

        message.MarkRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked parent notification {NotificationId} as read for {ParentUserId}.",
            request.NotificationId,
            userId);

        return Result<ParentInAppNotificationDto>.Success(
            ParentNotificationMapping.ToInAppDto(message));
    }
}
