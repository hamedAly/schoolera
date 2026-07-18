using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;

namespace Schoolera.Application.Integrations.Queries.ListNotificationTemplates;

public sealed record ListNotificationTemplatesQuery
    : IRequest<Result<IReadOnlyList<NotificationTemplateListItemDto>>>;

public sealed class ListNotificationTemplatesQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<ListNotificationTemplatesQuery, Result<IReadOnlyList<NotificationTemplateListItemDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationTemplateListItemDto>>> Handle(
        ListNotificationTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<NotificationTemplateListItemDto>>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var templates = await notificationRepository.ListTemplatesAsync(cancellationToken);
        return Result<IReadOnlyList<NotificationTemplateListItemDto>>.Success(
            templates.Select(IntegrationMapping.ToTemplateListItem).ToArray());
    }
}
