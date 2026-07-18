using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class School
{
    private School()
    {
    }

    public School(
        string nameAr,
        string? nameEn,
        string slug,
        SchoolType schoolType,
        GenderType genderType,
        SchoolStatus status)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SchoolType = schoolType;
        GenderType = genderType;
        Status = status;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string? ShortDescriptionAr { get; private set; }

    public string? ShortDescriptionEn { get; private set; }

    public string? FullDescriptionAr { get; private set; }

    public string? FullDescriptionEn { get; private set; }

    public string? LogoUrl { get; private set; }

    public string? CoverUrl { get; private set; }

    public SchoolType SchoolType { get; private set; }

    public GenderType GenderType { get; private set; }

    public int? FoundedYear { get; private set; }

    public int? StudentCount { get; private set; }

    public SchoolStatus Status { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public string? PublicPhone { get; private set; }

    public string? PublicEmail { get; private set; }

    public string? WebsiteUrl { get; private set; }

    public string? WhatsAppNumber { get; private set; }

    public string? SeoTitleAr { get; private set; }

    public string? SeoTitleEn { get; private set; }

    public string? SeoDescriptionAr { get; private set; }

    public string? SeoDescriptionEn { get; private set; }

    /// <summary>
    /// Explicit fee-visibility policy. Null means Product Egypt default is unresolved
    /// (see docs/fee-display-policy.md). Do not invent Public or AuthenticatedParentsOnly.
    /// </summary>
    public FeeVisibilityPolicy? FeeVisibilityPolicy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<SchoolBranch> Branches { get; private set; } = [];

    public ICollection<SchoolCurriculum> Curricula { get; private set; } = [];

    public ICollection<SchoolFacility> Facilities { get; private set; } = [];

    public ICollection<SchoolImage> Images { get; private set; } = [];

    public ICollection<SchoolTeamMember> TeamMembers { get; private set; } = [];

    public ICollection<SchoolAdditionalService> AdditionalServices { get; private set; } = [];

    public ICollection<SchoolPublishedDiscount> PublishedDiscounts { get; private set; } = [];

    public ICollection<SchoolFinancialNote> FinancialNotes { get; private set; } = [];

    /// <summary>Links the school to its owning SchoolOwner user (onboarding approval).</summary>
    public void AssignOwner(Guid ownerUserId)
    {
        OwnerUserId = ownerUserId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetFeeVisibilityPolicy(FeeVisibilityPolicy? policy)
    {
        if (policy is { } value && !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }

        FeeVisibilityPolicy = policy;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetStatus(SchoolStatus status)
    {
        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Applies onboarding-sourced profile fields to a newly created school.</summary>
    public void ApplyOnboardingProfile(
        string? shortDescriptionAr,
        string? shortDescriptionEn,
        int? foundedYear,
        string? websiteUrl,
        string? publicPhone,
        string? publicEmail,
        string? whatsAppNumber)
    {
        ShortDescriptionAr = shortDescriptionAr;
        ShortDescriptionEn = shortDescriptionEn;
        FoundedYear = foundedYear;
        WebsiteUrl = websiteUrl;
        PublicPhone = publicPhone;
        PublicEmail = publicEmail;
        WhatsAppNumber = whatsAppNumber;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates portal-managed profile fields. Does not change ownership, status, slug, or approval metadata.
    /// </summary>
    public void UpdatePortalProfile(
        string nameAr,
        string? nameEn,
        string? shortDescriptionAr,
        string? shortDescriptionEn,
        string? fullDescriptionAr,
        string? fullDescriptionEn,
        SchoolType schoolType,
        GenderType genderType,
        int? foundedYear,
        int? studentCount,
        string? publicPhone,
        string? publicEmail,
        string? websiteUrl,
        string? whatsAppNumber,
        string? seoTitleAr,
        string? seoTitleEn,
        string? seoDescriptionAr,
        string? seoDescriptionEn)
    {
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        ShortDescriptionAr = string.IsNullOrWhiteSpace(shortDescriptionAr) ? null : shortDescriptionAr.Trim();
        ShortDescriptionEn = string.IsNullOrWhiteSpace(shortDescriptionEn) ? null : shortDescriptionEn.Trim();
        FullDescriptionAr = string.IsNullOrWhiteSpace(fullDescriptionAr) ? null : fullDescriptionAr.Trim();
        FullDescriptionEn = string.IsNullOrWhiteSpace(fullDescriptionEn) ? null : fullDescriptionEn.Trim();
        SchoolType = schoolType;
        GenderType = genderType;
        FoundedYear = foundedYear;
        StudentCount = studentCount;
        PublicPhone = string.IsNullOrWhiteSpace(publicPhone) ? null : publicPhone.Trim();
        PublicEmail = string.IsNullOrWhiteSpace(publicEmail) ? null : publicEmail.Trim();
        WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim();
        WhatsAppNumber = string.IsNullOrWhiteSpace(whatsAppNumber) ? null : whatsAppNumber.Trim();
        SeoTitleAr = string.IsNullOrWhiteSpace(seoTitleAr) ? null : seoTitleAr.Trim();
        SeoTitleEn = string.IsNullOrWhiteSpace(seoTitleEn) ? null : seoTitleEn.Trim();
        SeoDescriptionAr = string.IsNullOrWhiteSpace(seoDescriptionAr) ? null : seoDescriptionAr.Trim();
        SeoDescriptionEn = string.IsNullOrWhiteSpace(seoDescriptionEn) ? null : seoDescriptionEn.Trim();
        Touch();
    }

    public void SetLogoUrl(string? logoUrl)
    {
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        Touch();
    }

    public void SetCoverUrl(string? coverUrl)
    {
        CoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl.Trim();
        Touch();
    }

    public void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    public void AddBranch(SchoolBranch branch)
    {
        Branches.Add(branch);
        Touch();
    }

    public void AddTeamMember(SchoolTeamMember member)
    {
        TeamMembers.Add(member);
        Touch();
    }

    public void AddAdditionalService(SchoolAdditionalService service)
    {
        AdditionalServices.Add(service);
        Touch();
    }

    public void AddFacility(SchoolFacility facility)
    {
        Facilities.Add(facility);
        Touch();
    }

    public void AddImage(SchoolImage image)
    {
        Images.Add(image);
        Touch();
    }

    /// <summary>
    /// Legacy factory for minimal create flows during transition.
    /// </summary>
    public static School CreateLegacy(string name, string? cityName)
    {
        var nameAr = name.Trim();
        var slug = SlugHelper.FromLatinText(nameAr);
        var school = new School(nameAr, null, slug, SchoolType.Private, GenderType.Mixed, SchoolStatus.Draft);
        if (!string.IsNullOrWhiteSpace(cityName))
        {
            school.ShortDescriptionAr = cityName.Trim();
        }

        return school;
    }
}
