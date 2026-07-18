using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.UpdateFaqCategory;

public sealed record UpdateFaqCategoryCommand(
    Guid Id,
    string NameAr,
    string NameEn,
    string Slug) : IRequest<Result<FaqCategoryAdminDto>>;

public sealed class UpdateFaqCategoryCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UpdateFaqCategoryCommandHandler> logger)
    : IRequestHandler<UpdateFaqCategoryCommand, Result<FaqCategoryAdminDto>>
{
    public async Task<Result<FaqCategoryAdminDto>> Handle(
        UpdateFaqCategoryCommand request,
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

        if (!CmsSlugValidator.IsValidCustomSlug(request.Slug))
        {
            var errorCode = CmsSlugs.IsReserved(request.Slug)
                ? CmsErrorCodes.PageSlugReserved
                : CmsErrorCodes.PageSlugInvalid;

            return Result<FaqCategoryAdminDto>.Failure(
                ["Invalid FAQ category slug."],
                [errorCode]);
        }

        if (await cmsRepository.IsFaqCategorySlugTakenAsync(request.Slug, request.Id, cancellationToken))
        {
            return Result<FaqCategoryAdminDto>.Failure(
                ["FAQ category slug is already taken."],
                [CmsErrorCodes.PageSlugDuplicate]);
        }

        category.Update(request.NameAr, request.NameEn, request.Slug);

        var conflict = await CmsResults.TrySaveAsync<FaqCategoryAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqCategoryUpdated,
            "FaqCategory",
            category.Id.ToString(),
            $"Updated FAQ category '{category.Slug}'.",
            cancellationToken);

        logger.LogInformation("Updated FAQ category {CategoryId}.", category.Id);

        return Result<FaqCategoryAdminDto>.Success(
            FaqCategoryAdminDto.FromEntity(
                category,
                category.Items.OrderBy(item => item.SortOrder).ToArray()));
    }
}
