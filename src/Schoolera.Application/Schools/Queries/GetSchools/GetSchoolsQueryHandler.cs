using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Schools.Dtos;

namespace Schoolera.Application.Schools.Queries.GetSchools;

public sealed record GetSchoolsQuery : IRequest<IReadOnlyCollection<SchoolDto>>;

public sealed class GetSchoolsQueryHandler(
    ISchoolRepository schoolRepository,
    ILogger<GetSchoolsQueryHandler> logger)
    : IRequestHandler<GetSchoolsQuery, IReadOnlyCollection<SchoolDto>>
{
    public async Task<IReadOnlyCollection<SchoolDto>> Handle(
        GetSchoolsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting schools.");

        var schools = await schoolRepository.ListAsync(cancellationToken);

        return schools
            .Select(SchoolDto.FromEntity)
            .ToArray();
    }
}