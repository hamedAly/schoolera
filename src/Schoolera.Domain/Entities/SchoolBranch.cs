namespace Schoolera.Domain.Entities;

public sealed class SchoolBranch
{
    private SchoolBranch()
    {
    }

    public SchoolBranch(
        Guid schoolId,
        string nameAr,
        string? nameEn,
        string slug,
        Guid cityId,
        Guid districtId,
        bool isMainBranch)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Slug = slug;
        CityId = cityId;
        DistrictId = districtId;
        IsMainBranch = isMainBranch;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public Guid CityId { get; private set; }

    public City City { get; private set; } = null!;

    public Guid DistrictId { get; private set; }

    public District District { get; private set; } = null!;

    public string? AddressLineAr { get; private set; }

    public string? AddressLineEn { get; private set; }

    public string? BuildingNumber { get; private set; }

    public string? StreetName { get; private set; }

    public string? Landmark { get; private set; }

    public string? PostalCode { get; private set; }

    public string? AddressReference { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public bool IsMainBranch { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<SchoolStageOffering> StageOfferings { get; private set; } = [];

    public ICollection<TuitionFee> TuitionFees { get; private set; } = [];

    public void UpdateAddress(
        string? addressLineAr,
        string? addressLineEn,
        string? buildingNumber,
        string? streetName,
        string? landmark,
        string? postalCode,
        string? addressReference,
        decimal? latitude,
        decimal? longitude)
    {
        AddressLineAr = addressLineAr;
        AddressLineEn = addressLineEn;
        BuildingNumber = buildingNumber;
        StreetName = streetName;
        Landmark = landmark;
        PostalCode = postalCode;
        AddressReference = addressReference;
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateContact(string? phone, string? email)
    {
        Phone = phone;
        Email = email;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateIdentity(string nameAr, string? nameEn, Guid cityId, Guid districtId)
    {
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        CityId = cityId;
        DistrictId = districtId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetMainBranch(bool isMainBranch)
    {
        IsMainBranch = isMainBranch;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
