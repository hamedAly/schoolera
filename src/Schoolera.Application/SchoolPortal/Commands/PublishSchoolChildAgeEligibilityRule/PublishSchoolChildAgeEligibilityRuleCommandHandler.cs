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

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolChildAgeEligibilityRule;

public sealed record PublishSchoolChildAgeEligibilityRuleCommand(
    Guid SchoolId,
    Guid RuleId) : IRequest<Result<SchoolChildAgeEligibilityRuleDetailDto>>;

public sealed class PublishSchoolChildAgeEligibilityRuleCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<PublishSchoolChildAgeEligibilityRuleCommandHandler> logger)
    : IRequestHandler<PublishSchoolChildAgeEligibilityRuleCommand, Result<SchoolChildAgeEligibilityRuleDetailDto>>
{
    public async Task<Result<SchoolChildAgeEligibilityRuleDetailDto>> Handle(
        PublishSchoolChildAgeEligibilityRuleCommand request,
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

        var validationCode = SchoolChildAgeEligibilityRuleMapping.ValidateForPublish(entity);
        if (validationCode is not null)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, validationCode);
        }

        if (await ruleRepository.HasPublishedConflictAsync(
                request.SchoolId,
                entity.ScopeKey,
                entity.Id,
                cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRuleConflict);
        }

        try
        {
            entity.Publish(access.Data.UserId);
        }
        catch (InvalidOperationException)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRulePublishInvalid);
        }

        await ruleRepository.AddAuditAsync(
            new SchoolChildAgeEligibilityRuleAudit(
                request.SchoolId,
                entity.Id,
                SchoolChildAgeEligibilityRuleAuditActions.Published,
                access.Data.UserId,
                metadata: $"version={entity.RuleVersion}"),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolChildAgeEligibilityRuleDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Published age eligibility rule {RuleId} v{Version} for school {SchoolId}.",
            entity.Id,
            entity.RuleVersion,
            request.SchoolId);
        return Result<SchoolChildAgeEligibilityRuleDetailDto>.Success(
            SchoolChildAgeEligibilityRuleMapping.ToDetail(entity));
    }
}
