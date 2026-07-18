using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Cms.Queries.GetPublishedHomepage;

public sealed record GetPublishedHomepageQuery() : IRequest<Result<PublicHomepageDto>>;

public sealed class GetPublishedHomepageQueryHandler(
    ICmsRepository cmsRepository,
    IStringLocalizer<ValidationMessages> localizer)
    : IRequestHandler<GetPublishedHomepageQuery, Result<PublicHomepageDto>>
{
    public async Task<Result<PublicHomepageDto>> Handle(
        GetPublishedHomepageQuery request,
        CancellationToken cancellationToken)
    {
        var content = await cmsRepository.GetPublishedHomepageAsync(cancellationToken);
        if (content is null)
        {
            return Result<PublicHomepageDto>.Failure(
                [localizer["NotFound"].Value],
                [CmsErrorCodes.HomeNotFound]);
        }

        return Result<PublicHomepageDto>.Success(CmsDtoMapping.ToPublicHomepage(content));
    }
}
