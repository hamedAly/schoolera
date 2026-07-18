using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(country => country.Id);

        builder.Property(country => country.Code)
            .HasMaxLength(FieldLengthLimits.CountryCode)
            .IsRequired();

        builder.Property(country => country.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(country => country.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(country => country.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(country => country.CreatedAtUtc)
            .IsRequired();

        builder.Property(country => country.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(country => country.Code)
            .IsUnique();

        builder.HasIndex(country => country.Slug)
            .IsUnique();

        builder.HasMany(country => country.Governorates)
            .WithOne(governorate => governorate.Country)
            .HasForeignKey(governorate => governorate.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
