using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolOnboardingStatusHistoryConfiguration
    : IEntityTypeConfiguration<SchoolOnboardingStatusHistory>
{
    public void Configure(EntityTypeBuilder<SchoolOnboardingStatusHistory> builder)
    {
        builder.ToTable("SchoolOnboardingStatusHistory");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.PreviousStatus).HasConversion<int>();
        builder.Property(history => history.NewStatus).HasConversion<int>().IsRequired();
        builder.Property(history => history.ActorUserId).IsRequired();
        builder.Property(history => history.OwnerVisibleReason).HasMaxLength(FieldLengthLimits.OnboardingReason);
        builder.Property(history => history.InternalNote).HasMaxLength(FieldLengthLimits.OnboardingReason);
        builder.Property(history => history.CreatedAtUtc).IsRequired();

        builder.HasIndex(history => new { history.ApplicationId, history.CreatedAtUtc });
    }
}
