using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetCurricula;

public sealed record GetCurriculaQuery : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetCurriculaQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetCurriculaQueryHandler> logger)
    : IRequestHandler<GetCurriculaQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetCurriculaQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active curricula.");

        var curricula = await taxonomyRepository.ListActiveCurriculaAsync(cancellationToken);

        return curricula
            .Select(TaxonomyItemDto.FromCurriculum)
            .ToArray();
    }
}
