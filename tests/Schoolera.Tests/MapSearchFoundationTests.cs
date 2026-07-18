using Schoolera.Application.Integrations;
using Schoolera.Application.Schools.Map;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

public sealed class MapSearchFoundationTests
{
    [Fact]
    public void MapIntegrationSettings_ValidLeafletOsm_ShouldPassValidation()
    {
        var validator = new IntegrationSettingsValidator();
        var json =
            """
            {
              "tileUrlTemplate": "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
              "attributionText": "© OpenStreetMap contributors",
              "defaultLatitude": 30.0444,
              "defaultLongitude": 31.2357,
              "defaultZoom": 11,
              "minZoom": 5,
              "maxZoom": 18,
              "requestTimeoutSeconds": 30
            }
            """;

        var result = validator.Validate(
            IntegrationType.Map,
            IntegrationProviderCodes.LeafletOsm,
            json,
            settingsSchemaVersion: 1);

        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorCodes);
    }

    [Fact]
    public void MapIntegrationSettings_MissingTileUrl_ShouldFail()
    {
        var validator = new IntegrationSettingsValidator();
        var json =
            """
            {
              "attributionText": "© OpenStreetMap contributors",
              "defaultLatitude": 30.0444,
              "defaultLongitude": 31.2357,
              "defaultZoom": 11,
              "minZoom": 5,
              "maxZoom": 18,
              "requestTimeoutSeconds": 30
            }
            """;

        var result = validator.Validate(
            IntegrationType.Map,
            IntegrationProviderCodes.LeafletOsm,
            json,
            settingsSchemaVersion: 1);

        Assert.False(result.IsValid);
        Assert.Contains("integrations.map.invalidTileUrl", result.ErrorCodes);
    }

    [Fact]
    public void MapSearchLimits_ClampRequestedMaxPins_ShouldCapAtAbsoluteMax()
    {
        Assert.Equal(MapSearchLimits.AbsoluteMaxPins, MapSearchLimits.ClampRequestedMaxPins(10_000));
        Assert.Equal(1, MapSearchLimits.ClampRequestedMaxPins(0));
        Assert.Equal(MapSearchLimits.DefaultMaxPins, MapSearchLimits.ClampRequestedMaxPins(null));
    }

    [Theory]
    [InlineData(null, null, null, null, true)] // no geo
    [InlineData(31.0, 30.0, 31.5, 31.0, false)] // valid bbox spans
    public void BoundingBoxSpan_Constants_AreDocumented(double? north, double? south, double? east, double? west, bool unused)
    {
        _ = unused;
        if (north is null)
        {
            Assert.True(MapSearchLimits.MaxBoundingBoxSpanDegrees > 0);
            return;
        }

        Assert.True(north - south! <= MapSearchLimits.MaxBoundingBoxSpanDegrees);
        Assert.True(east - west! <= MapSearchLimits.MaxBoundingBoxSpanDegrees);
    }

    [Fact]
    public void SensitiveConfigurationRedactor_ShouldMaskServerApiKey()
    {
        var masked = SensitiveConfigurationRedactor.MaskJson(
            """{"tileUrlTemplate":"https://example.invalid/{z}/{x}/{y}.png","serverApiKey":"secret-value"}""");

        Assert.DoesNotContain("secret-value", masked);
        Assert.Contains(SensitiveConfigurationRedactor.MaskToken, masked);
    }

    [Fact]
    public void PublicMapConfigurationDto_ShouldNotExposeSettingsJsonProperty()
    {
        var props = typeof(Schoolera.Application.Schools.Dtos.PublicMapConfigurationDto).GetProperties();
        Assert.DoesNotContain(props, p => p.Name.Contains("Settings", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, p => p.Name.Contains("Server", StringComparison.OrdinalIgnoreCase));
    }
}
