using Schoolera.Application.Admissions.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Common;

internal static class SchoolAdmissionRequirementMapping
{
    public static SchoolAdmissionRequirementListItemDto ToListItem(SchoolAdmissionRequirement requirement) =>
        new(
            requirement.Id,
            requirement.RequirementCode,
            requirement.Kind,
            requirement.NameAr,
            requirement.NameEn,
            requirement.IsRequired,
            requirement.SortOrder,
            requirement.PublicationStatus,
            requirement.IsActive,
            requirement.SchoolBranchId,
            requirement.EducationalStageId,
            requirement.GradeId,
            requirement.AcademicYearId,
            requirement.UpdatedAtUtc);

    public static SchoolAdmissionRequirementDetailDto ToDetail(SchoolAdmissionRequirement requirement) =>
        new(
            requirement.Id,
            requirement.RequirementCode,
            requirement.Kind,
            requirement.NameAr,
            requirement.NameEn,
            requirement.DescriptionAr,
            requirement.DescriptionEn,
            requirement.IsRequired,
            requirement.SortOrder,
            requirement.PublicationStatus,
            requirement.IsActive,
            requirement.SchoolBranchId,
            requirement.EducationalStageId,
            requirement.GradeId,
            requirement.AcademicYearId,
            requirement.ScopeKey,
            requirement.SpecificityScore,
            requirement.ProfileFieldCode,
            requirement.DocumentCode,
            AdmissionRequirementCatalog.ParseExtensions(requirement.AllowedFileExtensions),
            requirement.MaxFileSizeBytes,
            requirement.AllowChildVaultCopy,
            requirement.CreatedAtUtc,
            requirement.UpdatedAtUtc,
            requirement.PublishedAtUtc);

    public static string? ValidateForPublish(
        SchoolAdmissionRequirement requirement,
        long platformMaxBytes)
    {
        if (requirement.SpecificityScore <= 0)
        {
            return SchoolPortalErrorCodes.AdmissionRequirementInvalidScope;
        }

        switch (requirement.Kind)
        {
            case AdmissionRequirementKind.InformationalText:
                if (requirement.ProfileFieldCode is not null || requirement.DocumentCode is not null)
                {
                    return SchoolPortalErrorCodes.AdmissionRequirementInvalidKind;
                }

                break;

            case AdmissionRequirementKind.ParentProfileField:
            case AdmissionRequirementKind.ChildProfileField:
                if (!AdmissionRequirementCatalog.IsValidProfileField(requirement.Kind, requirement.ProfileFieldCode))
                {
                    return SchoolPortalErrorCodes.AdmissionRequirementInvalidField;
                }

                if (requirement.DocumentCode is not null)
                {
                    return SchoolPortalErrorCodes.AdmissionRequirementInvalidKind;
                }

                break;

            case AdmissionRequirementKind.ApplicationDocument:
                if (!AdmissionRequirementCatalog.IsValidDocumentCode(requirement.DocumentCode))
                {
                    return SchoolPortalErrorCodes.AdmissionRequirementInvalidDocument;
                }

                if (requirement.ProfileFieldCode is not null)
                {
                    return SchoolPortalErrorCodes.AdmissionRequirementInvalidKind;
                }

                var extensions = AdmissionRequirementCatalog.ParseExtensions(requirement.AllowedFileExtensions);
                if (extensions.Count == 0)
                {
                    return SchoolPortalErrorCodes.AdmissionRequirementInvalidFileTypes;
                }

                if (requirement.MaxFileSizeBytes is { } max &&
                    (max <= 0 || max > platformMaxBytes))
                {
                    return SchoolPortalErrorCodes.AdmissionRequirementInvalidMaxSize;
                }

                break;

            default:
                return SchoolPortalErrorCodes.AdmissionRequirementInvalidKind;
        }

        return null;
    }
}
