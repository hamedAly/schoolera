using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolAdmissionEvaluationTemplateConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionEvaluationTemplate>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionEvaluationTemplate> builder)
    {
        builder.ToTable("SchoolAdmissionEvaluationTemplates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScopeKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Criteria).WithOne()
            .HasForeignKey(x => x.SchoolAdmissionEvaluationTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.SchoolId, x.PublicationStatus, x.IsActive });
        builder.HasIndex(x => new { x.SchoolId, x.ScopeKey, x.Kind, x.PublicationStatus })
            .HasFilter("[PublicationStatus] = 2 AND [IsActive] = 1");
    }
}

public sealed class SchoolAdmissionEvaluationCriterionConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionEvaluationCriterion>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionEvaluationCriterion> builder)
    {
        builder.ToTable("SchoolAdmissionEvaluationCriteria");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LabelAr).HasMaxLength(300).IsRequired();
        builder.Property(x => x.LabelEn).HasMaxLength(300).IsRequired();
        builder.Property(x => x.HelpTextAr).HasMaxLength(1000);
        builder.Property(x => x.HelpTextEn).HasMaxLength(1000);
        builder.HasMany(x => x.Options).WithOne()
            .HasForeignKey(x => x.SchoolAdmissionEvaluationCriterionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.SchoolAdmissionEvaluationTemplateId, x.SortOrder });
    }
}

public sealed class SchoolAdmissionEvaluationCriterionOptionConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionEvaluationCriterionOption>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionEvaluationCriterionOption> builder)
    {
        builder.ToTable("SchoolAdmissionEvaluationCriterionOptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LabelAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LabelEn).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.SchoolAdmissionEvaluationCriterionId, x.Value }).IsUnique();
    }
}

public sealed class AdmissionEvaluationResultConfiguration
    : IEntityTypeConfiguration<AdmissionEvaluationResult>
{
    public void Configure(EntityTypeBuilder<AdmissionEvaluationResult> builder)
    {
        builder.ToTable("AdmissionEvaluationResults");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TemplateSnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.AppointmentSnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.DraftAnswersJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SuggestedParentReasonAr).HasMaxLength(2000);
        builder.Property(x => x.SuggestedParentReasonEn).HasMaxLength(2000);
        builder.Property(x => x.InternalNotes).HasMaxLength(4000);
        builder.Property(x => x.PendingCorrectionReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<AdmissionApplication>().WithMany()
            .HasForeignKey(x => x.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Versions).WithOne()
            .HasForeignKey(x => x.AdmissionEvaluationResultId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AppointmentId, x.Kind }).IsUnique();
        builder.HasIndex(x => new { x.AdmissionApplicationId, x.Kind });
    }
}

public sealed class SchoolAdmissionEvaluationTemplateAuditConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionEvaluationTemplateAudit>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionEvaluationTemplateAudit> builder)
    {
        builder.ToTable("SchoolAdmissionEvaluationTemplateAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.HasIndex(x => new { x.SchoolAdmissionEvaluationTemplateId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.SchoolId, x.IdempotencyKey })
            .IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
    }
}

public sealed class AdmissionEvaluationResultVersionConfiguration
    : IEntityTypeConfiguration<AdmissionEvaluationResultVersion>
{
    public void Configure(EntityTypeBuilder<AdmissionEvaluationResultVersion> builder)
    {
        builder.ToTable("AdmissionEvaluationResultVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TemplateSnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.AppointmentSnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.AnswersJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SuggestedParentReasonAr).HasMaxLength(2000);
        builder.Property(x => x.SuggestedParentReasonEn).HasMaxLength(2000);
        builder.Property(x => x.InternalNotes).HasMaxLength(4000);
        builder.Property(x => x.CorrectionReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.AdmissionEvaluationResultId, x.VersionNumber }).IsUnique();
    }
}

public sealed class AdmissionEvaluationHistoryConfiguration
    : IEntityTypeConfiguration<AdmissionEvaluationHistory>
{
    public void Configure(EntityTypeBuilder<AdmissionEvaluationHistory> builder)
    {
        builder.ToTable("AdmissionEvaluationHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
        builder.HasOne<AdmissionEvaluationResult>().WithMany()
            .HasForeignKey(x => x.AdmissionEvaluationResultId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AdmissionEvaluationResultId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.AdmissionEvaluationResultId, x.IdempotencyKey })
            .IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
    }
}
