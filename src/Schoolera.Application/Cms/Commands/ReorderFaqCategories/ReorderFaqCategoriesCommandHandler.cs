using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Cms.Commands.ReorderFaqCategories;

public sealed record ReorderFaqCategoriesCommand(IReadOnlyList<Guid> OrderedIds)
    : IRequest<Result<IReadOnlyList<FaqCategoryAdminDto>>>;

public sealed class ReorderFaqCategoriesCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<ReorderFaqCategoriesCommandHandler> logger)
    : IRequestHandler<ReorderFaqCategoriesCommand, Result<IReadOnlyList<FaqCategoryAdminDto>>>
{
    public async Task<Result<IReadOnlyList<FaqCategoryAdminDto>>> Handle(
        ReorderFaqCategoriesCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<FaqCategoryAdminDto>>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var categories = await cmsRepository.ListFaqCategoriesAsync(publishedOnly: false, cancellationToken: cancellationToken);
        var orderedIds = request.OrderedIds;

        if (orderedIds.Count != categories.Count ||
            orderedIds.Distinct().Count() != orderedIds.Count ||
            !categories.Select(category => category.Id).ToHashSet().SetEquals(orderedIds))
        {
            return Result<IReadOnlyList<FaqCategoryAdminDto>>.Failure(
                ["Invalid FAQ category reorder request."],
                [CmsErrorCodes.FaqReorderInvalid]);
        }

        var categoriesById = categories.ToDictionary(category => category.Id);
        var itemsByCategoryId = categories.ToDictionary(
            category => category.Id,
            category => (IReadOnlyList<FaqItem>)category.Items.OrderBy(item => item.SortOrder).ToArray());
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var category = await cmsRepository.GetFaqCategoryByIdAsync(orderedIds[index], cancellationToken: cancellationToken);
            if (category is null)
            {
                return Result<IReadOnlyList<FaqCategoryAdminDto>>.Failure(
                    ["FAQ category not found."],
                    [CmsErrorCodes.FaqNotFound]);
            }

            category.SetSortOrder(index);
            categoriesById[category.Id] = category;
        }

        var conflict = await CmsResults.TrySaveAsync<IReadOnlyList<FaqCategoryAdminDto>>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqCategoryReordered,
            "FaqCategory",
            null,
            "Reordered FAQ categories.",
            cancellationToken);

        logger.LogInformation("Reordered FAQ categories.");

        var result = orderedIds
            .Select(id => FaqCategoryAdminDto.FromEntity(
                categoriesById[id],
                itemsByCategoryId[id]))
            .ToArray();

        return Result<IReadOnlyList<FaqCategoryAdminDto>>.Success(result);
    }
}
