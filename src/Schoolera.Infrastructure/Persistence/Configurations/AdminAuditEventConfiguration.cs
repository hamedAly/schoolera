using Microsoft.EntityFrameworkCore;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class AdminAuditEventConfiguration : IEntityTypeConfiguration<AdminAuditEvent>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AdminAuditEvent> builder)
    {
        builder.ToTable("AdminAuditEvents");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Action).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.EntityId).HasMaxLength(64);
        builder.Property(entity => entity.Summary).HasMaxLength(512);
        builder.HasIndex(entity => entity.CreatedAtUtc);
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId });
        builder.HasIndex(entity => entity.Action);
    }
}
