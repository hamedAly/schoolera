using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolAdmissionRequirements;

public sealed record ListSchoolAdmissionRequirementsQuery(
    Guid SchoolId,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    AdmissionRequirementKind? Kind,
    AdmissionRequirementPublicationStatus? PublicationStatus,
    bool? IsActive) : IRequest<Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>>
{
    public static ListSchoolAdmissionRequirementsQuery FromFilters(
        Guid schoolId,
        Guid? branchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int? kind,
        int? publicationStatus,
        bool? isActive) =>
        new(
            schoolId,
            branchId,
            educationalStageId,
            gradeId,
            academicYearId,
            kind is { } kindValue ? (AdmissionRequirementKind)kindValue : null,
            publicationStatus is { } statusValue
                ? (AdmissionRequirementPublicationStatus)statusValue
                : null,
            isActive);
}

public sealed class ListSchoolAdmissionRequirementsQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolAdmissionRequirementsQuery, Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>> Handle(
        ListSchoolAdmissionRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>.Failure(
                access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var items = await requirementRepository.ListAsync(
            request.SchoolId,
            request.BranchId,
            request.EducationalStageId,
            request.GradeId,
            request.AcademicYearId,
            request.Kind,
            request.PublicationStatus,
            request.IsActive,
            cancellationToken);

        return Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>.Success(
            items.Select(SchoolAdmissionRequirementMapping.ToListItem).ToArray());
    }
}
