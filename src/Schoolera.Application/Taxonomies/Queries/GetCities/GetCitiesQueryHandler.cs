using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetCities;

public sealed record GetCitiesQuery : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetCitiesQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetCitiesQueryHandler> logger)
    : IRequestHandler<GetCitiesQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetCitiesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active cities.");

        var cities = await taxonomyRepository.ListActiveCitiesAsync(cancellationToken);

        return cities
            .Select(TaxonomyItemDto.FromCity)
            .ToArray();
    }
}
