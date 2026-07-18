using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolStageOfferingConfiguration : IEntityTypeConfiguration<SchoolStageOffering>
{
    public void Configure(EntityTypeBuilder<SchoolStageOffering> builder)
    {
        builder.ToTable("SchoolStageOfferings");

        builder.HasKey(offering => offering.Id);

        builder.Property(offering => offering.GenderType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(offering => offering.CreatedAtUtc)
            .IsRequired();

        builder.Property(offering => offering.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(offering => new
            {
                offering.SchoolBranchId,
                offering.EducationalStageId,
                offering.GenderType,
            })
            .IsUnique();

        builder.HasOne(offering => offering.EducationalStage)
            .WithMany()
            .HasForeignKey(offering => offering.EducationalStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(offering => offering.GradeOfferings)
            .WithOne(gradeOffering => gradeOffering.SchoolStageOffering)
            .HasForeignKey(gradeOffering => gradeOffering.SchoolStageOfferingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
