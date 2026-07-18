using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Queries.GetPublicSchools;

namespace Schoolera.Application.Schools.Queries.GetSchools;

public sealed record GetSchoolsQuery(
    int PageNumber = 1,
    int PageSize = PagedRequest.DefaultPageSize) : IRequest<PagedResult<PublicSchoolListItemDto>>;

public sealed class GetSchoolsQueryHandler(
    IMediator mediator,
    ILogger<GetSchoolsQueryHandler> logger)
    : IRequestHandler<GetSchoolsQuery, PagedResult<PublicSchoolListItemDto>>
{
    public Task<PagedResult<PublicSchoolListItemDto>> Handle(
        GetSchoolsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting schools via public paged query.");

        return mediator.Send(
            new GetPublicSchoolsQuery(new PagedRequest(request.PageNumber, request.PageSize)),
            cancellationToken);
    }
}
