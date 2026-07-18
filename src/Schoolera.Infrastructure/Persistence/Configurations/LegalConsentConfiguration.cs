using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class LegalDocumentVersionConfiguration : IEntityTypeConfiguration<LegalDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LegalDocumentVersion> builder)
    {
        builder.ToTable("LegalDocumentVersions");
        builder.HasKey(version => version.Id);

        builder.Property(version => version.DocumentType).HasConversion<int>().IsRequired();
        builder.Property(version => version.Culture)
            .HasMaxLength(FieldLengthLimits.LegalCulture)
            .IsRequired();
        builder.Property(version => version.VersionNumber).IsRequired();
        builder.Property(version => version.Title)
            .HasMaxLength(FieldLengthLimits.LegalTitle)
            .IsRequired();
        builder.Property(version => version.Content)
            .HasMaxLength(FieldLengthLimits.LegalContent)
            .IsRequired();
        builder.Property(version => version.PublishedAtUtc).IsRequired();
        builder.Property(version => version.IsCurrentMandatory).IsRequired();
        builder.Property(version => version.CreatedAtUtc).IsRequired();

        builder.HasIndex(version => new { version.DocumentType, version.Culture, version.VersionNumber })
            .IsUnique();
        builder.HasIndex(version => new { version.DocumentType, version.Culture, version.IsCurrentMandatory });
    }
}

public sealed class LegalAcceptanceConfiguration : IEntityTypeConfiguration<LegalAcceptance>
{
    public void Configure(EntityTypeBuilder<LegalAcceptance> builder)
    {
        builder.ToTable("LegalAcceptances");
        builder.HasKey(acceptance => acceptance.Id);

        builder.Property(acceptance => acceptance.Purpose).HasConversion<int>().IsRequired();
        builder.Property(acceptance => acceptance.AcceptedAtUtc).IsRequired();

        builder.HasIndex(acceptance => new
            {
                acceptance.UserId,
                acceptance.LegalDocumentVersionId,
                acceptance.Purpose,
            })
            .IsUnique();

        builder.HasIndex(acceptance => new { acceptance.UserId, acceptance.Purpose });

        builder.HasOne(acceptance => acceptance.LegalDocumentVersion)
            .WithMany()
            .HasForeignKey(acceptance => acceptance.LegalDocumentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
