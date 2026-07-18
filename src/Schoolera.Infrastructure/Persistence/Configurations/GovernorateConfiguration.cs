using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class GovernorateConfiguration : IEntityTypeConfiguration<Governorate>
{
    public void Configure(EntityTypeBuilder<Governorate> builder)
    {
        builder.ToTable("Governorates");

        builder.HasKey(governorate => governorate.Id);

        builder.Property(governorate => governorate.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(governorate => governorate.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(governorate => governorate.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(governorate => governorate.CreatedAtUtc)
            .IsRequired();

        builder.Property(governorate => governorate.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(governorate => new { governorate.CountryId, governorate.Slug })
            .IsUnique();

        builder.HasMany(governorate => governorate.Cities)
            .WithOne(city => city.Governorate)
            .HasForeignKey(city => city.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
