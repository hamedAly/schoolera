using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolOnboardingApplicationConfiguration
    : IEntityTypeConfiguration<SchoolOnboardingApplication>
{
    public void Configure(EntityTypeBuilder<SchoolOnboardingApplication> builder)
    {
        builder.ToTable("SchoolOnboardingApplications");

        builder.HasKey(application => application.Id);

        builder.Property(application => application.OwnerUserId).IsRequired();

        builder.Property(application => application.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(application => application.CurrentStep)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(application => application.CreatedAtUtc).IsRequired();
        builder.Property(application => application.UpdatedAtUtc).IsRequired();

        builder.Property(application => application.RowVersion)
            .IsRowVersion();

        // Organization / legal
        builder.Property(a => a.OrganizationNameAr).HasMaxLength(FieldLengthLimits.OrganizationName);
        builder.Property(a => a.OrganizationNameEn).HasMaxLength(FieldLengthLimits.OrganizationName);
        builder.Property(a => a.LegalName).HasMaxLength(FieldLengthLimits.LegalName);
        builder.Property(a => a.CountryCode).HasMaxLength(FieldLengthLimits.CountryCode);
        builder.Property(a => a.RegistrationOrLicenseNumber).HasMaxLength(FieldLengthLimits.RegistrationNumber);
        builder.Property(a => a.NormalizedRegistrationNumber).HasMaxLength(FieldLengthLimits.RegistrationNumber);
        builder.Property(a => a.TaxRegistrationNumber).HasMaxLength(FieldLengthLimits.TaxNumber);
        builder.Property(a => a.LegalForm).HasMaxLength(FieldLengthLimits.LegalForm);
        builder.Property(a => a.OrganizationAddress).HasMaxLength(FieldLengthLimits.AddressLine);
        builder.Property(a => a.OrganizationWebsite).HasMaxLength(FieldLengthLimits.Url);

        // Authorized representative
        builder.Property(a => a.RepresentativeFullNameAr).HasMaxLength(FieldLengthLimits.PersonName);
        builder.Property(a => a.RepresentativeFullNameEn).HasMaxLength(FieldLengthLimits.PersonName);
        builder.Property(a => a.RepresentativeNationalOrIdentityReference)
            .HasMaxLength(FieldLengthLimits.IdentityReference);
        builder.Property(a => a.RepresentativeJobTitleAr).HasMaxLength(FieldLengthLimits.JobTitle);
        builder.Property(a => a.RepresentativeJobTitleEn).HasMaxLength(FieldLengthLimits.JobTitle);
        builder.Property(a => a.RepresentativeEmail).HasMaxLength(FieldLengthLimits.Email);
        builder.Property(a => a.RepresentativePhone).HasMaxLength(FieldLengthLimits.Phone);

        // Primary school details
        builder.Property(a => a.SchoolNameAr).HasMaxLength(FieldLengthLimits.SchoolName);
        builder.Property(a => a.SchoolNameEn).HasMaxLength(FieldLengthLimits.SchoolName);
        builder.Property(a => a.SchoolType).HasConversion<int>();
        builder.Property(a => a.GenderType).HasConversion<int>();
        builder.Property(a => a.SchoolShortDescriptionAr).HasMaxLength(FieldLengthLimits.TaxonomyDescription);
        builder.Property(a => a.SchoolShortDescriptionEn).HasMaxLength(FieldLengthLimits.TaxonomyDescription);
        builder.Property(a => a.SchoolWebsiteUrl).HasMaxLength(FieldLengthLimits.Url);
        builder.Property(a => a.RequestedSlug).HasMaxLength(FieldLengthLimits.Slug);

        // Primary branch / contact
        builder.Property(a => a.AddressLineAr).HasMaxLength(FieldLengthLimits.AddressLine);
        builder.Property(a => a.AddressLineEn).HasMaxLength(FieldLengthLimits.AddressLine);
        builder.Property(a => a.BuildingNumber).HasMaxLength(FieldLengthLimits.BranchCode);
        builder.Property(a => a.StreetName).HasMaxLength(FieldLengthLimits.AddressLine);
        builder.Property(a => a.Landmark).HasMaxLength(FieldLengthLimits.Landmark);
        builder.Property(a => a.PostalCode).HasMaxLength(FieldLengthLimits.PostalCode);
        builder.Property(a => a.LocalAddressReference).HasMaxLength(FieldLengthLimits.AddressLine);
        builder.Property(a => a.Latitude).HasPrecision(9, 6);
        builder.Property(a => a.Longitude).HasPrecision(9, 6);
        builder.Property(a => a.PublicPhone).HasMaxLength(FieldLengthLimits.Phone);
        builder.Property(a => a.PublicEmail).HasMaxLength(FieldLengthLimits.Email);
        builder.Property(a => a.WhatsAppOrAlternatePhone).HasMaxLength(FieldLengthLimits.Phone);

        // One active application per owner (Draft/Submitted/UnderReview/ChangesRequested => Status < 5).
        builder.HasIndex(application => application.OwnerUserId)
            .IsUnique()
            .HasFilter("[Status] < 5")
            .HasDatabaseName("IX_SchoolOnboardingApplications_OwnerUserId_Active");

        builder.HasIndex(application => application.Status);

        // Optional unique registration/license per country while not rejected (Status <> 6).
        builder.HasIndex(a => new { a.CountryCode, a.NormalizedRegistrationNumber })
            .IsUnique()
            .HasFilter("[NormalizedRegistrationNumber] IS NOT NULL AND [Status] <> 6")
            .HasDatabaseName("IX_SchoolOnboardingApplications_Registration_Active");

        // One onboarding application links to at most one approved school.
        builder.HasIndex(a => a.ApprovedSchoolId)
            .IsUnique()
            .HasFilter("[ApprovedSchoolId] IS NOT NULL");

        builder.HasOne(a => a.City)
            .WithMany()
            .HasForeignKey(a => a.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.District)
            .WithMany()
            .HasForeignKey(a => a.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(a => a.ApprovedSchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Documents)
            .WithOne(document => document.Application)
            .HasForeignKey(document => document.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.StatusHistory)
            .WithOne(history => history.Application)
            .HasForeignKey(history => history.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(SchoolOnboardingApplication.Documents))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(SchoolOnboardingApplication.StatusHistory))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
