using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("Districts");

        builder.HasKey(district => district.Id);

        builder.Property(district => district.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(district => district.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(district => district.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(district => district.CreatedAtUtc)
            .IsRequired();

        builder.Property(district => district.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(district => new { district.CityId, district.Slug })
            .IsUnique();
    }
}
