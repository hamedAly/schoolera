using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolTeamMemberConfiguration : IEntityTypeConfiguration<SchoolTeamMember>
{
    public void Configure(EntityTypeBuilder<SchoolTeamMember> builder)
    {
        builder.ToTable("SchoolTeamMembers");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(member => member.BranchScopeMode)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(SchoolBranchScopeMode.AllBranches);

        builder.Property(member => member.IsActive)
            .IsRequired();

        builder.Property(member => member.CreatedAtUtc)
            .IsRequired();

        builder.Property(member => member.CreatedByUserId)
            .IsRequired();

        builder.Property(member => member.UpdatedAtUtc)
            .IsRequired();

        builder.HasMany(member => member.BranchAssignments)
            .WithOne(branch => branch.SchoolTeamMember)
            .HasForeignKey(branch => branch.SchoolTeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(member => member.BranchAssignments)
            .AutoInclude(false);

        // One active membership per school/user.
        builder.HasIndex(member => new { member.SchoolId, member.UserId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(member => new { member.UserId, member.IsActive });

        builder.HasIndex(member => member.SchoolId);
    }
}

public sealed class SchoolTeamMemberBranchConfiguration : IEntityTypeConfiguration<SchoolTeamMemberBranch>
{
    public void Configure(EntityTypeBuilder<SchoolTeamMemberBranch> builder)
    {
        builder.ToTable("SchoolTeamMemberBranches");

        builder.HasKey(entry => new { entry.SchoolTeamMemberId, entry.SchoolBranchId });

        builder.HasOne(entry => entry.SchoolBranch)
            .WithMany()
            .HasForeignKey(entry => entry.SchoolBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => entry.SchoolBranchId);
    }
}
