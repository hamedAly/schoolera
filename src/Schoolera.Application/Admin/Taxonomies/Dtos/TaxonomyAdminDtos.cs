using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Dtos;

public sealed record TaxonomyAdminDto(
    Guid Id,
    string Slug,
    string NameAr,
    string? NameEn,
    int SortOrder,
    bool IsActive)
{
    public static TaxonomyAdminDto FromCurriculum(Curriculum curriculum) =>
        new(
            curriculum.Id,
            curriculum.Slug,
            curriculum.NameAr,
            curriculum.NameEn,
            curriculum.SortOrder,
            curriculum.IsActive);

    public static TaxonomyAdminDto FromEducationalStage(EducationalStage stage) =>
        new(stage.Id, stage.Slug, stage.NameAr, stage.NameEn, stage.SortOrder, stage.IsActive);
}

public sealed record CountryAdminDto(
    Guid Id,
    string Code,
    string Slug,
    string NameAr,
    string? NameEn,
    int SortOrder,
    bool IsActive)
{
    public static CountryAdminDto FromCountry(Country country) =>
        new(
            country.Id,
            country.Code,
            country.Slug,
            country.NameAr,
            country.NameEn,
            country.SortOrder,
            country.IsActive);
}

public sealed record GovernorateAdminDto(
    Guid Id,
    Guid CountryId,
    string Slug,
    string NameAr,
    string? NameEn,
    int SortOrder,
    bool IsActive)
{
    public static GovernorateAdminDto FromGovernorate(Governorate governorate) =>
        new(
            governorate.Id,
            governorate.CountryId,
            governorate.Slug,
            governorate.NameAr,
            governorate.NameEn,
            governorate.SortOrder,
            governorate.IsActive);
}

public sealed record CityAdminDto(
    Guid Id,
    Guid? GovernorateId,
    string Slug,
    string NameAr,
    string? NameEn,
    int SortOrder,
    bool IsActive)
{
    public static CityAdminDto FromCity(City city) =>
        new(
            city.Id,
            city.GovernorateId,
            city.Slug,
            city.NameAr,
            city.NameEn,
            city.SortOrder,
            city.IsActive);
}

public sealed record DistrictAdminDto(
    Guid Id,
    Guid CityId,
    string Slug,
    string NameAr,
    string? NameEn,
    int SortOrder,
    bool IsActive)
{
    public static DistrictAdminDto FromDistrict(District district) =>
        new(
            district.Id,
            district.CityId,
            district.Slug,
            district.NameAr,
            district.NameEn,
            district.SortOrder,
            district.IsActive);
}

public sealed record GradeAdminDto(
    Guid Id,
    Guid EducationalStageId,
    string Slug,
    string NameAr,
    string? NameEn,
    int SortOrder,
    bool IsActive)
{
    public static GradeAdminDto FromGrade(Grade grade) =>
        new(
            grade.Id,
            grade.EducationalStageId,
            grade.Slug,
            grade.NameAr,
            grade.NameEn,
            grade.SortOrder,
            grade.IsActive);
}

public sealed record FacilityAdminDto(
    Guid Id,
    string Slug,
    string NameAr,
    string? NameEn,
    string? IconKey,
    int SortOrder,
    bool IsActive)
{
    public static FacilityAdminDto FromFacility(Facility facility) =>
        new(
            facility.Id,
            facility.Slug,
            facility.NameAr,
            facility.NameEn,
            facility.IconKey,
            facility.SortOrder,
            facility.IsActive);
}

public sealed record AcademicYearAdminDto(
    Guid Id,
    string Slug,
    string NameAr,
    string? NameEn,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent,
    int SortOrder,
    bool IsActive)
{
    public static AcademicYearAdminDto FromAcademicYear(AcademicYear academicYear) =>
        new(
            academicYear.Id,
            academicYear.Slug,
            academicYear.NameAr,
            academicYear.NameEn,
            academicYear.StartDate,
            academicYear.EndDate,
            academicYear.IsCurrent,
            0,
            academicYear.IsActive);
}
