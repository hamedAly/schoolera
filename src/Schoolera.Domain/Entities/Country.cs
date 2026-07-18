namespace Schoolera.Domain.Entities;

public sealed class Country
{
    private Country()
    {
    }

    public Country(string code, string nameAr, string? nameEn, string slug, int sortOrder)
    {
        Id = Guid.NewGuid();
        Code = code.Trim().ToUpperInvariant();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    /// <summary>ISO 3166-1 alpha-2 country code (e.g. EG).</summary>
    public string Code { get; private set; } = string.Empty;

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<Governorate> Governorates { get; private set; } = [];

    public void Update(string code, string nameAr, string? nameEn, string slug, int sortOrder, bool isActive)
    {
        Code = code.Trim().ToUpperInvariant();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
