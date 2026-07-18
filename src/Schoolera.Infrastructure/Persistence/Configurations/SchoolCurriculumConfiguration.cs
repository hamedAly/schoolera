using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolCurriculumConfiguration : IEntityTypeConfiguration<SchoolCurriculum>
{
    public void Configure(EntityTypeBuilder<SchoolCurriculum> builder)
    {
        builder.ToTable("SchoolCurricula");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(link => new { link.SchoolId, link.CurriculumId })
            .IsUnique();

        builder.HasOne(link => link.Curriculum)
            .WithMany()
            .HasForeignKey(link => link.CurriculumId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
