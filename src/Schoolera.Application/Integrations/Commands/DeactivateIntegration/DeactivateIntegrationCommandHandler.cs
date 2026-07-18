using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;

namespace Schoolera.Application.Integrations.Commands.DeactivateIntegration;

public sealed record DeactivateIntegrationCommand(Guid Id)
    : IRequest<Result<PlatformIntegrationDetailDto>>;

public sealed class DeactivateIntegrationCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IPlatformIntegrationConfigurationAccessor configurationAccessor,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateIntegrationCommandHandler> logger)
    : IRequestHandler<DeactivateIntegrationCommand, Result<PlatformIntegrationDetailDto>>
{
    public async Task<Result<PlatformIntegrationDetailDto>> Handle(
        DeactivateIntegrationCommand request,
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

        // Deactivate clears IsDefault on the entity.
        entity.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        configurationAccessor.InvalidateCache(entity.IntegrationType);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.IntegrationDeactivated,
            "PlatformIntegrationConfiguration",
            entity.Id.ToString(),
            $"Deactivated {entity.IntegrationType}/{entity.ProviderCode} integration.",
            cancellationToken);

        logger.LogInformation("Deactivated platform integration {IntegrationId}.", entity.Id);

        return Result<PlatformIntegrationDetailDto>.Success(IntegrationMapping.ToDetail(entity));
    }
}
