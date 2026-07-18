namespace Schoolera.Application.Admin.Dtos;

public sealed record AdminDashboardDto(
    int TotalSchools,
    int PublishedSchools,
    int UnpublishedSchools,
    int SuspendedSchools,
    int DraftSchools,
    int PendingOnboardingApplications,
    int UnderReviewOnboardingApplications,
    int ChangesRequestedApplications,
    int ActiveParents,
    int ActiveSchoolOwners,
    int ActiveSchoolAdmins,
    int TotalUsers,
    bool AdmissionsAvailable,
    int? AdmissionApplicationsCount,
    int? AdmissionDraftCount,
    int? AdmissionSubmittedCount,
    int? AdmissionUnderReviewCount,
    int? AdmissionMissingDocumentsCount,
    int? AdmissionInterviewRequiredCount,
    int? AdmissionAssessmentRequiredCount,
    int? AdmissionWaitingListCount,
    int? AdmissionAcceptedCount,
    int? AdmissionRejectedCount,
    int? AdmissionCancelledCount,
    int? AdmissionRegisteredCount,
    int? AdmissionApplicationsTodayCount,
    int? AdmissionPendingSchoolReviewCount,
    IReadOnlyList<AdminAuditEventDto> RecentAuditEvents);

public sealed record AdminSchoolListItemDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string Slug,
    string Status,
    string SchoolType,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AdminSchoolDetailDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string Slug,
    string Status,
    string SchoolType,
    Guid? OwnerUserId,
    string? OwnerEmail,
    string? ShortDescriptionAr,
    string? ShortDescriptionEn,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string DisplayName,
    string AccountStatus,
    IReadOnlyList<string> Roles,
    string PreferredLanguage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);

public sealed record AdminAuditEventDto(
    Guid Id,
    Guid ActorUserId,
    string? ActorEmail,
    string Action,
    string EntityType,
    string? EntityId,
    string? Summary,
    DateTimeOffset CreatedAtUtc);

public sealed record UpdateAdminSchoolStatusRequest(string Status);

public sealed record UpdateAdminUserStatusRequest(string AccountStatus);
