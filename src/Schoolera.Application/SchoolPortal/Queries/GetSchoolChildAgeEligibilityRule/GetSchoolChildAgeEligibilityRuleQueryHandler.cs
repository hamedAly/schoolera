using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolChildAgeEligibilityRule;

public sealed record GetSchoolChildAgeEligibilityRuleQuery(
    Guid SchoolId,
    Guid RuleId) : IRequest<Result<SchoolChildAgeEligibilityRuleDetailDto>>;

public sealed class GetSchoolChildAgeEligibilityRuleQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetSchoolChildAgeEligibilityRuleQuery, Result<SchoolChildAgeEligibilityRuleDetailDto>>
{
    public async Task<Result<SchoolChildAgeEligibilityRuleDetailDto>> Handle(
        GetSchoolChildAgeEligibilityRuleQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<SchoolChildAgeEligibilityRuleDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<SchoolChildAgeEligibilityRuleDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var entity = await ruleRepository.GetByIdAsync(
            request.SchoolId, request.RuleId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRuleNotFound);
        }

        return Result<SchoolChildAgeEligibilityRuleDetailDto>.Success(
            SchoolChildAgeEligibilityRuleMapping.ToDetail(entity));
    }
}
