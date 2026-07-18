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

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolChildAgeEligibilityRule;

public sealed record CreateSchoolChildAgeEligibilityRuleCommand(
    Guid SchoolId,
    CreateSchoolChildAgeEligibilityRuleRequest Body)
    : IRequest<Result<SchoolChildAgeEligibilityRuleDetailDto>>;

public sealed class CreateSchoolChildAgeEligibilityRuleCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository portalRepository,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolChildAgeEligibilityRuleCommandHandler> logger)
    : IRequestHandler<CreateSchoolChildAgeEligibilityRuleCommand, Result<SchoolChildAgeEligibilityRuleDetailDto>>
{
    public async Task<Result<SchoolChildAgeEligibilityRuleDetailDto>> Handle(
        CreateSchoolChildAgeEligibilityRuleCommand request,
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

        try
        {
            var entity = new SchoolChildAgeEligibilityRule(
                request.SchoolId,
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

            await ruleRepository.AddAsync(entity, cancellationToken);
            await ruleRepository.AddAuditAsync(
                new SchoolChildAgeEligibilityRuleAudit(
                    request.SchoolId,
                    entity.Id,
                    SchoolChildAgeEligibilityRuleAuditActions.Created,
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
                "Created age eligibility rule {RuleId} for school {SchoolId}.",
                entity.Id,
                request.SchoolId);
            return Result<SchoolChildAgeEligibilityRuleDetailDto>.Success(
                SchoolChildAgeEligibilityRuleMapping.ToDetail(entity));
        }
        catch (ArgumentOutOfRangeException)
        {
            return SchoolPortalResults.FailureForCode<SchoolChildAgeEligibilityRuleDetailDto>(
                localizer, SchoolPortalErrorCodes.AgeEligibilityRulePublishInvalid);
        }
    }
}
