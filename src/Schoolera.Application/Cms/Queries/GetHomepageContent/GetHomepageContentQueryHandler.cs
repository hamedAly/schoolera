using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Queries.GetHomepageContent;

public sealed record GetHomepageContentQuery() : IRequest<Result<HomepageAdminDto>>;

public sealed class GetHomepageContentQueryHandler(
    ICmsRepository cmsRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetHomepageContentQuery, Result<HomepageAdminDto>>
{
    public async Task<Result<HomepageAdminDto>> Handle(
        GetHomepageContentQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<HomepageAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var content = await cmsRepository.GetLatestHomepageDraftOrSingleAsync(cancellationToken);
        if (content is null)
        {
            return Result<HomepageAdminDto>.Failure(
                ["Homepage content not found."],
                [CmsErrorCodes.HomeNotFound]);
        }

        return Result<HomepageAdminDto>.Success(HomepageAdminDto.FromEntity(content));
    }
}
