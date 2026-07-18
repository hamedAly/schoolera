using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class EducationalStageConfiguration : IEntityTypeConfiguration<EducationalStage>
{
    public void Configure(EntityTypeBuilder<EducationalStage> builder)
    {
        builder.ToTable("EducationalStages");

        builder.HasKey(stage => stage.Id);

        builder.Property(stage => stage.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(stage => stage.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(stage => stage.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(stage => stage.CreatedAtUtc)
            .IsRequired();

        builder.Property(stage => stage.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(stage => stage.Slug)
            .IsUnique();

        builder.HasMany(stage => stage.Grades)
            .WithOne(grade => grade.EducationalStage)
            .HasForeignKey(grade => grade.EducationalStageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
