using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Mapping;

namespace Schoolera.Application.Schools.Queries.GetRelatedPublicSchools;

public sealed record GetRelatedPublicSchoolsQuery(string Slug, int? Limit)
    : IRequest<Result<IReadOnlyList<PublicSchoolListItemDto>>>;

public sealed class GetRelatedPublicSchoolsQueryHandler(
    ISchoolReadRepository schoolReadRepository,
    ILogger<GetRelatedPublicSchoolsQueryHandler> logger)
    : IRequestHandler<GetRelatedPublicSchoolsQuery, Result<IReadOnlyList<PublicSchoolListItemDto>>>
{
    public async Task<Result<IReadOnlyList<PublicSchoolListItemDto>>> Handle(
        GetRelatedPublicSchoolsQuery request,
        CancellationToken cancellationToken)
    {
        var limit = request.Limit is null or < 1 ? 4 : Math.Min(request.Limit.Value, 8);
        logger.LogInformation(
            "Getting related published schools for slug {Slug} with limit {Limit}.",
            request.Slug,
            limit);

        var related = await schoolReadRepository.GetRelatedPublishedAsync(
            request.Slug,
            limit,
            cancellationToken);

        if (related is null)
        {
            return Result<IReadOnlyList<PublicSchoolListItemDto>>.Failure(
                ["School not found."],
                [SchoolErrorCodes.NotFound]);
        }

        return Result<IReadOnlyList<PublicSchoolListItemDto>>.Success(
            related.Select(PublicSchoolMapping.ToListItem).ToArray());
    }
}
