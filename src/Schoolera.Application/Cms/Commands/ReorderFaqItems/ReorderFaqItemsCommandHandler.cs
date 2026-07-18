using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.ReorderFaqItems;

public sealed record ReorderFaqItemsCommand(Guid CategoryId, IReadOnlyList<Guid> OrderedIds)
    : IRequest<Result<IReadOnlyList<FaqItemAdminDto>>>;

public sealed class ReorderFaqItemsCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<ReorderFaqItemsCommandHandler> logger)
    : IRequestHandler<ReorderFaqItemsCommand, Result<IReadOnlyList<FaqItemAdminDto>>>
{
    public async Task<Result<IReadOnlyList<FaqItemAdminDto>>> Handle(
        ReorderFaqItemsCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<FaqItemAdminDto>>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var category = await cmsRepository.GetFaqCategoryByIdAsync(request.CategoryId, cancellationToken: cancellationToken);
        if (category is null)
        {
            return Result<IReadOnlyList<FaqItemAdminDto>>.Failure(
                ["FAQ category not found."],
                [CmsErrorCodes.FaqNotFound]);
        }

        var items = await cmsRepository.GetFaqItemsByCategoryIdAsync(request.CategoryId, cancellationToken: cancellationToken);
        var orderedIds = request.OrderedIds;

        if (orderedIds.Count != items.Count ||
            orderedIds.Distinct().Count() != orderedIds.Count ||
            !items.Select(item => item.Id).ToHashSet().SetEquals(orderedIds))
        {
            return Result<IReadOnlyList<FaqItemAdminDto>>.Failure(
                ["Invalid FAQ item reorder request."],
                [CmsErrorCodes.FaqReorderInvalid]);
        }

        var itemsById = items.ToDictionary(item => item.Id);
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var item = await cmsRepository.GetFaqItemByIdAsync(orderedIds[index], cancellationToken);
            if (item is null || item.FaqCategoryId != request.CategoryId)
            {
                return Result<IReadOnlyList<FaqItemAdminDto>>.Failure(
                    ["FAQ item not found."],
                    [CmsErrorCodes.FaqNotFound]);
            }

            item.SetSortOrder(index);
            itemsById[item.Id] = item;
        }

        var conflict = await CmsResults.TrySaveAsync<IReadOnlyList<FaqItemAdminDto>>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqItemReordered,
            "FaqCategory",
            request.CategoryId.ToString(),
            "Reordered FAQ items.",
            cancellationToken);

        logger.LogInformation("Reordered FAQ items in category {CategoryId}.", request.CategoryId);

        var result = orderedIds
            .Select(id => FaqItemAdminDto.FromEntity(itemsById[id]))
            .ToArray();

        return Result<IReadOnlyList<FaqItemAdminDto>>.Success(result);
    }
}
