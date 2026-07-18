using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Dtos;

public sealed record PublicSchoolListItemDto(
    Guid Id,
    string Slug,
    string Name,
    string City,
    string? LogoUrl,
    SchoolType SchoolType,
    GenderType GenderType,
    bool IsAdmissionOpen,
    string? CoverUrl = null,
    string? District = null,
    IReadOnlyList<string>? CurriculumSummary = null,
    IReadOnlyList<string>? EducationalStageSummary = null,
    decimal? MinimumAnnualFee = null,
    string? FeeCurrency = null,
    double? DistanceKm = null,
    IReadOnlyList<string>? Badges = null,
    bool HasPublishedFees = false,
    bool FeesRequireLogin = false,
    bool? IsFavorite = null);

public sealed record PublicSchoolContactDto(
    string? Phone,
    string? Email,
    string? WebsiteUrl,
    string? WhatsAppNumber);

public sealed record PublicSchoolSeoDto(string? Title, string? Description);

public sealed record PublicSchoolBranchDto(
    Guid Id,
    string Slug,
    string Name,
    string City,
    string District,
    string? AddressLine,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? Email,
    bool IsMainBranch);

public sealed record PublicSchoolCurriculumItemDto(Guid Id, string Slug, string Name);

public sealed record PublicSchoolFacilityItemDto(
    Guid Id,
    string Slug,
    string Name,
    string? IconKey);

public sealed record PublicSchoolImageItemDto(
    Guid Id,
    string ImageUrl,
    string? Caption,
    string? AltText,
    int SortOrder,
    Guid? EducationalStageId = null);

public sealed record PublicSchoolGradeOfferingDto(Guid Id, string Slug, string Name);

public sealed record PublicSchoolStageOfferingDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid EducationalStageId,
    string StageName,
    GenderType GenderType,
    bool IsAdmissionOpen,
    int? Capacity,
    IReadOnlyList<PublicSchoolGradeOfferingDto> Grades);

public sealed record PublicSchoolFeeInstallmentDto(
    int SequenceNumber,
    string Name,
    FeeInstallmentAmountMode AmountMode,
    decimal? FixedAmount,
    decimal? Percentage,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset? DueWindowStartUtc,
    DateTimeOffset? DueWindowEndUtc,
    string? Notes,
    int SortOrder);

public sealed record PublicSchoolPublishedDiscountDto(
    string Title,
    string EligibilityDescription,
    FeeDiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string LifecycleState,
    int SortOrder);

public sealed record PublicSchoolFinancialNoteDto(
    string Text,
    int SortOrder);

public sealed record PublicSchoolTuitionFeeDto(
    string BranchName,
    string StageName,
    string? GradeName,
    string AcademicYearName,
    string CurrencyCode,
    decimal? Amount,
    string? Notes,
    bool IsStartingFrom = false,
    FeeCategory Category = FeeCategory.Tuition,
    string? Name = null,
    IReadOnlyList<PublicSchoolFeeInstallmentDto>? Installments = null);

public sealed record PublicSchoolAdditionalServiceDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconKey,
    int SortOrder);

public sealed record PublicSchoolProfileDto(
    Guid Id,
    string Slug,
    string Name,
    string? ShortDescription,
    string? FullDescription,
    string? LogoUrl,
    string? CoverUrl,
    SchoolType SchoolType,
    GenderType GenderType,
    int? FoundedYear,
    int? StudentCount,
    bool IsAdmissionOpen,
    PublicSchoolContactDto Contact,
    PublicSchoolSeoDto Seo,
    IReadOnlyList<PublicSchoolBranchDto> Branches,
    IReadOnlyList<PublicSchoolCurriculumItemDto> Curricula,
    IReadOnlyList<PublicSchoolFacilityItemDto> Facilities,
    IReadOnlyList<PublicSchoolImageItemDto> Images,
    IReadOnlyList<PublicSchoolStageOfferingDto> Offerings,
    IReadOnlyList<PublicSchoolTuitionFeeDto> Fees,
    IReadOnlyList<PublicSchoolAdditionalServiceDto> AdditionalServices,
    bool HasPublishedFees = false,
    bool FeesRequireLogin = false,
    IReadOnlyList<PublicSchoolPublishedDiscountDto>? PublishedDiscounts = null,
    IReadOnlyList<PublicSchoolFinancialNoteDto>? FinancialNotes = null,
    bool? IsFavorite = null);

public sealed record SchoolContactLeadRequest(
    string Name,
    string Phone,
    string? Email,
    string? Message,
    bool ConsentAccepted,
    string Source,
    string? Website);

public sealed record SchoolContactLeadResultDto(
    Guid LeadId,
    DateTimeOffset SubmittedAtUtc,
    string Message);

public static class SchoolContactLeadSources
{
    public const string SchoolProfile = "school-profile";
    public const string RelatedSchoolCard = "related-school-card";
    public const string SearchResult = "search-result";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        SchoolProfile,
        RelatedSchoolCard,
        SearchResult,
    };

    public static string? Normalize(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var trimmed = source.Trim();
        return Allowed.FirstOrDefault(allowed =>
            string.Equals(allowed, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
