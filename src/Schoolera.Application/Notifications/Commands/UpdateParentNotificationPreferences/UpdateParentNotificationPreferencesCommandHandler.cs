using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Common;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;

namespace Schoolera.Application.Notifications.Commands.UpdateParentNotificationPreferences;

public sealed record UpdateParentNotificationPreferencesCommand(
    UpdateParentNotificationPreferencesRequest Body)
    : IRequest<Result<ParentNotificationPreferenceDto>>;

public sealed class UpdateParentNotificationPreferencesCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateParentNotificationPreferencesCommandHandler> logger)
    : IRequestHandler<UpdateParentNotificationPreferencesCommand, Result<ParentNotificationPreferenceDto>>
{
    public async Task<Result<ParentNotificationPreferenceDto>> Handle(
        UpdateParentNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentNotificationPreferenceDto>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var preferences = await notificationRepository.GetOrCreatePreferencesAsync(
            userId,
            cancellationToken);

        var body = request.Body;
        if (ParentNotificationMapping.HasRowVersionMismatch(body.RowVersion, preferences.RowVersion))
        {
            return Result<ParentNotificationPreferenceDto>.Failure(
                ["Preferences were modified by another operation."],
                [ParentErrorCodes.PreferenceConcurrency]);
        }

        preferences.Update(
            body.InAppEnabled,
            body.EmailEnabled,
            body.SmsEnabled,
            body.WhatsAppEnabled,
            body.OptionalAdmissionsOpenEnabled,
            body.ConsentSource);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated notification preferences for parent {ParentUserId}.", userId);

        return Result<ParentNotificationPreferenceDto>.Success(
            ParentNotificationMapping.ToPreferenceDto(preferences));
    }
}
