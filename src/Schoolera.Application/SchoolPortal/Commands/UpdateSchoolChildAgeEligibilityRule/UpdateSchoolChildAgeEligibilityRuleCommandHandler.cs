using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolChildAgeEligibilityRule;

public sealed record UpdateSchoolChildAgeEligibilityRuleCommand(
    Guid SchoolId,
    Guid RuleId,
    UpdateSchoolChildAgeEligibilityRuleRequest Body)
    : IRequest<Result<SchoolChildAgeEligibilityRuleDetailDto>>;

public sealed class UpdateSchoolChildAgeEligibilityRuleCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository portalRepository,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolChildAgeEligibilityRuleCommandHandler> logger)
    : IRequestHandler<UpdateSchoolChildAgeEligibilityRuleCommand, Result<SchoolChildAgeEligibilityRuleDetailDto>>
{
    public async Task<Result<SchoolChildAgeEligibilityRuleDetailDto>> Handle(
        UpdateSchoolChildAgeEligibilityRuleCommand request,
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

        if (request.Body.RowVersion is { Length: > 0 } rowVersion &&
            entity.RowVersion is { Length: > 0 } &&
            !rowVersion.AsSpan().SequenceEqual(entity.RowVersion))
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.ConcurrentUpdate);
        }

        if (entity.PublicationStatus == ChildAgeEligibilityPublicationStatus.Published)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRuleMustUnpublish);
        }

        var body = request.Body;
        if (!ChildAgeEligibilityCatalog.IsValidDefinitionScope(
                body.SchoolBranchId, body.EducationalStageId, body.GradeId, body.AcademicYearId))
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRuleInvalidScope);
        }

        if (body.SchoolBranchId is { } branchId)
        {
            var branch = await portalRepository.GetBranchForWriteAsync(
                request.SchoolId, branchId, cancellationToken);
            if (branch is null)
            {
                return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                    localizer, SchoolPortalErrorCodes.BranchNotFound);
            }

            var branchCheck = SchoolPortalAccess.RequireBranch<SchoolChildAgeEligibilityRuleDetailDto>(
                access.Data!, branchId, localizer);
            if (!branchCheck.Succeeded)
            {
                return branchCheck;
            }
        }

        if (!await portalRepository.AcademicYearExistsAsync(body.AcademicYearId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.InvalidAcademicYear);
        }

        var previousScopeKey = entity.ScopeKey;
        try
        {
            entity.UpdateDraft(
                body.EducationalStageId,
                body.AcademicYearId,
                body.MinAgeCompletedMonths,
                body.MaxAgeCompletedMonths,
                body.ReferenceDateMode,
                body.ExplanationAr,
                body.ExplanationEn,
                body.ManualExceptionAllowed,
                body.SchoolBranchId,
                body.GradeId,
                access.Data.UserId);
        }
        catch (InvalidOperationException)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRuleMustUnpublish);
        }
        catch (ArgumentOutOfRangeException)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRulePublishInvalid);
        }

        var scopeChanged = !string.Equals(previousScopeKey, entity.ScopeKey, StringComparison.Ordinal);
        await ruleRepository.AddAuditAsync(
            new SchoolChildAgeEligibilityRuleAudit(
                request.SchoolId,
                entity.Id,
                scopeChanged
                    ? SchoolChildAgeEligibilityRuleAuditActions.ScopeChanged
                    : SchoolChildAgeEligibilityRuleAuditActions.Updated,
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
            "Updated age eligibility rule {RuleId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolChildAgeEligibilityRuleDetailDto>.Success(
            SchoolChildAgeEligibilityRuleMapping.ToDetail(entity));
    }
}
