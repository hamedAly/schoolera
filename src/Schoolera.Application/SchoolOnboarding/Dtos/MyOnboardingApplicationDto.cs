using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolOnboarding.Dtos;

/// <summary>
/// Owner-facing read model of the onboarding application. Contains only owner-visible data
/// (no internal admin notes) plus the derived completion state used by the wizard.
/// </summary>
public sealed record MyOnboardingApplicationDto
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }

    public required string CurrentStep { get; init; }

    // Organization / legal
    public string? OrganizationNameAr { get; init; }
    public string? OrganizationNameEn { get; init; }
    public string? LegalName { get; init; }
    public string? CountryCode { get; init; }
    public string? RegistrationOrLicenseNumber { get; init; }
    public string? TaxRegistrationNumber { get; init; }
    public string? LegalForm { get; init; }
    public string? OrganizationAddress { get; init; }
    public string? OrganizationWebsite { get; init; }

    // Authorized representative
    public string? RepresentativeFullNameAr { get; init; }
    public string? RepresentativeFullNameEn { get; init; }
    public string? RepresentativeNationalOrIdentityReference { get; init; }
    public string? RepresentativeJobTitleAr { get; init; }
    public string? RepresentativeJobTitleEn { get; init; }
    public string? RepresentativeEmail { get; init; }
    public string? RepresentativePhone { get; init; }

    // Primary school
    public string? SchoolNameAr { get; init; }
    public string? SchoolNameEn { get; init; }
    public SchoolType? SchoolType { get; init; }
    public GenderType? GenderType { get; init; }
    public int? FoundedYear { get; init; }
    public string? SchoolShortDescriptionAr { get; init; }
    public string? SchoolShortDescriptionEn { get; init; }
    public string? SchoolWebsiteUrl { get; init; }
    public string? RequestedSlug { get; init; }

    // Primary branch / contact
    public Guid? CityId { get; init; }
    public Guid? DistrictId { get; init; }
    public string? AddressLineAr { get; init; }
    public string? AddressLineEn { get; init; }
    public string? BuildingNumber { get; init; }
    public string? StreetName { get; init; }
    public string? Landmark { get; init; }
    public string? PostalCode { get; init; }
    public string? LocalAddressReference { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? PublicPhone { get; init; }
    public string? PublicEmail { get; init; }
    public string? WhatsAppOrAlternatePhone { get; init; }

    public required IReadOnlyList<OnboardingDocumentDto> Documents { get; init; }

    public required IReadOnlyList<OnboardingStatusHistoryDto> StatusHistory { get; init; }

    public string? OwnerVisibleReason { get; init; }

    public Guid? ApprovedSchoolId { get; init; }

    public string? ApprovedSchoolName { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }

    public DateTimeOffset? SubmittedAtUtc { get; init; }

    // Derived completion state
    public required bool OrganizationComplete { get; init; }
    public required bool RepresentativeComplete { get; init; }
    public required bool SchoolComplete { get; init; }
    public required bool BranchComplete { get; init; }
    public required bool DocumentsComplete { get; init; }
    public required IReadOnlyList<Guid> MissingRequiredDocumentTypeIds { get; init; }

    public static MyOnboardingApplicationDto FromEntity(
        SchoolOnboardingApplication a,
        IReadOnlyList<Guid> requiredDocumentTypeIds,
        string? approvedSchoolName)
    {
        var currentDocs = a.Documents.Where(d => d.IsCurrent).ToArray();
        var currentDocTypeIds = currentDocs.Select(d => d.DocumentTypeId).ToArray();
        var missing = OnboardingCompleteness.MissingRequiredDocumentTypeIds(
            requiredDocumentTypeIds,
            currentDocTypeIds);

        var latestReason = a.StatusHistory
            .Where(h => h.NewStatus is SchoolOnboardingStatus.ChangesRequested or SchoolOnboardingStatus.Rejected)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Select(h => h.OwnerVisibleReason)
            .FirstOrDefault();

        return new MyOnboardingApplicationDto
        {
            Id = a.Id,
            Status = a.Status.ToString(),
            CurrentStep = a.CurrentStep.ToString(),
            OrganizationNameAr = a.OrganizationNameAr,
            OrganizationNameEn = a.OrganizationNameEn,
            LegalName = a.LegalName,
            CountryCode = a.CountryCode,
            RegistrationOrLicenseNumber = a.RegistrationOrLicenseNumber,
            TaxRegistrationNumber = a.TaxRegistrationNumber,
            LegalForm = a.LegalForm,
            OrganizationAddress = a.OrganizationAddress,
            OrganizationWebsite = a.OrganizationWebsite,
            RepresentativeFullNameAr = a.RepresentativeFullNameAr,
            RepresentativeFullNameEn = a.RepresentativeFullNameEn,
            RepresentativeNationalOrIdentityReference = a.RepresentativeNationalOrIdentityReference,
            RepresentativeJobTitleAr = a.RepresentativeJobTitleAr,
            RepresentativeJobTitleEn = a.RepresentativeJobTitleEn,
            RepresentativeEmail = a.RepresentativeEmail,
            RepresentativePhone = a.RepresentativePhone,
            SchoolNameAr = a.SchoolNameAr,
            SchoolNameEn = a.SchoolNameEn,
            SchoolType = a.SchoolType,
            GenderType = a.GenderType,
            FoundedYear = a.FoundedYear,
            SchoolShortDescriptionAr = a.SchoolShortDescriptionAr,
            SchoolShortDescriptionEn = a.SchoolShortDescriptionEn,
            SchoolWebsiteUrl = a.SchoolWebsiteUrl,
            RequestedSlug = a.RequestedSlug,
            CityId = a.CityId,
            DistrictId = a.DistrictId,
            AddressLineAr = a.AddressLineAr,
            AddressLineEn = a.AddressLineEn,
            BuildingNumber = a.BuildingNumber,
            StreetName = a.StreetName,
            Landmark = a.Landmark,
            PostalCode = a.PostalCode,
            LocalAddressReference = a.LocalAddressReference,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            PublicPhone = a.PublicPhone,
            PublicEmail = a.PublicEmail,
            WhatsAppOrAlternatePhone = a.WhatsAppOrAlternatePhone,
            Documents = currentDocs.Select(OnboardingDocumentDto.FromEntity).ToArray(),
            StatusHistory = a.StatusHistory
                .OrderBy(h => h.CreatedAtUtc)
                .Select(OnboardingStatusHistoryDto.FromEntity)
                .ToArray(),
            OwnerVisibleReason = latestReason,
            ApprovedSchoolId = a.ApprovedSchoolId,
            ApprovedSchoolName = approvedSchoolName,
            CreatedAtUtc = a.CreatedAtUtc,
            UpdatedAtUtc = a.UpdatedAtUtc,
            SubmittedAtUtc = a.SubmittedAtUtc,
            OrganizationComplete = OnboardingCompleteness.IsOrganizationComplete(a),
            RepresentativeComplete = OnboardingCompleteness.IsRepresentativeComplete(a),
            SchoolComplete = OnboardingCompleteness.IsSchoolComplete(a),
            BranchComplete = OnboardingCompleteness.IsBranchComplete(a),
            DocumentsComplete = missing.Count == 0,
            MissingRequiredDocumentTypeIds = missing,
        };
    }
}
