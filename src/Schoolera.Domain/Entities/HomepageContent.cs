using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Single structured homepage content row (not a page builder).
/// Prefer one published row; drafts may coexist for editing.
/// </summary>
public sealed class HomepageContent
{
    private HomepageContent()
    {
    }

    public HomepageContent(
        string heroTitleAr,
        string heroTitleEn,
        string heroSubtitleAr,
        string heroSubtitleEn,
        string primaryCtaLabelAr,
        string primaryCtaLabelEn,
        string primaryCtaUrl,
        string? secondaryCtaLabelAr,
        string? secondaryCtaLabelEn,
        string? secondaryCtaUrl,
        string schoolsSectionTitleAr,
        string schoolsSectionTitleEn,
        string parentJourneyTitleAr,
        string parentJourneyTitleEn,
        string parentJourneyTextAr,
        string parentJourneyTextEn,
        string schoolJourneyTitleAr,
        string schoolJourneyTitleEn,
        string schoolJourneyTextAr,
        string schoolJourneyTextEn,
        string faqSectionTitleAr,
        string faqSectionTitleEn,
        string? faqSectionSubtitleAr,
        string? faqSectionSubtitleEn)
    {
        Id = Guid.NewGuid();
        Apply(
            heroTitleAr,
            heroTitleEn,
            heroSubtitleAr,
            heroSubtitleEn,
            primaryCtaLabelAr,
            primaryCtaLabelEn,
            primaryCtaUrl,
            secondaryCtaLabelAr,
            secondaryCtaLabelEn,
            secondaryCtaUrl,
            schoolsSectionTitleAr,
            schoolsSectionTitleEn,
            parentJourneyTitleAr,
            parentJourneyTitleEn,
            parentJourneyTextAr,
            parentJourneyTextEn,
            schoolJourneyTitleAr,
            schoolJourneyTitleEn,
            schoolJourneyTextAr,
            schoolJourneyTextEn,
            faqSectionTitleAr,
            faqSectionTitleEn,
            faqSectionSubtitleAr,
            faqSectionSubtitleEn);
        Status = CmsPublicationStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string HeroTitleAr { get; private set; } = string.Empty;

    public string HeroTitleEn { get; private set; } = string.Empty;

    public string HeroSubtitleAr { get; private set; } = string.Empty;

    public string HeroSubtitleEn { get; private set; } = string.Empty;

    public string PrimaryCtaLabelAr { get; private set; } = string.Empty;

    public string PrimaryCtaLabelEn { get; private set; } = string.Empty;

    public string PrimaryCtaUrl { get; private set; } = string.Empty;

    public string? SecondaryCtaLabelAr { get; private set; }

    public string? SecondaryCtaLabelEn { get; private set; }

    public string? SecondaryCtaUrl { get; private set; }

    public string SchoolsSectionTitleAr { get; private set; } = string.Empty;

    public string SchoolsSectionTitleEn { get; private set; } = string.Empty;

    public string ParentJourneyTitleAr { get; private set; } = string.Empty;

    public string ParentJourneyTitleEn { get; private set; } = string.Empty;

    public string ParentJourneyTextAr { get; private set; } = string.Empty;

    public string ParentJourneyTextEn { get; private set; } = string.Empty;

    public string SchoolJourneyTitleAr { get; private set; } = string.Empty;

    public string SchoolJourneyTitleEn { get; private set; } = string.Empty;

    public string SchoolJourneyTextAr { get; private set; } = string.Empty;

    public string SchoolJourneyTextEn { get; private set; } = string.Empty;

    public string FaqSectionTitleAr { get; private set; } = string.Empty;

    public string FaqSectionTitleEn { get; private set; } = string.Empty;

    public string? FaqSectionSubtitleAr { get; private set; }

    public string? FaqSectionSubtitleEn { get; private set; }

    public CmsPublicationStatus Status { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(
        string heroTitleAr,
        string heroTitleEn,
        string heroSubtitleAr,
        string heroSubtitleEn,
        string primaryCtaLabelAr,
        string primaryCtaLabelEn,
        string primaryCtaUrl,
        string? secondaryCtaLabelAr,
        string? secondaryCtaLabelEn,
        string? secondaryCtaUrl,
        string schoolsSectionTitleAr,
        string schoolsSectionTitleEn,
        string parentJourneyTitleAr,
        string parentJourneyTitleEn,
        string parentJourneyTextAr,
        string parentJourneyTextEn,
        string schoolJourneyTitleAr,
        string schoolJourneyTitleEn,
        string schoolJourneyTextAr,
        string schoolJourneyTextEn,
        string faqSectionTitleAr,
        string faqSectionTitleEn,
        string? faqSectionSubtitleAr,
        string? faqSectionSubtitleEn)
    {
        Apply(
            heroTitleAr,
            heroTitleEn,
            heroSubtitleAr,
            heroSubtitleEn,
            primaryCtaLabelAr,
            primaryCtaLabelEn,
            primaryCtaUrl,
            secondaryCtaLabelAr,
            secondaryCtaLabelEn,
            secondaryCtaUrl,
            schoolsSectionTitleAr,
            schoolsSectionTitleEn,
            parentJourneyTitleAr,
            parentJourneyTitleEn,
            parentJourneyTextAr,
            parentJourneyTextEn,
            schoolJourneyTitleAr,
            schoolJourneyTitleEn,
            schoolJourneyTextAr,
            schoolJourneyTextEn,
            faqSectionTitleAr,
            faqSectionTitleEn,
            faqSectionSubtitleAr,
            faqSectionSubtitleEn);
        Touch();
    }

    public void Publish()
    {
        Status = CmsPublicationStatus.Published;
        PublishedAtUtc ??= DateTimeOffset.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        Status = CmsPublicationStatus.Draft;
        Touch();
    }

    private void Apply(
        string heroTitleAr,
        string heroTitleEn,
        string heroSubtitleAr,
        string heroSubtitleEn,
        string primaryCtaLabelAr,
        string primaryCtaLabelEn,
        string primaryCtaUrl,
        string? secondaryCtaLabelAr,
        string? secondaryCtaLabelEn,
        string? secondaryCtaUrl,
        string schoolsSectionTitleAr,
        string schoolsSectionTitleEn,
        string parentJourneyTitleAr,
        string parentJourneyTitleEn,
        string parentJourneyTextAr,
        string parentJourneyTextEn,
        string schoolJourneyTitleAr,
        string schoolJourneyTitleEn,
        string schoolJourneyTextAr,
        string schoolJourneyTextEn,
        string faqSectionTitleAr,
        string faqSectionTitleEn,
        string? faqSectionSubtitleAr,
        string? faqSectionSubtitleEn)
    {
        HeroTitleAr = heroTitleAr.Trim();
        HeroTitleEn = heroTitleEn.Trim();
        HeroSubtitleAr = heroSubtitleAr.Trim();
        HeroSubtitleEn = heroSubtitleEn.Trim();
        PrimaryCtaLabelAr = primaryCtaLabelAr.Trim();
        PrimaryCtaLabelEn = primaryCtaLabelEn.Trim();
        PrimaryCtaUrl = primaryCtaUrl.Trim();
        SecondaryCtaLabelAr = NormalizeOptional(secondaryCtaLabelAr);
        SecondaryCtaLabelEn = NormalizeOptional(secondaryCtaLabelEn);
        SecondaryCtaUrl = NormalizeOptional(secondaryCtaUrl);
        SchoolsSectionTitleAr = schoolsSectionTitleAr.Trim();
        SchoolsSectionTitleEn = schoolsSectionTitleEn.Trim();
        ParentJourneyTitleAr = parentJourneyTitleAr.Trim();
        ParentJourneyTitleEn = parentJourneyTitleEn.Trim();
        ParentJourneyTextAr = parentJourneyTextAr.Trim();
        ParentJourneyTextEn = parentJourneyTextEn.Trim();
        SchoolJourneyTitleAr = schoolJourneyTitleAr.Trim();
        SchoolJourneyTitleEn = schoolJourneyTitleEn.Trim();
        SchoolJourneyTextAr = schoolJourneyTextAr.Trim();
        SchoolJourneyTextEn = schoolJourneyTextEn.Trim();
        FaqSectionTitleAr = faqSectionTitleAr.Trim();
        FaqSectionTitleEn = faqSectionTitleEn.Trim();
        FaqSectionSubtitleAr = NormalizeOptional(faqSectionSubtitleAr);
        FaqSectionSubtitleEn = NormalizeOptional(faqSectionSubtitleEn);
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
