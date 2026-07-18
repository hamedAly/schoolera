using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Queries.GetCmsPageById;

public sealed record GetCmsPageByIdQuery(Guid Id) : IRequest<Result<CmsPageAdminDto>>;

public sealed class GetCmsPageByIdQueryHandler(
    ICmsRepository cmsRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetCmsPageByIdQuery, Result<CmsPageAdminDto>>
{
    public async Task<Result<CmsPageAdminDto>> Handle(
        GetCmsPageByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<CmsPageAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var page = await cmsRepository.GetPageByIdAsync(request.Id, cancellationToken);
        if (page is null)
        {
            return Result<CmsPageAdminDto>.Failure(
                ["CMS page not found."],
                [CmsErrorCodes.PageNotFound]);
        }

        return Result<CmsPageAdminDto>.Success(CmsPageAdminDto.FromEntity(page));
    }
}
