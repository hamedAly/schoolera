using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("Facilities");

        builder.HasKey(facility => facility.Id);

        builder.Property(facility => facility.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(facility => facility.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(facility => facility.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(facility => facility.IconKey)
            .HasMaxLength(FieldLengthLimits.IconKey);

        builder.Property(facility => facility.CreatedAtUtc)
            .IsRequired();

        builder.Property(facility => facility.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(facility => facility.Slug)
            .IsUnique();
    }
}
