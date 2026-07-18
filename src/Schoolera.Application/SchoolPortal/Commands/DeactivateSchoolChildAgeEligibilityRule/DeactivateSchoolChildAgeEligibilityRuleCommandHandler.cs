using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolChildAgeEligibilityRule;

public sealed record DeactivateSchoolChildAgeEligibilityRuleCommand(
    Guid SchoolId,
    Guid RuleId) : IRequest<Result<SchoolChildAgeEligibilityRuleDetailDto>>;

public sealed class DeactivateSchoolChildAgeEligibilityRuleCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeactivateSchoolChildAgeEligibilityRuleCommandHandler> logger)
    : IRequestHandler<DeactivateSchoolChildAgeEligibilityRuleCommand, Result<SchoolChildAgeEligibilityRuleDetailDto>>
{
    public async Task<Result<SchoolChildAgeEligibilityRuleDetailDto>> Handle(
        DeactivateSchoolChildAgeEligibilityRuleCommand request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<SchoolChildAgeEligibilityRuleDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolChildAgeEligibilityRuleDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var entity = await ruleRepository.GetByIdForUpdateAsync(
            request.SchoolId, request.RuleId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRuleNotFound);
        }

        entity.Deactivate(access.Data.UserId);

        await ruleRepository.AddAuditAsync(
            new SchoolChildAgeEligibilityRuleAudit(
                request.SchoolId,
                entity.Id,
                SchoolChildAgeEligibilityRuleAuditActions.Deactivated,
                access.Data.UserId,
                metadata: null),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolChildAgeEligibilityRuleDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Deactivated age eligibility rule {RuleId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolChildAgeEligibilityRuleDetailDto>.Success(
            SchoolChildAgeEligibilityRuleMapping.ToDetail(entity));
    }
}
