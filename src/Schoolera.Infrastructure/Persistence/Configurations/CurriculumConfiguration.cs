using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> builder)
    {
        builder.ToTable("Curricula");

        builder.HasKey(curriculum => curriculum.Id);

        builder.Property(curriculum => curriculum.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(curriculum => curriculum.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(curriculum => curriculum.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(curriculum => curriculum.DescriptionAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyDescription);

        builder.Property(curriculum => curriculum.DescriptionEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyDescription);

        builder.Property(curriculum => curriculum.CreatedAtUtc)
            .IsRequired();

        builder.Property(curriculum => curriculum.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(curriculum => curriculum.Slug)
            .IsUnique();
    }
}
