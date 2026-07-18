using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.UnpublishFaqCategory;

public sealed record UnpublishFaqCategoryCommand(Guid Id) : IRequest<Result<FaqCategoryAdminDto>>;

public sealed class UnpublishFaqCategoryCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UnpublishFaqCategoryCommandHandler> logger)
    : IRequestHandler<UnpublishFaqCategoryCommand, Result<FaqCategoryAdminDto>>
{
    public async Task<Result<FaqCategoryAdminDto>> Handle(
        UnpublishFaqCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<FaqCategoryAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var category = await cmsRepository.GetFaqCategoryByIdAsync(request.Id, includeItems: true, cancellationToken);
        if (category is null)
        {
            return Result<FaqCategoryAdminDto>.Failure(
                ["FAQ category not found."],
                [CmsErrorCodes.FaqNotFound]);
        }

        category.Unpublish();

        var conflict = await CmsResults.TrySaveAsync<FaqCategoryAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqCategoryUnpublished,
            "FaqCategory",
            category.Id.ToString(),
            $"Unpublished FAQ category '{category.Slug}'.",
            cancellationToken);

        logger.LogInformation("Unpublished FAQ category {CategoryId}.", category.Id);

        return Result<FaqCategoryAdminDto>.Success(
            FaqCategoryAdminDto.FromEntity(
                category,
                category.Items.OrderBy(item => item.SortOrder).ToArray()));
    }
}
