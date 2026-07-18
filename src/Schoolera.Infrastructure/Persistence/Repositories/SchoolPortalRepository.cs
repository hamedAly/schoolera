using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class SchoolPortalRepository(SchooleraDbContext dbContext) : ISchoolPortalRepository
{
    public async Task<IReadOnlyList<AccessibleSchoolRow>> ListAccessibleSchoolsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var owned = await dbContext.Schools
            .AsNoTracking()
            .Where(school => school.OwnerUserId == userId)
            .Select(school => new AccessibleSchoolRow(
                school.Id,
                school.NameAr,
                school.NameEn,
                school.Slug,
                school.Status,
                school.LogoUrl,
                true))
            .ToListAsync(cancellationToken);

        var memberSchoolIds = await dbContext.SchoolTeamMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId && member.IsActive)
            .Select(member => member.SchoolId)
            .ToListAsync(cancellationToken);

        var memberSchools = await dbContext.Schools
            .AsNoTracking()
            .Where(school => memberSchoolIds.Contains(school.Id) && school.OwnerUserId != userId)
            .Select(school => new AccessibleSchoolRow(
                school.Id,
                school.NameAr,
                school.NameEn,
                school.Slug,
                school.Status,
                school.LogoUrl,
                false))
            .ToListAsync(cancellationToken);

        return owned
            .Concat(memberSchools)
            .OrderBy(school => school.NameAr)
            .ToArray();
    }

    public async Task<SchoolAccessSnapshot?> GetAccessSnapshotAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var school = await dbContext.Schools
            .AsNoTracking()
            .Where(entry => entry.Id == schoolId)
            .Select(entry => new
            {
                entry.Id,
                entry.OwnerUserId,
                entry.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (school is null)
        {
            return null;
        }

        var membership = await dbContext.SchoolTeamMembers
            .AsNoTracking()
            .Include(member => member.BranchAssignments)
            .Where(member => member.SchoolId == schoolId &&
                             member.UserId == userId &&
                             member.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        var hasActiveMembership = membership is not null;

        if (school.OwnerUserId != userId && !hasActiveMembership)
        {
            return null;
        }

        return new SchoolAccessSnapshot(
            school.Id,
            school.OwnerUserId,
            school.Status,
            hasActiveMembership,
            membership?.Id,
            membership?.Role,
            membership?.BranchScopeMode ?? SchoolBranchScopeMode.AllBranches,
            membership?.BranchAssignments.Select(b => b.SchoolBranchId).ToArray()
                ?? Array.Empty<Guid>());
    }

    public Task<School?> GetSchoolForWriteAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        dbContext.Schools.FirstOrDefaultAsync(school => school.Id == schoolId, cancellationToken);

    public Task<School?> GetSchoolProfileAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        dbContext.Schools.AsNoTracking().FirstOrDefaultAsync(school => school.Id == schoolId, cancellationToken);

    public async Task<IReadOnlyList<SchoolBranch>> ListBranchesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolBranches
            .AsNoTracking()
            .Include(branch => branch.City)
            .Include(branch => branch.District)
            .Where(branch => branch.SchoolId == schoolId)
            .OrderByDescending(branch => branch.IsMainBranch)
            .ThenBy(branch => branch.NameAr)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SchoolBranch?> GetBranchForWriteAsync(
        Guid schoolId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolBranches
            .Include(branch => branch.City)
            .Include(branch => branch.District)
            .FirstOrDefaultAsync(
                branch => branch.SchoolId == schoolId && branch.Id == branchId,
                cancellationToken);
    }

    public Task<bool> BranchSlugExistsAsync(
        Guid schoolId,
        string slug,
        Guid? excludeBranchId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolBranches
            .AsNoTracking()
            .AnyAsync(
                branch => branch.SchoolId == schoolId &&
                          branch.Slug == slug &&
                          (excludeBranchId == null || branch.Id != excludeBranchId),
                cancellationToken);

    public Task<bool> HasOtherActiveMainBranchAsync(
        Guid schoolId,
        Guid excludeBranchId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolBranches
            .AsNoTracking()
            .AnyAsync(
                branch => branch.SchoolId == schoolId &&
                          branch.Id != excludeBranchId &&
                          branch.IsMainBranch &&
                          branch.IsActive,
                cancellationToken);

    public async Task<IReadOnlyList<SchoolStageOffering>> ListOfferingsAsync(
        Guid schoolId,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SchoolStageOfferings
            .AsNoTracking()
            .Include(offering => offering.EducationalStage)
            .Include(offering => offering.GradeOfferings)
            .Where(offering => offering.SchoolBranch.SchoolId == schoolId);

        if (branchId is { } filterBranchId)
        {
            query = query.Where(offering => offering.SchoolBranchId == filterBranchId);
        }

        return await query
            .OrderBy(offering => offering.EducationalStage.SortOrder)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SchoolStageOffering?> GetOfferingForWriteAsync(
        Guid schoolId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolStageOfferings
            .Include(offering => offering.EducationalStage)
            .Include(offering => offering.GradeOfferings)
            .FirstOrDefaultAsync(
                offering => offering.Id == offeringId &&
                            offering.SchoolBranch.SchoolId == schoolId,
                cancellationToken);
    }

    public Task<bool> DuplicateOfferingExistsAsync(
        Guid branchId,
        Guid educationalStageId,
        GenderType genderType,
        Guid? excludeOfferingId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolStageOfferings
            .AsNoTracking()
            .AnyAsync(
                offering => offering.SchoolBranchId == branchId &&
                            offering.EducationalStageId == educationalStageId &&
                            offering.GenderType == genderType &&
                            (excludeOfferingId == null || offering.Id != excludeOfferingId),
                cancellationToken);

    public async Task<IReadOnlyList<TuitionFee>> ListTuitionFeesAsync(
        Guid schoolId,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TuitionFees
            .AsNoTracking()
            .Include(fee => fee.EducationalStage)
            .Include(fee => fee.Grade)
            .Include(fee => fee.AcademicYear)
            .Include(fee => fee.Installments)
            .Where(fee => fee.SchoolBranch.SchoolId == schoolId);

        if (branchId is { } filterBranchId)
        {
            query = query.Where(fee => fee.SchoolBranchId == filterBranchId);
        }

        return await query
            .OrderBy(fee => fee.EducationalStage.SortOrder)
            .ThenBy(fee => fee.Grade!.SortOrder)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TuitionFee?> GetTuitionFeeForWriteAsync(
        Guid schoolId,
        Guid feeId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TuitionFees
            .Include(fee => fee.EducationalStage)
            .Include(fee => fee.Grade)
            .Include(fee => fee.AcademicYear)
            .Include(fee => fee.Installments)
            .FirstOrDefaultAsync(
                fee => fee.Id == feeId &&
                       fee.SchoolBranch.SchoolId == schoolId,
                cancellationToken);
    }

    public Task<bool> DuplicateFeeExistsAsync(
        Guid branchId,
        Guid educationalStageId,
        Guid? gradeId,
        Guid academicYearId,
        FeeCategory category,
        Guid? excludeFeeId,
        CancellationToken cancellationToken = default) =>
        dbContext.TuitionFees
            .AsNoTracking()
            .AnyAsync(
                fee => fee.IsActive &&
                       fee.SchoolBranchId == branchId &&
                       fee.EducationalStageId == educationalStageId &&
                       fee.GradeId == gradeId &&
                       fee.AcademicYearId == academicYearId &&
                       fee.Category == category &&
                       (excludeFeeId == null || fee.Id != excludeFeeId),
                cancellationToken);

    public Task<SchoolFeeInstallmentDisplay?> GetInstallmentForWriteAsync(
        Guid schoolId,
        Guid feeId,
        Guid installmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolFeeInstallmentDisplays
            .FirstOrDefaultAsync(
                item => item.Id == installmentId &&
                        item.TuitionFeeId == feeId &&
                        item.TuitionFee.SchoolBranch.SchoolId == schoolId,
                cancellationToken);

    public async Task AddInstallmentAsync(
        SchoolFeeInstallmentDisplay installment,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SchoolFeeInstallmentDisplays.AddAsync(installment, cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolPublishedDiscount>> ListPublishedDiscountsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolPublishedDiscounts
            .AsNoTracking()
            .Where(item => item.SchoolId == schoolId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.TitleAr)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SchoolPublishedDiscount?> GetPublishedDiscountForWriteAsync(
        Guid schoolId,
        Guid discountId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolPublishedDiscounts
            .FirstOrDefaultAsync(
                item => item.Id == discountId && item.SchoolId == schoolId,
                cancellationToken);

    public async Task AddPublishedDiscountAsync(
        SchoolPublishedDiscount discount,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SchoolPublishedDiscounts.AddAsync(discount, cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolFinancialNote>> ListFinancialNotesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolFinancialNotes
            .AsNoTracking()
            .Where(item => item.SchoolId == schoolId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.TextAr)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SchoolFinancialNote?> GetFinancialNoteForWriteAsync(
        Guid schoolId,
        Guid noteId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolFinancialNotes
            .FirstOrDefaultAsync(
                item => item.Id == noteId && item.SchoolId == schoolId,
                cancellationToken);

    public async Task AddFinancialNoteAsync(
        SchoolFinancialNote note,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SchoolFinancialNotes.AddAsync(note, cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolFacility>> ListSchoolFacilitiesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolFacilities
            .AsNoTracking()
            .Include(facility => facility.Facility)
            .Where(facility => facility.SchoolId == schoolId)
            .OrderBy(facility => facility.Facility.SortOrder)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Facility>> GetActiveFacilitiesByIdsAsync(
        IReadOnlyList<Guid> facilityIds,
        CancellationToken cancellationToken = default)
    {
        if (facilityIds.Count == 0)
        {
            return Array.Empty<Facility>();
        }

        return await dbContext.Facilities
            .AsNoTracking()
            .Where(facility => facilityIds.Contains(facility.Id) && facility.IsActive)
            .ToArrayAsync(cancellationToken);
    }

    public async Task ReplaceSchoolFacilitiesAsync(
        Guid schoolId,
        IReadOnlyList<Guid> facilityIds,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.SchoolFacilities
            .Where(facility => facility.SchoolId == schoolId)
            .ToListAsync(cancellationToken);

        dbContext.SchoolFacilities.RemoveRange(existing);

        foreach (var facilityId in facilityIds.Distinct())
        {
            dbContext.SchoolFacilities.Add(new SchoolFacility(schoolId, facilityId));
        }
    }

    public async Task<IReadOnlyList<SchoolImage>> ListImagesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolImages
            .AsNoTracking()
            .Where(image => image.SchoolId == schoolId)
            .OrderBy(image => image.SortOrder)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SchoolImage?> GetImageForWriteAsync(
        Guid schoolId,
        Guid imageId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolImages.FirstOrDefaultAsync(
            image => image.SchoolId == schoolId && image.Id == imageId,
            cancellationToken);

    public Task<int> CountGalleryImagesAsync(
        Guid schoolId,
        Guid? educationalStageId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolImages
            .AsNoTracking()
            .CountAsync(
                image => image.SchoolId == schoolId &&
                         image.IsActive &&
                         image.EducationalStageId == educationalStageId,
                cancellationToken);

    public async Task<IReadOnlyList<SchoolAdditionalService>> ListServicesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolAdditionalServices
            .AsNoTracking()
            .Where(service => service.SchoolId == schoolId)
            .OrderBy(service => service.SortOrder)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SchoolAdditionalService?> GetServiceForWriteAsync(
        Guid schoolId,
        Guid serviceId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdditionalServices.FirstOrDefaultAsync(
            service => service.SchoolId == schoolId && service.Id == serviceId,
            cancellationToken);

    public async Task<IReadOnlyList<SchoolTeamMember>> ListActiveTeamMembersAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SchoolTeamMembers
            .AsNoTracking()
            .Include(member => member.BranchAssignments)
            .Where(member => member.SchoolId == schoolId && member.IsActive)
            .OrderBy(member => member.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SchoolTeamMember?> GetTeamMemberForWriteAsync(
        Guid schoolId,
        Guid membershipId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolTeamMembers
            .Include(member => member.BranchAssignments)
            .FirstOrDefaultAsync(
                member => member.SchoolId == schoolId && member.Id == membershipId,
                cancellationToken);

    public Task<SchoolTeamMember?> GetTeamMemberByUserForWriteAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolTeamMembers
            .Include(member => member.BranchAssignments)
            .FirstOrDefaultAsync(
                member => member.SchoolId == schoolId && member.UserId == userId,
                cancellationToken);

    public Task<bool> ActiveTeamMemberExistsAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolTeamMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.SchoolId == schoolId && member.UserId == userId && member.IsActive,
                cancellationToken);

    public Task<bool> DistrictBelongsToCityAsync(
        Guid districtId,
        Guid cityId,
        CancellationToken cancellationToken = default) =>
        dbContext.Districts
            .AsNoTracking()
            .AnyAsync(district => district.Id == districtId && district.CityId == cityId, cancellationToken);

    public Task<bool> EducationalStageExistsAsync(
        Guid educationalStageId,
        CancellationToken cancellationToken = default) =>
        dbContext.EducationalStages
            .AsNoTracking()
            .AnyAsync(stage => stage.Id == educationalStageId && stage.IsActive, cancellationToken);

    public async Task<bool> GradesBelongToStageAsync(
        Guid educationalStageId,
        IReadOnlyList<Guid> gradeIds,
        CancellationToken cancellationToken = default)
    {
        if (gradeIds.Count == 0)
        {
            return true;
        }

        var distinct = gradeIds.Distinct().ToArray();
        var count = await dbContext.Grades
            .AsNoTracking()
            .CountAsync(
                grade => distinct.Contains(grade.Id) &&
                         grade.EducationalStageId == educationalStageId &&
                         grade.IsActive,
                cancellationToken);

        return count == distinct.Length;
    }

    public Task<bool> AcademicYearExistsAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default) =>
        dbContext.AcademicYears
            .AsNoTracking()
            .AnyAsync(year => year.Id == academicYearId && year.IsActive, cancellationToken);

    public async Task<SchoolDashboardCounts> GetDashboardCountsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var school = await dbContext.Schools
            .AsNoTracking()
            .Where(entry => entry.Id == schoolId)
            .Select(entry => new
            {
                entry.NameAr,
                entry.NameEn,
                entry.Slug,
                entry.LogoUrl,
                entry.CoverUrl,
                entry.UpdatedAtUtc,
                entry.ShortDescriptionAr,
                entry.FullDescriptionAr,
                entry.PublicPhone,
                entry.PublicEmail,
            })
            .FirstAsync(cancellationToken);

        var branchCount = await dbContext.SchoolBranches
            .AsNoTracking()
            .CountAsync(branch => branch.SchoolId == schoolId, cancellationToken);
        var activeBranchCount = await dbContext.SchoolBranches
            .AsNoTracking()
            .CountAsync(branch => branch.SchoolId == schoolId && branch.IsActive, cancellationToken);
        var mainBranchCount = await dbContext.SchoolBranches
            .AsNoTracking()
            .CountAsync(
                branch => branch.SchoolId == schoolId && branch.IsMainBranch && branch.IsActive,
                cancellationToken);

        var offeringCount = await dbContext.SchoolStageOfferings
            .AsNoTracking()
            .CountAsync(
                offering => offering.SchoolBranch.SchoolId == schoolId,
                cancellationToken);
        var activeOfferingCount = await dbContext.SchoolStageOfferings
            .AsNoTracking()
            .CountAsync(
                offering => offering.SchoolBranch.SchoolId == schoolId && offering.IsActive,
                cancellationToken);

        var tuitionFeeCount = await dbContext.TuitionFees
            .AsNoTracking()
            .CountAsync(fee => fee.SchoolBranch.SchoolId == schoolId, cancellationToken);
        var activeTuitionFeeCount = await dbContext.TuitionFees
            .AsNoTracking()
            .CountAsync(fee => fee.SchoolBranch.SchoolId == schoolId && fee.IsActive, cancellationToken);

        var serviceCount = await dbContext.SchoolAdditionalServices
            .AsNoTracking()
            .CountAsync(service => service.SchoolId == schoolId, cancellationToken);
        var activeServiceCount = await dbContext.SchoolAdditionalServices
            .AsNoTracking()
            .CountAsync(service => service.SchoolId == schoolId && service.IsActive, cancellationToken);

        var teamMemberCount = await dbContext.SchoolTeamMembers
            .AsNoTracking()
            .CountAsync(member => member.SchoolId == schoolId && member.IsActive, cancellationToken);

        var galleryImageCount = await dbContext.SchoolImages
            .AsNoTracking()
            .CountAsync(image => image.SchoolId == schoolId && image.IsActive, cancellationToken);

        var facilityCount = await dbContext.SchoolFacilities
            .AsNoTracking()
            .CountAsync(facility => facility.SchoolId == schoolId, cancellationToken);

        var activeGradeCount = await dbContext.SchoolGradeOfferings
            .AsNoTracking()
            .CountAsync(
                grade => grade.IsActive &&
                         grade.SchoolStageOffering.IsActive &&
                         grade.SchoolStageOffering.SchoolBranch.SchoolId == schoolId,
                cancellationToken);

        return new SchoolDashboardCounts(
            school.NameAr,
            school.NameEn,
            school.Slug,
            school.LogoUrl,
            school.CoverUrl,
            school.UpdatedAtUtc,
            branchCount,
            activeBranchCount,
            mainBranchCount,
            offeringCount,
            activeOfferingCount,
            activeGradeCount,
            tuitionFeeCount,
            activeTuitionFeeCount,
            facilityCount,
            serviceCount,
            activeServiceCount,
            teamMemberCount,
            galleryImageCount,
            !string.IsNullOrWhiteSpace(school.LogoUrl),
            !string.IsNullOrWhiteSpace(school.CoverUrl),
            !string.IsNullOrWhiteSpace(school.ShortDescriptionAr),
            !string.IsNullOrWhiteSpace(school.FullDescriptionAr),
            !string.IsNullOrWhiteSpace(school.PublicPhone) ||
            !string.IsNullOrWhiteSpace(school.PublicEmail));
    }

    public async Task<IReadOnlySet<string>> GetReferencedMediaUrlsAsync(
        CancellationToken cancellationToken = default)
    {
        var logoUrls = await dbContext.Schools
            .AsNoTracking()
            .Where(school => school.LogoUrl != null)
            .Select(school => school.LogoUrl!)
            .ToListAsync(cancellationToken);

        var coverUrls = await dbContext.Schools
            .AsNoTracking()
            .Where(school => school.CoverUrl != null)
            .Select(school => school.CoverUrl!)
            .ToListAsync(cancellationToken);

        var imageUrls = await dbContext.SchoolImages
            .AsNoTracking()
            .Select(image => image.ImageUrl)
            .ToListAsync(cancellationToken);

        return logoUrls.Concat(coverUrls).Concat(imageUrls).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task<School?> GetSchoolBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default) =>
        dbContext.Schools.AsNoTracking().FirstOrDefaultAsync(school => school.Slug == slug, cancellationToken);
}
