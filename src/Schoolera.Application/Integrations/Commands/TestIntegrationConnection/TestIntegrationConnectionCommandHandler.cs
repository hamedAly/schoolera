using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Application.Couriers;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Integrations.Commands.TestIntegrationConnection;

public sealed record TestIntegrationConnectionCommand(Guid Id)
    : IRequest<Result<TestConnectionResultDto>>;

public sealed class TestIntegrationConnectionCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IIntegrationSettingsValidator settingsValidator,
    ICourierHealthService courierHealthService,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<TestIntegrationConnectionCommandHandler> logger)
    : IRequestHandler<TestIntegrationConnectionCommand, Result<TestConnectionResultDto>>
{
    public async Task<Result<TestConnectionResultDto>> Handle(
        TestIntegrationConnectionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<TestConnectionResultDto>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var entity = await notificationRepository.GetIntegrationAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result<TestConnectionResultDto>.Failure(
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
            entity.RecordHealth(IntegrationHealthStatus.Unhealthy, "integrations.validationFailed");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<TestConnectionResultDto>.Failure(
                ["Integration settings are invalid."],
                validation.ErrorCodes.Count > 0
                    ? validation.ErrorCodes
                    : [IntegrationErrorCodes.ValidationFailed]);
        }

        TestConnectionResultDto result;
        if (entity.IntegrationType == IntegrationType.Courier)
        {
            try
            {
                var health = await courierHealthService.CheckAndRecordAsync(entity.Id, cancellationToken);
                result = health is null
                    ? new TestConnectionResultDto(
                        false, entity.ProviderCode, IntegrationHealthStatus.Unknown,
                        "integrations.courier.notConfigured")
                    : new TestConnectionResultDto(
                        health.Status == IntegrationHealthStatus.Healthy,
                        entity.ProviderCode,
                        health.Status,
                        health.SafeCode);
            }
            catch (InvalidOperationException)
            {
                entity.RecordHealth(
                    IntegrationHealthStatus.Unhealthy,
                    "integrations.courier.simulatedDevelopmentOnly");
                result = new TestConnectionResultDto(
                    false,
                    entity.ProviderCode,
                    IntegrationHealthStatus.Unhealthy,
                    "integrations.courier.simulatedDevelopmentOnly");
            }
        }
        else if (IntegrationProviderCodes.IsSimulated(entity.ProviderCode))
        {
            entity.RecordHealth(IntegrationHealthStatus.Healthy, null);
            result = new TestConnectionResultDto(
                true,
                entity.ProviderCode,
                IntegrationHealthStatus.Healthy,
                "integrations.test.simulated");
        }
        else if (entity.IntegrationType == IntegrationType.Email &&
                 string.Equals(entity.ProviderCode, IntegrationProviderCodes.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            // Resolve/parse SMTP settings without sending real email; never log secrets.
            try
            {
                var settings = IntegrationSettingsSerializer.Deserialize<EmailIntegrationSettings>(
                    entity.SettingsJson);
                if (string.IsNullOrWhiteSpace(settings.Host) ||
                    string.IsNullOrWhiteSpace(settings.SenderEmail))
                {
                    entity.RecordHealth(IntegrationHealthStatus.Unhealthy, "integrations.email.hostRequired");
                    result = new TestConnectionResultDto(
                        false,
                        entity.ProviderCode,
                        IntegrationHealthStatus.Unhealthy,
                        "integrations.email.hostRequired");
                }
                else
                {
                    entity.RecordHealth(IntegrationHealthStatus.Healthy, null);
                    result = new TestConnectionResultDto(
                        true,
                        entity.ProviderCode,
                        IntegrationHealthStatus.Healthy,
                        "integrations.test.smtpResolved");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "SMTP settings resolve failed for integration {IntegrationId}.",
                    entity.Id);
                entity.RecordHealth(IntegrationHealthStatus.Unhealthy, "integrations.testConnectionFailed");
                result = new TestConnectionResultDto(
                    false,
                    entity.ProviderCode,
                    IntegrationHealthStatus.Unhealthy,
                    IntegrationErrorCodes.TestConnectionFailed);
            }
        }
        else
        {
            entity.RecordHealth(IntegrationHealthStatus.Unknown, "integrations.unsupportedProviderCode");
            result = new TestConnectionResultDto(
                false,
                entity.ProviderCode,
                IntegrationHealthStatus.Unknown,
                "integrations.unsupportedProviderCode");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.IntegrationTested,
            "PlatformIntegrationConfiguration",
            entity.Id.ToString(),
            $"Tested {entity.IntegrationType}/{entity.ProviderCode}: {(result.Succeeded ? "ok" : "failed")} ({result.SafeMessageCode}).",
            cancellationToken);

        logger.LogInformation(
            "Tested platform integration {IntegrationId} result {Succeeded} code {Code}.",
            entity.Id,
            result.Succeeded,
            result.SafeMessageCode);

        return result.Succeeded
            ? Result<TestConnectionResultDto>.Success(result)
            : Result<TestConnectionResultDto>.Failure(
                result,
                ["Connection test failed."],
                [result.SafeMessageCode ?? IntegrationErrorCodes.TestConnectionFailed]);
    }
}
