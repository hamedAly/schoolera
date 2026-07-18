namespace Schoolera.Domain.Entities;

public sealed class Governorate
{
    private Governorate()
    {
    }

    public Governorate(Guid countryId, string nameAr, string? nameEn, string slug, int sortOrder)
    {
        Id = Guid.NewGuid();
        CountryId = countryId;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CountryId { get; private set; }

    public Country Country { get; private set; } = null!;

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<City> Cities { get; private set; } = [];

    public void Update(
        Guid countryId,
        string nameAr,
        string? nameEn,
        string slug,
        int sortOrder,
        bool isActive)
    {
        CountryId = countryId;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
