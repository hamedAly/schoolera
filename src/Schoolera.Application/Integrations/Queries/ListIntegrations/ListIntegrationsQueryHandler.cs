using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Integrations.Queries.ListIntegrations;

public sealed record ListIntegrationsQuery(
    int? Type,
    string? ProviderCode,
    bool? IsActive,
    int? HealthStatus)
    : IRequest<Result<IReadOnlyList<PlatformIntegrationSummaryDto>>>;

public sealed class ListIntegrationsQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<ListIntegrationsQuery, Result<IReadOnlyList<PlatformIntegrationSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<PlatformIntegrationSummaryDto>>> Handle(
        ListIntegrationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<PlatformIntegrationSummaryDto>>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        IntegrationType? type = null;
        if (request.Type is { } typeValue)
        {
            if (!Enum.IsDefined(typeof(IntegrationType), typeValue))
            {
                return Result<IReadOnlyList<PlatformIntegrationSummaryDto>>.Failure(
                    ["Invalid integration type."],
                    [IntegrationErrorCodes.InvalidType]);
            }

            type = (IntegrationType)typeValue;
        }

        IntegrationHealthStatus? healthStatus = null;
        if (request.HealthStatus is { } healthValue)
        {
            if (!Enum.IsDefined(typeof(IntegrationHealthStatus), healthValue))
            {
                return Result<IReadOnlyList<PlatformIntegrationSummaryDto>>.Failure(
                    ["Invalid health status."],
                    [IntegrationErrorCodes.ValidationFailed]);
            }

            healthStatus = (IntegrationHealthStatus)healthValue;
        }

        var items = await notificationRepository.ListIntegrationsAsync(
            type,
            request.ProviderCode,
            request.IsActive,
            healthStatus,
            cancellationToken);

        return Result<IReadOnlyList<PlatformIntegrationSummaryDto>>.Success(
            items.Select(IntegrationMapping.ToSummary).ToArray());
    }
}
