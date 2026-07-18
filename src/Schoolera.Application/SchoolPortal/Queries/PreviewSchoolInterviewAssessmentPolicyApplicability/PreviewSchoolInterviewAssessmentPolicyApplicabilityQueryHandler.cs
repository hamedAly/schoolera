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

namespace Schoolera.Application.SchoolPortal.Queries.PreviewSchoolInterviewAssessmentPolicyApplicability;

public sealed record PreviewSchoolInterviewAssessmentPolicyApplicabilityQuery(
    Guid SchoolId,
    PreviewSchoolInterviewAssessmentPolicyApplicabilityRequest Body)
    : IRequest<Result<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto>>;

public sealed class PreviewSchoolInterviewAssessmentPolicyApplicabilityQueryValidator
    : AbstractValidator<PreviewSchoolInterviewAssessmentPolicyApplicabilityQuery>
{
    public PreviewSchoolInterviewAssessmentPolicyApplicabilityQueryValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.SchoolBranchId).NotEmpty();
        RuleFor(x => x.Body.EducationalStageId).NotEmpty();
        RuleFor(x => x.Body.GradeId).NotEmpty();
        RuleFor(x => x.Body.AcademicYearId).NotEmpty();
    }
}

public sealed class PreviewSchoolInterviewAssessmentPolicyApplicabilityQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<PreviewSchoolInterviewAssessmentPolicyApplicabilityQuery, Result<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto>>
{
    public async Task<Result<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto>> Handle(
        PreviewSchoolInterviewAssessmentPolicyApplicabilityQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto>.Failure(
                access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var published = await policyRepository.ListPublishedActiveAsync(request.SchoolId, cancellationToken);
        var matched = InterviewAssessmentPolicyCatalog.ResolveApplicable(
            published,
            request.Body.SchoolBranchId,
            request.Body.EducationalStageId,
            request.Body.GradeId,
            request.Body.AcademicYearId);

        return Result<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto>.Success(
            new PreviewSchoolInterviewAssessmentPolicyApplicabilityDto(
                matched is null ? null : SchoolInterviewAssessmentPolicyMapping.ToDetail(matched),
                matched?.SpecificityScore));
    }
}
