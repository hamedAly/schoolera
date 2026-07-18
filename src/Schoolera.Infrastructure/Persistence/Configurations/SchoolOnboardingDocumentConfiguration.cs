using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolOnboardingDocumentConfiguration
    : IEntityTypeConfiguration<SchoolOnboardingDocument>
{
    public void Configure(EntityTypeBuilder<SchoolOnboardingDocument> builder)
    {
        builder.ToTable("SchoolOnboardingDocuments");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.OriginalFileName)
            .HasMaxLength(FieldLengthLimits.OnboardingFileName)
            .IsRequired();

        builder.Property(document => document.StoredFileReference)
            .HasMaxLength(FieldLengthLimits.OnboardingStoredReference)
            .IsRequired();

        builder.Property(document => document.ContentType)
            .HasMaxLength(FieldLengthLimits.OnboardingContentType)
            .IsRequired();

        builder.Property(document => document.FileSize).IsRequired();

        builder.Property(document => document.Sha256Hash)
            .HasMaxLength(FieldLengthLimits.Sha256Hex);

        builder.Property(document => document.UploadedAtUtc).IsRequired();
        builder.Property(document => document.UploadedByUserId).IsRequired();
        builder.Property(document => document.IsCurrent).IsRequired();

        builder.HasIndex(document => new { document.ApplicationId, document.DocumentTypeId, document.IsCurrent });

        // At most one current document per application/document-type.
        builder.HasIndex(document => new { document.ApplicationId, document.DocumentTypeId })
            .IsUnique()
            .HasFilter("[IsCurrent] = 1")
            .HasDatabaseName("IX_SchoolOnboardingDocuments_Current_Unique");

        builder.HasOne(document => document.DocumentType)
            .WithMany()
            .HasForeignKey(document => document.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
