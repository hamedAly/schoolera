using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Integrations.Commands.ValidateIntegration;

public sealed record ValidateIntegrationCommand(Guid? Id, ValidateIntegrationRequest Body)
    : IRequest<Result<IReadOnlyList<string>>>;

public sealed class ValidateIntegrationCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    IIntegrationSettingsValidator settingsValidator,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<ValidateIntegrationCommandHandler> logger)
    : IRequestHandler<ValidateIntegrationCommand, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(
        ValidateIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<string>>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        IntegrationType type;
        string providerCode;
        int schemaVersion;
        string settingsJson = request.Body.SettingsJson;

        if (request.Id is { } id)
        {
            var entity = await notificationRepository.GetIntegrationAsync(id, cancellationToken);
            if (entity is null)
            {
                return Result<IReadOnlyList<string>>.Failure(
                    ["Integration not found."],
                    [IntegrationErrorCodes.NotFound]);
            }

            type = request.Body.IntegrationType ?? entity.IntegrationType;
            providerCode = string.IsNullOrWhiteSpace(request.Body.ProviderCode)
                ? entity.ProviderCode
                : request.Body.ProviderCode;
            schemaVersion = request.Body.SettingsSchemaVersion ?? entity.SettingsSchemaVersion;
            if (string.IsNullOrWhiteSpace(settingsJson))
            {
                settingsJson = entity.SettingsJson;
            }
        }
        else
        {
            if (request.Body.IntegrationType is null ||
                !Enum.IsDefined(request.Body.IntegrationType.Value))
            {
                return Result<IReadOnlyList<string>>.Failure(
                    ["Invalid integration type."],
                    [IntegrationErrorCodes.InvalidType]);
            }

            type = request.Body.IntegrationType.Value;
            providerCode = request.Body.ProviderCode ?? string.Empty;
            schemaVersion = request.Body.SettingsSchemaVersion ?? 1;
        }

        var validation = settingsValidator.Validate(type, providerCode, settingsJson, schemaVersion);

        await adminPlatform.WriteAuditAsync(
            actorId,
            IntegrationAuditActions.IntegrationValidated,
            "PlatformIntegrationConfiguration",
            request.Id?.ToString() ?? type.ToString(),
            validation.IsValid
                ? $"Validated {type}/{providerCode} settings successfully."
                : $"Validated {type}/{providerCode} settings with {validation.ErrorCodes.Count} error(s).",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Validated platform integration settings for {IntegrationType}/{ProviderCode}: {Outcome}.",
            type,
            providerCode,
            validation.IsValid ? "ok" : "failed");

        if (!validation.IsValid)
        {
            return Result<IReadOnlyList<string>>.Failure(
                validation.ErrorCodes,
                validation.ErrorCodes.Count > 0
                    ? validation.ErrorCodes
                    : [IntegrationErrorCodes.ValidationFailed]);
        }

        return Result<IReadOnlyList<string>>.Success(Array.Empty<string>());
    }
}
