using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;

namespace Schoolera.Application.Integrations.Queries.GetNotificationOpsSummary;

public sealed record GetNotificationOpsSummaryQuery
    : IRequest<Result<NotificationOpsSummaryDto>>;

public sealed class GetNotificationOpsSummaryQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetNotificationOpsSummaryQuery, Result<NotificationOpsSummaryDto>>
{
    public async Task<Result<NotificationOpsSummaryDto>> Handle(
        GetNotificationOpsSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<NotificationOpsSummaryDto>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var counts = await notificationRepository.GetOutboxCountsAsync(cancellationToken);
        return Result<NotificationOpsSummaryDto>.Success(
            new NotificationOpsSummaryDto(
                counts.Pending,
                counts.Failed,
                counts.DeadLetter,
                counts.Processing));
    }
}
