using MediatR;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Queries.GetPublishedFaqs;

public sealed record GetPublishedFaqsQuery() : IRequest<Result<IReadOnlyList<PublicFaqCategoryDto>>>;

public sealed class GetPublishedFaqsQueryHandler(ICmsRepository cmsRepository)
    : IRequestHandler<GetPublishedFaqsQuery, Result<IReadOnlyList<PublicFaqCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<PublicFaqCategoryDto>>> Handle(
        GetPublishedFaqsQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await cmsRepository.ListFaqCategoriesAsync(publishedOnly: true, cancellationToken: cancellationToken);
        var result = categories
            .Select(category => CmsDtoMapping.ToPublicCategory(
                category,
                category.Items.Where(item => item.IsPublished).ToArray()))
            .Where(category => category.Items.Count > 0)
            .ToArray();

        return Result<IReadOnlyList<PublicFaqCategoryDto>>.Success(result);
    }
}
