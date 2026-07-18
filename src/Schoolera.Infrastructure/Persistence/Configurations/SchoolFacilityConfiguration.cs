using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolFacilityConfiguration : IEntityTypeConfiguration<SchoolFacility>
{
    public void Configure(EntityTypeBuilder<SchoolFacility> builder)
    {
        builder.ToTable("SchoolFacilities");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(link => new { link.SchoolId, link.FacilityId })
            .IsUnique();

        builder.HasOne(link => link.Facility)
            .WithMany()
            .HasForeignKey(link => link.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
