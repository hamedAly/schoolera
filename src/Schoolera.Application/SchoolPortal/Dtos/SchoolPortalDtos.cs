using Schoolera.Application.Admissions.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Dtos;

public sealed record AccessibleSchoolDto(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    SchoolStatus Status,
    string? LogoUrl,
    bool IsOwner,
    SchoolPortalPermissionsDto? Permissions = null);

public sealed record SchoolPortalPermissionsDto(
    bool CanViewDashboard,
    bool CanViewTeam,
    bool CanManageTeam,
    bool CanTransferOwnership,
    bool CanViewProfile,
    bool CanManageProfile,
    bool CanManageBranches,
    bool CanManageOfferings,
    bool CanManageFacilities,
    bool CanManageGallery,
    bool CanManageServices,
    bool CanManagePublicContact,
    bool CanViewApplications,
    bool CanManageApplicationReview,
    bool CanDownloadApplicationAttachments,
    bool CanExportApplications,
    bool CanManageAdmissionRequirements,
    bool CanManageAdmissionQuestions,
    bool CanViewFees,
    bool CanManageFees,
    bool CanManageContent,
    SchoolBranchScopeMode BranchScopeMode,
    IReadOnlyList<Guid> AllowedBranchIds);

public sealed record SchoolDashboardDto(
    Guid SchoolId,
    string NameAr,
    string? NameEn,
    string Slug,
    SchoolStatus Status,
    bool IsEditable,
    string? LogoUrl,
    string? CoverUrl,
    int BranchCount,
    int ActiveBranchCount,
    int OfferingCount,
    int ActiveOfferingCount,
    int ActiveGradeCount,
    int TuitionFeeCount,
    int ActiveTuitionFeeCount,
    int FacilityCount,
    int ServiceCount,
    int ActiveServiceCount,
    int TeamMemberCount,
    int GalleryImageCount,
    int ProfileCompletionPercent,
    IReadOnlyList<string> Warnings,
    DateTimeOffset UpdatedAtUtc,
    bool AdmissionsAvailable,
    int SubmittedApplications,
    int UnderReviewApplications,
    int AcceptedApplications,
    int RejectedApplications,
    int TotalActiveApplications,
    IReadOnlyList<SchoolAdmissionRecentItemDto> RecentSubmittedApplications);

public sealed record SchoolPortalProfileDto(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    string? ShortDescriptionAr,
    string? ShortDescriptionEn,
    string? FullDescriptionAr,
    string? FullDescriptionEn,
    SchoolType SchoolType,
    GenderType GenderType,
    int? FoundedYear,
    int? StudentCount,
    SchoolStatus Status,
    string? PublicPhone,
    string? PublicEmail,
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? SeoTitleAr,
    string? SeoTitleEn,
    string? SeoDescriptionAr,
    string? SeoDescriptionEn,
    string? LogoUrl,
    string? CoverUrl,
    DateTimeOffset UpdatedAtUtc,
    FeeVisibilityPolicy? FeeVisibilityPolicy = null);

public sealed record SchoolFeeVisibilityDto(FeeVisibilityPolicy? FeeVisibilityPolicy);

public sealed record UpdateSchoolPortalProfileRequest(
    string NameAr,
    string? NameEn,
    string? ShortDescriptionAr,
    string? ShortDescriptionEn,
    string? FullDescriptionAr,
    string? FullDescriptionEn,
    SchoolType SchoolType,
    GenderType GenderType,
    int? FoundedYear,
    int? StudentCount,
    string? PublicPhone,
    string? PublicEmail,
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? SeoTitleAr,
    string? SeoTitleEn,
    string? SeoDescriptionAr,
    string? SeoDescriptionEn);

public sealed record SchoolBranchDto(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    Guid CityId,
    string CityNameAr,
    string? CityNameEn,
    Guid DistrictId,
    string DistrictNameAr,
    string? DistrictNameEn,
    string? AddressLineAr,
    string? AddressLineEn,
    string? BuildingNumber,
    string? StreetName,
    string? Landmark,
    string? PostalCode,
    string? AddressReference,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? Email,
    bool IsMainBranch,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateSchoolBranchRequest(
    string NameAr,
    string? NameEn,
    Guid CityId,
    Guid DistrictId,
    string? AddressLineAr,
    string? AddressLineEn,
    string? BuildingNumber,
    string? StreetName,
    string? Landmark,
    string? PostalCode,
    string? AddressReference,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? Email,
    bool IsMainBranch);

public sealed record UpdateSchoolBranchRequest(
    string NameAr,
    string? NameEn,
    Guid CityId,
    Guid DistrictId,
    string? AddressLineAr,
    string? AddressLineEn,
    string? BuildingNumber,
    string? StreetName,
    string? Landmark,
    string? PostalCode,
    string? AddressReference,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? Email,
    bool IsMainBranch);

public sealed record SchoolStageOfferingDto(
    Guid Id,
    Guid BranchId,
    Guid EducationalStageId,
    string StageNameAr,
    string? StageNameEn,
    GenderType GenderType,
    int? Capacity,
    bool IsAdmissionOpen,
    bool IsActive,
    IReadOnlyList<Guid> GradeIds,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateSchoolStageOfferingRequest(
    Guid BranchId,
    Guid EducationalStageId,
    GenderType GenderType,
    int? Capacity,
    bool IsAdmissionOpen,
    IReadOnlyList<Guid> GradeIds);

public sealed record UpdateSchoolStageOfferingRequest(
    GenderType GenderType,
    int? Capacity,
    bool IsAdmissionOpen,
    IReadOnlyList<Guid> GradeIds);

public sealed record TuitionFeeDto(
    Guid Id,
    Guid BranchId,
    Guid EducationalStageId,
    string StageNameAr,
    string? StageNameEn,
    Guid? GradeId,
    string? GradeNameAr,
    string? GradeNameEn,
    Guid AcademicYearId,
    string AcademicYearNameAr,
    string? AcademicYearNameEn,
    FeeCategory Category,
    string? NameAr,
    string? NameEn,
    string CurrencyCode,
    decimal Amount,
    bool IsStartingFrom,
    string? NotesAr,
    string? NotesEn,
    string? InternalNotesAr,
    string? InternalNotesEn,
    int SortOrder,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<SchoolFeeInstallmentDisplayDto> Installments);

public sealed record SchoolFeeInstallmentDisplayDto(
    Guid Id,
    Guid TuitionFeeId,
    int SequenceNumber,
    string NameAr,
    string? NameEn,
    FeeInstallmentAmountMode AmountMode,
    decimal? FixedAmount,
    decimal? Percentage,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset? DueWindowStartUtc,
    DateTimeOffset? DueWindowEndUtc,
    string? NotesAr,
    string? NotesEn,
    int SortOrder,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset UpdatedAtUtc);

public sealed record SchoolPublishedDiscountDto(
    Guid Id,
    string TitleAr,
    string? TitleEn,
    string EligibilityDescriptionAr,
    string? EligibilityDescriptionEn,
    FeeDiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    int SortOrder,
    bool IsActive,
    bool IsPublished,
    string LifecycleState,
    DateTimeOffset UpdatedAtUtc);

public sealed record SchoolFinancialNoteDto(
    Guid Id,
    string TextAr,
    string? TextEn,
    bool IsInternal,
    int SortOrder,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateTuitionFeeRequest(
    Guid BranchId,
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    FeeCategory Category,
    string? NameAr,
    string? NameEn,
    string CurrencyCode,
    decimal Amount,
    bool IsStartingFrom,
    string? NotesAr,
    string? NotesEn,
    string? InternalNotesAr,
    string? InternalNotesEn,
    int SortOrder,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc);

public sealed record UpdateTuitionFeeRequest(
    Guid EducationalStageId,
    Guid? GradeId,
    Guid AcademicYearId,
    FeeCategory Category,
    string? NameAr,
    string? NameEn,
    string CurrencyCode,
    decimal Amount,
    bool IsStartingFrom,
    string? NotesAr,
    string? NotesEn,
    string? InternalNotesAr,
    string? InternalNotesEn,
    int SortOrder,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc);

public sealed record UpdateSchoolFeeVisibilityRequest(FeeVisibilityPolicy? FeeVisibilityPolicy);

public sealed record CreateSchoolFeeInstallmentDisplayRequest(
    int SequenceNumber,
    string NameAr,
    string? NameEn,
    FeeInstallmentAmountMode AmountMode,
    decimal? FixedAmount,
    decimal? Percentage,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset? DueWindowStartUtc,
    DateTimeOffset? DueWindowEndUtc,
    string? NotesAr,
    string? NotesEn,
    int SortOrder);

public sealed record UpdateSchoolFeeInstallmentDisplayRequest(
    int SequenceNumber,
    string NameAr,
    string? NameEn,
    FeeInstallmentAmountMode AmountMode,
    decimal? FixedAmount,
    decimal? Percentage,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset? DueWindowStartUtc,
    DateTimeOffset? DueWindowEndUtc,
    string? NotesAr,
    string? NotesEn,
    int SortOrder);

public sealed record CreateSchoolPublishedDiscountRequest(
    string TitleAr,
    string? TitleEn,
    string EligibilityDescriptionAr,
    string? EligibilityDescriptionEn,
    FeeDiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    int SortOrder);

public sealed record UpdateSchoolPublishedDiscountRequest(
    string TitleAr,
    string? TitleEn,
    string EligibilityDescriptionAr,
    string? EligibilityDescriptionEn,
    FeeDiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    int SortOrder);

public sealed record CreateSchoolFinancialNoteRequest(
    string TextAr,
    string? TextEn,
    bool IsInternal,
    int SortOrder);

public sealed record UpdateSchoolFinancialNoteRequest(
    string TextAr,
    string? TextEn,
    bool IsInternal,
    int SortOrder);

public sealed record SchoolFacilityListItemDto(
    Guid FacilityId,
    string NameAr,
    string? NameEn,
    string Slug,
    string? IconKey,
    bool IsSelected);

public sealed record ReplaceSchoolFacilitiesRequest(IReadOnlyList<Guid> FacilityIds);

public sealed record SchoolMediaDto(
    string? LogoUrl,
    string? CoverUrl,
    IReadOnlyList<SchoolGalleryImageDto> GalleryImages);

public sealed record SchoolGalleryImageDto(
    Guid Id,
    string ImageUrl,
    string? CaptionAr,
    string? CaptionEn,
    string? AltTextAr,
    string? AltTextEn,
    Guid? EducationalStageId,
    int SortOrder,
    bool IsActive);

public sealed record UpdateSchoolGalleryImageRequest(
    string? CaptionAr,
    string? CaptionEn,
    string? AltTextAr,
    string? AltTextEn,
    Guid? EducationalStageId,
    int SortOrder);

public sealed record ReorderSchoolGalleryImagesRequest(IReadOnlyList<Guid> ImageIds);

public sealed record SchoolAdditionalServiceDto(
    Guid Id,
    string NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? IconKey,
    int SortOrder,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateSchoolAdditionalServiceRequest(
    string NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? IconKey,
    int SortOrder);

public sealed record UpdateSchoolAdditionalServiceRequest(
    string NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? IconKey,
    int SortOrder);

public sealed record ReorderSchoolAdditionalServicesRequest(IReadOnlyList<Guid> ServiceIds);

public sealed record SchoolTeamMemberDto(
    Guid? MembershipId,
    Guid UserId,
    string DisplayName,
    string Email,
    bool IsOwner,
    SchoolTeamRole? Role,
    bool IsActive,
    SchoolBranchScopeMode BranchScopeMode,
    IReadOnlyList<Guid> AllowedBranchIds,
    DateTimeOffset? JoinedAtUtc);

public sealed record AddSchoolAdminRequest(string Email);

public sealed record UpsertSchoolTeamMemberRequest(
    string Email,
    SchoolTeamRole Role,
    SchoolBranchScopeMode BranchScopeMode,
    IReadOnlyList<Guid>? BranchIds);

public sealed record UpdateSchoolTeamMemberRequest(
    SchoolTeamRole Role,
    SchoolBranchScopeMode BranchScopeMode,
    IReadOnlyList<Guid>? BranchIds,
    bool IsActive);

public sealed record TransferSchoolOwnershipRequest(string NewOwnerEmail);
