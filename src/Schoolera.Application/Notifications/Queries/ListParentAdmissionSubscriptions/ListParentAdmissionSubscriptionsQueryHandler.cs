using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Common;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Queries.ListParentAdmissionSubscriptions;

public sealed record ListParentAdmissionSubscriptionsQuery
    : IRequest<Result<IReadOnlyList<ParentAdmissionOpenSubscriptionDto>>>;

public sealed class ListParentAdmissionSubscriptionsQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<ListParentAdmissionSubscriptionsQuery, Result<IReadOnlyList<ParentAdmissionOpenSubscriptionDto>>>
{
    public async Task<Result<IReadOnlyList<ParentAdmissionOpenSubscriptionDto>>> Handle(
        ListParentAdmissionSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<IReadOnlyList<ParentAdmissionOpenSubscriptionDto>>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var items = await notificationRepository.ListSubscriptionsAsync(userId, cancellationToken);
        return Result<IReadOnlyList<ParentAdmissionOpenSubscriptionDto>>.Success(
            items.Select(ParentNotificationMapping.ToSubscriptionDto).ToArray());
    }
}
