using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolPortalRepository
{
    Task<IReadOnlyList<AccessibleSchoolRow>> ListAccessibleSchoolsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SchoolAccessSnapshot?> GetAccessSnapshotAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<School?> GetSchoolForWriteAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<School?> GetSchoolProfileAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolBranch>> ListBranchesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolBranch?> GetBranchForWriteAsync(
        Guid schoolId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<bool> BranchSlugExistsAsync(
        Guid schoolId,
        string slug,
        Guid? excludeBranchId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOtherActiveMainBranchAsync(
        Guid schoolId,
        Guid excludeBranchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolStageOffering>> ListOfferingsAsync(
        Guid schoolId,
        Guid? branchId,
        CancellationToken cancellationToken = default);

    Task<SchoolStageOffering?> GetOfferingForWriteAsync(
        Guid schoolId,
        Guid offeringId,
        CancellationToken cancellationToken = default);

    Task<bool> DuplicateOfferingExistsAsync(
        Guid branchId,
        Guid educationalStageId,
        GenderType genderType,
        Guid? excludeOfferingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TuitionFee>> ListTuitionFeesAsync(
        Guid schoolId,
        Guid? branchId,
        CancellationToken cancellationToken = default);

    Task<TuitionFee?> GetTuitionFeeForWriteAsync(
        Guid schoolId,
        Guid feeId,
        CancellationToken cancellationToken = default);

    Task<bool> DuplicateFeeExistsAsync(
        Guid branchId,
        Guid educationalStageId,
        Guid? gradeId,
        Guid academicYearId,
        FeeCategory category,
        Guid? excludeFeeId,
        CancellationToken cancellationToken = default);

    Task<SchoolFeeInstallmentDisplay?> GetInstallmentForWriteAsync(
        Guid schoolId,
        Guid feeId,
        Guid installmentId,
        CancellationToken cancellationToken = default);

    Task AddInstallmentAsync(
        SchoolFeeInstallmentDisplay installment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolPublishedDiscount>> ListPublishedDiscountsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolPublishedDiscount?> GetPublishedDiscountForWriteAsync(
        Guid schoolId,
        Guid discountId,
        CancellationToken cancellationToken = default);

    Task AddPublishedDiscountAsync(
        SchoolPublishedDiscount discount,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolFinancialNote>> ListFinancialNotesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolFinancialNote?> GetFinancialNoteForWriteAsync(
        Guid schoolId,
        Guid noteId,
        CancellationToken cancellationToken = default);

    Task AddFinancialNoteAsync(
        SchoolFinancialNote note,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolFacility>> ListSchoolFacilitiesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Facility>> GetActiveFacilitiesByIdsAsync(
        IReadOnlyList<Guid> facilityIds,
        CancellationToken cancellationToken = default);

    Task ReplaceSchoolFacilitiesAsync(
        Guid schoolId,
        IReadOnlyList<Guid> facilityIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolImage>> ListImagesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolImage?> GetImageForWriteAsync(
        Guid schoolId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task<int> CountGalleryImagesAsync(
        Guid schoolId,
        Guid? educationalStageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolAdditionalService>> ListServicesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolAdditionalService?> GetServiceForWriteAsync(
        Guid schoolId,
        Guid serviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolTeamMember>> ListActiveTeamMembersAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolTeamMember?> GetTeamMemberForWriteAsync(
        Guid schoolId,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<SchoolTeamMember?> GetTeamMemberByUserForWriteAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveTeamMemberExistsAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> DistrictBelongsToCityAsync(
        Guid districtId,
        Guid cityId,
        CancellationToken cancellationToken = default);

    Task<bool> EducationalStageExistsAsync(
        Guid educationalStageId,
        CancellationToken cancellationToken = default);

    Task<bool> GradesBelongToStageAsync(
        Guid educationalStageId,
        IReadOnlyList<Guid> gradeIds,
        CancellationToken cancellationToken = default);

    Task<bool> AcademicYearExistsAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default);

    Task<SchoolDashboardCounts> GetDashboardCountsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetReferencedMediaUrlsAsync(
        CancellationToken cancellationToken = default);

    Task<School?> GetSchoolBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}

public sealed record AccessibleSchoolRow(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    SchoolStatus Status,
    string? LogoUrl,
    bool IsOwner);

public sealed record SchoolAccessSnapshot(
    Guid SchoolId,
    Guid? OwnerUserId,
    SchoolStatus Status,
    bool HasActiveMembership,
    Guid? MembershipId,
    SchoolTeamRole? MembershipRole,
    SchoolBranchScopeMode BranchScopeMode,
    IReadOnlyList<Guid> AllowedBranchIds);

public sealed record SchoolDashboardCounts(
    string NameAr,
    string? NameEn,
    string Slug,
    string? LogoUrl,
    string? CoverUrl,
    DateTimeOffset UpdatedAtUtc,
    int BranchCount,
    int ActiveBranchCount,
    int MainBranchCount,
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
    bool HasLogo,
    bool HasCover,
    bool HasShortDescription,
    bool HasFullDescription,
    bool HasPublicContact);
