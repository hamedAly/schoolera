using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class PlatformIntegrationConfigurationConfiguration
    : IEntityTypeConfiguration<PlatformIntegrationConfiguration>
{
    public void Configure(EntityTypeBuilder<PlatformIntegrationConfiguration> builder)
    {
        builder.ToTable("PlatformIntegrationConfigurations");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.IntegrationType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.ProviderCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.DisplayNameAr)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.DisplayNameEn)
            .HasMaxLength(200);

        builder.Property(entity => entity.SettingsJson)
            .HasMaxLength(16_000)
            .IsRequired();

        builder.Property(entity => entity.HealthStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.LastSafeFailureCode)
            .HasMaxLength(100);

        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();
        builder.Property(entity => entity.RowVersion).IsRowVersion();

        builder.HasIndex(entity => entity.IntegrationType);
        builder.HasIndex(entity => new { entity.IntegrationType, entity.ProviderCode });
        builder.HasIndex(entity => entity.IsActive);
        builder.HasIndex(entity => entity.IntegrationType)
            .IsUnique()
            .HasFilter("[IsDefault] = 1 AND [IsActive] = 1")
            .HasDatabaseName("IX_PlatformIntegrationConfigurations_DefaultPerType");
    }
}

public sealed class NotificationOutboxMessageConfiguration
    : IEntityTypeConfiguration<NotificationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxMessage> builder)
    {
        builder.ToTable("NotificationOutboxMessages");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.Channel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.Culture)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(entity => entity.TemplateCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.Subject)
            .HasMaxLength(500);

        builder.Property(entity => entity.BodyOrPayload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.DeduplicationKey)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(entity => entity.SafeMetadataJson)
            .HasMaxLength(2000);

        builder.Property(entity => entity.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(entity => entity.LastSafeFailureCode)
            .HasMaxLength(100);

        builder.Property(entity => entity.ActionPath)
            .HasMaxLength(500);

        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();
        builder.Property(entity => entity.NextAttemptAtUtc).IsRequired();
        builder.Property(entity => entity.RowVersion).IsRowVersion();

        builder.HasIndex(entity => entity.DeduplicationKey)
            .IsUnique()
            .HasDatabaseName("IX_NotificationOutboxMessages_DeduplicationKey");

        builder.HasIndex(entity => new { entity.Status, entity.NextAttemptAtUtc });
        builder.HasIndex(entity => new { entity.RecipientUserId, entity.Channel, entity.CreatedAtUtc });
        builder.HasIndex(entity => entity.EventType);
        builder.HasIndex(entity => entity.RelatedSchoolId);
    }
}

public sealed class NotificationTemplateConfiguration
    : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.Channel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.Culture)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(entity => entity.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();

        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => new { entity.EventType, entity.Channel, entity.Culture })
            .IsUnique()
            .HasDatabaseName("IX_NotificationTemplates_EventChannelCulture");

        builder.HasMany(entity => entity.Versions)
            .WithOne(version => version.Template)
            .HasForeignKey(version => version.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(NotificationTemplate.Versions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class NotificationTemplateVersionConfiguration
    : IEntityTypeConfiguration<NotificationTemplateVersion>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateVersion> builder)
    {
        builder.ToTable("NotificationTemplateVersions");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Subject)
            .HasMaxLength(500);

        builder.Property(entity => entity.Body)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(entity => entity.AllowedVariablesCsv)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(entity => entity.ProviderTemplateId)
            .HasMaxLength(200);

        builder.Property(entity => entity.CreatedAtUtc).IsRequired();

        builder.HasIndex(entity => new { entity.TemplateId, entity.VersionNumber })
            .IsUnique();

        builder.HasIndex(entity => new { entity.TemplateId, entity.IsPublished, entity.VersionNumber });
    }
}

public sealed class ParentNotificationPreferenceConfiguration
    : IEntityTypeConfiguration<ParentNotificationPreference>
{
    public void Configure(EntityTypeBuilder<ParentNotificationPreference> builder)
    {
        builder.ToTable("ParentNotificationPreferences");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.EmailConsentSource)
            .HasMaxLength(100);

        builder.Property(entity => entity.SmsConsentSource)
            .HasMaxLength(100);

        builder.Property(entity => entity.WhatsAppConsentSource)
            .HasMaxLength(100);

        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();
        builder.Property(entity => entity.RowVersion).IsRowVersion();

        builder.HasIndex(entity => entity.ParentUserId)
            .IsUnique()
            .HasDatabaseName("IX_ParentNotificationPreferences_ParentUserId");
    }
}

public sealed class ParentAdmissionOpenSubscriptionConfiguration
    : IEntityTypeConfiguration<ParentAdmissionOpenSubscription>
{
    public void Configure(EntityTypeBuilder<ParentAdmissionOpenSubscription> builder)
    {
        builder.ToTable("ParentAdmissionOpenSubscriptions");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.PreferredChannel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();
        builder.Property(entity => entity.RowVersion).IsRowVersion();

        builder.HasIndex(entity => entity.ParentUserId);
        builder.HasIndex(entity => new { entity.SchoolId, entity.IsActive });

        builder.HasIndex(entity => new
            {
                entity.ParentUserId,
                entity.SchoolId,
                entity.SchoolBranchId,
                entity.EducationalStageId,
                entity.GradeId,
                entity.AcademicYearId,
            })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_ParentAdmissionOpenSubscriptions_ActiveScope");
    }
}
