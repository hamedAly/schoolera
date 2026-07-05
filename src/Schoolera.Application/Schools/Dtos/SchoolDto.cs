using Schoolera.Domain.Entities;

namespace Schoolera.Application.Schools.Dtos;

public sealed record SchoolDto(
    Guid Id,
    string Name,
    string? City,
    DateTimeOffset CreatedAtUtc)
{
    public static SchoolDto FromEntity(School school)
    {
        return new SchoolDto(
            school.Id,
            school.Name,
            school.City,
            school.CreatedAtUtc);
    }
}