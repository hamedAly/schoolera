using Schoolera.Domain.Entities;

namespace Schoolera.Application.Schools.Dtos;

public sealed record SchoolDto(
    Guid Id,
    string Slug,
    string Name,
    string? NameEn,
    string? City,
    string? LogoUrl,
    DateTimeOffset CreatedAtUtc)
{
    public static SchoolDto FromEntity(School school)
    {
        return new SchoolDto(
            school.Id,
            school.Slug,
            school.NameAr,
            school.NameEn,
            school.ShortDescriptionAr,
            school.LogoUrl,
            school.CreatedAtUtc);
    }
}