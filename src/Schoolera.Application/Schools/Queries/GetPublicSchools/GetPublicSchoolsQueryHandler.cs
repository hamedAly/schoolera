using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Mapping;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Queries.GetPublicSchools;

public sealed record GetPublicSchoolsQuery(
    PagedRequest Paging,
    string? Search = null,
    Guid? CountryId = null,
    Guid? GovernorateId = null,
    Guid? CityId = null,
    Guid? DistrictId = null,
    Guid? CurriculumId = null,
    IReadOnlyList<Guid>? CurriculumIds = null,
    Guid? StageId = null,
    Guid? GradeId = null,
    int? SchoolType = null,
    int? GenderType = null,
    bool? AdmissionOpen = null,
    IReadOnlyList<Guid>? FacilityIds = null,
    decimal? MinimumTuition = null,
    decimal? MaximumTuition = null,
    Guid? AcademicYearId = null,
    double? Latitude = null,
    double? Longitude = null,
    string? Sort = null) : IRequest<PagedResult<PublicSchoolListItemDto>>;

public sealed class GetPublicSchoolsQueryHandler(
    ISchoolReadRepository schoolReadRepository,
    ILogger<GetPublicSchoolsQueryHandler> logger)
    : IRequestHandler<GetPublicSchoolsQuery, PagedResult<PublicSchoolListItemDto>>
{
    public async Task<PagedResult<PublicSchoolListItemDto>> Handle(
        GetPublicSchoolsQuery request,
        CancellationToken cancellationToken)
    {
        PublicSchoolSortParser.TryParse(request.Sort, out var sort);

        logger.LogInformation("Searching published public schools with sort {Sort}.", sort);

        IReadOnlyList<Guid>? curriculumIds = request.CurriculumIds is { Count: > 0 }
            ? request.CurriculumIds
            : request.CurriculumId is { } single ? [single] : null;

        var filter = new PublicSchoolListFilter(
            request.Search,
            request.CityId,
            request.DistrictId,
            request.CountryId,
            request.GovernorateId,
            curriculumIds,
            request.StageId,
            request.GradeId,
            request.SchoolType is int st ? (SchoolType)st : null,
            request.GenderType is int gt ? (GenderType)gt : null,
            request.AdmissionOpen,
            request.FacilityIds,
            request.MinimumTuition,
            request.MaximumTuition,
            request.AcademicYearId,
            request.Latitude,
            request.Longitude,
            sort);

        var (items, totalCount) = await schoolReadRepository.SearchPublishedAsync(
            request.Paging,
            filter,
            cancellationToken);

        var mapped = items
            .Select(PublicSchoolMapping.ToListItem)
            .ToArray();

        return PagedResult<PublicSchoolListItemDto>.Create(mapped, totalCount, request.Paging);
    }
}
