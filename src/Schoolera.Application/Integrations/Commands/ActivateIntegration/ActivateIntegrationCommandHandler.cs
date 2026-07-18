using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Application.Meetings;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Integrations.Commands.ActivateIntegration;

public sealed record ActivateIntegrationCommand(Guid Id)
    : IRequest<Result<PlatformIntegrationDetailDto>>;

public sealed class ActivateIntegrationCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IIntegrationSettingsValidator settingsValidator,
    IRuntimeEnvironment runtimeEnvironment,
    IPlatformIntegrationConfigurationAccessor configurationAccessor,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<ActivateIntegrationCommandHandler> logger)
    : IRequestHandler<ActivateIntegrationCommand, Result<PlatformIntegrationDetailDto>>
{
    public async Task<Result<PlatformIntegrationDetailDto>> Handle(
        ActivateIntegrationCommand request,
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

        var validation = settingsValidator.Validate(
            entity.IntegrationType,
            entity.ProviderCode,
            entity.SettingsJson,
            entity.SettingsSchemaVersion);
        if (!validation.IsValid)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Integration settings are invalid; cannot activate."],
                validation.ErrorCodes.Count > 0
                    ? validation.ErrorCodes
                    : [IntegrationErrorCodes.ValidationFailed]);
        }

        if (entity.IntegrationType == IntegrationType.Meeting &&
            IntegrationProviderCodes.IsSimulated(entity.ProviderCode) &&
            !runtimeEnvironment.IsDevelopment && !runtimeEnvironment.IsEnvironment("Test"))
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["The simulated Meeting provider is Development/Test only."],
                ["integrations.meeting.simulatedDevelopmentOnly"]);
        }

        if (entity.IntegrationType == IntegrationType.Courier)
        {
            CourierIntegrationSettings courierSettings;
            try
            {
                courierSettings = IntegrationSettingsSerializer.Deserialize<CourierIntegrationSettings>(
                    entity.SettingsJson);
            }
            catch
            {
                return Result<PlatformIntegrationDetailDto>.Failure(
                    ["Courier settings are invalid."],
                    ["integrations.courier.invalidEnvironment"]);
            }

            var simulatedProvider =
                string.Equals(entity.ProviderCode, IntegrationProviderCodes.Simulated, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entity.ProviderCode, IntegrationProviderCodes.Development, StringComparison.OrdinalIgnoreCase);
            var simulatedEnvironment = Enum.TryParse<CourierProviderEnvironment>(
                courierSettings.Environment, true, out var courierEnvironment) &&
                courierEnvironment == CourierProviderEnvironment.Simulated;
            if (!simulatedProvider || !simulatedEnvironment ||
                (!runtimeEnvironment.IsDevelopment && !runtimeEnvironment.IsEnvironment("Test")))
            {
                return Result<PlatformIntegrationDetailDto>.Failure(
                    ["Courier activation is limited to simulated Development/Test configurations."],
                    ["integrations.courier.simulatedDevelopmentOnly"]);
            }
        }

        entity.Activate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        configurationAccessor.InvalidateCache(entity.IntegrationType);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.IntegrationActivated,
            "PlatformIntegrationConfiguration",
            entity.Id.ToString(),
            $"Activated {entity.IntegrationType}/{entity.ProviderCode} integration.",
            cancellationToken);

        logger.LogInformation("Activated platform integration {IntegrationId}.", entity.Id);

        return Result<PlatformIntegrationDetailDto>.Success(IntegrationMapping.ToDetail(entity));
    }
}
