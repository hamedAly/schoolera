using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class FavoriteSchoolConfiguration : IEntityTypeConfiguration<FavoriteSchool>
{
    public void Configure(EntityTypeBuilder<FavoriteSchool> builder)
    {
        builder.ToTable("FavoriteSchools");
        builder.HasKey(favorite => favorite.Id);

        builder.Property(favorite => favorite.ParentUserId).IsRequired();
        builder.Property(favorite => favorite.SchoolId).IsRequired();
        builder.Property(favorite => favorite.CreatedAtUtc).IsRequired();

        builder.HasIndex(favorite => new { favorite.ParentUserId, favorite.SchoolId })
            .IsUnique()
            .HasDatabaseName("IX_FavoriteSchools_ParentUserId_SchoolId");

        builder.HasIndex(favorite => favorite.ParentUserId);
        builder.HasIndex(favorite => favorite.SchoolId);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(favorite => favorite.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
