using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetCitiesByGovernorate;

public sealed record GetCitiesByGovernorateQuery(Guid GovernorateId)
    : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetCitiesByGovernorateQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetCitiesByGovernorateQueryHandler> logger)
    : IRequestHandler<GetCitiesByGovernorateQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetCitiesByGovernorateQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Getting active cities for governorate {GovernorateId}.",
            request.GovernorateId);

        var cities = await taxonomyRepository.ListActiveCitiesByGovernorateAsync(
            request.GovernorateId,
            cancellationToken);

        return cities
            .Select(TaxonomyItemDto.FromCity)
            .ToArray();
    }
}
