using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class CourierProviderProfileConfiguration : IEntityTypeConfiguration<CourierProviderProfile>
{
    public void Configure(EntityTypeBuilder<CourierProviderProfile> builder)
    {
        builder.ToTable("CourierProviderProfiles");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.DescriptionAr)
            .HasMaxLength(FieldLengthLimits.CourierDescription)
            .IsRequired();
        builder.Property(profile => profile.DescriptionEn)
            .HasMaxLength(FieldLengthLimits.CourierDescription)
            .IsRequired();
        builder.Property(profile => profile.LogoReference)
            .HasMaxLength(FieldLengthLimits.CourierLogoReference);
        builder.Property(profile => profile.TermsUrl)
            .HasMaxLength(FieldLengthLimits.Url)
            .IsRequired();
        builder.Property(profile => profile.PrivacyUrl)
            .HasMaxLength(FieldLengthLimits.Url)
            .IsRequired();
        builder.Property(profile => profile.CreatedAtUtc).IsRequired();
        builder.Property(profile => profile.UpdatedAtUtc).IsRequired();
        builder.Property(profile => profile.RowVersion).IsRowVersion();

        builder.HasIndex(profile => profile.IntegrationId).IsUnique();
        builder.HasOne(profile => profile.Integration)
            .WithOne()
            .HasForeignKey<CourierProviderProfile>(profile => profile.IntegrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierServiceConfiguration : IEntityTypeConfiguration<CourierService>
{
    public void Configure(EntityTypeBuilder<CourierService> builder)
    {
        builder.ToTable("CourierServices");
        builder.HasKey(service => service.Id);
        builder.Property(service => service.ServiceType).HasConversion<int>().IsRequired();
        builder.Property(service => service.Code)
            .HasMaxLength(FieldLengthLimits.CourierCode)
            .IsRequired();
        builder.Property(service => service.NameAr)
            .HasMaxLength(FieldLengthLimits.ServiceName)
            .IsRequired();
        builder.Property(service => service.NameEn)
            .HasMaxLength(FieldLengthLimits.ServiceName)
            .IsRequired();
        builder.Property(service => service.DescriptionAr)
            .HasMaxLength(FieldLengthLimits.CourierDescription)
            .IsRequired();
        builder.Property(service => service.DescriptionEn)
            .HasMaxLength(FieldLengthLimits.CourierDescription)
            .IsRequired();
        builder.Property(service => service.DailyCutoffLocalTime).HasColumnType("time").IsRequired();
        builder.Property(service => service.MaximumEnvelopeLengthCm).HasPrecision(8, 2).IsRequired();
        builder.Property(service => service.MaximumEnvelopeWidthCm).HasPrecision(8, 2).IsRequired();
        builder.Property(service => service.MaximumEnvelopeHeightCm).HasPrecision(8, 2).IsRequired();
        builder.Property(service => service.CreatedAtUtc).IsRequired();
        builder.Property(service => service.UpdatedAtUtc).IsRequired();
        builder.Property(service => service.RowVersion).IsRowVersion();

        builder.HasIndex(service => new { service.IntegrationId, service.Code })
            .IsUnique()
            .HasDatabaseName("IX_CourierServices_Integration_Code");
        builder.HasIndex(service => new { service.IntegrationId, service.IsActive, service.SortOrder });
        builder.HasOne(service => service.Integration)
            .WithMany()
            .HasForeignKey(service => service.IntegrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierCoverageRuleConfiguration : IEntityTypeConfiguration<CourierCoverageRule>
{
    public void Configure(EntityTypeBuilder<CourierCoverageRule> builder)
    {
        builder.ToTable("CourierCoverageRules");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Result).HasConversion<int>().IsRequired();
        builder.Property(rule => rule.ScopeKey)
            .HasMaxLength(FieldLengthLimits.CourierScopeKey)
            .IsRequired();
        builder.Property(rule => rule.NotesAr).HasMaxLength(FieldLengthLimits.Notes);
        builder.Property(rule => rule.NotesEn).HasMaxLength(FieldLengthLimits.Notes);
        builder.Property(rule => rule.CreatedAtUtc).IsRequired();
        builder.Property(rule => rule.UpdatedAtUtc).IsRequired();
        builder.Property(rule => rule.RowVersion).IsRowVersion();

        builder.HasIndex(rule => new { rule.IntegrationId, rule.ServiceId, rule.ScopeKey })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_CourierCoverageRules_ActiveScope");
        builder.HasIndex(rule => new { rule.IntegrationId, rule.IsActive, rule.Result });
        builder.HasIndex(rule => rule.CountryId);
        builder.HasIndex(rule => rule.GovernorateId);
        builder.HasIndex(rule => rule.CityId);
        builder.HasIndex(rule => rule.DistrictId);

        builder.HasOne(rule => rule.Integration)
            .WithMany()
            .HasForeignKey(rule => rule.IntegrationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rule => rule.Service)
            .WithMany()
            .HasForeignKey(rule => rule.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rule => rule.Country)
            .WithMany()
            .HasForeignKey(rule => rule.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rule => rule.Governorate)
            .WithMany()
            .HasForeignKey(rule => rule.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rule => rule.City)
            .WithMany()
            .HasForeignKey(rule => rule.CityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rule => rule.District)
            .WithMany()
            .HasForeignKey(rule => rule.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierOperatingWindowConfiguration : IEntityTypeConfiguration<CourierOperatingWindow>
{
    public void Configure(EntityTypeBuilder<CourierOperatingWindow> builder)
    {
        builder.ToTable("CourierOperatingWindows");
        builder.HasKey(window => window.Id);
        builder.Property(window => window.ScopeKey)
            .HasMaxLength(FieldLengthLimits.CourierScopeKey)
            .IsRequired();
        builder.Property(window => window.TimeZoneId)
            .HasMaxLength(FieldLengthLimits.CourierTimeZone)
            .IsRequired();
        builder.Property(window => window.DayOfWeek).HasConversion<int>().IsRequired();
        builder.Property(window => window.LocalStartTime).HasColumnType("time").IsRequired();
        builder.Property(window => window.LocalEndTime).HasColumnType("time").IsRequired();
        builder.Property(window => window.CreatedAtUtc).IsRequired();
        builder.Property(window => window.UpdatedAtUtc).IsRequired();
        builder.Property(window => window.RowVersion).IsRowVersion();

        builder.HasIndex(window => new
            {
                window.IntegrationId,
                window.ServiceId,
                window.ScopeKey,
                window.DayOfWeek,
                window.LocalStartTime,
                window.LocalEndTime,
            })
            .IsUnique()
            .HasDatabaseName("IX_CourierOperatingWindows_ExactWindow");
        builder.HasIndex(window => new
            {
                window.IntegrationId,
                window.ServiceId,
                window.ScopeKey,
                window.IsActive,
                window.DayOfWeek,
            });

        builder.HasOne(window => window.Integration)
            .WithMany()
            .HasForeignKey(window => window.IntegrationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(window => window.Service)
            .WithMany()
            .HasForeignKey(window => window.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(window => window.CoverageRule)
            .WithMany()
            .HasForeignKey(window => window.CoverageRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierSlaDefinitionConfiguration : IEntityTypeConfiguration<CourierSlaDefinition>
{
    public void Configure(EntityTypeBuilder<CourierSlaDefinition> builder)
    {
        builder.ToTable("CourierSlaDefinitions");
        builder.HasKey(sla => sla.Id);
        builder.Property(sla => sla.ScopeKey)
            .HasMaxLength(FieldLengthLimits.CourierScopeKey)
            .IsRequired();
        builder.Property(sla => sla.CreatedAtUtc).IsRequired();
        builder.Property(sla => sla.UpdatedAtUtc).IsRequired();
        builder.Property(sla => sla.RowVersion).IsRowVersion();

        builder.HasIndex(sla => new { sla.IntegrationId, sla.ServiceId, sla.ScopeKey })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_CourierSlaDefinitions_ActiveScope");
        builder.HasIndex(sla => new { sla.IntegrationId, sla.IsActive });

        builder.HasOne(sla => sla.Integration)
            .WithMany()
            .HasForeignKey(sla => sla.IntegrationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(sla => sla.Service)
            .WithMany()
            .HasForeignKey(sla => sla.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(sla => sla.CoverageRule)
            .WithMany()
            .HasForeignKey(sla => sla.CoverageRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierHealthCheckRecordConfiguration
    : IEntityTypeConfiguration<CourierHealthCheckRecord>
{
    public void Configure(EntityTypeBuilder<CourierHealthCheckRecord> builder)
    {
        builder.ToTable("CourierHealthCheckRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Status).HasConversion<int>().IsRequired();
        builder.Property(record => record.SafeCode)
            .HasMaxLength(FieldLengthLimits.CourierSafeCode);
        builder.Property(record => record.CheckedAtUtc).IsRequired();
        builder.Property(record => record.CreatedAtUtc).IsRequired();

        builder.HasIndex(record => new { record.IntegrationId, record.CheckedAtUtc });
        builder.HasIndex(record => new { record.Status, record.CheckedAtUtc });
        builder.HasOne(record => record.Integration)
            .WithMany()
            .HasForeignKey(record => record.IntegrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
