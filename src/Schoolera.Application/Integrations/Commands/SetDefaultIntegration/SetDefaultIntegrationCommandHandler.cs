using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;

namespace Schoolera.Application.Integrations.Commands.SetDefaultIntegration;

public sealed record SetDefaultIntegrationCommand(Guid Id)
    : IRequest<Result<PlatformIntegrationDetailDto>>;

public sealed class SetDefaultIntegrationCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IPlatformIntegrationConfigurationAccessor configurationAccessor,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<SetDefaultIntegrationCommandHandler> logger)
    : IRequestHandler<SetDefaultIntegrationCommand, Result<PlatformIntegrationDetailDto>>
{
    public async Task<Result<PlatformIntegrationDetailDto>> Handle(
        SetDefaultIntegrationCommand request,
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

        if (!entity.IsActive)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Only an active integration can be the default."],
                [IntegrationErrorCodes.CannotSetInactiveDefault]);
        }

        await notificationRepository.ClearDefaultAsync(
            entity.IntegrationType,
            exceptId: entity.Id,
            cancellationToken);

        try
        {
            entity.SetAsDefault();
        }
        catch (InvalidOperationException)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Only an active integration can be the default."],
                [IntegrationErrorCodes.CannotSetInactiveDefault]);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        configurationAccessor.InvalidateCache(entity.IntegrationType);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.IntegrationSetDefault,
            "PlatformIntegrationConfiguration",
            entity.Id.ToString(),
            $"Set default {entity.IntegrationType}/{entity.ProviderCode} integration.",
            cancellationToken);

        logger.LogInformation("Set default platform integration {IntegrationId}.", entity.Id);

        return Result<PlatformIntegrationDetailDto>.Success(IntegrationMapping.ToDetail(entity));
    }
}
