using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("AcademicYears");

        builder.HasKey(year => year.Id);

        builder.Property(year => year.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(year => year.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(year => year.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(year => year.StartDate)
            .IsRequired();

        builder.Property(year => year.EndDate)
            .IsRequired();

        builder.Property(year => year.CreatedAtUtc)
            .IsRequired();

        builder.Property(year => year.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(year => year.Slug)
            .IsUnique();
    }
}
