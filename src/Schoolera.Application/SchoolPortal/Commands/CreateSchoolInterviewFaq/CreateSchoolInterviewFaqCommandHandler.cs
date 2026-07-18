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
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolInterviewFaq;

public sealed record CreateSchoolInterviewFaqCommand(
    Guid SchoolId,
    CreateSchoolInterviewFaqRequest Body) : IRequest<Result<SchoolInterviewFaqDetailDto>>;

public sealed class CreateSchoolInterviewFaqCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository portalRepository,
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    ISchoolPortalAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolInterviewFaqCommandHandler> logger)
    : IRequestHandler<CreateSchoolInterviewFaqCommand, Result<SchoolInterviewFaqDetailDto>>
{
    public async Task<Result<SchoolInterviewFaqDetailDto>> Handle(
        CreateSchoolInterviewFaqCommand request,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await SchoolInterviewFaqSupport.ResolveWriteAccessAsync(
            portalAccess, request.SchoolId, localizer, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var body = request.Body;
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

        var category = await SchoolInterviewFaqSupport.EnsureInterviewCategoryAsync(
            cmsRepository, unitOfWork, cancellationToken);

        var sortOrder = await cmsRepository.GetNextFaqItemSortOrderAsync(
            category.Id,
            FaqOwnershipScope.School,
            request.SchoolId,
            cancellationToken);

        var item = new FaqItem(
            category.Id,
            body.QuestionAr,
            body.QuestionEn,
            contentSanitizer.SanitizeHtml(body.AnswerAr),
            contentSanitizer.SanitizeHtml(body.AnswerEn),
            sortOrder,
            FaqOwnershipScope.School,
            request.SchoolId,
            body.InterviewCategory,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId);

        await cmsRepository.AddFaqItemAsync(item, cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolInterviewFaqDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            access!.UserId,
            SchoolPortalAuditActions.InterviewFaqCreated,
            "FaqItem",
            item.Id.ToString(),
            $"Created school interview FAQ ({body.InterviewCategory}).",
            cancellationToken);

        logger.LogInformation(
            "Created school interview FAQ {ItemId} for school {SchoolId}.",
            item.Id,
            request.SchoolId);

        return Result<SchoolInterviewFaqDetailDto>.Success(SchoolInterviewFaqSupport.ToDetail(item));
    }
}
