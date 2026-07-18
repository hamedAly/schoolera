using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

// ParentProfile / ChildProfile / School* types are Domain.Entities.

namespace Schoolera.Application.Common.Interfaces;

public interface IAdmissionApplicationNumberGenerator
{
    /// <summary>Race-safe next application number (APP-{year}-{n:D6}) inside the ambient transaction.</summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}

public interface IAdmissionApplicationRepository
{
    Task<AdmissionApplication?> GetOwnedAsync(
        Guid parentUserId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplication?> GetOwnedForUpdateAsync(
        Guid parentUserId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationAttachment?> GetOwnedAttachmentAsync(
        Guid parentUserId,
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveDuplicateAsync(
        Guid childProfileId,
        Guid schoolId,
        Guid schoolBranchId,
        Guid gradeId,
        Guid academicYearId,
        Guid? excludeApplicationId,
        CancellationToken cancellationToken = default);

    Task<bool> HasRequirementSnapshotsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<bool> HasInterviewOrAssessmentAppointmentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyForChildAsync(
        Guid childProfileId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AdmissionApplication application, CancellationToken cancellationToken = default);

    void AddHistory(AdmissionApplicationHistory entry);

    void AddRequirementSnapshot(AdmissionApplicationRequirementSnapshot snapshot);

    void AddAttachment(AdmissionApplicationAttachment attachment);

    void AddAnswer(AdmissionApplicationAnswer answer);

    void RemoveAnswer(AdmissionApplicationAnswer answer);

    Task<AdmissionApplicationAnswer?> GetOwnedAnswerForUpdateAsync(
        Guid parentUserId,
        Guid applicationId,
        Guid questionSnapshotId,
        CancellationToken cancellationToken = default);

    void RemoveAttachment(AdmissionApplicationAttachment attachment);

    Task<AdmissionApplicationAttachment?> GetOwnedAttachmentForUpdateAsync(
        Guid parentUserId,
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdmissionApplicationListItemDto>> ListForParentAsync(
        Guid parentUserId,
        AdmissionApplicationListQuery query,
        string preferredLanguage,
        CancellationToken cancellationToken = default);

    Task<Dictionary<AdmissionApplicationStatus, int>> CountByStatusForParentAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParentRecentActivityDto>> ListRecentParentVisibleActivityAsync(
        Guid parentUserId,
        int take,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplication?> GetForSchoolAsync(
        Guid schoolId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplication?> GetForSchoolForUpdateAsync(
        Guid schoolId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationAttachment?> GetSchoolAttachmentAsync(
        Guid schoolId,
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SchoolAdmissionApplicationListItemDto>> ListForSchoolFilteredAsync(
        Guid schoolId,
        SchoolAdmissionApplicationListQuery query,
        string preferredLanguage,
        CancellationToken cancellationToken = default);

    Task<Dictionary<AdmissionApplicationStatus, int>> CountByStatusForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolAdmissionRecentItemDto>> ListRecentSubmittedForSchoolAsync(
        Guid schoolId,
        int take,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplication?> GetForAdminAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationAttachment?> GetAdminAttachmentAsync(
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminAdmissionApplicationListItemDto>> ListForAdminAsync(
        AdminAdmissionApplicationListQuery query,
        string preferredLanguage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAdmissionExportRowDto>> ListForAdminExportAsync(
        AdminAdmissionApplicationListQuery query,
        string preferredLanguage,
        int maxRows,
        bool includeAnswers = false,
        IReadOnlyList<string>? answerColumns = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ResolveExportAnswerColumnsAsync(
        AdminAdmissionApplicationListQuery query,
        int maxColumns,
        CancellationToken cancellationToken = default);

    Task<AdminAdmissionDashboardMetricsDto> GetAdminDashboardMetricsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Legacy simple school list retained for callers that only filter by status.</summary>
    Task<PagedResult<AdmissionApplication>> ListForSchoolAsync(
        Guid schoolId,
        AdmissionApplicationStatus? status,
        PagedRequest paging,
        CancellationToken cancellationToken = default);
}

/// <summary>Validates school/branch/offering/year/gender eligibility for draft create/update/submit.</summary>
public interface IAdmissionEligibilityService
{
    Task<Result<AdmissionEligibilityContext>> ValidateAsync(
        AdmissionEligibilityRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AdmissionEligibilityRequest(
    Guid ParentUserId,
    Guid ChildProfileId,
    Guid? SchoolId,
    string? SchoolSlug,
    Guid SchoolBranchId,
    Guid EducationalStageId,
    Guid GradeId,
    Guid AcademicYearId,
    bool RequireActiveChild = true);

public sealed record AdmissionEligibilityContext(
    ParentProfile ParentProfile,
    ChildProfile Child,
    School School,
    SchoolBranch Branch,
    EducationalStage Stage,
    Grade Grade,
    AcademicYear AcademicYear,
    SchoolStageOffering StageOffering,
    SchoolGradeOffering GradeOffering);
