using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolChildAgeEligibilityRuleConfiguration
    : IEntityTypeConfiguration<SchoolChildAgeEligibilityRule>
{
    public void Configure(EntityTypeBuilder<SchoolChildAgeEligibilityRule> builder)
    {
        builder.ToTable("SchoolChildAgeEligibilityRules");
        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.ReferenceDateMode).HasConversion<int>().IsRequired();
        builder.Property(rule => rule.PublicationStatus).HasConversion<int>().IsRequired();
        builder.Property(rule => rule.ScopeKey)
            .HasMaxLength(FieldLengthLimits.AgeEligibilityScopeKey)
            .IsRequired();
        builder.Property(rule => rule.ExplanationAr)
            .HasMaxLength(FieldLengthLimits.AgeEligibilityExplanation);
        builder.Property(rule => rule.ExplanationEn)
            .HasMaxLength(FieldLengthLimits.AgeEligibilityExplanation);
        builder.Property(rule => rule.EducationalStageId).IsRequired();
        builder.Property(rule => rule.AcademicYearId).IsRequired();
        builder.Property(rule => rule.MinAgeCompletedMonths).IsRequired();
        builder.Property(rule => rule.MaxAgeCompletedMonths).IsRequired();
        builder.Property(rule => rule.ManualExceptionAllowed).IsRequired();
        builder.Property(rule => rule.CreatedByUserId).IsRequired();
        builder.Property(rule => rule.UpdatedByUserId).IsRequired();
        builder.Property(rule => rule.CreatedAtUtc).IsRequired();
        builder.Property(rule => rule.UpdatedAtUtc).IsRequired();
        builder.Property(rule => rule.RuleVersion).IsRequired();
        builder.Property(rule => rule.IsActive).IsRequired();
        builder.Property(rule => rule.RowVersion).IsRowVersion();

        builder.HasOne(rule => rule.School)
            .WithMany()
            .HasForeignKey(rule => rule.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rule => new { rule.SchoolId, rule.ScopeKey })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [PublicationStatus] = 2");

        builder.HasIndex(rule => new
        {
            rule.SchoolId,
            rule.PublicationStatus,
            rule.IsActive,
        });
    }
}

public sealed class AdmissionApplicationChildAgeEligibilitySnapshotConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationChildAgeEligibilitySnapshot>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationChildAgeEligibilitySnapshot> builder)
    {
        builder.ToTable("AdmissionApplicationChildAgeEligibilitySnapshots");
        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.ReferenceDateMode).HasConversion<int>().IsRequired();
        builder.Property(snapshot => snapshot.ResultCode).HasConversion<int>().IsRequired();
        builder.Property(snapshot => snapshot.ManualExceptionReasonCode).HasConversion<int>();
        builder.Property(snapshot => snapshot.ManualExceptionReasonNote)
            .HasMaxLength(FieldLengthLimits.AgeEligibilityExceptionNote);
        builder.Property(snapshot => snapshot.RuleVersion).IsRequired();
        builder.Property(snapshot => snapshot.SchoolBranchId).IsRequired();
        builder.Property(snapshot => snapshot.EducationalStageId).IsRequired();
        builder.Property(snapshot => snapshot.GradeId).IsRequired();
        builder.Property(snapshot => snapshot.AcademicYearId).IsRequired();
        builder.Property(snapshot => snapshot.MinAgeCompletedMonths).IsRequired();
        builder.Property(snapshot => snapshot.MaxAgeCompletedMonths).IsRequired();
        builder.Property(snapshot => snapshot.ReferenceDate).IsRequired();
        builder.Property(snapshot => snapshot.CalculatedAtUtc).IsRequired();
        builder.Property(snapshot => snapshot.ManualExceptionAllowedAtEvaluation).IsRequired();
        builder.Property(snapshot => snapshot.ManualExceptionIsApproved).IsRequired();

        builder.HasIndex(snapshot => snapshot.AdmissionApplicationId).IsUnique();
        builder.HasIndex(snapshot => snapshot.SourceRuleId);
    }
}

public sealed class SchoolChildAgeEligibilityRuleAuditConfiguration
    : IEntityTypeConfiguration<SchoolChildAgeEligibilityRuleAudit>
{
    public void Configure(EntityTypeBuilder<SchoolChildAgeEligibilityRuleAudit> builder)
    {
        builder.ToTable("SchoolChildAgeEligibilityRuleAudits");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Action)
            .HasMaxLength(FieldLengthLimits.AgeEligibilityAuditAction)
            .IsRequired();
        builder.Property(audit => audit.Metadata)
            .HasMaxLength(FieldLengthLimits.AgeEligibilityAuditMetadata);
        builder.Property(audit => audit.ActorUserId).IsRequired();
        builder.Property(audit => audit.CreatedAtUtc).IsRequired();
        builder.HasIndex(audit => new { audit.SchoolId, audit.CreatedAtUtc });
    }
}
