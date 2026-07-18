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

namespace Schoolera.Application.Cms.Commands.UpdateFaqItem;

public sealed record UpdateFaqItemCommand(
    Guid Id,
    Guid CategoryId,
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    InterviewFaqCategory? InterviewCategory = null,
    byte[]? RowVersion = null) : IRequest<Result<FaqItemAdminDto>>
{
    public static UpdateFaqItemCommand FromBody(
        Guid id,
        Guid categoryId,
        string questionAr,
        string questionEn,
        string answerAr,
        string answerEn,
        int? interviewCategory,
        byte[]? rowVersion) =>
        new(id, categoryId, questionAr, questionEn, answerAr, answerEn,
            interviewCategory is { } value ? (InterviewFaqCategory?)value : null, rowVersion);
}

public sealed class UpdateFaqItemCommandHandler(
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UpdateFaqItemCommandHandler> logger)
    : IRequestHandler<UpdateFaqItemCommand, Result<FaqItemAdminDto>>
{
    public async Task<Result<FaqItemAdminDto>> Handle(
        UpdateFaqItemCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var item = await cmsRepository.GetFaqItemByIdForUpdateAsync(request.Id, cancellationToken);
        if (item is null || item.OwnershipScope != FaqOwnershipScope.Platform)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["FAQ item not found."],
                [CmsErrorCodes.FaqNotFound]);
        }

        if (request.RowVersion is { Length: > 0 } &&
            CmsResults.HasRowVersionMismatch(request.RowVersion, item.RowVersion))
        {
            return CmsResults.ConcurrencyFailure<FaqItemAdminDto>();
        }

        var category = await cmsRepository.GetFaqCategoryByIdAsync(request.CategoryId, cancellationToken: cancellationToken);
        if (category is null)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["FAQ category not found."],
                [CmsErrorCodes.FaqNotFound]);
        }

        if (item.IsInterviewFaq && request.InterviewCategory is null)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Interview FAQ category is required."],
                [CmsErrorCodes.FaqInterviewCategoryRequired]);
        }

        if (!item.IsInterviewFaq && request.InterviewCategory is not null)
        {
            // Allow promoting a general FAQ to interview FAQ.
        }

        item.Update(
            request.CategoryId,
            request.QuestionAr,
            request.QuestionEn,
            contentSanitizer.SanitizeHtml(request.AnswerAr),
            contentSanitizer.SanitizeHtml(request.AnswerEn),
            request.InterviewCategory ?? item.InterviewCategory,
            schoolBranchId: null,
            educationalStageId: null,
            gradeId: null,
            academicYearId: null);

        var conflict = await CmsResults.TrySaveAsync<FaqItemAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqItemUpdated,
            "FaqItem",
            item.Id.ToString(),
            $"Updated FAQ item in category '{category.Slug}'.",
            cancellationToken);

        logger.LogInformation("Updated FAQ item {ItemId}.", item.Id);

        return Result<FaqItemAdminDto>.Success(FaqItemAdminDto.FromEntity(item));
    }
}
