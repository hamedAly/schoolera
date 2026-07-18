using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolOnboardingDocumentTypeConfiguration
    : IEntityTypeConfiguration<SchoolOnboardingDocumentType>
{
    public void Configure(EntityTypeBuilder<SchoolOnboardingDocumentType> builder)
    {
        builder.ToTable("SchoolOnboardingDocumentTypes");

        builder.HasKey(type => type.Id);

        builder.Property(type => type.Code)
            .HasMaxLength(FieldLengthLimits.OnboardingDocumentTypeCode)
            .IsRequired();

        builder.Property(type => type.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(type => type.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(type => type.IsRequired).IsRequired();
        builder.Property(type => type.IsActive).IsRequired();
        builder.Property(type => type.SortOrder).IsRequired();
        builder.Property(type => type.CreatedAtUtc).IsRequired();
        builder.Property(type => type.UpdatedAtUtc).IsRequired();

        builder.HasIndex(type => type.Code).IsUnique();
    }
}
