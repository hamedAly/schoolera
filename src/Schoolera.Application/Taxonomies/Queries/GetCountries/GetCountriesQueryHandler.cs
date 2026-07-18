using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetCountries;

public sealed record GetCountriesQuery : IRequest<IReadOnlyList<CountryTaxonomyItemDto>>;

public sealed class GetCountriesQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetCountriesQueryHandler> logger)
    : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryTaxonomyItemDto>>
{
    public async Task<IReadOnlyList<CountryTaxonomyItemDto>> Handle(
        GetCountriesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active countries.");

        var countries = await taxonomyRepository.ListActiveCountriesAsync(cancellationToken);

        return countries
            .Select(CountryTaxonomyItemDto.FromCountry)
            .ToArray();
    }
}
