using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Commands.CreateFaqItem;

public sealed record CreateFaqItemCommand(
    Guid CategoryId,
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    FaqOwnershipScope? OwnershipScope = null,
    InterviewFaqCategory? InterviewCategory = null) : IRequest<Result<FaqItemAdminDto>>;

public sealed class CreateFaqItemCommandHandler(
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<CreateFaqItemCommandHandler> logger)
    : IRequestHandler<CreateFaqItemCommand, Result<FaqItemAdminDto>>
{
    public async Task<Result<FaqItemAdminDto>> Handle(
        CreateFaqItemCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var ownership = request.OwnershipScope ?? FaqOwnershipScope.Platform;
        if (ownership != FaqOwnershipScope.Platform)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Platform Admin may only create Platform-owned FAQ items."],
                [CmsErrorCodes.FaqInvalidOwnership]);
        }

        var category = await cmsRepository.GetFaqCategoryByIdAsync(request.CategoryId, cancellationToken: cancellationToken);
        if (category is null)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["FAQ category not found."],
                [CmsErrorCodes.FaqNotFound]);
        }

        var sortOrder = await cmsRepository.GetNextFaqItemSortOrderAsync(
            request.CategoryId,
            FaqOwnershipScope.Platform,
            schoolId: null,
            cancellationToken);

        FaqItem item;
        if (request.InterviewCategory is { } interviewCategory)
        {
            item = new FaqItem(
                request.CategoryId,
                request.QuestionAr,
                request.QuestionEn,
                contentSanitizer.SanitizeHtml(request.AnswerAr),
                contentSanitizer.SanitizeHtml(request.AnswerEn),
                sortOrder,
                FaqOwnershipScope.Platform,
                schoolId: null,
                interviewCategory);
        }
        else
        {
            item = new FaqItem(
                request.CategoryId,
                request.QuestionAr,
                request.QuestionEn,
                contentSanitizer.SanitizeHtml(request.AnswerAr),
                contentSanitizer.SanitizeHtml(request.AnswerEn),
                sortOrder);
        }

        await cmsRepository.AddFaqItemAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqItemCreated,
            "FaqItem",
            item.Id.ToString(),
            request.InterviewCategory is null
                ? $"Created FAQ item in category '{category.Slug}'."
                : $"Created platform interview FAQ ({request.InterviewCategory}) in category '{category.Slug}'.",
            cancellationToken);

        logger.LogInformation("Created FAQ item {ItemId} in category {CategoryId}.", item.Id, request.CategoryId);

        return Result<FaqItemAdminDto>.Success(FaqItemAdminDto.FromEntity(item));
    }
}
