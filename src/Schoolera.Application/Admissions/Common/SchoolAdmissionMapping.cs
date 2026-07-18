using System.Globalization;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

internal sealed record SchoolParentContactInfo(
    string DisplayName,
    string? Email,
    string? Phone);

internal static class SchoolAdmissionMapping
{
    public static SchoolParentContactInfo ToParentContact(UserSummary? user) =>
        new(user?.DisplayName ?? string.Empty, user?.Email, user?.PhoneNumber);

    public static SchoolAdmissionApplicationDetailDto ToSchoolDetail(
        AdmissionApplication application,
        SchoolParentContactInfo parent,
        IChildIdentityProtector identityProtector,
        AgeEligibilityResultDto? ageEligibility = null,
        bool canGrantAgeException = false)
    {
        var child = application.ChildProfile;
        var preferEnglish = AdmissionResults.PreferredLanguageCode() == "en";
        var useSnapshot = application.HasSubmissionSnapshot;
        var profile = application.ParentProfile;
        var resolvedAge = ageEligibility ?? AgeEligibilityMapping.FromSnapshot(application.AgeEligibilitySnapshot);

        return new SchoolAdmissionApplicationDetailDto(
            application.Id,
            application.ApplicationNumber,
            application.Status,
            application.CreatedAtUtc,
            application.SubmittedAtUtc,
            application.ReviewStartedAtUtc,
            application.AcceptedAtUtc,
            application.RejectedAtUtc,
            useSnapshot
                ? application.SubmittedChildFullName ?? child?.FullName ?? string.Empty
                : child?.FullName ?? application.SubmittedChildFullName ?? string.Empty,
            useSnapshot
                ? (child is null ? null : identityProtector.Mask(child.IdentityLastFour))
                : (child is null ? null : identityProtector.Mask(child.IdentityLastFour)),
            child?.BirthDate,
            child?.Gender,
            child?.CurrentGrade is null
                ? null
                : Prefer(preferEnglish, child.CurrentGrade.NameAr, child.CurrentGrade.NameEn),
            useSnapshot
                ? application.SubmittedChildCurrentSchoolName
                : child?.CurrentSchoolName,
            useSnapshot
                ? (application.SubmittedChildPreferredStudyLanguage is { } studyLang
                    ? (ChildStudyLanguage)studyLang
                    : null)
                : child?.PreferredStudyLanguage,
            useSnapshot ? application.SubmittedChildSkills : child?.Skills,
            useSnapshot ? application.SubmittedChildHobbies : child?.Hobbies,
            useSnapshot ? application.SubmittedChildStrengths : child?.Strengths,
            useSnapshot ? application.SubmittedChildImprovementAreas : child?.ImprovementAreas,
            useSnapshot
                ? application.SubmittedChildHasSpecialNeeds ?? child?.HasSpecialNeeds ?? false
                : child?.HasSpecialNeeds ?? false,
            useSnapshot
                ? application.SubmittedChildSpecialNeedsNotes
                : child?.SpecialNeedsNotes,
            useSnapshot
                ? application.SubmittedParentDisplayName ?? parent.DisplayName
                : parent.DisplayName,
            useSnapshot ? application.SubmittedParentEmail ?? parent.Email : parent.Email,
            useSnapshot ? application.SubmittedParentPhone ?? parent.Phone : parent.Phone,
            useSnapshot
                ? application.SubmittedParentAlternatePhone ?? profile?.AlternatePhone
                : profile?.AlternatePhone,
            useSnapshot
                ? application.SubmittedFatherFullName
                : profile?.FatherFullName,
            useSnapshot
                ? application.SubmittedFatherPhone
                : profile?.FatherPhone,
            useSnapshot
                ? application.SubmittedFatherEmail
                : profile?.FatherEmail,
            useSnapshot
                ? application.SubmittedFatherOccupation
                : profile?.FatherOccupation,
            useSnapshot
                ? application.SubmittedFatherQualification
                : profile?.FatherQualification,
            useSnapshot
                ? application.SubmittedFatherMaskedIdentity
                : (string.IsNullOrWhiteSpace(profile?.FatherIdentityLastFour)
                    ? null
                    : identityProtector.Mask(profile.FatherIdentityLastFour)),
            useSnapshot
                ? application.SubmittedMotherFullName
                : profile?.MotherFullName,
            useSnapshot
                ? application.SubmittedMotherPhone
                : profile?.MotherPhone,
            useSnapshot
                ? application.SubmittedMotherEmail
                : profile?.MotherEmail,
            useSnapshot
                ? application.SubmittedMotherOccupation
                : profile?.MotherOccupation,
            useSnapshot
                ? application.SubmittedMotherQualification
                : profile?.MotherQualification,
            useSnapshot
                ? application.SubmittedMotherMaskedIdentity
                : (string.IsNullOrWhiteSpace(profile?.MotherIdentityLastFour)
                    ? null
                    : identityProtector.Mask(profile.MotherIdentityLastFour)),
            application.SchoolId,
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedSchoolNameAr,
                application.SubmittedSchoolNameEn,
                application.School?.NameAr,
                application.School?.NameEn),
            application.SchoolBranchId,
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedBranchNameAr,
                application.SubmittedBranchNameEn,
                application.SchoolBranch?.NameAr,
                application.SchoolBranch?.NameEn),
            application.EducationalStageId,
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedStageNameAr,
                application.SubmittedStageNameEn,
                application.EducationalStage?.NameAr,
                application.EducationalStage?.NameEn),
            application.GradeId,
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedGradeNameAr,
                application.SubmittedGradeNameEn,
                application.Grade?.NameAr,
                application.Grade?.NameEn),
            application.AcademicYearId,
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedAcademicYearNameAr,
                application.SubmittedAcademicYearNameEn,
                application.AcademicYear?.NameAr,
                application.AcademicYear?.NameEn),
            application.ParentNotes,
            application.SchoolNotes,
            application.RejectionReason,
            application.Attachments
                .OrderByDescending(item => item.CreatedAtUtc)
                .Select(item => new AdmissionAttachmentDto(
                    item.Id,
                    item.AttachmentType,
                    item.OriginalFileName,
                    item.ContentType,
                    item.FileSizeBytes,
                    item.CreatedAtUtc,
                    item.RequirementSnapshotId,
                    item.QuestionSnapshotId,
                    item.RequiredDocumentCode))
                .ToArray(),
            application.History
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => new SchoolAdmissionHistoryDto(
                    item.Id,
                    item.FromStatus,
                    item.ToStatus,
                    item.Action,
                    item.InternalNote,
                    item.ParentVisibleNote,
                    item.ActorUserId,
                    item.ActorRole,
                    item.CreatedAtUtc))
                .ToArray(),
            AdmissionMapping.ToRequirementChecklist(application, evaluation: null),
            AdmissionMapping.ToQuestionChecklist(application, evaluation: null),
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
            resolvedAge,
            SchoolAdmissionCapabilityFactory.From(application.Status, canGrantAgeException),
            application.RowVersion);
    }

    public static AdminAdmissionApplicationDetailDto ToAdminDetail(
        AdmissionApplication application,
        SchoolParentContactInfo parent,
        IChildIdentityProtector identityProtector)
    {
        var child = application.ChildProfile;
        var preferEnglish = AdmissionResults.PreferredLanguageCode() == "en";
        var useSnapshot = application.HasSubmissionSnapshot;
        var parentVisibleRejection = application.History
            .Where(entry =>
                entry.ParentVisible &&
                entry.ToStatus == AdmissionApplicationStatus.Rejected &&
                !string.IsNullOrWhiteSpace(entry.ParentVisibleNote))
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Select(entry => entry.ParentVisibleNote)
            .FirstOrDefault();

        return new AdminAdmissionApplicationDetailDto(
            application.Id,
            application.ApplicationNumber,
            application.Status,
            application.SchoolId,
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedSchoolNameAr,
                application.SubmittedSchoolNameEn,
                application.School?.NameAr,
                application.School?.NameEn),
            Prefer(preferEnglish, application.SchoolBranch?.City?.NameAr, application.SchoolBranch?.City?.NameEn),
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedBranchNameAr,
                application.SubmittedBranchNameEn,
                application.SchoolBranch?.NameAr,
                application.SchoolBranch?.NameEn),
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedStageNameAr,
                application.SubmittedStageNameEn,
                application.EducationalStage?.NameAr,
                application.EducationalStage?.NameEn),
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedGradeNameAr,
                application.SubmittedGradeNameEn,
                application.Grade?.NameAr,
                application.Grade?.NameEn),
            PreferSnapshot(
                useSnapshot,
                preferEnglish,
                application.SubmittedAcademicYearNameAr,
                application.SubmittedAcademicYearNameEn,
                application.AcademicYear?.NameAr,
                application.AcademicYear?.NameEn),
            useSnapshot
                ? application.SubmittedChildFullName ?? child?.FullName ?? string.Empty
                : child?.FullName ?? application.SubmittedChildFullName ?? string.Empty,
            child is null ? null : identityProtector.Mask(child.IdentityLastFour),
            child?.BirthDate,
            child?.Gender,
            useSnapshot
                ? application.SubmittedParentDisplayName ?? parent.DisplayName
                : parent.DisplayName,
            useSnapshot ? application.SubmittedParentEmail ?? parent.Email : parent.Email,
            useSnapshot ? application.SubmittedParentPhone ?? parent.Phone : parent.Phone,
            application.ParentNotes,
            application.SchoolNotes,
            application.RejectionReason,
            parentVisibleRejection,
            application.CreatedAtUtc,
            application.SubmittedAtUtc,
            application.ReviewStartedAtUtc,
            application.AcceptedAtUtc,
            application.RejectedAtUtc,
            application.CancelledAtUtc,
            application.Attachments
                .OrderByDescending(item => item.CreatedAtUtc)
                .Select(item => new AdmissionAttachmentDto(
                    item.Id,
                    item.AttachmentType,
                    item.OriginalFileName,
                    item.ContentType,
                    item.FileSizeBytes,
                    item.CreatedAtUtc,
                    item.RequirementSnapshotId,
                    item.QuestionSnapshotId,
                    item.RequiredDocumentCode))
                .ToArray(),
            application.History
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => new SchoolAdmissionHistoryDto(
                    item.Id,
                    item.FromStatus,
                    item.ToStatus,
                    item.Action,
                    item.InternalNote,
                    item.ParentVisibleNote,
                    item.ActorUserId,
                    item.ActorRole,
                    item.CreatedAtUtc))
                .ToArray());
    }

    private static string Prefer(bool preferEnglish, string? ar, string? en) =>
        preferEnglish ? en ?? ar ?? string.Empty : ar ?? string.Empty;

    private static string PreferSnapshot(
        bool useSnapshot,
        bool preferEnglish,
        string? snapshotAr,
        string? snapshotEn,
        string? liveAr,
        string? liveEn)
    {
        if (useSnapshot)
        {
            var fromSnapshot = Prefer(preferEnglish, snapshotAr, snapshotEn);
            if (!string.IsNullOrWhiteSpace(fromSnapshot))
            {
                return fromSnapshot;
            }
        }

        return Prefer(preferEnglish, liveAr, liveEn);
    }
}
