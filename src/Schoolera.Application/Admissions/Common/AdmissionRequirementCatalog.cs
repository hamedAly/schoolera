using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Platform allowlists for school-configurable admission requirements.</summary>
public static class AdmissionRequirementCatalog
{
    public static readonly IReadOnlySet<AdmissionProfileFieldCode> ParentFieldCodes =
        new HashSet<AdmissionProfileFieldCode>
        {
            AdmissionProfileFieldCode.ParentOccupation,
            AdmissionProfileFieldCode.ParentQualification,
            AdmissionProfileFieldCode.FatherFullName,
            AdmissionProfileFieldCode.FatherPhone,
            AdmissionProfileFieldCode.FatherOccupation,
            AdmissionProfileFieldCode.FatherQualification,
            AdmissionProfileFieldCode.MotherFullName,
            AdmissionProfileFieldCode.MotherPhone,
            AdmissionProfileFieldCode.MotherOccupation,
            AdmissionProfileFieldCode.MotherQualification,
        };

    public static readonly IReadOnlySet<AdmissionProfileFieldCode> ChildFieldCodes =
        new HashSet<AdmissionProfileFieldCode>
        {
            AdmissionProfileFieldCode.ChildCurrentSchoolName,
            AdmissionProfileFieldCode.ChildPreferredStudyLanguage,
            AdmissionProfileFieldCode.ChildHasSpecialNeeds,
            AdmissionProfileFieldCode.ChildSpecialNeedsNotes,
            AdmissionProfileFieldCode.ChildSkills,
            AdmissionProfileFieldCode.ChildHobbies,
            AdmissionProfileFieldCode.ChildStrengths,
            AdmissionProfileFieldCode.ChildImprovementAreas,
        };

    public static readonly IReadOnlySet<string> PlatformFileExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp",
        };

    public static readonly IReadOnlyDictionary<string, string> ExtensionToContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
        };

    public static bool IsValidProfileField(AdmissionRequirementKind kind, AdmissionProfileFieldCode? code) =>
        kind switch
        {
            AdmissionRequirementKind.ParentProfileField =>
                code is { } value && ParentFieldCodes.Contains(value),
            AdmissionRequirementKind.ChildProfileField =>
                code is { } value && ChildFieldCodes.Contains(value),
            _ => code is null,
        };

    public static bool IsValidDocumentCode(AdmissionRequiredDocumentCode? code) =>
        code is { } value && Enum.IsDefined(value);

    public static AdmissionAttachmentType ToAttachmentType(AdmissionRequiredDocumentCode code) =>
        code switch
        {
            AdmissionRequiredDocumentCode.BirthCertificate => AdmissionAttachmentType.BirthCertificate,
            AdmissionRequiredDocumentCode.PreviousSchoolCertificate => AdmissionAttachmentType.PreviousSchoolReport,
            AdmissionRequiredDocumentCode.ChildPhoto => AdmissionAttachmentType.ChildPhoto,
            AdmissionRequiredDocumentCode.MedicalReport => AdmissionAttachmentType.MedicalReport,
            AdmissionRequiredDocumentCode.SupportingDocument => AdmissionAttachmentType.SupportingDocument,
            _ => AdmissionAttachmentType.Other,
        };

    public static string WizardSection(AdmissionRequirementKind kind) =>
        kind switch
        {
            AdmissionRequirementKind.InformationalText => "info",
            AdmissionRequirementKind.ParentProfileField => "parent",
            AdmissionRequirementKind.ChildProfileField => "child",
            AdmissionRequirementKind.ApplicationDocument => "documents",
            _ => "review",
        };

    public static IReadOnlyList<string> ParseExtensions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return PlatformFileExtensions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        return raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeExtension)
            .Where(ext => PlatformFileExtensions.Contains(ext))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string? SerializeExtensions(IEnumerable<string>? extensions)
    {
        var list = (extensions ?? [])
            .Select(NormalizeExtension)
            .Where(ext => PlatformFileExtensions.Contains(ext))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return list.Length == 0 ? null : string.Join(';', list);
    }

    public static string NormalizeExtension(string extension)
    {
        var value = (extension ?? string.Empty).Trim().ToLowerInvariant();
        if (value.Length == 0)
        {
            return value;
        }

        return value.StartsWith('.') ? value : $".{value}";
    }

    public static bool MatchesScope(
        SchoolAdmissionRequirement requirement,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId) =>
        AdmissionScope.MatchesScope(
            requirement.SchoolBranchId,
            requirement.EducationalStageId,
            requirement.GradeId,
            requirement.AcademicYearId,
            branchId,
            stageId,
            gradeId,
            academicYearId);

    /// <summary>
    /// For each requirement code, keep the single most specific matching published definition.
    /// Equal specificity for the same code is treated as a conflict (should be blocked at publish).
    /// </summary>
    public static IReadOnlyList<SchoolAdmissionRequirement> ResolveApplicable(
        IEnumerable<SchoolAdmissionRequirement> candidates,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId)
    {
        var matching = candidates
            .Where(requirement =>
                requirement.IsActive &&
                requirement.PublicationStatus == AdmissionRequirementPublicationStatus.Published &&
                MatchesScope(requirement, branchId, stageId, gradeId, academicYearId))
            .ToList();

        return matching
            .GroupBy(requirement => requirement.RequirementCode, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group
                    .OrderByDescending(requirement => requirement.SpecificityScore)
                    .ThenBy(requirement => requirement.Id)
                    .ToList();
                return ordered[0];
            })
            .OrderBy(requirement => requirement.SortOrder)
            .ThenBy(requirement => requirement.RequirementCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
