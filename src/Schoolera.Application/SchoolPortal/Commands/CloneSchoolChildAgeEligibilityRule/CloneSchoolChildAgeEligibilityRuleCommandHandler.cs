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

namespace Schoolera.Application.SchoolPortal.Commands.CloneSchoolChildAgeEligibilityRule;

public sealed record CloneSchoolChildAgeEligibilityRuleCommand(
    Guid SchoolId,
    Guid RuleId,
    CloneSchoolChildAgeEligibilityRuleRequest? Body = null)
    : IRequest<Result<SchoolChildAgeEligibilityRuleDetailDto>>;

public sealed class CloneSchoolChildAgeEligibilityRuleCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CloneSchoolChildAgeEligibilityRuleCommandHandler> logger)
    : IRequestHandler<CloneSchoolChildAgeEligibilityRuleCommand, Result<SchoolChildAgeEligibilityRuleDetailDto>>
{
    public async Task<Result<SchoolChildAgeEligibilityRuleDetailDto>> Handle(
        CloneSchoolChildAgeEligibilityRuleCommand request,
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

        var source = await ruleRepository.GetByIdAsync(
            request.SchoolId, request.RuleId, cancellationToken);
        if (source is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRuleNotFound);
        }

        var clone = source.CloneAsDraft(access.Data.UserId);
        await ruleRepository.AddAsync(clone, cancellationToken);
        await ruleRepository.AddAuditAsync(
            new SchoolChildAgeEligibilityRuleAudit(
                request.SchoolId,
                clone.Id,
                SchoolChildAgeEligibilityRuleAuditActions.Cloned,
                access.Data.UserId,
                metadata: $"source={source.Id:N}"),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolChildAgeEligibilityRuleDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Cloned age eligibility rule {SourceId} to {RuleId} for school {SchoolId}.",
            source.Id,
            clone.Id,
            request.SchoolId);
        return Result<SchoolChildAgeEligibilityRuleDetailDto>.Success(
            SchoolChildAgeEligibilityRuleMapping.ToDetail(clone));
    }
}
