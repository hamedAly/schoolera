using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Models;

/// <summary>
/// Public catalog filters for school list queries.
/// </summary>
public sealed record SchoolCatalogFilters(
    string? Search = null,
    Guid? CityId = null,
    Guid? DistrictId = null,
    Guid? CurriculumId = null,
    Guid? EducationalStageId = null,
    Guid? GradeId = null,
    SchoolType? SchoolType = null,
    GenderType? GenderType = null,
    bool? AdmissionOpen = null,
    SchoolStatus? Status = SchoolStatus.Published,
    decimal? MinTuition = null,
    decimal? MaxTuition = null);
