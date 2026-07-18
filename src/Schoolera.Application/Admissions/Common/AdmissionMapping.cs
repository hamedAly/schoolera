using System.Globalization;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

internal static class AdmissionMapping
{
    public static AdmissionApplicationDetailDto ToDetail(
        AdmissionApplication application,
        IChildIdentityProtector? identityProtector) =>
        ToDetail(application, identityProtector, requirementEvaluation: null, questionEvaluation: null, interviewFaqs: null);

    public static AdmissionApplicationDetailDto ToDetail(
        AdmissionApplication application,
        IChildIdentityProtector? identityProtector,
        AdmissionRequirementsEvaluation? requirementEvaluation) =>
        ToDetail(application, identityProtector, requirementEvaluation, questionEvaluation: null, interviewFaqs: null);

    public static AdmissionApplicationDetailDto ToDetail(
        AdmissionApplication application,
        IChildIdentityProtector? identityProtector,
        AdmissionRequirementsEvaluation? requirementEvaluation,
        AdmissionQuestionsEvaluation? questionEvaluation) =>
        ToDetail(application, identityProtector, requirementEvaluation, questionEvaluation, interviewFaqs: null);

    public static AdmissionApplicationDetailDto ToDetail(
        AdmissionApplication application,
        IChildIdentityProtector? identityProtector,
        AdmissionRequirementsEvaluation? requirementEvaluation,
        AdmissionQuestionsEvaluation? questionEvaluation,
        IReadOnlyList<PublicInterviewFaqItemDto>? interviewFaqs) =>
        ToDetail(
            application,
            identityProtector,
            requirementEvaluation,
            questionEvaluation,
            interviewFaqs,
            ageEligibility: AgeEligibilityMapping.FromSnapshot(application.AgeEligibilitySnapshot));

    public static AdmissionApplicationDetailDto ToDetail(
        AdmissionApplication application,
        IChildIdentityProtector? identityProtector,
        AdmissionRequirementsEvaluation? requirementEvaluation,
        AdmissionQuestionsEvaluation? questionEvaluation,
        IReadOnlyList<PublicInterviewFaqItemDto>? interviewFaqs,
        AgeEligibilityResultDto? ageEligibility)
    {
        var child = application.ChildProfile;
        var school = application.School;
        var branch = application.SchoolBranch;
        var stage = application.EducationalStage;
        var grade = application.Grade;
        var year = application.AcademicYear;

        var childName = child?.FullName
            ?? application.SubmittedChildFullName
            ?? string.Empty;

        var maskedIdentity = identityProtector is not null && child is not null
            ? identityProtector.Mask(child.IdentityLastFour)
            : null;

        var history = application.History ?? Array.Empty<AdmissionApplicationHistory>();
        var attachmentRows = application.Attachments ?? Array.Empty<AdmissionApplicationAttachment>();

        var parentVisibleRejection = history
            .Where(entry =>
                entry.ParentVisible &&
                entry.ToStatus == AdmissionApplicationStatus.Rejected &&
                !string.IsNullOrWhiteSpace(entry.ParentVisibleNote))
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Select(entry => entry.ParentVisibleNote)
            .FirstOrDefault();

        var timeline = history
            .Where(entry => entry.ParentVisible)
            .OrderBy(entry => entry.CreatedAtUtc)
            .Select(entry => new AdmissionHistoryDto(
                entry.Id,
                entry.FromStatus,
                entry.ToStatus,
                entry.Action,
                entry.ParentVisibleNote,
                entry.CreatedAtUtc))
            .ToArray();

        var attachments = attachmentRows
            .OrderByDescending(attachment => attachment.CreatedAtUtc)
            .Select(attachment => new AdmissionAttachmentDto(
                attachment.Id,
                attachment.AttachmentType,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.FileSizeBytes,
                attachment.CreatedAtUtc,
                attachment.RequirementSnapshotId,
                attachment.QuestionSnapshotId,
                attachment.RequiredDocumentCode))
            .ToArray();

        return new AdmissionApplicationDetailDto(
            application.Id,
            application.ApplicationNumber,
            application.Status,
            application.ChildProfileId,
            childName,
            maskedIdentity,
            application.SchoolId,
            school?.Slug ?? string.Empty,
            PickLocalized(
                school?.NameAr,
                school?.NameEn,
                application.SubmittedSchoolNameAr,
                application.SubmittedSchoolNameEn),
            application.SchoolBranchId,
            PickLocalized(
                branch?.NameAr,
                branch?.NameEn,
                application.SubmittedBranchNameAr,
                application.SubmittedBranchNameEn),
            application.EducationalStageId,
            PickLocalized(
                stage?.NameAr,
                stage?.NameEn,
                application.SubmittedStageNameAr,
                application.SubmittedStageNameEn),
            application.GradeId,
            PickLocalized(
                grade?.NameAr,
                grade?.NameEn,
                application.SubmittedGradeNameAr,
                application.SubmittedGradeNameEn),
            application.AcademicYearId,
            PickLocalized(
                year?.NameAr,
                year?.NameEn,
                application.SubmittedAcademicYearNameAr,
                application.SubmittedAcademicYearNameEn),
            application.ParentNotes,
            application.CreatedAtUtc,
            application.UpdatedAtUtc,
            application.SubmittedAtUtc,
            application.ReviewStartedAtUtc,
            application.AcceptedAtUtc,
            application.RejectedAtUtc,
            application.CancelledAtUtc,
            application.CancellationReason,
            parentVisibleRejection,
            attachments,
            timeline,
            ToRequirementChecklist(application, requirementEvaluation),
            ToQuestionChecklist(application, questionEvaluation),
            AdmissionLifecycleMapping.ToActiveMissingRequest(application),
            AdmissionLifecycleMapping.ToActiveInterview(application),
            AdmissionLifecycleMapping.ToActiveAssessment(application),
            AdmissionLifecycleMapping.ToWaitingList(application),
            application.RegisteredAtUtc,
            application.PolicySnapshot is null
                ? null
                : SchoolInterviewAssessmentPolicyMapping.ToSafeSummary(
                    application.PolicySnapshot,
                    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                        .Equals("ar", StringComparison.OrdinalIgnoreCase),
                    isOnlineCapabilityAvailable:
                        SchoolInterviewAssessmentPolicyMapping.NeedsOnlineCapability(
                            application.PolicySnapshot.DeliveryMode) &&
                        !string.IsNullOrWhiteSpace(application.PolicySnapshot.MeetingProviderCode)),
            interviewFaqs ?? Array.Empty<PublicInterviewFaqItemDto>(),
            ageEligibility,
            AdmissionCapabilityFactory.From(application.Status, application.ReviewStartedAtUtc),
            application.RowVersion);
    }

    public static IReadOnlyList<AdmissionRequirementChecklistItemDto> ToRequirementChecklist(
        AdmissionApplication application,
        AdmissionRequirementsEvaluation? evaluation)
    {
        var missingById = (evaluation?.Missing ?? [])
            .ToDictionary(item => item.RequirementSnapshotId);
        var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        var snapshots = application.RequirementSnapshots
            ?? Array.Empty<AdmissionApplicationRequirementSnapshot>();
        var attachments = application.Attachments
            ?? Array.Empty<AdmissionApplicationAttachment>();

        return snapshots
            .OrderBy(snapshot => snapshot.SortOrder)
            .ThenBy(snapshot => snapshot.RequirementCode)
            .Select(snapshot =>
            {
                missingById.TryGetValue(snapshot.Id, out var missing);
                var linked = attachments
                    .FirstOrDefault(attachment => attachment.RequirementSnapshotId == snapshot.Id);
                return new AdmissionRequirementChecklistItemDto(
                    snapshot.Id,
                    snapshot.RequirementCode,
                    snapshot.Kind,
                    isArabic ? snapshot.NameAr : snapshot.NameEn,
                    isArabic ? snapshot.DescriptionAr : snapshot.DescriptionEn,
                    snapshot.IsRequired,
                    snapshot.SortOrder,
                    missing is null,
                    missing?.ReasonCode,
                    AdmissionRequirementCatalog.WizardSection(snapshot.Kind),
                    snapshot.ProfileFieldCode,
                    snapshot.DocumentCode,
                    AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions),
                    snapshot.MaxFileSizeBytes,
                    snapshot.AllowChildVaultCopy,
                    linked?.Id);
            })
            .ToArray();
    }

    public static IReadOnlyList<AdmissionQuestionChecklistItemDto> ToQuestionChecklist(
        AdmissionApplication application,
        AdmissionQuestionsEvaluation? evaluation)
    {
        var missingById = (evaluation?.Missing ?? [])
            .ToDictionary(item => item.QuestionSnapshotId);
        var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        var snapshots = application.QuestionSnapshots
            ?? Array.Empty<AdmissionApplicationQuestionSnapshot>();
        var answers = application.Answers
            ?? Array.Empty<AdmissionApplicationAnswer>();
        var attachments = application.Attachments
            ?? Array.Empty<AdmissionApplicationAttachment>();

        return snapshots
            .OrderBy(snapshot => snapshot.SortOrder)
            .ThenBy(snapshot => snapshot.QuestionCode)
            .Select(snapshot =>
            {
                missingById.TryGetValue(snapshot.Id, out var missing);
                var answer = answers.FirstOrDefault(item => item.QuestionSnapshotId == snapshot.Id);
                var linkedAttachmentId = answer?.AttachmentId
                    ?? attachments.FirstOrDefault(item => item.QuestionSnapshotId == snapshot.Id)?.Id;
                return new AdmissionQuestionChecklistItemDto(
                    snapshot.Id,
                    snapshot.QuestionCode,
                    snapshot.QuestionType,
                    isArabic ? snapshot.LabelAr : snapshot.LabelEn,
                    isArabic ? snapshot.HelpAr : snapshot.HelpEn,
                    snapshot.IsRequired,
                    snapshot.SortOrder,
                    missing is null,
                    missing?.ReasonCode,
                    AdmissionQuestionCatalog.WizardSection,
                    snapshot.MinLength,
                    snapshot.MaxLength,
                    snapshot.MinSelectedOptions,
                    snapshot.MaxSelectedOptions,
                    snapshot.MinDate,
                    snapshot.MaxDate,
                    AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions),
                    snapshot.MaxFileSizeBytes,
                    snapshot.AllowChildVaultCopy,
                    snapshot.Options
                        .OrderBy(option => option.SortOrder)
                        .Select(option => new AdmissionQuestionOptionSnapshotDto(
                            option.OptionCode,
                            isArabic ? option.LabelAr : option.LabelEn,
                            option.SortOrder))
                        .ToArray(),
                    answer?.TextValue,
                    answer?.GetSelectedOptionCodes() ?? [],
                    answer?.DateValue,
                    answer?.BooleanValue,
                    linkedAttachmentId);
            })
            .ToArray();
    }

    public static IReadOnlyList<MissingAdmissionQuestionDto> ToMissingQuestionDtos(
        IEnumerable<MissingAdmissionQuestionItem> items) =>
        items.Select(item => new MissingAdmissionQuestionDto(
            item.QuestionSnapshotId,
            item.QuestionCode,
            item.QuestionType,
            item.DisplayName,
            item.ReasonCode,
            item.WizardSection)).ToArray();

    public static IReadOnlyList<AdmissionRequirementChecklistItemDto> ToChecklist(
        AdmissionApplication application,
        AdmissionRequirementsEvaluation? evaluation) =>
        ToRequirementChecklist(application, evaluation);

    public static IReadOnlyList<MissingAdmissionRequirementDto> ToMissingDtos(
        IEnumerable<MissingAdmissionRequirementItem> items) =>
        items.Select(item => new MissingAdmissionRequirementDto(
            item.RequirementSnapshotId,
            item.RequirementCode,
            item.Kind,
            item.DisplayName,
            item.ReasonCode,
            item.WizardSection,
            item.DocumentCode)).ToArray();

    public static AdmissionSubmissionSnapshot ToSnapshot(
        AdmissionEligibilityContext context,
        ParentAccountSnapshot account,
        IChildIdentityProtector identityProtector)
    {
        var profile = context.ParentProfile;
        return new(
            context.Child.FullName,
            context.Child.CurrentSchoolName,
            context.Child.PreferredStudyLanguage is { } language ? (int)language : null,
            context.Child.Skills,
            context.Child.Hobbies,
            context.Child.Strengths,
            context.Child.ImprovementAreas,
            context.Child.HasSpecialNeeds,
            context.Child.SpecialNeedsNotes,
            context.School.NameAr,
            context.School.NameEn,
            context.Branch.NameAr,
            context.Branch.NameEn,
            context.Stage.NameAr,
            context.Stage.NameEn,
            context.Grade.NameAr,
            context.Grade.NameEn,
            context.AcademicYear.NameAr,
            context.AcademicYear.NameEn,
            $"{account.FirstName} {account.LastName}".Trim(),
            account.Email,
            account.Phone,
            profile.AlternatePhone,
            profile.FatherFullName,
            profile.FatherPhone,
            profile.FatherEmail,
            profile.FatherOccupation,
            profile.FatherQualification,
            string.IsNullOrWhiteSpace(profile.FatherIdentityLastFour)
                ? null
                : identityProtector.Mask(profile.FatherIdentityLastFour),
            profile.MotherFullName,
            profile.MotherPhone,
            profile.MotherEmail,
            profile.MotherOccupation,
            profile.MotherQualification,
            string.IsNullOrWhiteSpace(profile.MotherIdentityLastFour)
                ? null
                : identityProtector.Mask(profile.MotherIdentityLastFour));
    }

    public static string SafeOriginalFileName(string? originalFileName)
    {
        var name = Path.GetFileName(originalFileName ?? string.Empty);
        return string.IsNullOrWhiteSpace(name) ? "document" : name;
    }

    private static string PickLocalized(
        string? nameAr,
        string? nameEn,
        string? snapshotAr,
        string? snapshotEn)
    {
        var live = LocalizationDisplayHelper.Pick(nameAr, nameEn);
        if (!string.IsNullOrWhiteSpace(live))
        {
            return live;
        }

        return LocalizationDisplayHelper.Pick(snapshotAr, snapshotEn);
    }
}
