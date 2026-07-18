using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolAdditionalServiceConfiguration : IEntityTypeConfiguration<SchoolAdditionalService>
{
    public void Configure(EntityTypeBuilder<SchoolAdditionalService> builder)
    {
        builder.ToTable("SchoolAdditionalServices");

        builder.HasKey(service => service.Id);

        builder.Property(service => service.NameAr)
            .HasMaxLength(FieldLengthLimits.ServiceName)
            .IsRequired();

        builder.Property(service => service.NameEn)
            .HasMaxLength(FieldLengthLimits.ServiceName);

        builder.Property(service => service.DescriptionAr)
            .HasMaxLength(FieldLengthLimits.ServiceDescription);

        builder.Property(service => service.DescriptionEn)
            .HasMaxLength(FieldLengthLimits.ServiceDescription);

        builder.Property(service => service.IconKey)
            .HasMaxLength(FieldLengthLimits.IconKey);

        builder.Property(service => service.SortOrder)
            .IsRequired();

        builder.Property(service => service.IsActive)
            .IsRequired();

        builder.Property(service => service.CreatedAtUtc)
            .IsRequired();

        builder.Property(service => service.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(service => new { service.SchoolId, service.SortOrder });

        builder.HasIndex(service => new { service.SchoolId, service.IsActive });
    }
}
