using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Queries.GetParentDashboard;

public sealed record GetParentDashboardQuery : IRequest<Result<ParentDashboardDto>>;

public sealed class GetParentDashboardQueryHandler(
    ICurrentUser currentUser,
    IParentProfileRepository parentProfileRepository,
    IChildProfileRepository childProfileRepository,
    IAdmissionApplicationRepository admissionRepository,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<GetParentDashboardQueryHandler> logger)
    : IRequestHandler<GetParentDashboardQuery, Result<ParentDashboardDto>>
{
    public async Task<Result<ParentDashboardDto>> Handle(
        GetParentDashboardQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentDashboardDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        logger.LogInformation("Loading parent dashboard for user {UserId}.", userId);

        var children = await childProfileRepository.ListByParentUserIdAsync(
            userId,
            includeInactive: true,
            cancellationToken);
        var activeCount = children.Count(child => child.IsActive);
        var profile = await parentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        var incomplete = profile is null ||
            profile.CityId is null ||
            profile.DistrictId is null;

        var counts = await admissionRepository.CountByStatusForParentAsync(userId, cancellationToken);
        int CountOf(AdmissionApplicationStatus status) =>
            counts.TryGetValue(status, out var value) ? value : 0;

        var draft = CountOf(AdmissionApplicationStatus.Draft);
        var submitted = CountOf(AdmissionApplicationStatus.Submitted);
        var underReview = CountOf(AdmissionApplicationStatus.UnderReview);
        var missingDocuments = CountOf(AdmissionApplicationStatus.MissingDocuments);
        var interviewRequired = CountOf(AdmissionApplicationStatus.InterviewRequired);
        var assessmentRequired = CountOf(AdmissionApplicationStatus.AssessmentRequired);
        var waitingList = CountOf(AdmissionApplicationStatus.WaitingList);
        var accepted = CountOf(AdmissionApplicationStatus.Accepted);
        var rejected = CountOf(AdmissionApplicationStatus.Rejected);
        var cancelled = CountOf(AdmissionApplicationStatus.Cancelled);
        var registered = CountOf(AdmissionApplicationStatus.Registered);
        var total = draft + submitted + underReview + missingDocuments + interviewRequired +
            assessmentRequired + waitingList + accepted + rejected + cancelled + registered;

        var activities = await admissionRepository.ListRecentParentVisibleActivityAsync(
            userId,
            take: 5,
            cancellationToken);
        var hasActivity = activities.Count > 0;

        return Result<ParentDashboardDto>.Success(
            new ParentDashboardDto(
                ChildCount: activeCount,
                ActiveChildCount: activeCount,
                ApplicationsAvailable: true,
                TotalApplications: total,
                DraftApplications: draft,
                SubmittedApplications: submitted,
                UnderReviewApplications: underReview + missingDocuments + interviewRequired +
                    assessmentRequired + waitingList,
                AcceptedApplications: accepted,
                RejectedApplications: rejected,
                CancelledApplications: cancelled,
                RecentActivityAvailable: hasActivity,
                RecentActivities: hasActivity ? activities : null,
                ProfileIncomplete: incomplete));
    }
}
