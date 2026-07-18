using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class TaxonomyRepository(SchooleraDbContext dbContext) : ITaxonomyRepository
{
    public async Task<IReadOnlyList<Country>> ListActiveCountriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Countries
            .AsNoTracking()
            .Where(country => country.IsActive)
            .OrderBy(country => country.SortOrder)
            .ThenBy(country => country.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Governorate>> ListActiveGovernoratesByCountryAsync(
        Guid countryId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Governorates
            .AsNoTracking()
            .Where(governorate => governorate.CountryId == countryId && governorate.IsActive)
            .OrderBy(governorate => governorate.SortOrder)
            .ThenBy(governorate => governorate.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<City>> ListActiveCitiesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Cities
            .AsNoTracking()
            .Where(city => city.IsActive)
            .OrderBy(city => city.SortOrder)
            .ThenBy(city => city.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<City>> ListActiveCitiesByGovernorateAsync(
        Guid governorateId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Cities
            .AsNoTracking()
            .Where(city => city.GovernorateId == governorateId && city.IsActive)
            .OrderBy(city => city.SortOrder)
            .ThenBy(city => city.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<District>> ListActiveDistrictsByCityAsync(
        Guid cityId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Districts
            .AsNoTracking()
            .Where(district => district.CityId == cityId && district.IsActive)
            .OrderBy(district => district.SortOrder)
            .ThenBy(district => district.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Curriculum>> ListActiveCurriculaAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Curricula
            .AsNoTracking()
            .Where(curriculum => curriculum.IsActive)
            .OrderBy(curriculum => curriculum.SortOrder)
            .ThenBy(curriculum => curriculum.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<EducationalStage>> ListActiveEducationalStagesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.EducationalStages
            .AsNoTracking()
            .Where(stage => stage.IsActive)
            .OrderBy(stage => stage.SortOrder)
            .ThenBy(stage => stage.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Grade>> ListActiveGradesByStageAsync(
        Guid stageId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Grades
            .AsNoTracking()
            .Where(grade => grade.EducationalStageId == stageId && grade.IsActive)
            .OrderBy(grade => grade.SortOrder)
            .ThenBy(grade => grade.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Facility>> ListActiveFacilitiesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Facilities
            .AsNoTracking()
            .Where(facility => facility.IsActive)
            .OrderBy(facility => facility.SortOrder)
            .ThenBy(facility => facility.NameAr)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<AcademicYear>> ListActiveAcademicYearsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AcademicYears
            .AsNoTracking()
            .Where(year => year.IsActive)
            .OrderByDescending(year => year.StartDate)
            .ThenBy(year => year.NameAr)
            .ToArrayAsync(cancellationToken);

    public Task<Country?> GetCountryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Countries.FirstOrDefaultAsync(country => country.Id == id, cancellationToken);

    public Task<Country?> GetCountryByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return dbContext.Countries.FirstOrDefaultAsync(country => country.Code == normalized, cancellationToken);
    }

    public Task<Governorate?> GetGovernorateByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Governorates.FirstOrDefaultAsync(governorate => governorate.Id == id, cancellationToken);

    public Task<City?> GetCityByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Cities.FirstOrDefaultAsync(city => city.Id == id, cancellationToken);

    public Task<District?> GetDistrictByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Districts.FirstOrDefaultAsync(district => district.Id == id, cancellationToken);

    public Task<Curriculum?> GetCurriculumByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Curricula.FirstOrDefaultAsync(curriculum => curriculum.Id == id, cancellationToken);

    public Task<EducationalStage?> GetEducationalStageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.EducationalStages.FirstOrDefaultAsync(stage => stage.Id == id, cancellationToken);

    public Task<Grade?> GetGradeByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Grades.FirstOrDefaultAsync(grade => grade.Id == id, cancellationToken);

    public Task<Facility?> GetFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Facilities.FirstOrDefaultAsync(facility => facility.Id == id, cancellationToken);

    public Task<AcademicYear?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.AcademicYears.FirstOrDefaultAsync(year => year.Id == id, cancellationToken);

    public Task<bool> CountryExistsAsync(Guid countryId, CancellationToken cancellationToken = default) =>
        dbContext.Countries.AsNoTracking().AnyAsync(country => country.Id == countryId, cancellationToken);

    public Task<bool> GovernorateExistsAsync(Guid governorateId, CancellationToken cancellationToken = default) =>
        dbContext.Governorates.AsNoTracking().AnyAsync(
            governorate => governorate.Id == governorateId,
            cancellationToken);

    public Task<bool> CityExistsAsync(Guid cityId, CancellationToken cancellationToken = default) =>
        dbContext.Cities.AsNoTracking().AnyAsync(city => city.Id == cityId, cancellationToken);

    public Task<bool> EducationalStageExistsAsync(Guid stageId, CancellationToken cancellationToken = default) =>
        dbContext.EducationalStages.AsNoTracking().AnyAsync(stage => stage.Id == stageId, cancellationToken);

    public Task<bool> IsCountryCodeTakenAsync(
        string code,
        Guid? excludeCountryId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = dbContext.Countries.AsNoTracking().Where(country => country.Code == normalized);
        if (excludeCountryId is { } id)
        {
            query = query.Where(country => country.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsCountrySlugTakenAsync(
        string slug,
        Guid? excludeCountryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Countries.AsNoTracking().Where(country => country.Slug == slug);
        if (excludeCountryId is { } id)
        {
            query = query.Where(country => country.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsGovernorateSlugTakenAsync(
        Guid countryId,
        string slug,
        Guid? excludeGovernorateId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Governorates
            .AsNoTracking()
            .Where(governorate => governorate.CountryId == countryId && governorate.Slug == slug);
        if (excludeGovernorateId is { } id)
        {
            query = query.Where(governorate => governorate.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsCitySlugTakenAsync(
        string slug,
        Guid? excludeCityId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cities.AsNoTracking().Where(city => city.Slug == slug);
        if (excludeCityId is { } id)
        {
            query = query.Where(city => city.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsDistrictSlugTakenAsync(
        string slug,
        Guid? excludeDistrictId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Districts.AsNoTracking().Where(district => district.Slug == slug);
        if (excludeDistrictId is { } id)
        {
            query = query.Where(district => district.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsCurriculumSlugTakenAsync(
        string slug,
        Guid? excludeCurriculumId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Curricula.AsNoTracking().Where(curriculum => curriculum.Slug == slug);
        if (excludeCurriculumId is { } id)
        {
            query = query.Where(curriculum => curriculum.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsEducationalStageSlugTakenAsync(
        string slug,
        Guid? excludeStageId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EducationalStages.AsNoTracking().Where(stage => stage.Slug == slug);
        if (excludeStageId is { } id)
        {
            query = query.Where(stage => stage.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsGradeSlugTakenAsync(
        string slug,
        Guid? excludeGradeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Grades.AsNoTracking().Where(grade => grade.Slug == slug);
        if (excludeGradeId is { } id)
        {
            query = query.Where(grade => grade.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsFacilitySlugTakenAsync(
        string slug,
        Guid? excludeFacilityId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Facilities.AsNoTracking().Where(facility => facility.Slug == slug);
        if (excludeFacilityId is { } id)
        {
            query = query.Where(facility => facility.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsAcademicYearSlugTakenAsync(
        string slug,
        Guid? excludeAcademicYearId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AcademicYears.AsNoTracking().Where(year => year.Slug == slug);
        if (excludeAcademicYearId is { } id)
        {
            query = query.Where(year => year.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddCountryAsync(Country country, CancellationToken cancellationToken = default) =>
        await dbContext.Countries.AddAsync(country, cancellationToken);

    public async Task AddGovernorateAsync(Governorate governorate, CancellationToken cancellationToken = default) =>
        await dbContext.Governorates.AddAsync(governorate, cancellationToken);

    public async Task AddCityAsync(City city, CancellationToken cancellationToken = default) =>
        await dbContext.Cities.AddAsync(city, cancellationToken);

    public async Task AddDistrictAsync(District district, CancellationToken cancellationToken = default) =>
        await dbContext.Districts.AddAsync(district, cancellationToken);

    public async Task AddCurriculumAsync(Curriculum curriculum, CancellationToken cancellationToken = default) =>
        await dbContext.Curricula.AddAsync(curriculum, cancellationToken);

    public async Task AddEducationalStageAsync(
        EducationalStage stage,
        CancellationToken cancellationToken = default) =>
        await dbContext.EducationalStages.AddAsync(stage, cancellationToken);

    public async Task AddGradeAsync(Grade grade, CancellationToken cancellationToken = default) =>
        await dbContext.Grades.AddAsync(grade, cancellationToken);

    public async Task AddFacilityAsync(Facility facility, CancellationToken cancellationToken = default) =>
        await dbContext.Facilities.AddAsync(facility, cancellationToken);

    public async Task AddAcademicYearAsync(AcademicYear academicYear, CancellationToken cancellationToken = default) =>
        await dbContext.AcademicYears.AddAsync(academicYear, cancellationToken);
}
