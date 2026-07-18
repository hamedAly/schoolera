using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Queries.ListFaqCategories;

public sealed record ListFaqCategoriesQuery() : IRequest<Result<IReadOnlyList<FaqCategoryAdminDto>>>;

public sealed class ListFaqCategoriesQueryHandler(
    ICmsRepository cmsRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListFaqCategoriesQuery, Result<IReadOnlyList<FaqCategoryAdminDto>>>
{
    public async Task<Result<IReadOnlyList<FaqCategoryAdminDto>>> Handle(
        ListFaqCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<FaqCategoryAdminDto>>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var categories = await cmsRepository.ListFaqCategoriesAsync(publishedOnly: false, cancellationToken: cancellationToken);
        var result = categories
            .Select(category => FaqCategoryAdminDto.FromEntity(
                category,
                category.Items.OrderBy(item => item.SortOrder).ToArray()))
            .ToArray();

        return Result<IReadOnlyList<FaqCategoryAdminDto>>.Success(result);
    }
}
