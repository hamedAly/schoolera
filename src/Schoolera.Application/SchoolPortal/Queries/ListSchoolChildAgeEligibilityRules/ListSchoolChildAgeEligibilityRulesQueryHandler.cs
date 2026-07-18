using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolChildAgeEligibilityRules;

public sealed record ListSchoolChildAgeEligibilityRulesQuery(
    Guid SchoolId,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    ChildAgeEligibilityPublicationStatus? PublicationStatus,
    bool? IsActive) : IRequest<Result<IReadOnlyList<SchoolChildAgeEligibilityRuleListItemDto>>>
{
    public static ListSchoolChildAgeEligibilityRulesQuery FromFilters(
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
                ? (ChildAgeEligibilityPublicationStatus)statusValue
                : null,
            isActive);
}

public sealed class ListSchoolChildAgeEligibilityRulesQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolChildAgeEligibilityRulesQuery, Result<IReadOnlyList<SchoolChildAgeEligibilityRuleListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolChildAgeEligibilityRuleListItemDto>>> Handle(
        ListSchoolChildAgeEligibilityRulesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<IReadOnlyList<SchoolChildAgeEligibilityRuleListItemDto>>.Failure(
                access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolChildAgeEligibilityRuleListItemDto>>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var items = await ruleRepository.ListAsync(
            request.SchoolId,
            request.BranchId,
            request.EducationalStageId,
            request.GradeId,
            request.AcademicYearId,
            request.PublicationStatus,
            request.IsActive,
            cancellationToken);

        return Result<IReadOnlyList<SchoolChildAgeEligibilityRuleListItemDto>>.Success(
            items.Select(SchoolChildAgeEligibilityRuleMapping.ToListItem).ToArray());
    }
}
