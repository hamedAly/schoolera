using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class ParentProfileConfiguration : IEntityTypeConfiguration<ParentProfile>
{
    public void Configure(EntityTypeBuilder<ParentProfile> builder)
    {
        builder.ToTable("ParentProfiles");
        builder.HasKey(profile => profile.Id);

        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.Property(profile => profile.AlternatePhone).HasMaxLength(FieldLengthLimits.Phone);
        builder.Property(profile => profile.AddressLine).HasMaxLength(FieldLengthLimits.AddressLine);
        builder.Property(profile => profile.Qualification).HasMaxLength(FieldLengthLimits.ParentQualification);
        builder.Property(profile => profile.Occupation).HasMaxLength(FieldLengthLimits.ParentOccupation);
        builder.Property(profile => profile.PreferredContactMethod).HasConversion<int>();

        ConfigureGuardian(builder, isFather: true);
        ConfigureGuardian(builder, isFather: false);

        builder.HasOne(profile => profile.Country)
            .WithMany()
            .HasForeignKey(profile => profile.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profile => profile.Governorate)
            .WithMany()
            .HasForeignKey(profile => profile.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profile => profile.City)
            .WithMany()
            .HasForeignKey(profile => profile.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profile => profile.District)
            .WithMany()
            .HasForeignKey(profile => profile.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(profile => profile.Children)
            .WithOne(child => child.ParentProfile)
            .HasForeignKey(child => child.ParentProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureGuardian(EntityTypeBuilder<ParentProfile> builder, bool isFather)
    {
        if (isFather)
        {
            builder.Property(profile => profile.FatherFullName).HasMaxLength(FieldLengthLimits.PersonName);
            builder.Property(profile => profile.FatherPhone).HasMaxLength(FieldLengthLimits.Phone);
            builder.Property(profile => profile.FatherEmail).HasMaxLength(FieldLengthLimits.Email);
            builder.Property(profile => profile.FatherOccupation).HasMaxLength(FieldLengthLimits.ParentOccupation);
            builder.Property(profile => profile.FatherQualification).HasMaxLength(FieldLengthLimits.ParentQualification);
            builder.Property(profile => profile.FatherIdentityType).HasConversion<int>();
            builder.Property(profile => profile.FatherProtectedIdentityValue)
                .HasMaxLength(FieldLengthLimits.ChildIdentityProtected);
            builder.Property(profile => profile.FatherIdentityLookupHash)
                .HasMaxLength(FieldLengthLimits.Sha256Hex);
            builder.Property(profile => profile.FatherIdentityLastFour)
                .HasMaxLength(FieldLengthLimits.ChildIdentityLastFour);
            return;
        }

        builder.Property(profile => profile.MotherFullName).HasMaxLength(FieldLengthLimits.PersonName);
        builder.Property(profile => profile.MotherPhone).HasMaxLength(FieldLengthLimits.Phone);
        builder.Property(profile => profile.MotherEmail).HasMaxLength(FieldLengthLimits.Email);
        builder.Property(profile => profile.MotherOccupation).HasMaxLength(FieldLengthLimits.ParentOccupation);
        builder.Property(profile => profile.MotherQualification).HasMaxLength(FieldLengthLimits.ParentQualification);
        builder.Property(profile => profile.MotherIdentityType).HasConversion<int>();
        builder.Property(profile => profile.MotherProtectedIdentityValue)
            .HasMaxLength(FieldLengthLimits.ChildIdentityProtected);
        builder.Property(profile => profile.MotherIdentityLookupHash)
            .HasMaxLength(FieldLengthLimits.Sha256Hex);
        builder.Property(profile => profile.MotherIdentityLastFour)
            .HasMaxLength(FieldLengthLimits.ChildIdentityLastFour);
    }
}

public sealed class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        builder.ToTable("ChildProfiles");
        builder.HasKey(child => child.Id);

        builder.Property(child => child.FullName).HasMaxLength(FieldLengthLimits.PersonName).IsRequired();
        builder.Property(child => child.IdentityType).HasConversion<int>();
        builder.Property(child => child.ProtectedIdentityValue)
            .HasMaxLength(FieldLengthLimits.ChildIdentityProtected)
            .IsRequired();
        builder.Property(child => child.IdentityLookupHash)
            .HasMaxLength(FieldLengthLimits.Sha256Hex)
            .IsRequired();
        builder.Property(child => child.IdentityLastFour)
            .HasMaxLength(FieldLengthLimits.ChildIdentityLastFour)
            .IsRequired();
        builder.Property(child => child.Gender).HasConversion<int>();
        builder.Property(child => child.PreferredStudyLanguage).HasConversion<int>();
        builder.Property(child => child.CurrentSchoolName).HasMaxLength(FieldLengthLimits.ChildCurrentSchoolName);
        builder.Property(child => child.Skills).HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(child => child.Hobbies).HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(child => child.Strengths).HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(child => child.ImprovementAreas).HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(child => child.SpecialNeedsNotes).HasMaxLength(FieldLengthLimits.Notes);
        builder.Property(child => child.HealthNotes).HasMaxLength(FieldLengthLimits.Notes);

        // Within-parent uniqueness only — system-wide uniqueness is not a documented legal requirement.
        builder.HasIndex(child => new { child.ParentUserId, child.IdentityLookupHash }).IsUnique();
        builder.HasIndex(child => new { child.ParentUserId, child.IsActive });

        builder.HasOne(child => child.CurrentGrade)
            .WithMany()
            .HasForeignKey(child => child.CurrentGradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(child => child.Documents)
            .WithOne(document => document.ChildProfile)
            .HasForeignKey(document => document.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ChildDocumentConfiguration : IEntityTypeConfiguration<ChildDocument>
{
    public void Configure(EntityTypeBuilder<ChildDocument> builder)
    {
        builder.ToTable("ChildDocuments");
        builder.HasKey(document => document.Id);

        builder.Property(document => document.DocumentType).HasConversion<int>().IsRequired();
        builder.Property(document => document.OriginalFileName)
            .HasMaxLength(FieldLengthLimits.OnboardingFileName)
            .IsRequired();
        builder.Property(document => document.ContentType)
            .HasMaxLength(FieldLengthLimits.OnboardingContentType)
            .IsRequired();
        builder.Property(document => document.StorageKey)
            .HasMaxLength(FieldLengthLimits.OnboardingStoredReference)
            .IsRequired();
        builder.Property(document => document.FileSizeBytes).IsRequired();
        builder.Property(document => document.UploadedByUserId).IsRequired();
        builder.Property(document => document.CreatedAtUtc).IsRequired();
        builder.Property(document => document.UpdatedAtUtc).IsRequired();

        builder.HasIndex(document => new { document.ChildProfileId, document.DocumentType });
        builder.HasIndex(document => new { document.ParentUserId, document.ChildProfileId });
    }
}
