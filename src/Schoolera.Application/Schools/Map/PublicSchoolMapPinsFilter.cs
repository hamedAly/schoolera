using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Schools.Map;

public enum MapGeoSearchMode
{
    BoundingBox = 1,
    Radius = 2,
}

/// <summary>Map pin search filter. Exactly one geo mode must be active.</summary>
public sealed record PublicSchoolMapPinsFilter(
    PublicSchoolListFilter SchoolFilter,
    MapGeoSearchMode GeoMode,
    double? NorthLatitude = null,
    double? SouthLatitude = null,
    double? EastLongitude = null,
    double? WestLongitude = null,
    double? CenterLatitude = null,
    double? CenterLongitude = null,
    double? RadiusKm = null,
    int MaxPins = MapSearchLimits.DefaultMaxPins);

/// <summary>Raw pin projection before localization.</summary>
public sealed record PublicSchoolMapPinProjection(
    Guid SchoolId,
    string SchoolSlug,
    Guid BranchId,
    string SchoolNameAr,
    string? SchoolNameEn,
    string BranchNameAr,
    string? BranchNameEn,
    double Latitude,
    double Longitude,
    string? LogoUrl,
    string CityNameAr,
    string? CityNameEn,
    string DistrictNameAr,
    string? DistrictNameEn,
    bool IsAdmissionOpen,
    decimal? MinimumAnnualFee,
    string? FeeCurrency,
    bool HasPublishedFees,
    bool FeesRequireLogin,
    double? DistanceKm);
