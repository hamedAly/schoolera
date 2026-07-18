using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class AdmissionMeetingSessionConfiguration
    : IEntityTypeConfiguration<AdmissionMeetingSession>
{
    public void Configure(EntityTypeBuilder<AdmissionMeetingSession> builder)
    {
        builder.ToTable("AdmissionMeetingSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ConfigurationRowVersion).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProvisioningIdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ProviderMeetingReference).HasMaxLength(200);
        builder.Property(x => x.LastSafeProviderStatusCode).HasMaxLength(100);
        builder.Property(x => x.LastSafeFailureCode).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<AdmissionApplication>().WithMany()
            .HasForeignKey(x => x.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformIntegrationConfiguration>().WithMany()
            .HasForeignKey(x => x.IntegrationConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AppointmentId, x.Kind, x.Generation }).IsUnique();
        builder.HasIndex(x => x.ProvisioningIdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextRetryAtUtc });
    }
}

public sealed class AdmissionMeetingSessionHistoryConfiguration
    : IEntityTypeConfiguration<AdmissionMeetingSessionHistory>
{
    public void Configure(EntityTypeBuilder<AdmissionMeetingSessionHistory> builder)
    {
        builder.ToTable("AdmissionMeetingSessionHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SafeCode).HasMaxLength(100);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.HasOne<AdmissionMeetingSession>().WithMany()
            .HasForeignKey(x => x.AdmissionMeetingSessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AdmissionMeetingSessionId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.AdmissionMeetingSessionId, x.IdempotencyKey }).IsUnique();
    }
}
