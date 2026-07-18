using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolGradeOfferingConfiguration : IEntityTypeConfiguration<SchoolGradeOffering>
{
    public void Configure(EntityTypeBuilder<SchoolGradeOffering> builder)
    {
        builder.ToTable("SchoolGradeOfferings");

        builder.HasKey(offering => offering.Id);

        builder.Property(offering => offering.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(offering => new { offering.SchoolStageOfferingId, offering.GradeId })
            .IsUnique();

        builder.HasOne(offering => offering.Grade)
            .WithMany()
            .HasForeignKey(offering => offering.GradeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
