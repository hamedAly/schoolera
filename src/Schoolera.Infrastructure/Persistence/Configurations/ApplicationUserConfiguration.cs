using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FirstName)
            .HasMaxLength(FieldLengthLimits.UserFirstName)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(FieldLengthLimits.UserLastName)
            .IsRequired();

        builder.Property(user => user.PreferredLanguage)
            .HasMaxLength(FieldLengthLimits.PreferredLanguage)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(FieldLengthLimits.UserPhone);

        builder.HasIndex(user => user.PhoneNumber)
            .IsUnique()
            .HasFilter("[PhoneNumber] IS NOT NULL");

        builder.Property(user => user.AccountStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
    }
}
