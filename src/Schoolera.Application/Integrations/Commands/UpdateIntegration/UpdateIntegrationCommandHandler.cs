using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;

namespace Schoolera.Application.Integrations.Commands.UpdateIntegration;

public sealed record UpdateIntegrationCommand(Guid Id, UpdatePlatformIntegrationRequest Body)
    : IRequest<Result<PlatformIntegrationDetailDto>>;

public sealed class UpdateIntegrationCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IIntegrationSettingsValidator settingsValidator,
    IPlatformIntegrationConfigurationAccessor configurationAccessor,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<UpdateIntegrationCommandHandler> logger)
    : IRequestHandler<UpdateIntegrationCommand, Result<PlatformIntegrationDetailDto>>
{
    public async Task<Result<PlatformIntegrationDetailDto>> Handle(
        UpdateIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var entity = await notificationRepository.GetIntegrationAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Integration not found."],
                [IntegrationErrorCodes.NotFound]);
        }

        var body = request.Body;
        if (IntegrationMapping.HasRowVersionMismatch(body.RowVersion, entity.RowVersion))
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["The integration was modified by another operation."],
                [IntegrationErrorCodes.Concurrency]);
        }

        string mergedJson;
        try
        {
            var clearSet = body.ClearSensitiveProperties is null
                ? null
                : body.ClearSensitiveProperties.ToHashSet(StringComparer.OrdinalIgnoreCase);
            mergedJson = SensitiveConfigurationRedactor.MergePreservingSecrets(
                entity.SettingsJson,
                body.SettingsJson,
                clearSet);
        }
        catch (Exception)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Settings JSON is invalid."],
                [IntegrationErrorCodes.InvalidJson]);
        }

        var validation = settingsValidator.Validate(
            entity.IntegrationType,
            body.ProviderCode,
            mergedJson,
            body.SettingsSchemaVersion);
        if (!validation.IsValid)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Integration settings validation failed."],
                validation.ErrorCodes.Count > 0
                    ? validation.ErrorCodes
                    : [IntegrationErrorCodes.ValidationFailed]);
        }

        try
        {
            entity.UpdateDetails(
                body.ProviderCode,
                body.DisplayNameAr,
                body.DisplayNameEn,
                mergedJson,
                body.SettingsSchemaVersion,
                body.SortOrder);
        }
        catch (ArgumentException)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Invalid integration payload."],
                [IntegrationErrorCodes.InvalidJson]);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        configurationAccessor.InvalidateCache(entity.IntegrationType);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.IntegrationUpdated,
            "PlatformIntegrationConfiguration",
            entity.Id.ToString(),
            $"Updated {entity.IntegrationType}/{entity.ProviderCode} integration '{entity.DisplayNameAr}'.",
            cancellationToken);

        logger.LogInformation("Updated platform integration {IntegrationId}.", entity.Id);

        return Result<PlatformIntegrationDetailDto>.Success(IntegrationMapping.ToDetail(entity));
    }
}
