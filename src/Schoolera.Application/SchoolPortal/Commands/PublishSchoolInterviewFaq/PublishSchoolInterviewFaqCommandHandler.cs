using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolInterviewFaq;

public sealed record PublishSchoolInterviewFaqCommand(Guid SchoolId, Guid ItemId)
    : IRequest<Result<SchoolInterviewFaqDetailDto>>;

public sealed class PublishSchoolInterviewFaqCommandHandler(
    ISchoolPortalAccess portalAccess,
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    ISchoolPortalAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<PublishSchoolInterviewFaqCommandHandler> logger)
    : IRequestHandler<PublishSchoolInterviewFaqCommand, Result<SchoolInterviewFaqDetailDto>>
{
    public async Task<Result<SchoolInterviewFaqDetailDto>> Handle(
        PublishSchoolInterviewFaqCommand request,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await SchoolInterviewFaqSupport.ResolveWriteAccessAsync(
            portalAccess, request.SchoolId, localizer, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var item = await cmsRepository.GetSchoolFaqItemAsync(request.SchoolId, request.ItemId, cancellationToken);
        if (item is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewFaqDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewFaqNotFound);
        }

        var sanitizedAr = contentSanitizer.SanitizeHtml(item.AnswerAr);
        var sanitizedEn = contentSanitizer.SanitizeHtml(item.AnswerEn);
        item.Update(
            item.FaqCategoryId,
            item.QuestionAr,
            item.QuestionEn,
            sanitizedAr,
            sanitizedEn,
            item.InterviewCategory,
            item.SchoolBranchId,
            item.EducationalStageId,
            item.GradeId,
            item.AcademicYearId);

        if (item.InterviewCategory is null ||
            !CmsPublishRules.HasPublishableBilingualContent(
                item.QuestionAr, item.QuestionEn, item.AnswerAr, item.AnswerEn))
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewFaqDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewFaqPublishInvalid);
        }

        item.Publish();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolInterviewFaqDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            access!.UserId,
            SchoolPortalAuditActions.InterviewFaqPublished,
            "FaqItem",
            item.Id.ToString(),
            "Published school interview FAQ.",
            cancellationToken);

        logger.LogInformation("Published school interview FAQ {ItemId}.", item.Id);
        return Result<SchoolInterviewFaqDetailDto>.Success(SchoolInterviewFaqSupport.ToDetail(item));
    }
}
