using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.ToTable("VerificationCodes");

        builder.HasKey(code => code.Id);

        builder.Property(code => code.Purpose)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(code => code.CodeHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(code => new { code.UserId, code.Purpose, code.IsConsumed });

        builder.HasOne(code => code.User)
            .WithMany()
            .HasForeignKey(code => code.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
