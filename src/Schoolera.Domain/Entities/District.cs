namespace Schoolera.Domain.Entities;

public sealed class District
{
    private District()
    {
    }

    public District(Guid cityId, string nameAr, string? nameEn, string slug, int sortOrder)
    {
        Id = Guid.NewGuid();
        CityId = cityId;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CityId { get; private set; }

    public City City { get; private set; } = null!;

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(Guid cityId, string nameAr, string? nameEn, string slug, int sortOrder, bool isActive)
    {
        CityId = cityId;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
