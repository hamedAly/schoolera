using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;

namespace Schoolera.Application.Admissions.Queries.GetSchoolAdmissionApplication;

public sealed record GetSchoolAdmissionApplicationQuery(Guid SchoolId, Guid ApplicationId)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class GetSchoolAdmissionApplicationQueryHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IChildAgeEligibilityEvaluator ageEligibilityEvaluator,
    IChildIdentityProtector identityProtector,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<GetSchoolAdmissionApplicationQueryHandler> logger)
    : IRequestHandler<GetSchoolAdmissionApplicationQuery, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        GetSchoolAdmissionApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolAdmissionApplicationDetailDto>.Failure(
                accessResult.Errors,
                accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolAdmissionApplicationDetailDto>(
            access, SchoolPortalPermission.ViewApplications, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var application = await admissionRepository.GetForSchoolAsync(
            request.SchoolId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.ReviewNotFound<SchoolAdmissionApplicationDetailDto>();
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<SchoolAdmissionApplicationDetailDto>(
            access, application.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        var users = await userDirectory.GetUsersAsync([application.ParentUserId], cancellationToken);
        users.TryGetValue(application.ParentUserId, out var parentUser);

        var ageEligibility = AgeEligibilityMapping.FromSnapshot(application.AgeEligibilitySnapshot);
        if (ageEligibility is null)
        {
            var live = await ageEligibilityEvaluator.EvaluateAsync(
                application.SchoolId,
                application.SchoolBranchId,
                application.EducationalStageId,
                application.GradeId,
                application.AcademicYearId,
                application.ChildProfile?.BirthDate,
                cancellationToken);
            ageEligibility = AgeEligibilityMapping.FromEvaluation(live);
        }

        var canGrant = AgeEligibilityMapping.ComputeCanGrantAgeException(
            access.HasPermission(SchoolPortalPermission.ManageApplicationReview),
            application.Status,
            ageEligibility);

        logger.LogInformation(
            "Loaded school admission application {ApplicationId} for school {SchoolId}.",
            application.Id,
            request.SchoolId);

        return Result<SchoolAdmissionApplicationDetailDto>.Success(
            SchoolAdmissionMapping.ToSchoolDetail(
                application,
                SchoolAdmissionMapping.ToParentContact(parentUser),
                identityProtector,
                ageEligibility,
                canGrant));
    }
}
