using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Cms.Queries.GetPublishedCmsPageBySlug;

public sealed record GetPublishedCmsPageBySlugQuery(string Slug) : IRequest<Result<PublicCmsPageDto>>;

public sealed class GetPublishedCmsPageBySlugQueryHandler(
    ICmsRepository cmsRepository,
    IStringLocalizer<ValidationMessages> localizer)
    : IRequestHandler<GetPublishedCmsPageBySlugQuery, Result<PublicCmsPageDto>>
{
    public async Task<Result<PublicCmsPageDto>> Handle(
        GetPublishedCmsPageBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var page = await cmsRepository.GetPublishedPageBySlugAsync(request.Slug, cancellationToken);
        if (page is null)
        {
            return Result<PublicCmsPageDto>.Failure(
                [localizer["NotFound"].Value],
                [CmsErrorCodes.PageNotFound]);
        }

        return Result<PublicCmsPageDto>.Success(CmsDtoMapping.ToPublicPage(page));
    }
}
