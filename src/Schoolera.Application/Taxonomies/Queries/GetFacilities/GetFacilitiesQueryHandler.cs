using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetFacilities;

public sealed record GetFacilitiesQuery : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetFacilitiesQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetFacilitiesQueryHandler> logger)
    : IRequestHandler<GetFacilitiesQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetFacilitiesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active facilities.");

        var facilities = await taxonomyRepository.ListActiveFacilitiesAsync(cancellationToken);

        return facilities
            .Select(TaxonomyItemDto.FromFacility)
            .ToArray();
    }
}
