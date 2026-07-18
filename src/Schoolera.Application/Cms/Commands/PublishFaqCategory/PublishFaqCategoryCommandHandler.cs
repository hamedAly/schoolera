using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.PublishFaqCategory;

public sealed record PublishFaqCategoryCommand(Guid Id) : IRequest<Result<FaqCategoryAdminDto>>;

public sealed class PublishFaqCategoryCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<PublishFaqCategoryCommandHandler> logger)
    : IRequestHandler<PublishFaqCategoryCommand, Result<FaqCategoryAdminDto>>
{
    public async Task<Result<FaqCategoryAdminDto>> Handle(
        PublishFaqCategoryCommand request,
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

        if (string.IsNullOrWhiteSpace(category.NameAr) || string.IsNullOrWhiteSpace(category.NameEn))
        {
            return Result<FaqCategoryAdminDto>.Failure(
                ["Arabic and English names are required to publish."],
                [CmsErrorCodes.PagePublishValidation]);
        }

        category.Publish();

        var conflict = await CmsResults.TrySaveAsync<FaqCategoryAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqCategoryPublished,
            "FaqCategory",
            category.Id.ToString(),
            $"Published FAQ category '{category.Slug}'.",
            cancellationToken);

        logger.LogInformation("Published FAQ category {CategoryId}.", category.Id);

        return Result<FaqCategoryAdminDto>.Success(
            FaqCategoryAdminDto.FromEntity(
                category,
                category.Items.OrderBy(item => item.SortOrder).ToArray()));
    }
}
