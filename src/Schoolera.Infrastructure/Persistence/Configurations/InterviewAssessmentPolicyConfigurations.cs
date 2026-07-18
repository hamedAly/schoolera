using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolInterviewAssessmentPolicyConfiguration
    : IEntityTypeConfiguration<SchoolInterviewAssessmentPolicy>
{
    public void Configure(EntityTypeBuilder<SchoolInterviewAssessmentPolicy> builder)
    {
        builder.ToTable("SchoolInterviewAssessmentPolicies");
        builder.HasKey(policy => policy.Id);

        builder.Property(policy => policy.RequirementMode).HasConversion<int>().IsRequired();
        builder.Property(policy => policy.DeliveryMode).HasConversion<int>();
        builder.Property(policy => policy.RequiredParticipants).HasConversion<int>();
        builder.Property(policy => policy.HybridSelectionAuthority).HasConversion<int>();
        builder.Property(policy => policy.PublicationStatus).HasConversion<int>().IsRequired();
        builder.Property(policy => policy.ScopeKey)
            .HasMaxLength(FieldLengthLimits.PolicyScopeKey)
            .IsRequired();
        builder.Property(policy => policy.PreparationNotesAr)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(policy => policy.PreparationNotesEn)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(policy => policy.OnSiteInstructionsAr)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(policy => policy.OnSiteInstructionsEn)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(policy => policy.OnlineInstructionsAr)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(policy => policy.OnlineInstructionsEn)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(policy => policy.MeetingProviderCode)
            .HasMaxLength(FieldLengthLimits.MeetingProviderCode);
        builder.Property(policy => policy.CreatedByUserId).IsRequired();
        builder.Property(policy => policy.UpdatedByUserId).IsRequired();
        builder.Property(policy => policy.CreatedAtUtc).IsRequired();
        builder.Property(policy => policy.UpdatedAtUtc).IsRequired();
        builder.Property(policy => policy.PolicyVersion).IsRequired();
        builder.Property(policy => policy.IsActive).IsRequired();
        builder.Property(policy => policy.ParentReschedulingAllowed).IsRequired();
        builder.Property(policy => policy.MaxParentRescheduleAttempts).IsRequired();
        builder.Property(policy => policy.ParentCancellationAllowed).IsRequired();
        builder.Property(policy => policy.RowVersion).IsRowVersion();

        builder.HasOne(policy => policy.School)
            .WithMany()
            .HasForeignKey(policy => policy.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(policy => new { policy.SchoolId, policy.ScopeKey })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [PublicationStatus] = 2");

        builder.HasIndex(policy => new
        {
            policy.SchoolId,
            policy.PublicationStatus,
            policy.IsActive,
        });
    }
}

public sealed class AdmissionApplicationInterviewAssessmentPolicySnapshotConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationInterviewAssessmentPolicySnapshot>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationInterviewAssessmentPolicySnapshot> builder)
    {
        builder.ToTable("AdmissionApplicationInterviewAssessmentPolicySnapshots");
        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.RequirementMode).HasConversion<int>().IsRequired();
        builder.Property(snapshot => snapshot.DeliveryMode).HasConversion<int>();
        builder.Property(snapshot => snapshot.RequiredParticipants).HasConversion<int>();
        builder.Property(snapshot => snapshot.HybridSelectionAuthority).HasConversion<int>();
        builder.Property(snapshot => snapshot.PreparationNotesAr)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(snapshot => snapshot.PreparationNotesEn)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(snapshot => snapshot.OnSiteInstructionsAr)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(snapshot => snapshot.OnSiteInstructionsEn)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(snapshot => snapshot.OnlineInstructionsAr)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(snapshot => snapshot.OnlineInstructionsEn)
            .HasMaxLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        builder.Property(snapshot => snapshot.MeetingProviderCode)
            .HasMaxLength(FieldLengthLimits.MeetingProviderCode);
        builder.Property(snapshot => snapshot.SourcePolicyId).IsRequired();
        builder.Property(snapshot => snapshot.PolicyVersion).IsRequired();
        builder.Property(snapshot => snapshot.SchoolBranchId).IsRequired();
        builder.Property(snapshot => snapshot.EducationalStageId).IsRequired();
        builder.Property(snapshot => snapshot.GradeId).IsRequired();
        builder.Property(snapshot => snapshot.AcademicYearId).IsRequired();
        builder.Property(snapshot => snapshot.CreatedAtUtc).IsRequired();
        builder.Property(snapshot => snapshot.ParentReschedulingAllowed).IsRequired();
        builder.Property(snapshot => snapshot.MaxParentRescheduleAttempts).IsRequired();
        builder.Property(snapshot => snapshot.ParentCancellationAllowed).IsRequired();

        builder.HasIndex(snapshot => snapshot.AdmissionApplicationId).IsUnique();
        builder.HasIndex(snapshot => snapshot.SourcePolicyId);
    }
}

public sealed class SchoolInterviewAssessmentPolicyAuditConfiguration
    : IEntityTypeConfiguration<SchoolInterviewAssessmentPolicyAudit>
{
    public void Configure(EntityTypeBuilder<SchoolInterviewAssessmentPolicyAudit> builder)
    {
        builder.ToTable("SchoolInterviewAssessmentPolicyAudits");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Action)
            .HasMaxLength(FieldLengthLimits.PolicyAuditAction)
            .IsRequired();
        builder.Property(audit => audit.Metadata)
            .HasMaxLength(FieldLengthLimits.PolicyAuditMetadata);
        builder.Property(audit => audit.ActorUserId).IsRequired();
        builder.Property(audit => audit.CreatedAtUtc).IsRequired();
        builder.HasIndex(audit => new { audit.SchoolId, audit.CreatedAtUtc });
    }
}
