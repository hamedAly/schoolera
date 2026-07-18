using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.PreviewSchoolChildAgeEligibility;

public sealed record PreviewSchoolChildAgeEligibilityQuery(
    Guid SchoolId,
    PreviewSchoolChildAgeEligibilityRequest Body)
    : IRequest<Result<PreviewSchoolChildAgeEligibilityDto>>;

public sealed class PreviewSchoolChildAgeEligibilityQueryValidator
    : AbstractValidator<PreviewSchoolChildAgeEligibilityQuery>
{
    public PreviewSchoolChildAgeEligibilityQueryValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.EducationalStageId).NotEmpty();
        RuleFor(x => x.Body.AcademicYearId).NotEmpty();
    }
}

public sealed class PreviewSchoolChildAgeEligibilityQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IChildAgeEligibilityEvaluator evaluator,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<PreviewSchoolChildAgeEligibilityQuery, Result<PreviewSchoolChildAgeEligibilityDto>>
{
    public async Task<Result<PreviewSchoolChildAgeEligibilityDto>> Handle(
        PreviewSchoolChildAgeEligibilityQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<PreviewSchoolChildAgeEligibilityDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<PreviewSchoolChildAgeEligibilityDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var body = request.Body;
        var evaluation = await evaluator.EvaluateAsync(
            request.SchoolId,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId,
            body.BirthDate,
            cancellationToken);

        SchoolChildAgeEligibilityRuleDetailDto? matchedDetail = null;
        if (evaluation.RuleId is { } ruleId)
        {
            var rule = await ruleRepository.GetByIdAsync(request.SchoolId, ruleId, cancellationToken);
            if (rule is not null)
            {
                matchedDetail = SchoolChildAgeEligibilityRuleMapping.ToDetail(rule);
            }
        }

        return Result<PreviewSchoolChildAgeEligibilityDto>.Success(
            new PreviewSchoolChildAgeEligibilityDto(
                evaluation.ResultCode,
                evaluation.CalculatedAgeCompletedMonths,
                evaluation.MinAgeCompletedMonths,
                evaluation.MaxAgeCompletedMonths,
                evaluation.ReferenceDate,
                evaluation.RuleId,
                evaluation.RuleVersion,
                evaluation.ManualExceptionAllowed,
                evaluation.CanContinue,
                matchedDetail));
    }
}
