using Schoolera.Application.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Taxonomies.Dtos;

public sealed record TaxonomyItemDto(Guid Id, string Slug, string Name, int SortOrder)
{
    public static TaxonomyItemDto FromCountry(Country country) =>
        new(
            country.Id,
            country.Slug,
            LocalizationDisplayHelper.Pick(country.NameAr, country.NameEn),
            country.SortOrder);

    public static TaxonomyItemDto FromGovernorate(Governorate governorate) =>
        new(
            governorate.Id,
            governorate.Slug,
            LocalizationDisplayHelper.Pick(governorate.NameAr, governorate.NameEn),
            governorate.SortOrder);

    public static TaxonomyItemDto FromCity(City city) =>
        new(city.Id, city.Slug, LocalizationDisplayHelper.Pick(city.NameAr, city.NameEn), city.SortOrder);

    public static TaxonomyItemDto FromDistrict(District district) =>
        new(
            district.Id,
            district.Slug,
            LocalizationDisplayHelper.Pick(district.NameAr, district.NameEn),
            district.SortOrder);

    public static TaxonomyItemDto FromCurriculum(Curriculum curriculum) =>
        new(
            curriculum.Id,
            curriculum.Slug,
            LocalizationDisplayHelper.Pick(curriculum.NameAr, curriculum.NameEn),
            curriculum.SortOrder);

    public static TaxonomyItemDto FromEducationalStage(EducationalStage stage) =>
        new(
            stage.Id,
            stage.Slug,
            LocalizationDisplayHelper.Pick(stage.NameAr, stage.NameEn),
            stage.SortOrder);

    public static TaxonomyItemDto FromGrade(Grade grade) =>
        new(
            grade.Id,
            grade.Slug,
            LocalizationDisplayHelper.Pick(grade.NameAr, grade.NameEn),
            grade.SortOrder);

    public static TaxonomyItemDto FromFacility(Facility facility) =>
        new(
            facility.Id,
            facility.Slug,
            LocalizationDisplayHelper.Pick(facility.NameAr, facility.NameEn),
            facility.SortOrder);

    public static TaxonomyItemDto FromAcademicYear(AcademicYear academicYear) =>
        new(
            academicYear.Id,
            academicYear.Slug,
            LocalizationDisplayHelper.Pick(academicYear.NameAr, academicYear.NameEn),
            academicYear.StartDate.DayNumber);
}

public sealed record CountryTaxonomyItemDto(Guid Id, string Code, string Slug, string Name, int SortOrder)
{
    public static CountryTaxonomyItemDto FromCountry(Country country) =>
        new(
            country.Id,
            country.Code,
            country.Slug,
            LocalizationDisplayHelper.Pick(country.NameAr, country.NameEn),
            country.SortOrder);
}
