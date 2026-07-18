using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Common;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Commands.UnsubscribeParentAdmissionSubscription;

public sealed record UnsubscribeParentAdmissionSubscriptionCommand(Guid SubscriptionId)
    : IRequest<Result<ParentAdmissionOpenSubscriptionDto>>;

public sealed class UnsubscribeParentAdmissionSubscriptionCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<UnsubscribeParentAdmissionSubscriptionCommandHandler> logger)
    : IRequestHandler<UnsubscribeParentAdmissionSubscriptionCommand, Result<ParentAdmissionOpenSubscriptionDto>>
{
    public async Task<Result<ParentAdmissionOpenSubscriptionDto>> Handle(
        UnsubscribeParentAdmissionSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentAdmissionOpenSubscriptionDto>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var subscription = await notificationRepository.GetSubscriptionAsync(
            userId,
            request.SubscriptionId,
            cancellationToken);
        if (subscription is null)
        {
            return Result<ParentAdmissionOpenSubscriptionDto>.Failure(
                ["Subscription not found."],
                [ParentErrorCodes.SubscriptionNotFound]);
        }

        subscription.Unsubscribe();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Unsubscribed admission-open subscription {SubscriptionId} for parent {ParentUserId}.",
            request.SubscriptionId,
            userId);

        return Result<ParentAdmissionOpenSubscriptionDto>.Success(
            ParentNotificationMapping.ToSubscriptionDto(subscription));
    }
}
