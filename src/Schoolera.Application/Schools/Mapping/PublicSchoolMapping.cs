using Schoolera.Application.Common;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Fees;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Mapping;

internal static class PublicSchoolMapping
{
    private static readonly TimeSpan NewlyAddedWindow = TimeSpan.FromDays(30);

    public static PublicSchoolListItemDto ToListItem(PublicSchoolSearchProjection projection)
    {
        var badges = new List<string>(4);
        if (projection.IsAdmissionOpen)
        {
            badges.Add("admission-open");
        }

        if (DateTimeOffset.UtcNow - projection.CreatedAtUtc <= NewlyAddedWindow)
        {
            badges.Add("newly-added");
        }

        badges.Add($"school-type:{projection.SchoolType}");
        badges.Add($"gender:{projection.GenderType}");

        var useArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        return new PublicSchoolListItemDto(
            projection.Id,
            projection.Slug,
            LocalizationDisplayHelper.Pick(projection.NameAr, projection.NameEn),
            LocalizationDisplayHelper.Pick(projection.CityNameAr, projection.CityNameEn),
            projection.LogoUrl,
            projection.SchoolType,
            projection.GenderType,
            projection.IsAdmissionOpen,
            projection.CoverUrl,
            LocalizationDisplayHelper.Pick(projection.DistrictNameAr, projection.DistrictNameEn),
            useArabic ? projection.CurriculumNamesAr : projection.CurriculumNamesEn,
            useArabic ? projection.StageNamesAr : projection.StageNamesEn,
            projection.MinimumAnnualFee,
            projection.FeeCurrency,
            projection.DistanceKm,
            badges,
            projection.HasPublishedFees,
            projection.FeesRequireLogin,
            projection.IsFavorite);
    }

    public static PublicSchoolProfileDto ToProfile(
        School school,
        bool isAuthenticatedParent,
        bool? isFavorite = null)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var publishedFees = school.Branches
            .Where(branch => branch.IsActive)
            .SelectMany(branch => branch.TuitionFees)
            .Where(fee => fee.IsActive && fee.IsPublished && fee.IsCurrentlyEffective(utcNow))
            .ToArray();

        var hasPublishedFees = publishedFees.Length > 0;
        var canReveal = FeeVisibilityResolver.CanRevealDetailedFees(
            school.FeeVisibilityPolicy,
            isAuthenticatedParent);
        var feesRequireLogin = FeeVisibilityResolver.FeesRequireLogin(school.FeeVisibilityPolicy) &&
                               !canReveal;

        var isAdmissionOpen = school.Branches
            .Where(branch => branch.IsActive)
            .SelectMany(branch => branch.StageOfferings)
            .Any(offering => offering.IsActive && offering.IsAdmissionOpen);

        return new PublicSchoolProfileDto(
            school.Id,
            school.Slug,
            LocalizationDisplayHelper.Pick(school.NameAr, school.NameEn),
            LocalizationDisplayHelper.Pick(school.ShortDescriptionAr, school.ShortDescriptionEn),
            LocalizationDisplayHelper.Pick(school.FullDescriptionAr, school.FullDescriptionEn),
            school.LogoUrl,
            school.CoverUrl,
            school.SchoolType,
            school.GenderType,
            school.FoundedYear,
            school.StudentCount,
            isAdmissionOpen,
            new PublicSchoolContactDto(
                school.PublicPhone,
                school.PublicEmail,
                school.WebsiteUrl,
                school.WhatsAppNumber),
            new PublicSchoolSeoDto(
                LocalizationDisplayHelper.Pick(school.SeoTitleAr, school.SeoTitleEn),
                LocalizationDisplayHelper.Pick(school.SeoDescriptionAr, school.SeoDescriptionEn)),
            MapBranches(school),
            MapCurricula(school),
            MapFacilities(school),
            MapImages(school),
            MapOfferings(school),
            MapFees(publishedFees, canReveal),
            MapAdditionalServices(school),
            hasPublishedFees,
            feesRequireLogin,
            canReveal ? MapDiscounts(school, utcNow) : [],
            canReveal ? MapFinancialNotes(school) : [],
            isFavorite);
    }

    private static IReadOnlyList<PublicSchoolBranchDto> MapBranches(School school) =>
        school.Branches
            .Where(branch => branch.IsActive)
            .OrderByDescending(branch => branch.IsMainBranch)
            .ThenBy(branch => LocalizationDisplayHelper.Pick(branch.NameAr, branch.NameEn), StringComparer.OrdinalIgnoreCase)
            .ThenBy(branch => branch.Id)
            .Select(branch => new PublicSchoolBranchDto(
                branch.Id,
                branch.Slug,
                LocalizationDisplayHelper.Pick(branch.NameAr, branch.NameEn),
                LocalizationDisplayHelper.Pick(branch.City.NameAr, branch.City.NameEn),
                LocalizationDisplayHelper.Pick(branch.District.NameAr, branch.District.NameEn),
                LocalizationDisplayHelper.Pick(branch.AddressLineAr, branch.AddressLineEn),
                branch.Latitude,
                branch.Longitude,
                branch.Phone,
                branch.Email,
                branch.IsMainBranch))
            .ToArray();

    private static IReadOnlyList<PublicSchoolCurriculumItemDto> MapCurricula(School school) =>
        school.Curricula
            .Select(link => link.Curriculum)
            .Where(curriculum => curriculum.IsActive)
            .OrderBy(curriculum => curriculum.SortOrder)
            .ThenBy(curriculum => LocalizationDisplayHelper.Pick(curriculum.NameAr, curriculum.NameEn), StringComparer.OrdinalIgnoreCase)
            .Select(curriculum => new PublicSchoolCurriculumItemDto(
                curriculum.Id,
                curriculum.Slug,
                LocalizationDisplayHelper.Pick(curriculum.NameAr, curriculum.NameEn)))
            .ToArray();

    private static IReadOnlyList<PublicSchoolFacilityItemDto> MapFacilities(School school) =>
        school.Facilities
            .Select(link => link.Facility)
            .Where(facility => facility.IsActive)
            .OrderBy(facility => facility.SortOrder)
            .ThenBy(facility => LocalizationDisplayHelper.Pick(facility.NameAr, facility.NameEn), StringComparer.OrdinalIgnoreCase)
            .Select(facility => new PublicSchoolFacilityItemDto(
                facility.Id,
                facility.Slug,
                LocalizationDisplayHelper.Pick(facility.NameAr, facility.NameEn),
                facility.IconKey))
            .ToArray();

    private static IReadOnlyList<PublicSchoolImageItemDto> MapImages(School school) =>
        school.Images
            .Where(image => image.IsActive)
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .Select(image => new PublicSchoolImageItemDto(
                image.Id,
                image.ImageUrl,
                LocalizationDisplayHelper.Pick(image.CaptionAr, image.CaptionEn),
                LocalizationDisplayHelper.Pick(image.AltTextAr, image.AltTextEn),
                image.SortOrder,
                image.EducationalStageId))
            .ToArray();

    private static IReadOnlyList<PublicSchoolStageOfferingDto> MapOfferings(School school) =>
        school.Branches
            .Where(branch => branch.IsActive)
            .SelectMany(branch => branch.StageOfferings
                .Where(offering => offering.IsActive)
                .Select(offering => new { branch, offering }))
            .OrderBy(row => LocalizationDisplayHelper.Pick(row.branch.NameAr, row.branch.NameEn), StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.offering.EducationalStage.SortOrder)
            .ThenBy(row => row.offering.GenderType)
            .ThenBy(row => row.offering.Id)
            .Select(row => new PublicSchoolStageOfferingDto(
                row.offering.Id,
                row.branch.Id,
                LocalizationDisplayHelper.Pick(row.branch.NameAr, row.branch.NameEn),
                row.offering.EducationalStageId,
                LocalizationDisplayHelper.Pick(
                    row.offering.EducationalStage.NameAr,
                    row.offering.EducationalStage.NameEn),
                row.offering.GenderType,
                row.offering.IsAdmissionOpen,
                row.offering.Capacity,
                row.offering.GradeOfferings
                    .Where(gradeOffering => gradeOffering.IsActive)
                    .OrderBy(gradeOffering => gradeOffering.Grade.SortOrder)
                    .ThenBy(gradeOffering => LocalizationDisplayHelper.Pick(
                        gradeOffering.Grade.NameAr,
                        gradeOffering.Grade.NameEn), StringComparer.OrdinalIgnoreCase)
                    .Select(gradeOffering => new PublicSchoolGradeOfferingDto(
                        gradeOffering.Grade.Id,
                        gradeOffering.Grade.Slug,
                        LocalizationDisplayHelper.Pick(
                            gradeOffering.Grade.NameAr,
                            gradeOffering.Grade.NameEn)))
                    .ToArray()))
            .ToArray();

    private static IReadOnlyList<PublicSchoolTuitionFeeDto> MapFees(
        IReadOnlyList<TuitionFee> fees,
        bool canRevealDetails)
    {
        if (!canRevealDetails)
        {
            return Array.Empty<PublicSchoolTuitionFeeDto>();
        }

        return fees
            .OrderBy(fee => fee.AcademicYear.StartDate)
            .ThenBy(fee => LocalizationDisplayHelper.Pick(fee.SchoolBranch.NameAr, fee.SchoolBranch.NameEn), StringComparer.OrdinalIgnoreCase)
            .ThenBy(fee => fee.EducationalStage.SortOrder)
            .ThenBy(fee => fee.Grade?.SortOrder ?? int.MaxValue)
            .ThenBy(fee => fee.SortOrder)
            .ThenBy(fee => fee.Id)
            .Select(fee => new PublicSchoolTuitionFeeDto(
                LocalizationDisplayHelper.Pick(fee.SchoolBranch.NameAr, fee.SchoolBranch.NameEn),
                LocalizationDisplayHelper.Pick(
                    fee.EducationalStage.NameAr,
                    fee.EducationalStage.NameEn),
                fee.Grade is null
                    ? null
                    : LocalizationDisplayHelper.Pick(fee.Grade.NameAr, fee.Grade.NameEn),
                LocalizationDisplayHelper.Pick(fee.AcademicYear.NameAr, fee.AcademicYear.NameEn),
                fee.CurrencyCode,
                fee.Amount,
                LocalizationDisplayHelper.Pick(fee.NotesAr, fee.NotesEn),
                fee.IsStartingFrom,
                fee.Category,
                LocalizationDisplayHelper.Pick(fee.NameAr, fee.NameEn),
                MapInstallments(fee)))
            .ToArray();
    }

    private static IReadOnlyList<PublicSchoolFeeInstallmentDto> MapInstallments(TuitionFee fee) =>
        fee.Installments
            .Where(item => item.IsActive && item.IsPublished)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.SequenceNumber)
            .Select(item => new PublicSchoolFeeInstallmentDto(
                item.SequenceNumber,
                LocalizationDisplayHelper.Pick(item.NameAr, item.NameEn) ?? item.NameAr,
                item.AmountMode,
                item.FixedAmount,
                item.Percentage,
                item.DueDateUtc,
                item.DueWindowStartUtc,
                item.DueWindowEndUtc,
                LocalizationDisplayHelper.Pick(item.NotesAr, item.NotesEn),
                item.SortOrder))
            .ToArray();

    private static IReadOnlyList<PublicSchoolPublishedDiscountDto> MapDiscounts(
        School school,
        DateTimeOffset utcNow) =>
        school.PublishedDiscounts
            .Where(item => item.IsActive && item.IsPublished)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.StartUtc)
            .Select(item =>
            {
                var state = item.IsCurrent(utcNow)
                    ? "Current"
                    : item.IsUpcoming(utcNow)
                        ? "Upcoming"
                        : "Expired";

                return new PublicSchoolPublishedDiscountDto(
                    LocalizationDisplayHelper.Pick(item.TitleAr, item.TitleEn) ?? item.TitleAr,
                    LocalizationDisplayHelper.Pick(
                        item.EligibilityDescriptionAr,
                        item.EligibilityDescriptionEn) ?? item.EligibilityDescriptionAr,
                    item.DiscountType,
                    item.Value,
                    item.CurrencyCode,
                    item.StartUtc,
                    item.EndUtc,
                    state,
                    item.SortOrder);
            })
            .ToArray();

    private static IReadOnlyList<PublicSchoolFinancialNoteDto> MapFinancialNotes(School school) =>
        school.FinancialNotes
            .Where(note => note.IsActive && note.IsPublished && !note.IsInternal)
            .OrderBy(note => note.SortOrder)
            .ThenBy(note => note.Id)
            .Select(note => new PublicSchoolFinancialNoteDto(
                LocalizationDisplayHelper.Pick(note.TextAr, note.TextEn) ?? note.TextAr,
                note.SortOrder))
            .ToArray();

    private static IReadOnlyList<PublicSchoolAdditionalServiceDto> MapAdditionalServices(School school) =>
        school.AdditionalServices
            .Where(service => service.IsActive)
            .OrderBy(service => service.SortOrder)
            .ThenBy(service => LocalizationDisplayHelper.Pick(service.NameAr, service.NameEn), StringComparer.OrdinalIgnoreCase)
            .ThenBy(service => service.Id)
            .Select(service => new PublicSchoolAdditionalServiceDto(
                service.Id,
                LocalizationDisplayHelper.Pick(service.NameAr, service.NameEn),
                LocalizationDisplayHelper.Pick(service.DescriptionAr, service.DescriptionEn),
                service.IconKey,
                service.SortOrder))
            .ToArray();
}
