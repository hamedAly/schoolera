using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Aggregate root for a school-owner educational-institution onboarding application.
/// A draft may hold incomplete data; complete validation is enforced on submit/resubmit
/// by the application layer. Status transitions are guarded by the workflow methods.
/// </summary>
public sealed class SchoolOnboardingApplication
{
    private readonly List<SchoolOnboardingDocument> _documents = [];
    private readonly List<SchoolOnboardingStatusHistory> _statusHistory = [];

    private SchoolOnboardingApplication()
    {
    }

    public SchoolOnboardingApplication(Guid ownerUserId)
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Status = SchoolOnboardingStatus.Draft;
        CurrentStep = SchoolOnboardingStep.Organization;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public SchoolOnboardingStatus Status { get; private set; }

    public SchoolOnboardingStep CurrentStep { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public DateTimeOffset? ReviewStartedAtUtc { get; private set; }

    public DateTimeOffset? ChangesRequestedAtUtc { get; private set; }

    public DateTimeOffset? LastResubmittedAtUtc { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public DateTimeOffset? RejectedAtUtc { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public Guid? ApprovedSchoolId { get; private set; }

    // Organization / legal
    public string? OrganizationNameAr { get; private set; }

    public string? OrganizationNameEn { get; private set; }

    public string? LegalName { get; private set; }

    public string? CountryCode { get; private set; }

    public string? RegistrationOrLicenseNumber { get; private set; }

    /// <summary>Uppercase, trimmed registration number used for uniqueness checks.</summary>
    public string? NormalizedRegistrationNumber { get; private set; }

    public string? TaxRegistrationNumber { get; private set; }

    public string? LegalForm { get; private set; }

    public string? OrganizationAddress { get; private set; }

    public string? OrganizationWebsite { get; private set; }

    // Authorized representative
    public string? RepresentativeFullNameAr { get; private set; }

    public string? RepresentativeFullNameEn { get; private set; }

    public string? RepresentativeNationalOrIdentityReference { get; private set; }

    public string? RepresentativeJobTitleAr { get; private set; }

    public string? RepresentativeJobTitleEn { get; private set; }

    public string? RepresentativeEmail { get; private set; }

    public string? RepresentativePhone { get; private set; }

    // Primary school details
    public string? SchoolNameAr { get; private set; }

    public string? SchoolNameEn { get; private set; }

    public SchoolType? SchoolType { get; private set; }

    public GenderType? GenderType { get; private set; }

    public int? FoundedYear { get; private set; }

    public string? SchoolShortDescriptionAr { get; private set; }

    public string? SchoolShortDescriptionEn { get; private set; }

    public string? SchoolWebsiteUrl { get; private set; }

    public string? RequestedSlug { get; private set; }

    // Primary branch / contact
    public Guid? CityId { get; private set; }

    public City? City { get; private set; }

    public Guid? DistrictId { get; private set; }

    public District? District { get; private set; }

    public string? AddressLineAr { get; private set; }

    public string? AddressLineEn { get; private set; }

    public string? BuildingNumber { get; private set; }

    public string? StreetName { get; private set; }

    public string? Landmark { get; private set; }

    public string? PostalCode { get; private set; }

    public string? LocalAddressReference { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public string? PublicPhone { get; private set; }

    public string? PublicEmail { get; private set; }

    public string? WhatsAppOrAlternatePhone { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<SchoolOnboardingDocument> Documents => _documents;

    public IReadOnlyCollection<SchoolOnboardingStatusHistory> StatusHistory => _statusHistory;

    public bool IsEditable =>
        Status is SchoolOnboardingStatus.Draft or SchoolOnboardingStatus.ChangesRequested;

    public void SaveOrganization(
        string? organizationNameAr,
        string? organizationNameEn,
        string? legalName,
        string? countryCode,
        string? registrationOrLicenseNumber,
        string? taxRegistrationNumber,
        string? legalForm,
        string? organizationAddress,
        string? organizationWebsite)
    {
        OrganizationNameAr = Clean(organizationNameAr);
        OrganizationNameEn = Clean(organizationNameEn);
        LegalName = Clean(legalName);
        CountryCode = Clean(countryCode)?.ToUpperInvariant();
        RegistrationOrLicenseNumber = Clean(registrationOrLicenseNumber);
        NormalizedRegistrationNumber = NormalizeRegistration(RegistrationOrLicenseNumber);
        TaxRegistrationNumber = Clean(taxRegistrationNumber);
        LegalForm = Clean(legalForm);
        OrganizationAddress = Clean(organizationAddress);
        OrganizationWebsite = Clean(organizationWebsite);
        Touch();
    }

    public void SaveAuthorizedRepresentative(
        string? fullNameAr,
        string? fullNameEn,
        string? nationalOrIdentityReference,
        string? jobTitleAr,
        string? jobTitleEn,
        string? email,
        string? phone)
    {
        RepresentativeFullNameAr = Clean(fullNameAr);
        RepresentativeFullNameEn = Clean(fullNameEn);
        RepresentativeNationalOrIdentityReference = Clean(nationalOrIdentityReference);
        RepresentativeJobTitleAr = Clean(jobTitleAr);
        RepresentativeJobTitleEn = Clean(jobTitleEn);
        RepresentativeEmail = Clean(email);
        RepresentativePhone = Clean(phone);
        Touch();
    }

    public void SaveSchoolDetails(
        string? schoolNameAr,
        string? schoolNameEn,
        SchoolType? schoolType,
        GenderType? genderType,
        int? foundedYear,
        string? shortDescriptionAr,
        string? shortDescriptionEn,
        string? websiteUrl,
        string? requestedSlug)
    {
        SchoolNameAr = Clean(schoolNameAr);
        SchoolNameEn = Clean(schoolNameEn);
        SchoolType = schoolType;
        GenderType = genderType;
        FoundedYear = foundedYear;
        SchoolShortDescriptionAr = Clean(shortDescriptionAr);
        SchoolShortDescriptionEn = Clean(shortDescriptionEn);
        SchoolWebsiteUrl = Clean(websiteUrl);
        RequestedSlug = Clean(requestedSlug)?.ToLowerInvariant();
        Touch();
    }

    public void SavePrimaryBranch(
        Guid? cityId,
        Guid? districtId,
        string? addressLineAr,
        string? addressLineEn,
        string? buildingNumber,
        string? streetName,
        string? landmark,
        string? postalCode,
        string? localAddressReference,
        decimal? latitude,
        decimal? longitude,
        string? publicPhone,
        string? publicEmail,
        string? whatsAppOrAlternatePhone)
    {
        CityId = cityId;
        DistrictId = districtId;
        AddressLineAr = Clean(addressLineAr);
        AddressLineEn = Clean(addressLineEn);
        BuildingNumber = Clean(buildingNumber);
        StreetName = Clean(streetName);
        Landmark = Clean(landmark);
        PostalCode = Clean(postalCode);
        LocalAddressReference = Clean(localAddressReference);
        Latitude = latitude;
        Longitude = longitude;
        PublicPhone = Clean(publicPhone);
        PublicEmail = Clean(publicEmail);
        WhatsAppOrAlternatePhone = Clean(whatsAppOrAlternatePhone);
        Touch();
    }

    /// <summary>Advances the furthest reached step marker (never moves backwards).</summary>
    public void MarkStepReached(SchoolOnboardingStep step)
    {
        if (step > CurrentStep && IsEditable)
        {
            CurrentStep = step;
            Touch();
        }
    }

    public void AddDocument(SchoolOnboardingDocument document)
    {
        _documents.Add(document);
        Touch();
    }

    public void AddStatusHistory(SchoolOnboardingStatusHistory history)
    {
        _statusHistory.Add(history);
    }

    public void Submit(DateTimeOffset now)
    {
        Status = SchoolOnboardingStatus.Submitted;
        SubmittedAtUtc = now;
        CurrentStep = SchoolOnboardingStep.Review;
        UpdatedAtUtc = now;
    }

    public void Resubmit(DateTimeOffset now)
    {
        Status = SchoolOnboardingStatus.Submitted;
        SubmittedAtUtc = now;
        LastResubmittedAtUtc = now;
        CurrentStep = SchoolOnboardingStep.Review;
        UpdatedAtUtc = now;
    }

    public void StartReview(Guid reviewerUserId, DateTimeOffset now)
    {
        Status = SchoolOnboardingStatus.UnderReview;
        ReviewStartedAtUtc = now;
        ReviewedByUserId = reviewerUserId;
        UpdatedAtUtc = now;
    }

    public void RequestChanges(Guid reviewerUserId, DateTimeOffset now)
    {
        Status = SchoolOnboardingStatus.ChangesRequested;
        ChangesRequestedAtUtc = now;
        ReviewedByUserId = reviewerUserId;
        CurrentStep = SchoolOnboardingStep.Organization;
        UpdatedAtUtc = now;
    }

    public void Approve(Guid reviewerUserId, Guid approvedSchoolId, DateTimeOffset now)
    {
        Status = SchoolOnboardingStatus.Approved;
        ApprovedAtUtc = now;
        ApprovedSchoolId = approvedSchoolId;
        ReviewedByUserId = reviewerUserId;
        UpdatedAtUtc = now;
    }

    public void Reject(Guid reviewerUserId, DateTimeOffset now)
    {
        Status = SchoolOnboardingStatus.Rejected;
        RejectedAtUtc = now;
        ReviewedByUserId = reviewerUserId;
        UpdatedAtUtc = now;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeRegistration(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
}
