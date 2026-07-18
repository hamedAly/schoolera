using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Queries.GetPublicSchools;

namespace Schoolera.Application.Schools.Dtos;

/// <summary>
/// ASP.NET model-bound query parameters for GET /api/schools.
/// </summary>
public sealed class PublicSchoolsQueryParams
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = PagedRequest.DefaultPageSize;

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

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Sort { get; set; }

    public GetPublicSchoolsQuery ToQuery() =>
        new(
            new PagedRequest(PageNumber, PageSize),
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
            Latitude,
            Longitude,
            Sort);
}
