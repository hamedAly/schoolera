using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolAdmissionRequirement;

public sealed record GetSchoolAdmissionRequirementQuery(
    Guid SchoolId,
    Guid RequirementId) : IRequest<Result<SchoolAdmissionRequirementDetailDto>>;

public sealed class GetSchoolAdmissionRequirementQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolAdmissionRequirementQuery, Result<SchoolAdmissionRequirementDetailDto>>
{
    public async Task<Result<SchoolAdmissionRequirementDetailDto>> Handle(
        GetSchoolAdmissionRequirementQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<SchoolAdmissionRequirementDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolAdmissionRequirementDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var entity = await requirementRepository.GetByIdAsync(
            request.SchoolId,
            request.RequirementId,
            cancellationToken);

        return entity is null
            ? SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementNotFound)
            : Result<SchoolAdmissionRequirementDetailDto>.Success(
                SchoolAdmissionRequirementMapping.ToDetail(entity));
    }
}
