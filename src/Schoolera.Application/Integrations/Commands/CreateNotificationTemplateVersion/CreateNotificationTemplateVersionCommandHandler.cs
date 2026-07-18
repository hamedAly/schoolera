using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Integrations.Commands.CreateNotificationTemplateVersion;

public sealed record CreateNotificationTemplateVersionCommand(CreateTemplateVersionRequest Body)
    : IRequest<Result<NotificationTemplateVersionDto>>;

public sealed class CreateNotificationTemplateVersionCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<CreateNotificationTemplateVersionCommandHandler> logger)
    : IRequestHandler<CreateNotificationTemplateVersionCommand, Result<NotificationTemplateVersionDto>>
{
    public async Task<Result<NotificationTemplateVersionDto>> Handle(
        CreateNotificationTemplateVersionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<NotificationTemplateVersionDto>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var body = request.Body;
        if (!Enum.IsDefined(body.EventType) || !Enum.IsDefined(body.Channel))
        {
            return Result<NotificationTemplateVersionDto>.Failure(
                ["Invalid template event or channel."],
                [IntegrationErrorCodes.InvalidType]);
        }

        var culture = body.Culture.Trim().ToLowerInvariant();
        if (culture is not ("ar" or "en"))
        {
            return Result<NotificationTemplateVersionDto>.Failure(
                ["Culture must be ar or en."],
                [IntegrationErrorCodes.ValidationFailed]);
        }

        if (string.IsNullOrWhiteSpace(body.Body))
        {
            return Result<NotificationTemplateVersionDto>.Failure(
                ["Template body is required."],
                [IntegrationErrorCodes.ValidationFailed]);
        }

        var template = await notificationRepository.GetTemplateAsync(
            body.EventType,
            body.Channel,
            culture,
            cancellationToken);

        if (template is null)
        {
            var code = string.IsNullOrWhiteSpace(body.Code)
                ? $"{body.EventType}.{body.Channel}.{culture}".ToLowerInvariant()
                : body.Code.Trim();
            template = new NotificationTemplate(body.EventType, body.Channel, culture, code);
            await notificationRepository.AddTemplateAsync(template, cancellationToken);
        }

        NotificationTemplateVersion version;
        try
        {
            version = template.AddVersion(
                body.Subject,
                body.Body,
                body.AllowedVariablesCsv ?? string.Empty,
                body.ProviderTemplateId,
                actorId);
        }
        catch (ArgumentException)
        {
            return Result<NotificationTemplateVersionDto>.Failure(
                ["Invalid template version payload."],
                [IntegrationErrorCodes.ValidationFailed]);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.TemplateVersionCreated,
            "NotificationTemplateVersion",
            version.Id.ToString(),
            $"Created template version {version.VersionNumber} for {template.Code}.",
            cancellationToken);

        logger.LogInformation(
            "Created notification template version {VersionId} for template {TemplateId}.",
            version.Id,
            template.Id);

        return Result<NotificationTemplateVersionDto>.Success(
            IntegrationMapping.ToTemplateVersion(version, template));
    }
}
