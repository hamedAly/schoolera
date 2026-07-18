using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed class AdmissionRequirementCompletenessService(
    IParentProfileRepository parentProfileRepository,
    IChildProfileRepository childProfileRepository,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IAdmissionApplicationRepository admissionRepository)
    : IAdmissionRequirementCompletenessService
{
    public async Task<AdmissionRequirementsEvaluation> EvaluateAsync(
        AdmissionApplication application,
        string culture,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<MissingAdmissionRequirementItem>();
        var isArabic = culture.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        var snapshots = (application.RequirementSnapshots?.Count ?? 0) > 0
            ? application.RequirementSnapshots!.ToList()
            : (await requirementRepository.ListApplicationSnapshotsAsync(
                application.Id,
                cancellationToken)).ToList();

        var attachments = (application.Attachments?.Count ?? 0) > 0
            ? application.Attachments!.ToList()
            : await LoadAttachmentsAsync(application, cancellationToken);

        ParentProfile? parentProfile = null;
        ChildProfile? childProfile = null;

        var needsParent = snapshots.Any(snapshot =>
            snapshot.Kind == AdmissionRequirementKind.ParentProfileField);
        var needsChild = snapshots.Any(snapshot =>
            snapshot.Kind == AdmissionRequirementKind.ChildProfileField);

        if (needsParent)
        {
            parentProfile = await parentProfileRepository.GetByUserIdAsync(
                application.ParentUserId,
                cancellationToken);
        }

        if (needsChild)
        {
            childProfile = await childProfileRepository.GetOwnedAsync(
                application.ParentUserId,
                application.ChildProfileId,
                cancellationToken);
        }

        foreach (var snapshot in snapshots.OrderBy(item => item.SortOrder))
        {
            if (!snapshot.IsRequired)
            {
                continue;
            }

            switch (snapshot.Kind)
            {
                case AdmissionRequirementKind.InformationalText:
                    break;

                case AdmissionRequirementKind.ParentProfileField:
                    if (!IsParentFieldPresent(parentProfile, snapshot.ProfileFieldCode))
                    {
                        missing.Add(CreateMissing(
                            snapshot,
                            isArabic,
                            AdmissionRequirementReasonCodes.MissingField));
                    }

                    break;

                case AdmissionRequirementKind.ChildProfileField:
                    if (!IsChildFieldPresent(childProfile, snapshot.ProfileFieldCode))
                    {
                        missing.Add(CreateMissing(
                            snapshot,
                            isArabic,
                            AdmissionRequirementReasonCodes.MissingField));
                    }

                    break;

                case AdmissionRequirementKind.ApplicationDocument:
                    var attachment = attachments.FirstOrDefault(item =>
                        item.RequirementSnapshotId == snapshot.Id);
                    if (attachment is null)
                    {
                        missing.Add(CreateMissing(
                            snapshot,
                            isArabic,
                            AdmissionRequirementReasonCodes.MissingDocument));
                        break;
                    }

                    if (!IsAttachmentValidForSnapshot(attachment, snapshot))
                    {
                        missing.Add(CreateMissing(
                            snapshot,
                            isArabic,
                            AdmissionRequirementReasonCodes.InvalidDocument));
                    }

                    break;
            }
        }

        return new AdmissionRequirementsEvaluation(missing.Count == 0, missing);
    }

    private async Task<IReadOnlyList<AdmissionApplicationAttachment>> LoadAttachmentsAsync(
        AdmissionApplication application,
        CancellationToken cancellationToken)
    {
        var loaded = await admissionRepository.GetOwnedAsync(
            application.ParentUserId,
            application.Id,
            cancellationToken);
        return loaded?.Attachments?.ToList() ?? [];
    }

    private static MissingAdmissionRequirementItem CreateMissing(
        AdmissionApplicationRequirementSnapshot snapshot,
        bool isArabic,
        string reasonCode) =>
        new(
            snapshot.Id,
            snapshot.RequirementCode,
            snapshot.Kind,
            isArabic ? snapshot.NameAr : snapshot.NameEn,
            reasonCode,
            AdmissionRequirementCatalog.WizardSection(snapshot.Kind),
            snapshot.DocumentCode);

    private static bool IsParentFieldPresent(ParentProfile? profile, AdmissionProfileFieldCode? code)
    {
        if (profile is null || code is null)
        {
            return false;
        }

        return code switch
        {
            AdmissionProfileFieldCode.ParentOccupation => HasText(profile.Occupation),
            AdmissionProfileFieldCode.ParentQualification => HasText(profile.Qualification),
            AdmissionProfileFieldCode.FatherFullName => HasText(profile.FatherFullName),
            AdmissionProfileFieldCode.FatherPhone => HasText(profile.FatherPhone),
            AdmissionProfileFieldCode.FatherOccupation => HasText(profile.FatherOccupation),
            AdmissionProfileFieldCode.FatherQualification => HasText(profile.FatherQualification),
            AdmissionProfileFieldCode.MotherFullName => HasText(profile.MotherFullName),
            AdmissionProfileFieldCode.MotherPhone => HasText(profile.MotherPhone),
            AdmissionProfileFieldCode.MotherOccupation => HasText(profile.MotherOccupation),
            AdmissionProfileFieldCode.MotherQualification => HasText(profile.MotherQualification),
            _ => false,
        };
    }

    private static bool IsChildFieldPresent(ChildProfile? child, AdmissionProfileFieldCode? code)
    {
        if (child is null || code is null)
        {
            return false;
        }

        return code switch
        {
            AdmissionProfileFieldCode.ChildCurrentSchoolName => HasText(child.CurrentSchoolName),
            AdmissionProfileFieldCode.ChildPreferredStudyLanguage => child.PreferredStudyLanguage is not null,
            AdmissionProfileFieldCode.ChildHasSpecialNeeds => true,
            AdmissionProfileFieldCode.ChildSpecialNeedsNotes =>
                !child.HasSpecialNeeds || HasText(child.SpecialNeedsNotes),
            AdmissionProfileFieldCode.ChildSkills => HasText(child.Skills),
            AdmissionProfileFieldCode.ChildHobbies => HasText(child.Hobbies),
            AdmissionProfileFieldCode.ChildStrengths => HasText(child.Strengths),
            AdmissionProfileFieldCode.ChildImprovementAreas => HasText(child.ImprovementAreas),
            _ => false,
        };
    }

    private static bool IsAttachmentValidForSnapshot(
        AdmissionApplicationAttachment attachment,
        AdmissionApplicationRequirementSnapshot snapshot)
    {
        if (snapshot.DocumentCode is { } expected &&
            attachment.RequiredDocumentCode is { } actual &&
            expected != actual)
        {
            return false;
        }

        var allowed = AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions);
        var extension = Path.GetExtension(attachment.OriginalFileName);
        if (allowed.Count > 0 &&
            !allowed.Contains(AdmissionRequirementCatalog.NormalizeExtension(extension)))
        {
            return false;
        }

        if (snapshot.MaxFileSizeBytes is { } max && attachment.FileSizeBytes > max)
        {
            return false;
        }

        return true;
    }

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);
}

public static class AdmissionRequirementReasonCodes
{
    public const string MissingField = "admission.requirement.missingField";
    public const string MissingDocument = "admission.requirement.missingDocument";
    public const string InvalidDocument = "admission.requirement.invalidDocument";
}
