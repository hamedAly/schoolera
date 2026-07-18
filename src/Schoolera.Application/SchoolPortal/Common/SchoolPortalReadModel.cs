using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Common;

public static class SchoolPortalReadModel
{
    public static AccessibleSchoolDto ToAccessibleSchool(
        AccessibleSchoolRow row,
        SchoolPortalPermissionsDto? permissions = null) =>
        new(row.Id, row.NameAr, row.NameEn, row.Slug, row.Status, row.LogoUrl, row.IsOwner, permissions);

    public static SchoolTeamMemberDto ToTeamMemberDto(
        SchoolTeamMember member,
        UserSummary? summary) =>
        new(
            member.Id,
            member.UserId,
            summary?.DisplayName ?? string.Empty,
            summary?.Email ?? string.Empty,
            IsOwner: false,
            member.Role,
            member.IsActive,
            member.BranchScopeMode,
            member.BranchAssignments.Select(b => b.SchoolBranchId).ToArray(),
            member.CreatedAtUtc);

    public static SchoolPortalProfileDto ToProfile(School school) =>
        new(
            school.Id,
            school.NameAr,
            school.NameEn,
            school.Slug,
            school.ShortDescriptionAr,
            school.ShortDescriptionEn,
            school.FullDescriptionAr,
            school.FullDescriptionEn,
            school.SchoolType,
            school.GenderType,
            school.FoundedYear,
            school.StudentCount,
            school.Status,
            school.PublicPhone,
            school.PublicEmail,
            school.WebsiteUrl,
            school.WhatsAppNumber,
            school.SeoTitleAr,
            school.SeoTitleEn,
            school.SeoDescriptionAr,
            school.SeoDescriptionEn,
            school.LogoUrl,
            school.CoverUrl,
            school.UpdatedAtUtc,
            school.FeeVisibilityPolicy);

    public static SchoolBranchDto ToBranch(SchoolBranch branch) =>
        new(
            branch.Id,
            branch.NameAr,
            branch.NameEn,
            branch.Slug,
            branch.CityId,
            branch.City.NameAr,
            branch.City.NameEn,
            branch.DistrictId,
            branch.District.NameAr,
            branch.District.NameEn,
            branch.AddressLineAr,
            branch.AddressLineEn,
            branch.BuildingNumber,
            branch.StreetName,
            branch.Landmark,
            branch.PostalCode,
            branch.AddressReference,
            branch.Latitude,
            branch.Longitude,
            branch.Phone,
            branch.Email,
            branch.IsMainBranch,
            branch.IsActive,
            branch.UpdatedAtUtc);

    public static SchoolStageOfferingDto ToOffering(SchoolStageOffering offering) =>
        new(
            offering.Id,
            offering.SchoolBranchId,
            offering.EducationalStageId,
            offering.EducationalStage.NameAr,
            offering.EducationalStage.NameEn,
            offering.GenderType,
            offering.Capacity,
            offering.IsAdmissionOpen,
            offering.IsActive,
            offering.GradeOfferings.Where(grade => grade.IsActive).Select(grade => grade.GradeId).ToArray(),
            offering.UpdatedAtUtc);

    public static TuitionFeeDto ToTuitionFee(TuitionFee fee) =>
        new(
            fee.Id,
            fee.SchoolBranchId,
            fee.EducationalStageId,
            fee.EducationalStage.NameAr,
            fee.EducationalStage.NameEn,
            fee.GradeId,
            fee.Grade?.NameAr,
            fee.Grade?.NameEn,
            fee.AcademicYearId,
            fee.AcademicYear.NameAr,
            fee.AcademicYear.NameEn,
            fee.Category,
            fee.NameAr,
            fee.NameEn,
            fee.CurrencyCode,
            fee.Amount,
            fee.IsStartingFrom,
            fee.NotesAr,
            fee.NotesEn,
            fee.InternalNotesAr,
            fee.InternalNotesEn,
            fee.SortOrder,
            fee.IsActive,
            fee.IsPublished,
            fee.EffectiveFromUtc,
            fee.EffectiveToUtc,
            fee.UpdatedAtUtc,
            fee.Installments
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.SequenceNumber)
                .Select(ToInstallment)
                .ToArray());

    public static SchoolFeeInstallmentDisplayDto ToInstallment(SchoolFeeInstallmentDisplay item) =>
        new(
            item.Id,
            item.TuitionFeeId,
            item.SequenceNumber,
            item.NameAr,
            item.NameEn,
            item.AmountMode,
            item.FixedAmount,
            item.Percentage,
            item.DueDateUtc,
            item.DueWindowStartUtc,
            item.DueWindowEndUtc,
            item.NotesAr,
            item.NotesEn,
            item.SortOrder,
            item.IsActive,
            item.IsPublished,
            item.UpdatedAtUtc);

    public static SchoolPublishedDiscountDto ToDiscount(
        SchoolPublishedDiscount item,
        DateTimeOffset utcNow)
    {
        var state = item.IsCurrent(utcNow)
            ? "Current"
            : item.IsUpcoming(utcNow)
                ? "Upcoming"
                : "Expired";

        return new SchoolPublishedDiscountDto(
            item.Id,
            item.TitleAr,
            item.TitleEn,
            item.EligibilityDescriptionAr,
            item.EligibilityDescriptionEn,
            item.DiscountType,
            item.Value,
            item.CurrencyCode,
            item.StartUtc,
            item.EndUtc,
            item.SchoolBranchId,
            item.EducationalStageId,
            item.GradeId,
            item.AcademicYearId,
            item.SortOrder,
            item.IsActive,
            item.IsPublished,
            state,
            item.UpdatedAtUtc);
    }

    public static SchoolFinancialNoteDto ToFinancialNote(SchoolFinancialNote note) =>
        new(
            note.Id,
            note.TextAr,
            note.TextEn,
            note.IsInternal,
            note.SortOrder,
            note.IsActive,
            note.IsPublished,
            note.UpdatedAtUtc);

    public static SchoolGalleryImageDto ToGalleryImage(SchoolImage image) =>
        new(
            image.Id,
            image.ImageUrl,
            image.CaptionAr,
            image.CaptionEn,
            image.AltTextAr,
            image.AltTextEn,
            image.EducationalStageId,
            image.SortOrder,
            image.IsActive);

    public static SchoolAdditionalServiceDto ToService(SchoolAdditionalService service) =>
        new(
            service.Id,
            service.NameAr,
            service.NameEn,
            service.DescriptionAr,
            service.DescriptionEn,
            service.IconKey,
            service.SortOrder,
            service.IsActive,
            service.UpdatedAtUtc);

    public static async Task<IReadOnlyList<SchoolTeamMemberDto>> BuildTeamAsync(
        School school,
        ISchoolPortalRepository repository,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        var members = await repository.ListActiveTeamMembersAsync(school.Id, cancellationToken);
        var userIds = members.Select(member => member.UserId).ToList();
        if (school.OwnerUserId is { } ownerId)
        {
            userIds.Add(ownerId);
        }

        var users = await userDirectory.GetUsersAsync(userIds, cancellationToken);
        var result = new List<SchoolTeamMemberDto>();

        if (school.OwnerUserId is { } ownerUserId &&
            users.TryGetValue(ownerUserId, out var ownerSummary))
        {
            result.Add(new SchoolTeamMemberDto(
                MembershipId: null,
                ownerUserId,
                ownerSummary.DisplayName,
                ownerSummary.Email,
                IsOwner: true,
                Role: null,
                IsActive: true,
                BranchScopeMode: SchoolBranchScopeMode.AllBranches,
                AllowedBranchIds: Array.Empty<Guid>(),
                JoinedAtUtc: null));
        }

        foreach (var member in members.OrderBy(member => member.CreatedAtUtc))
        {
            if (school.OwnerUserId == member.UserId)
            {
                continue;
            }

            users.TryGetValue(member.UserId, out var summary);
            result.Add(ToTeamMemberDto(member, summary));
        }

        return result;
    }

    public static SchoolDashboardDto BuildDashboard(
        Guid schoolId,
        SchoolStatus status,
        bool isEditable,
        SchoolDashboardCounts counts,
        int submittedApplications = 0,
        int underReviewApplications = 0,
        int acceptedApplications = 0,
        int rejectedApplications = 0,
        int totalActiveApplications = 0,
        IReadOnlyList<SchoolAdmissionRecentItemDto>? recentSubmittedApplications = null)
    {
        var completion = SchoolPortalCompleteness.CalculatePercent(counts);
        var warnings = SchoolPortalCompleteness.BuildWarnings(status, counts);

        return new SchoolDashboardDto(
            schoolId,
            counts.NameAr,
            counts.NameEn,
            counts.Slug,
            status,
            isEditable,
            counts.LogoUrl,
            counts.CoverUrl,
            counts.BranchCount,
            counts.ActiveBranchCount,
            counts.OfferingCount,
            counts.ActiveOfferingCount,
            counts.ActiveGradeCount,
            counts.TuitionFeeCount,
            counts.ActiveTuitionFeeCount,
            counts.FacilityCount,
            counts.ServiceCount,
            counts.ActiveServiceCount,
            counts.TeamMemberCount,
            counts.GalleryImageCount,
            completion,
            warnings,
            counts.UpdatedAtUtc,
            AdmissionsAvailable: true,
            submittedApplications,
            underReviewApplications,
            acceptedApplications,
            rejectedApplications,
            totalActiveApplications,
            recentSubmittedApplications ?? []);
    }
}
