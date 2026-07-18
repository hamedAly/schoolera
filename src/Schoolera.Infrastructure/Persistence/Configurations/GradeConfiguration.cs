using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades");

        builder.HasKey(grade => grade.Id);

        builder.Property(grade => grade.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(grade => grade.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(grade => grade.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(grade => grade.CreatedAtUtc)
            .IsRequired();

        builder.Property(grade => grade.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(grade => new { grade.EducationalStageId, grade.Slug })
            .IsUnique();
    }
}
