using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;
using FluentValidation;
using Schoolera.Application.Meetings;

namespace Schoolera.Application.Admissions.Queries.GetAdmissionApplication;

public sealed record GetAdmissionApplicationQuery(Guid ApplicationId)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class GetAdmissionApplicationQueryValidator
    : AbstractValidator<GetAdmissionApplicationQuery>
{
    public GetAdmissionApplicationQueryValidator()
    {
        RuleFor(query => query.ApplicationId).NotEmpty();
    }
}

public sealed class GetAdmissionApplicationQueryHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    ICmsRepository cmsRepository,
    IChildAgeEligibilityEvaluator ageEligibilityEvaluator,
    IChildIdentityProtector identityProtector,
    IMeetingSessionService meetingSessions,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<GetAdmissionApplicationQueryHandler> logger)
    : IRequestHandler<GetAdmissionApplicationQuery, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        GetAdmissionApplicationQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionApplicationDetailDto>(localizer);
        }

        var application = await admissionRepository.GetOwnedAsync(
            userId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.NotFound<AdmissionApplicationDetailDto>();
        }

        var candidates = await cmsRepository.ListInterviewFaqsAsync(
            schoolId: application.SchoolId,
            interviewCategory: null,
            publishedOnly: true,
            activeOnly: true,
            branchId: application.SchoolBranchId,
            stageId: application.EducationalStageId,
            gradeId: application.GradeId,
            yearId: application.AcademicYearId,
            cancellationToken: cancellationToken);

        var interviewFaqs = candidates
            .Where(item =>
                InterviewFaqApplicability.Matches(
                    item,
                    application.SchoolBranchId,
                    application.EducationalStageId,
                    application.GradeId,
                    application.AcademicYearId))
            .OrderBy(item => item.OwnershipScope == FaqOwnershipScope.Platform ? 0 : 1)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Select(PublicInterviewFaqItemDto.FromEntity)
            .ToArray();

        var ageEligibility = AgeEligibilityMapping.FromSnapshot(application.AgeEligibilitySnapshot);
        if (ageEligibility is null ||
            application.AgeEligibilitySnapshot?.BirthDateSnapshot != application.ChildProfile?.BirthDate)
        {
            var live = await ageEligibilityEvaluator.EvaluateAsync(
                application.SchoolId,
                application.SchoolBranchId,
                application.EducationalStageId,
                application.GradeId,
                application.AcademicYearId,
                application.ChildProfile?.BirthDate,
                cancellationToken);

            if (application.AgeEligibilitySnapshot is { ManualExceptionIsApproved: true } &&
                live.ResultCode is ChildAgeEligibilityResultCode.NotEligibleBelowMinimum
                    or ChildAgeEligibilityResultCode.NotEligibleAboveMaximum &&
                live.ManualExceptionAllowed)
            {
                ageEligibility = AgeEligibilityMapping.FromSnapshot(application.AgeEligibilitySnapshot);
            }
            else
            {
                ageEligibility = AgeEligibilityMapping.FromEvaluation(live);
            }
        }

        logger.LogInformation(
            "Loaded admission application {ApplicationId} for parent {UserId}.",
            application.Id,
            userId);

        var detail = AdmissionMapping.ToDetail(
                application,
                identityProtector,
                requirementEvaluation: null,
                questionEvaluation: null,
                interviewFaqs,
                ageEligibility);
        var interview = AdmissionLifecycleMapping.ToActiveInterview(application);
        var assessment = AdmissionLifecycleMapping.ToActiveAssessment(application);
        var interviewMeeting = interview is null ? null :
            await meetingSessions.GetSummaryAsync(
                interview.Id, SlotKind.Interview, false, cancellationToken);
        var assessmentMeeting = assessment is null ? null :
            await meetingSessions.GetSummaryAsync(
                assessment.Id, SlotKind.Assessment, false, cancellationToken);
        detail = detail with
        {
            ActiveInterview = AdmissionLifecycleMapping.ToActiveInterview(
                application, interviewMeeting),
            ActiveAssessment = AdmissionLifecycleMapping.ToActiveAssessment(
                application, assessmentMeeting),
        };
        return Result<AdmissionApplicationDetailDto>.Success(detail);
    }
}
