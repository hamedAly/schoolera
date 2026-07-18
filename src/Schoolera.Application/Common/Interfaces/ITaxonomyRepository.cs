using Schoolera.Domain.Entities;

namespace Schoolera.Application.Common.Interfaces;

public interface ITaxonomyRepository
{
    // Public reads
    Task<IReadOnlyList<Country>> ListActiveCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Governorate>> ListActiveGovernoratesByCountryAsync(
        Guid countryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<City>> ListActiveCitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<City>> ListActiveCitiesByGovernorateAsync(
        Guid governorateId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<District>> ListActiveDistrictsByCityAsync(
        Guid cityId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Curriculum>> ListActiveCurriculaAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EducationalStage>> ListActiveEducationalStagesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> ListActiveGradesByStageAsync(
        Guid stageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Facility>> ListActiveFacilitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicYear>> ListActiveAcademicYearsAsync(
        CancellationToken cancellationToken = default);

    // Admin reads
    Task<Country?> GetCountryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Country?> GetCountryByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Governorate?> GetGovernorateByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<City?> GetCityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<District?> GetDistrictByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Curriculum?> GetCurriculumByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EducationalStage?> GetEducationalStageByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Grade?> GetGradeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Facility?> GetFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AcademicYear?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Existence checks
    Task<bool> CountryExistsAsync(Guid countryId, CancellationToken cancellationToken = default);

    Task<bool> GovernorateExistsAsync(Guid governorateId, CancellationToken cancellationToken = default);

    Task<bool> CityExistsAsync(Guid cityId, CancellationToken cancellationToken = default);

    Task<bool> EducationalStageExistsAsync(Guid stageId, CancellationToken cancellationToken = default);

    Task<bool> IsCountryCodeTakenAsync(
        string code,
        Guid? excludeCountryId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsCountrySlugTakenAsync(
        string slug,
        Guid? excludeCountryId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsGovernorateSlugTakenAsync(
        Guid countryId,
        string slug,
        Guid? excludeGovernorateId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsCitySlugTakenAsync(
        string slug,
        Guid? excludeCityId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsDistrictSlugTakenAsync(
        string slug,
        Guid? excludeDistrictId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsCurriculumSlugTakenAsync(
        string slug,
        Guid? excludeCurriculumId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsEducationalStageSlugTakenAsync(
        string slug,
        Guid? excludeStageId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsGradeSlugTakenAsync(
        string slug,
        Guid? excludeGradeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsFacilitySlugTakenAsync(
        string slug,
        Guid? excludeFacilityId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsAcademicYearSlugTakenAsync(
        string slug,
        Guid? excludeAcademicYearId = null,
        CancellationToken cancellationToken = default);

    // Admin writes
    Task AddCountryAsync(Country country, CancellationToken cancellationToken = default);

    Task AddGovernorateAsync(Governorate governorate, CancellationToken cancellationToken = default);

    Task AddCityAsync(City city, CancellationToken cancellationToken = default);

    Task AddDistrictAsync(District district, CancellationToken cancellationToken = default);

    Task AddCurriculumAsync(Curriculum curriculum, CancellationToken cancellationToken = default);

    Task AddEducationalStageAsync(EducationalStage stage, CancellationToken cancellationToken = default);

    Task AddGradeAsync(Grade grade, CancellationToken cancellationToken = default);

    Task AddFacilityAsync(Facility facility, CancellationToken cancellationToken = default);

    Task AddAcademicYearAsync(AcademicYear academicYear, CancellationToken cancellationToken = default);
}
