using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolInterviewAssessmentPolicy;

public sealed record GetSchoolInterviewAssessmentPolicyQuery(
    Guid SchoolId,
    Guid PolicyId) : IRequest<Result<SchoolInterviewAssessmentPolicyDetailDto>>;

public sealed class GetSchoolInterviewAssessmentPolicyQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolInterviewAssessmentPolicyQuery, Result<SchoolInterviewAssessmentPolicyDetailDto>>
{
    public async Task<Result<SchoolInterviewAssessmentPolicyDetailDto>> Handle(
        GetSchoolInterviewAssessmentPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<SchoolInterviewAssessmentPolicyDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolInterviewAssessmentPolicyDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var entity = await policyRepository.GetByIdAsync(
            request.SchoolId, request.PolicyId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewAssessmentPolicyDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewAssessmentPolicyNotFound);
        }

        return Result<SchoolInterviewAssessmentPolicyDetailDto>.Success(
            SchoolInterviewAssessmentPolicyMapping.ToDetail(entity));
    }
}
