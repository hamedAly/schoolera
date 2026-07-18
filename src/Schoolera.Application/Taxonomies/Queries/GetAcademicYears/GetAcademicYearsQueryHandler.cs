using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetAcademicYears;

public sealed record GetAcademicYearsQuery : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetAcademicYearsQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetAcademicYearsQueryHandler> logger)
    : IRequestHandler<GetAcademicYearsQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetAcademicYearsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active academic years.");

        var academicYears = await taxonomyRepository.ListActiveAcademicYearsAsync(cancellationToken);

        return academicYears
            .Select(TaxonomyItemDto.FromAcademicYear)
            .ToArray();
    }
}
