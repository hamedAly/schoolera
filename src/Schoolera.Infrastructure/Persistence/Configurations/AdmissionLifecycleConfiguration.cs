using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class AdmissionMissingItemsRequestConfiguration : IEntityTypeConfiguration<AdmissionMissingItemsRequest>
{
    public void Configure(EntityTypeBuilder<AdmissionMissingItemsRequest> builder)
    {
        builder.ToTable("AdmissionMissingItemsRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.ParentVisibleReason).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote).IsRequired();
        builder.Property(request => request.Instructions).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.HasIndex(request => new { request.AdmissionApplicationId, request.ClearedAtUtc });
        builder.HasMany(request => request.Items)
            .WithOne()
            .HasForeignKey(item => item.MissingItemsRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(request => request.Items).AutoInclude(false);
    }
}

public sealed class AdmissionMissingItemConfiguration : IEntityTypeConfiguration<AdmissionMissingItem>
{
    public void Configure(EntityTypeBuilder<AdmissionMissingItem> builder)
    {
        builder.ToTable("AdmissionMissingItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.LabelAr).HasMaxLength(FieldLengthLimits.AdmissionQuestionLabel).IsRequired();
        builder.Property(item => item.LabelEn).HasMaxLength(FieldLengthLimits.AdmissionQuestionLabel).IsRequired();
        builder.HasIndex(item => item.MissingItemsRequestId);
        builder.HasIndex(item => item.RequirementSnapshotId);
        builder.HasIndex(item => item.QuestionSnapshotId);
    }
}

public sealed class AdmissionInterviewAppointmentConfiguration : IEntityTypeConfiguration<AdmissionInterviewAppointment>
{
    public void Configure(EntityTypeBuilder<AdmissionInterviewAppointment> builder)
    {
        builder.ToTable("AdmissionInterviewAppointments");
        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(appointment => appointment.Location).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.OnlineInstructions).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.ParentVisibleNotes).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.PreparationInstructions).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.OutcomeNotes).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.LastParentVisibleRescheduleReason).HasMaxLength(500);
        builder.Property(appointment => appointment.CancellationReason).HasMaxLength(500);
        builder.Property(appointment => appointment.RowVersion).IsRowVersion();
        builder.HasIndex(appointment => appointment.AdmissionApplicationId)
            .IsUnique().HasFilter("[Lifecycle] IN (1,5,7)");
        builder.HasIndex(appointment => new { appointment.InterviewAssessmentSlotId, appointment.Lifecycle });
        builder.HasOne(appointment => appointment.InterviewAssessmentSlot)
            .WithMany().HasForeignKey(appointment => appointment.InterviewAssessmentSlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionAssessmentAppointmentConfiguration : IEntityTypeConfiguration<AdmissionAssessmentAppointment>
{
    public void Configure(EntityTypeBuilder<AdmissionAssessmentAppointment> builder)
    {
        builder.ToTable("AdmissionAssessmentAppointments");
        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(appointment => appointment.Location).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.OnlineInstructions).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.ParentVisibleNotes).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.PreparationInstructions).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.OutcomeNotes).HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);
        builder.Property(appointment => appointment.LastParentVisibleRescheduleReason).HasMaxLength(500);
        builder.Property(appointment => appointment.CancellationReason).HasMaxLength(500);
        builder.Property(appointment => appointment.RowVersion).IsRowVersion();
        builder.HasIndex(appointment => appointment.AdmissionApplicationId)
            .IsUnique().HasFilter("[Lifecycle] IN (1,5,7)");
        builder.HasIndex(appointment => new { appointment.InterviewAssessmentSlotId, appointment.Lifecycle });
        builder.HasOne(appointment => appointment.InterviewAssessmentSlot)
            .WithMany().HasForeignKey(appointment => appointment.InterviewAssessmentSlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionAppointmentActionHistoryConfiguration
    : IEntityTypeConfiguration<AdmissionAppointmentActionHistory>
{
    public void Configure(EntityTypeBuilder<AdmissionAppointmentActionHistory> builder)
    {
        builder.ToTable("AdmissionAppointmentActionHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SafeReason).HasMaxLength(500);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.HasIndex(x => new { x.AppointmentId, x.IdempotencyKey })
            .IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(x => new { x.AdmissionApplicationId, x.CreatedAtUtc });
    }
}
