using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;

namespace Schoolera.Application.Integrations.Commands.PublishNotificationTemplateVersion;

public sealed record PublishNotificationTemplateVersionCommand(Guid VersionId)
    : IRequest<Result<NotificationTemplateVersionDto>>;

public sealed class PublishNotificationTemplateVersionCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<PublishNotificationTemplateVersionCommandHandler> logger)
    : IRequestHandler<PublishNotificationTemplateVersionCommand, Result<NotificationTemplateVersionDto>>
{
    public async Task<Result<NotificationTemplateVersionDto>> Handle(
        PublishNotificationTemplateVersionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<NotificationTemplateVersionDto>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var version = await notificationRepository.GetTemplateVersionAsync(
            request.VersionId,
            cancellationToken);
        if (version is null)
        {
            return Result<NotificationTemplateVersionDto>.Failure(
                ["Template version not found."],
                [IntegrationErrorCodes.TemplateVersionNotFound]);
        }

        if (version.IsPublished)
        {
            return Result<NotificationTemplateVersionDto>.Success(
                IntegrationMapping.ToTemplateVersion(version));
        }

        version.Publish();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.TemplateVersionPublished,
            "NotificationTemplateVersion",
            version.Id.ToString(),
            $"Published template version {version.VersionNumber} for {version.Template.Code}.",
            cancellationToken);

        logger.LogInformation("Published notification template version {VersionId}.", version.Id);

        return Result<NotificationTemplateVersionDto>.Success(
            IntegrationMapping.ToTemplateVersion(version));
    }
}
