using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Dtos;

public sealed record ParentDashboardDto(
    int ChildCount,
    int ActiveChildCount,
    bool ApplicationsAvailable,
    int? TotalApplications,
    int? DraftApplications,
    int? SubmittedApplications,
    int? UnderReviewApplications,
    int? AcceptedApplications,
    int? RejectedApplications,
    int? CancelledApplications,
    bool RecentActivityAvailable,
    IReadOnlyList<ParentRecentActivityDto>? RecentActivities,
    bool ProfileIncomplete);

public sealed record ParentRecentActivityDto(
    string ActivityType,
    DateTimeOffset OccurredAtUtc,
    string Summary);

public sealed record ParentGuardianDto(
    string? FullName,
    string? Phone,
    string? Email,
    string? Occupation,
    string? Qualification,
    ChildIdentityType? IdentityType,
    string? MaskedIdentity);

public sealed record ParentProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? AlternatePhone,
    string? AddressLine,
    string? Qualification,
    string? Occupation,
    Guid? CountryId,
    string? CountryName,
    Guid? GovernorateId,
    string? GovernorateName,
    Guid? CityId,
    string? CityName,
    Guid? DistrictId,
    string? DistrictName,
    PreferredContactMethod PreferredContactMethod,
    string PreferredLanguage,
    ParentGuardianDto Father,
    ParentGuardianDto Mother,
    bool IsComplete);

public sealed record UpdateParentGuardianRequest(
    string? FullName,
    string? Phone,
    string? Email,
    string? Occupation,
    string? Qualification,
    ChildIdentityType? IdentityType,
    string? IdentityValue);

public sealed record UpdateParentProfileRequest(
    string FirstName,
    string LastName,
    string Phone,
    string? AlternatePhone,
    string? AddressLine,
    string? Qualification,
    string? Occupation,
    Guid? CountryId,
    Guid? GovernorateId,
    Guid? CityId,
    Guid? DistrictId,
    PreferredContactMethod PreferredContactMethod,
    string PreferredLanguage,
    UpdateParentGuardianRequest? Father,
    UpdateParentGuardianRequest? Mother);

public sealed record ChildProfileListItemDto(
    Guid Id,
    string FullName,
    string MaskedIdentity,
    ChildIdentityType IdentityType,
    DateOnly BirthDate,
    int AgeYears,
    ChildGender Gender,
    Guid CurrentGradeId,
    string CurrentGradeName,
    string? CurrentSchoolName,
    ChildStudyLanguage? PreferredStudyLanguage,
    bool HasSpecialNeeds,
    bool IsActive);

public sealed record ChildProfileDetailDto(
    Guid Id,
    string FullName,
    string MaskedIdentity,
    ChildIdentityType IdentityType,
    DateOnly BirthDate,
    int AgeYears,
    ChildGender Gender,
    Guid CurrentGradeId,
    string CurrentGradeName,
    string? CurrentSchoolName,
    ChildStudyLanguage? PreferredStudyLanguage,
    string? Skills,
    string? Hobbies,
    string? Strengths,
    string? ImprovementAreas,
    bool HasSpecialNeeds,
    string? SpecialNeedsNotes,
    string? HealthNotes,
    bool IsActive);

public sealed record CreateChildProfileRequest(
    string FullName,
    ChildIdentityType IdentityType,
    string IdentityValue,
    DateOnly BirthDate,
    ChildGender Gender,
    Guid CurrentGradeId,
    bool HasSpecialNeeds,
    string? SpecialNeedsNotes,
    string? CurrentSchoolName,
    ChildStudyLanguage? PreferredStudyLanguage,
    string? Skills,
    string? Hobbies,
    string? Strengths,
    string? ImprovementAreas,
    string? HealthNotes);

public sealed record UpdateChildProfileRequest(
    string FullName,
    DateOnly BirthDate,
    ChildGender Gender,
    Guid CurrentGradeId,
    bool HasSpecialNeeds,
    string? SpecialNeedsNotes,
    string? CurrentSchoolName,
    ChildStudyLanguage? PreferredStudyLanguage,
    string? Skills,
    string? Hobbies,
    string? Strengths,
    string? ImprovementAreas,
    string? HealthNotes,
    ChildIdentityType? IdentityType,
    string? IdentityValue);

public sealed record ChildDocumentDto(
    Guid Id,
    ChildDocumentType DocumentType,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CopyChildDocumentToAdmissionRequest(
    Guid ChildDocumentId,
    Guid? RequirementSnapshotId = null);
