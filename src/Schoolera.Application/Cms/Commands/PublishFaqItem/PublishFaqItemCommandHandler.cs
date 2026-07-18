using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Commands.PublishFaqItem;

public sealed record PublishFaqItemCommand(Guid Id) : IRequest<Result<FaqItemAdminDto>>;

public sealed class PublishFaqItemCommandHandler(
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<PublishFaqItemCommandHandler> logger)
    : IRequestHandler<PublishFaqItemCommand, Result<FaqItemAdminDto>>
{
    public async Task<Result<FaqItemAdminDto>> Handle(
        PublishFaqItemCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var item = await cmsRepository.GetFaqItemByIdAsync(request.Id, cancellationToken);
        if (item is null || item.OwnershipScope != FaqOwnershipScope.Platform)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["FAQ item not found."],
                [CmsErrorCodes.FaqNotFound]);
        }

        var sanitizedAnswerAr = contentSanitizer.SanitizeHtml(item.AnswerAr);
        var sanitizedAnswerEn = contentSanitizer.SanitizeHtml(item.AnswerEn);
        item.Update(
            item.FaqCategoryId,
            item.QuestionAr,
            item.QuestionEn,
            sanitizedAnswerAr,
            sanitizedAnswerEn);

        if (item.IsInterviewFaq && item.InterviewCategory is null)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Interview FAQ category is required to publish."],
                [CmsErrorCodes.FaqInterviewCategoryRequired]);
        }

        if (!CmsPublishRules.HasPublishableBilingualContent(
                item.QuestionAr,
                item.QuestionEn,
                item.AnswerAr,
                item.AnswerEn))
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Arabic and English question and answer are required to publish."],
                [CmsErrorCodes.PagePublishValidation]);
        }

        // Interview FAQs do not require the general CMS category publish gate beyond existence;
        // keep category published check for general FAQs.
        if (!item.IsInterviewFaq)
        {
            var category = await cmsRepository.GetFaqCategoryByIdAsync(item.FaqCategoryId, cancellationToken: cancellationToken);
            if (category is null || !category.IsPublished)
            {
                return Result<FaqItemAdminDto>.Failure(
                    ["FAQ category must be published before publishing items."],
                    [CmsErrorCodes.FaqCategoryNotPublished]);
            }
        }
        else
        {
            var category = await cmsRepository.GetFaqCategoryByIdAsync(item.FaqCategoryId, cancellationToken: cancellationToken);
            if (category is null)
            {
                return Result<FaqItemAdminDto>.Failure(
                    ["FAQ category not found."],
                    [CmsErrorCodes.FaqNotFound]);
            }
        }

        item.Publish();

        var conflict = await CmsResults.TrySaveAsync<FaqItemAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqItemPublished,
            "FaqItem",
            item.Id.ToString(),
            "Published FAQ item.",
            cancellationToken);

        logger.LogInformation("Published FAQ item {ItemId}.", item.Id);

        return Result<FaqItemAdminDto>.Success(FaqItemAdminDto.FromEntity(item));
    }
}
