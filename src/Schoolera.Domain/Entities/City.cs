namespace Schoolera.Domain.Entities;

public sealed class City
{
    private City()
    {
    }

    public City(string nameAr, string? nameEn, string slug, int sortOrder, Guid? governorateId = null)
    {
        Id = Guid.NewGuid();
        GovernorateId = governorateId;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Optional until a verified governorate mapping exists. Never invent a default.
    /// </summary>
    public Guid? GovernorateId { get; private set; }

    public Governorate? Governorate { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<District> Districts { get; private set; } = [];

    public void AssignGovernorate(Guid governorateId)
    {
        GovernorateId = governorateId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Update(
        string nameAr,
        string? nameEn,
        string slug,
        int sortOrder,
        bool isActive,
        Guid? governorateId)
    {
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        SortOrder = sortOrder;
        IsActive = isActive;
        GovernorateId = governorateId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
