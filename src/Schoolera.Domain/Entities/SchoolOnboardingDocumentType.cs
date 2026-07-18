namespace Schoolera.Domain.Entities;

/// <summary>
/// Configurable, country-neutral onboarding document type with bilingual display names.
/// Codes are stable and must not embed government-specific service names.
/// </summary>
public sealed class SchoolOnboardingDocumentType
{
    private SchoolOnboardingDocumentType()
    {
    }

    public SchoolOnboardingDocumentType(
        string code,
        string nameAr,
        string nameEn,
        bool isRequired,
        int sortOrder,
        bool isActive = true)
    {
        Id = Guid.NewGuid();
        Code = code.Trim();
        NameAr = nameAr.Trim();
        NameEn = nameEn.Trim();
        IsRequired = isRequired;
        SortOrder = sortOrder;
        IsActive = isActive;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string NameAr { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public bool IsRequired { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }
}
