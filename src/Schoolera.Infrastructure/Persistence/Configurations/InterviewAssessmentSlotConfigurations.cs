using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class InterviewAssessmentSlotConfiguration : IEntityTypeConfiguration<InterviewAssessmentSlot>
{
    public void Configure(EntityTypeBuilder<InterviewAssessmentSlot> b)
    {
        b.ToTable("InterviewAssessmentSlots");
        b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<int>();
        b.Property(x => x.DeliveryMode).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.ResourceKind).HasConversion<int>();
        b.Property(x => x.TimeZoneId).HasMaxLength(128).IsRequired();
        b.Property(x => x.InstructionsAr).HasMaxLength(2000);
        b.Property(x => x.InstructionsEn).HasMaxLength(2000);
        b.Property(x => x.CancellationReasonAr).HasMaxLength(2000);
        b.Property(x => x.CancellationReasonEn).HasMaxLength(2000);
        b.Property(x => x.MeetingProviderCode).HasMaxLength(FieldLengthLimits.MeetingProviderCode);
        b.Property(x => x.GenerationBatchReference).HasMaxLength(128);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.SchoolId, x.SchoolBranchId, x.StartAtUtc, x.EndAtUtc });
        b.HasIndex(x => new { x.ResourceKind, x.ResourceReferenceId, x.StartAtUtc, x.EndAtUtc });
        b.HasIndex(x => x.GenerationBatchReference);
        b.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SchoolBranch>().WithMany().HasForeignKey(x => x.SchoolBranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<EducationalStage>().WithMany().HasForeignKey(x => x.EducationalStageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Grade>().WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InterviewAssessmentSlotAuditConfiguration : IEntityTypeConfiguration<InterviewAssessmentSlotAudit>
{
    public void Configure(EntityTypeBuilder<InterviewAssessmentSlotAudit> b)
    {
        b.ToTable("InterviewAssessmentSlotAudits"); b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(64).IsRequired();
        b.Property(x => x.Metadata).HasMaxLength(500);
        b.HasIndex(x => new { x.InterviewAssessmentSlotId, x.CreatedAtUtc });
        b.HasOne<InterviewAssessmentSlot>().WithMany().HasForeignKey(x => x.InterviewAssessmentSlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InterviewAssessmentSlotGenerationBatchConfiguration :
    IEntityTypeConfiguration<InterviewAssessmentSlotGenerationBatch>
{
    public void Configure(EntityTypeBuilder<InterviewAssessmentSlotGenerationBatch> b)
    {
        b.ToTable("InterviewAssessmentSlotGenerationBatches"); b.HasKey(x => x.Id);
        b.Property(x => x.RequestKey).HasMaxLength(128).IsRequired();
        b.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
        b.Property(x => x.BatchReference).HasMaxLength(128).IsRequired();
        b.HasIndex(x => new { x.SchoolId, x.RequestKey }).IsUnique();
        b.HasIndex(x => x.BatchReference).IsUnique();
    }
}
