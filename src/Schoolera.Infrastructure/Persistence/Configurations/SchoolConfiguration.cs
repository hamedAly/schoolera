using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("Schools");

        builder.HasKey(school => school.Id);

        builder.Property(school => school.NameAr)
            .HasMaxLength(FieldLengthLimits.SchoolName)
            .IsRequired();

        builder.Property(school => school.NameEn)
            .HasMaxLength(FieldLengthLimits.SchoolName);

        builder.Property(school => school.Slug)
            .HasMaxLength(FieldLengthLimits.Slug)
            .IsRequired();

        builder.Property(school => school.ShortDescriptionAr)
            .HasMaxLength(FieldLengthLimits.ShortDescription);

        builder.Property(school => school.ShortDescriptionEn)
            .HasMaxLength(FieldLengthLimits.ShortDescription);

        builder.Property(school => school.FullDescriptionAr)
            .HasMaxLength(FieldLengthLimits.FullDescription);

        builder.Property(school => school.FullDescriptionEn)
            .HasMaxLength(FieldLengthLimits.FullDescription);

        builder.Property(school => school.LogoUrl)
            .HasMaxLength(FieldLengthLimits.Url);

        builder.Property(school => school.CoverUrl)
            .HasMaxLength(FieldLengthLimits.Url);

        builder.Property(school => school.SchoolType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(school => school.GenderType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(school => school.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(school => school.PublicPhone)
            .HasMaxLength(FieldLengthLimits.Phone);

        builder.Property(school => school.PublicEmail)
            .HasMaxLength(FieldLengthLimits.Email);

        builder.Property(school => school.WebsiteUrl)
            .HasMaxLength(FieldLengthLimits.Url);

        builder.Property(school => school.WhatsAppNumber)
            .HasMaxLength(FieldLengthLimits.Phone);

        builder.Property(school => school.SeoTitleAr)
            .HasMaxLength(FieldLengthLimits.SeoTitle);

        builder.Property(school => school.SeoTitleEn)
            .HasMaxLength(FieldLengthLimits.SeoTitle);

        builder.Property(school => school.SeoDescriptionAr)
            .HasMaxLength(FieldLengthLimits.SeoDescription);

        builder.Property(school => school.SeoDescriptionEn)
            .HasMaxLength(FieldLengthLimits.SeoDescription);

        builder.Property(school => school.FeeVisibilityPolicy)
            .HasConversion<int?>();

        builder.Property(school => school.CreatedAtUtc)
            .IsRequired();

        builder.Property(school => school.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(school => school.Slug)
            .IsUnique();

        builder.HasIndex(school => school.NameAr);

        builder.HasIndex(school => new { school.Status, school.CreatedAtUtc });

        builder.HasIndex(school => new { school.Status, school.NameAr });

        builder.HasMany(school => school.Branches)
            .WithOne(branch => branch.School)
            .HasForeignKey(branch => branch.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(school => school.Curricula)
            .WithOne(curriculum => curriculum.School)
            .HasForeignKey(curriculum => curriculum.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(school => school.Facilities)
            .WithOne(facility => facility.School)
            .HasForeignKey(facility => facility.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(school => school.Images)
            .WithOne(image => image.School)
            .HasForeignKey(image => image.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(school => school.TeamMembers)
            .WithOne(member => member.School)
            .HasForeignKey(member => member.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(school => school.AdditionalServices)
            .WithOne(service => service.School)
            .HasForeignKey(service => service.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(school => school.OwnerUserId);
    }
}
