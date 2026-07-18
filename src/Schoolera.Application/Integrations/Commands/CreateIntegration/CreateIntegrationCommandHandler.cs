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

namespace Schoolera.Application.Integrations.Commands.CreateIntegration;

public sealed record CreateIntegrationCommand(CreatePlatformIntegrationRequest Body)
    : IRequest<Result<PlatformIntegrationDetailDto>>;

public sealed class CreateIntegrationCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IIntegrationSettingsValidator settingsValidator,
    IPlatformIntegrationConfigurationAccessor configurationAccessor,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<CreateIntegrationCommandHandler> logger)
    : IRequestHandler<CreateIntegrationCommand, Result<PlatformIntegrationDetailDto>>
{
    public async Task<Result<PlatformIntegrationDetailDto>> Handle(
        CreateIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var body = request.Body;
        if (!Enum.IsDefined(body.IntegrationType) ||
            body.IntegrationType is IntegrationType.OtherExternalService)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Invalid integration type."],
                [IntegrationErrorCodes.InvalidType]);
        }

        var validation = settingsValidator.Validate(
            body.IntegrationType,
            body.ProviderCode,
            body.SettingsJson,
            body.SettingsSchemaVersion);
        if (!validation.IsValid)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Integration settings validation failed."],
                validation.ErrorCodes.Count > 0
                    ? validation.ErrorCodes
                    : [IntegrationErrorCodes.ValidationFailed]);
        }

        PlatformIntegrationConfiguration entity;
        try
        {
            entity = new PlatformIntegrationConfiguration(
                body.IntegrationType,
                body.ProviderCode,
                body.DisplayNameAr,
                body.DisplayNameEn,
                body.SettingsJson,
                body.SettingsSchemaVersion,
                body.SortOrder);
        }
        catch (ArgumentException)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Invalid integration payload."],
                [IntegrationErrorCodes.InvalidJson]);
        }

        await notificationRepository.AddIntegrationAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        configurationAccessor.InvalidateCache(entity.IntegrationType);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.IntegrationCreated,
            "PlatformIntegrationConfiguration",
            entity.Id.ToString(),
            $"Created {entity.IntegrationType}/{entity.ProviderCode} integration '{entity.DisplayNameAr}'.",
            cancellationToken);

        logger.LogInformation(
            "Created platform integration {IntegrationId} type {IntegrationType} provider {ProviderCode}.",
            entity.Id,
            entity.IntegrationType,
            entity.ProviderCode);

        return Result<PlatformIntegrationDetailDto>.Success(IntegrationMapping.ToDetail(entity));
    }
}
