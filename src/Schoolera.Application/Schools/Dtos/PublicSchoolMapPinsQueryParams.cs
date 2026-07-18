using Schoolera.Application.Schools.Queries.GetPublicSchoolMapPins;

namespace Schoolera.Application.Schools.Dtos;

/// <summary>ASP.NET model-bound query parameters for GET /api/schools/map-pins.</summary>
public sealed class PublicSchoolMapPinsQueryParams
{
    public string? Search { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? GovernorateId { get; set; }

    public Guid? CityId { get; set; }

    public Guid? DistrictId { get; set; }

    public Guid? CurriculumId { get; set; }

    public List<Guid>? CurriculumIds { get; set; }

    public Guid? StageId { get; set; }

    public Guid? GradeId { get; set; }

    public int? SchoolType { get; set; }

    public int? GenderType { get; set; }

    public bool? AdmissionOpen { get; set; }

    public List<Guid>? FacilityIds { get; set; }

    public decimal? MinimumTuition { get; set; }

    public decimal? MaximumTuition { get; set; }

    public Guid? AcademicYearId { get; set; }

    public double? NorthLatitude { get; set; }

    public double? SouthLatitude { get; set; }

    public double? EastLongitude { get; set; }

    public double? WestLongitude { get; set; }

    public double? CenterLatitude { get; set; }

    public double? CenterLongitude { get; set; }

    public double? RadiusKm { get; set; }

    public int? MaxPins { get; set; }

    public GetPublicSchoolMapPinsQuery ToQuery() =>
        new(
            Search,
            CountryId,
            GovernorateId,
            CityId,
            DistrictId,
            CurriculumId,
            CurriculumIds,
            StageId,
            GradeId,
            SchoolType,
            GenderType,
            AdmissionOpen,
            FacilityIds,
            MinimumTuition,
            MaximumTuition,
            AcademicYearId,
            NorthLatitude,
            SouthLatitude,
            EastLongitude,
            WestLongitude,
            CenterLatitude,
            CenterLongitude,
            RadiusKm,
            MaxPins);
}
