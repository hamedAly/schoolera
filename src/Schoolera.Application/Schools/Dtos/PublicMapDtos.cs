namespace Schoolera.Application.Schools.Dtos;

/// <summary>Public-safe Map client configuration. Never includes SettingsJson or server secrets.</summary>
public sealed record PublicMapConfigurationDto(
    bool IsAvailable,
    string? ProviderCode,
    string? TileUrlTemplate,
    string? PublicBrowserToken,
    double? DefaultLatitude,
    double? DefaultLongitude,
    int? DefaultZoom,
    int? MinZoom,
    int? MaxZoom,
    string? AttributionText,
    string? UnavailableReasonCode);

/// <summary>Compact Branch pin for map rendering (server-projected).</summary>
public sealed record PublicSchoolMapPinDto(
    Guid SchoolId,
    string SchoolSlug,
    Guid BranchId,
    string SchoolName,
    string BranchName,
    double Latitude,
    double Longitude,
    string? LogoUrl,
    string? LocationSummary,
    decimal? MinimumAnnualFee,
    string? FeeCurrency,
    bool HasPublishedFees,
    bool FeesRequireLogin,
    double? DistanceKm,
    bool IsAdmissionOpen);

public sealed record PublicSchoolMapPinsResultDto(
    IReadOnlyList<PublicSchoolMapPinDto> Pins,
    int ReturnedCount,
    bool IsTruncated,
    int MaxPins,
    string? GeoMode);
