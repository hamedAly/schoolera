using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetDistrictsByCity;

public sealed record GetDistrictsByCityQuery(Guid CityId) : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetDistrictsByCityQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetDistrictsByCityQueryHandler> logger)
    : IRequestHandler<GetDistrictsByCityQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetDistrictsByCityQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active districts for city {CityId}.", request.CityId);

        var districts = await taxonomyRepository.ListActiveDistrictsByCityAsync(
            request.CityId,
            cancellationToken);

        return districts
            .Select(TaxonomyItemDto.FromDistrict)
            .ToArray();
    }
}
