using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolBranchConfiguration : IEntityTypeConfiguration<SchoolBranch>
{
    public void Configure(EntityTypeBuilder<SchoolBranch> builder)
    {
        builder.ToTable("SchoolBranches");

        builder.HasKey(branch => branch.Id);

        builder.Property(branch => branch.NameAr)
            .HasMaxLength(FieldLengthLimits.SchoolName)
            .IsRequired();

        builder.Property(branch => branch.NameEn)
            .HasMaxLength(FieldLengthLimits.SchoolName);

        builder.Property(branch => branch.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(branch => branch.AddressLineAr)
            .HasMaxLength(FieldLengthLimits.AddressLine);

        builder.Property(branch => branch.AddressLineEn)
            .HasMaxLength(FieldLengthLimits.AddressLine);

        builder.Property(branch => branch.BuildingNumber)
            .HasMaxLength(FieldLengthLimits.BranchCode);

        builder.Property(branch => branch.StreetName)
            .HasMaxLength(FieldLengthLimits.AddressLine);

        builder.Property(branch => branch.Landmark)
            .HasMaxLength(FieldLengthLimits.Landmark);

        builder.Property(branch => branch.PostalCode)
            .HasMaxLength(FieldLengthLimits.PostalCode);

        builder.Property(branch => branch.AddressReference)
            .HasMaxLength(FieldLengthLimits.AddressLine);

        builder.Property(branch => branch.Latitude)
            .HasPrecision(9, 6);

        builder.Property(branch => branch.Longitude)
            .HasPrecision(9, 6);

        builder.Property(branch => branch.Phone)
            .HasMaxLength(FieldLengthLimits.Phone);

        builder.Property(branch => branch.Email)
            .HasMaxLength(FieldLengthLimits.Email);

        builder.Property(branch => branch.CreatedAtUtc)
            .IsRequired();

        builder.Property(branch => branch.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(branch => new { branch.SchoolId, branch.Slug })
            .IsUnique();

        builder.HasIndex(branch => new { branch.IsActive, branch.CityId, branch.DistrictId });

        // Supports public map pin bounding-box / coarse radius filters.
        builder.HasIndex(branch => new { branch.IsActive, branch.Latitude, branch.Longitude })
            .HasDatabaseName("IX_SchoolBranches_IsActive_Latitude_Longitude");

        builder.HasOne(branch => branch.City)
            .WithMany()
            .HasForeignKey(branch => branch.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(branch => branch.District)
            .WithMany()
            .HasForeignKey(branch => branch.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(branch => branch.StageOfferings)
            .WithOne(offering => offering.SchoolBranch)
            .HasForeignKey(offering => offering.SchoolBranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(branch => branch.TuitionFees)
            .WithOne(fee => fee.SchoolBranch)
            .HasForeignKey(fee => fee.SchoolBranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
