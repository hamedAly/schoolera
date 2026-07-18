using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Models;

/// <summary>
/// Explicit public school list sort values. Arbitrary field names are rejected.
/// </summary>
public enum PublicSchoolSort
{
    Relevance = 0,
    NameAsc = 1,
    NameDesc = 2,
    LowestFee = 3,
    HighestFee = 4,
    Newest = 5,
    Nearest = 6,
}

public static class PublicSchoolSortParser
{
    public static bool TryParse(string? value, out PublicSchoolSort sort)
    {
        sort = PublicSchoolSort.Newest;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "relevance":
                sort = PublicSchoolSort.Relevance;
                return true;
            case "name-asc":
            case "nameasc":
                sort = PublicSchoolSort.NameAsc;
                return true;
            case "name-desc":
            case "namedesc":
                sort = PublicSchoolSort.NameDesc;
                return true;
            case "lowest-fee":
            case "lowestfee":
                sort = PublicSchoolSort.LowestFee;
                return true;
            case "highest-fee":
            case "highestfee":
                sort = PublicSchoolSort.HighestFee;
                return true;
            case "newest":
                sort = PublicSchoolSort.Newest;
                return true;
            case "nearest":
                sort = PublicSchoolSort.Nearest;
                return true;
            default:
                return false;
        }
    }

    public static string ToQueryValue(PublicSchoolSort sort) => sort switch
    {
        PublicSchoolSort.Relevance => "relevance",
        PublicSchoolSort.NameAsc => "name-asc",
        PublicSchoolSort.NameDesc => "name-desc",
        PublicSchoolSort.LowestFee => "lowest-fee",
        PublicSchoolSort.HighestFee => "highest-fee",
        PublicSchoolSort.Newest => "newest",
        PublicSchoolSort.Nearest => "nearest",
        _ => "newest",
    };
}

/// <summary>
/// Filters for published public school catalog search.
/// CurriculumIds: ANY match. FacilityIds: ALL must be present.
/// </summary>
public sealed record PublicSchoolListFilter(
    string? Search = null,
    Guid? CityId = null,
    Guid? DistrictId = null,
    Guid? CountryId = null,
    Guid? GovernorateId = null,
    IReadOnlyList<Guid>? CurriculumIds = null,
    Guid? StageId = null,
    Guid? GradeId = null,
    SchoolType? SchoolType = null,
    GenderType? GenderType = null,
    bool? AdmissionOpen = null,
    IReadOnlyList<Guid>? FacilityIds = null,
    decimal? MinimumTuition = null,
    decimal? MaximumTuition = null,
    Guid? AcademicYearId = null,
    double? Latitude = null,
    double? Longitude = null,
    PublicSchoolSort Sort = PublicSchoolSort.Newest);

/// <summary>
/// Intermediate projection before localized list-item mapping.
/// </summary>
public sealed record PublicSchoolSearchProjection(
    Guid Id,
    string Slug,
    string NameAr,
    string? NameEn,
    string? LogoUrl,
    string? CoverUrl,
    SchoolType SchoolType,
    GenderType GenderType,
    DateTimeOffset CreatedAtUtc,
    string CityNameAr,
    string? CityNameEn,
    string DistrictNameAr,
    string? DistrictNameEn,
    bool IsAdmissionOpen,
    IReadOnlyList<string> CurriculumNamesAr,
    IReadOnlyList<string> CurriculumNamesEn,
    IReadOnlyList<string> StageNamesAr,
    IReadOnlyList<string> StageNamesEn,
    decimal? MinimumAnnualFee,
    string? FeeCurrency,
    double? DistanceKm,
    bool HasPublishedFees = false,
    bool FeesRequireLogin = false,
    bool? IsFavorite = null);
