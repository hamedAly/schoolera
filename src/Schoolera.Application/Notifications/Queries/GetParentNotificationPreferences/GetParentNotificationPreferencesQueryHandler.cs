using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Common;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Queries.GetParentNotificationPreferences;

public sealed record GetParentNotificationPreferencesQuery
    : IRequest<Result<ParentNotificationPreferenceDto>>;

public sealed class GetParentNotificationPreferencesQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetParentNotificationPreferencesQuery, Result<ParentNotificationPreferenceDto>>
{
    public async Task<Result<ParentNotificationPreferenceDto>> Handle(
        GetParentNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentNotificationPreferenceDto>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var preferences = await notificationRepository.GetPreferencesAsync(
            userId,
            cancellationToken);

        if (preferences is null)
        {
            return Result<ParentNotificationPreferenceDto>.Success(
                new ParentNotificationPreferenceDto(
                    Guid.Empty,
                    InAppEnabled: true,
                    EmailEnabled: true,
                    SmsEnabled: false,
                    WhatsAppEnabled: false,
                    OptionalAdmissionsOpenEnabled: true,
                    EmailConsentAtUtc: null,
                    SmsConsentAtUtc: null,
                    WhatsAppConsentAtUtc: null,
                    UpdatedAtUtc: DateTimeOffset.UtcNow,
                    RowVersion: []));
        }

        return Result<ParentNotificationPreferenceDto>.Success(
            ParentNotificationMapping.ToPreferenceDto(preferences));
    }
}
