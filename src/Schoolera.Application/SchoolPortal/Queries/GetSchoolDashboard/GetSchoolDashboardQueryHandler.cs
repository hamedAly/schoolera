using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolDashboard;

public sealed record GetSchoolDashboardQuery(Guid SchoolId) : IRequest<Result<SchoolDashboardDto>>;

public sealed class GetSchoolDashboardQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IAdmissionApplicationRepository admissionRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolDashboardQuery, Result<SchoolDashboardDto>>
{
    public async Task<Result<SchoolDashboardDto>> Handle(
        GetSchoolDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolDashboardDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolDashboardDto>(
            accessResult.Data, SchoolPortalPermission.ViewDashboard, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var counts = await repository.GetDashboardCountsAsync(request.SchoolId, cancellationToken);
        var statusCounts = await admissionRepository.CountByStatusForSchoolAsync(
            request.SchoolId,
            cancellationToken);
        var recent = await admissionRepository.ListRecentSubmittedForSchoolAsync(
            request.SchoolId,
            take: 5,
            cancellationToken);

        static int Count(Dictionary<AdmissionApplicationStatus, int> map, AdmissionApplicationStatus status) =>
            map.TryGetValue(status, out var value) ? value : 0;

        var submitted = Count(statusCounts, AdmissionApplicationStatus.Submitted);
        var underReview = Count(statusCounts, AdmissionApplicationStatus.UnderReview);
        var missingDocuments = Count(statusCounts, AdmissionApplicationStatus.MissingDocuments);
        var interviewRequired = Count(statusCounts, AdmissionApplicationStatus.InterviewRequired);
        var assessmentRequired = Count(statusCounts, AdmissionApplicationStatus.AssessmentRequired);
        var waitingList = Count(statusCounts, AdmissionApplicationStatus.WaitingList);
        var accepted = Count(statusCounts, AdmissionApplicationStatus.Accepted);
        var rejected = Count(statusCounts, AdmissionApplicationStatus.Rejected);
        var registered = Count(statusCounts, AdmissionApplicationStatus.Registered);
        var totalActive = submitted + underReview + missingDocuments + interviewRequired +
            assessmentRequired + waitingList + accepted + registered;

        return Result<SchoolDashboardDto>.Success(
            SchoolPortalReadModel.BuildDashboard(
                request.SchoolId,
                accessResult.Data.SchoolStatus,
                accessResult.Data.IsEditable,
                counts,
                submitted,
                underReview + missingDocuments + interviewRequired + assessmentRequired + waitingList,
                accepted,
                rejected,
                totalActive,
                recent));
    }
}
