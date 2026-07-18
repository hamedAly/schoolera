using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Queries.GetPublicInterviewAssessmentPolicySummary;

public sealed record GetPublicInterviewAssessmentPolicySummaryQuery(
    string Slug,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId) : IRequest<Result<SafeInterviewAssessmentPolicySummaryDto?>>;

public sealed class GetPublicInterviewAssessmentPolicySummaryQueryHandler(
    ISchoolReadRepository schoolReadRepository,
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    INotificationRepository notificationRepository,
    ILogger<GetPublicInterviewAssessmentPolicySummaryQueryHandler> logger)
    : IRequestHandler<GetPublicInterviewAssessmentPolicySummaryQuery, Result<SafeInterviewAssessmentPolicySummaryDto?>>
{
    public async Task<Result<SafeInterviewAssessmentPolicySummaryDto?>> Handle(
        GetPublicInterviewAssessmentPolicySummaryQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Public interview/assessment policy summary for slug {Slug}.", request.Slug);

        var school = await schoolReadRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);
        if (school is null)
        {
            return Result<SafeInterviewAssessmentPolicySummaryDto?>.Failure(
                ["School not found."],
                [SchoolErrorCodes.NotFound]);
        }

        var published = await policyRepository.ListPublishedActiveAsync(school.Id, cancellationToken);
        Domain.Entities.SchoolInterviewAssessmentPolicy? matched = null;

        if (request.BranchId is { } branchId &&
            request.EducationalStageId is { } stageId &&
            request.GradeId is { } gradeId &&
            request.AcademicYearId is { } yearId)
        {
            matched = InterviewAssessmentPolicyCatalog.ResolveApplicable(
                published, branchId, stageId, gradeId, yearId);
        }
        else
        {
            matched = published
                .Where(policy =>
                    policy.SchoolBranchId is null &&
                    policy.EducationalStageId is null &&
                    policy.GradeId is null &&
                    policy.AcademicYearId is null)
                .OrderBy(policy => policy.Id)
                .FirstOrDefault();
        }

        if (matched is null)
        {
            return Result<SafeInterviewAssessmentPolicySummaryDto?>.Success(null);
        }

        var preferArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        var onlineAvailable = false;
        if (SchoolInterviewAssessmentPolicyMapping.NeedsOnlineCapability(matched.DeliveryMode))
        {
            var meetings = await notificationRepository.ListIntegrationsAsync(
                IntegrationType.Meeting,
                providerCode: matched.MeetingProviderCode,
                isActive: true,
                healthStatus: null,
                cancellationToken);
            onlineAvailable = meetings.Count > 0;
        }

        return Result<SafeInterviewAssessmentPolicySummaryDto?>.Success(
            SchoolInterviewAssessmentPolicyMapping.ToSafeSummary(matched, preferArabic, onlineAvailable));
    }
}
