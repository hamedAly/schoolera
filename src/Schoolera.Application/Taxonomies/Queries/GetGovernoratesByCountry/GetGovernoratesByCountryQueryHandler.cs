using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetGovernoratesByCountry;

public sealed record GetGovernoratesByCountryQuery(Guid CountryId)
    : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetGovernoratesByCountryQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetGovernoratesByCountryQueryHandler> logger)
    : IRequestHandler<GetGovernoratesByCountryQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetGovernoratesByCountryQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active governorates for country {CountryId}.", request.CountryId);

        var governorates = await taxonomyRepository.ListActiveGovernoratesByCountryAsync(
            request.CountryId,
            cancellationToken);

        return governorates
            .Select(TaxonomyItemDto.FromGovernorate)
            .ToArray();
    }
}
