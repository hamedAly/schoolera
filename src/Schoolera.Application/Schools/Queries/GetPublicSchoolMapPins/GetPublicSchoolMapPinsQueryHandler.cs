using FluentValidation;
using MediatR;
using Schoolera.Application.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Map;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Queries.GetPublicSchoolMapPins;

public sealed record GetPublicSchoolMapPinsQuery(
    string? Search,
    Guid? CountryId,
    Guid? GovernorateId,
    Guid? CityId,
    Guid? DistrictId,
    Guid? CurriculumId,
    IReadOnlyList<Guid>? CurriculumIds,
    Guid? StageId,
    Guid? GradeId,
    int? SchoolType,
    int? GenderType,
    bool? AdmissionOpen,
    IReadOnlyList<Guid>? FacilityIds,
    decimal? MinimumTuition,
    decimal? MaximumTuition,
    Guid? AcademicYearId,
    double? NorthLatitude,
    double? SouthLatitude,
    double? EastLongitude,
    double? WestLongitude,
    double? CenterLatitude,
    double? CenterLongitude,
    double? RadiusKm,
    int? MaxPins) : IRequest<Result<PublicSchoolMapPinsResultDto>>;

public sealed class GetPublicSchoolMapPinsQueryValidator : AbstractValidator<GetPublicSchoolMapPinsQuery>
{
    public GetPublicSchoolMapPinsQueryValidator()
    {
        RuleFor(query => query.MinimumTuition)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MinimumTuition.HasValue);

        RuleFor(query => query.MaximumTuition)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MaximumTuition.HasValue);

        RuleFor(query => query)
            .Must(query =>
                !query.MinimumTuition.HasValue ||
                !query.MaximumTuition.HasValue ||
                query.MinimumTuition <= query.MaximumTuition)
            .WithErrorCode("schools.invalidTuitionRange");
    }
}

public sealed class GetPublicSchoolMapPinsQueryHandler(ISchoolMapPinReadRepository mapPinRepository)
    : IRequestHandler<GetPublicSchoolMapPinsQuery, Result<PublicSchoolMapPinsResultDto>>
{
    public async Task<Result<PublicSchoolMapPinsResultDto>> Handle(
        GetPublicSchoolMapPinsQuery request,
        CancellationToken cancellationToken)
    {
        var geo = ResolveGeoMode(request);
        if (!geo.Succeeded)
        {
            return Result<PublicSchoolMapPinsResultDto>.Failure(
                geo.Errors,
                geo.ErrorCodes);
        }

        var schoolType = ParseEnum<SchoolType>(request.SchoolType);
        var genderType = ParseEnum<GenderType>(request.GenderType);
        if (request.SchoolType.HasValue && schoolType is null)
        {
            return Result<PublicSchoolMapPinsResultDto>.Failure(
                ["Invalid school type."],
                ["schools.invalidSchoolType"]);
        }

        if (request.GenderType.HasValue && genderType is null)
        {
            return Result<PublicSchoolMapPinsResultDto>.Failure(
                ["Invalid gender type."],
                ["schools.invalidGenderType"]);
        }

        var curriculumIds = MergeIds(request.CurriculumId, request.CurriculumIds);
        var schoolFilter = new PublicSchoolListFilter(
            Search: request.Search,
            CityId: request.CityId,
            DistrictId: request.DistrictId,
            CountryId: request.CountryId,
            GovernorateId: request.GovernorateId,
            CurriculumIds: curriculumIds,
            StageId: request.StageId,
            GradeId: request.GradeId,
            SchoolType: schoolType,
            GenderType: genderType,
            AdmissionOpen: request.AdmissionOpen,
            FacilityIds: request.FacilityIds,
            MinimumTuition: request.MinimumTuition,
            MaximumTuition: request.MaximumTuition,
            AcademicYearId: request.AcademicYearId,
            Latitude: geo.Data!.CenterLatitude,
            Longitude: geo.Data.CenterLongitude,
            Sort: PublicSchoolSort.Newest);

        var filter = new PublicSchoolMapPinsFilter(
            SchoolFilter: schoolFilter,
            GeoMode: geo.Data.Mode,
            NorthLatitude: geo.Data.North,
            SouthLatitude: geo.Data.South,
            EastLongitude: geo.Data.East,
            WestLongitude: geo.Data.West,
            CenterLatitude: geo.Data.CenterLatitude,
            CenterLongitude: geo.Data.CenterLongitude,
            RadiusKm: geo.Data.RadiusKm,
            MaxPins: MapSearchLimits.ClampRequestedMaxPins(request.MaxPins));

        var (pins, isTruncated) = await mapPinRepository.SearchMapPinsAsync(filter, cancellationToken);
        var mapped = pins.Select(MapPin).ToArray();

        return Result<PublicSchoolMapPinsResultDto>.Success(
            new PublicSchoolMapPinsResultDto(
                mapped,
                mapped.Length,
                isTruncated,
                filter.MaxPins,
                geo.Data.Mode.ToString()));
    }

    private static PublicSchoolMapPinDto MapPin(PublicSchoolMapPinProjection pin)
    {
        var schoolName = LocalizationDisplayHelper.Pick(pin.SchoolNameAr, pin.SchoolNameEn);
        var branchName = LocalizationDisplayHelper.Pick(pin.BranchNameAr, pin.BranchNameEn);
        var city = LocalizationDisplayHelper.Pick(pin.CityNameAr, pin.CityNameEn);
        var district = LocalizationDisplayHelper.Pick(pin.DistrictNameAr, pin.DistrictNameEn);
        var location = string.Join(
            " · ",
            new[] { district, city }.Where(static s => !string.IsNullOrWhiteSpace(s)));

        return new PublicSchoolMapPinDto(
            pin.SchoolId,
            pin.SchoolSlug,
            pin.BranchId,
            schoolName,
            branchName,
            pin.Latitude,
            pin.Longitude,
            pin.LogoUrl,
            string.IsNullOrWhiteSpace(location) ? null : location,
            pin.MinimumAnnualFee,
            pin.FeeCurrency,
            pin.HasPublishedFees,
            pin.FeesRequireLogin,
            pin.DistanceKm,
            pin.IsAdmissionOpen);
    }

    private static Result<ResolvedGeo> ResolveGeoMode(GetPublicSchoolMapPinsQuery request)
    {
        var hasBounds =
            request.NorthLatitude.HasValue ||
            request.SouthLatitude.HasValue ||
            request.EastLongitude.HasValue ||
            request.WestLongitude.HasValue;

        var hasCompleteBounds =
            request.NorthLatitude.HasValue &&
            request.SouthLatitude.HasValue &&
            request.EastLongitude.HasValue &&
            request.WestLongitude.HasValue;

        var hasRadiusParts =
            request.CenterLatitude.HasValue ||
            request.CenterLongitude.HasValue ||
            request.RadiusKm.HasValue;

        var hasCompleteRadius =
            request.CenterLatitude.HasValue &&
            request.CenterLongitude.HasValue &&
            request.RadiusKm.HasValue;

        if (hasBounds && hasRadiusParts)
        {
            return Result<ResolvedGeo>.Failure(
                ["Provide either bounding box or radius, not both."],
                [MapSearchErrorCodes.ConflictingGeoMode]);
        }

        if (!hasBounds && !hasRadiusParts)
        {
            return Result<ResolvedGeo>.Failure(
                ["Map search requires a bounding box or radius."],
                [MapSearchErrorCodes.GeoModeRequired]);
        }

        if (hasBounds)
        {
            if (!hasCompleteBounds)
            {
                return Result<ResolvedGeo>.Failure(
                    ["All four bounding-box values are required."],
                    [MapSearchErrorCodes.IncompleteBounds]);
            }

            var north = request.NorthLatitude!.Value;
            var south = request.SouthLatitude!.Value;
            var east = request.EastLongitude!.Value;
            var west = request.WestLongitude!.Value;

            if (north is < -90 or > 90 ||
                south is < -90 or > 90 ||
                east is < -180 or > 180 ||
                west is < -180 or > 180 ||
                south >= north)
            {
                return Result<ResolvedGeo>.Failure(
                    ["Invalid bounding box."],
                    [MapSearchErrorCodes.InvalidBounds]);
            }

            if (west >= east)
            {
                return Result<ResolvedGeo>.Failure(
                    ["Invalid bounding box longitude order."],
                    [MapSearchErrorCodes.InvalidBounds]);
            }

            if (north - south > MapSearchLimits.MaxBoundingBoxSpanDegrees ||
                east - west > MapSearchLimits.MaxBoundingBoxSpanDegrees)
            {
                return Result<ResolvedGeo>.Failure(
                    ["Bounding box is too large. Zoom in and try again."],
                    [MapSearchErrorCodes.BoundsTooLarge]);
            }

            return Result<ResolvedGeo>.Success(
                new ResolvedGeo(
                    MapGeoSearchMode.BoundingBox,
                    north,
                    south,
                    east,
                    west,
                    CenterLatitude: (north + south) / 2d,
                    CenterLongitude: (east + west) / 2d,
                    RadiusKm: null));
        }

        if (!hasCompleteRadius)
        {
            return Result<ResolvedGeo>.Failure(
                ["Center latitude, longitude, and radius are required."],
                [MapSearchErrorCodes.InvalidRadius]);
        }

        var lat = request.CenterLatitude!.Value;
        var lon = request.CenterLongitude!.Value;
        var radius = request.RadiusKm!.Value;

        if (lat is < -90 or > 90 || lon is < -180 or > 180 || radius <= 0)
        {
            return Result<ResolvedGeo>.Failure(
                ["Invalid radius search parameters."],
                [MapSearchErrorCodes.InvalidRadius]);
        }

        if (radius > MapSearchLimits.MaxRadiusKm)
        {
            return Result<ResolvedGeo>.Failure(
                ["Radius exceeds the platform maximum."],
                [MapSearchErrorCodes.RadiusTooLarge]);
        }

        return Result<ResolvedGeo>.Success(
            new ResolvedGeo(
                MapGeoSearchMode.Radius,
                North: null,
                South: null,
                East: null,
                West: null,
                CenterLatitude: lat,
                CenterLongitude: lon,
                RadiusKm: radius));
    }

    private static TEnum? ParseEnum<TEnum>(int? value)
        where TEnum : struct, Enum =>
        value is { } raw && Enum.IsDefined(typeof(TEnum), raw)
            ? (TEnum)Enum.ToObject(typeof(TEnum), raw)
            : null;

    private static IReadOnlyList<Guid>? MergeIds(Guid? single, IReadOnlyList<Guid>? many)
    {
        if (single is null && (many is null || many.Count == 0))
        {
            return null;
        }

        var set = new HashSet<Guid>();
        if (single is { } id)
        {
            set.Add(id);
        }

        if (many is not null)
        {
            foreach (var item in many)
            {
                set.Add(item);
            }
        }

        return set.ToArray();
    }

    private sealed record ResolvedGeo(
        MapGeoSearchMode Mode,
        double? North,
        double? South,
        double? East,
        double? West,
        double? CenterLatitude,
        double? CenterLongitude,
        double? RadiusKm);
}
