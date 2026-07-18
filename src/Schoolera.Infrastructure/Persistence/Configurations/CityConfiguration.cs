using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");

        builder.HasKey(city => city.Id);

        builder.Property(city => city.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(city => city.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(city => city.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(city => city.CreatedAtUtc)
            .IsRequired();

        builder.Property(city => city.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(city => city.Slug)
            .IsUnique();

        builder.HasIndex(city => city.GovernorateId);

        builder.HasMany(city => city.Districts)
            .WithOne(district => district.City)
            .HasForeignKey(district => district.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
