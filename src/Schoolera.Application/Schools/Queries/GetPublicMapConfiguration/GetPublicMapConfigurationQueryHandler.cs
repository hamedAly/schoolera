using MediatR;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Map;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Queries.GetPublicMapConfiguration;

public sealed record GetPublicMapConfigurationQuery : IRequest<Result<PublicMapConfigurationDto>>;

public sealed class GetPublicMapConfigurationQueryHandler(
    IPlatformIntegrationConfigurationAccessor integrationAccessor)
    : IRequestHandler<GetPublicMapConfigurationQuery, Result<PublicMapConfigurationDto>>
{
    public async Task<Result<PublicMapConfigurationDto>> Handle(
        GetPublicMapConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var resolved = await integrationAccessor.GetActiveDefaultAsync(
            IntegrationType.Map,
            cancellationToken);

        if (resolved is null || !resolved.IsConfigured)
        {
            return Result<PublicMapConfigurationDto>.Success(
                new PublicMapConfigurationDto(
                    IsAvailable: false,
                    ProviderCode: null,
                    TileUrlTemplate: null,
                    PublicBrowserToken: null,
                    DefaultLatitude: null,
                    DefaultLongitude: null,
                    DefaultZoom: null,
                    MinZoom: null,
                    MaxZoom: null,
                    AttributionText: null,
                    UnavailableReasonCode: MapSearchErrorCodes.MapUnavailable));
        }

        if (IntegrationProviderCodes.IsSimulated(resolved.ProviderCode))
        {
            return Result<PublicMapConfigurationDto>.Success(
                new PublicMapConfigurationDto(
                    IsAvailable: false,
                    ProviderCode: resolved.ProviderCode,
                    TileUrlTemplate: null,
                    PublicBrowserToken: null,
                    DefaultLatitude: null,
                    DefaultLongitude: null,
                    DefaultZoom: null,
                    MinZoom: null,
                    MaxZoom: null,
                    AttributionText: null,
                    UnavailableReasonCode: MapSearchErrorCodes.MapUnavailable));
        }

        MapIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<MapIntegrationSettings>(resolved.SettingsJson);
        }
        catch
        {
            return Result<PublicMapConfigurationDto>.Success(
                new PublicMapConfigurationDto(
                    IsAvailable: false,
                    ProviderCode: resolved.ProviderCode,
                    TileUrlTemplate: null,
                    PublicBrowserToken: null,
                    DefaultLatitude: null,
                    DefaultLongitude: null,
                    DefaultZoom: null,
                    MinZoom: null,
                    MaxZoom: null,
                    AttributionText: null,
                    UnavailableReasonCode: MapSearchErrorCodes.MapUnavailable));
        }

        // Never return ServerApiKey or complete SettingsJson.
        return Result<PublicMapConfigurationDto>.Success(
            new PublicMapConfigurationDto(
                IsAvailable: true,
                ProviderCode: resolved.ProviderCode,
                TileUrlTemplate: settings.TileUrlTemplate,
                PublicBrowserToken: string.IsNullOrWhiteSpace(settings.PublicBrowserToken)
                    ? null
                    : settings.PublicBrowserToken,
                DefaultLatitude: settings.DefaultLatitude,
                DefaultLongitude: settings.DefaultLongitude,
                DefaultZoom: settings.DefaultZoom,
                MinZoom: settings.MinZoom,
                MaxZoom: settings.MaxZoom,
                AttributionText: settings.AttributionText,
                UnavailableReasonCode: null));
    }
}
