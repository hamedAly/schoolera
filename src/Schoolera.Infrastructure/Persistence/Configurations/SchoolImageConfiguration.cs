using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolImageConfiguration : IEntityTypeConfiguration<SchoolImage>
{
    public void Configure(EntityTypeBuilder<SchoolImage> builder)
    {
        builder.ToTable("SchoolImages");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.ImageUrl)
            .HasMaxLength(FieldLengthLimits.Url)
            .IsRequired();

        builder.Property(image => image.CaptionAr)
            .HasMaxLength(FieldLengthLimits.ImageCaption);

        builder.Property(image => image.CaptionEn)
            .HasMaxLength(FieldLengthLimits.ImageCaption);

        builder.Property(image => image.AltTextAr)
            .HasMaxLength(FieldLengthLimits.ImageAlt);

        builder.Property(image => image.AltTextEn)
            .HasMaxLength(FieldLengthLimits.ImageAlt);

        builder.Property(image => image.CreatedAtUtc)
            .IsRequired();

        builder.Property(image => image.UpdatedAtUtc)
            .IsRequired();
    }
}
