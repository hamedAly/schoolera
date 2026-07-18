using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Parent-specific profile fields not already stored on ApplicationUser
/// (FirstName, LastName, PhoneNumber, PreferredLanguage remain on Identity).
/// Includes two fixed guardian sections (Father/FirstGuardian and Mother/SecondGuardian).
/// </summary>
public sealed class ParentProfile
{
    private ParentProfile()
    {
    }

    public ParentProfile(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PreferredContactMethod = PreferredContactMethod.Phone;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string? AlternatePhone { get; private set; }

    public string? AddressLine { get; private set; }

    public string? Qualification { get; private set; }

    public string? Occupation { get; private set; }

    public Guid? CountryId { get; private set; }

    public Country? Country { get; private set; }

    public Guid? GovernorateId { get; private set; }

    public Governorate? Governorate { get; private set; }

    public Guid? CityId { get; private set; }

    public City? City { get; private set; }

    public Guid? DistrictId { get; private set; }

    public District? District { get; private set; }

    public PreferredContactMethod PreferredContactMethod { get; private set; }

    // Father / first guardian
    public string? FatherFullName { get; private set; }

    public string? FatherPhone { get; private set; }

    public string? FatherEmail { get; private set; }

    public string? FatherOccupation { get; private set; }

    public string? FatherQualification { get; private set; }

    public ChildIdentityType? FatherIdentityType { get; private set; }

    public string? FatherProtectedIdentityValue { get; private set; }

    public string? FatherIdentityLookupHash { get; private set; }

    public string? FatherIdentityLastFour { get; private set; }

    // Mother / second guardian
    public string? MotherFullName { get; private set; }

    public string? MotherPhone { get; private set; }

    public string? MotherEmail { get; private set; }

    public string? MotherOccupation { get; private set; }

    public string? MotherQualification { get; private set; }

    public ChildIdentityType? MotherIdentityType { get; private set; }

    public string? MotherProtectedIdentityValue { get; private set; }

    public string? MotherIdentityLookupHash { get; private set; }

    public string? MotherIdentityLastFour { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<ChildProfile> Children { get; private set; } = [];

    public void Update(
        string? alternatePhone,
        string? addressLine,
        string? qualification,
        string? occupation,
        Guid? countryId,
        Guid? governorateId,
        Guid? cityId,
        Guid? districtId,
        PreferredContactMethod preferredContactMethod,
        GuardianProfileUpdate father,
        GuardianProfileUpdate mother)
    {
        AlternatePhone = NormalizeOptional(alternatePhone);
        AddressLine = NormalizeOptional(addressLine);
        Qualification = NormalizeOptional(qualification);
        Occupation = NormalizeOptional(occupation);
        CountryId = countryId;
        GovernorateId = governorateId;
        CityId = cityId;
        DistrictId = districtId;
        PreferredContactMethod = preferredContactMethod;
        ApplyGuardian(isFather: true, father);
        ApplyGuardian(isFather: false, mother);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ReplaceFatherIdentity(
        ChildIdentityType identityType,
        string protectedIdentityValue,
        string identityLookupHash,
        string identityLastFour)
    {
        FatherIdentityType = identityType;
        FatherProtectedIdentityValue = protectedIdentityValue;
        FatherIdentityLookupHash = identityLookupHash;
        FatherIdentityLastFour = identityLastFour;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ReplaceMotherIdentity(
        ChildIdentityType identityType,
        string protectedIdentityValue,
        string identityLookupHash,
        string identityLastFour)
    {
        MotherIdentityType = identityType;
        MotherProtectedIdentityValue = protectedIdentityValue;
        MotherIdentityLookupHash = identityLookupHash;
        MotherIdentityLastFour = identityLastFour;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void ApplyGuardian(bool isFather, GuardianProfileUpdate guardian)
    {
        if (isFather)
        {
            FatherFullName = NormalizeOptional(guardian.FullName);
            FatherPhone = NormalizeOptional(guardian.Phone);
            FatherEmail = NormalizeOptional(guardian.Email);
            FatherOccupation = NormalizeOptional(guardian.Occupation);
            FatherQualification = NormalizeOptional(guardian.Qualification);
            return;
        }

        MotherFullName = NormalizeOptional(guardian.FullName);
        MotherPhone = NormalizeOptional(guardian.Phone);
        MotherEmail = NormalizeOptional(guardian.Email);
        MotherOccupation = NormalizeOptional(guardian.Occupation);
        MotherQualification = NormalizeOptional(guardian.Qualification);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Non-identity guardian fields applied on parent profile update.</summary>
public sealed record GuardianProfileUpdate(
    string? FullName,
    string? Phone,
    string? Email,
    string? Occupation,
    string? Qualification);
