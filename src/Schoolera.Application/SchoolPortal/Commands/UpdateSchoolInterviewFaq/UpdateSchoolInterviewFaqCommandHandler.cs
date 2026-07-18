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

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolInterviewFaq;

public sealed record UpdateSchoolInterviewFaqCommand(
    Guid SchoolId,
    Guid ItemId,
    UpdateSchoolInterviewFaqRequest Body) : IRequest<Result<SchoolInterviewFaqDetailDto>>;

public sealed class UpdateSchoolInterviewFaqCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository portalRepository,
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    ISchoolPortalAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolInterviewFaqCommandHandler> logger)
    : IRequestHandler<UpdateSchoolInterviewFaqCommand, Result<SchoolInterviewFaqDetailDto>>
{
    public async Task<Result<SchoolInterviewFaqDetailDto>> Handle(
        UpdateSchoolInterviewFaqCommand request,
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

        var body = request.Body;
        if (body.RowVersion is { Length: > 0 } &&
            CmsResults.HasRowVersionMismatch(body.RowVersion, item.RowVersion))
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewFaqDetailDto>(
                localizer, SchoolPortalErrorCodes.ConcurrentUpdate);
        }

        var applicabilityError = await SchoolInterviewFaqSupport.ValidateApplicabilityAsync(
            portalRepository,
            request.SchoolId,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId,
            cancellationToken);
        if (applicabilityError is not null)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewFaqDetailDto>(localizer, applicabilityError);
        }

        if (body.SchoolBranchId is { } branchId)
        {
            var branchCheck = SchoolPortalAccess.RequireBranch<SchoolInterviewFaqDetailDto>(
                access!, branchId, localizer);
            if (!branchCheck.Succeeded)
            {
                return branchCheck;
            }
        }

        item.Update(
            item.FaqCategoryId,
            body.QuestionAr,
            body.QuestionEn,
            contentSanitizer.SanitizeHtml(body.AnswerAr),
            contentSanitizer.SanitizeHtml(body.AnswerEn),
            body.InterviewCategory,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolInterviewFaqDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            access!.UserId,
            SchoolPortalAuditActions.InterviewFaqUpdated,
            "FaqItem",
            item.Id.ToString(),
            $"Updated school interview FAQ ({body.InterviewCategory}).",
            cancellationToken);

        logger.LogInformation("Updated school interview FAQ {ItemId}.", item.Id);
        return Result<SchoolInterviewFaqDetailDto>.Success(SchoolInterviewFaqSupport.ToDetail(item));
    }
}
