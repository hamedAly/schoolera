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

namespace Schoolera.Application.Cms.Commands.CreateFaqCategory;

public sealed record CreateFaqCategoryCommand(
    string NameAr,
    string NameEn,
    string Slug) : IRequest<Result<FaqCategoryAdminDto>>;

public sealed class CreateFaqCategoryCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<CreateFaqCategoryCommandHandler> logger)
    : IRequestHandler<CreateFaqCategoryCommand, Result<FaqCategoryAdminDto>>
{
    public async Task<Result<FaqCategoryAdminDto>> Handle(
        CreateFaqCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<FaqCategoryAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
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

        if (await cmsRepository.IsFaqCategorySlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<FaqCategoryAdminDto>.Failure(
                ["FAQ category slug is already taken."],
                [CmsErrorCodes.PageSlugDuplicate]);
        }

        var sortOrder = await cmsRepository.GetNextFaqCategorySortOrderAsync(cancellationToken);
        var category = new FaqCategory(request.NameAr, request.NameEn, request.Slug, sortOrder);

        await cmsRepository.AddFaqCategoryAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqCategoryCreated,
            "FaqCategory",
            category.Id.ToString(),
            $"Created FAQ category '{category.Slug}'.",
            cancellationToken);

        logger.LogInformation("Created FAQ category {CategoryId}.", category.Id);

        return Result<FaqCategoryAdminDto>.Success(
            FaqCategoryAdminDto.FromEntity(category, Array.Empty<FaqItem>()));
    }
}
