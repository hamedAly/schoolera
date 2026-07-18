using Schoolera.Application.Schools.Map;
using Schoolera.Application.Schools.Queries.GetPublicSchoolMapPins;

namespace Schoolera.Tests;

public sealed class MapPinsGeoValidationTests
{
    private readonly GetPublicSchoolMapPinsQueryHandler _handler = new(new StubMapPinRepository());

    [Fact]
    public async Task MapPins_ShouldRejectIncompleteBounds()
    {
        var result = await _handler.Handle(
            new GetPublicSchoolMapPinsQuery(
                Search: null,
                CountryId: null,
                GovernorateId: null,
                CityId: null,
                DistrictId: null,
                CurriculumId: null,
                CurriculumIds: null,
                StageId: null,
                GradeId: null,
                SchoolType: null,
                GenderType: null,
                AdmissionOpen: null,
                FacilityIds: null,
                MinimumTuition: null,
                MaximumTuition: null,
                AcademicYearId: null,
                NorthLatitude: 30.1,
                SouthLatitude: 30.0,
                EastLongitude: null,
                WestLongitude: 31.0,
                CenterLatitude: null,
                CenterLongitude: null,
                RadiusKm: null,
                MaxPins: 50),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(MapSearchErrorCodes.IncompleteBounds, result.ErrorCodes);
    }

    [Fact]
    public async Task MapPins_ShouldRejectConflictingGeoModes()
    {
        var result = await _handler.Handle(
            new GetPublicSchoolMapPinsQuery(
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                NorthLatitude: 30.2,
                SouthLatitude: 30.0,
                EastLongitude: 31.3,
                WestLongitude: 31.0,
                CenterLatitude: 30.1,
                CenterLongitude: 31.1,
                RadiusKm: 5,
                MaxPins: 50),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(MapSearchErrorCodes.ConflictingGeoMode, result.ErrorCodes);
    }

    [Fact]
    public async Task MapPins_ShouldRejectOversizedBounds()
    {
        var result = await _handler.Handle(
            new GetPublicSchoolMapPinsQuery(
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                NorthLatitude: 32.0,
                SouthLatitude: 29.0,
                EastLongitude: 33.0,
                WestLongitude: 30.0,
                CenterLatitude: null,
                CenterLongitude: null,
                RadiusKm: null,
                MaxPins: 50),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(MapSearchErrorCodes.BoundsTooLarge, result.ErrorCodes);
    }

    [Fact]
    public async Task MapPins_ShouldRejectRadiusAbovePlatformMax()
    {
        var result = await _handler.Handle(
            new GetPublicSchoolMapPinsQuery(
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                NorthLatitude: null,
                SouthLatitude: null,
                EastLongitude: null,
                WestLongitude: null,
                CenterLatitude: 30.0,
                CenterLongitude: 31.0,
                RadiusKm: MapSearchLimits.MaxRadiusKm + 1,
                MaxPins: 50),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(MapSearchErrorCodes.RadiusTooLarge, result.ErrorCodes);
    }

    [Fact]
    public async Task MapPins_ValidBoundingBox_ShouldSucceed()
    {
        var result = await _handler.Handle(
            new GetPublicSchoolMapPinsQuery(
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                NorthLatitude: 30.2,
                SouthLatitude: 30.0,
                EastLongitude: 31.3,
                WestLongitude: 31.0,
                CenterLatitude: null,
                CenterLongitude: null,
                RadiusKm: null,
                MaxPins: 50),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(nameof(MapGeoSearchMode.BoundingBox), result.Data!.GeoMode);
    }

    private sealed class StubMapPinRepository : Schoolera.Application.Common.Interfaces.ISchoolMapPinReadRepository
    {
        public Task<(IReadOnlyList<PublicSchoolMapPinProjection> Pins, bool IsTruncated)> SearchMapPinsAsync(
            PublicSchoolMapPinsFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PublicSchoolMapPinProjection>, bool)>(([], false));
    }
}
