using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("Schools");

        builder.HasKey(school => school.Id);

        builder.Property(school => school.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(school => school.City)
            .HasMaxLength(100);

        builder.Property(school => school.CreatedAtUtc)
            .IsRequired();
    }
}