using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetEducationalStages;

public sealed record GetEducationalStagesQuery : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetEducationalStagesQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetEducationalStagesQueryHandler> logger)
    : IRequestHandler<GetEducationalStagesQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetEducationalStagesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active educational stages.");

        var stages = await taxonomyRepository.ListActiveEducationalStagesAsync(cancellationToken);

        return stages
            .Select(TaxonomyItemDto.FromEducationalStage)
            .ToArray();
    }
}
