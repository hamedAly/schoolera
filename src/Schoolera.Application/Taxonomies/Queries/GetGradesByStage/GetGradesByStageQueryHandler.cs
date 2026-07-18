using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Taxonomies.Dtos;

namespace Schoolera.Application.Taxonomies.Queries.GetGradesByStage;

public sealed record GetGradesByStageQuery(Guid StageId) : IRequest<IReadOnlyList<TaxonomyItemDto>>;

public sealed class GetGradesByStageQueryHandler(
    ITaxonomyRepository taxonomyRepository,
    ILogger<GetGradesByStageQueryHandler> logger)
    : IRequestHandler<GetGradesByStageQuery, IReadOnlyList<TaxonomyItemDto>>
{
    public async Task<IReadOnlyList<TaxonomyItemDto>> Handle(
        GetGradesByStageQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active grades for stage {StageId}.", request.StageId);

        var grades = await taxonomyRepository.ListActiveGradesByStageAsync(
            request.StageId,
            cancellationToken);

        return grades
            .Select(TaxonomyItemDto.FromGrade)
            .ToArray();
    }
}
