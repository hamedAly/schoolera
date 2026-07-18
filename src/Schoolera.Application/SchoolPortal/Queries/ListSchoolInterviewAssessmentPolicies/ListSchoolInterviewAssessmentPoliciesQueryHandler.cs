using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolInterviewAssessmentPolicies;

public sealed record ListSchoolInterviewAssessmentPoliciesQuery(
    Guid SchoolId,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    InterviewAssessmentPolicyPublicationStatus? PublicationStatus,
    bool? IsActive) : IRequest<Result<IReadOnlyList<SchoolInterviewAssessmentPolicyListItemDto>>>
{
    public static ListSchoolInterviewAssessmentPoliciesQuery FromFilters(
        Guid schoolId,
        Guid? branchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int? publicationStatus,
        bool? isActive) =>
        new(
            schoolId,
            branchId,
            educationalStageId,
            gradeId,
            academicYearId,
            publicationStatus is { } statusValue
                ? (InterviewAssessmentPolicyPublicationStatus)statusValue
                : null,
            isActive);
}

public sealed class ListSchoolInterviewAssessmentPoliciesQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolInterviewAssessmentPoliciesQuery, Result<IReadOnlyList<SchoolInterviewAssessmentPolicyListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolInterviewAssessmentPolicyListItemDto>>> Handle(
        ListSchoolInterviewAssessmentPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<IReadOnlyList<SchoolInterviewAssessmentPolicyListItemDto>>.Failure(
                access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolInterviewAssessmentPolicyListItemDto>>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var items = await policyRepository.ListAsync(
            request.SchoolId,
            request.BranchId,
            request.EducationalStageId,
            request.GradeId,
            request.AcademicYearId,
            request.PublicationStatus,
            request.IsActive,
            cancellationToken);

        return Result<IReadOnlyList<SchoolInterviewAssessmentPolicyListItemDto>>.Success(
            items.Select(SchoolInterviewAssessmentPolicyMapping.ToListItem).ToArray());
    }
}
